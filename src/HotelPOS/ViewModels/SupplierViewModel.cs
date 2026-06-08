using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace HotelPOS.ViewModels
{
    public partial class SupplierViewModel : ObservableObject
    {
        private readonly ISupplierService _supplierService;
        private readonly INotificationService _notificationService;
        private readonly List<Supplier> _allSuppliers = new();

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

        private Supplier? _selectedSupplier;
        public Supplier? SelectedSupplier
        {
            get => _selectedSupplier;
            set => SetProperty(ref _selectedSupplier, value);
        }

        public ObservableCollection<Supplier> Suppliers { get; } = new();

        // Callback to show the entry dialog (Add/Edit)
        public Func<Supplier?, Task<bool>>? ShowEntryDialogAsync { get; set; }

        // Callback for confirming deletion
        public Func<string, Task<bool>>? ConfirmDeleteAsync { get; set; }

        public SupplierViewModel(ISupplierService supplierService, INotificationService notificationService)
        {
            _supplierService = supplierService;
            _notificationService = notificationService;
        }

        public async Task LoadSuppliersAsync()
        {
            using (var scope = App.CreateDbScope())
            {
                var supplierService = scope.ServiceProvider.GetService<ISupplierService>() ?? _supplierService;
                try
                {
                    var suppliers = await supplierService.GetSuppliersAsync();
                    _allSuppliers.Clear();
                    _allSuppliers.AddRange(suppliers);
                    ApplyFilter();
                }
                catch (Exception ex)
                {
                    _notificationService.ShowError($"Failed to load suppliers: {ex.Message}");
                }
            }
        }

        private void ApplyFilter()
        {
            Suppliers.Clear();
            var search = SearchText.Trim().ToLower();

            var filtered = _allSuppliers.Where(s =>
                string.IsNullOrEmpty(search) ||
                s.Name.ToLower().Contains(search) ||
                (s.ContactPerson != null && s.ContactPerson.ToLower().Contains(search)) ||
                (s.Phone != null && s.Phone.Contains(search)) ||
                (s.City != null && s.City.ToLower().Contains(search)) ||
                (s.Gstin != null && s.Gstin.ToLower().Contains(search))
            );

            foreach (var sup in filtered)
            {
                Suppliers.Add(sup);
            }
        }

        [RelayCommand]
        private async Task AddSupplierAsync()
        {
            if (ShowEntryDialogAsync != null)
            {
                var success = await ShowEntryDialogAsync(null);
                if (success)
                {
                    await LoadSuppliersAsync();
                }
            }
        }

        [RelayCommand]
        private async Task EditSupplierAsync(Supplier? supplier)
        {
            var target = supplier ?? SelectedSupplier;
            if (target == null)
            {
                _notificationService.ShowWarning("Please select a supplier to edit.");
                return;
            }

            if (ShowEntryDialogAsync != null)
            {
                var success = await ShowEntryDialogAsync(target);
                if (success)
                {
                    await LoadSuppliersAsync();
                }
            }
        }

        [RelayCommand]
        private async Task DeleteSupplierAsync(Supplier? supplier)
        {
            var target = supplier ?? SelectedSupplier;
            if (target == null)
            {
                _notificationService.ShowWarning("Please select a supplier to delete.");
                return;
            }

            if (ConfirmDeleteAsync != null)
            {
                var confirmed = await ConfirmDeleteAsync(target.Name);
                if (confirmed)
                {
                    using (var scope = App.CreateDbScope())
                    {
                        var supplierService = scope.ServiceProvider.GetService<ISupplierService>() ?? _supplierService;
                        try
                        {
                            await supplierService.DeleteSupplierAsync(target.Id);
                            _notificationService.ShowSuccess($"Supplier '{target.Name}' deleted successfully.");
                        }
                        catch (Exception ex)
                        {
                            _notificationService.ShowError($"Failed to delete supplier: {ex.Message}");
                        }
                    }
                    await LoadSuppliersAsync();
                }
            }
        }
    }
}
