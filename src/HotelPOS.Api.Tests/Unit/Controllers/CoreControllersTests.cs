using AutoMapper;
using HotelPOS.Api.Controllers;
using HotelPOS.Application.Common.Mappings;
using HotelPOS.Application.DTOs.Category;
using HotelPOS.Application.DTOs.Customer;
using HotelPOS.Application.DTOs.Expense;
using HotelPOS.Application.DTOs.Item;
using HotelPOS.Application.DTOs.Order;
using HotelPOS.Application.DTOs.Payroll;
using HotelPOS.Application.DTOs.Purchase;
using HotelPOS.Application.DTOs.Report;
using HotelPOS.Application.DTOs.Setting;
using HotelPOS.Application.DTOs.Supplier;
using HotelPOS.Application.DTOs.Table;
using HotelPOS.Application.DTOs.User;
using HotelPOS.Application.DTOs.CashSession;
using HotelPOS.Application.DTOs.Audit;
using HotelPOS.Application.Interfaces;
using HotelPOS.Application.UseCases.Items.Commands;
using HotelPOS.Application.UseCases.Items.Queries;
using HotelPOS.Application.UseCases.Orders.Commands;
using HotelPOS.Application.UseCases.Orders.Queries;
using HotelPOS.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace HotelPOS.Tests.Unit.Controllers
{
    public class CoreControllersTests
    {
        private static readonly IMapper Mapper = new MapperConfiguration(
            cfg => cfg.AddProfile(new MappingProfile()),
            Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance).CreateMapper();

        // ---------- CategoriesController ----------
        [Fact]
        public async Task Categories_GetCategories_ReturnsMappedDtos()
        {
            var svc = new Mock<ICategoryService>();
            svc.Setup(s => s.GetCategoriesAsync()).ReturnsAsync(new List<Category>
            {
                new Category { Id = 1, Name = "Beverages", DisplayOrder = 1 }
            });

            var controller = new CategoriesController(svc.Object, Mapper);
            var result = await controller.GetCategories();

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var dtos = Assert.IsAssignableFrom<IEnumerable<CategoryDto>>(ok.Value);
            Assert.Single(dtos);
        }

        [Fact]
        public async Task Categories_CreateCategory_InvalidState_ReturnsBadRequest()
        {
            var controller = new CategoriesController(Mock.Of<ICategoryService>(), Mapper);
            controller.ModelState.AddModelError("Name", "Required");

            var result = await controller.CreateCategory(new SaveCategoryDto { Name = "" });

            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task Categories_CreateCategory_Valid_ReturnsCreatedAtAction()
        {
            var svc = new Mock<ICategoryService>();
            svc.Setup(s => s.AddCategoryAsync("Snacks", 2)).ReturnsAsync(5);

            var controller = new CategoriesController(svc.Object, Mapper);
            var result = await controller.CreateCategory(new SaveCategoryDto { Name = "Snacks", DisplayOrder = 2 });

            var created = Assert.IsType<CreatedAtActionResult>(result.Result);
            var dto = Assert.IsType<CategoryDto>(created.Value);
            Assert.Equal(5, dto.Id);
            Assert.Equal("Snacks", dto.Name);
        }

        [Fact]
        public async Task Categories_UpdateCategory_KeyNotFound_ReturnsNotFound()
        {
            var svc = new Mock<ICategoryService>();
            svc.Setup(s => s.UpdateCategoryAsync(99, "Name", 1)).ThrowsAsync(new KeyNotFoundException());

            var controller = new CategoriesController(svc.Object, Mapper);
            var result = await controller.UpdateCategory(99, new SaveCategoryDto { Name = "Name", DisplayOrder = 1 });

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Categories_UpdateCategory_InvalidId_ReturnsBadRequest()
        {
            var controller = new CategoriesController(Mock.Of<ICategoryService>(), Mapper);
            var result = await controller.UpdateCategory(0, new SaveCategoryDto { Name = "Test" });

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Categories_DeleteCategory_InvalidId_ReturnsBadRequest()
        {
            var controller = new CategoriesController(Mock.Of<ICategoryService>(), Mapper);
            var result = await controller.DeleteCategory(-1);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        // ---------- CustomersController ----------
        [Fact]
        public async Task Customers_GetCustomers_ReturnsMappedDtos()
        {
            var svc = new Mock<ICustomerService>();
            svc.Setup(s => s.GetCustomersAsync(It.IsAny<bool>())).ReturnsAsync(new List<Customer>
            {
                new Customer { Id = 1, Name = "Alice", Phone = "1234567890" }
            });

            var controller = new CustomersController(svc.Object, Mapper);
            var result = await controller.GetCustomers();

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var dtos = Assert.IsAssignableFrom<IEnumerable<CustomerDto>>(ok.Value);
            Assert.Single(dtos);
        }

        [Fact]
        public async Task Customers_GetCustomer_InvalidId_ReturnsBadRequest()
        {
            var controller = new CustomersController(Mock.Of<ICustomerService>(), Mapper);
            var result = await controller.GetCustomer(0);
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task Customers_GetCustomer_NotFound_ReturnsNotFound()
        {
            var svc = new Mock<ICustomerService>();
            svc.Setup(s => s.GetCustomerByIdAsync(99)).ReturnsAsync((Customer?)null);

            var controller = new CustomersController(svc.Object, Mapper);
            var result = await controller.GetCustomer(99);

            Assert.IsType<NotFoundResult>(result.Result);
        }

        // ---------- ItemsController ----------
        [Fact]
        public async Task Items_GetItems_ReturnsMappedDtos()
        {
            var mediator = new Mock<IMediator>();
            mediator.Setup(m => m.Send(It.IsAny<GetItemsQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Item> { new Item { Id = 1, Name = "Coffee", Price = 50 } });

            var controller = new ItemsController(mediator.Object, Mapper);
            var result = await controller.GetItems();

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var dtos = Assert.IsAssignableFrom<IEnumerable<ItemDto>>(ok.Value);
            Assert.Single(dtos);
        }

        [Fact]
        public async Task Items_GetItem_InvalidId_ReturnsBadRequest()
        {
            var controller = new ItemsController(Mock.Of<IMediator>(), Mapper);
            var result = await controller.GetItem(-1);
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task Items_GetItem_NotFound_ReturnsNotFound()
        {
            var mediator = new Mock<IMediator>();
            mediator.Setup(m => m.Send(It.IsAny<GetItemByIdQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Item?)null);

            var controller = new ItemsController(mediator.Object, Mapper);
            var result = await controller.GetItem(999);

            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task Items_UpdateItem_KeyNotFound_ReturnsNotFound()
        {
            var mediator = new Mock<IMediator>();
            mediator.Setup(m => m.Send(It.IsAny<UpdateItemCommand>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new KeyNotFoundException());

            var controller = new ItemsController(mediator.Object, Mapper);
            var result = await controller.UpdateItem(999, new CreateItemRequest { Name = "Juice", Price = 100 });

            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task Items_DeleteItem_KeyNotFound_ReturnsNotFound()
        {
            var mediator = new Mock<IMediator>();
            mediator.Setup(m => m.Send(It.IsAny<DeleteItemCommand>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new KeyNotFoundException());

            var controller = new ItemsController(mediator.Object, Mapper);
            var result = await controller.DeleteItem(999);

            Assert.IsType<NotFoundResult>(result);
        }

        // ---------- OrdersController ----------
        [Fact]
        public async Task Orders_GetPagedOrders_ReturnsPagedResponse()
        {
            var mediator = new Mock<IMediator>();
            var userCtx = new Mock<IUserContext>();
            mediator.Setup(m => m.Send(It.IsAny<GetOrdersQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((new List<Order> { new Order { Id = 1, TotalAmount = 100 } }, 1));

            var controller = new OrdersController(mediator.Object, userCtx.Object, Mapper);
            var result = await controller.GetPagedOrders(new GetOrdersQueryRequest());

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<PagedOrdersResponse>(ok.Value);
            Assert.Equal(1, response.TotalCount);
            Assert.Single(response.Items);
        }

        [Fact]
        public async Task Orders_RefundOrder_InvalidId_ReturnsBadRequest()
        {
            var controller = new OrdersController(Mock.Of<IMediator>(), Mock.Of<IUserContext>(), Mapper);
            var result = await controller.RefundOrder(0, new RefundOrderRequest { Items = new() { new() { ItemId = 1, QuantityToRefund = 1 } }, Reason = "Wrong item" });

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Orders_RefundOrder_NoItems_ReturnsBadRequest()
        {
            var controller = new OrdersController(Mock.Of<IMediator>(), Mock.Of<IUserContext>(), Mapper);
            var result = await controller.RefundOrder(1, new RefundOrderRequest { Items = new(), Reason = "Wrong item" });

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Orders_RefundOrder_MissingReason_ReturnsBadRequest()
        {
            var controller = new OrdersController(Mock.Of<IMediator>(), Mock.Of<IUserContext>(), Mapper);
            var result = await controller.RefundOrder(1, new RefundOrderRequest { Items = new() { new() { ItemId = 1, QuantityToRefund = 1 } }, Reason = "" });

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Orders_RefundOrder_Valid_SendsCommandAndReturnsNoContent()
        {
            var mediator = new Mock<IMediator>();
            var controller = new OrdersController(mediator.Object, Mock.Of<IUserContext>(), Mapper);

            var result = await controller.RefundOrder(5, new RefundOrderRequest
            {
                Items = new() { new() { ItemId = 2, QuantityToRefund = 1 } },
                Reason = "Customer complaint"
            });

            Assert.IsType<NoContentResult>(result);
            mediator.Verify(m => m.Send(
                It.Is<RefundOrderCommand>(c => c.OrderId == 5 && c.Reason == "Customer complaint" && c.ItemsToRefund.Count == 1),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Orders_ProcessPartialPayment_InvalidId_ReturnsBadRequest()
        {
            var controller = new OrdersController(Mock.Of<IMediator>(), Mock.Of<IUserContext>(), Mapper);
            var result = await controller.ProcessPartialPayment(0, new ProcessPartialPaymentRequest { Cash = 10 });

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Orders_ProcessPartialPayment_NegativeAmount_ReturnsBadRequest()
        {
            var controller = new OrdersController(Mock.Of<IMediator>(), Mock.Of<IUserContext>(), Mapper);
            var result = await controller.ProcessPartialPayment(1, new ProcessPartialPaymentRequest { Cash = -5 });

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Orders_ProcessPartialPayment_AllZero_ReturnsBadRequest()
        {
            var controller = new OrdersController(Mock.Of<IMediator>(), Mock.Of<IUserContext>(), Mapper);
            var result = await controller.ProcessPartialPayment(1, new ProcessPartialPaymentRequest());

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Orders_ProcessPartialPayment_Valid_SendsCommandAndReturnsNoContent()
        {
            var mediator = new Mock<IMediator>();
            var controller = new OrdersController(mediator.Object, Mock.Of<IUserContext>(), Mapper);

            var result = await controller.ProcessPartialPayment(5, new ProcessPartialPaymentRequest { Cash = 50, Card = 25, Upi = 25 });

            Assert.IsType<NoContentResult>(result);
            mediator.Verify(m => m.Send(
                It.Is<ProcessPartialPaymentCommand>(c => c.OrderId == 5 && c.Cash == 50 && c.Card == 25 && c.Upi == 25),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Orders_UpdateOrder_InvalidId_ReturnsBadRequest()
        {
            var controller = new OrdersController(Mock.Of<IMediator>(), Mock.Of<IUserContext>(), Mapper);
            var result = await controller.UpdateOrder(0, new UpdateOrderRequest { Items = new() { new() { ItemId = 1, Quantity = 1 } } });

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Orders_UpdateOrder_Valid_SendsCommandAndReturnsNoContent()
        {
            var mediator = new Mock<IMediator>();
            var controller = new OrdersController(mediator.Object, Mock.Of<IUserContext>(), Mapper);

            var result = await controller.UpdateOrder(7, new UpdateOrderRequest
            {
                Items = new() { new() { ItemId = 3, Quantity = 2 } },
                TableNumber = 4,
                Discount = 5
            });

            Assert.IsType<NoContentResult>(result);
            mediator.Verify(m => m.Send(
                It.Is<UpdateOrderCommand>(c => c.Order.Id == 7 && c.Order.TableNumber == 4 && c.Order.DiscountAmount == 5 && c.Order.Items.Count == 1),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        // ---------- UsersController ----------
        [Fact]
        public async Task Users_GetUsers_ReturnsMappedUsers()
        {
            var svc = new Mock<IUserService>();
            var userCtx = new Mock<IUserContext>();
            svc.Setup(s => s.GetAllUsersAsync()).ReturnsAsync(new List<User>
            {
                new User { Id = 1, Username = "admin", Role = "Admin" }
            });

            var controller = new UsersController(svc.Object, userCtx.Object, Mapper);
            var result = await controller.GetUsers();

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var dtos = Assert.IsAssignableFrom<IEnumerable<UserDto>>(ok.Value);
            Assert.Single(dtos);
        }

        // ---------- CashSessionsController ----------
        [Fact]
        public async Task CashSessions_GetCurrentSession_NotFound_ReturnsNotFound()
        {
            var cashSvc = new Mock<ICashService>();
            var userCtx = new Mock<IUserContext>();
            cashSvc.Setup(s => s.GetCurrentSessionAsync()).ReturnsAsync((CashSession?)null);

            var controller = new CashSessionsController(cashSvc.Object, userCtx.Object, Mapper);
            var result = await controller.GetCurrentSession();

            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task CashSessions_GetCurrentSession_Found_ReturnsDto()
        {
            var cashSvc = new Mock<ICashService>();
            var userCtx = new Mock<IUserContext>();
            cashSvc.Setup(s => s.GetCurrentSessionAsync()).ReturnsAsync(new CashSession
            {
                Id = 10,
                OpeningBalance = 500,
                Status = "Open"
            });

            var controller = new CashSessionsController(cashSvc.Object, userCtx.Object, Mapper);
            var result = await controller.GetCurrentSession();

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var dto = Assert.IsType<CashSessionDto>(ok.Value);
            Assert.Equal(10, dto.Id);
            Assert.Equal(500, dto.OpeningBalance);
        }

        // ---------- PayrollController ----------
        [Fact]
        public async Task Payroll_VoidRun_InvalidId_ReturnsBadRequest()
        {
            var controller = new PayrollController(Mock.Of<IPayrollService>(), Mapper);
            var result = await controller.VoidRun(0, new VoidPayrollRunDto { Reason = "Wrong month" });

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Payroll_VoidRun_MissingReason_ReturnsBadRequest()
        {
            var controller = new PayrollController(Mock.Of<IPayrollService>(), Mapper);
            var result = await controller.VoidRun(1, new VoidPayrollRunDto { Reason = "" });

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Payroll_VoidRun_NotFound_ReturnsNotFound()
        {
            var svc = new Mock<IPayrollService>();
            svc.Setup(s => s.VoidRunAsync(99, It.IsAny<string>())).ThrowsAsync(new KeyNotFoundException());

            var controller = new PayrollController(svc.Object, Mapper);
            var result = await controller.VoidRun(99, new VoidPayrollRunDto { Reason = "Wrong month" });

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Payroll_VoidRun_AlreadyPaid_ReturnsBadRequest()
        {
            var svc = new Mock<IPayrollService>();
            svc.Setup(s => s.VoidRunAsync(1, It.IsAny<string>())).ThrowsAsync(new InvalidOperationException("Cannot void a payroll run that has already been paid."));

            var controller = new PayrollController(svc.Object, Mapper);
            var result = await controller.VoidRun(1, new VoidPayrollRunDto { Reason = "Too late" });

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Payroll_VoidRun_Valid_ReturnsNoContent()
        {
            var svc = new Mock<IPayrollService>();
            var controller = new PayrollController(svc.Object, Mapper);

            var result = await controller.VoidRun(1, new VoidPayrollRunDto { Reason = "Attendance data was wrong" });

            Assert.IsType<NoContentResult>(result);
            svc.Verify(s => s.VoidRunAsync(1, "Attendance data was wrong"), Times.Once);
        }

        // ---------- PurchasesController ----------
        [Fact]
        public async Task Purchases_GetPurchase_InvalidId_ReturnsBadRequest()
        {
            var controller = new PurchasesController(Mock.Of<IPurchaseService>(), Mapper);
            var result = await controller.GetPurchase(0);

            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task Purchases_GetPurchase_NotFound_ReturnsNotFound()
        {
            var svc = new Mock<IPurchaseService>();
            svc.Setup(s => s.GetPurchaseByIdAsync(99)).ReturnsAsync((Purchase?)null);

            var controller = new PurchasesController(svc.Object, Mapper);
            var result = await controller.GetPurchase(99);

            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task Purchases_GetPurchase_Found_ReturnsDto()
        {
            var svc = new Mock<IPurchaseService>();
            svc.Setup(s => s.GetPurchaseByIdAsync(5)).ReturnsAsync(new Purchase { Id = 5, InvoiceNumber = "INV-5" });

            var controller = new PurchasesController(svc.Object, Mapper);
            var result = await controller.GetPurchase(5);

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var dto = Assert.IsType<HotelPOS.Application.DTOs.Purchase.PurchaseDto>(ok.Value);
            Assert.Equal(5, dto.Id);
        }

        [Fact]
        public async Task Purchases_UpdatePurchase_InvalidId_ReturnsBadRequest()
        {
            var controller = new PurchasesController(Mock.Of<IPurchaseService>(), Mapper);
            var request = new HotelPOS.Application.DTOs.Purchase.SavePurchaseDto
            {
                SupplierId = 1,
                InvoiceNumber = "INV-1",
                Items = new() { new() { ItemId = 1, ItemName = "Rice", Quantity = 1, UnitPrice = 10 } }
            };

            var result = await controller.UpdatePurchase(0, request);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Purchases_UpdatePurchase_NoItems_ReturnsBadRequest()
        {
            var controller = new PurchasesController(Mock.Of<IPurchaseService>(), Mapper);
            var request = new HotelPOS.Application.DTOs.Purchase.SavePurchaseDto { SupplierId = 1, InvoiceNumber = "INV-1" };

            var result = await controller.UpdatePurchase(1, request);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Purchases_UpdatePurchase_NotFound_ReturnsNotFound()
        {
            var svc = new Mock<IPurchaseService>();
            svc.Setup(s => s.UpdatePurchaseAsync(It.IsAny<Purchase>())).ThrowsAsync(new KeyNotFoundException());

            var controller = new PurchasesController(svc.Object, Mapper);
            var request = new HotelPOS.Application.DTOs.Purchase.SavePurchaseDto
            {
                SupplierId = 1,
                InvoiceNumber = "INV-1",
                Items = new() { new() { ItemId = 1, ItemName = "Rice", Quantity = 1, UnitPrice = 10 } }
            };

            var result = await controller.UpdatePurchase(99, request);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Purchases_UpdatePurchase_Valid_ReturnsNoContent()
        {
            var svc = new Mock<IPurchaseService>();
            var controller = new PurchasesController(svc.Object, Mapper);
            var request = new HotelPOS.Application.DTOs.Purchase.SavePurchaseDto
            {
                SupplierId = 2,
                InvoiceNumber = "INV-2",
                Items = new() { new() { ItemId = 1, ItemName = "Rice", Quantity = 3, UnitPrice = 20 } }
            };

            var result = await controller.UpdatePurchase(7, request);

            Assert.IsType<NoContentResult>(result);
            svc.Verify(s => s.UpdatePurchaseAsync(It.Is<Purchase>(p => p.Id == 7 && p.SupplierId == 2)), Times.Once);
        }

        [Fact]
        public async Task Purchases_DeletePurchase_InvalidId_ReturnsBadRequest()
        {
            var controller = new PurchasesController(Mock.Of<IPurchaseService>(), Mapper);
            var result = await controller.DeletePurchase(0);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Purchases_DeletePurchase_Valid_ReturnsNoContent()
        {
            var svc = new Mock<IPurchaseService>();
            var controller = new PurchasesController(svc.Object, Mapper);

            var result = await controller.DeletePurchase(3);

            Assert.IsType<NoContentResult>(result);
            svc.Verify(s => s.DeletePurchaseAsync(3), Times.Once);
        }
    }
}
