using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Entities;
using System.Collections.ObjectModel;

namespace HotelPOS.ViewModels
{
    public partial class RawMaterialViewModel : ObservableObject
    {
        private readonly IBomService _bomService;
        private readonly IDialogService _dialogService;

        public RawMaterialViewModel(IBomService bomService, IDialogService dialogService)
        {
            _bomService = bomService;
            _dialogService = dialogService;
        }

        [ObservableProperty] private ObservableCollection<RawMaterial> _rawMaterials = new();
        [ObservableProperty] private RawMaterial? _selectedRawMaterial;

        // Form fields
        [ObservableProperty] private string _name = string.Empty;
        [ObservableProperty] private string _unit = "kg";
        [ObservableProperty] private decimal _costPerUnit;
        [ObservableProperty] private decimal _currentStock;
        [ObservableProperty] private decimal _minStockThreshold;

        [ObservableProperty] private bool _isLoading;
        [ObservableProperty] private string _searchText = string.Empty;

        public ObservableCollection<string> Units { get; } = new()
        {
            "kg", "g", "litre", "ml", "pcs", "dozen", "packet", "box"
        };

        partial void OnSearchTextChanged(string value) => _ = LoadAsync();

        public async Task LoadAsync()
        {
            IsLoading = true;
            try
            {
                var all = await _bomService.GetAllRawMaterialsAsync();
                var filtered = string.IsNullOrWhiteSpace(SearchText)
                    ? all
                    : all.Where(r => r.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase)).ToList();

                RawMaterials = new ObservableCollection<RawMaterial>(filtered);
            }
            finally { IsLoading = false; }
        }

        [RelayCommand]
        private void New()
        {
            SelectedRawMaterial = null;
            ClearForm();
        }

        partial void OnSelectedRawMaterialChanged(RawMaterial? value)
        {
            if (value == null) return;
            Name = value.Name;
            Unit = value.Unit;
            CostPerUnit = value.CostPerUnit;
            CurrentStock = value.CurrentStock;
            MinStockThreshold = value.MinStockThreshold;
        }

        [RelayCommand]
        private async Task SaveAsync()
        {
            if (string.IsNullOrWhiteSpace(Name))
            {
                await _dialogService.ShowMessageAsync("Name is required.", "Validation", DialogButton.OK, DialogIcon.Warning);
                return;
            }

            if (CostPerUnit < 0 || CurrentStock < 0 || MinStockThreshold < 0)
            {
                await _dialogService.ShowMessageAsync("Cost, stock, and threshold values cannot be negative.", "Validation", DialogButton.OK, DialogIcon.Warning);
                return;
            }

            var entity = SelectedRawMaterial ?? new RawMaterial();
            entity.Name = Name.Trim();
            entity.Unit = Unit;
            entity.CostPerUnit = CostPerUnit;
            entity.CurrentStock = CurrentStock;
            entity.MinStockThreshold = MinStockThreshold;

            try
            {
                await _bomService.SaveRawMaterialAsync(entity);
            }
            catch (InvalidOperationException ex)
            {
                await _dialogService.ShowMessageAsync(ex.Message, "Save Failed", DialogButton.OK, DialogIcon.Warning);
                return;
            }

            ClearForm();
            await LoadAsync();
        }

        [RelayCommand]
        private async Task DeleteAsync()
        {
            if (SelectedRawMaterial == null) return;

            var result = await _dialogService.ShowMessageAsync(
                $"Delete raw material '{SelectedRawMaterial.Name}'?",
                "Confirm Delete",
                DialogButton.YesNo, DialogIcon.Warning);

            if (result != DialogResult.Yes) return;

            try
            {
                await _bomService.DeleteRawMaterialAsync(SelectedRawMaterial.Id);
                ClearForm();
                await LoadAsync();
            }
            catch (InvalidOperationException ex)
            {
                await _dialogService.ShowMessageAsync(ex.Message, "Cannot Delete", DialogButton.OK, DialogIcon.Warning);
            }
        }

        private void ClearForm() // NOSONAR
        {
            Name = string.Empty;
            Unit = "kg";
            CostPerUnit = 0;
            CurrentStock = 0;
            MinStockThreshold = 0;
            SelectedRawMaterial = null;
        }
    }
}
