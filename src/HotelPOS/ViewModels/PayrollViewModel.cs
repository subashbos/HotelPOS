using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;

namespace HotelPOS.ViewModels
{
    public partial class PayrollViewModel : ObservableObject
    {
        private readonly INotificationService _notificationService;

        [ObservableProperty]
        private int _month = DateTime.Today.Month;

        [ObservableProperty]
        private int _year = DateTime.Today.Year;

        [ObservableProperty]
        private PayrollRun? _selectedRun;

        [ObservableProperty]
        private Employee? _salaryEmployee;

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

        public ObservableCollection<Employee> Employees { get; } = new();
        public ObservableCollection<PayrollRun> Runs { get; } = new();
        public ObservableCollection<Payslip> Payslips { get; } = new();

        public PayrollViewModel(IPayrollService payrollService, IEmployeeService employeeService, INotificationService notificationService)
        {
            _notificationService = notificationService;

            if (System.Windows.Application.Current == null)
            {
                App.RegisterTestService(payrollService);
                App.RegisterTestService(employeeService);
                App.RegisterTestService(notificationService);
            }
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
                    foreach (var e in employees) Employees.Add(e);
                }
                catch (Exception ex)
                {
                    _notificationService.ShowError($"Failed to load employees: {ex.Message}");
                }
            }

            await LoadRunsAsync();
        }

        public async Task LoadRunsAsync()
        {
            using (var scope = App.CreateDbScope())
            {
                var payrollService = scope.ServiceProvider.GetRequiredService<IPayrollService>();
                try
                {
                    var runs = await payrollService.GetRunsAsync();
                    Runs.Clear();
                    foreach (var r in runs) Runs.Add(r);
                }
                catch (Exception ex)
                {
                    _notificationService.ShowError($"Failed to load payroll runs: {ex.Message}");
                }
            }
        }

        partial void OnSelectedRunChanged(PayrollRun? value)
        {
            _ = LoadPayslipsAsync();
        }

        private async Task LoadPayslipsAsync()
        {
            Payslips.Clear();
            if (SelectedRun == null) return;

            using (var scope = App.CreateDbScope())
            {
                var payrollService = scope.ServiceProvider.GetRequiredService<IPayrollService>();
                try
                {
                    var run = await payrollService.GetRunByIdAsync(SelectedRun.Id);
                    if (run != null)
                        foreach (var p in run.Payslips) Payslips.Add(p);
                }
                catch (Exception ex)
                {
                    _notificationService.ShowError($"Failed to load payslips: {ex.Message}");
                }
            }
        }

        [RelayCommand]
        private async Task RunPayrollAsync()
        {
            using (var scope = App.CreateDbScope())
            {
                var payrollService = scope.ServiceProvider.GetRequiredService<IPayrollService>();
                try
                {
                    await payrollService.RunPayrollAsync(Month, Year, AppSession.CurrentUser?.Id);
                    _notificationService.ShowSuccess($"Payroll for {Month:D2}/{Year} processed successfully.");
                }
                catch (Exception ex)
                {
                    _notificationService.ShowError($"Failed to run payroll: {ex.Message}");
                    return;
                }
            }

            await LoadRunsAsync();
        }

        [RelayCommand]
        private async Task MarkRunAsPaidAsync()
        {
            if (SelectedRun == null)
            {
                _notificationService.ShowWarning("Please select a payroll run.");
                return;
            }

            using (var scope = App.CreateDbScope())
            {
                var payrollService = scope.ServiceProvider.GetRequiredService<IPayrollService>();
                try
                {
                    await payrollService.MarkRunAsPaidAsync(SelectedRun.Id);
                    _notificationService.ShowSuccess($"Payroll for {SelectedRun.Month:D2}/{SelectedRun.Year} marked as paid.");
                }
                catch (Exception ex)
                {
                    _notificationService.ShowError($"Failed to mark payroll as paid: {ex.Message}");
                    return;
                }
            }

            await LoadRunsAsync();
        }

        [RelayCommand]
        private async Task SaveSalaryStructureAsync()
        {
            if (SalaryEmployee == null)
            {
                _notificationService.ShowWarning("Please select an employee.");
                return;
            }

            var structure = new SalaryStructure
            {
                EmployeeId = SalaryEmployee.Id,
                EffectiveFrom = DateTime.Today,
                Basic = Basic,
                Hra = Hra,
                Da = Da,
                ConveyanceAllowance = ConveyanceAllowance,
                MedicalAllowance = MedicalAllowance,
                SpecialAllowance = SpecialAllowance,
                PfApplicable = PfApplicable,
                EsiApplicable = EsiApplicable,
                ProfessionalTaxApplicable = ProfessionalTaxApplicable
            };

            using (var scope = App.CreateDbScope())
            {
                var payrollService = scope.ServiceProvider.GetRequiredService<IPayrollService>();
                try
                {
                    await payrollService.SaveSalaryStructureAsync(structure);
                    _notificationService.ShowSuccess($"Salary structure saved for {SalaryEmployee.FirstName}.");
                }
                catch (Exception ex)
                {
                    _notificationService.ShowError($"Failed to save salary structure: {ex.Message}");
                }
            }
        }
    }
}
