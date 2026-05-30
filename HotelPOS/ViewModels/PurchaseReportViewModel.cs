using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HotelPOS.Application;
using HotelPOS.Application.Interfaces;
using HotelPOS.Domain;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace HotelPOS.ViewModels
{
    public partial class PurchaseReportViewModel : ObservableObject
    {
        private readonly IReportService _reportService;
        private readonly IPurchaseService _purchaseService;
        private readonly INotificationService _notificationService;

        [ObservableProperty]
        private DateTime? _filterFrom = DateTime.Today;

        [ObservableProperty]
        private DateTime? _filterTo = DateTime.Today;

        [ObservableProperty]
        private Supplier? _selectedSupplier;

        [ObservableProperty]
        private string _itemNameSearch = string.Empty;

        [ObservableProperty]
        private string _invoiceNoSearch = string.Empty;

        [ObservableProperty]
        private string _selectedPaymentType = "All";

        [ObservableProperty]
        private int _totalPurchasesCount;

        [ObservableProperty]
        private decimal _totalPurchaseAmount;

        [ObservableProperty]
        private decimal _totalTax;

        [ObservableProperty]
        private decimal _totalDiscount;

        [ObservableProperty]
        private int _totalQuantity;

        public ObservableCollection<Supplier> Suppliers { get; } = new();
        public ObservableCollection<PurchaseReportRowDto> ReportRows { get; } = new();

        public Func<int>? GetPageSizeRequested { get; set; }
        public Action<int>? SetPagerTotalCount { get; set; }

        public PurchaseReportViewModel(IReportService reportService, IPurchaseService purchaseService, INotificationService notificationService)
        {
            _reportService = reportService;
            _purchaseService = purchaseService;
            _notificationService = notificationService;
        }

        public async Task InitializeAsync()
        {
            try
            {
                var suppliers = await _purchaseService.GetSuppliersAsync();
                Suppliers.Clear();
                Suppliers.Add(new Supplier { Id = 0, Name = "All Suppliers" });
                foreach (var sup in suppliers) Suppliers.Add(sup);
                SelectedSupplier = Suppliers.First();
                
                await LoadDataAsync(1, 10);
            }
            catch (Exception ex)
            {
                _notificationService.ShowError($"Failed to initialize report: {ex.Message}");
            }
        }

        [RelayCommand]
        public async Task ApplyFilterAsync()
        {
            await LoadDataAsync(1, GetPageSizeRequested?.Invoke() ?? 10);
        }

        [RelayCommand]
        public async Task ResetFilterAsync()
        {
            FilterFrom = DateTime.Today;
            FilterTo = DateTime.Today;
            SelectedSupplier = Suppliers.FirstOrDefault();
            ItemNameSearch = string.Empty;
            InvoiceNoSearch = string.Empty;
            SelectedPaymentType = "All";
            await LoadDataAsync(1, GetPageSizeRequested?.Invoke() ?? 10);
        }

        public async Task LoadPageAsync(int page, int pageSize)
        {
            await LoadDataAsync(page, pageSize);
        }

        private async Task LoadDataAsync(int page, int pageSize)
        {
            try
            {
                var from = FilterFrom;
                var to = FilterTo?.AddDays(1);
                var supplierId = SelectedSupplier?.Id == 0 ? (int?)null : SelectedSupplier?.Id;

                var result = await _reportService.GetPagedPurchaseReportAsync(
                    page, pageSize, from, to, supplierId, ItemNameSearch, SelectedPaymentType, InvoiceNoSearch);

                ReportRows.Clear();
                foreach (var row in result.items) ReportRows.Add(row);

                TotalPurchasesCount = result.totalCount;
                TotalPurchaseAmount = result.totalPurchases;
                TotalTax = result.totalTax;
                TotalDiscount = result.totalDiscount;
                TotalQuantity = result.totalQty;

                SetPagerTotalCount?.Invoke(result.totalCount);
            }
            catch (Exception ex)
            {
                _notificationService.ShowError($"Failed to load report: {ex.Message}");
            }
        }
    }
}
