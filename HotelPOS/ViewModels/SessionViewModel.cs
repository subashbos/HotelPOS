using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HotelPOS.Application.Interfaces;
using HotelPOS.Domain;
using System.Collections.ObjectModel;

namespace HotelPOS.ViewModels
{
    public partial class SessionViewModel : ObservableObject
    {
        private readonly ICashService _cashService;
        private readonly INotificationService _notificationService;

        [ObservableProperty]
        private decimal _openingBalance;

        [ObservableProperty]
        private decimal _actualCash;

        [ObservableProperty]
        private string? _notes;

        [ObservableProperty]
        private CashSession? _currentSession;

        [ObservableProperty]
        private bool _isSessionOpen;

        [ObservableProperty]
        private decimal _expectedCash;

        public ObservableCollection<CashSession> SessionHistory { get; } = new();

        public SessionViewModel(ICashService cashService, INotificationService notificationService)
        {
            _cashService = cashService;
            _notificationService = notificationService;
        }

        public async Task InitializeAsync()
        {
            await RefreshStatusAsync();
            await LoadHistoryAsync();
        }

        public async Task RefreshStatusAsync()
        {
            await App.DbLock.WaitAsync();
            try
            {
                CurrentSession = await _cashService.GetCurrentSessionAsync();
                IsSessionOpen = CurrentSession != null;
                if (IsSessionOpen)
                {
                    var sales = await _cashService.GetTotalSalesForCurrentSessionAsync();
                    ExpectedCash = CurrentSession!.OpeningBalance + sales;
                }
            }
            finally
            {
                App.DbLock.Release();
            }
        }

        public async Task LoadHistoryAsync()
        {
            List<CashSession> history;
            await App.DbLock.WaitAsync();
            try
            {
                history = await _cashService.GetSessionHistoryAsync();
            }
            finally
            {
                App.DbLock.Release();
            }
            SessionHistory.Clear();
            for (int i = 0; i < history.Count; i++)
            {
                history[i].SNo = i + 1;
                SessionHistory.Add(history[i]);
            }
        }

        [RelayCommand]
        private async Task OpenSessionAsync()
        {
            try
            {
                if (OpeningBalance < 0)
                {
                    _notificationService.ShowError("Opening balance cannot be negative");
                    return;
                }

                await App.DbLock.WaitAsync();
                try
                {
                    await _cashService.OpenSessionAsync(OpeningBalance, AppSession.CurrentUser?.Username ?? "System");
                }
                finally
                {
                    App.DbLock.Release();
                }
                await RefreshStatusAsync();
                _notificationService.ShowSuccess("Shift started successfully");
            }
            catch (Exception ex)
            {
                _notificationService.ShowError($"Failed to open session: {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task CloseSessionAsync()
        {
            try
            {
                await App.DbLock.WaitAsync();
                try
                {
                    await _cashService.CloseSessionAsync(ActualCash, Notes, AppSession.CurrentUser?.Username ?? "System");
                }
                finally
                {
                    App.DbLock.Release();
                }
                await RefreshStatusAsync();
                await LoadHistoryAsync();
                _notificationService.ShowSuccess("Shift closed successfully");
            }
            catch (Exception ex)
            {
                _notificationService.ShowError($"Failed to close session: {ex.Message}");
            }
        }
    }
}
