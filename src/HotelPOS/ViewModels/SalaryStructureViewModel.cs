using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Entities;
using System.Collections.ObjectModel;

namespace HotelPOS.ViewModels
{
    public partial class SalaryStructureViewModel : ObservableObject
    {
        private readonly IPayrollService _payrollService;
        private readonly IEmployeeService _employeeService;
        private readonly INotificationService _notificationService;

        [ObservableProperty]
        private Employee? _selectedEmployee;

        [ObservableProperty]
        private SalaryStructure? _selectedStructure;

        [ObservableProperty]
        private decimal _basic;

        [ObservableProperty]
        private decimal _hra;

        [ObservableProperty]
        private decimal _da;

        [ObservableProperty]
        private decimal _conveyanceAllowance;

        [ObservableProperty]
        private decimal _medicalAllowance;

        [ObservableProperty]
        private decimal _specialAllowance;

        [ObservableProperty]
        private bool _pfApplicable = true;

        [ObservableProperty]
        private bool _esiApplicable;

        [ObservableProperty]
        private bool _professionalTaxApplicable = true;

        [ObservableProperty]
        private DateTime _effectiveFrom = DateTime.Today;

        public decimal GrossMonthly => Basic + Hra + Da + ConveyanceAllowance + MedicalAllowance + SpecialAllowance;

        public ObservableCollection<Employee> Employees { get; } = new();
        public ObservableCollection<SalaryStructure> Structures { get; } = new();

        public SalaryStructureViewModel(IPayrollService payrollService, IEmployeeService employeeService, INotificationService notificationService)
        {
            _payrollService = payrollService;
            _employeeService = employeeService;
            _notificationService = notificationService;
        }

        public async Task InitializeAsync()
        {
            await LoadEmployeesAsync();
        }

        [RelayCommand]
        public async Task LoadEmployeesAsync()
        {
            try
            {
                var employees = await _employeeService.GetEmployeesAsync();
                Employees.Clear();
                foreach (var emp in employees)
                {
                    Employees.Add(emp);
                }
                if (Employees.Count > 0 && SelectedEmployee == null)
                {
                    SelectedEmployee = Employees[0];
                }
            }
            catch (Exception ex)
            {
                _notificationService.ShowError($"Failed to load employees: {ex.Message}");
            }
        }

        [RelayCommand]
        public async Task LoadStructuresAsync()
        {
            if (SelectedEmployee == null) return;

            try
            {
                var structures = await _payrollService.GetSalaryStructuresAsync(SelectedEmployee.Id);
                Structures.Clear();
                foreach (var st in structures)
                {
                    Structures.Add(st);
                }
                if (Structures.Count > 0)
                {
                    SelectedStructure = Structures.OrderByDescending(s => s.EffectiveFrom).First();
                }
                else
                {
                    NewStructure();
                }
            }
            catch (Exception ex)
            {
                _notificationService.ShowError($"Failed to load salary structures: {ex.Message}");
            }
        }

        partial void OnSelectedEmployeeChanged(Employee? value)
        {
            if (value != null)
            {
                LoadStructuresCommand.Execute(null);
            }
        }

        partial void OnSelectedStructureChanged(SalaryStructure? value)
        {
            if (value != null)
            {
                Basic = value.Basic;
                Hra = value.Hra;
                Da = value.Da;
                ConveyanceAllowance = value.ConveyanceAllowance;
                MedicalAllowance = value.MedicalAllowance;
                SpecialAllowance = value.SpecialAllowance;
                PfApplicable = value.PfApplicable;
                EsiApplicable = value.EsiApplicable;
                ProfessionalTaxApplicable = value.ProfessionalTaxApplicable;
                EffectiveFrom = value.EffectiveFrom;
            }
            RecalculateGross();
        }

        partial void OnBasicChanged(decimal value) => RecalculateGross();
        partial void OnHraChanged(decimal value) => RecalculateGross();
        partial void OnDaChanged(decimal value) => RecalculateGross();
        partial void OnConveyanceAllowanceChanged(decimal value) => RecalculateGross();
        partial void OnMedicalAllowanceChanged(decimal value) => RecalculateGross();
        partial void OnSpecialAllowanceChanged(decimal value) => RecalculateGross();

        private void RecalculateGross()
        {
            OnPropertyChanged(nameof(GrossMonthly));
        }

        [RelayCommand]
        public void NewStructure()
        {
            SelectedStructure = null;
            Basic = 0m;
            Hra = 0m;
            Da = 0m;
            ConveyanceAllowance = 0m;
            MedicalAllowance = 0m;
            SpecialAllowance = 0m;
            PfApplicable = true;
            EsiApplicable = false;
            ProfessionalTaxApplicable = true;
            EffectiveFrom = DateTime.Today;
        }

        [RelayCommand]
        public async Task SaveStructureAsync()
        {
            if (SelectedEmployee == null)
            {
                _notificationService.ShowError("Please select an employee first.");
                return;
            }

            try
            {
                var structure = SelectedStructure ?? new SalaryStructure();
                structure.EmployeeId = SelectedEmployee.Id;
                structure.Basic = Basic;
                structure.Hra = Hra;
                structure.Da = Da;
                structure.ConveyanceAllowance = ConveyanceAllowance;
                structure.MedicalAllowance = MedicalAllowance;
                structure.SpecialAllowance = SpecialAllowance;
                structure.PfApplicable = PfApplicable;
                structure.EsiApplicable = EsiApplicable;
                structure.ProfessionalTaxApplicable = ProfessionalTaxApplicable;
                structure.EffectiveFrom = EffectiveFrom;

                await _payrollService.SaveSalaryStructureAsync(structure);
                _notificationService.ShowSuccess($"Salary structure for {SelectedEmployee.FirstName} saved successfully.");
                await LoadStructuresAsync();
            }
            catch (Exception ex)
            {
                _notificationService.ShowError($"Failed to save salary structure: {ex.Message}");
            }
        }
    }
}
