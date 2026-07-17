using HotelPOS.Application.Interfaces;
using HotelPOS.Application.UseCases;
using HotelPOS.Domain.Common.Constants;
using HotelPOS.Domain.Entities;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace HotelPOS.Tests.Unit.Services
{
    public class CustomerServiceTests
    {
        private readonly Mock<ICustomerRepository> _repoMock;
        private readonly Mock<IOrderRepository> _orderRepoMock;
        private readonly CustomerService _service;

        public CustomerServiceTests()
        {
            _repoMock = new Mock<ICustomerRepository>();
            _orderRepoMock = new Mock<IOrderRepository>();
            _service = new CustomerService(_repoMock.Object, _orderRepoMock.Object);
        }

        [Fact]
        public async Task SaveCustomerAsync_ValidNewCustomer_ShouldSaveSuccessfully()
        {
            var customer = new Customer { Id = 0, Name = "Asha Rao", Phone = "9876543210" };

            _repoMock.Setup(r => r.ExistsByPhoneAsync("9876543210", 0)).ReturnsAsync(false);

            await _service.SaveCustomerAsync(customer);

            _repoMock.Verify(r => r.AddAsync(customer), Times.Once);
            Assert.True(customer.IsActive);
        }

        [Fact]
        public async Task SaveCustomerAsync_EmptyName_ShouldThrowArgumentException()
        {
            var customer = new Customer { Name = "" };

            var ex = await Assert.ThrowsAsync<ArgumentException>(() => _service.SaveCustomerAsync(customer));
            Assert.Contains("Name is required", ex.Message);
        }

        [Fact]
        public async Task SaveCustomerAsync_DuplicatePhone_ShouldThrowArgumentException()
        {
            var customer = new Customer { Id = 0, Name = "Vikram", Phone = "9876543210" };

            _repoMock.Setup(r => r.ExistsByPhoneAsync("9876543210", 0)).ReturnsAsync(true);

            var ex = await Assert.ThrowsAsync<ArgumentException>(() => _service.SaveCustomerAsync(customer));
            Assert.Contains("already exists", ex.Message);
        }

        [Fact]
        public async Task SaveCustomerAsync_InvalidGstin_ShouldThrowArgumentException()
        {
            var customer = new Customer { Name = "Priya", Gstin = "invalid" };

            await Assert.ThrowsAsync<ArgumentException>(() => _service.SaveCustomerAsync(customer));
        }

        [Fact]
        public async Task DeleteCustomerAsync_NotFound_ThrowsKeyNotFoundException()
        {
            _repoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Customer?)null);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.DeleteCustomerAsync(999));
        }

        [Fact]
        public async Task DeleteCustomerAsync_Found_DeactivatesCustomer()
        {
            var customer = new Customer { Id = 5, Name = "Rohit" };
            _repoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(customer);

            await _service.DeleteCustomerAsync(5);

            _repoMock.Verify(r => r.DeactivateAsync(5), Times.Once);
        }

        [Fact]
        public async Task GetCustomerHistoryAsync_NotFound_ThrowsKeyNotFoundException()
        {
            _repoMock.Setup(r => r.GetByIdAsync(42)).ReturnsAsync((Customer?)null);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.GetCustomerHistoryAsync(42));
        }

        [Fact]
        public async Task GetCustomerHistoryAsync_AggregatesOrders()
        {
            var customer = new Customer { Id = 7, Name = "Deepa" };
            _repoMock.Setup(r => r.GetByIdAsync(7)).ReturnsAsync(customer);

            var orders = new List<Order>
            {
                new Order { Id = 1, TotalAmount = 500, Status = OrderStatuses.Paid, CreatedAt = new DateTime(2026, 1, 1) },
                new Order { Id = 2, TotalAmount = 300, Status = OrderStatuses.Paid, CreatedAt = new DateTime(2026, 2, 1) },
                new Order { Id = 3, TotalAmount = 200, Status = OrderStatuses.Void, CreatedAt = new DateTime(2026, 3, 1) }
            };
            _orderRepoMock.Setup(r => r.GetPagedWithItemsAsync(1, -1, It.Is<OrderQueryFilter>(f => f.CustomerId == 7), It.IsAny<CancellationToken>()))
                .ReturnsAsync((orders, orders.Count));

            var history = await _service.GetCustomerHistoryAsync(7);

            Assert.Equal(3, history.TotalOrders);
            Assert.Equal(800, history.TotalSpent); // Void order excluded from spend
            Assert.Equal(new DateTime(2026, 1, 1), history.FirstOrderDate);
            Assert.Equal(new DateTime(2026, 3, 1), history.LastOrderDate);
        }
    }
}
