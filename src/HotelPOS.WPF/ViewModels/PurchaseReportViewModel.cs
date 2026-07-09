using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HotelPOS.Application.DTOs.Report;
using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;

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

        private int _currentPage = 1;
        private const int PageSize = 20;
        private bool _isLoading = false;
        private bool _hasMoreData = true;

        public ObservableCollection<Supplier> Suppliers { get; } = new();
        public ObservableCollection<PurchaseReportRowDto> ReportRows { get; } = new();

        public PurchaseReportViewModel(IReportService reportService, IPurchaseService purchaseService, INotificationService notificationService)
        {
            _reportService = reportService;
            _purchaseService = purchaseService;
            _notificationService = notificationService;

            if (System.Windows.Application.Current == null)
            {
                App.RegisterTestService(reportService);
                App.RegisterTestService(purchaseService);
                App.RegisterTestService(notificationService);
            }
        }

        /// <summary>
        /// Populates the Suppliers collection (including an "All Suppliers" entry), selects it, and loads the first page of the purchase report.
        /// </summary>
        /// <remarks>
        /// If an exception occurs during initialization, an error message is shown via the notification service.
        /// </remarks>
        public async Task InitializeAsync()
        {
            try
            {
                List<Supplier> suppliers;
                using (var scope = App.CreateDbScope())
                {
                    var purchaseService = scope.ServiceProvider.GetRequiredService<IPurchaseService>();
                    suppliers = await purchaseService.GetSuppliersAsync();
                }
                Suppliers.Clear();
                Suppliers.Add(new Supplier { Id = 0, Name = "All Suppliers" });
                foreach (var sup in suppliers) Suppliers.Add(sup);
                SelectedSupplier = Suppliers.First();

                await ApplyFilterAsync();
            }
            catch (Exception ex)
            {
                _notificationService.ShowError($"Failed to initialize report: {ex.Message}");
            }
        }

        public async Task ApplyFilterAsync()
        {
            _currentPage = 1;
            _hasMoreData = true;
            ReportRows.Clear();
            await LoadMoreAsync();
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
            await ApplyFilterAsync();
        }

        public async Task LoadMoreAsync()
        {
            if (_isLoading || !_hasMoreData) return;
            _isLoading = true;

            using (var scope = App.CreateDbScope())
            {
                var reportService = scope.ServiceProvider.GetRequiredService<IReportService>();
                try
                {
                    var from = FilterFrom;
                    var to = FilterTo?.AddDays(1);
                    var supplierId = SelectedSupplier?.Id == 0 ? (int?)null : SelectedSupplier?.Id;

                    var result = await reportService.GetPagedPurchaseReportAsync(new PagedPurchaseReportRequest(
                        _currentPage, PageSize, from, to, supplierId, ItemNameSearch, SelectedPaymentType, InvoiceNoSearch));

                    if (result.items.Count < PageSize)
                    {
                        _hasMoreData = false;
                    }

                    foreach (var row in result.items) ReportRows.Add(row);
                    _currentPage++;

                    TotalPurchasesCount = result.totalCount;
                    TotalPurchaseAmount = result.totalPurchases;
                    TotalTax = result.totalTax;
                    TotalDiscount = result.totalDiscount;
                    TotalQuantity = result.totalQty;
                }
                catch (Exception ex)
                {
                    _notificationService.ShowError($"Failed to load report: {ex.Message}");
                }
                finally
                {
                    _isLoading = false;
                }
            }
        }
    }
}
