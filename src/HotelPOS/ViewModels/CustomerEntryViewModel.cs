using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using System.Text.RegularExpressions;
using System.Windows.Input;

namespace HotelPOS.ViewModels
{
    public partial class CustomerEntryViewModel : ObservableObject, IEntryDialogViewModel
    {
        private readonly INotificationService _notificationService;

        public event EventHandler<bool>? RequestClose;

        ICommand IEntryDialogViewModel.SaveCommand => SaveCommand;

        [ObservableProperty]
        private int _id;

        [ObservableProperty]
        private string _name = string.Empty;

        [ObservableProperty]
        private string? _phone;

        [ObservableProperty]
        private string? _email;

        [ObservableProperty]
        private string? _gstin;

        [ObservableProperty]
        private string? _address;

        [ObservableProperty]
        private string? _notes;

        [ObservableProperty]
        private bool _isEditMode;

        [ObservableProperty]
        private string _nameError = string.Empty;

        public bool IsNameInvalid => !string.IsNullOrEmpty(NameError); // NOSONAR

        [ObservableProperty]
        private string _phoneError = string.Empty;

        public bool IsPhoneInvalid => !string.IsNullOrEmpty(PhoneError); // NOSONAR

        [ObservableProperty]
        private string _emailError = string.Empty;

        public bool IsEmailInvalid => !string.IsNullOrEmpty(EmailError); // NOSONAR

        public CustomerEntryViewModel(ICustomerService customerService, INotificationService notificationService)
        {
            _notificationService = notificationService;

            if (System.Windows.Application.Current == null)
            {
                App.RegisterTestService(customerService);
                App.RegisterTestService(notificationService);
            }
        }

        public void LoadCustomer(Customer customer) // NOSONAR
        {
            Id = customer.Id;
            Name = customer.Name;
            Phone = customer.Phone;
            Email = customer.Email;
            Gstin = customer.Gstin;
            Address = customer.Address;
            Notes = customer.Notes;
            IsEditMode = Id > 0;

            NameError = string.Empty;
            PhoneError = string.Empty;
            EmailError = string.Empty;
        }

        partial void OnNameChanged(string value)
        {
            ValidateName();
        }

        partial void OnPhoneChanged(string? value)
        {
            ValidatePhone();
        }

        partial void OnEmailChanged(string? value)
        {
            ValidateEmail();
        }

        public bool ValidateName() // NOSONAR
        {
            var isValid = EntryValidation.ValidateRequired(Name, "Customer Name", out var error);
            NameError = error;
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
                bool isNameValid = ValidateName();
                bool isPhoneValid = ValidatePhone();
                bool isEmailValid = ValidateEmail();

                if (!isNameValid || !isPhoneValid || !isEmailValid)
                {
                    _notificationService.ShowWarning("Please correct the errors in the form before saving.");
                    return;
                }

                using (var scope = App.CreateDbScope())
                {
                    var customerService = scope.ServiceProvider.GetRequiredService<ICustomerService>();

                    var customer = new Customer
                    {
                        Id = Id,
                        Name = Name.Trim(),
                        Phone = string.IsNullOrWhiteSpace(Phone) ? null : Regex.Replace(Phone, @"[^\d\+\-\(\)\s]", "", RegexOptions.None, TimeSpan.FromMilliseconds(250)).Trim(),
                        Email = Email?.Trim(),
                        Gstin = Gstin?.Trim(),
                        Address = Address?.Trim(),
                        Notes = Notes?.Trim()
                    };

                    try
                    {
                        await customerService.SaveCustomerAsync(customer);
                    }
                    catch (ArgumentException ex)
                    {
                        // This handles server-side validation errors not caught by client-side checks, such as duplicate phone or invalid GSTIN.
                        _notificationService.ShowWarning(ex.Message);
                        return;
                    }
                }

                _notificationService.ShowSuccess(IsEditMode ? "Customer updated successfully." : "Customer saved successfully.");
                RequestClose?.Invoke(this, true);
            }
            catch (Exception ex)
            {
                _notificationService.ShowError($"Failed to save customer: {ex.Message}");
            }
        }

        [RelayCommand]
        private void Cancel()
        {
            RequestClose?.Invoke(this, false);
        }
    }
}
