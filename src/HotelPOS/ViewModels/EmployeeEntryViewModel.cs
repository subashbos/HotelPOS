using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Common.Constants;
using HotelPOS.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace HotelPOS.ViewModels
{
    public partial class EmployeeEntryViewModel : ObservableObject, IEntryDialogViewModel
    {
        private readonly INotificationService _notificationService;

        public event EventHandler<bool>? RequestClose;

        ICommand IEntryDialogViewModel.SaveCommand => SaveCommand;

        [ObservableProperty]
        private int _id;

        [ObservableProperty]
        private string _employeeCode = string.Empty;

        [ObservableProperty]
        private string _firstName = string.Empty;

        [ObservableProperty]
        private string? _lastName;

        [ObservableProperty]
        private string? _gender = "Male";

        [ObservableProperty]
        private DateTime? _dateOfBirth;

        [ObservableProperty]
        private DateTime _dateOfJoining = DateTime.Today;

        [ObservableProperty]
        private int? _departmentId;

        [ObservableProperty]
        private int? _designationId;

        [ObservableProperty]
        private string _employmentType = EmploymentTypes.Permanent;

        [ObservableProperty]
        private string _status = EmployeeStatuses.Active;

        [ObservableProperty]
        private string? _phone;

        [ObservableProperty]
        private string? _email;

        [ObservableProperty]
        private string? _address;

        [ObservableProperty]
        private string? _pan;

        [ObservableProperty]
        private string? _aadhaar;

        [ObservableProperty]
        private string? _uan;

        [ObservableProperty]
        private string? _esicNumber;

        [ObservableProperty]
        private string? _bankName;

        [ObservableProperty]
        private string? _bankAccountNumber;

        [ObservableProperty]
        private string? _bankIfsc;

        [ObservableProperty]
        private string? _emergencyContactName;

        [ObservableProperty]
        private string? _emergencyContactPhone;

        [ObservableProperty]
        private bool _isEditMode;

        [ObservableProperty]
        private string _firstNameError = string.Empty;
        public bool IsFirstNameInvalid => !string.IsNullOrEmpty(FirstNameError); // NOSONAR

        [ObservableProperty]
        private string _employeeCodeError = string.Empty;
        public bool IsEmployeeCodeInvalid => !string.IsNullOrEmpty(EmployeeCodeError); // NOSONAR

        [ObservableProperty]
        private string _phoneError = string.Empty;
        public bool IsPhoneInvalid => !string.IsNullOrEmpty(PhoneError); // NOSONAR

        [ObservableProperty]
        private string _emailError = string.Empty;
        public bool IsEmailInvalid => !string.IsNullOrEmpty(EmailError); // NOSONAR

        public ObservableCollection<Department> Departments { get; } = new();
        public ObservableCollection<Designation> Designations { get; } = new();
        public string[] EmploymentTypeOptions { get; } = EmploymentTypes.All;
        public string[] StatusOptions { get; } = EmployeeStatuses.All;

        public EmployeeEntryViewModel(IEmployeeService employeeService, INotificationService notificationService)
        {
            _notificationService = notificationService;

            if (System.Windows.Application.Current == null)
            {
                App.RegisterTestService(employeeService);
                App.RegisterTestService(notificationService);
            }
        }

        public async Task InitializeAsync()
        {
            using (var scope = App.CreateDbScope())
            {
                var employeeService = scope.ServiceProvider.GetRequiredService<IEmployeeService>();
                var departments = await employeeService.GetDepartmentsAsync();
                var designations = await employeeService.GetDesignationsAsync();

                Departments.Clear();
                foreach (var d in departments) Departments.Add(d);

                Designations.Clear();
                foreach (var d in designations) Designations.Add(d);
            }
        }

        public void LoadEmployee(Employee employee) // NOSONAR
        {
            Id = employee.Id;
            EmployeeCode = employee.EmployeeCode;
            FirstName = employee.FirstName;
            LastName = employee.LastName;
            Gender = employee.Gender ?? "Male";
            DateOfBirth = employee.DateOfBirth;
            DateOfJoining = employee.DateOfJoining;
            DepartmentId = employee.DepartmentId;
            DesignationId = employee.DesignationId;
            EmploymentType = employee.EmploymentType;
            Status = employee.Status;
            Phone = employee.Phone;
            Email = employee.Email;
            Address = employee.Address;
            Pan = employee.Pan;
            Aadhaar = employee.Aadhaar;
            Uan = employee.Uan;
            EsicNumber = employee.EsicNumber;
            BankName = employee.BankName;
            BankAccountNumber = employee.BankAccountNumber;
            BankIfsc = employee.BankIfsc;
            EmergencyContactName = employee.EmergencyContactName;
            EmergencyContactPhone = employee.EmergencyContactPhone;
            IsEditMode = Id > 0;

            FirstNameError = string.Empty;
            PhoneError = string.Empty;
            EmailError = string.Empty;
        }

        partial void OnFirstNameChanged(string value) => ValidateFirstName();
        partial void OnPhoneChanged(string? value) => ValidatePhone();
        partial void OnEmailChanged(string? value) => ValidateEmail();

        public bool ValidateFirstName() // NOSONAR
        {
            var isValid = EntryValidation.ValidateRequired(FirstName, "First Name", out var error);
            FirstNameError = error;
            return isValid;
        }

        public bool ValidatePhone() // NOSONAR
        {
            var isValid = EntryValidation.ValidatePhone(Phone, out var error);
            PhoneError = error;
            return isValid;
        }

        public bool ValidateEmail() // NOSONAR
        {
            var isValid = EntryValidation.ValidateEmail(Email, out var error);
            EmailError = error;
            return isValid;
        }

        [RelayCommand]
        private async Task SaveAsync()
        {
            try
            {
                bool isFirstNameValid = ValidateFirstName();
                bool isPhoneValid = ValidatePhone();
                bool isEmailValid = ValidateEmail();

                if (!isFirstNameValid || !isPhoneValid || !isEmailValid)
                {
                    _notificationService.ShowWarning("Please correct the errors in the form before saving.");
                    return;
                }

                using (var scope = App.CreateDbScope())
                {
                    var employeeService = scope.ServiceProvider.GetRequiredService<IEmployeeService>();

                    var trimmedCode = EmployeeCode.Trim();
                    if (!string.IsNullOrEmpty(trimmedCode))
                    {
                        var isUnique = await employeeService.ValidateEmployeeCodeUniqueAsync(trimmedCode, Id);
                        if (!isUnique)
                        {
                            EmployeeCodeError = $"An employee with code '{trimmedCode}' already exists.";
                            _notificationService.ShowWarning(EmployeeCodeError);
                            return;
                        }
                    }
                    EmployeeCodeError = string.Empty;

                    var employee = new Employee
                    {
                        Id = Id,
                        EmployeeCode = EmployeeCode.Trim(),
                        FirstName = FirstName.Trim(),
                        LastName = LastName?.Trim(),
                        Gender = Gender,
                        DateOfBirth = DateOfBirth,
                        DateOfJoining = DateOfJoining,
                        DepartmentId = DepartmentId,
                        DesignationId = DesignationId,
                        EmploymentType = EmploymentType,
                        Status = Status,
                        Phone = Phone?.Trim(),
                        Email = Email?.Trim(),
                        Address = Address?.Trim(),
                        Pan = Pan?.Trim().ToUpperInvariant(),
                        Aadhaar = Aadhaar?.Trim(),
                        Uan = Uan?.Trim(),
                        EsicNumber = EsicNumber?.Trim(),
                        BankName = BankName?.Trim(),
                        BankAccountNumber = BankAccountNumber?.Trim(),
                        BankIfsc = BankIfsc?.Trim().ToUpperInvariant(),
                        EmergencyContactName = EmergencyContactName?.Trim(),
                        EmergencyContactPhone = EmergencyContactPhone?.Trim()
                    };

                    await employeeService.SaveEmployeeAsync(employee);
                    EmployeeCode = employee.EmployeeCode;
                }

                _notificationService.ShowSuccess(IsEditMode ? "Employee updated successfully." : "Employee saved successfully.");
                RequestClose?.Invoke(this, true);
            }
            catch (Exception ex)
            {
                _notificationService.ShowError($"Failed to save employee: {ex.Message}");
            }
        }

        [RelayCommand]
        private void Cancel()
        {
            RequestClose?.Invoke(this, false);
        }
    }
}
