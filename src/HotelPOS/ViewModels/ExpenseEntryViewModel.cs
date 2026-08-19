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
    public partial class ExpenseEntryViewModel : ObservableObject, IEntryDialogViewModel
    {
        private readonly INotificationService _notificationService;

        public event EventHandler<bool>? RequestClose;

        ICommand IEntryDialogViewModel.SaveCommand => SaveCommand;

        [ObservableProperty]
        private int _id;

        [ObservableProperty]
        private DateTime _date = DateTime.Today;

        [ObservableProperty]
        private string _title = string.Empty;

        [ObservableProperty]
        private string? _description;

        [ObservableProperty]
        private decimal _amount;

        [ObservableProperty]
        private string _category = ExpenseCategories.General;

        [ObservableProperty]
        private string _paymentMode = PaymentModes.Cash;

        [ObservableProperty]
        private bool _isEditMode;

        [ObservableProperty]
        private string _titleError = string.Empty;

        public bool IsTitleInvalid => !string.IsNullOrEmpty(TitleError); // NOSONAR

        [ObservableProperty]
        private string _amountError = string.Empty;

        public bool IsAmountInvalid => !string.IsNullOrEmpty(AmountError); // NOSONAR

        public ObservableCollection<string> Categories { get; } = new(ExpenseCategories.All);

        public ExpenseEntryViewModel(IExpenseService expenseService, INotificationService notificationService)
        {
            _notificationService = notificationService;

            if (System.Windows.Application.Current == null)
            {
                App.RegisterTestService(expenseService);
                App.RegisterTestService(notificationService);
            }
        }

        public void LoadExpense(Expense expense) // NOSONAR
        {
            Id = expense.Id;
            Date = expense.Date;
            Title = expense.Title;
            Description = expense.Description;
            Amount = expense.Amount;
            Category = expense.Category;
            PaymentMode = expense.PaymentMode ?? PaymentModes.Cash;
            IsEditMode = Id > 0;

            TitleError = string.Empty;
            AmountError = string.Empty;
        }

        partial void OnTitleChanged(string value)
        {
            ValidateTitle();
        }

        partial void OnAmountChanged(decimal value)
        {
            ValidateAmount();
        }

        public bool ValidateTitle() // NOSONAR
        {
            var isValid = EntryValidation.ValidateRequired(Title, "Title", out var error);
            TitleError = error;
            return isValid;
        }

        public bool ValidateAmount() // NOSONAR
        {
            if (Amount <= 0)
            {
                AmountError = "Amount must be greater than zero";
                return false;
            }
            AmountError = string.Empty;
            return true;
        }

        /// <summary>
        /// Validates the view-model's fields, persists the expense to the data store, and requests the UI to close on success.
        /// </summary>
        [RelayCommand]
        private async Task SaveAsync()
        {
            try
            {
                bool isTitleValid = ValidateTitle();
                bool isAmountValid = ValidateAmount();

                if (!isTitleValid || !isAmountValid)
                {
                    _notificationService.ShowWarning("Please correct the errors in the form before saving.");
                    return;
                }

                var expense = new Expense
                {
                    Id = Id,
                    Date = Date,
                    Title = Title.Trim(),
                    Description = Description?.Trim(),
                    Amount = Amount,
                    Category = Category.Trim(),
                    PaymentMode = PaymentMode.Trim()
                };

                using (var scope = App.CreateDbScope())
                {
                    var expenseService = scope.ServiceProvider.GetRequiredService<IExpenseService>();
                    await expenseService.SaveExpenseAsync(expense);
                }

                _notificationService.ShowSuccess(IsEditMode ? "Expense updated successfully." : "Expense saved successfully.");
                RequestClose?.Invoke(this, true);
            }
            catch (Exception ex)
            {
                _notificationService.ShowError($"Failed to save expense: {ex.Message}");
            }
        }

        [RelayCommand]
        private void Cancel()
        {
            RequestClose?.Invoke(this, false);
        }
    }
}
