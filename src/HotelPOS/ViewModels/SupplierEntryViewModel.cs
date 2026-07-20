using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Common.Constants;
using HotelPOS.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using System.Text.RegularExpressions;

namespace HotelPOS.ViewModels
{
    public partial class SupplierEntryViewModel : ObservableObject
    {
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
        private string? _paymentTerms = PaymentModes.Cash; // Cash / Credit / Days

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

        public SupplierEntryViewModel(ISupplierService supplierService, INotificationService notificationService)
        {
            _notificationService = notificationService;

            if (System.Windows.Application.Current == null)
            {
                App.RegisterTestService(supplierService);
                App.RegisterTestService(notificationService);
            }
        }

        public void LoadSupplier(Supplier supplier) // NOSONAR
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
            PaymentTerms = supplier.PaymentTerms ?? PaymentModes.Cash;
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

        public bool ValidateName() // NOSONAR
        {
            if (string.IsNullOrWhiteSpace(Name))
            {
                NameError = "Supplier Name is required";
                return false;
            }
            NameError = string.Empty;
            return true;
        }

        public bool ValidatePhone() // NOSONAR
        {
            if (string.IsNullOrWhiteSpace(Phone))
            {
                PhoneError = string.Empty;
                return true;
            }
            var cleanPhone = Regex.Replace(Phone, @"[^\d]", "", RegexOptions.None, TimeSpan.FromMilliseconds(250));
            if (cleanPhone.Length < 10 || cleanPhone.Length > 15)
            {
                PhoneError = "Invalid phone number (must be 10-15 digits)";
                return false;
            }
            PhoneError = string.Empty;
            return true;
        }

        public bool ValidateEmail() // NOSONAR
        {
            if (string.IsNullOrWhiteSpace(Email))
            {
                EmailError = string.Empty;
                return true;
            }
            var emailRegex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.None, TimeSpan.FromSeconds(1));
            if (!emailRegex.IsMatch(Email.Trim()))
            {
                EmailError = "Please enter a valid Email ID";
                return false;
            }
            EmailError = string.Empty;
            return true;
        }

        /// <summary>
        /// Validates the view-model's fields, persists the supplier to the data store, and requests the UI to close on success.
        /// </summary>
        /// <remarks>
        /// Runs name, phone, and email validation; if validation fails it shows a warning and aborts. It verifies supplier-name uniqueness (excluding the current record), normalizes the phone value, maps view-model fields to a Supplier entity, and saves it using an ISupplierService resolved from a scoped provider (falling back to the injected service). On success it shows a success notification and invokes <c>RequestClose</c> with <c>true</c>. On failure it shows an error notification; exceptions are handled internally.
        /// </remarks>
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

                using (var scope = App.CreateDbScope())
                {
                    var supplierService = scope.ServiceProvider.GetRequiredService<ISupplierService>();

                    // Check for duplicate names (excluding current ID)
                    var isUnique = await supplierService.ValidateSupplierNameUniqueAsync(Name, Id);
                    if (!isUnique)
                    {
                        NameError = $"A supplier named '{Name.Trim()}' already exists.";
                        _notificationService.ShowWarning(NameError);
                        return;
                    }

                    // Phone formatting
                    var cleanPhone = Regex.Replace(Phone, @"[^\d\+\-\(\)\s]", "", RegexOptions.None, TimeSpan.FromMilliseconds(250));

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

                    await supplierService.SaveSupplierAsync(supplier);
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
