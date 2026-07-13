using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;

namespace HotelPOS.ViewModels
{
    public partial class LeaveViewModel : ObservableObject
    {
        private readonly INotificationService _notificationService;

        [ObservableProperty]
        private Employee? _selectedEmployee;

        [ObservableProperty]
        private LeaveType? _selectedLeaveType;

        [ObservableProperty]
        private DateTime _fromDate = DateTime.Today;

        [ObservableProperty]
        private DateTime _toDate = DateTime.Today;

        [ObservableProperty]
        private string? _reason;

        [ObservableProperty]
        private Employee? _approverEmployee;

        [ObservableProperty]
        private string? _rejectReasonText;

        public ObservableCollection<Employee> Employees { get; } = new();
        public ObservableCollection<LeaveType> LeaveTypes { get; } = new();
        public ObservableCollection<LeaveBalance> Balances { get; } = new();
        public ObservableCollection<LeaveRequest> Requests { get; } = new();

        public LeaveViewModel(ILeaveService leaveService, IEmployeeService employeeService, INotificationService notificationService)
        {
            _notificationService = notificationService;

            if (System.Windows.Application.Current == null)
            {
                App.RegisterTestService(leaveService);
                App.RegisterTestService(employeeService);
                App.RegisterTestService(notificationService);
            }
        }

        partial void OnSelectedEmployeeChanged(Employee? value)
        {
            _ = LoadBalancesAsync();
        }

        public async Task InitializeAsync()
        {
            using (var scope = App.CreateDbScope())
            {
                var employeeService = scope.ServiceProvider.GetRequiredService<IEmployeeService>();
                var leaveService = scope.ServiceProvider.GetRequiredService<ILeaveService>();
                try
                {
                    var employees = await employeeService.GetEmployeesAsync();
                    Employees.Clear();
                    foreach (var e in employees) Employees.Add(e);

                    var leaveTypes = await leaveService.GetLeaveTypesAsync();
                    LeaveTypes.Clear();
                    foreach (var t in leaveTypes) LeaveTypes.Add(t);
                }
                catch (Exception ex)
                {
                    _notificationService.ShowError($"Failed to load leave data: {ex.Message}");
                }
            }

            await LoadRequestsAsync();
        }

        public async Task LoadRequestsAsync()
        {
            using (var scope = App.CreateDbScope())
            {
                var leaveService = scope.ServiceProvider.GetRequiredService<ILeaveService>();
                try
                {
                    var requests = await leaveService.GetRequestsAsync();
                    Requests.Clear();
                    foreach (var r in requests) Requests.Add(r);
                }
                catch (Exception ex)
                {
                    _notificationService.ShowError($"Failed to load leave requests: {ex.Message}");
                }
            }
        }

        private async Task LoadBalancesAsync()
        {
            Balances.Clear();
            if (SelectedEmployee == null) return;

            using (var scope = App.CreateDbScope())
            {
                var leaveService = scope.ServiceProvider.GetRequiredService<ILeaveService>();
                try
                {
                    var balances = await leaveService.GetBalancesAsync(SelectedEmployee.Id, DateTime.Today.Year);
                    foreach (var b in balances) Balances.Add(b);
                }
                catch (Exception ex)
                {
                    _notificationService.ShowError($"Failed to load leave balances: {ex.Message}");
                }
            }
        }

        [RelayCommand]
        private async Task ApplyAsync()
        {
            if (SelectedEmployee == null)
            {
                _notificationService.ShowWarning("Please select an employee.");
                return;
            }
            if (SelectedLeaveType == null)
            {
                _notificationService.ShowWarning("Please select a leave type.");
                return;
            }

            var request = new LeaveRequest
            {
                EmployeeId = SelectedEmployee.Id,
                LeaveTypeId = SelectedLeaveType.Id,
                FromDate = FromDate,
                ToDate = ToDate,
                Reason = Reason
            };

            using (var scope = App.CreateDbScope())
            {
                var leaveService = scope.ServiceProvider.GetRequiredService<ILeaveService>();
                try
                {
                    await leaveService.ApplyLeaveAsync(request);
                    _notificationService.ShowSuccess("Leave request submitted.");
                    Reason = null;
                }
                catch (Exception ex)
                {
                    _notificationService.ShowError($"Failed to apply leave: {ex.Message}");
                    return;
                }
            }

            await LoadRequestsAsync();
            await LoadBalancesAsync();
        }

        [RelayCommand]
        private async Task ApproveAsync(LeaveRequest? request)
        {
            if (request == null) return;
            if (ApproverEmployee == null)
            {
                _notificationService.ShowWarning("Please select who is approving this request.");
                return;
            }

            using (var scope = App.CreateDbScope())
            {
                var leaveService = scope.ServiceProvider.GetRequiredService<ILeaveService>();
                try
                {
                    await leaveService.ApproveLeaveAsync(request.Id, ApproverEmployee.Id);
                    _notificationService.ShowSuccess("Leave request approved.");
                }
                catch (Exception ex)
                {
                    _notificationService.ShowError($"Failed to approve leave: {ex.Message}");
                    return;
                }
            }

            await LoadRequestsAsync();
            await LoadBalancesAsync();
        }

        [RelayCommand]
        private async Task RejectAsync(LeaveRequest? request)
        {
            if (request == null) return;
            if (ApproverEmployee == null)
            {
                _notificationService.ShowWarning("Please select who is rejecting this request.");
                return;
            }

            using (var scope = App.CreateDbScope())
            {
                var leaveService = scope.ServiceProvider.GetRequiredService<ILeaveService>();
                try
                {
                    await leaveService.RejectLeaveAsync(request.Id, ApproverEmployee.Id, RejectReasonText ?? string.Empty);
                    _notificationService.ShowSuccess("Leave request rejected.");
                    RejectReasonText = null;
                }
                catch (Exception ex)
                {
                    _notificationService.ShowError($"Failed to reject leave: {ex.Message}");
                    return;
                }
            }

            await LoadRequestsAsync();
        }
    }
}
