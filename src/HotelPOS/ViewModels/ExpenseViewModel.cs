using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Common.Constants;
using HotelPOS.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;

namespace HotelPOS.ViewModels
{
    public partial class ExpenseViewModel : ObservableObject
    {
        private readonly IExpenseService _expenseService;
        private readonly INotificationService _notificationService;

        [ObservableProperty]
        private DateTime? _filterFrom = DateTime.Today;

        [ObservableProperty]
        private DateTime? _filterTo = DateTime.Today;

        [ObservableProperty]
        private string _selectedCategory = "All Categories";

        [ObservableProperty]
        private decimal _totalAmount;

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

        private Expense? _selectedExpense;
        public Expense? SelectedExpense
        {
            get => _selectedExpense;
            set => SetProperty(ref _selectedExpense, value);
        }

        private readonly List<Expense> _allExpenses = new();

        public ObservableCollection<Expense> Expenses { get; } = new();
        public ObservableCollection<string> Categories { get; } = new();

        // Callback to show the entry dialog (Add/Edit)
        public Func<Expense?, Task<bool>>? ShowEntryDialogAsync { get; set; }

        // Callback for confirming deletion
        public Func<string, Task<bool>>? ConfirmDeleteAsync { get; set; }

        public ExpenseViewModel(IExpenseService expenseService, INotificationService notificationService)
        {
            _expenseService = expenseService;
            _notificationService = notificationService;

            Categories.Add("All Categories");
            foreach (var category in ExpenseCategories.All)
            {
                Categories.Add(category);
            }

            if (System.Windows.Application.Current == null)
            {
                App.RegisterTestService(expenseService);
                App.RegisterTestService(notificationService);
            }
        }

        /// <summary>
        /// Loads expenses for the current filter range from the data service into the view model's in-memory list and updates the filtered Expenses collection.
        /// </summary>
        public async Task LoadExpensesAsync()
        {
            using (var scope = App.CreateDbScope())
            {
                var expenseService = scope.ServiceProvider.GetRequiredService<IExpenseService>();
                try
                {
                    var from = FilterFrom;
                    var to = FilterTo?.AddDays(1);
                    var expenses = await expenseService.GetExpensesAsync(from, to);
                    _allExpenses.Clear();
                    _allExpenses.AddRange(expenses);
                    ApplyFilter();
                }
                catch (Exception ex)
                {
                    _notificationService.ShowError($"Failed to load expenses: {ex.Message}");
                }
            }
        }

        [RelayCommand]
        public async Task ApplyFilterAsync()
        {
            await LoadExpensesAsync();
        }

        [RelayCommand]
        public async Task ResetFilterAsync()
        {
            FilterFrom = DateTime.Today;
            FilterTo = DateTime.Today;
            SelectedCategory = "All Categories";
            SearchText = string.Empty;
            await LoadExpensesAsync();
        }

        partial void OnSelectedCategoryChanged(string value)
        {
            ApplyFilter();
        }

        private void ApplyFilter()
        {
            Expenses.Clear();
            var search = SearchText?.Trim();

            var filtered = _allExpenses.Where(e =>
                (SelectedCategory == "All Categories" || e.Category == SelectedCategory) &&
                (string.IsNullOrEmpty(search) ||
                 e.Title.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                 (e.Description != null && e.Description.Contains(search, StringComparison.OrdinalIgnoreCase)))
            );

            decimal total = 0;
            foreach (var expense in filtered)
            {
                Expenses.Add(expense);
                total += expense.Amount;
            }
            TotalAmount = total;
        }

        [RelayCommand]
        private async Task AddExpenseAsync()
        {
            if (ShowEntryDialogAsync != null)
            {
                var success = await ShowEntryDialogAsync(null);
                if (success)
                {
                    await LoadExpensesAsync();
                }
            }
        }

        [RelayCommand]
        private async Task EditExpenseAsync(Expense? expense)
        {
            var target = expense ?? SelectedExpense;
            if (target == null)
            {
                _notificationService.ShowWarning("Please select an expense to edit.");
                return;
            }

            if (ShowEntryDialogAsync != null)
            {
                var success = await ShowEntryDialogAsync(target);
                if (success)
                {
                    await LoadExpensesAsync();
                }
            }
        }

        /// <summary>
        /// Deletes the specified expense (or the currently selected expense) after confirmation, displays a success or error notification, and refreshes the expense list.
        /// </summary>
        /// <param name="expense">The expense to delete; if null the currently selected expense is used.</param>
        [RelayCommand]
        private async Task DeleteExpenseAsync(Expense? expense)
        {
            var target = expense ?? SelectedExpense;
            if (target == null)
            {
                _notificationService.ShowWarning("Please select an expense to delete.");
                return;
            }

            if (ConfirmDeleteAsync != null)
            {
                var confirmed = await ConfirmDeleteAsync(target.Title);
                if (confirmed)
                {
                    using (var scope = App.CreateDbScope())
                    {
                        var expenseService = scope.ServiceProvider.GetRequiredService<IExpenseService>();
                        try
                        {
                            await expenseService.DeleteExpenseAsync(target.Id);
                            _notificationService.ShowSuccess($"Expense '{target.Title}' deleted successfully.");
                        }
                        catch (Exception ex)
                        {
                            _notificationService.ShowError($"Failed to delete expense: {ex.Message}");
                        }
                    }
                    await LoadExpensesAsync();
                }
            }
        }
    }
}
