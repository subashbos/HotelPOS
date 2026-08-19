using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Entities;
using System.Collections.ObjectModel;

namespace HotelPOS.ViewModels
{
    public partial class DepartmentDesignationViewModel : ObservableObject
    {
        private readonly IEmployeeService _employeeService;
        private readonly INotificationService _notificationService;

        // Department form
        [ObservableProperty]
        private Department? _selectedDepartment;

        [ObservableProperty]
        private string _deptName = string.Empty;

        [ObservableProperty]
        private string _deptDescription = string.Empty;

        // Designation form
        [ObservableProperty]
        private Designation? _selectedDesignation;

        [ObservableProperty]
        private string _desigTitle = string.Empty;

        [ObservableProperty]
        private Department? _desigDepartment;

        public ObservableCollection<Department> Departments { get; } = new();
        public ObservableCollection<Designation> Designations { get; } = new();

        public DepartmentDesignationViewModel(IEmployeeService employeeService, INotificationService notificationService)
        {
            _employeeService = employeeService;
            _notificationService = notificationService;
        }

        public async Task InitializeAsync()
        {
            await LoadDataAsync();
        }

        [RelayCommand]
        public async Task LoadDataAsync()
        {
            try
            {
                var depts = await _employeeService.GetDepartmentsAsync();
                Departments.Clear();
                foreach (var d in depts)
                {
                    Departments.Add(d);
                }

                var desigs = await _employeeService.GetDesignationsAsync();
                Designations.Clear();
                foreach (var ds in desigs)
                {
                    Designations.Add(ds);
                }
            }
            catch (Exception ex)
            {
                _notificationService.ShowError($"Failed to load departments and designations: {ex.Message}");
            }
        }

        // Department CRUD
        [RelayCommand]
        public void NewDepartment()
        {
            SelectedDepartment = null;
            DeptName = string.Empty;
            DeptDescription = string.Empty;
        }

        [RelayCommand]
        public async Task SaveDepartmentAsync()
        {
            try
            {
                var dept = SelectedDepartment ?? new Department();
                dept.Name = DeptName;
                dept.Description = DeptDescription;

                await _employeeService.SaveDepartmentAsync(dept);
                _notificationService.ShowSuccess($"Department '{dept.Name}' saved successfully.");
                NewDepartment();
                await LoadDataAsync();
            }
            catch (Exception ex)
            {
                _notificationService.ShowError($"Save Error: {ex.Message}");
            }
        }

        [RelayCommand]
        public async Task DeleteDepartmentAsync()
        {
            if (SelectedDepartment == null) return;

            try
            {
                await _employeeService.DeleteDepartmentAsync(SelectedDepartment.Id);
                _notificationService.ShowSuccess($"Department deleted.");
                NewDepartment();
                await LoadDataAsync();
            }
            catch (Exception ex)
            {
                _notificationService.ShowError($"Delete Error: {ex.Message}");
            }
        }

        partial void OnSelectedDepartmentChanged(Department? value)
        {
            if (value != null)
            {
                DeptName = value.Name;
                DeptDescription = value.Description ?? string.Empty;
            }
        }

        // Designation CRUD
        [RelayCommand]
        public void NewDesignation()
        {
            SelectedDesignation = null;
            DesigTitle = string.Empty;
            DesigDepartment = null;
        }

        [RelayCommand]
        public async Task SaveDesignationAsync()
        {
            if (DesigDepartment == null)
            {
                _notificationService.ShowError("Please select a department for the designation.");
                return;
            }

            try
            {
                var desig = SelectedDesignation ?? new Designation();
                desig.Title = DesigTitle;
                desig.DepartmentId = DesigDepartment.Id;

                await _employeeService.SaveDesignationAsync(desig);
                _notificationService.ShowSuccess($"Designation '{desig.Title}' saved successfully.");
                NewDesignation();
                await LoadDataAsync();
            }
            catch (Exception ex)
            {
                _notificationService.ShowError($"Save Error: {ex.Message}");
            }
        }

        [RelayCommand]
        public async Task DeleteDesignationAsync()
        {
            if (SelectedDesignation == null) return;

            try
            {
                await _employeeService.DeleteDesignationAsync(SelectedDesignation.Id);
                _notificationService.ShowSuccess($"Designation deleted.");
                NewDesignation();
                await LoadDataAsync();
            }
            catch (Exception ex)
            {
                _notificationService.ShowError($"Delete Error: {ex.Message}");
            }
        }

        partial void OnSelectedDesignationChanged(Designation? value)
        {
            if (value != null)
            {
                DesigTitle = value.Title;
                DesigDepartment = Departments.FirstOrDefault(d => d.Id == value.DepartmentId);
            }
        }
    }
}
