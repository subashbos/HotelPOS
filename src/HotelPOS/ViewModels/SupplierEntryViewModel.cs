using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Entities;
using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace HotelPOS.ViewModels
{
    public partial class SupplierEntryViewModel : ObservableObject
    {
        private readonly ISupplierService _supplierService;
        private readonly INotificationService _notificationService;

        public event EventHandler<bool>? RequestClose;

        [ObservableProperty]
        private int _id;

        [ObservableProperty]
        private string _name = string.Empty;

        [ObservableProperty]
        private string? _contactPerson;

        [ObservableProperty]
        private string _phone = string.Empty;

        [ObservableProperty]
        private string? _email;

        [ObservableProperty]
        private string? _gstin;

        [ObservableProperty]
        private string? _address;

        [ObservableProperty]
        private string? _city;

        [ObservableProperty]
        private string? _state;

        [ObservableProperty]
        private string? _pincode;

        [ObservableProperty]
        private decimal _openingBalance;

        [ObservableProperty]
        private decimal _creditLimit = 50000; // sensible default limit

        [ObservableProperty]
        private string? _paymentTerms = "Cash"; // Cash / Credit / Days

        [ObservableProperty]
        private bool _isEditMode;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsNameInvalid))]
        private string _nameError = string.Empty;

        public bool IsNameInvalid => !string.IsNullOrEmpty(NameError);

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsPhoneInvalid))]
        private string _phoneError = string.Empty;

        public bool IsPhoneInvalid => !string.IsNullOrEmpty(PhoneError);

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsEmailInvalid))]
        private string _emailError = string.Empty;

        public bool IsEmailInvalid => !string.IsNullOrEmpty(EmailError);

        public SupplierEntryViewModel(ISupplierService supplierService, INotificationService notificationService)
        {
            _supplierService = supplierService;
            _notificationService = notificationService;
        }

        public void LoadSupplier(Supplier supplier)
        {
            Id = supplier.Id;
            Name = supplier.Name;
            ContactPerson = supplier.ContactPerson;
            Phone = supplier.Phone ?? string.Empty;
            Email = supplier.Email;
            Gstin = supplier.Gstin;
            Address = supplier.Address;
            City = supplier.City;
            State = supplier.State;
            Pincode = supplier.Pincode;
            OpeningBalance = supplier.OpeningBalance;
            CreditLimit = supplier.CreditLimit;
            PaymentTerms = supplier.PaymentTerms ?? "Cash";
            IsEditMode = Id > 0;

            // Clear errors on initial load
            NameError = string.Empty;
            PhoneError = string.Empty;
            EmailError = string.Empty;
        }

        partial void OnNameChanged(string value)
        {
            ValidateName();
        }

        partial void OnPhoneChanged(string value)
        {
            ValidatePhone();
        }

        partial void OnEmailChanged(string? value)
        {
            ValidateEmail();
        }

        public bool ValidateName()
        {
            if (string.IsNullOrWhiteSpace(Name))
            {
                NameError = "Supplier Name is required";
                return false;
            }
            NameError = string.Empty;
            return true;
        }

        public bool ValidatePhone()
        {
            if (string.IsNullOrWhiteSpace(Phone))
            {
                PhoneError = string.Empty;
                return true;
            }
            var cleanPhone = Regex.Replace(Phone, @"[^\d]", "");
            if (cleanPhone.Length < 10 || cleanPhone.Length > 15)
            {
                PhoneError = "Invalid phone number (must be 10-15 digits)";
                return false;
            }
            PhoneError = string.Empty;
            return true;
        }

        public bool ValidateEmail()
        {
            if (string.IsNullOrWhiteSpace(Email))
            {
                EmailError = string.Empty;
                return true;
            }
            var emailRegex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
            if (!emailRegex.IsMatch(Email.Trim()))
            {
                EmailError = "Please enter a valid Email ID";
                return false;
            }
            EmailError = string.Empty;
            return true;
        }

        [RelayCommand]
        private async Task SaveAsync()
        {
            try
            {
                // Run all validations
                bool isNameValid = ValidateName();
                bool isPhoneValid = ValidatePhone();
                bool isEmailValid = ValidateEmail();

                if (!isNameValid || !isPhoneValid || !isEmailValid)
                {
                    _notificationService.ShowWarning("Please correct the errors in the form before saving.");
                    return;
                }

                await App.DbLock.WaitAsync();
                try
                {
                    // Check for duplicate names (excluding current ID)
                    var isUnique = await _supplierService.ValidateSupplierNameUniqueAsync(Name, Id);
                    if (!isUnique)
                    {
                        NameError = $"A supplier named '{Name.Trim()}' already exists.";
                        _notificationService.ShowWarning(NameError);
                        return;
                    }

                    // Phone formatting
                    var cleanPhone = Regex.Replace(Phone, @"[^\d\+\-\(\)\s]", "");

                    // Map to domain entity
                    var supplier = new Supplier
                    {
                        Id = Id,
                        Name = Name.Trim(),
                        ContactPerson = ContactPerson?.Trim(),
                        Phone = cleanPhone.Trim(),
                        Email = Email?.Trim(),
                        Gstin = Gstin?.Trim(),
                        Address = Address?.Trim(),
                        City = City?.Trim(),
                        State = State?.Trim(),
                        Pincode = Pincode?.Trim(),
                        OpeningBalance = OpeningBalance,
                        CreditLimit = CreditLimit,
                        PaymentTerms = PaymentTerms?.Trim()
                    };

                    await _supplierService.SaveSupplierAsync(supplier);
                }
                finally
                {
                    App.DbLock.Release();
                }

                _notificationService.ShowSuccess(IsEditMode ? "Supplier updated successfully." : "Supplier saved successfully.");
                RequestClose?.Invoke(this, true);
            }
            catch (Exception ex)
            {
                _notificationService.ShowError($"Failed to save supplier: {ex.Message}");
            }
        }

        [RelayCommand]
        private void Cancel()
        {
            RequestClose?.Invoke(this, false);
        }
    }
}
