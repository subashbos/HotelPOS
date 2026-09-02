using HotelPOS.Application.DTOs.Report;
using HotelPOS.Application.Interfaces;
using HotelPOS.Application.UseCases.CashSessions.Queries;
using HotelPOS.Application.UseCases.Categories.Queries;
using HotelPOS.Application.UseCases.Expenses.Queries;
using HotelPOS.Application.UseCases.Purchases.Queries;
using HotelPOS.Application.UseCases.Reports.Queries;
using HotelPOS.Application.UseCases.Roles.Queries;
using HotelPOS.Application.UseCases.Suppliers.Queries;
using HotelPOS.Application.UseCases.Tables.Queries;
using HotelPOS.Domain.Entities;
using Moq;
using Xunit;

namespace HotelPOS.Tests
{
    public class QueryHandlerTests
    {
        [Fact]
        public async Task GetCurrentSessionQueryHandler_DelegatesToRepository()
        {
            var repo = new Mock<ICashRepository>();
            var session = new CashSession { Id = 1, OpeningBalance = 500m };
            repo.Setup(r => r.GetCurrentSessionAsync()).ReturnsAsync(session);
            var handler = new GetCurrentSessionQueryHandler(repo.Object);

            var result = await handler.Handle(new GetCurrentSessionQuery(), CancellationToken.None);

            Assert.Same(session, result);
        }

        [Fact]
        public async Task GetCurrentSessionQueryHandler_NoOpenSession_ReturnsNull()
        {
            var repo = new Mock<ICashRepository>();
            repo.Setup(r => r.GetCurrentSessionAsync()).ReturnsAsync((CashSession?)null);
            var handler = new GetCurrentSessionQueryHandler(repo.Object);

            var result = await handler.Handle(new GetCurrentSessionQuery(), CancellationToken.None);

            Assert.Null(result);
        }

        [Fact]
        public async Task GetSessionHistoryQueryHandler_DelegatesCountToRepository()
        {
            var repo = new Mock<ICashRepository>();
            var history = new List<CashSession> { new CashSession { Id = 1 }, new CashSession { Id = 2 } };
            repo.Setup(r => r.GetHistoryAsync(10)).ReturnsAsync(history);
            var handler = new GetSessionHistoryQueryHandler(repo.Object);

            var result = await handler.Handle(new GetSessionHistoryQuery(10), CancellationToken.None);

            Assert.Same(history, result);
            repo.Verify(r => r.GetHistoryAsync(10), Times.Once);
        }

        [Fact]
        public async Task GetSalesReportQueryHandler_DelegatesToReportService()
        {
            var service = new Mock<IReportService>();
            var from = new DateTime(2026, 1, 1);
            var to = new DateTime(2026, 1, 31);
            var dto = new SalesReportDto { TotalRevenue = 1000m };
            service.Setup(s => s.GetSalesReportInternalAsync(from, to)).ReturnsAsync(dto);
            var handler = new GetSalesReportQueryHandler(service.Object);

            var result = await handler.Handle(new GetSalesReportQuery(from, to), CancellationToken.None);

            Assert.Same(dto, result);
        }

        [Fact]
        public async Task GetCategoriesQueryHandler_DelegatesToRepository()
        {
            var repo = new Mock<ICategoryRepository>();
            var categories = new List<Category> { new Category { Id = 1, Name = "Food" } };
            repo.Setup(r => r.GetAllAsync()).ReturnsAsync(categories);
            var handler = new GetCategoriesQueryHandler(repo.Object);

            var result = await handler.Handle(new GetCategoriesQuery(), CancellationToken.None);

            Assert.Same(categories, result);
        }

        [Fact]
        public async Task GetCategoriesQueryHandler_NullFromRepo_ReturnsEmptyList()
        {
            var repo = new Mock<ICategoryRepository>();
            repo.Setup(r => r.GetAllAsync()).ReturnsAsync((List<Category>)null!);
            var handler = new GetCategoriesQueryHandler(repo.Object);

            var result = await handler.Handle(new GetCategoriesQuery(), CancellationToken.None);

            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetTablesQueryHandler_DelegatesToRepository()
        {
            var repo = new Mock<ITableRepository>();
            var tables = new List<Table> { new Table { Id = 1, Number = 1, Name = "T1" } };
            repo.Setup(r => r.GetAllAsync()).ReturnsAsync(tables);
            var handler = new GetTablesQueryHandler(repo.Object);

            var result = await handler.Handle(new GetTablesQuery(), CancellationToken.None);

            Assert.Same(tables, result);
        }

        [Fact]
        public async Task GetTablesQueryHandler_NullFromRepo_ReturnsEmptyList()
        {
            var repo = new Mock<ITableRepository>();
            repo.Setup(r => r.GetAllAsync()).ReturnsAsync((List<Table>)null!);
            var handler = new GetTablesQueryHandler(repo.Object);

            var result = await handler.Handle(new GetTablesQuery(), CancellationToken.None);

            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetAllRolesQueryHandler_DelegatesToRepository()
        {
            var repo = new Mock<IRoleRepository>();
            var roles = new List<Role> { new Role { Id = 1, Name = "Admin" } };
            repo.Setup(r => r.GetAllRolesAsync()).ReturnsAsync(roles);
            var handler = new GetAllRolesQueryHandler(repo.Object);

            var result = await handler.Handle(new GetAllRolesQuery(), CancellationToken.None);

            Assert.Same(roles, result);
        }

        [Fact]
        public async Task GetAllSuppliersQueryHandler_DelegatesToRepository()
        {
            var repo = new Mock<ISupplierRepository>();
            var suppliers = new List<Supplier> { new Supplier { Id = 1, Name = "Acme" } };
            repo.Setup(r => r.GetAllAsync()).ReturnsAsync(suppliers);
            var handler = new GetAllSuppliersQueryHandler(repo.Object);

            var result = await handler.Handle(new GetAllSuppliersQuery(), CancellationToken.None);

            Assert.Same(suppliers, result);
        }

        [Fact]
        public async Task GetAllSuppliersQueryHandler_NullFromRepo_ReturnsEmptyList()
        {
            var repo = new Mock<ISupplierRepository>();
            repo.Setup(r => r.GetAllAsync()).ReturnsAsync((List<Supplier>)null!);
            var handler = new GetAllSuppliersQueryHandler(repo.Object);

            var result = await handler.Handle(new GetAllSuppliersQuery(), CancellationToken.None);

            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetExpensesQueryHandler_DelegatesDateRangeToRepository()
        {
            var repo = new Mock<IExpenseRepository>();
            var from = new DateTime(2026, 1, 1);
            var to = new DateTime(2026, 1, 31);
            var expenses = new List<Expense> { new Expense { Id = 1 } };
            repo.Setup(r => r.GetAllAsync(from, to)).ReturnsAsync(expenses);
            var handler = new GetExpensesQueryHandler(repo.Object);

            var result = await handler.Handle(new GetExpensesQuery(from, to), CancellationToken.None);

            Assert.Same(expenses, result);
        }

        [Fact]
        public async Task GetExpensesQueryHandler_NullFromRepo_ReturnsEmptyList()
        {
            var repo = new Mock<IExpenseRepository>();
            repo.Setup(r => r.GetAllAsync(null, null)).ReturnsAsync((List<Expense>)null!);
            var handler = new GetExpensesQueryHandler(repo.Object);

            var result = await handler.Handle(new GetExpensesQuery(null, null), CancellationToken.None);

            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetPagedPurchasesQueryHandler_DelegatesToRepository()
        {
            var repo = new Mock<IPurchaseRepository>();
            var purchases = new List<Purchase> { new Purchase { Id = 1 } };
            repo.Setup(r => r.GetPagedPurchasesAsync(1, 20, null)).ReturnsAsync((purchases, 1));
            var handler = new GetPagedPurchasesQueryHandler(repo.Object);

            var result = await handler.Handle(new GetPagedPurchasesQuery(1, 20), CancellationToken.None);

            Assert.Same(purchases, result.purchases);
            Assert.Equal(1, result.totalCount);
        }
    }
}
