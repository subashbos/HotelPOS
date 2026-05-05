using HotelPOS.Application;
using HotelPOS.Application.Interface;
using HotelPOS.Domain;
using HotelPOS.Domain.Interface;
using HotelPOS.Domain.Events;
using MediatR;
using Moq;
using Xunit;

namespace HotelPOS.Tests
{
    /// <summary>
    /// Extended tests for OrderService.UpdateOrderAsync covering all edge cases:
    /// totals recalculation, discount persistence, mediator event, guard clauses.
    /// </summary>
    public class OrderServiceUpdateTests
    {
        private readonly Mock<IOrderRepository> _repoMock = new();
        private readonly Mock<IMediator> _mediatorMock = new();
        private readonly Mock<IItemService> _itemServiceMock = new();
        private readonly OrderService _service;

        public OrderServiceUpdateTests()
        {
            _service = new OrderService(_repoMock.Object, _mediatorMock.Object, _itemServiceMock.Object);
        }

        // ========== Guard clauses ===========

        [Fact]
        public async Task UpdateOrderAsync_EmptyItems_ThrowsArgumentException()
        {
            var order = new Order { Id = 1, Items = new List<OrderItem>() };

            await Assert.ThrowsAsync<ArgumentException>(
                () => _service.UpdateOrderAsync(order));

            _repoMock.Verify(r => r.UpdateAsync(It.IsAny<Order>()), Times.Never);
        }

        [Fact]
        public async Task UpdateOrderAsync_NullItems_ThrowsArgumentException()
        {
            var order = new Order { Id = 1, Items = null! };

            await Assert.ThrowsAsync<ArgumentException>(
                () => _service.UpdateOrderAsync(order));
        }

        [Fact]
        public async Task UpdateOrderAsync_OrderNotFound_ThrowsKeyNotFoundException()
        {
            _repoMock.Setup(r => r.GetByIdWithItemsAsync(99)).ReturnsAsync((Order?)null);

            var order = new Order
            {
                Id = 99,
                Items = new List<OrderItem> { new OrderItem { ItemId = 1, Quantity = 1, Price = 10, Total = 10 } }
            };

            await Assert.ThrowsAsync<KeyNotFoundException>(
                () => _service.UpdateOrderAsync(order));

            _repoMock.Verify(r => r.UpdateAsync(It.IsAny<Order>()), Times.Never);
        }

        // ========== Totals recalculation ===========

        [Fact]
        public async Task UpdateOrderAsync_RecalculatesSubtotalAndGst()
        {
            var oldOrder = new Order
            {
                Id = 1,
                Items = new List<OrderItem> { new OrderItem { ItemId = 1, Quantity = 1, Price = 100, Total = 100 } }
            };
            var newOrder = new Order
            {
                Id = 1,
                Items = new List<OrderItem>
                {
                    new OrderItem { ItemId = 1, Quantity = 2, Price = 100, TaxPercentage = 10, Total = 200 }
                },
                DiscountAmount = 0
            };
            _repoMock.Setup(r => r.GetByIdWithItemsAsync(1)).ReturnsAsync(oldOrder);

            await _service.UpdateOrderAsync(newOrder);

            Assert.Equal(200m, newOrder.Subtotal);
            Assert.Equal(20m, newOrder.GstAmount);   // 10% of 200
            Assert.Equal(220m, newOrder.TotalAmount); // 200 + 20
        }

        [Fact]
        public async Task UpdateOrderAsync_WithDiscount_TotalAmountIsSubtotalPlusGstMinusDiscount()
        {
            var oldOrder = new Order
            {
                Id = 2,
                Items = new List<OrderItem> { new OrderItem { ItemId = 1, Quantity = 1, Price = 100, Total = 100 } }
            };
            var newOrder = new Order
            {
                Id = 2,
                Items = new List<OrderItem>
                {
                    new OrderItem { ItemId = 1, Quantity = 1, Price = 200, TaxPercentage = 5, Total = 200 }
                },
                DiscountAmount = 15m
            };
            _repoMock.Setup(r => r.GetByIdWithItemsAsync(2)).ReturnsAsync(oldOrder);

            await _service.UpdateOrderAsync(newOrder);

            // Subtotal=200, GST=10, Discount=15 → Total=195
            Assert.Equal(200m, newOrder.Subtotal);
            Assert.Equal(10m, newOrder.GstAmount);
            Assert.Equal(195m, newOrder.TotalAmount);
        }

        [Fact]
        public async Task UpdateOrderAsync_DiscountExceedsTotal_TotalIsZeroNotNegative()
        {
            var oldOrder = new Order
            {
                Id = 3,
                Items = new List<OrderItem> { new OrderItem { ItemId = 1, Quantity = 1, Price = 50, Total = 50 } }
            };
            var newOrder = new Order
            {
                Id = 3,
                Items = new List<OrderItem>
                {
                    new OrderItem { ItemId = 1, Quantity = 1, Price = 50, TaxPercentage = 0, Total = 50 }
                },
                DiscountAmount = 9999m
            };
            _repoMock.Setup(r => r.GetByIdWithItemsAsync(3)).ReturnsAsync(oldOrder);

            await _service.UpdateOrderAsync(newOrder);

            Assert.Equal(0m, newOrder.TotalAmount); // clamped to 0
        }

        [Fact]
        public async Task UpdateOrderAsync_ZeroTaxItem_GstIsZero()
        {
            var oldOrder = new Order
            {
                Id = 4,
                Items = new List<OrderItem> { new OrderItem { ItemId = 1, Quantity = 1, Price = 100, Total = 100 } }
            };
            var newOrder = new Order
            {
                Id = 4,
                Items = new List<OrderItem>
                {
                    new OrderItem { ItemId = 1, Quantity = 3, Price = 100, TaxPercentage = 0, Total = 300 }
                }
            };
            _repoMock.Setup(r => r.GetByIdWithItemsAsync(4)).ReturnsAsync(oldOrder);

            await _service.UpdateOrderAsync(newOrder);

            Assert.Equal(300m, newOrder.Subtotal);
            Assert.Equal(0m, newOrder.GstAmount);
            Assert.Equal(300m, newOrder.TotalAmount);
        }

        // ========== CGST/SGST split ===========

        [Fact]
        public async Task UpdateOrderAsync_GstSplitEvenly_CgstAndSgstAreHalfEach()
        {
            var oldOrder = new Order
            {
                Id = 5,
                Items = new List<OrderItem> { new OrderItem { ItemId = 1, Quantity = 1, Price = 100, Total = 100 } }
            };
            var newOrder = new Order
            {
                Id = 5,
                Items = new List<OrderItem>
                {
                    new OrderItem { ItemId = 1, Quantity = 1, Price = 100, TaxPercentage = 18, Total = 100 }
                }
            };
            _repoMock.Setup(r => r.GetByIdWithItemsAsync(5)).ReturnsAsync(oldOrder);

            await _service.UpdateOrderAsync(newOrder);

            // GST=18, CGST=9, SGST=9
            Assert.Equal(18m, newOrder.GstAmount);
            Assert.Equal(9m, newOrder.CgstAmount);
            Assert.Equal(9m, newOrder.SgstAmount);
            Assert.Equal(0m, newOrder.IgstAmount);
        }

        // ========== Mediator event ===========

        [Fact]
        public async Task UpdateOrderAsync_PublishesMediatorEvent()
        {
            var oldOrder = new Order
            {
                Id = 6,
                Items = new List<OrderItem> { new OrderItem { ItemId = 1, Quantity = 1, Price = 100, Total = 100 } }
            };
            var newOrder = new Order
            {
                Id = 6,
                Items = new List<OrderItem>
                {
                    new OrderItem { ItemId = 1, Quantity = 2, Price = 100, TaxPercentage = 0, Total = 200 }
                }
            };
            _repoMock.Setup(r => r.GetByIdWithItemsAsync(6)).ReturnsAsync(oldOrder);

            await _service.UpdateOrderAsync(newOrder);

            _mediatorMock.Verify(
                m => m.Publish(It.Is<EntityActionEvent>(e => e.EntityName == "Order" && e.Action == "Update"), default),
                Times.Once);
        }

        // ========== Repo calls ===========

        [Fact]
        public async Task UpdateOrderAsync_CallsRepoUpdateExactlyOnce()
        {
            var oldOrder = new Order
            {
                Id = 7,
                Items = new List<OrderItem> { new OrderItem { ItemId = 1, Quantity = 1, Price = 50, Total = 50 } }
            };
            var newOrder = new Order
            {
                Id = 7,
                Items = new List<OrderItem>
                {
                    new OrderItem { ItemId = 1, Quantity = 1, Price = 50, TaxPercentage = 0, Total = 50 }
                }
            };
            _repoMock.Setup(r => r.GetByIdWithItemsAsync(7)).ReturnsAsync(oldOrder);

            await _service.UpdateOrderAsync(newOrder);

            _repoMock.Verify(r => r.UpdateAsync(newOrder), Times.Once);
        }

        // ========== Adding a new item in an update ===========

        [Fact]
        public async Task UpdateOrderAsync_NewItemAddedDuringEdit_StockDeductedForNewItem()
        {
            // Old order: item 1 qty 2
            // New order: item 1 qty 2 + item 2 qty 1 (newly added)
            var oldOrder = new Order
            {
                Id = 8,
                Items = new List<OrderItem> { new OrderItem { ItemId = 1, Quantity = 2, Price = 50, Total = 100 } }
            };
            var newOrder = new Order
            {
                Id = 8,
                Items = new List<OrderItem>
                {
                    new OrderItem { ItemId = 1, Quantity = 2, Price = 50, TaxPercentage = 0, Total = 100 },
                    new OrderItem { ItemId = 2, Quantity = 1, Price = 80, TaxPercentage = 0, Total = 80 }
                }
            };
            _repoMock.Setup(r => r.GetByIdWithItemsAsync(8)).ReturnsAsync(oldOrder);

            await _service.UpdateOrderAsync(newOrder);

            // Old item 1 stock returned
            _itemServiceMock.Verify(s => s.DeductStockAsync(1, -2), Times.Once);
            // New item 1 stock deducted
            _itemServiceMock.Verify(s => s.DeductStockAsync(1, 2), Times.Once);
            // New item 2 stock deducted
            _itemServiceMock.Verify(s => s.DeductStockAsync(2, 1), Times.Once);
        }

        // ========== Removing an item during update ===========

        [Fact]
        public async Task UpdateOrderAsync_ItemRemovedDuringEdit_OnlyReturnCallForRemovedItem()
        {
            // Old: item 1 qty 2, item 2 qty 3
            // New: only item 1 qty 2 (item 2 removed)
            var oldOrder = new Order
            {
                Id = 9,
                Items = new List<OrderItem>
                {
                    new OrderItem { ItemId = 1, Quantity = 2, Price = 50, Total = 100 },
                    new OrderItem { ItemId = 2, Quantity = 3, Price = 30, Total = 90 }
                }
            };
            var newOrder = new Order
            {
                Id = 9,
                Items = new List<OrderItem>
                {
                    new OrderItem { ItemId = 1, Quantity = 2, Price = 50, TaxPercentage = 0, Total = 100 }
                }
            };
            _repoMock.Setup(r => r.GetByIdWithItemsAsync(9)).ReturnsAsync(oldOrder);

            await _service.UpdateOrderAsync(newOrder);

            // Item 2: return 3 units, no new deduction
            _itemServiceMock.Verify(s => s.DeductStockAsync(2, -3), Times.Once);
            _itemServiceMock.Verify(s => s.DeductStockAsync(2, It.Is<int>(q => q > 0)), Times.Never);
        }
    }
}
