using AutoMapper;
using HotelPOS.Application.DTOs.Expense;
using HotelPOS.Application.UseCases;
using HotelPOS.Application.UseCases.Expenses.Commands;
using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Entities;
using HotelPOS.Domain.Events;
using MediatR;
using Moq;
using System;
using System.Collections.Generic;
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

        // ── Mediator (production/DI) path: audit event publishing ────────────
        // The legacy repository constructor above is test-only (see ExpenseService's doc
        // comment); production always resolves ExpenseService via the mediator constructor,
        // so the audit hook is only reachable there.

        [Fact]
        public async Task SaveExpenseAsync_ViaMediator_NewExpense_PublishesCreateAuditEvent()
        {
            var mediator = new Mock<IMediator>();
            var mapper = new Mock<IMapper>();
            var dto = new SaveExpenseDto { Id = 0, Title = "Vegetable Purchase", Amount = 1500 };
            mapper.Setup(m => m.Map<SaveExpenseDto>(It.IsAny<Expense>())).Returns(dto);
            mediator.Setup(m => m.Send(It.IsAny<SaveExpenseCommand>(), default)).ReturnsAsync(42);

            var service = new ExpenseService(mediator.Object, mapper.Object);
            await service.SaveExpenseAsync(new Expense { Id = 0, Title = "Vegetable Purchase", Amount = 1500 });

            mediator.Verify(m => m.Publish(It.Is<EntityActionEvent>(e =>
                e.EntityName == "Expense" && e.EntityId == 42 && e.Action == "Create"), default), Times.Once);
        }

        [Fact]
        public async Task SaveExpenseAsync_ViaMediator_ExistingExpense_PublishesUpdateAuditEvent()
        {
            var mediator = new Mock<IMediator>();
            var mapper = new Mock<IMapper>();
            var dto = new SaveExpenseDto { Id = 5, Title = "Electricity Bill", Amount = 3000 };
            mapper.Setup(m => m.Map<SaveExpenseDto>(It.IsAny<Expense>())).Returns(dto);
            mediator.Setup(m => m.Send(It.IsAny<SaveExpenseCommand>(), default)).ReturnsAsync(5);

            var service = new ExpenseService(mediator.Object, mapper.Object);
            await service.SaveExpenseAsync(new Expense { Id = 5, Title = "Electricity Bill", Amount = 3000 });

            mediator.Verify(m => m.Publish(It.Is<EntityActionEvent>(e =>
                e.EntityName == "Expense" && e.EntityId == 5 && e.Action == "Update"), default), Times.Once);
        }

        [Fact]
        public async Task DeleteExpenseAsync_ViaMediator_PublishesDeleteAuditEvent()
        {
            var mediator = new Mock<IMediator>();
            var mapper = new Mock<IMapper>();
            mediator.Setup(m => m.Send(It.IsAny<DeleteExpenseCommand>(), default)).Returns(Task.CompletedTask);

            var service = new ExpenseService(mediator.Object, mapper.Object);
            await service.DeleteExpenseAsync(7);

            mediator.Verify(m => m.Publish(It.Is<EntityActionEvent>(e =>
                e.EntityName == "Expense" && e.EntityId == 7 && e.Action == "Delete"), default), Times.Once);
        }
    }
}
