using HotelPOS.Application.DTOs.Expense;
using HotelPOS.Application.UseCases;
using HotelPOS.Application.UseCases.Expenses.Commands;
using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Entities;
using HotelPOS.Domain.Events;
using AutoMapper;
using MediatR;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace HotelPOS.Tests.Unit.Services
{
    public class ExpenseServiceTests
    {
        private readonly Mock<IExpenseRepository> _expenseRepoMock;
        private readonly ExpenseService _expenseService;

        public ExpenseServiceTests()
        {
            _expenseRepoMock = new Mock<IExpenseRepository>();
            _expenseService = new ExpenseService(_expenseRepoMock.Object);
        }

        [Fact]
        public async Task SaveExpenseAsync_ValidNewExpense_ShouldSaveSuccessfully()
        {
            // Arrange
            var expense = new Expense
            {
                Id = 0,
                Title = "Vegetable Purchase",
                Category = "Raw Material",
                Amount = 1500
            };

            // Act
            await _expenseService.SaveExpenseAsync(expense);

            // Assert
            _expenseRepoMock.Verify(r => r.AddAsync(expense), Times.Once);
            Assert.Equal("Vegetable Purchase", expense.Title);
        }

        [Fact]
        public async Task SaveExpenseAsync_ExistingExpense_ShouldUpdate()
        {
            // Arrange
            var expense = new Expense
            {
                Id = 5,
                Title = "Electricity Bill",
                Category = "Utilities",
                Amount = 3000
            };

            // Act
            await _expenseService.SaveExpenseAsync(expense);

            // Assert
            _expenseRepoMock.Verify(r => r.UpdateAsync(expense), Times.Once);
        }

        [Fact]
        public async Task SaveExpenseAsync_EmptyTitle_ShouldThrowArgumentException()
        {
            // Arrange
            var expense = new Expense { Title = "", Category = "General", Amount = 100 };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(() => _expenseService.SaveExpenseAsync(expense));
            Assert.Contains("Title is required.", ex.Message);
        }

        [Fact]
        public async Task SaveExpenseAsync_EmptyCategory_ShouldThrowArgumentException()
        {
            // Arrange
            var expense = new Expense { Title = "Misc", Category = "", Amount = 100 };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(() => _expenseService.SaveExpenseAsync(expense));
            Assert.Contains("Category is required.", ex.Message);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-50)]
        public async Task SaveExpenseAsync_NonPositiveAmount_ShouldThrowArgumentException(decimal amount)
        {
            // Arrange
            var expense = new Expense { Title = "Misc", Category = "General", Amount = amount };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(() => _expenseService.SaveExpenseAsync(expense));
            Assert.Contains("Amount must be greater than zero.", ex.Message);
        }

        [Fact]
        public async Task GetExpensesAsync_NullFromRepo_ReturnsEmptyList()
        {
            // Arrange
            _expenseRepoMock.Setup(r => r.GetAllAsync(null, null)).ReturnsAsync((List<Expense>)null!);

            // Act
            var result = await _expenseService.GetExpensesAsync(null, null);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task DeleteExpenseAsync_NotFound_ThrowsKeyNotFoundException()
        {
            // Arrange
            _expenseRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Expense?)null);

            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(
                () => _expenseService.DeleteExpenseAsync(999));
        }

        [Fact]
        public async Task DeleteExpenseAsync_Found_DeletesSuccessfully()
        {
            // Arrange
            var expense = new Expense { Id = 7, Title = "Rent", Category = "Rent", Amount = 20000 };
            _expenseRepoMock.Setup(r => r.GetByIdAsync(7)).ReturnsAsync(expense);

            // Act
            await _expenseService.DeleteExpenseAsync(7);

            // Assert
            _expenseRepoMock.Verify(r => r.DeleteAsync(7), Times.Once);
        }

        // ── Audit trail: mediator path publishes EntityActionEvent ──────────

        [Fact]
        public async Task SaveExpenseAsync_MediatorPath_PublishesCreateEvent()
        {
            var mediatorMock = new Mock<IMediator>();
            var mapperMock = new Mock<IMapper>();
            mapperMock.Setup(m => m.Map<SaveExpenseDto>(It.IsAny<Expense>()))
                .Returns(new SaveExpenseDto { Title = "Veg", Category = "Raw Material", Amount = 1500 });
            mediatorMock.Setup(m => m.Send(It.IsAny<SaveExpenseCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(77);

            var service = new ExpenseService(mediatorMock.Object, mapperMock.Object);
            var expense = new Expense { Id = 0, Title = "Veg", Category = "Raw Material", Amount = 1500 };

            await service.SaveExpenseAsync(expense);

            mediatorMock.Verify(
                m => m.Publish(It.Is<EntityActionEvent>(e => e.EntityName == "Expense" && e.EntityId == 77 && e.Action == "Create"), default),
                Times.Once);
        }

        [Fact]
        public async Task DeleteExpenseAsync_MediatorPath_PublishesDeleteEvent()
        {
            var mediatorMock = new Mock<IMediator>();
            var mapperMock = new Mock<IMapper>();

            var service = new ExpenseService(mediatorMock.Object, mapperMock.Object);

            await service.DeleteExpenseAsync(42);

            mediatorMock.Verify(
                m => m.Publish(It.Is<EntityActionEvent>(e => e.EntityName == "Expense" && e.EntityId == 42 && e.Action == "Delete"), default),
                Times.Once);
        }
    }
}
