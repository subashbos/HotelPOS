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
    public partial class AttendanceViewModel : ObservableObject
    {
        private readonly IAttendanceService _attendanceService;
        private readonly IEmployeeService _employeeService;
        private readonly INotificationService _notificationService;

        [ObservableProperty]
        private DateTime _selectedDate = DateTime.Today;

        [ObservableProperty]
        private Employee? _selectedEmployee;

        [ObservableProperty]
        private string _status = AttendanceStatuses.Present;

        [ObservableProperty]
        private string _checkInText = string.Empty;

        [ObservableProperty]
        private string _checkOutText = string.Empty;

        [ObservableProperty]
        private string? _remarks;

        public ObservableCollection<Employee> Employees { get; } = new();
        public ObservableCollection<Attendance> AttendanceRecords { get; } = new();
        public string[] StatusOptions { get; } = AttendanceStatuses.All;

        public AttendanceViewModel(IAttendanceService attendanceService, IEmployeeService employeeService, INotificationService notificationService)
        {
            _attendanceService = attendanceService;
            _employeeService = employeeService;
            _notificationService = notificationService;

            if (System.Windows.Application.Current == null)
            {
                App.RegisterTestService(attendanceService);
                App.RegisterTestService(employeeService);
                App.RegisterTestService(notificationService);
            }
        }

        partial void OnSelectedDateChanged(DateTime value)
        {
            _ = LoadAttendanceForDateAsync();
        }

        public async Task InitializeAsync()
        {
            using (var scope = App.CreateDbScope())
            {
                var employeeService = scope.ServiceProvider.GetRequiredService<IEmployeeService>();
                try
                {
                    var employees = await employeeService.GetEmployeesAsync();
                    Employees.Clear();
                    foreach (var e in employees.Where(e => e.Status == EmployeeStatuses.Active))
                        Employees.Add(e);
                }
                catch (Exception ex)
                {
                    _notificationService.ShowError($"Failed to load employees: {ex.Message}");
                }
            }

            await LoadAttendanceForDateAsync();
        }

        public async Task LoadAttendanceForDateAsync()
        {
            using (var scope = App.CreateDbScope())
            {
                var attendanceService = scope.ServiceProvider.GetRequiredService<IAttendanceService>();
                try
                {
                    var records = await attendanceService.GetAttendanceForDateAsync(SelectedDate);
                    AttendanceRecords.Clear();
                    foreach (var r in records) AttendanceRecords.Add(r);
                }
                catch (Exception ex)
                {
                    _notificationService.ShowError($"Failed to load attendance: {ex.Message}");
                }
            }
        }

        [RelayCommand]
        private async Task MarkAsync()
        {
            if (SelectedEmployee == null)
            {
                _notificationService.ShowWarning("Please select an employee.");
                return;
            }

            var attendance = new Attendance
            {
                EmployeeId = SelectedEmployee.Id,
                Date = SelectedDate,
                Status = Status,
                CheckInTime = TryParseTime(CheckInText),
                CheckOutTime = TryParseTime(CheckOutText),
                Remarks = Remarks
            };

            using (var scope = App.CreateDbScope())
            {
                var attendanceService = scope.ServiceProvider.GetRequiredService<IAttendanceService>();
                try
                {
                    await attendanceService.MarkAttendanceAsync(attendance);
                    _notificationService.ShowSuccess($"Attendance marked for {SelectedEmployee.FirstName}.");
                    CheckInText = string.Empty;
                    CheckOutText = string.Empty;
                    Remarks = null;
                }
                catch (Exception ex)
                {
                    _notificationService.ShowError($"Failed to mark attendance: {ex.Message}");
                    return;
                }
            }

            await LoadAttendanceForDateAsync();
        }

        [RelayCommand]
        private async Task DeleteAsync(Attendance? attendance)
        {
            if (attendance == null) return;

            using (var scope = App.CreateDbScope())
            {
                var attendanceService = scope.ServiceProvider.GetRequiredService<IAttendanceService>();
                try
                {
                    await attendanceService.DeleteAttendanceAsync(attendance.Id);
                }
                catch (Exception ex)
                {
                    _notificationService.ShowError($"Failed to delete attendance record: {ex.Message}");
                    return;
                }
            }

            await LoadAttendanceForDateAsync();
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
