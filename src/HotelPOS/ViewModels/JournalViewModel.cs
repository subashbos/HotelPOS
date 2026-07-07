using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Entities;
using HotelPOS.Views;
using System.Collections.ObjectModel;

namespace HotelPOS.ViewModels
{
    public partial class JournalViewModel : ObservableObject, IDisposable
    {
        private readonly IOrderService _orderService;
        private readonly INotificationService _notificationService;
        private CancellationTokenSource? _cancellationTokenSource;
        private int _currentPage = 1;
        private const int PageSize = 20;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private bool _hasMoreData = true;

        [ObservableProperty]
        private string _rowCountText = "";

        [ObservableProperty]
        private DateTime? _fromDate;

        [ObservableProperty]
        private DateTime? _toDate;

        [ObservableProperty]
        private int? _tableFilter;

        public ObservableCollection<JournalRow> Items { get; } = new();

        public JournalViewModel(IOrderService orderService, INotificationService notificationService)
        {
            _orderService = orderService;
            _notificationService = notificationService;
        }

        partial void OnFromDateChanged(DateTime? value) => Refresh();
        partial void OnToDateChanged(DateTime? value) => Refresh();
        partial void OnTableFilterChanged(int? value) => Refresh();

        [RelayCommand]
        public void Refresh()
        {
            _currentPage = 1;
            HasMoreData = true;
            Items.Clear();
            _ = LoadMoreAsync();
        }

        [RelayCommand]
        public async Task LoadMoreAsync()
        {
            if (IsLoading || !HasMoreData) return;

            IsLoading = true;

            // Cancel any ongoing request if multiple scrolls happen quickly
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = new CancellationTokenSource();
            var token = _cancellationTokenSource.Token;

            try
            {
                var to = ToDate?.AddDays(1);
                var request = new PagedOrdersRequest(_currentPage, PageSize, FromDate, to, TableFilter);
                
                var result = await _orderService.GetPagedOrdersAsync(request, token);
                
                if (result.Items.Count < PageSize)
                {
                    HasMoreData = false;
                }

                int startSno = (_currentPage - 1) * PageSize + 1;
                foreach (var o in result.Items)
                {
                    Items.Add(new JournalRow
                    {
                        SNo = startSno++,
                        Id = o.Id,
                        TableNumber = o.TableNumber,
                        CreatedAt = o.CreatedAt,
                        TotalAmount = o.TotalAmount,
                        DiscountAmount = o.DiscountAmount,
                        PaymentMode = o.PaymentMode,
                        Items = o.Items ?? new List<OrderItem>()
                    });
                }

                RowCountText = $"{result.TotalCount} transaction{(result.TotalCount == 1 ? "" : "s")}";
                _currentPage++;
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested
            }
            catch (Exception ex)
            {
                _notificationService.ShowError($"Failed to load data: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        public void Dispose()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }
    }
}
