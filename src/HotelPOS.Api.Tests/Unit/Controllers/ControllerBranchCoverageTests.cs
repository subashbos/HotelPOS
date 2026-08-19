using AutoMapper;
using HotelPOS.Api.Controllers;
using HotelPOS.Application.Common.Mappings;
using HotelPOS.Application.DTOs.Item;
using HotelPOS.Application.DTOs.Order;
using HotelPOS.Application.Interfaces;
using HotelPOS.Application.UseCases.Items.Commands;
using HotelPOS.Application.UseCases.Orders.Commands;
using HotelPOS.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace HotelPOS.Tests.Unit.Controllers
{
    /// <summary>
    /// Fills in the remaining branch-coverage gaps on otherwise-covered controllers:
    /// ItemsController's UpdateItem/DeleteItem success and error branches, OrdersController's
    /// CreateOrder success path, and CategoriesController's DeleteCategory success/conflict branches.
    /// </summary>
    public class ControllerBranchCoverageTests
    {
        private static readonly IMapper Mapper = new MapperConfiguration(
            cfg => cfg.AddProfile(new MappingProfile()),
            Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance).CreateMapper();

        // Mirrors the extra CreateMap calls that HotelPOS.Api's Program.cs registers on top of
        // MappingProfile (CreateOrderRequest/CreateItemRequest live in the API project, which
        // Application can't reference, so they're wired up at composition-root time instead).
        private static readonly IMapper MapperWithApiRequestMaps = new MapperConfiguration(
            cfg =>
            {
                cfg.AddProfile(new MappingProfile());
                cfg.CreateMap<CreateOrderRequest, CreateOrderCommand>();
            },
            Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance).CreateMapper();

        // ================= ItemsController.UpdateItem =================

        [Fact]
        public async Task Items_UpdateItem_Valid_ReturnsOkWithMappedDto()
        {
            var mediator = new Mock<IMediator>();
            mediator.Setup(m => m.Send(It.IsAny<UpdateItemCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Item { Id = 5, Name = "Juice", Price = 100 });

            var controller = new ItemsController(mediator.Object, Mapper);
            var result = await controller.UpdateItem(5, new CreateItemRequest { Name = "Juice", Price = 100 });

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var dto = Assert.IsType<ItemDto>(ok.Value);
            Assert.Equal("Juice", dto.Name);
        }

        [Fact]
        public async Task Items_UpdateItem_ArgumentException_ReturnsBadRequest()
        {
            var mediator = new Mock<IMediator>();
            mediator.Setup(m => m.Send(It.IsAny<UpdateItemCommand>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ArgumentException("Duplicate barcode."));

            var controller = new ItemsController(mediator.Object, Mapper);
            var result = await controller.UpdateItem(5, new CreateItemRequest { Name = "Juice", Price = 100 });

            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task Items_UpdateItem_InvalidOperationException_ReturnsConflict()
        {
            var mediator = new Mock<IMediator>();
            mediator.Setup(m => m.Send(It.IsAny<UpdateItemCommand>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Stock is locked by an open order."));

            var controller = new ItemsController(mediator.Object, Mapper);
            var result = await controller.UpdateItem(5, new CreateItemRequest { Name = "Juice", Price = 100 });

            Assert.IsType<ConflictObjectResult>(result.Result);
        }

        [Fact]
        public async Task Items_UpdateItem_InvalidId_ReturnsBadRequest()
        {
            var controller = new ItemsController(Mock.Of<IMediator>(), Mapper);
            var result = await controller.UpdateItem(0, new CreateItemRequest { Name = "Juice", Price = 100 });
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task Items_UpdateItem_InvalidModelState_ReturnsBadRequest()
        {
            var controller = new ItemsController(Mock.Of<IMediator>(), Mapper);
            controller.ModelState.AddModelError("Name", "Required");

            var result = await controller.UpdateItem(5, new CreateItemRequest { Name = "" });

            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        // ================= ItemsController.DeleteItem =================

        [Fact]
        public async Task Items_DeleteItem_Valid_ReturnsNoContent()
        {
            var mediator = new Mock<IMediator>();
            mediator.Setup(m => m.Send(It.IsAny<DeleteItemCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var controller = new ItemsController(mediator.Object, Mapper);
            var result = await controller.DeleteItem(5);

            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task Items_DeleteItem_ArgumentException_ReturnsBadRequest()
        {
            var mediator = new Mock<IMediator>();
            mediator.Setup(m => m.Send(It.IsAny<DeleteItemCommand>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ArgumentException("Item is referenced by an active order."));

            var controller = new ItemsController(mediator.Object, Mapper);
            var result = await controller.DeleteItem(5);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Items_DeleteItem_InvalidId_ReturnsBadRequest()
        {
            var controller = new ItemsController(Mock.Of<IMediator>(), Mapper);
            var result = await controller.DeleteItem(0);
            Assert.IsType<BadRequestObjectResult>(result);
        }

        // ================= OrdersController.CreateOrder =================

        [Fact]
        public async Task Orders_CreateOrder_Valid_ReturnsOkWithOrderId()
        {
            var mediator = new Mock<IMediator>();
            mediator.Setup(m => m.Send(It.IsAny<CreateOrderCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(42);

            var controller = new OrdersController(mediator.Object, Mock.Of<IUserContext>(), MapperWithApiRequestMaps);
            var request = new CreateOrderRequest
            {
                Items = new List<OrderItemDto> { new OrderItemDto { ItemId = 1, Quantity = 2, Price = 50 } },
                TableNumber = 3
            };

            var result = await controller.CreateOrder(request);

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(42, ok.Value);
        }

        [Fact]
        public async Task Orders_CreateOrder_InvalidModelState_ReturnsBadRequest()
        {
            var controller = new OrdersController(Mock.Of<IMediator>(), Mock.Of<IUserContext>(), MapperWithApiRequestMaps);
            controller.ModelState.AddModelError("Items", "Required");

            var result = await controller.CreateOrder(new CreateOrderRequest());

            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task Orders_CreateOrder_MapsRequestFieldsIntoCommand()
        {
            CreateOrderCommand? captured = null;
            var mediator = new Mock<IMediator>();
            mediator.Setup(m => m.Send(It.IsAny<CreateOrderCommand>(), It.IsAny<CancellationToken>()))
                .Callback<IRequest<int>, CancellationToken>((cmd, _) => captured = (CreateOrderCommand)cmd)
                .ReturnsAsync(1);

            var controller = new OrdersController(mediator.Object, Mock.Of<IUserContext>(), MapperWithApiRequestMaps);
            var request = new CreateOrderRequest
            {
                Items = new List<OrderItemDto> { new OrderItemDto { ItemId = 1, Quantity = 2, Price = 50 } },
                TableNumber = 7,
                Discount = 10,
                PaymentMode = "Card",
                CustomerName = "Alice"
            };

            await controller.CreateOrder(request);

            Assert.NotNull(captured);
            Assert.Equal(7, captured!.TableNumber);
            Assert.Equal(10, captured.Discount);
            Assert.Equal("Card", captured.PaymentMode);
            Assert.Equal("Alice", captured.CustomerName);
            Assert.Single(captured.Items);
        }

        // ================= CategoriesController.DeleteCategory =================

        [Fact]
        public async Task Categories_DeleteCategory_Valid_ReturnsNoContent()
        {
            var svc = new Mock<ICategoryService>();
            svc.Setup(s => s.DeleteCategoryAsync(3)).Returns(Task.CompletedTask);

            var controller = new CategoriesController(svc.Object, Mapper);
            var result = await controller.DeleteCategory(3);

            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task Categories_DeleteCategory_InUse_ReturnsConflict()
        {
            var svc = new Mock<ICategoryService>();
            svc.Setup(s => s.DeleteCategoryAsync(3)).ThrowsAsync(new InvalidOperationException("Category has items assigned."));

            var controller = new CategoriesController(svc.Object, Mapper);
            var result = await controller.DeleteCategory(3);

            Assert.IsType<ConflictObjectResult>(result);
        }
    }
}
