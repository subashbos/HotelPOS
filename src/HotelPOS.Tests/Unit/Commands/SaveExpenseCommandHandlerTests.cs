using HotelPOS.Application.DTOs.Expense;
using HotelPOS.Application.Interfaces;
using HotelPOS.Application.UseCases.Expenses.Commands;
using HotelPOS.Domain.Entities;
using Moq;
using Xunit;

namespace HotelPOS.Tests
{
    public class SaveExpenseCommandHandlerTests
    {
        private readonly Mock<IExpenseRepository> _expenseRepoMock = new();
        private readonly SaveExpenseCommandHandler _handler;

        public SaveExpenseCommandHandlerTests()
        {
            _handler = new SaveExpenseCommandHandler(_expenseRepoMock.Object);
        }

        [Fact]
        public async Task Handle_NewExpense_AddsToRepository()
        {
            var dto = new SaveExpenseDto
            {
                Id = 0,
                Title = "  Vegetable Purchase  ",
                Category = "Raw Material",
                Amount = 1500,
                Date = new DateTime(2026, 7, 12),
                PaymentMode = "Cash"
            };

            var result = await _handler.Handle(new SaveExpenseCommand(dto), CancellationToken.None);

            _expenseRepoMock.Verify(r => r.AddAsync(It.Is<Expense>(e =>
                e.Title == "Vegetable Purchase" &&
                e.Category == "Raw Material" &&
                e.Amount == 1500)), Times.Once);
            _expenseRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Expense>()), Times.Never);
        }

        [Fact]
        public async Task Handle_ExistingExpense_UpdatesRepository()
        {
            var existing = new Expense { Id = 5, Title = "Old Title", Category = "General", Amount = 100 };
            _expenseRepoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(existing);

            var dto = new SaveExpenseDto
            {
                Id = 5,
                Title = "Updated Title",
                Category = "Utilities",
                Amount = 250,
                Date = DateTime.Today
            };

            await _handler.Handle(new SaveExpenseCommand(dto), CancellationToken.None);

            Assert.Equal("Updated Title", existing.Title);
            Assert.Equal("Utilities", existing.Category);
            Assert.Equal(250, existing.Amount);
            _expenseRepoMock.Verify(r => r.UpdateAsync(existing), Times.Once);
            _expenseRepoMock.Verify(r => r.AddAsync(It.IsAny<Expense>()), Times.Never);
        }

        [Fact]
        public async Task Handle_ExistingExpenseNotFound_ThrowsKeyNotFoundException()
        {
            _expenseRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Expense?)null);

            var dto = new SaveExpenseDto { Id = 999, Title = "X", Category = "General", Amount = 10 };

            await Assert.ThrowsAsync<KeyNotFoundException>(
                () => _handler.Handle(new SaveExpenseCommand(dto), CancellationToken.None));
        }
    }
}
