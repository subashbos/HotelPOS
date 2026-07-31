using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HotelPOS.Application.Common.Models;
using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Entities;
using System.Collections.ObjectModel;

namespace HotelPOS.ViewModels
{
    public partial class TdsViewModel : ObservableObject
    {
        private readonly ITdsService _tdsService;
        private readonly INotificationService _notificationService;

        [ObservableProperty]
        private int _selectedYear = DateTime.Today.Year;

        [ObservableProperty]
        private decimal _standardDeduction = 75000m;

        [ObservableProperty]
        private decimal _rebateIncomeLimit = 700000m;

        [ObservableProperty]
        private decimal _cessRatePercent = 4m;

        [ObservableProperty]
        private TdsSlab? _selectedSlab;

        public ObservableCollection<int> FinancialYears { get; } = new();
        public ObservableCollection<TdsSlab> Slabs { get; } = new();

        public TdsViewModel(ITdsService tdsService, INotificationService notificationService)
        {
            _tdsService = tdsService;
            _notificationService = notificationService;
        }

        public async Task InitializeAsync()
        {
            await LoadFinancialYearsAsync();
        }

        [RelayCommand]
        public async Task LoadFinancialYearsAsync()
        {
            try
            {
                var years = await _tdsService.GetFinancialYearsAsync();
                FinancialYears.Clear();
                if (years.Count == 0)
                {
                    years.Add(DateTime.Today.Year);
                }
                foreach (var y in years)
                {
                    FinancialYears.Add(y);
                }
                if (!FinancialYears.Contains(SelectedYear))
                {
                    SelectedYear = FinancialYears[0];
                }
                await LoadRuleSetAsync();
            }
            catch (Exception ex)
            {
                _notificationService.ShowError($"Failed to load TDS financial years: {ex.Message}");
            }
        }

        [RelayCommand]
        public async Task LoadRuleSetAsync()
        {
            try
            {
                Slabs.Clear();
                var ruleSet = await _tdsService.GetRuleSetAsync(SelectedYear);
                if (ruleSet != null)
                {
                    StandardDeduction = ruleSet.Config.StandardDeduction;
                    RebateIncomeLimit = ruleSet.Config.RebateIncomeLimit;
                    CessRatePercent = ruleSet.Config.CessRatePercent;

                    foreach (var slab in ruleSet.Slabs.OrderBy(s => s.DisplayOrder))
                    {
                        Slabs.Add(slab);
                    }
                }
                else
                {
                    // Defaults for a new year
                    StandardDeduction = 75000m;
                    RebateIncomeLimit = 700000m;
                    CessRatePercent = 4m;

                    // Add default starter slabs
                    Slabs.Add(new TdsSlab { FinancialYearStart = SelectedYear, IncomeFrom = 0m, IncomeTo = 400000m, RatePercent = 0, DisplayOrder = 1 });
                    Slabs.Add(new TdsSlab { FinancialYearStart = SelectedYear, IncomeFrom = 400000m, IncomeTo = 800000m, RatePercent = 5, DisplayOrder = 2 });
                    Slabs.Add(new TdsSlab { FinancialYearStart = SelectedYear, IncomeFrom = 800000m, IncomeTo = 1200000m, RatePercent = 10, DisplayOrder = 3 });
                    Slabs.Add(new TdsSlab { FinancialYearStart = SelectedYear, IncomeFrom = 1200000m, IncomeTo = null, RatePercent = 15, DisplayOrder = 4 });
                }
            }
            catch (Exception ex)
            {
                _notificationService.ShowError($"Failed to load TDS rule set: {ex.Message}");
            }
        }

        [RelayCommand]
        public void AddSlab()
        {
            var nextOrder = Slabs.Count > 0 ? Slabs.Max(s => s.DisplayOrder) + 1 : 1;
            decimal lastTo = Slabs.Count > 0 ? (Slabs.Last().IncomeTo ?? Slabs.Last().IncomeFrom + 400000m) : 0m;

            if (Slabs.Count > 0 && !Slabs.Last().IncomeTo.HasValue)
            {
                Slabs.Last().IncomeTo = lastTo;
            }

            var newSlab = new TdsSlab
            {
                FinancialYearStart = SelectedYear,
                IncomeFrom = lastTo,
                IncomeTo = null,
                RatePercent = 20,
                DisplayOrder = nextOrder
            };
            Slabs.Add(newSlab);
        }

        [RelayCommand]
        public void RemoveSlab()
        {
            if (SelectedSlab != null)
            {
                Slabs.Remove(SelectedSlab);
                ReorderSlabs();
            }
        }

        [RelayCommand]
        public async Task SaveAsync()
        {
            try
            {
                var ruleSet = new TdsRuleSet
                {
                    Config = new TdsConfig
                    {
                        FinancialYearStart = SelectedYear,
                        StandardDeduction = StandardDeduction,
                        RebateIncomeLimit = RebateIncomeLimit,
                        CessRatePercent = CessRatePercent
                    },
                    Slabs = Slabs.ToList()
                };

                await _tdsService.SaveRuleSetAsync(ruleSet);
                _notificationService.ShowSuccess($"TDS rule set for FY {SelectedYear}-{SelectedYear + 1} saved successfully.");
                await LoadFinancialYearsAsync();
            }
            catch (Exception ex)
            {
                _notificationService.ShowError($"Validation/Save Error: {ex.Message}");
            }
        }

        [RelayCommand]
        public async Task DeleteAsync()
        {
            try
            {
                await _tdsService.DeleteRuleSetAsync(SelectedYear);
                _notificationService.ShowSuccess($"TDS rule set for FY {SelectedYear}-{SelectedYear + 1} deleted.");
                await LoadFinancialYearsAsync();
            }
            catch (Exception ex)
            {
                _notificationService.ShowError($"Failed to delete TDS rule set: {ex.Message}");
            }
        }

        private void ReorderSlabs()
        {
            int order = 1;
            foreach (var slab in Slabs)
            {
                slab.DisplayOrder = order++;
            }
        }
    }
}
