using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Common.Constants;
using HotelPOS.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;
using System.Globalization;

namespace HotelPOS.ViewModels
{
    public partial class ReservationViewModel : ObservableObject
    {
        private readonly INotificationService _notificationService;

        [ObservableProperty]
        private DateTime _selectedDate = DateTime.Today;

        [ObservableProperty]
        private Table? _formTable;

        [ObservableProperty]
        private Customer? _formCustomer;

        [ObservableProperty]
        private string? _formCustomerName;

        [ObservableProperty]
        private string? _formCustomerPhone;

        [ObservableProperty]
        private DateTime _formDate = DateTime.Today;

        [ObservableProperty]
        private string _formStartTimeText = string.Empty;

        [ObservableProperty]
        private string _formEndTimeText = string.Empty;

        [ObservableProperty]
        private int _formPartySize = 2;

        [ObservableProperty]
        private string? _formNotes;

        [ObservableProperty]
        private bool _isBusy;

        public ObservableCollection<Table> Tables { get; } = new();
        public ObservableCollection<Customer> Customers { get; } = new();
        public ObservableCollection<Reservation> Reservations { get; } = new();

        public Task InitializationTask { get; }

        public ReservationViewModel(
            IReservationService reservationService,
            ITableService tableService,
            ICustomerService customerService,
            INotificationService notificationService)
        {
            _notificationService = notificationService;

            if (System.Windows.Application.Current == null)
            {
                App.RegisterTestService(reservationService);
                App.RegisterTestService(tableService);
                App.RegisterTestService(customerService);
                App.RegisterTestService(notificationService);
            }

            InitializationTask = LoadDataAsync();
        }

        public async Task LoadDataAsync()
        {
            using (var scope = App.CreateDbScope())
            {
                var reservationService = scope.ServiceProvider.GetRequiredService<IReservationService>();
                var tableService = scope.ServiceProvider.GetRequiredService<ITableService>();
                var customerService = scope.ServiceProvider.GetRequiredService<ICustomerService>();
                try
                {
                    Tables.Clear();
                    foreach (var t in await tableService.GetTablesAsync())
                    {
                        if (t.IsActive && !t.IsDeleted) Tables.Add(t);
                    }

                    Customers.Clear();
                    foreach (var c in await customerService.GetCustomersAsync())
                    {
                        Customers.Add(c);
                    }

                    await RefreshReservationsAsync(reservationService);
                }
                catch (Exception ex)
                {
                    _notificationService.ShowError($"Failed to load reservation data: {ex.Message}");
                }
            }
        }

        partial void OnSelectedDateChanged(DateTime value)
        {
            _ = RefreshReservationsAsync();
        }

        [RelayCommand]
        private async Task RefreshReservationsAsync()
        {
            using (var scope = App.CreateDbScope())
            {
                var reservationService = scope.ServiceProvider.GetRequiredService<IReservationService>();
                await RefreshReservationsAsync(reservationService);
            }
        }

        private async Task RefreshReservationsAsync(IReservationService reservationService)
        {
            try
            {
                var reservations = await reservationService.GetReservationsAsync(SelectedDate);
                Reservations.Clear();
                foreach (var r in reservations)
                {
                    Reservations.Add(r);
                }
            }
            catch (Exception ex)
            {
                _notificationService.ShowError($"Failed to load reservations: {ex.Message}");
            }
        }

        [RelayCommand]
        private void OpenForm()
        {
            FormTable = null;
            FormCustomer = null;
            FormCustomerName = null;
            FormCustomerPhone = null;
            FormDate = SelectedDate;
            FormStartTimeText = string.Empty;
            FormEndTimeText = string.Empty;
            FormPartySize = 2;
            FormNotes = null;
        }

        [RelayCommand]
        private async Task SaveReservationAsync()
        {
            try
            {
                if (FormTable == null)
                {
                    _notificationService.ShowWarning("Please select a table.");
                    return;
                }

                if (FormPartySize <= 0)
                {
                    _notificationService.ShowWarning("Party size must be at least 1.");
                    return;
                }

                var start = TryParseTime(FormStartTimeText);
                var end = TryParseTime(FormEndTimeText);
                if (start == null || end == null)
                {
                    _notificationService.ShowWarning("Enter both start and end time as HH:mm (e.g. 19:30).");
                    return;
                }

                if (end <= start)
                {
                    _notificationService.ShowWarning("End time must be after start time.");
                    return;
                }

                var reservation = new Reservation
                {
                    TableId = FormTable.Id,
                    CustomerId = FormCustomer?.Id,
                    CustomerName = FormCustomer?.Name ?? FormCustomerName,
                    CustomerPhone = FormCustomer?.Phone ?? FormCustomerPhone,
                    ReservationDate = FormDate.Date,
                    StartTime = start.Value,
                    EndTime = end.Value,
                    PartySize = FormPartySize,
                    Notes = FormNotes?.Trim()
                };

                using (var scope = App.CreateDbScope())
                {
                    var reservationService = scope.ServiceProvider.GetRequiredService<IReservationService>();
                    await reservationService.SaveReservationAsync(reservation);
                    await RefreshReservationsAsync(reservationService);
                }
                _notificationService.ShowSuccess("Reservation booked successfully.");

                OpenForm();
            }
            catch (Exception ex)
            {
                _notificationService.ShowError($"Failed to save reservation: {ex.Message}");
            }
        }

        [RelayCommand]
        private Task MarkCheckedInAsync(Reservation? reservation) => ChangeStatusAsync(reservation, ReservationStatuses.CheckedIn);

        [RelayCommand]
        private Task MarkCompletedAsync(Reservation? reservation) => ChangeStatusAsync(reservation, ReservationStatuses.Completed);

        [RelayCommand]
        private Task MarkCancelledAsync(Reservation? reservation) => ChangeStatusAsync(reservation, ReservationStatuses.Cancelled);

        [RelayCommand]
        private Task MarkNoShowAsync(Reservation? reservation) => ChangeStatusAsync(reservation, ReservationStatuses.NoShow);

        private async Task ChangeStatusAsync(Reservation? reservation, string newStatus)
        {
            if (reservation == null || IsBusy) return;

            IsBusy = true;
            try
            {
                using (var scope = App.CreateDbScope())
                {
                    var reservationService = scope.ServiceProvider.GetRequiredService<IReservationService>();
                    await reservationService.ChangeStatusAsync(reservation.Id, newStatus);
                    await RefreshReservationsAsync(reservationService);
                }
                _notificationService.ShowSuccess($"Reservation marked as {newStatus}.");
            }
            catch (Exception ex)
            {
                _notificationService.ShowError($"Failed to update reservation status: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task DeleteAsync(Reservation? reservation)
        {
            if (reservation == null || IsBusy) return;

            IsBusy = true;
            try
            {
                using (var scope = App.CreateDbScope())
                {
                    var reservationService = scope.ServiceProvider.GetRequiredService<IReservationService>();
                    await reservationService.DeleteReservationAsync(reservation.Id);
                    await RefreshReservationsAsync(reservationService);
                }
            }
            catch (Exception ex)
            {
                _notificationService.ShowError($"Failed to delete reservation: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private static readonly string[] TimeFormats = { "hh\\:mm", "h\\:mm" };

        private static TimeSpan? TryParseTime(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            return TimeSpan.TryParseExact(text.Trim(), TimeFormats, CultureInfo.InvariantCulture, out var result)
                ? result
                : null;
        }
    }
}
