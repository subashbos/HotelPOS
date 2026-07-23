using AutoMapper;
using HotelPOS.Api.Controllers;
using HotelPOS.Application.Common.Mappings;
using HotelPOS.Application.DTOs.Category;
using HotelPOS.Application.DTOs.Customer;
using HotelPOS.Application.DTOs.Expense;
using HotelPOS.Application.DTOs.Item;
using HotelPOS.Application.DTOs.Order;
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
    }
}
