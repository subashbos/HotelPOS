using HotelPOS.Application;
using HotelPOS.Domain;
using HotelPOS.Domain.Interface;
using Moq;
using Xunit;
using System.Collections.Generic;
using System.Threading.Tasks;
using MediatR;
using HotelPOS.Application.Interfaces;

namespace HotelPOS.Tests
{
    public class LoopholesFinalTests
    {
        private readonly Mock<IOrderRepository> _orderRepo = new();
        private readonly Mock<IMediator> _mediator = new();
        private readonly Mock<IItemService> _itemService = new();
        private readonly OrderService _service;

        public LoopholesFinalTests()
        {
            _service = new OrderService(_orderRepo.Object, _mediator.Object, _itemService.Object);
        }

        [Fact]
        public async Task SaveOrderAsync_NegativePrice_ThrowsArgumentException()
        {
            var items = new List<OrderItem> { new OrderItem { ItemName = "Buggy", Price = -10, Quantity = 1 } };
            await Assert.ThrowsAsync<ArgumentException>(() => _service.SaveOrderAsync(items, 1));
        }

        [Fact]
        public async Task SaveOrderAsync_ZeroQuantity_ThrowsArgumentException()
        {
            var items = new List<OrderItem> { new OrderItem { ItemName = "Buggy", Price = 10, Quantity = 0 } };
            await Assert.ThrowsAsync<ArgumentException>(() => _service.SaveOrderAsync(items, 1));
        }

        [Fact]
        public async Task SaveOrderAsync_InvalidOrderType_ThrowsArgumentException()
        {
            var items = new List<OrderItem> { new OrderItem { ItemName = "Valid", Price = 10, Quantity = 1 } };
            await Assert.ThrowsAsync<ArgumentException>(() => _service.SaveOrderAsync(items, 1, orderType: "DroneDelivery"));
        }

        [Fact]
        public async Task SaveOrderAsync_InvalidPaymentMode_ThrowsArgumentException()
        {
            var items = new List<OrderItem> { new OrderItem { ItemName = "Valid", Price = 10, Quantity = 1 } };
            await Assert.ThrowsAsync<ArgumentException>(() => _service.SaveOrderAsync(items, 1, paymentMode: "Bitcoin"));
        }
        
        [Fact]
        public async Task SaveOrderAsync_DineInWithoutTable_ThrowsArgumentException()
        {
            var items = new List<OrderItem> { new OrderItem { ItemName = "Valid", Price = 10, Quantity = 1 } };
            await Assert.ThrowsAsync<ArgumentException>(() => _service.SaveOrderAsync(items, 0, orderType: "DineIn"));
        }
    }
}
