using HotelPOS.Domain.Common.Constants;
using HotelPOS.Application;
using HotelPOS.Application.UseCases;
using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Entities;
using HotelPOS.Domain.Events;
using MediatR;
using Moq;
using Xunit;

namespace HotelPOS.Tests
{
    /// <summary>
    /// Covers OrderService edge cases missing from OrderServiceTests.cs:
    /// invalid table number, negative discount, discount exceeding subtotal (now an error),
    /// invalid payment mode, delete non-existent order, GetOrder null, and fiscal year boundary.
    /// </summary>
    public class OrderServiceLoopholeTests
    {
        private readonly Mock<IOrderRepository> _repo = new();
        private readonly Mock<IMediator> _mediator = new();
        private readonly Mock<IItemService> _itemService = new();
        private readonly OrderService _service;

        public OrderServiceLoopholeTests()
        {
            _itemService.Setup(s => s.GetItemsByIdsAsync(It.IsAny<List<int>>()))
                .ReturnsAsync((List<int> ids) => ids.Select(id => new Item { Id = id, Name = "Tea", Price = 100m, TaxPercentage = 0m }).ToList());
            _service = new OrderService(_repo.Object, _mediator.Object, _itemService.Object);
        }

        private static List<OrderItem> OneItem() =>
            new() { new OrderItem { ItemId = 1, ItemName = "Tea", Quantity = 1, Price = 100, TaxPercentage = 0, Total = 100 } };

        // ── SaveOrderAsync guard clauses ─────────────────────────────────────

        [Fact]
        public async Task SaveOrderAsync_TableNumberZero_ThrowsArgumentException()
        {
            await Assert.ThrowsAsync<ArgumentException>(
                () => _service.SaveOrderAsync(new SaveOrderRequest(OneItem(), 0, OrderType: OrderTypes.DineIn)));
        }

        [Fact]
        public async Task SaveOrderAsync_Takeaway_TableNumberZero_Succeeds()
        {
            _repo.Setup(r => r.GetNextInvoiceNumberAsync(It.IsAny<string>())).ReturnsAsync("INV/2526/0001");
            _repo.Setup(r => r.AddAsync(It.IsAny<Order>())).ReturnsAsync(42);

            var orderId = await _service.SaveOrderAsync(new SaveOrderRequest(OneItem(), 0, OrderType: OrderTypes.Takeaway));

            Assert.Equal(42, orderId);
            _repo.Verify(r => r.AddAsync(It.Is<Order>(o => o.TableNumber == 0 && o.OrderType == OrderTypes.Takeaway)), Times.Once);
        }

        [Fact]
        public async Task SaveOrderAsync_NegativeTableNumber_ThrowsArgumentException()
        {
            await Assert.ThrowsAsync<ArgumentException>(
                () => _service.SaveOrderAsync(new SaveOrderRequest(OneItem(), -1)));
        }

        [Fact]
        public async Task SaveOrderAsync_NegativeDiscount_ThrowsArgumentException()
        {
            await Assert.ThrowsAsync<ArgumentException>(
                () => _service.SaveOrderAsync(new SaveOrderRequest(OneItem(), 1, Discount: -10m)));
        }

        [Fact]
        public async Task SaveOrderAsync_InvalidPaymentMode_ThrowsArgumentException()
        {
            await Assert.ThrowsAsync<ArgumentException>(
                () => _service.SaveOrderAsync(new SaveOrderRequest(OneItem(), 1, PaymentMode: "Bitcoin")));
        }

        [Fact]
        public async Task SaveOrderAsync_NullPaymentMode_ThrowsArgumentException()
        {
            await Assert.ThrowsAsync<ArgumentException>(
                () => _service.SaveOrderAsync(new SaveOrderRequest(OneItem(), 1, PaymentMode: null!)));
        }

        [Theory]
        [InlineData(PaymentModes.Cash)]
        [InlineData(PaymentModes.Card)]
        [InlineData(PaymentModes.Upi)]
        public async Task SaveOrderAsync_ValidPaymentModes_DoNotThrow(string mode)
        {
            _repo.Setup(r => r.GetNextInvoiceNumberAsync(It.IsAny<string>())).ReturnsAsync("INV/2526/0001");
            _repo.Setup(r => r.AddAsync(It.IsAny<Order>())).ReturnsAsync(1);

            var ex = await Record.ExceptionAsync(
                () => _service.SaveOrderAsync(new SaveOrderRequest(OneItem(), 1, PaymentMode: mode)));
            Assert.Null(ex);
        }

        // ── SaveOrderAsync — discount > subtotal is now a hard error ────────

        [Fact]
        public async Task SaveOrderAsync_DiscountExceedsSubtotal_ThrowsArgumentException()
        {
            // Subtotal of OneItem() = 100. Discount of 9999 must now be rejected.
            await Assert.ThrowsAsync<ArgumentException>(
                () => _service.SaveOrderAsync(new SaveOrderRequest(OneItem(), 1, Discount: 9999m)));
        }

        [Fact]
        public async Task SaveOrderAsync_ZeroDiscount_TotalEqualsSubtotalPlusGst()
        {
            _repo.Setup(r => r.GetNextInvoiceNumberAsync(It.IsAny<string>())).ReturnsAsync("INV/2526/0001");
            Order? saved = null;
            _repo.Setup(r => r.AddAsync(It.IsAny<Order>()))
                 .Callback<Order>(o => saved = o)
                 .ReturnsAsync(1);

            var items = new List<OrderItem>
            {
                new OrderItem { ItemId = 1, ItemName = "Pizza", Quantity = 1, Price = 200, TaxPercentage = 5, Total = 200 }
            };
            _itemService.Setup(s => s.GetItemsByIdsAsync(It.IsAny<List<int>>()))
                .ReturnsAsync(new List<Item> { new Item { Id = 1, Name = "Pizza", Price = 200m, TaxPercentage = 5m } });

            await _service.SaveOrderAsync(new SaveOrderRequest(items, 1, Discount: 0m));

            Assert.Equal(200m, saved!.Subtotal);
            Assert.Equal(10m, saved.GstAmount);
            Assert.Equal(210m, saved.TotalAmount);
        }

        // ── DeleteOrderAsync — non-existent order ────────────────────────────

        [Fact]
        public async Task DeleteOrderAsync_OrderNotFound_DoesNotThrowAndSkipsStockReturn()
        {
            _repo.Setup(r => r.GetByIdWithItemsAsync(999)).ReturnsAsync((Order?)null);

            var ex = await Record.ExceptionAsync(() => _service.DeleteOrderAsync(999));
            Assert.Null(ex);

            _itemService.Verify(s => s.DeductStockAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
            _repo.Verify(r => r.DeleteAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task DeleteOrderAsync_ExistingOrder_ReturnsStockBeforeDeleting()
        {
            var order = new Order
            {
                Id = 5,
                Items = new List<OrderItem>
                {
                    new OrderItem { ItemId = 1, Quantity = 3 },
                    new OrderItem { ItemId = 2, Quantity = 1 }
                }
            };
            _repo.Setup(r => r.GetByIdWithItemsAsync(5)).ReturnsAsync(order);

            await _service.DeleteOrderAsync(5);

            _itemService.Verify(s => s.DeductStockAsync(1, -3), Times.Once);
            _itemService.Verify(s => s.DeductStockAsync(2, -1), Times.Once);
            _repo.Verify(r => r.DeleteAsync(5), Times.Once);
        }

        // ── GetOrderAsync ────────────────────────────────────────────────────

        [Fact]
        public async Task GetOrderAsync_UnknownId_ReturnsNull()
        {
            _repo.Setup(r => r.GetByIdWithItemsAsync(999)).ReturnsAsync((Order?)null);

            var result = await _service.GetOrderAsync(999);

            Assert.Null(result);
        }

        [Fact]
        public async Task GetOrderAsync_KnownId_ReturnsOrder()
        {
            var order = new Order { Id = 3, TableNumber = 2 };
            _repo.Setup(r => r.GetByIdWithItemsAsync(3)).ReturnsAsync(order);

            var result = await _service.GetOrderAsync(3);

            Assert.NotNull(result);
            Assert.Equal(3, result!.Id);
        }

        // ── SaveOrderAsync — customer fields stored ──────────────────────────

        [Fact]
        public async Task SaveOrderAsync_CustomerFields_StoredOnOrder()
        {
            _repo.Setup(r => r.GetNextInvoiceNumberAsync(It.IsAny<string>())).ReturnsAsync("INV/2526/0001");
            Order? saved = null;
            _repo.Setup(r => r.AddAsync(It.IsAny<Order>()))
                 .Callback<Order>(o => saved = o)
                 .ReturnsAsync(1);

            await _service.SaveOrderAsync(new SaveOrderRequest(
                OneItem(), 1,
                CustomerName: "Ravi Kumar",
                CustomerPhone: "9876543210",
                CustomerGstin: "27AABCU9603R1ZX"));

            Assert.Equal("Ravi Kumar", saved!.CustomerName);
            Assert.Equal("9876543210", saved.CustomerPhone);
            Assert.Equal("27AABCU9603R1ZX", saved.CustomerGstin);
        }

        [Fact]
        public async Task SaveOrderAsync_NullCustomerFields_StoredAsNull()
        {
            _repo.Setup(r => r.GetNextInvoiceNumberAsync(It.IsAny<string>())).ReturnsAsync("INV/2526/0001");
            Order? saved = null;
            _repo.Setup(r => r.AddAsync(It.IsAny<Order>()))
                 .Callback<Order>(o => saved = o)
                 .ReturnsAsync(1);

            await _service.SaveOrderAsync(new SaveOrderRequest(OneItem(), 1));

            Assert.Null(saved!.CustomerName);
            Assert.Null(saved.CustomerPhone);
            Assert.Null(saved.CustomerGstin);
        }

        // ── SaveOrderAsync — CGST/SGST split ────────────────────────────────

        [Fact]
        public async Task SaveOrderAsync_GstSplitEvenly_CgstAndSgstAreHalfEach()
        {
            _repo.Setup(r => r.GetNextInvoiceNumberAsync(It.IsAny<string>())).ReturnsAsync("INV/2526/0001");
            Order? saved = null;
            _repo.Setup(r => r.AddAsync(It.IsAny<Order>()))
                 .Callback<Order>(o => saved = o)
                 .ReturnsAsync(1);

            var items = new List<OrderItem>
            {
                new OrderItem { ItemId = 1, ItemName = "Biryani", Quantity = 1, Price = 100, TaxPercentage = 18, Total = 100 }
            };
            _itemService.Setup(s => s.GetItemsByIdsAsync(It.IsAny<List<int>>()))
                .ReturnsAsync(new List<Item> { new Item { Id = 1, Name = "Biryani", Price = 100m, TaxPercentage = 18m } });

            await _service.SaveOrderAsync(new SaveOrderRequest(items, 1));

            Assert.Equal(18m, saved!.GstAmount);
            Assert.Equal(9m, saved.CgstAmount);
            Assert.Equal(9m, saved.SgstAmount);
            Assert.Equal(0m, saved.IgstAmount);
        }

        // ── SaveOrderAsync — mediator event published ────────────────────────

        [Fact]
        public async Task SaveOrderAsync_PublishesMediatorEvent()
        {
            _repo.Setup(r => r.GetNextInvoiceNumberAsync(It.IsAny<string>())).ReturnsAsync("INV/2526/0001");
            _repo.Setup(r => r.AddAsync(It.IsAny<Order>())).ReturnsAsync(42);

            await _service.SaveOrderAsync(new SaveOrderRequest(OneItem(), 1));

            _mediator.Verify(
                m => m.Publish(It.Is<EntityActionEvent>(e => e.EntityName == "Order" && e.Action == "Create"), default),
                Times.Once);
        }

        // ── Fiscal year boundary ─────────────────────────────────────────────

        [Fact]
        public void FiscalYear_March31_ReturnsPreviousYearRange()
        {
            var method = typeof(OrderService).GetMethod("GetFiscalYear",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            var result = (string)method!.Invoke(null, new object[] { new DateTime(2026, 3, 31) })!;

            Assert.Equal("2025-26", result);
        }

        [Fact]
        public void FiscalYear_April1_ReturnsCurrentYearRange()
        {
            var method = typeof(OrderService).GetMethod("GetFiscalYear",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            var result = (string)method!.Invoke(null, new object[] { new DateTime(2026, 4, 1) })!;

            Assert.Equal("2026-27", result);
        }

        [Fact]
        public void FiscalYear_January1_ReturnsPreviousYearRange()
        {
            var method = typeof(OrderService).GetMethod("GetFiscalYear",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            var result = (string)method!.Invoke(null, new object[] { new DateTime(2026, 1, 1) })!;

            Assert.Equal("2025-26", result);
        }
    }
}

