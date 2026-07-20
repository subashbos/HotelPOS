using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HotelPOS.Application.DTOs.Customer;
using HotelPOS.Application.Interfaces;
using HotelPOS.ViewModels;
using Moq;
using Xunit;

namespace HotelPOS.Tests.Unit.ViewModels
{
    public class CustomerHistoryViewModelTests
    {
        private readonly Mock<ICustomerService> _customerServiceMock = new();
        private readonly Mock<INotificationService> _notificationServiceMock = new();
        private readonly CustomerHistoryViewModel _vm;

        public CustomerHistoryViewModelTests()
        {
            _vm = new CustomerHistoryViewModel(_customerServiceMock.Object, _notificationServiceMock.Object);
        }

        [Fact]
        public async Task LoadAsync_PopulatesSummaryAndOrders()
        {
            var history = new CustomerHistoryDto
            {
                CustomerId = 7,
                CustomerName = "Deepa",
                TotalOrders = 2,
                TotalSpent = 800m,
                FirstOrderDate = new DateTime(2026, 1, 1),
                LastOrderDate = new DateTime(2026, 2, 1),
                Orders = new List<CustomerOrderSummaryDto>
                {
                    new CustomerOrderSummaryDto { OrderId = 1, InvoiceNumber = "INV-1", TotalAmount = 500m, Status = "Paid", OrderType = "DineIn", CreatedAt = new DateTime(2026, 1, 1) },
                    new CustomerOrderSummaryDto { OrderId = 2, InvoiceNumber = "INV-2", TotalAmount = 300m, Status = "Paid", OrderType = "Takeaway", CreatedAt = new DateTime(2026, 2, 1) }
                }
            };
            _customerServiceMock.Setup(s => s.GetCustomerHistoryAsync(7)).ReturnsAsync(history);

            await _vm.LoadAsync(7);

            Assert.Equal("Deepa", _vm.CustomerName);
            Assert.Equal(2, _vm.TotalOrders);
            Assert.Equal(800m, _vm.TotalSpent);
            Assert.Equal(new DateTime(2026, 1, 1), _vm.FirstOrderDate);
            Assert.Equal(new DateTime(2026, 2, 1), _vm.LastOrderDate);
            Assert.Equal(2, _vm.Orders.Count);
            Assert.Equal("INV-1", _vm.Orders[0].InvoiceNumber);
        }

        [Fact]
        public async Task LoadAsync_ServiceThrows_ShowsError()
        {
            _customerServiceMock.Setup(s => s.GetCustomerHistoryAsync(99)).ThrowsAsync(new Exception("Not found"));

            await _vm.LoadAsync(99);

            _notificationServiceMock.Verify(n => n.ShowError(It.Is<string>(m => m.Contains("Failed to load customer history"))), Times.Once);
            Assert.Empty(_vm.Orders);
        }
    }
}
