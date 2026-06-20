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
    /// Tests that Takeaway and Online orders:
    ///   - Store TableNumber = 0 in the database
    ///   - Do NOT require a table number (no ArgumentException for tableNumber = 0)
    ///   - DineIn still requires a valid table number (> 0)
    ///   - UpdateOrderAsync normalises TableNumber to 0 for non-DineIn
    ///   - Invalid order types are rejected
    /// </summary>
    public class OrderTypeTableTests
    {
        private readonly Mock<IOrderRepository> _repo = new();
        private readonly Mock<IMediator> _mediator = new();
        private readonly Mock<IItemService> _itemService = new();
        private readonly OrderService _service;

        public OrderTypeTableTests()
        {
            _service = new OrderService(_repo.Object, _mediator.Object, _itemService.Object);
        }

        private static List<OrderItem> OneItem() =>
            new() { new OrderItem { ItemId = 1, ItemName = "Tea", Quantity = 1, Price = 50, TaxPercentage = 0, Total = 50 } };

        private void SetupSave(int returnId = 1)
        {
            _repo.Setup(r => r.GetNextInvoiceNumberAsync(It.IsAny<string>())).ReturnsAsync("INV/2526/0001");
            _repo.Setup(r => r.AddAsync(It.IsAny<Order>())).ReturnsAsync(returnId);
        }

        // ── DineIn: table number required ────────────────────────────────────

        [Fact]
        public async Task SaveOrder_DineIn_ValidTable_Succeeds()
        {
            SetupSave();
            var ex = await Record.ExceptionAsync(
                () => _service.SaveOrderAsync(new SaveOrderRequest(OneItem(), 3, OrderType: "DineIn")));
            Assert.Null(ex);
        }

        [Fact]
        public async Task SaveOrder_DineIn_TableZero_ThrowsArgumentException()
        {
            await Assert.ThrowsAsync<ArgumentException>(
                () => _service.SaveOrderAsync(new SaveOrderRequest(OneItem(), 0, OrderType: "DineIn")));
        }

        [Fact]
        public async Task SaveOrder_DineIn_NegativeTable_ThrowsArgumentException()
        {
            await Assert.ThrowsAsync<ArgumentException>(
                () => _service.SaveOrderAsync(new SaveOrderRequest(OneItem(), -1, OrderType: "DineIn")));
        }

        [Fact]
        public async Task SaveOrder_DineIn_StoresRealTableNumber()
        {
            SetupSave();
            Order? saved = null;
            _repo.Setup(r => r.AddAsync(It.IsAny<Order>()))
                 .Callback<Order>(o => saved = o).ReturnsAsync(1);

            await _service.SaveOrderAsync(new SaveOrderRequest(OneItem(), 5, OrderType: "DineIn"));

            Assert.Equal(5, saved!.TableNumber);
        }

        // ── Takeaway: no table required, stored as 0 ─────────────────────────

        [Fact]
        public async Task SaveOrder_Takeaway_TableZero_Succeeds()
        {
            SetupSave();
            var ex = await Record.ExceptionAsync(
                () => _service.SaveOrderAsync(new SaveOrderRequest(OneItem(), 0, OrderType: "Takeaway")));
            Assert.Null(ex);
        }

        [Fact]
        public async Task SaveOrder_Takeaway_AnyTableNumber_StoredAsZero()
        {
            SetupSave();
            Order? saved = null;
            _repo.Setup(r => r.AddAsync(It.IsAny<Order>()))
                 .Callback<Order>(o => saved = o).ReturnsAsync(1);

            // Even if caller passes a table number, it must be normalised to 0
            await _service.SaveOrderAsync(new SaveOrderRequest(OneItem(), 7, OrderType: "Takeaway"));

            Assert.Equal(0, saved!.TableNumber);
        }

        [Fact]
        public async Task SaveOrder_Takeaway_OrderTypeStoredCorrectly()
        {
            SetupSave();
            Order? saved = null;
            _repo.Setup(r => r.AddAsync(It.IsAny<Order>()))
                 .Callback<Order>(o => saved = o).ReturnsAsync(1);

            await _service.SaveOrderAsync(new SaveOrderRequest(OneItem(), 0, OrderType: "Takeaway"));

            Assert.Equal("Takeaway", saved!.OrderType);
        }

        [Fact]
        public async Task SaveOrder_Takeaway_NegativeTable_StoredAsZero()
        {
            SetupSave();
            Order? saved = null;
            _repo.Setup(r => r.AddAsync(It.IsAny<Order>()))
                 .Callback<Order>(o => saved = o).ReturnsAsync(1);

            await _service.SaveOrderAsync(new SaveOrderRequest(OneItem(), -5, OrderType: "Takeaway"));

            Assert.Equal(0, saved!.TableNumber);
        }

        // ── Online: no table required, stored as 0 ───────────────────────────

        [Fact]
        public async Task SaveOrder_Online_TableZero_Succeeds()
        {
            SetupSave();
            var ex = await Record.ExceptionAsync(
                () => _service.SaveOrderAsync(new SaveOrderRequest(OneItem(), 0, OrderType: "Online")));
            Assert.Null(ex);
        }

        [Fact]
        public async Task SaveOrder_Online_AnyTableNumber_StoredAsZero()
        {
            SetupSave();
            Order? saved = null;
            _repo.Setup(r => r.AddAsync(It.IsAny<Order>()))
                 .Callback<Order>(o => saved = o).ReturnsAsync(1);

            await _service.SaveOrderAsync(new SaveOrderRequest(OneItem(), 99, OrderType: "Online"));

            Assert.Equal(0, saved!.TableNumber);
        }

        [Fact]
        public async Task SaveOrder_Online_OrderTypeStoredCorrectly()
        {
            SetupSave();
            Order? saved = null;
            _repo.Setup(r => r.AddAsync(It.IsAny<Order>()))
                 .Callback<Order>(o => saved = o).ReturnsAsync(1);

            await _service.SaveOrderAsync(new SaveOrderRequest(OneItem(), 0, OrderType: "Online"));

            Assert.Equal("Online", saved!.OrderType);
        }

        // ── Invalid order type ───────────────────────────────────────────────

        [Fact]
        public async Task SaveOrder_InvalidOrderType_ThrowsArgumentException()
        {
            await Assert.ThrowsAsync<ArgumentException>(
                () => _service.SaveOrderAsync(new SaveOrderRequest(OneItem(), 1, OrderType: "DriveThru")));
        }

        [Fact]
        public async Task SaveOrder_EmptyOrderType_ThrowsArgumentException()
        {
            await Assert.ThrowsAsync<ArgumentException>(
                () => _service.SaveOrderAsync(new SaveOrderRequest(OneItem(), 1, OrderType: "")));
        }

        [Fact]
        public async Task SaveOrder_NullOrderType_ThrowsArgumentException()
        {
            await Assert.ThrowsAsync<ArgumentException>(
                () => _service.SaveOrderAsync(new SaveOrderRequest(OneItem(), 1, OrderType: null!)));
        }

        // ── UpdateOrderAsync: normalises TableNumber for non-DineIn ──────────

        [Fact]
        public async Task UpdateOrder_Takeaway_TableNumberNormalisedToZero()
        {
            var oldOrder = new Order
            {
                Id = 10,
                TableNumber = 3,
                OrderType = "DineIn",
                Items = new List<OrderItem> { new OrderItem { ItemId = 1, Quantity = 1, Price = 50, Total = 50 } }
            };
            _repo.Setup(r => r.GetByIdWithItemsAsync(10)).ReturnsAsync(oldOrder);

            var updatedOrder = new Order
            {
                Id = 10,
                TableNumber = 3,   // caller still passes old table — must be overridden
                OrderType = "Takeaway",
                Items = new List<OrderItem> { new OrderItem { ItemId = 1, Quantity = 1, Price = 50, TaxPercentage = 0, Total = 50 } }
            };

            await _service.UpdateOrderAsync(updatedOrder);

            Assert.Equal(0, updatedOrder.TableNumber);
        }

        [Fact]
        public async Task UpdateOrder_Online_TableNumberNormalisedToZero()
        {
            var oldOrder = new Order
            {
                Id = 11,
                TableNumber = 2,
                OrderType = "DineIn",
                Items = new List<OrderItem> { new OrderItem { ItemId = 1, Quantity = 1, Price = 80, Total = 80 } }
            };
            _repo.Setup(r => r.GetByIdWithItemsAsync(11)).ReturnsAsync(oldOrder);

            var updatedOrder = new Order
            {
                Id = 11,
                TableNumber = 2,
                OrderType = "Online",
                Items = new List<OrderItem> { new OrderItem { ItemId = 1, Quantity = 1, Price = 80, TaxPercentage = 0, Total = 80 } }
            };

            await _service.UpdateOrderAsync(updatedOrder);

            Assert.Equal(0, updatedOrder.TableNumber);
        }

        [Fact]
        public async Task UpdateOrder_DineIn_TableNumberPreserved()
        {
            var oldOrder = new Order
            {
                Id = 12,
                TableNumber = 4,
                OrderType = "DineIn",
                Items = new List<OrderItem> { new OrderItem { ItemId = 1, Quantity = 1, Price = 100, Total = 100 } }
            };
            _repo.Setup(r => r.GetByIdWithItemsAsync(12)).ReturnsAsync(oldOrder);

            var updatedOrder = new Order
            {
                Id = 12,
                TableNumber = 4,
                OrderType = "DineIn",
                Items = new List<OrderItem> { new OrderItem { ItemId = 1, Quantity = 2, Price = 100, TaxPercentage = 0, Total = 200 } }
            };

            await _service.UpdateOrderAsync(updatedOrder);

            Assert.Equal(4, updatedOrder.TableNumber);
        }

        // ── Audit event includes order type ──────────────────────────────────

        [Fact]
        public async Task SaveOrder_Takeaway_AuditEventMentionsType()
        {
            SetupSave(42);
            string? auditDetails = null;
            _mediator.Setup(m => m.Publish(It.IsAny<EntityActionEvent>(), default))
                     .Callback<INotification, CancellationToken>((n, _) =>
                     {
                         if (n is EntityActionEvent e) auditDetails = e.Details;
                     });

            await _service.SaveOrderAsync(new SaveOrderRequest(OneItem(), 0, OrderType: "Takeaway"));

            Assert.Contains("Takeaway", auditDetails);
        }

        [Fact]
        public async Task SaveOrder_Online_AuditEventMentionsType()
        {
            SetupSave(43);
            string? auditDetails = null;
            _mediator.Setup(m => m.Publish(It.IsAny<EntityActionEvent>(), default))
                     .Callback<INotification, CancellationToken>((n, _) =>
                     {
                         if (n is EntityActionEvent e) auditDetails = e.Details;
                     });

            await _service.SaveOrderAsync(new SaveOrderRequest(OneItem(), 0, OrderType: "Online"));

            Assert.Contains("Online", auditDetails);
        }

        [Fact]
        public async Task SaveOrder_Takeaway_AuditEventShowsTableZero()
        {
            SetupSave(44);
            string? auditDetails = null;
            _mediator.Setup(m => m.Publish(It.IsAny<EntityActionEvent>(), default))
                     .Callback<INotification, CancellationToken>((n, _) =>
                     {
                         if (n is EntityActionEvent e) auditDetails = e.Details;
                     });

            await _service.SaveOrderAsync(new SaveOrderRequest(OneItem(), 0, OrderType: "Takeaway"));

            Assert.Contains("Table: 0", auditDetails);
        }

        // ── Stock deduction still works for tableless orders ─────────────────

        [Fact]
        public async Task SaveOrder_Takeaway_StockDeductedNormally()
        {
            SetupSave();

            await _service.SaveOrderAsync(new SaveOrderRequest(
                new List<OrderItem> { new OrderItem { ItemId = 5, ItemName = "Burger", Quantity = 3, Price = 100, TaxPercentage = 0, Total = 300 } },
                0, OrderType: "Takeaway"));

            _itemService.Verify(s => s.DeductStockAsync(5, 3), Times.Once);
        }

        [Fact]
        public async Task SaveOrder_Online_StockDeductedNormally()
        {
            SetupSave();

            await _service.SaveOrderAsync(new SaveOrderRequest(
                new List<OrderItem> { new OrderItem { ItemId = 6, ItemName = "Pizza", Quantity = 2, Price = 200, TaxPercentage = 5, Total = 400 } },
                0, OrderType: "Online"));

            _itemService.Verify(s => s.DeductStockAsync(6, 2), Times.Once);
        }

        // ── Totals calculated correctly regardless of order type ─────────────

        [Fact]
        public async Task SaveOrder_Takeaway_TotalsCalculatedCorrectly()
        {
            SetupSave();
            Order? saved = null;
            _repo.Setup(r => r.AddAsync(It.IsAny<Order>()))
                 .Callback<Order>(o => saved = o).ReturnsAsync(1);

            var items = new List<OrderItem>
            {
                new OrderItem { ItemId = 1, ItemName = "Biryani", Quantity = 2, Price = 150, TaxPercentage = 5, Total = 300 }
            };

            await _service.SaveOrderAsync(new SaveOrderRequest(items, 0, OrderType: "Takeaway"));

            Assert.Equal(300m, saved!.Subtotal);
            Assert.Equal(15m, saved.GstAmount);   // 5% of 300
            Assert.Equal(315m, saved.TotalAmount);
        }

        [Fact]
        public async Task SaveOrder_Online_WithDiscount_TotalCorrect()
        {
            SetupSave();
            Order? saved = null;
            _repo.Setup(r => r.AddAsync(It.IsAny<Order>()))
                 .Callback<Order>(o => saved = o).ReturnsAsync(1);

            var items = new List<OrderItem>
            {
                new OrderItem { ItemId = 1, ItemName = "Pasta", Quantity = 1, Price = 200, TaxPercentage = 0, Total = 200 }
            };

            await _service.SaveOrderAsync(new SaveOrderRequest(items, 0, Discount: 20m, OrderType: "Online"));

            Assert.Equal(200m, saved!.Subtotal);
            Assert.Equal(0m, saved.GstAmount);
            Assert.Equal(180m, saved.TotalAmount);
        }
    }

    /// <summary>
    /// Tests for BillingViewModel order-type / table-number behaviour.
    /// Verifies IsTableless, IsTableVisible, and that SaveOrderAsync
    /// passes the correct tableNumber (0) for Takeaway/Online.
    /// </summary>
    public class BillingViewModelOrderTypeTests
    {
        private (HotelPOS.ViewModels.BillingViewModel vm,
                 Mock<IOrderService> orderSvc,
                 Mock<ICartService> cartSvc)
            BuildVm(string initialOrderType = "DineIn")
        {
            var cartSvc = new Mock<ICartService>();
            var orderSvc = new Mock<IOrderService>();
            var itemSvc = new Mock<IItemService>();
            var settSvc = new Mock<ISettingService>();
            var catSvc = new Mock<ICategoryService>();
            var notifSvc = new Mock<INotificationService>();
            var cashSvc = new Mock<ICashService>();
            var tableSvc = new Mock<ITableService>();

            cartSvc.Setup(s => s.GetHeldOrders()).Returns(new List<HeldOrder>());
            cartSvc.Setup(s => s.GetItems(It.IsAny<int>())).Returns(new List<OrderItem>());
            cartSvc.Setup(s => s.GetSubtotal(It.IsAny<int>())).Returns(0m);
            cartSvc.Setup(s => s.GetGstAmount(It.IsAny<int>())).Returns(0m);
            cartSvc.Setup(s => s.GetActiveTables()).Returns(new List<int>());

            settSvc.Setup(s => s.GetSettingsAsync()).ReturnsAsync(new SystemSetting());
            orderSvc.Setup(s => s.SaveOrderAsync(It.IsAny<SaveOrderRequest>())).ReturnsAsync(1);
            cashSvc.Setup(s => s.GetCurrentSessionAsync()).ReturnsAsync(new CashSession { Id = 1 });

            var vm = new HotelPOS.ViewModels.BillingViewModel(
                itemSvc.Object, cartSvc.Object, orderSvc.Object,
                settSvc.Object, catSvc.Object, notifSvc.Object,
                cashSvc.Object, tableSvc.Object);

            vm.OrderType = initialOrderType;
            return (vm, orderSvc, cartSvc);
        }

        // ── IsTableless / IsTableVisible ─────────────────────────────────────

        [Fact]
        public void IsTableless_DineIn_IsFalse()
        {
            var (vm, _, _) = BuildVm("DineIn");
            Assert.False(vm.IsTableless);
        }

        [Fact]
        public void IsTableless_Takeaway_IsTrue()
        {
            var (vm, _, _) = BuildVm("Takeaway");
            Assert.True(vm.IsTableless);
        }

        [Fact]
        public void IsTableless_Online_IsTrue()
        {
            var (vm, _, _) = BuildVm("Online");
            Assert.True(vm.IsTableless);
        }

        [Fact]
        public void IsTableVisible_DineIn_IsTrue()
        {
            var (vm, _, _) = BuildVm("DineIn");
            Assert.True(vm.IsTableVisible);
        }

        [Fact]
        public void IsTableVisible_Takeaway_IsFalse()
        {
            var (vm, _, _) = BuildVm("Takeaway");
            Assert.False(vm.IsTableVisible);
        }

        [Fact]
        public void IsTableVisible_Online_IsFalse()
        {
            var (vm, _, _) = BuildVm("Online");
            Assert.False(vm.IsTableVisible);
        }

        // ── TableNumber set to 0 when switching to tableless ─────────────────

        [Fact]
        public void SwitchToTakeaway_TableNumberBecomesZero()
        {
            var (vm, _, _) = BuildVm("DineIn");
            vm.TableNumber = 3;

            vm.OrderType = "Takeaway";

            Assert.Equal(0, vm.TableNumber);
        }

        [Fact]
        public void SwitchToOnline_TableNumberBecomesZero()
        {
            var (vm, _, _) = BuildVm("DineIn");
            vm.TableNumber = 5;

            vm.OrderType = "Online";

            Assert.Equal(0, vm.TableNumber);
        }

        [Fact]
        public void SwitchBackToDineIn_TableNumberRestored()
        {
            var (vm, _, _) = BuildVm("Takeaway");
            // TableNumber should be 0 after Takeaway
            Assert.Equal(0, vm.TableNumber);

            vm.OrderType = "DineIn";

            // Should restore to 1 (default)
            Assert.Equal(1, vm.TableNumber);
        }

        // ── SaveOrderAsync passes tableNumber = 0 for tableless ──────────────

        [Fact]
        public async Task SaveOrder_Takeaway_PassesTableZeroToService()
        {
            var (vm, orderSvc, cartSvc) = BuildVm("Takeaway");

            // Provide items in the cart
            cartSvc.Setup(s => s.GetItems(0)).Returns(new List<OrderItem>
            {
                new OrderItem { ItemId = 1, ItemName = "Burger", Quantity = 1, Price = 100, Total = 100 }
            });
            cartSvc.Setup(s => s.GetSubtotal(0)).Returns(100m);

            await vm.SaveOrderCommand.ExecuteAsync(null);

            orderSvc.Verify(s => s.SaveOrderAsync(It.Is<SaveOrderRequest>(r => r.TableNumber == 0 && r.OrderType == "Takeaway")), Times.Once);
        }

        [Fact]
        public async Task SaveOrder_Online_PassesTableZeroToService()
        {
            var (vm, orderSvc, cartSvc) = BuildVm("Online");

            cartSvc.Setup(s => s.GetItems(0)).Returns(new List<OrderItem>
            {
                new OrderItem { ItemId = 2, ItemName = "Pizza", Quantity = 1, Price = 200, Total = 200 }
            });
            cartSvc.Setup(s => s.GetSubtotal(0)).Returns(200m);

            await vm.SaveOrderCommand.ExecuteAsync(null);

            orderSvc.Verify(s => s.SaveOrderAsync(It.Is<SaveOrderRequest>(r => r.TableNumber == 0 && r.OrderType == "Online")), Times.Once);
        }

        [Fact]
        public async Task SaveOrder_DineIn_PassesRealTableNumberToService()
        {
            var (vm, orderSvc, cartSvc) = BuildVm("DineIn");
            vm.TableNumber = 4;

            cartSvc.Setup(s => s.GetItems(4)).Returns(new List<OrderItem>
            {
                new OrderItem { ItemId = 3, ItemName = "Tea", Quantity = 2, Price = 30, Total = 60 }
            });
            cartSvc.Setup(s => s.GetSubtotal(4)).Returns(60m);

            await vm.SaveOrderCommand.ExecuteAsync(null);

            orderSvc.Verify(s => s.SaveOrderAsync(It.Is<SaveOrderRequest>(r => r.TableNumber == 4 && r.OrderType == "DineIn")), Times.Once);
        }

        // ── LoadOrderForEdit restores tableless state correctly ───────────────

        [Fact]
        public void LoadOrderForEdit_TakeawayOrder_IsTablelessTrue()
        {
            var (vm, _, cartSvc) = BuildVm("DineIn");
            cartSvc.Setup(s => s.GetItems(0)).Returns(new List<OrderItem>());

            var order = new Order
            {
                Id = 20, TableNumber = 0, OrderType = "Takeaway",
                Items = new List<OrderItem> { new OrderItem { ItemId = 1, ItemName = "Wrap", Quantity = 1, Price = 80, Total = 80 } }
            };

            vm.LoadOrderForEdit(order);

            Assert.True(vm.IsTableless);
            Assert.Equal(0, vm.TableNumber);
            Assert.Equal("Takeaway", vm.OrderType);
        }

        [Fact]
        public void LoadOrderForEdit_OnlineOrder_IsTablelessTrue()
        {
            var (vm, _, cartSvc) = BuildVm("DineIn");
            cartSvc.Setup(s => s.GetItems(0)).Returns(new List<OrderItem>());

            var order = new Order
            {
                Id = 21, TableNumber = 0, OrderType = "Online",
                Items = new List<OrderItem> { new OrderItem { ItemId = 1, ItemName = "Salad", Quantity = 1, Price = 120, Total = 120 } }
            };

            vm.LoadOrderForEdit(order);

            Assert.True(vm.IsTableless);
            Assert.Equal(0, vm.TableNumber);
            Assert.Equal("Online", vm.OrderType);
        }

        [Fact]
        public void LoadOrderForEdit_DineInOrder_IsTablelessFalse()
        {
            var (vm, _, cartSvc) = BuildVm("Takeaway");
            cartSvc.Setup(s => s.GetItems(3)).Returns(new List<OrderItem>());

            var order = new Order
            {
                Id = 22, TableNumber = 3, OrderType = "DineIn",
                Items = new List<OrderItem> { new OrderItem { ItemId = 1, ItemName = "Coffee", Quantity = 1, Price = 50, Total = 50 } }
            };

            vm.LoadOrderForEdit(order);

            Assert.False(vm.IsTableless);
            Assert.Equal(3, vm.TableNumber);
            Assert.Equal("DineIn", vm.OrderType);
        }

        // ── After save, OrderType resets to DineIn ───────────────────────────

        [Fact]
        public async Task AfterSave_Takeaway_OrderTypeResetsToDineIn()
        {
            var (vm, _, cartSvc) = BuildVm("Takeaway");

            cartSvc.Setup(s => s.GetItems(0)).Returns(new List<OrderItem>
            {
                new OrderItem { ItemId = 1, ItemName = "Burger", Quantity = 1, Price = 100, Total = 100 }
            });
            cartSvc.Setup(s => s.GetSubtotal(0)).Returns(100m);

            await vm.SaveOrderCommand.ExecuteAsync(null);

            Assert.Equal("DineIn", vm.OrderType);
            Assert.False(vm.IsTableless);
        }

        [Fact]
        public async Task AfterSave_Online_TableNumberRestored()
        {
            var (vm, _, cartSvc) = BuildVm("Online");

            cartSvc.Setup(s => s.GetItems(0)).Returns(new List<OrderItem>
            {
                new OrderItem { ItemId = 1, ItemName = "Noodles", Quantity = 1, Price = 150, Total = 150 }
            });
            cartSvc.Setup(s => s.GetSubtotal(0)).Returns(150m);

            await vm.SaveOrderCommand.ExecuteAsync(null);

            // After reset to DineIn, TableNumber should be 1
            Assert.Equal(1, vm.TableNumber);
        }

        // ── Empty cart still blocked for tableless orders ─────────────────────

        [Fact]
        public async Task SaveOrder_Takeaway_EmptyCart_DoesNotCallService()
        {
            var (vm, orderSvc, cartSvc) = BuildVm("Takeaway");
            cartSvc.Setup(s => s.GetItems(0)).Returns(new List<OrderItem>());

            await vm.SaveOrderCommand.ExecuteAsync(null);

            orderSvc.Verify(s => s.SaveOrderAsync(It.IsAny<SaveOrderRequest>()), Times.Never);
        }
    }
}

