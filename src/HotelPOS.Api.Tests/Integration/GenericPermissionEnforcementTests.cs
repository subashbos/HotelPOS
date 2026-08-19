using AutoMapper;
using HotelPOS.Application.Common.Models;
using HotelPOS.Application.DTOs.Expense;
using HotelPOS.Application.DTOs.Supplier;
using HotelPOS.Application.DTOs.Table;
using HotelPOS.Application.Interfaces;
using HotelPOS.Application.UseCases;
using HotelPOS.Application.UseCases.Categories.Commands;
using HotelPOS.Application.UseCases.Expenses.Commands;
using HotelPOS.Application.UseCases.Items.Commands;
using HotelPOS.Application.UseCases.Orders.Commands;
using HotelPOS.Application.UseCases.Purchases.Commands;
using HotelPOS.Application.UseCases.Suppliers.Commands;
using HotelPOS.Application.UseCases.Tables.Commands;
using HotelPOS.Domain.Common.Constants;
using HotelPOS.Domain.Entities;
using Moq;
using Xunit;

namespace HotelPOS.Tests.Integration
{
    /// <summary>
    /// Regression coverage for the sweep that replaced hardcoded [Authorize(Roles=...)]
    /// controller attributes with generic, data-driven PermissionModules.EnsurePermission
    /// checks at the service/handler layer. Each case proves the check exists (denies without
    /// the grant, throws before touching the repository) and, where the module choice was
    /// non-obvious (e.g. distinguishing Customers from CustomerManagement, or Orders from
    /// Billing to avoid handing Cashiers void/refund rights they don't have), that the correct
    /// module is checked.
    /// </summary>
    public class GenericPermissionEnforcementTests
    {
        private static IMapper CreateMapper() => new MapperConfiguration(
            cfg => cfg.AddProfile(new HotelPOS.Application.Common.Mappings.MappingProfile()),
            Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance).CreateMapper();

        // ---------- Categories ----------

        [Fact]
        public async Task CreateCategory_WhenUnauthorized_ThrowsAndDoesNotPersist()
        {
            var repo = new Mock<ICategoryRepository>();
            var handler = new CreateCategoryCommandHandler(repo.Object, TestAuthorization.DenyAll().Object);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                handler.Handle(new CreateCategoryCommand("Beverages"), CancellationToken.None));

            repo.Verify(r => r.AddAsync(It.IsAny<Category>()), Times.Never);
        }

        [Fact]
        public async Task UpdateCategory_WhenUnauthorized_Throws()
        {
            var repo = new Mock<ICategoryRepository>();
            var handler = new UpdateCategoryCommandHandler(repo.Object, TestAuthorization.DenyAll().Object);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                handler.Handle(new UpdateCategoryCommand(1, "Beverages"), CancellationToken.None));
        }

        [Fact]
        public async Task DeleteCategory_WhenUnauthorized_Throws()
        {
            var repo = new Mock<ICategoryRepository>();
            var itemRepo = new Mock<IItemRepository>();
            var handler = new DeleteCategoryCommandHandler(repo.Object, itemRepo.Object, TestAuthorization.DenyAll().Object);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                handler.Handle(new DeleteCategoryCommand(1), CancellationToken.None));

            repo.Verify(r => r.DeleteAsync(It.IsAny<int>()), Times.Never);
        }

        // ---------- Tables ----------

        [Fact]
        public async Task CreateTable_WhenUnauthorized_ThrowsAndDoesNotPersist()
        {
            var repo = new Mock<ITableRepository>();
            var handler = new CreateTableCommandHandler(repo.Object, CreateMapper(), TestAuthorization.DenyAll().Object);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                handler.Handle(new CreateTableCommand(new CreateTableDto { Number = 1 }), CancellationToken.None));

            repo.Verify(r => r.AddAsync(It.IsAny<Table>()), Times.Never);
        }

        [Fact]
        public async Task UpdateTable_WhenUnauthorized_Throws()
        {
            var repo = new Mock<ITableRepository>();
            var handler = new UpdateTableCommandHandler(repo.Object, CreateMapper(), TestAuthorization.DenyAll().Object);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                handler.Handle(new UpdateTableCommand(1, new CreateTableDto { Number = 1 }), CancellationToken.None));
        }

        [Fact]
        public async Task DeleteTable_WhenUnauthorized_Throws()
        {
            var repo = new Mock<ITableRepository>();
            var handler = new DeleteTableCommandHandler(repo.Object, TestAuthorization.DenyAll().Object);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                handler.Handle(new DeleteTableCommand(1), CancellationToken.None));

            repo.Verify(r => r.DeleteAsync(It.IsAny<int>()), Times.Never);
        }

        // ---------- Units ----------

        [Fact]
        public async Task CreateUnit_WhenUnauthorized_ThrowsAndDoesNotPersist()
        {
            var repo = new Mock<IUnitOfMeasurementRepository>();
            var handler = new HotelPOS.Application.UseCases.UnitOfMeasurements.Commands.CreateUnitOfMeasurementCommandHandler(
                repo.Object, TestAuthorization.DenyAll().Object);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                handler.Handle(new HotelPOS.Application.UseCases.UnitOfMeasurements.Commands.CreateUnitOfMeasurementCommand("Kg"), CancellationToken.None));

            repo.Verify(r => r.AddAsync(It.IsAny<UnitOfMeasurement>()), Times.Never);
        }

        // ---------- Expenses ----------

        [Fact]
        public async Task SaveExpense_WhenUnauthorized_ThrowsAndDoesNotPersist()
        {
            var repo = new Mock<IExpenseRepository>();
            var handler = new SaveExpenseCommandHandler(repo.Object, TestAuthorization.DenyAll().Object);
            var dto = new SaveExpenseDto { Title = "X", Category = "General", Amount = 10, Date = DateTime.Today };

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                handler.Handle(new SaveExpenseCommand(dto), CancellationToken.None));

            repo.Verify(r => r.AddAsync(It.IsAny<Expense>()), Times.Never);
        }

        [Fact]
        public async Task DeleteExpense_WhenUnauthorized_Throws()
        {
            var repo = new Mock<IExpenseRepository>();
            var handler = new DeleteExpenseCommandHandler(repo.Object, TestAuthorization.DenyAll().Object);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                handler.Handle(new DeleteExpenseCommand(1), CancellationToken.None));

            repo.Verify(r => r.DeleteAsync(It.IsAny<int>()), Times.Never);
        }

        // ---------- Purchases & Suppliers (share the "Purchase" module) ----------

        [Fact]
        public async Task SavePurchase_WhenUnauthorized_ThrowsAndDoesNotPersist()
        {
            var purchaseRepo = new Mock<IPurchaseRepository>();
            var itemRepo = new Mock<IItemRepository>();
            var handler = new SavePurchaseCommandHandler(purchaseRepo.Object, itemRepo.Object, TestAuthorization.DenyAll().Object);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                handler.Handle(new SavePurchaseCommand(new Purchase { PurchaseItems = new List<PurchaseItem>() }), CancellationToken.None));

            purchaseRepo.Verify(r => r.AddAsync(It.IsAny<Purchase>()), Times.Never);
        }

        [Fact]
        public async Task DeletePurchase_WhenUnauthorized_Throws()
        {
            var purchaseRepo = new Mock<IPurchaseRepository>();
            var itemRepo = new Mock<IItemRepository>();
            var handler = new DeletePurchaseCommandHandler(purchaseRepo.Object, itemRepo.Object, TestAuthorization.DenyAll().Object);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                handler.Handle(new DeletePurchaseCommand(1), CancellationToken.None));

            purchaseRepo.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task SaveSupplier_WhenUnauthorized_ThrowsAndDoesNotPersist()
        {
            var repo = new Mock<ISupplierRepository>();
            var handler = new SaveSupplierCommandHandler(repo.Object, TestAuthorization.DenyAll().Object);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                handler.Handle(new SaveSupplierCommand(new SaveSupplierDto { Name = "Acme" }), CancellationToken.None));

            repo.Verify(r => r.AddAsync(It.IsAny<Supplier>()), Times.Never);
        }

        [Fact]
        public async Task DeleteSupplier_WhenUnauthorized_Throws()
        {
            var repo = new Mock<ISupplierRepository>();
            var handler = new DeleteSupplierCommandHandler(repo.Object, TestAuthorization.DenyAll().Object);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                handler.Handle(new DeleteSupplierCommand(1), CancellationToken.None));

            repo.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task SavePurchase_WhenGrantedPurchaseModule_ChecksPurchaseNotSuppliersOrAnyOtherModule()
        {
            var purchaseRepo = new Mock<IPurchaseRepository>();
            var itemRepo = new Mock<IItemRepository>();
            itemRepo.Setup(r => r.GetByIdsAsync(It.IsAny<List<int>>())).ReturnsAsync(new List<Item>());
            var auth = new Mock<IAuthorizationService>();
            var handler = new SavePurchaseCommandHandler(purchaseRepo.Object, itemRepo.Object, auth.Object);

            await handler.Handle(new SavePurchaseCommand(new Purchase { PurchaseItems = new List<PurchaseItem>() }), CancellationToken.None);

            auth.Verify(a => a.EnsureEditPermission(PermissionModules.Purchase), Times.Once);
        }

        // ---------- Items ----------

        [Fact]
        public async Task CreateItem_WhenUnauthorized_ThrowsAndDoesNotPersist()
        {
            var repo = new Mock<IItemRepository>();
            var handler = new CreateItemCommandHandler(repo.Object, TestAuthorization.DenyAll().Object);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                handler.Handle(new CreateItemCommand("Coffee", 10, 5, null, null, null, 0, false, 1), CancellationToken.None));

            repo.Verify(r => r.AddAsync(It.IsAny<Item>()), Times.Never);
        }

        [Fact]
        public async Task DeleteItem_WhenUnauthorized_Throws()
        {
            var repo = new Mock<IItemRepository>();
            var handler = new DeleteItemCommandHandler(repo.Object, TestAuthorization.DenyAll().Object);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                handler.Handle(new DeleteItemCommand(1), CancellationToken.None));

            repo.Verify(r => r.DeleteAsync(It.IsAny<int>()), Times.Never);
        }

        // ---------- Orders: OrderManagement, deliberately distinct from Billing ----------
        // Cashiers hold Billing (for creating orders / recording payment) but must NOT be able
        // to void, refund, or edit an already-placed order - that stayed Admin/Manager-only.

        [Fact]
        public async Task VoidOrder_WhenUnauthorized_ThrowsAndDoesNotVoid()
        {
            var orderService = new Mock<IOrderService>();
            var handler = new VoidOrderCommandHandler(orderService.Object, TestAuthorization.DenyAll().Object);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                handler.Handle(new VoidOrderCommand(1, "reason", "user"), CancellationToken.None));

            orderService.Verify(s => s.VoidOrderInternalAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task RefundOrder_WhenUnauthorized_Throws()
        {
            var orderService = new Mock<IOrderService>();
            var handler = new RefundOrderCommandHandler(orderService.Object, TestAuthorization.DenyAll().Object);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                handler.Handle(new RefundOrderCommand(1, new List<OrderItemRefundDto>(), "reason"), CancellationToken.None));
        }

        [Fact]
        public async Task UpdateOrder_WhenUnauthorized_Throws()
        {
            var orderService = new Mock<IOrderService>();
            var handler = new UpdateOrderCommandHandler(orderService.Object, TestAuthorization.DenyAll().Object);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                handler.Handle(new UpdateOrderCommand(new Order { Id = 1 }), CancellationToken.None));
        }

        [Fact]
        public async Task VoidOrder_WhenGrantedBillingOnly_StillThrows_BecauseOrderManagementIsARequiredSeparateGrant()
        {
            // Cashiers are granted Billing by default; VoidOrder must check OrderManagement
            // specifically, or a Cashier would silently gain void rights through Billing alone.
            var orderService = new Mock<IOrderService>();
            var auth = new Mock<IAuthorizationService>();
            auth.Setup(a => a.EnsureEditPermission(PermissionModules.Billing));
            auth.Setup(a => a.EnsureEditPermission(PermissionModules.OrderManagement))
                .Throws(new UnauthorizedAccessException("Access denied."));
            var handler = new VoidOrderCommandHandler(orderService.Object, auth.Object);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                handler.Handle(new VoidOrderCommand(1, "reason", "user"), CancellationToken.None));

            auth.Verify(a => a.EnsureEditPermission(PermissionModules.OrderManagement), Times.Once);
        }

        // ---------- OrderService itself: the WPF desktop client calls IOrderService directly
        // (in-process, no HTTP/MediatR involved), never through VoidOrderCommandHandler etc.
        // above. Those handlers alone left VoidOrderAsync/RefundOrderAsync/UpdateOrderAsync/
        // DeleteOrderAsync completely unguarded for that caller - the checks below close that gap
        // at the one place both callers (API-via-handler and WPF-direct) actually converge.

        [Fact]
        public async Task OrderServiceVoidOrder_WhenUnauthorized_ThrowsAndDoesNotVoid()
        {
            var repo = new Mock<IOrderRepository>();
            var service = new OrderService(repo.Object, null, new Mock<IItemService>().Object, TestCashService.WithOpenSession().Object, TestAuthorization.DenyAll().Object);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                service.VoidOrderAsync(1, "reason", "user"));

            repo.Verify(r => r.GetByIdWithItemsAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task OrderServiceRefundOrder_WhenUnauthorized_Throws()
        {
            var repo = new Mock<IOrderRepository>();
            var service = new OrderService(repo.Object, null, new Mock<IItemService>().Object, TestCashService.WithOpenSession().Object, TestAuthorization.DenyAll().Object);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                service.RefundOrderAsync(1, new List<OrderItemRefundDto> { new(1, 1) }, "reason"));

            repo.Verify(r => r.GetByIdWithItemsAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task OrderServiceUpdateOrder_WhenUnauthorized_Throws()
        {
            var repo = new Mock<IOrderRepository>();
            var service = new OrderService(repo.Object, null, new Mock<IItemService>().Object, TestCashService.WithOpenSession().Object, TestAuthorization.DenyAll().Object);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                service.UpdateOrderAsync(new Order { Id = 1, Items = new List<OrderItem> { new() { ItemId = 1, Quantity = 1 } } }));

            repo.Verify(r => r.GetByIdWithItemsAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task OrderServiceDeleteOrder_WhenUnauthorized_Throws()
        {
            var repo = new Mock<IOrderRepository>();
            var service = new OrderService(repo.Object, null, new Mock<IItemService>().Object, TestCashService.WithOpenSession().Object, TestAuthorization.DenyAll().Object);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                service.DeleteOrderAsync(1));

            repo.Verify(r => r.GetByIdWithItemsAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task OrderServiceVoidOrder_WhenGrantedOrderManagement_Voids()
        {
            var repo = new Mock<IOrderRepository>();
            var order = new Order { Id = 1, Status = OrderStatuses.Paid, Items = new List<OrderItem>() };
            repo.Setup(r => r.GetByIdWithItemsAsync(1)).ReturnsAsync(order);
            var auth = new Mock<IAuthorizationService>();
            var service = new OrderService(repo.Object, null, new Mock<IItemService>().Object, TestCashService.WithOpenSession().Object, auth.Object);

            await service.VoidOrderAsync(1, "reason", "user");

            auth.Verify(a => a.EnsureEditPermission(PermissionModules.OrderManagement), Times.Once);
            Assert.Equal(OrderStatuses.Void, order.Status);
        }

        [Fact]
        public async Task OrderServiceSaveOrder_WhenUnauthorized_ThrowsAndDoesNotPersist()
        {
            var repo = new Mock<IOrderRepository>();
            var itemService = new Mock<IItemService>();
            var service = new OrderService(repo.Object, null, itemService.Object, TestCashService.WithOpenSession().Object, TestAuthorization.DenyAll().Object);
            var request = new SaveOrderRequest(
                new List<OrderItem> { new() { ItemId = 1, Quantity = 1 } }, 1);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                service.SaveOrderAsync(request));

            itemService.Verify(s => s.GetItemsByIdsAsync(It.IsAny<List<int>>()), Times.Never);
            repo.Verify(r => r.AddAsync(It.IsAny<Order>()), Times.Never);
        }

        [Fact]
        public async Task OrderServiceProcessPartialPayment_WhenUnauthorized_ThrowsAndDoesNotPay()
        {
            var repo = new Mock<IOrderRepository>();
            var service = new OrderService(repo.Object, null, new Mock<IItemService>().Object, TestCashService.WithOpenSession().Object, TestAuthorization.DenyAll().Object);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                service.ProcessPartialPaymentAsync(1, 10, 0, 0));

            repo.Verify(r => r.GetByIdWithItemsAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task OrderServiceSaveOrder_WhenGrantedBilling_Saves()
        {
            var repo = new Mock<IOrderRepository>();
            repo.Setup(r => r.GetNextInvoiceNumberAsync(It.IsAny<string>())).ReturnsAsync("INV-001");
            repo.Setup(r => r.AddAsync(It.IsAny<Order>())).ReturnsAsync(1);
            var itemService = new Mock<IItemService>();
            itemService.Setup(s => s.GetItemsByIdsAsync(It.IsAny<List<int>>()))
                .ReturnsAsync((List<int> ids) => ids.Select(id => new Item { Id = id, Name = "Coffee", Price = 50m }).ToList());
            var auth = new Mock<IAuthorizationService>();
            var service = new OrderService(repo.Object, null, itemService.Object, TestCashService.WithOpenSession().Object, auth.Object);
            var request = new SaveOrderRequest(
                new List<OrderItem> { new() { ItemId = 1, Quantity = 1 } }, 1);

            await service.SaveOrderAsync(request);

            auth.Verify(a => a.EnsureEditPermission(PermissionModules.Billing), Times.Once);
        }

        // ---------- Customers: CustomerManagement (delete) distinct from Customers (view/save) ----------
        // Cashiers hold Customers by default; DeleteCustomer must check CustomerManagement
        // specifically, or a Cashier would silently gain delete rights through Customers alone.

        [Fact]
        public async Task SaveCustomer_WhenUnauthorized_Throws()
        {
            var repo = new Mock<ICustomerRepository>();
            var orderRepo = new Mock<IOrderRepository>();
            var service = new CustomerService(repo.Object, orderRepo.Object, TestAuthorization.DenyAll().Object);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                service.SaveCustomerAsync(new Customer { Name = "Asha" }));

            repo.Verify(r => r.AddAsync(It.IsAny<Customer>()), Times.Never);
        }

        [Fact]
        public async Task DeleteCustomer_WhenGrantedCustomersButNotCustomerManagement_StillThrows()
        {
            var repo = new Mock<ICustomerRepository>();
            var orderRepo = new Mock<IOrderRepository>();
            var auth = new Mock<IAuthorizationService>();
            auth.Setup(a => a.EnsureEditPermission(PermissionModules.Customers));
            auth.Setup(a => a.EnsureDeletePermission(PermissionModules.CustomerManagement))
                .Throws(new UnauthorizedAccessException("Access denied."));
            var service = new CustomerService(repo.Object, orderRepo.Object, auth.Object);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.DeleteCustomerAsync(1));

            auth.Verify(a => a.EnsureDeletePermission(PermissionModules.CustomerManagement), Times.Once);
            repo.Verify(r => r.DeactivateAsync(It.IsAny<int>()), Times.Never);
        }

        // ---------- Tds ----------

        [Fact]
        public async Task Tds_AllOperations_WhenUnauthorized_Throw()
        {
            var repo = new Mock<ITdsRepository>();
            var service = new TdsService(repo.Object, TestAuthorization.DenyAll().Object);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.GetFinancialYearsAsync());
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.GetRuleSetAsync(2025));
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.SaveRuleSetAsync(new TdsRuleSet
            {
                Config = new TdsConfig(),
                Slabs = new List<TdsSlab> { new() { IncomeFrom = 0 } }
            }));
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.DeleteRuleSetAsync(2025));

            repo.Verify(r => r.SaveRuleSetAsync(It.IsAny<TdsRuleSet>()), Times.Never);
        }

        // ---------- Audit ----------

        [Fact]
        public async Task GetAuditLogs_WhenUnauthorized_Throws()
        {
            var mediator = new Mock<MediatR.IMediator>();
            var service = new AuditService(mediator.Object, TestAuthorization.DenyAll().Object);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.GetLogsAsync());

            mediator.Verify(m => m.Send(It.IsAny<MediatR.IRequest<List<HotelPOS.Application.DTOs.Audit.AuditLogDto>>>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        // ---------- Gap fills: previously protected only by the removed controller attribute ----------

        [Fact]
        public async Task MarkAttendance_WhenUnauthorized_ThrowsAndDoesNotPersist()
        {
            var repo = new Mock<IAttendanceRepository>();
            var service = new AttendanceService(repo.Object, TestAuthorization.DenyAll().Object);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                service.MarkAttendanceAsync(new Attendance { EmployeeId = 1, Status = AttendanceStatuses.Present }));

            repo.Verify(r => r.AddAsync(It.IsAny<Attendance>()), Times.Never);
        }

        [Fact]
        public async Task DeleteAttendance_WhenUnauthorized_Throws()
        {
            var repo = new Mock<IAttendanceRepository>();
            var service = new AttendanceService(repo.Object, TestAuthorization.DenyAll().Object);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.DeleteAttendanceAsync(1));

            repo.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task SaveEmployee_WhenUnauthorized_ThrowsAndDoesNotPersist()
        {
            var repo = new Mock<IEmployeeRepository>();
            var service = new EmployeeService(repo.Object, TestAuthorization.DenyAll().Object);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                service.SaveEmployeeAsync(new Employee { FirstName = "A", LastName = "B" }));

            repo.Verify(r => r.AddAsync(It.IsAny<Employee>()), Times.Never);
        }

        [Fact]
        public async Task DeleteEmployee_WhenUnauthorized_Throws()
        {
            var repo = new Mock<IEmployeeRepository>();
            var service = new EmployeeService(repo.Object, TestAuthorization.DenyAll().Object);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.DeleteEmployeeAsync(1));

            repo.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task ApproveLeave_WhenUnauthorized_ThrowsAndDoesNotLoadRequest()
        {
            var repo = new Mock<ILeaveRepository>();
            var employeeRepo = new Mock<IEmployeeRepository>();
            var service = new LeaveService(repo.Object, employeeRepo.Object, TestAuthorization.DenyAll().Object);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.ApproveLeaveAsync(1, 2));

            repo.Verify(r => r.GetRequestByIdAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task RejectLeave_WhenUnauthorized_Throws()
        {
            var repo = new Mock<ILeaveRepository>();
            var employeeRepo = new Mock<IEmployeeRepository>();
            var service = new LeaveService(repo.Object, employeeRepo.Object, TestAuthorization.DenyAll().Object);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.RejectLeaveAsync(1, 2, "reason"));

            repo.Verify(r => r.GetRequestByIdAsync(It.IsAny<int>()), Times.Never);
        }
    }
}
