using HotelPOS.Application.UseCases;
using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Entities;
using HotelPOS.ViewModels;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace HotelPOS.Tests.Unit.ViewModels
{
    public class ExpenseViewModelTests
    {
        private readonly Mock<IExpenseRepository> _expenseRepoMock;
        private readonly Mock<INotificationService> _notificationServiceMock;
        private readonly ExpenseService _expenseService;

        public ExpenseViewModelTests()
        {
            _expenseRepoMock = new Mock<IExpenseRepository>();
            _notificationServiceMock = new Mock<INotificationService>();
            _expenseService = new ExpenseService(_expenseRepoMock.Object);
        }

        [Fact]
        public async Task ExpenseViewModel_FiltersByCategoryAndSearchText()
        {
            // Arrange
            var mockExpenses = new List<Expense>
            {
                new Expense { Id = 1, Title = "Vegetable Purchase", Category = "Raw Material", Amount = 500, Date = DateTime.Today },
                new Expense { Id = 2, Title = "Electricity Bill", Category = "Utilities", Amount = 3000, Date = DateTime.Today },
                new Expense { Id = 3, Title = "Staff Salary", Category = "Salary", Amount = 15000, Date = DateTime.Today }
            };

            _expenseRepoMock.Setup(r => r.GetAllAsync(It.IsAny<DateTime?>(), It.IsAny<DateTime?>())).ReturnsAsync(mockExpenses);

            var vm = new ExpenseViewModel(_expenseService, _notificationServiceMock.Object);

            // Act & Assert (Load initially)
            await vm.LoadExpensesAsync();
            Assert.Equal(3, vm.Expenses.Count);
            Assert.Equal(18500, vm.TotalAmount);

            // Filter by category
            vm.SelectedCategory = "Utilities";
            Assert.Single(vm.Expenses);
            Assert.Equal("Electricity Bill", vm.Expenses[0].Title);
            Assert.Equal(3000, vm.TotalAmount);

            // Reset and filter by search text
            vm.SelectedCategory = "All Categories";
            vm.SearchText = "Salary";
            Assert.Single(vm.Expenses);
            Assert.Equal("Staff Salary", vm.Expenses[0].Title);
        }

        [Fact]
        public async Task ExpenseEntryViewModel_SaveCommand_ValidatesAndTriggersSave()
        {
            // Arrange
            var entryVm = new ExpenseEntryViewModel(_expenseService, _notificationServiceMock.Object);
            entryVm.Title = "New Expense";
            entryVm.Category = "Miscellaneous";
            entryVm.Amount = 250;

            bool closeRequested = false;
            entryVm.RequestClose += (s, success) => closeRequested = success;

            // Act
            await entryVm.SaveCommand.ExecuteAsync(null);

            // Assert
            _expenseRepoMock.Verify(r => r.AddAsync(It.Is<Expense>(e => e.Title == "New Expense" && e.Amount == 250)), Times.Once);
            _notificationServiceMock.Verify(n => n.ShowSuccess(It.IsAny<string>()), Times.Once);
            Assert.True(closeRequested);
        }

        [Fact]
        public async Task ExpenseEntryViewModel_SaveCommand_InvalidForm_ShowsWarning()
        {
            // Arrange
            var entryVm = new ExpenseEntryViewModel(_expenseService, _notificationServiceMock.Object);
            entryVm.Title = ""; // invalid
            entryVm.Amount = 0; // invalid

            // Act
            await entryVm.SaveCommand.ExecuteAsync(null);

            // Assert
            _notificationServiceMock.Verify(n => n.ShowWarning(It.IsAny<string>()), Times.Once);
            _expenseRepoMock.Verify(r => r.AddAsync(It.IsAny<Expense>()), Times.Never);
        }

        [Fact]
        public void ExpenseEntryViewModel_ValidateTitle_ValidAndEmpty_SetsErrors()
        {
            var vm = new ExpenseEntryViewModel(_expenseService, _notificationServiceMock.Object);

            vm.Title = "  ";
            Assert.False(vm.ValidateTitle());
            Assert.True(vm.IsTitleInvalid);
            Assert.Equal("Title is required", vm.TitleError);

            vm.Title = "Valid Title";
            Assert.True(vm.ValidateTitle());
            Assert.False(vm.IsTitleInvalid);
            Assert.Empty(vm.TitleError);
        }

        [Fact]
        public void ExpenseEntryViewModel_ValidateAmount_ValidAndInvalid_SetsErrors()
        {
            var vm = new ExpenseEntryViewModel(_expenseService, _notificationServiceMock.Object);

            vm.Amount = 0;
            Assert.False(vm.ValidateAmount());
            Assert.True(vm.IsAmountInvalid);
            Assert.Equal("Amount must be greater than zero", vm.AmountError);

            vm.Amount = 100;
            Assert.True(vm.ValidateAmount());
            Assert.False(vm.IsAmountInvalid);
            Assert.Empty(vm.AmountError);
        }

        [Fact]
        public void ExpenseEntryViewModel_LoadExpense_MapsAllFieldsAndSetsEditMode()
        {
            var vm = new ExpenseEntryViewModel(_expenseService, _notificationServiceMock.Object);
            var expense = new Expense
            {
                Id = 42,
                Date = new DateTime(2026, 7, 10),
                Title = "Water Bill",
                Description = "Monthly water bill",
                Amount = 800,
                Category = "Utilities",
                PaymentMode = "UPI"
            };

            vm.LoadExpense(expense);

            Assert.Equal(42, vm.Id);
            Assert.Equal(new DateTime(2026, 7, 10), vm.Date);
            Assert.Equal("Water Bill", vm.Title);
            Assert.Equal("Monthly water bill", vm.Description);
            Assert.Equal(800, vm.Amount);
            Assert.Equal("Utilities", vm.Category);
            Assert.Equal("UPI", vm.PaymentMode);
            Assert.True(vm.IsEditMode);

            var newExpense = new Expense { Id = 0, Title = "Fresh Expense", Category = "General", Amount = 10 };
            vm.LoadExpense(newExpense);
            Assert.False(vm.IsEditMode);
        }

        [Fact]
        public void ExpenseEntryViewModel_CancelCommand_TriggersRequestCloseWithFalse()
        {
            var vm = new ExpenseEntryViewModel(_expenseService, _notificationServiceMock.Object);
            bool closeFired = false;
            bool successResult = true;

            vm.RequestClose += (sender, result) =>
            {
                closeFired = true;
                successResult = result;
            };

            vm.CancelCommand.Execute(null);

            Assert.True(closeFired);
            Assert.False(successResult);
        }
    }
}
