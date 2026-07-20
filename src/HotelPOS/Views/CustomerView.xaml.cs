using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Entities;
using HotelPOS.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Controls;

namespace HotelPOS.Views
{
    public partial class CustomerView : UserControl
    {
        private readonly CustomerViewModel _viewModel;
        private readonly IServiceProvider _serviceProvider;

        public CustomerView(CustomerViewModel viewModel, IServiceProvider serviceProvider)
        {
            InitializeComponent();
            _viewModel = viewModel;
            _serviceProvider = serviceProvider;
            DataContext = _viewModel;

            // Wire up ViewModel callbacks
            _viewModel.ShowEntryDialogAsync = ShowCustomerEntryDialogAsync;
            _viewModel.ShowHistoryAsync = ShowCustomerHistoryAsync;
            _viewModel.ConfirmDeactivateAsync = ConfirmDeactivateAsync;

            Loaded += async (s, e) => await _viewModel.LoadCustomersAsync();
        }

        private async Task<bool> ShowCustomerEntryDialogAsync(Customer? customer)
        {
            try
            {
                var entryVm = _serviceProvider.GetRequiredService<CustomerEntryViewModel>();
                if (customer != null)
                {
                    entryVm.LoadCustomer(customer);
                }

                var dialog = new CustomerEntryDialog(entryVm)
                {
                    Owner = Window.GetWindow(this)
                };

                var result = dialog.ShowDialog();
                return result.GetValueOrDefault();
            }
            catch (Exception ex)
            {
                await App.CurrentApp!.ServiceProvider.GetRequiredService<HotelPOS.Application.Interfaces.IDialogService>()
                    .ShowMessageAsync($"Error opening entry form: {ex.Message}", "Error", HotelPOS.Application.Interfaces.DialogButton.OK, HotelPOS.Application.Interfaces.DialogIcon.Error);
                return false;
            }
        }

        private async Task ShowCustomerHistoryAsync(Customer customer)
        {
            try
            {
                var historyVm = _serviceProvider.GetRequiredService<CustomerHistoryViewModel>();
                await historyVm.LoadAsync(customer.Id);

                var dialog = new CustomerHistoryDialog(historyVm)
                {
                    Owner = Window.GetWindow(this)
                };
                dialog.ShowDialog();
            }
            catch (Exception ex)
            {
                await App.CurrentApp!.ServiceProvider.GetRequiredService<HotelPOS.Application.Interfaces.IDialogService>()
                    .ShowMessageAsync($"Error opening customer history: {ex.Message}", "Error", HotelPOS.Application.Interfaces.DialogButton.OK, HotelPOS.Application.Interfaces.DialogIcon.Error);
            }
        }

        private static async Task<bool> ConfirmDeactivateAsync(string customerName)
        {
            var result = await App.CurrentApp!.ServiceProvider.GetRequiredService<HotelPOS.Application.Interfaces.IDialogService>().ShowMessageAsync(
                $"Are you sure you want to deactivate customer '{customerName}'? Their order history will be preserved.",
                "Confirm Deactivate",
                HotelPOS.Application.Interfaces.DialogButton.YesNo,
                HotelPOS.Application.Interfaces.DialogIcon.Warning);

            return result == HotelPOS.Application.Interfaces.DialogResult.Yes;
        }
    }
}
