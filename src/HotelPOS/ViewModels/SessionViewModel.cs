using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
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

        /// <summary>
        /// Updates the view-model's current cash session state by retrieving the active session from the cash service and, if one exists, computing the expected cash.
        /// </summary>
        /// <remarks>
        /// Sets <see cref="CurrentSession"/>, <see cref="IsSessionOpen"/>, and when a session is open computes <see cref="ExpectedCash"/> as the session's OpeningBalance plus total sales.
        /// </remarks>
        public async Task RefreshStatusAsync()
        {
            using (var scope = App.CreateDbScope())
            {
                var cashService = scope.ServiceProvider.GetService<ICashService>() ?? _cashService;
                CurrentSession = await cashService.GetCurrentSessionAsync();
                IsSessionOpen = CurrentSession != null;
                if (IsSessionOpen)
                {
                    var sales = await cashService.GetTotalSalesForCurrentSessionAsync();
                    ExpectedCash = CurrentSession!.OpeningBalance + sales;
                }
            }
        }

        /// <summary>
        /// Loads past cash sessions from the data service into the SessionHistory collection and assigns sequential SNo values.
        /// </summary>
        /// <returns>A task that completes once the SessionHistory collection has been populated.</returns>
        public async Task LoadHistoryAsync()
        {
            List<CashSession> history;
            using (var scope = App.CreateDbScope())
            {
                var cashService = scope.ServiceProvider.GetService<ICashService>() ?? _cashService;
                history = await cashService.GetSessionHistoryAsync();
            }
            SessionHistory.Clear();
            for (int i = 0; i < history.Count; i++)
            {
                history[i].SNo = i + 1;
                SessionHistory.Add(history[i]);
            }
        }

        /// <summary>
        /// Starts a new cash session using the current OpeningBalance, refreshes view-model state, and shows a success or error notification.
        /// </summary>
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

                using (var scope = App.CreateDbScope())
                {
                    var cashService = scope.ServiceProvider.GetService<ICashService>() ?? _cashService;
                    await cashService.OpenSessionAsync(OpeningBalance, AppSession.CurrentUser?.Username ?? "System");
                }
                await RefreshStatusAsync();
                _notificationService.ShowSuccess("Shift started successfully");
            }
            catch (Exception ex)
            {
                _notificationService.ShowError($"Failed to open session: {ex.Message}");
            }
        }

        /// <summary>
        /// Closes the current cash session using the view-model's ActualCash and Notes, refreshes the session status and history, and shows a success or error notification.
        /// </summary>
        /// <remarks>
        /// The close operation is submitted with the current user's username from AppSession.CurrentUser?.Username, or "System" if no user is available.
        /// </remarks>
        [RelayCommand]
        private async Task CloseSessionAsync()
        {
            try
            {
                using (var scope = App.CreateDbScope())
                {
                    var cashService = scope.ServiceProvider.GetService<ICashService>() ?? _cashService;
                    await cashService.CloseSessionAsync(ActualCash, Notes, AppSession.CurrentUser?.Username ?? "System");
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
