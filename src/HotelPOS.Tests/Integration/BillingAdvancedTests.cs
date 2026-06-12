using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HotelPOS.Application.Interfaces;
using HotelPOS.Application.UseCases;
using HotelPOS.Domain.Entities;
using HotelPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using MediatR;

namespace HotelPOS.Tests
{
    public class BillingAdvancedTests
    {
        private readonly Mock<IOrderRepository> _orderRepoMock;
        private readonly Mock<IMediator> _mediatorMock;
        private readonly Mock<IItemService> _itemServiceMock;
        private readonly OrderService _service;

        public BillingAdvancedTests()
        {
            _orderRepoMock = new Mock<IOrderRepository>();
            _mediatorMock = new Mock<IMediator>();
            _itemServiceMock = new Mock<IItemService>();
            _service = new OrderService(_orderRepoMock.Object, _mediatorMock.Object, _itemServiceMock.Object);
        }

        [Fact]
        public async Task VoidOrderAsync_ReplenishesInventoryAndSetsStatusToVoid()
        {
            // Arrange
            var orderId = 1;
            var order = new Order
            {
                Id = orderId,
                InvoiceNumber = "INV-001",
                Status = "Paid",
                TotalAmount = 500,
                Items = new List<OrderItem>
                {
                    new OrderItem { ItemId = 101, ItemName = "Item A", Quantity = 2, Price = 200, Total = 400 },
                    new OrderItem { ItemId = 102, ItemName = "Item B", Quantity = 1, Price = 100, Total = 100 }
                }
            };

            _orderRepoMock.Setup(r => r.GetByIdWithItemsAsync(orderId)).ReturnsAsync(order);

            // Act
            await _service.VoidOrderAsync(orderId, "Customer request", "AdminUser");

            // Assert
            Assert.Equal("Void", order.Status);
            Assert.Equal("Customer request", order.VoidReason);
            Assert.Equal(0, order.TotalAmount);
            Assert.Equal(0, order.Subtotal);
            
            // Verify stock replenishment (negative quantities passed to DeductStockAsync indicate stock return)
            _itemServiceMock.Verify(s => s.DeductStockAsync(101, -2), Times.Once);
            _itemServiceMock.Verify(s => s.DeductStockAsync(102, -1), Times.Once);
            _orderRepoMock.Verify(r => r.UpdateAsync(order), Times.Once);
        }

        [Fact]
        public async Task RefundOrderAsync_ReducesQuantitiesRestoresStockAndRecalculatesTotals()
        {
            // Arrange
            var orderId = 1;
            var order = new Order
            {
                Id = orderId,
                InvoiceNumber = "INV-002",
                Status = "Paid",
                Subtotal = 500,
                GstAmount = 25,
                TotalAmount = 525,
                AmountPaid = 525,
                CashPaid = 525,
                Items = new List<OrderItem>
                {
                    new OrderItem { ItemId = 101, ItemName = "Item A", Quantity = 2, Price = 200, Total = 400, TaxPercentage = 5 },
                    new OrderItem { ItemId = 102, ItemName = "Item B", Quantity = 1, Price = 100, Total = 100, TaxPercentage = 5 }
                }
            };

            _orderRepoMock.Setup(r => r.GetByIdWithItemsAsync(orderId)).ReturnsAsync(order);

            var itemsToRefund = new List<OrderItemRefundDto>
            {
                new OrderItemRefundDto(101, 1) // Refund 1 of Item A
            };

            // Act
            await _service.RefundOrderAsync(orderId, itemsToRefund, "Quality issue");

            // Assert
            Assert.Equal("PartiallyRefunded", order.Status);
            Assert.Equal(200, order.RefundedAmount);
            Assert.Equal("Quality issue", order.RefundReason);
            
            // Check recalculated totals
            // Item A quantity is reduced from 2 to 1 (Total: 200). Item B is unchanged (Total: 100).
            // Subtotal: 300, GST: 300 * 5% = 15. Total: 315.
            Assert.Equal(300, order.Subtotal);
            Assert.Equal(15, order.GstAmount);
            Assert.Equal(315, order.TotalAmount);
            Assert.Equal(325, order.AmountPaid); // 525 - 200 = 325

            _itemServiceMock.Verify(s => s.DeductStockAsync(101, -1), Times.Once);
            _orderRepoMock.Verify(r => r.UpdateAsync(order), Times.Once);
        }

        [Fact]
        public async Task ProcessPartialPaymentAsync_UpdatesBalancesAndTransitionsStatus()
        {
            // Arrange
            var orderId = 1;
            var order = new Order
            {
                Id = orderId,
                InvoiceNumber = "INV-003",
                Status = "Partial",
                TotalAmount = 500,
                AmountPaid = 200,
                CashPaid = 200
            };

            _orderRepoMock.Setup(r => r.GetByIdWithItemsAsync(orderId)).ReturnsAsync(order);

            // Act 1: Add UPI payment (Partial payment still, not fully paid)
            await _service.ProcessPartialPaymentAsync(orderId, 0, 0, 100);

            // Assert 1
            Assert.Equal("Partial", order.Status);
            Assert.Equal(300, order.AmountPaid);
            Assert.Equal(100, order.UpiPaid);

            // Act 2: Add Cash payment to fully pay the order
            await _service.ProcessPartialPaymentAsync(orderId, 200, 0, 0);

            // Assert 2
            Assert.Equal("Paid", order.Status);
            Assert.Equal(500, order.AmountPaid);
            Assert.Equal(400, order.CashPaid);
        }
    }
}
