using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Entities;
using System.Collections.ObjectModel;

namespace HotelPOS.ViewModels
{
    /// <summary>Row model for the BOM ingredient grid.</summary>
    public partial class BomEntryRow : ObservableObject
    {
        public int Id { get; set; }
        public int RawMaterialId { get; set; }

        [ObservableProperty] private string _rawMaterialName = string.Empty;
        [ObservableProperty] private string _unit = string.Empty;
        [ObservableProperty] private decimal _quantityRequired;
        [ObservableProperty] private decimal _wastagePercentage;
        [ObservableProperty] private decimal _costPerUnit;

        // Computed
        public decimal EffectiveQuantity => QuantityRequired * (1 + WastagePercentage / 100m);
        public decimal IngredientCost => EffectiveQuantity * CostPerUnit;
        public decimal WastageCost => (EffectiveQuantity - QuantityRequired) * CostPerUnit;

        partial void OnQuantityRequiredChanged(decimal value)
        {
            OnPropertyChanged(nameof(EffectiveQuantity));
            OnPropertyChanged(nameof(IngredientCost));
            OnPropertyChanged(nameof(WastageCost));
        }

        partial void OnWastagePercentageChanged(decimal value)
        {
            OnPropertyChanged(nameof(EffectiveQuantity));
            OnPropertyChanged(nameof(IngredientCost));
            OnPropertyChanged(nameof(WastageCost));
        }

        partial void OnCostPerUnitChanged(decimal value)
        {
            OnPropertyChanged(nameof(IngredientCost));
            OnPropertyChanged(nameof(WastageCost));
        }

        public void RaiseCalculatedPropertiesChanged()
        {
            OnPropertyChanged(nameof(EffectiveQuantity));
            OnPropertyChanged(nameof(IngredientCost));
            OnPropertyChanged(nameof(WastageCost));
        }
    }

    public partial class BomViewModel : ObservableObject
    {
        private readonly IBomService _bomService;
        private readonly IItemService _itemService;
        private readonly IDialogService _dialogService;
        private List<Item> _allMenuItems = new();

        public BomViewModel(IBomService bomService, IItemService itemService, IDialogService dialogService)
        {
            _bomService = bomService;
            _itemService = itemService;
            _dialogService = dialogService;
        }

        // Item selection
        [ObservableProperty] private ObservableCollection<Item> _menuItems = new();
        [ObservableProperty] private Item? _selectedItem;
        [ObservableProperty] private string _itemSearchText = string.Empty;

        // Ingredient grid
        [ObservableProperty] private ObservableCollection<BomEntryRow> _bomRows = new();
        [ObservableProperty] private BomEntryRow? _selectedBomRow;

        // Available raw materials for the ComboBox in the row
        [ObservableProperty] private ObservableCollection<RawMaterial> _allRawMaterials = new();

        // Cost summary
        [ObservableProperty] private decimal _totalNetCost;
        [ObservableProperty] private decimal _totalWastageCost;
        [ObservableProperty] private decimal _totalFoodCost;
        [ObservableProperty] private decimal _menuPrice;
        [ObservableProperty] private decimal _grossMargin;

        [ObservableProperty] private bool _isLoading;

        partial void OnItemSearchTextChanged(string value) => FilterItems();

        public async Task LoadAsync()
        {
            IsLoading = true;
            try
            {
                _allMenuItems = (await _itemService.GetItemsAsync()).ToList();
                FilterItems();

                var raws = await _bomService.GetAllRawMaterialsAsync();
                AllRawMaterials = new ObservableCollection<RawMaterial>(raws);
            }
            finally { IsLoading = false; }
        }

        private void FilterItems()
        {
            var filtered = string.IsNullOrWhiteSpace(ItemSearchText)
                ? _allMenuItems
                : _allMenuItems.Where(i => i.Name.Contains(ItemSearchText, StringComparison.OrdinalIgnoreCase)).ToList();

            MenuItems = new ObservableCollection<Item>(filtered);
        }

        partial void OnSelectedItemChanged(Item? value)
        {
            if (value == null)
            {
                BomRows.Clear();
                RecalculateCosts();
                return;
            }
            _ = LoadBomForItemAsync(value.Id);
        }

        private async Task LoadBomForItemAsync(int itemId)
        {
            IsLoading = true;
            try
            {
                var entries = await _bomService.GetBomForItemAsync(itemId);
                BomRows = new ObservableCollection<BomEntryRow>(
                    entries.Select(e => new BomEntryRow
                    {
                        Id = e.Id,
                        RawMaterialId = e.RawMaterialId,
                        RawMaterialName = e.RawMaterial?.Name ?? string.Empty,
                        Unit = e.RawMaterial?.Unit ?? string.Empty,
                        CostPerUnit = e.RawMaterial?.CostPerUnit ?? 0,
                        QuantityRequired = e.QuantityRequired,
                        WastagePercentage = e.WastagePercentage,
                    }));

                BomRows.CollectionChanged += (_, _) => RecalculateCosts();
                foreach (var row in BomRows)
                {
                    row.PropertyChanged += (_, _) => RecalculateCosts();
                }

                MenuPrice = SelectedItem?.Price ?? 0;
                RecalculateCosts();
            }
            finally { IsLoading = false; }
        }

        [RelayCommand]
        private void AddIngredient()
        {
            if (SelectedItem == null) return;
            var row = new BomEntryRow { QuantityRequired = 1, WastagePercentage = 0 };
            row.PropertyChanged += (_, _) => RecalculateCosts();
            BomRows.Add(row);
        }

        [RelayCommand]
        private void RemoveIngredient(BomEntryRow? row)
        {
            if (row == null) return;
            BomRows.Remove(row);
            RecalculateCosts();
        }

        [RelayCommand]
        private async Task SaveBomAsync()
        {
            if (SelectedItem == null) return;

            var entries = BomRows
                .Where(r => r.RawMaterialId > 0 && r.QuantityRequired > 0)
                .Select(r => new BomEntry
                {
                    Id = r.Id,
                    ItemId = SelectedItem.Id,
                    RawMaterialId = r.RawMaterialId,
                    QuantityRequired = r.QuantityRequired,
                    WastagePercentage = r.WastagePercentage
                }).ToList();

            var duplicateNames = entries
                .GroupBy(e => e.RawMaterialId)
                .Where(g => g.Count() > 1)
                .Select(g => BomRows.First(r => r.RawMaterialId == g.Key).RawMaterialName)
                .ToList();

            if (duplicateNames.Count > 0)
            {
                await _dialogService.ShowMessageAsync(
                    $"Each ingredient can only appear once in a recipe. Remove the duplicate row(s) for: {string.Join(", ", duplicateNames)}.",
                    "Duplicate Ingredient",
                    DialogButton.OK, DialogIcon.Warning);
                return;
            }

            await _bomService.SaveBomAsync(SelectedItem.Id, entries);
            await _dialogService.ShowMessageAsync($"Recipe for '{SelectedItem.Name}' saved successfully.", "Saved", DialogButton.OK, DialogIcon.Information);
            await LoadBomForItemAsync(SelectedItem.Id);
        }

        [RelayCommand]
        private async Task ClearBomAsync()
        {
            if (SelectedItem == null) return;
            var result = await _dialogService.ShowMessageAsync(
                $"Remove all ingredients from '{SelectedItem.Name}'?",
                "Clear Recipe",
                DialogButton.YesNo, DialogIcon.Warning);
            if (result != DialogResult.Yes) return;

            BomRows.Clear();
            RecalculateCosts();
        }

        public void OnRawMaterialSelected(BomEntryRow row, RawMaterial material)
        {
            row.RawMaterialId = material.Id;
            row.RawMaterialName = material.Name;
            row.Unit = material.Unit;
            row.CostPerUnit = material.CostPerUnit;
            row.RaiseCalculatedPropertiesChanged();
            RecalculateCosts();
        }

        private void RecalculateCosts()
        {
            TotalNetCost = BomRows.Sum(r => r.QuantityRequired * r.CostPerUnit);
            TotalWastageCost = BomRows.Sum(r => r.WastageCost);
            TotalFoodCost = BomRows.Sum(r => r.IngredientCost);
            GrossMargin = MenuPrice > 0 ? Math.Round((MenuPrice - TotalFoodCost) / MenuPrice * 100, 1) : 0;
        }
    }
}
