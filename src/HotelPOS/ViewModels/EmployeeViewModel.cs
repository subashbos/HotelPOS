using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;

namespace HotelPOS.ViewModels
{
    public partial class EmployeeViewModel : ObservableObject
    {
        private readonly IEmployeeService _employeeService;
        private readonly INotificationService _notificationService;
        private readonly List<Employee> _allEmployees = new();

        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    ApplyFilter();
                }
            }
        }

        private Employee? _selectedEmployee;
        public Employee? SelectedEmployee
        {
            get => _selectedEmployee;
            set => SetProperty(ref _selectedEmployee, value);
        }

        public ObservableCollection<Employee> Employees { get; } = new();

        public Func<Employee?, Task<bool>>? ShowEntryDialogAsync { get; set; }
        public Func<string, Task<bool>>? ConfirmDeleteAsync { get; set; }

        public EmployeeViewModel(IEmployeeService employeeService, INotificationService notificationService)
        {
            _employeeService = employeeService;
            _notificationService = notificationService;

            if (System.Windows.Application.Current == null)
            {
                App.RegisterTestService(employeeService);
                App.RegisterTestService(notificationService);
            }
        }

        public async Task LoadEmployeesAsync()
        {
            using (var scope = App.CreateDbScope())
            {
                var employeeService = scope.ServiceProvider.GetRequiredService<IEmployeeService>();
                try
                {
                    var employees = await employeeService.GetEmployeesAsync();
                    _allEmployees.Clear();
                    _allEmployees.AddRange(employees);
                    ApplyFilter();
                }
                catch (Exception ex)
                {
                    _notificationService.ShowError($"Failed to load employees: {ex.Message}");
                }
            }
        }

        private void ApplyFilter()
        {
            Employees.Clear();
            var search = SearchText?.Trim();

            var filtered = _allEmployees.Where(e =>
                string.IsNullOrEmpty(search) ||
                e.EmployeeCode.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                e.FirstName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                (e.LastName != null && e.LastName.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                (e.Phone != null && e.Phone.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                (e.Department != null && e.Department.Name.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                (e.Designation != null && e.Designation.Title.Contains(search, StringComparison.OrdinalIgnoreCase))
            );

            foreach (var emp in filtered)
            {
                Employees.Add(emp);
            }
        }

        [RelayCommand]
        private async Task AddEmployeeAsync()
        {
            if (ShowEntryDialogAsync != null)
            {
                var success = await ShowEntryDialogAsync(null);
                if (success)
                {
                    await LoadEmployeesAsync();
                }
            }
        }

        [RelayCommand]
        private async Task EditEmployeeAsync(Employee? employee)
        {
            var target = employee ?? SelectedEmployee;
            if (target == null)
            {
                _notificationService.ShowWarning("Please select an employee to edit.");
                return;
            }

            if (ShowEntryDialogAsync != null)
            {
                var success = await ShowEntryDialogAsync(target);
                if (success)
                {
                    await LoadEmployeesAsync();
                }
            }
        }

        [RelayCommand]
        private async Task DeleteEmployeeAsync(Employee? employee)
        {
            var target = employee ?? SelectedEmployee;
            if (target == null)
            {
                _notificationService.ShowWarning("Please select an employee to delete.");
                return;
            }

            if (ConfirmDeleteAsync != null)
            {
                var confirmed = await ConfirmDeleteAsync($"{target.FirstName} {target.LastName}".Trim());
                if (confirmed)
                {
                    using (var scope = App.CreateDbScope())
                    {
                        var employeeService = scope.ServiceProvider.GetRequiredService<IEmployeeService>();
                        try
                        {
                            await employeeService.DeleteEmployeeAsync(target.Id);
                            _notificationService.ShowSuccess($"Employee '{target.FirstName}' deleted successfully.");
                        }
                        catch (Exception ex)
                        {
                            _notificationService.ShowError($"Failed to delete employee: {ex.Message}");
                        }
                    }
                    await LoadEmployeesAsync();
                }
            }
        }
    }
}
