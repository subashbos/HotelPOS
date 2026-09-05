using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Common.Constants;
using HotelPOS.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows.Media;

namespace HotelPOS.ViewModels
{
    /// <summary>One positioned block in the reservation scheduler timeline (see
    /// <see cref="ReservationViewModel.SchedulerBlocks"/>).</summary>
    public class ReservationBlock
    {
        public required Reservation Reservation { get; init; }
        public double Left { get; init; }
        public double Top { get; init; }
        public double Width { get; init; }
        public double Height { get; init; }
        public required Brush Brush { get; init; }
        public string Label => $"{Reservation.StartTime:hh\\:mm} {Reservation.CustomerName ?? "Walk-in"}";
    }

    /// <summary>One hour-ruler tick in the scheduler header.</summary>
    public class HourMark
    {
        public required string Label { get; init; }
        public double Left { get; init; }
    }

    public partial class ReservationViewModel : ObservableObject
    {
        private readonly INotificationService _notificationService;

        /// <summary>Pixel width of one timeline hour.</summary>
        public const double PxPerHour = 72;

        /// <summary>Pixel height of one table row.</summary>
        public const double RowHeight = 56;

        private const int DefaultRangeStartMinutes = 9 * 60;
        private const int DefaultRangeEndMinutes = 23 * 60;

        private static readonly Brush ReservedBrush = CreateFrozenBrush(0xCB, 0xD5, 0xE1);
        private static readonly Brush CheckedInBrush = CreateFrozenBrush(0xBA, 0xE6, 0xFD);
        private static readonly Brush CompletedBrush = CreateFrozenBrush(0xA7, 0xF3, 0xD0);
        private static readonly Brush CancelledBrush = CreateFrozenBrush(0xFE, 0xCA, 0xCA);

        /// <summary>These brushes are shared static instances reused across every scheduler block,
        /// so they're frozen: an unfrozen <see cref="Freezable"/> is mutable and not thread-safe,
        /// which is exactly the kind of shared-mutable-state bug a static analyzer flags.</summary>
        private static Brush CreateFrozenBrush(byte r, byte g, byte b)
        {
            var brush = new SolidColorBrush(Color.FromRgb(r, g, b));
            brush.Freeze();
            return brush;
        }

        [ObservableProperty]
        private bool _isSchedulerView = true;

        [ObservableProperty]
        private Reservation? _selectedReservation;

        [ObservableProperty]
        private int _rangeStartMinutes = DefaultRangeStartMinutes;

        [ObservableProperty]
        private int _rangeEndMinutes = DefaultRangeEndMinutes;

        [ObservableProperty]
        private double _timelineWidth;

        [ObservableProperty]
        private double _timelineHeight;

        [ObservableProperty]
        private double _nowLineLeft;

        [ObservableProperty]
        private bool _showNowLine;

        [ObservableProperty]
        private string _selectedReservationSummary = "(click a reservation block above)";

        public ObservableCollection<ReservationBlock> SchedulerBlocks { get; } = new();
        public ObservableCollection<HourMark> HourMarks { get; } = new();

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
            SelectedReservation = null;
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
                RecomputeScheduler();
            }
            catch (Exception ex)
            {
                _notificationService.ShowError($"Failed to load reservations: {ex.Message}");
            }
        }

        /// <summary>Rebuilds the scheduler's time range, hour ruler and positioned blocks from
        /// the current <see cref="Tables"/> and <see cref="Reservations"/>.</summary>
        private void RecomputeScheduler()
        {
            int rangeStart = DefaultRangeStartMinutes;
            int rangeEnd = DefaultRangeEndMinutes;
            foreach (var r in Reservations)
            {
                var startMin = (int)r.StartTime.TotalMinutes;
                var endMin = (int)r.EndTime.TotalMinutes;
                if (startMin < rangeStart) rangeStart = (startMin / 60) * 60;
                if (endMin > rangeEnd) rangeEnd = ((endMin + 59) / 60) * 60;
            }
            RangeStartMinutes = rangeStart;
            RangeEndMinutes = rangeEnd;
            TimelineWidth = (rangeEnd - rangeStart) / 60.0 * PxPerHour;
            TimelineHeight = Tables.Count * RowHeight;

            HourMarks.Clear();
            for (int m = rangeStart; m <= rangeEnd; m += 60)
            {
                HourMarks.Add(new HourMark
                {
                    Label = FormatHourMarkLabel(m),
                    Left = (m - rangeStart) / 60.0 * PxPerHour
                });
            }

            SchedulerBlocks.Clear();
            for (int i = 0; i < Tables.Count; i++)
            {
                var table = Tables[i];
                foreach (var r in Reservations.Where(x => x.TableId == table.Id))
                {
                    var startMin = (int)r.StartTime.TotalMinutes;
                    var endMin = (int)r.EndTime.TotalMinutes;
                    SchedulerBlocks.Add(new ReservationBlock
                    {
                        Reservation = r,
                        Left = (startMin - rangeStart) / 60.0 * PxPerHour,
                        Top = i * RowHeight + 4,
                        Width = Math.Max((endMin - startMin) / 60.0 * PxPerHour, 24),
                        Height = RowHeight - 8,
                        Brush = BrushForStatus(r.Status)
                    });
                }
            }

            double? nowLine = SelectedDate.Date == DateTime.Today
                ? MinutesToLineLeft((int)DateTime.Now.TimeOfDay.TotalMinutes, rangeStart, rangeEnd)
                : null;
            ShowNowLine = nowLine.HasValue;
            NowLineLeft = nowLine ?? 0;
        }

        /// <summary>Formats an hour-ruler tick as "HH:mm" without TimeSpan's 24h wraparound, so a
        /// range extending to midnight reads as "24:00" instead of "00:00" - TimeSpan.FromMinutes(1440)
        /// rolls over into a 1-day, 0-hour TimeSpan, so its "hh" component (and thus ToString("hh\:mm"))
        /// prints "00:00" for that tick. Matches the Angular scheduler's formatHourMarkLabel().</summary>
        private static string FormatHourMarkLabel(int minutes) => $"{minutes / 60:00}:{minutes % 60:00}";

        private static double? MinutesToLineLeft(int minutes, int rangeStart, int rangeEnd) =>
            minutes >= rangeStart && minutes <= rangeEnd ? (minutes - rangeStart) / 60.0 * PxPerHour : null;

        private static Brush BrushForStatus(string status) => status switch
        {
            ReservationStatuses.CheckedIn => CheckedInBrush,
            ReservationStatuses.Completed => CompletedBrush,
            ReservationStatuses.Cancelled => CancelledBrush,
            ReservationStatuses.NoShow => CancelledBrush,
            _ => ReservedBrush
        };

        [RelayCommand]
        private void SelectBlock(Reservation? reservation)
        {
            SelectedReservation = reservation;
        }

        partial void OnSelectedReservationChanged(Reservation? value)
        {
            SelectedReservationSummary = value == null
                ? "(click a reservation block above)"
                : $"{value.Table?.Name} — {value.CustomerName ?? "Walk-in"} ({value.Status})";
        }

        [RelayCommand]
        private void ShowSchedulerView() => IsSchedulerView = true;

        [RelayCommand]
        private void ShowListView() => IsSchedulerView = false;

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

        /// <summary>Same as <see cref="OpenForm"/> but prefilled from a scheduler click on an
        /// empty slot instead of cleared.</summary>
        public void OpenFormAt(Table table, int startMinutes)
        {
            ArgumentNullException.ThrowIfNull(table);

            var endMinutes = Math.Min(startMinutes + 60, RangeEndMinutes);
            FormTable = table;
            FormCustomer = null;
            FormCustomerName = null;
            FormCustomerPhone = null;
            FormDate = SelectedDate;
            FormStartTimeText = TimeSpan.FromMinutes(startMinutes).ToString("hh\\:mm");
            FormEndTimeText = TimeSpan.FromMinutes(endMinutes).ToString("hh\\:mm");
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
                SelectedReservation = null;
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
                SelectedReservation = null;
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
