using HotelPOS.Application;
using HotelPOS.Application.Interface;
using HotelPOS.Domain;
using HotelPOS.Domain.Interface;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace HotelPOS.Tests
{
    public class PhaseTests
    {
        #region Cash Management (Shift) Tests

        [Fact]
        public async Task CashService_OpenSession_CreatesCorrectEntry()
        {
            var mockRepo = new Mock<ICashRepository>();
            var service = new CashService(mockRepo.Object);

            await service.OpenSessionAsync(1500.50m, "admin");

            mockRepo.Verify(r => r.AddAsync(It.Is<CashSession>(s => 
                s.OpeningBalance == 1500.50m && 
                s.OpenedBy == "admin" && 
                s.Status == "Open")), Times.Once);
        }

        [Fact]
        public async Task CashService_CloseSession_CalculatesExpectedBalances()
        {
            var mockRepo = new Mock<ICashRepository>();
            var service = new CashService(mockRepo.Object);
            var session = new CashSession { Id = 1, OpeningBalance = 1000, Status = "Open", OpenedAt = DateTime.UtcNow.AddHours(-5) };

            mockRepo.Setup(r => r.GetCurrentSessionAsync()).ReturnsAsync(session);
            mockRepo.Setup(r => r.GetSalesTotalAsync(It.IsAny<DateTime>())).ReturnsAsync(500m);

            await service.CloseSessionAsync(1500m, "Perfect match", "admin");

            mockRepo.Verify(r => r.UpdateAsync(It.Is<CashSession>(s => 
                s.Status == "Closed" && 
                s.ClosingBalance == 1500m && 
                s.ActualCash == 1500m)), Times.Once);
        }

        #endregion

        #region Hold / Resume Tests

        [Fact]
        public void CartService_HoldOrder_MovesItemsToHeldList()
        {
            var cart = new CartService();
            var item = new Item { Id = 1, Name = "Coffee", Price = 50 };
            cart.AddItem(1, item); // Table 1

            cart.HoldOrder(1, "Bill's Table");

            var activeItems = cart.GetItems(1);
            var heldOrders = cart.GetHeldOrders();

            Assert.Empty(activeItems);
            Assert.Single(heldOrders);
            Assert.Equal("Bill's Table", heldOrders[0].HoldName);
            Assert.Single(heldOrders[0].Items);
        }

        [Fact]
        public void CartService_ResumeOrder_RestoresItemsToTable()
        {
            var cart = new CartService();
            var item = new Item { Id = 1, Name = "Coffee", Price = 50 };
            cart.AddItem(1, item); // Table 1
            
            cart.HoldOrder(1, "Test Hold"); // Guid is generated inside
            var held = cart.GetHeldOrders()[0];

            cart.ResumeHeldOrder(held.Id, 2); // Resume to Table 2

            var table2Items = cart.GetItems(2);
            var heldOrders = cart.GetHeldOrders();

            Assert.Single(table2Items);
            Assert.Empty(heldOrders);
            Assert.Equal("Coffee", table2Items[0].ItemName);
        }

        #endregion

        #region Billing Enhancements (Discount / PaymentMode)

        [Fact]
        public async Task OrderService_SaveOrder_PersistsDiscountAndPaymentMode()
        {
            var mockRepo = new Mock<IOrderRepository>();
            var mockMediator = new Mock<MediatR.IMediator>();
            var mockItemService = new Mock<IItemService>();
            var service = new OrderService(mockRepo.Object, mockMediator.Object, mockItemService.Object);

            var items = new List<OrderItem> 
            { 
                new OrderItem { ItemId = 1, ItemName = "Item 1", Quantity = 1, Price = 100, Total = 100 } 
            };
            
            mockRepo.Setup(r => r.GetNextInvoiceNumberAsync(It.IsAny<string>())).ReturnsAsync("INV-001");

            await service.SaveOrderAsync(items, 1, discount: 10m, paymentMode: "UPI");

            mockRepo.Verify(r => r.AddAsync(It.Is<Order>(o => 
                o.DiscountAmount == 10m && 
                o.PaymentMode == "UPI" && 
                o.TotalAmount == 90m)), Times.Once); // 100 - 10 + 0 Tax
        }

        #endregion

        #region Stock Reconciliation (White Box)

        [Fact]
        public async Task OrderService_UpdateOrder_ReversesPreviousStockAndAppliesNew()
        {
            // Arrange
            var mockRepo = new Mock<IOrderRepository>();
            var mockMediator = new Mock<MediatR.IMediator>();
            var mockItemService = new Mock<IItemService>();
            var service = new OrderService(mockRepo.Object, mockMediator.Object, mockItemService.Object);

            var oldOrder = new Order 
            { 
                Id = 1, 
                Items = new List<OrderItem> 
                { 
                    new OrderItem { ItemId = 10, Quantity = 2 } // Deducted 2 originally
                } 
            };

            var newOrder = new Order
            {
                Id = 1,
                Items = new List<OrderItem>
                {
                    new OrderItem { ItemId = 10, Quantity = 3 } // Now want 3
                }
            };

            mockRepo.Setup(r => r.GetAllWithItemsAsync()).ReturnsAsync(new List<Order> { oldOrder });
            
            // Act
            await service.UpdateOrderAsync(newOrder);

            // Assert
            mockItemService.Verify(s => s.DeductStockAsync(10, -2), Times.Once);
            mockItemService.Verify(s => s.DeductStockAsync(10, 3), Times.Once);
        }

        #endregion
    }
}
