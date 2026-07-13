using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Entities;
using HotelPOS.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Controls;

namespace HotelPOS.Views
{
    public partial class EmployeeView : UserControl
    {
        private readonly EmployeeViewModel _viewModel;
        private readonly IServiceProvider _serviceProvider;

        public EmployeeView(EmployeeViewModel viewModel, IServiceProvider serviceProvider)
        {
            InitializeComponent();
            _viewModel = viewModel;
            _serviceProvider = serviceProvider;
            DataContext = _viewModel;

            _viewModel.ShowEntryDialogAsync = ShowEmployeeEntryDialogAsync;
            _viewModel.ConfirmDeleteAsync = ConfirmDeleteAsync;

            Loaded += async (s, e) => await _viewModel.LoadEmployeesAsync();
        }

        private async Task<bool> ShowEmployeeEntryDialogAsync(Employee? employee)
        {
            try
            {
                var entryVm = _serviceProvider.GetRequiredService<EmployeeEntryViewModel>();
                await entryVm.InitializeAsync();
                if (employee != null)
                {
                    entryVm.LoadEmployee(employee);
                }

                var dialog = new EmployeeEntryDialog(entryVm)
                {
                    Owner = Window.GetWindow(this)
                };

                var result = dialog.ShowDialog();
                return result == true;
            }
            catch (Exception ex)
            {
                App.CurrentApp!.ServiceProvider.GetRequiredService<IDialogService>().ShowMessage($"Error opening entry form: {ex.Message}", "Error", DialogButton.OK, DialogIcon.Error);
                return false;
            }
        }

        private Task<bool> ConfirmDeleteAsync(string employeeName) // NOSONAR
        {
            var result = App.CurrentApp!.ServiceProvider.GetRequiredService<IDialogService>().ShowMessage(
                $"Are you sure you want to delete employee '{employeeName}'? This action cannot be undone.",
                "Confirm Delete",
                DialogButton.YesNo,
                DialogIcon.Warning);

            return Task.FromResult(result == DialogResult.Yes);
        }
    }
}
