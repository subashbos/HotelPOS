using HotelPOS.Domain.Entities;
using HotelPOS.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Controls;

namespace HotelPOS.Views
{
    public partial class SupplierView : UserControl
    {
        private readonly SupplierViewModel _viewModel;
        private readonly IServiceProvider _serviceProvider;

        public SupplierView(SupplierViewModel viewModel, IServiceProvider serviceProvider)
        {
            InitializeComponent();
            _viewModel = viewModel;
            _serviceProvider = serviceProvider;
            DataContext = _viewModel;

            // Wire up ViewModel callbacks
            _viewModel.ShowEntryDialogAsync = ShowSupplierEntryDialogAsync;
            _viewModel.ConfirmDeleteAsync = ConfirmDeleteAsync;

            Loaded += async (s, e) => await _viewModel.LoadSuppliersAsync();
        }

        private Task<bool> ShowSupplierEntryDialogAsync(Supplier? supplier)
        {
            try
            {
                // Retrieve transient entry ViewModel from container
                var entryVm = _serviceProvider.GetRequiredService<SupplierEntryViewModel>();
                if (supplier != null)
                {
                    entryVm.LoadSupplier(supplier);
                }

                var dialog = new SupplierEntryDialog(entryVm)
                {
                    Owner = Window.GetWindow(this)
                };

                var result = dialog.ShowDialog();
                return Task.FromResult(result == true);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening entry form: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return Task.FromResult(false);
            }
        }

        private Task<bool> ConfirmDeleteAsync(string supplierName)
        {
            var result = MessageBox.Show(
                $"Are you sure you want to delete supplier '{supplierName}'? This action cannot be undone.",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            return Task.FromResult(result == MessageBoxResult.Yes);
        }
    }
}
