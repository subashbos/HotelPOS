using HotelPOS.Application;
using HotelPOS.Application.Interface;
using HotelPOS.Application.Interfaces;
using HotelPOS.Domain;
using HotelPOS.Domain.Interface;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Moq;
using HotelPOS.Persistence;
using Xunit;

namespace HotelPOS.Tests
{
    // ═══════════════════════════════════════════════════════════════════════════
    // BUG 1: OrderRepository.UpdateAsync was silently dropping
    //         CustomerName, CustomerPhone, CustomerGstin,
    //         CgstAmount, SgstAmount, IgstAmount on every edit.
    // ═══════════════════════════════════════════════════════════════════════════
    public class OrderRepositoryUpdateTests : IDisposable
    {
        private readonly string _dbName = Guid.NewGuid().ToString();
        private HotelDbContext NewCtx() =>
            new HotelDbContext(new DbContextOptionsBuilder<HotelDbContext>()
                .UseInMemoryDatabase(_dbName).Options);

        public void Dispose() { }

        private async Task<int> SeedOrderAsync()
        {
            using var ctx = NewCtx();
            var repo = new OrderRepository(ctx);
            var order = new Order
            {
                InvoiceNumber = "INV/2526/0001",
                FiscalYear    = "2025-26",
                TableNumber   = 1,
                CreatedAt     = DateTime.UtcNow,
                Subtotal      = 100m,
                GstAmount     = 5m,
                CgstAmount    = 2.5m,
                SgstAmount    = 2.5m,
                IgstAmount    = 0m,
                TotalAmount   = 105m,
                PaymentMode   = "Cash",
                Items = new List<OrderItem>
                {
                    new OrderItem { ItemId = 1, ItemName = "Burger", Quantity = 1, Price = 100, TaxPercentage = 5, Total = 100 }
                }
            };
            await repo.AddAsync(order);
            return order.Id;
        }

        private Order BuildUpdate(int id, Action<Order> configure)
        {
            // Create a fully-disconnected order (as BillingViewModel does)
            var o = new Order
            {
                Id          = id,
                TableNumber = 1,
                PaymentMode = "Cash",
                Items       = new List<OrderItem>
                {
                    new OrderItem { ItemId = 1, ItemName = "Burger", Quantity = 1, Price = 100, TaxPercentage = 5, Total = 100 }
                }
            };
            configure(o);
            return o;
        }

        [Fact]
        public async Task UpdateAsync_PersistsCustomerName()
        {
            var id = await SeedOrderAsync();
            var update = BuildUpdate(id, o =>
            {
                o.CustomerName  = "Ravi Kumar";
                o.CustomerPhone = "9876543210";
                o.CustomerGstin = "27AABCU9603R1ZX";
            });

            using var ctx = NewCtx();
            await new OrderRepository(ctx).UpdateAsync(update);

            using var ctx2 = NewCtx();
            var saved = await ctx2.Orders.FindAsync(id);
            Assert.Equal("Ravi Kumar",       saved!.CustomerName);
            Assert.Equal("9876543210",       saved.CustomerPhone);
            Assert.Equal("27AABCU9603R1ZX", saved.CustomerGstin);
        }

        [Fact]
        public async Task UpdateAsync_PersistsCgstSgstIgst()
        {
            var id = await SeedOrderAsync();
            var update = BuildUpdate(id, o =>
            {
                o.GstAmount  = 18m;
                o.CgstAmount = 9m;
                o.SgstAmount = 9m;
                o.IgstAmount = 0m;
                o.TotalAmount = 218m;
            });

            using var ctx = NewCtx();
            await new OrderRepository(ctx).UpdateAsync(update);

            using var ctx2 = NewCtx();
            var saved = await ctx2.Orders.FindAsync(id);
            Assert.Equal(9m,  saved!.CgstAmount);
            Assert.Equal(9m,  saved.SgstAmount);
            Assert.Equal(0m,  saved.IgstAmount);
            Assert.Equal(18m, saved.GstAmount);
        }

        [Fact]
        public async Task UpdateAsync_PersistsDiscountAndPaymentMode()
        {
            var id = await SeedOrderAsync();
            var update = BuildUpdate(id, o =>
            {
                o.DiscountAmount = 20m;
                o.PaymentMode   = "UPI";
                o.TotalAmount   = 85m;
            });

            using var ctx = NewCtx();
            await new OrderRepository(ctx).UpdateAsync(update);

            using var ctx2 = NewCtx();
            var saved = await ctx2.Orders.FindAsync(id);
            Assert.Equal(20m,  saved!.DiscountAmount);
            Assert.Equal("UPI", saved.PaymentMode);
            Assert.Equal(85m,  saved.TotalAmount);
        }

        [Fact]
        public async Task UpdateAsync_ReplacesOrderItems()
        {
            var id = await SeedOrderAsync();
            var update = BuildUpdate(id, o =>
            {
                o.Items = new List<OrderItem>
                {
                    new OrderItem { ItemId = 2, ItemName = "Pizza", Quantity = 2, Price = 150, TaxPercentage = 5, Total = 300 },
                    new OrderItem { ItemId = 3, ItemName = "Cola",  Quantity = 1, Price = 40,  TaxPercentage = 0, Total = 40  }
                };
                o.Subtotal    = 340m;
                o.TotalAmount = 355m;
            });

            using var ctx = NewCtx();
            await new OrderRepository(ctx).UpdateAsync(update);

            using var ctx2 = NewCtx();
            var saved = await ctx2.Orders
                .Include(o => o.Items)
                .FirstAsync(o => o.Id == id);
            Assert.Equal(2, saved.Items.Count);
            Assert.Contains(saved.Items, i => i.ItemName == "Pizza");
            Assert.Contains(saved.Items, i => i.ItemName == "Cola");
            Assert.DoesNotContain(saved.Items, i => i.ItemName == "Burger");
        }

        [Fact]
        public async Task UpdateAsync_UpdatesUpdatedAtTimestamp()
        {
            var id = await SeedOrderAsync();
            var before = DateTime.UtcNow.AddSeconds(-1);
            var update = BuildUpdate(id, _ => { });

            using var ctx = NewCtx();
            await new OrderRepository(ctx).UpdateAsync(update);

            using var ctx2 = NewCtx();
            var saved = await ctx2.Orders.FindAsync(id);
            Assert.True(saved!.UpdatedAt >= before);
        }

        [Fact]
        public async Task UpdateAsync_OrderNotFound_ThrowsKeyNotFoundException()
        {
            using var ctx = NewCtx();
            var ghost = new Order
            {
                Id    = 9999,
                Items = new List<OrderItem>
                {
                    new OrderItem { ItemId = 1, ItemName = "X", Quantity = 1, Price = 10, Total = 10 }
                }
            };
            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                new OrderRepository(ctx).UpdateAsync(ghost));
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // BUG 2: SettingService.SaveSettingsAsync dropped ShowGstBreakdown,
    //         ShowItemsOnBill, ShowDiscountLine, ShowPhoneOnReceipt,
    //         ShowThankYouFooter on every save.
    // ═══════════════════════════════════════════════════════════════════════════
    public class SettingServiceSaveTests
    {
        private readonly Mock<ISettingRepository> _repoMock = new();
        private readonly SettingService _service;

        public SettingServiceSaveTests()
        {
            _service = new SettingService(_repoMock.Object);
        }

        [Fact]
        public async Task SaveSettingsAsync_PersistsAllReceiptFlags()
        {
            var existing = new SystemSetting { Id = 1, HotelName = "Old Hotel" };
            _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existing);

            var updated = new SystemSetting
            {
                HotelName         = "New Hotel",
                HotelAddress      = "123 Main St",
                HotelPhone        = "044-12345",
                HotelGst          = "GST999",
                DefaultPrinter    = "PDF Printer",
                ShowPrintPreview  = true,
                ReceiptFormat     = "A4",
                ShowGstBreakdown  = true,
                ShowItemsOnBill   = false,
                ShowDiscountLine  = true,
                ShowPhoneOnReceipt = false,
                ShowThankYouFooter = true
            };

            await _service.SaveSettingsAsync(updated);

            // Verify all fields were mapped to existing before UpdateAsync was called
            _repoMock.Verify(r => r.UpdateAsync(It.Is<SystemSetting>(s =>
                s.ShowGstBreakdown   == true  &&
                s.ShowItemsOnBill    == false &&
                s.ShowDiscountLine   == true  &&
                s.ShowPhoneOnReceipt == false &&
                s.ShowThankYouFooter == true
            )), Times.Once);
        }

        [Fact]
        public async Task SaveSettingsAsync_PersistsCoreFields()
        {
            var existing = new SystemSetting { Id = 1 };
            _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existing);

            await _service.SaveSettingsAsync(new SystemSetting
            {
                HotelName     = "Grand Palace",
                HotelAddress  = "Park Road",
                HotelPhone    = "080-9999",
                HotelGst      = "GSTIN123",
                DefaultPrinter = "Thermal Printer",
                ShowPrintPreview = false,
                ReceiptFormat = "Thermal"
            });

            _repoMock.Verify(r => r.UpdateAsync(It.Is<SystemSetting>(s =>
                s.HotelName      == "Grand Palace" &&
                s.ReceiptFormat  == "Thermal"      &&
                s.DefaultPrinter == "Thermal Printer"
            )), Times.Once);
        }

        [Fact]
        public async Task SaveSettingsAsync_WhenNoExisting_AddsNew()
        {
            _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((SystemSetting?)null);
            var newSettings = new SystemSetting { HotelName = "First Hotel" };

            await _service.SaveSettingsAsync(newSettings);

            _repoMock.Verify(r => r.AddAsync(newSettings), Times.Once);
            _repoMock.Verify(r => r.UpdateAsync(It.IsAny<SystemSetting>()), Times.Never);
        }

        [Fact]
        public async Task GetSettingsAsync_WhenNoneExist_CreatesDefault()
        {
            _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((SystemSetting?)null);

            var result = await _service.GetSettingsAsync();

            Assert.NotNull(result);
            Assert.Equal(1, result.Id);
            _repoMock.Verify(r => r.AddAsync(It.Is<SystemSetting>(s => s.Id == 1)), Times.Once);
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // BUG 3: UserService.AddUserAsync threw NullReferenceException when
    //         password was null (called .Length before null-check).
    // ═══════════════════════════════════════════════════════════════════════════
    public class UserServiceSaveTests
    {
        private readonly Mock<IUserRepository> _repoMock = new();
        private readonly UserService _service;

        public UserServiceSaveTests()
        {
            _service = new UserService(_repoMock.Object);
        }

        [Fact]
        public async Task AddUserAsync_NullPassword_ReturnsErrorGracefully()
        {
            // Before fix: NullReferenceException. After fix: returns clean error.
            var (success, error) = await _service.AddUserAsync("user1", null!, "Cashier");
            Assert.False(success);
            Assert.False(string.IsNullOrEmpty(error));
        }

        [Fact]
        public async Task AddUserAsync_EmptyPassword_ReturnsError()
        {
            var (success, error) = await _service.AddUserAsync("user1", "", "Cashier");
            Assert.False(success);
            Assert.Contains("10", error); // mentions min length
        }

        [Fact]
        public async Task AddUserAsync_ShortPassword_ReturnsError()
        {
            var (success, error) = await _service.AddUserAsync("user1", "short", "Admin");
            Assert.False(success);
            Assert.Contains("10", error);
        }

        [Fact]
        public async Task AddUserAsync_ValidData_Succeeds()
        {
            _repoMock.Setup(r => r.GetUserByUsernameAsync("newuser")).ReturnsAsync((User?)null);

            var (success, error) = await _service.AddUserAsync("newuser", "StrongPass123!", "Admin");

            Assert.True(success);
            Assert.Equal(string.Empty, error);
            _repoMock.Verify(r => r.AddAsync(It.Is<User>(u =>
                u.Username == "newuser" && u.Role == "Admin" && u.IsActive)), Times.Once);
        }

        [Fact]
        public async Task AddUserAsync_EmptyUsername_ReturnsError()
        {
            var (success, error) = await _service.AddUserAsync("   ", "StrongPass123!", "Admin");
            Assert.False(success);
            Assert.Contains("empty", error, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task AddUserAsync_InvalidRole_ReturnsError()
        {
            var (success, error) = await _service.AddUserAsync("user1", "StrongPass123!", "Manager");
            Assert.False(success);
            Assert.Contains("Role", error, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task AddUserAsync_DuplicateUsername_ReturnsError()
        {
            _repoMock.Setup(r => r.GetUserByUsernameAsync("existing"))
                     .ReturnsAsync(new User { Username = "existing" });

            var (success, error) = await _service.AddUserAsync("existing", "StrongPass123!", "Admin");

            Assert.False(success);
            Assert.Contains("existing", error);
        }

        [Fact]
        public async Task ResetPasswordAsync_UserNotFound_ReturnsError()
        {
            _repoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((User?)null);

            var (success, error) = await _service.ResetPasswordAsync(99, "NewSecure123!");

            Assert.False(success);
            Assert.Contains("not found", error, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task ResetPasswordAsync_ShortPassword_ReturnsError()
        {
            var (success, error) = await _service.ResetPasswordAsync(1, "short");
            Assert.False(success);
            Assert.Contains("10", error);
        }

        [Fact]
        public async Task ResetPasswordAsync_ValidPassword_UpdatesUser()
        {
            var user = new User { Id = 1, PasswordHash = "old", Salt = "old", MustChangePassword = true };
            _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);

            var (success, _) = await _service.ResetPasswordAsync(1, "NewSecure123!");

            Assert.True(success);
            Assert.False(user.MustChangePassword);
            Assert.NotEqual("old", user.PasswordHash);
            _repoMock.Verify(r => r.UpdateAsync(user), Times.Once);
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // BUG 4: CategoryService.UpdateCategoryAsync threw generic Exception
    //         instead of KeyNotFoundException for a not-found category.
    // ═══════════════════════════════════════════════════════════════════════════
    public class CategoryServiceUpdateTests
    {
        private readonly Mock<ICategoryRepository> _repoMock = new();
        private readonly CategoryService _service;

        public CategoryServiceUpdateTests()
        {
            _service = new CategoryService(_repoMock.Object);
        }

        [Fact]
        public async Task UpdateCategoryAsync_NotFound_ThrowsKeyNotFoundException()
        {
            // Before fix: threw base Exception. After fix: KeyNotFoundException.
            _repoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Category?)null);

            await Assert.ThrowsAsync<KeyNotFoundException>(
                () => _service.UpdateCategoryAsync(99, "New Name"));
        }

        [Fact]
        public async Task UpdateCategoryAsync_Valid_UpdatesNameAndTrims()
        {
            var cat = new Category { Id = 1, Name = "Old" };
            _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(cat);

            await _service.UpdateCategoryAsync(1, "  Beverages  ");

            Assert.Equal("Beverages", cat.Name);
            _repoMock.Verify(r => r.UpdateAsync(cat), Times.Once);
        }

        [Fact]
        public async Task UpdateCategoryAsync_EmptyName_ThrowsArgumentException()
        {
            await Assert.ThrowsAsync<ArgumentException>(
                () => _service.UpdateCategoryAsync(1, "   "));
        }

        [Fact]
        public async Task UpdateCategoryAsync_InvalidId_ThrowsArgumentException()
        {
            await Assert.ThrowsAsync<ArgumentException>(
                () => _service.UpdateCategoryAsync(0, "Name"));
        }

        [Fact]
        public async Task AddCategoryAsync_Valid_ReturnsId()
        {
            _repoMock.Setup(r => r.AddAsync(It.IsAny<Category>())).ReturnsAsync(5);

            var id = await _service.AddCategoryAsync("Desserts");

            Assert.Equal(5, id);
            _repoMock.Verify(r => r.AddAsync(It.Is<Category>(c => c.Name == "Desserts")), Times.Once);
        }

        [Fact]
        public async Task AddCategoryAsync_EmptyName_ThrowsArgumentException()
        {
            await Assert.ThrowsAsync<ArgumentException>(
                () => _service.AddCategoryAsync("  "));
        }

        [Fact]
        public async Task AddCategoryAsync_TrimsName()
        {
            _repoMock.Setup(r => r.AddAsync(It.IsAny<Category>())).ReturnsAsync(3);

            await _service.AddCategoryAsync("  Starters  ");

            _repoMock.Verify(r => r.AddAsync(It.Is<Category>(c => c.Name == "Starters")), Times.Once);
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Integration: Full OrderService.UpdateOrderAsync round-trip via
    //              InMemory EF to confirm all fields reach the database.
    // ═══════════════════════════════════════════════════════════════════════════
    public class OrderServiceIntegrationUpdateTests : IDisposable
    {
        private readonly HotelDbContext _ctx;
        private readonly OrderRepository _orderRepo;
        private readonly Mock<IItemService> _itemSvcMock = new();
        private readonly Mock<IMediator> _mediatorMock = new();
        private readonly OrderService _orderService;

        public OrderServiceIntegrationUpdateTests()
        {
            var opts = new DbContextOptionsBuilder<HotelDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _ctx = new HotelDbContext(opts);
            _orderRepo = new OrderRepository(_ctx);
            _orderService = new OrderService(_orderRepo, _mediatorMock.Object, _itemSvcMock.Object);
        }

        public void Dispose() => _ctx.Dispose();

        [Fact]
        public async Task UpdateOrderAsync_CustomerFieldsReachDatabase()
        {
            // Seed an initial order
            var original = new Order
            {
                InvoiceNumber = "INV/2526/0001",
                FiscalYear    = "2025-26",
                TableNumber   = 1,
                CreatedAt     = DateTime.UtcNow,
                Subtotal      = 100,
                GstAmount     = 5,
                TotalAmount   = 105,
                PaymentMode   = "Cash",
                Items = new List<OrderItem>
                {
                    new OrderItem { ItemId = 1, ItemName = "Burger", Quantity = 1, Price = 100, TaxPercentage = 5, Total = 100 }
                }
            };
            _ctx.Orders.Add(original);
            await _ctx.SaveChangesAsync();

            // Edit order with customer details
            var updated = new Order
            {
                Id             = original.Id,
                TableNumber    = 2,
                DiscountAmount = 10,
                PaymentMode    = "Card",
                CustomerName   = "Priya Sharma",
                CustomerPhone  = "9123456789",
                CustomerGstin  = "GSTIN_CORP",
                Items = new List<OrderItem>
                {
                    new OrderItem { ItemId = 1, ItemName = "Burger", Quantity = 2, Price = 100, TaxPercentage = 5, Total = 200 }
                }
            };

            await _orderService.UpdateOrderAsync(updated);

            var saved = await _ctx.Orders.Include(o => o.Items).FirstAsync(o => o.Id == original.Id);
            Assert.Equal("Priya Sharma",  saved.CustomerName);
            Assert.Equal("9123456789",    saved.CustomerPhone);
            Assert.Equal("GSTIN_CORP",    saved.CustomerGstin);
            Assert.Equal("Card",          saved.PaymentMode);
            Assert.Equal(2,               saved.TableNumber);
            Assert.Single(saved.Items);
            Assert.Equal(2,               saved.Items[0].Quantity);
        }

        [Fact]
        public async Task UpdateOrderAsync_TotalsRecalculatedAndSaved()
        {
            var original = new Order
            {
                InvoiceNumber = "INV/2526/0002",
                FiscalYear    = "2025-26",
                TableNumber   = 1,
                CreatedAt     = DateTime.UtcNow,
                Subtotal      = 100, GstAmount = 5, TotalAmount = 105, PaymentMode = "Cash",
                Items = new List<OrderItem>
                {
                    new OrderItem { ItemId = 1, ItemName = "Tea", Quantity = 1, Price = 100, TaxPercentage = 5, Total = 100 }
                }
            };
            _ctx.Orders.Add(original);
            await _ctx.SaveChangesAsync();

            var updated = new Order
            {
                Id          = original.Id,
                PaymentMode = "Cash",
                Items = new List<OrderItem>
                {
                    new OrderItem { ItemId = 1, ItemName = "Tea", Quantity = 3, Price = 100, TaxPercentage = 12, Total = 300 }
                }
            };

            await _orderService.UpdateOrderAsync(updated);

            var saved = await _ctx.Orders.FindAsync(original.Id);
            // Subtotal = 300, GST = 12% of 300 = 36, Total = 336
            Assert.Equal(300m, saved!.Subtotal);
            Assert.Equal(36m,  saved.GstAmount);
            Assert.Equal(336m, saved.TotalAmount);
            Assert.Equal(18m,  saved.CgstAmount);
            Assert.Equal(18m,  saved.SgstAmount);
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // BUG 5: When loading an order from the dashboard for editing, PaymentMode,
    //         DiscountAmount, CustomerName, CustomerPhone, CustomerGstin were all
    //         silently dropped because:
    //          (a) RecentOrderRowDto didn't have those fields
    //          (b) ReportService didn't map them
    //          (c) DashboardView.EditOrder_Click didn't copy them to the Order
    // ═══════════════════════════════════════════════════════════════════════════
    public class DashboardEditLoadTests
    {
        // ── RecentOrderRowDto has all needed fields ──────────────────────────

        [Fact]
        public void RecentOrderRowDto_HasPaymentModeField()
        {
            var dto = new RecentOrderRowDto { PaymentMode = "UPI" };
            Assert.Equal("UPI", dto.PaymentMode);
        }

        [Fact]
        public void RecentOrderRowDto_HasDiscountAmountField()
        {
            var dto = new RecentOrderRowDto { DiscountAmount = 50m };
            Assert.Equal(50m, dto.DiscountAmount);
        }

        [Fact]
        public void RecentOrderRowDto_HasCustomerFields()
        {
            var dto = new RecentOrderRowDto
            {
                CustomerName  = "Priya",
                CustomerPhone = "9876543210",
                CustomerGstin = "GSTIN123"
            };
            Assert.Equal("Priya",       dto.CustomerName);
            Assert.Equal("9876543210",  dto.CustomerPhone);
            Assert.Equal("GSTIN123",    dto.CustomerGstin);
        }

        [Fact]
        public void RecentOrderRowDto_DefaultPaymentMode_IsCash()
        {
            var dto = new RecentOrderRowDto();
            Assert.Equal("Cash", dto.PaymentMode);
        }

        // ── ReportService maps all fields correctly ──────────────────────────

        [Fact]
        public async Task ReportService_RecentOrders_MapsPaymentMode()
        {
            var repoMock     = new Mock<IOrderRepository>();
            var itemRepoMock = new Mock<IItemRepository>();
            var catRepoMock  = new Mock<ICategoryRepository>();

            var orders = new List<Order>
            {
                new Order
                {
                    Id          = 1,
                    TableNumber = 3,
                    CreatedAt   = DateTime.UtcNow,
                    PaymentMode = "Card",
                    DiscountAmount = 25m,
                    TotalAmount = 175m,
                    CustomerName  = "Ravi",
                    CustomerPhone = "9123456789",
                    CustomerGstin = "GST_CORP",
                    Items = new List<OrderItem>
                    {
                        new OrderItem { ItemId = 1, ItemName = "Burger", Quantity = 1, Price = 200, Total = 200 }
                    }
                }
            };

            repoMock.Setup(r => r.GetPagedWithItemsAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<int?>()))
                .ReturnsAsync((orders, orders.Count));
            itemRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Item>());
            catRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Category>());

            var service = new ReportService(repoMock.Object, itemRepoMock.Object, catRepoMock.Object);
            var report  = await service.GetSalesReportAsync();

            Assert.Single(report.RecentOrders);
            var row = report.RecentOrders[0];

            Assert.Equal("Card",       row.PaymentMode);
            Assert.Equal(25m,          row.DiscountAmount);
            Assert.Equal("Ravi",       row.CustomerName);
            Assert.Equal("9123456789", row.CustomerPhone);
            Assert.Equal("GST_CORP",   row.CustomerGstin);
        }

        [Fact]
        public async Task ReportService_RecentOrders_NullPaymentMode_DefaultsToCash()
        {
            var repoMock     = new Mock<IOrderRepository>();
            var itemRepoMock = new Mock<IItemRepository>();
            var catRepoMock  = new Mock<ICategoryRepository>();

            var orders = new List<Order>
            {
                new Order
                {
                    Id = 2, TableNumber = 1, CreatedAt = DateTime.UtcNow,
                    PaymentMode = null!,   // simulate old records with no payment mode
                    TotalAmount = 100m,
                    Items = new List<OrderItem>
                    {
                        new OrderItem { ItemId = 1, ItemName = "Tea", Quantity = 1, Price = 100, Total = 100 }
                    }
                }
            };

            repoMock.Setup(r => r.GetPagedWithItemsAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<int?>()))
                .ReturnsAsync((orders, orders.Count));
            itemRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Item>());
            catRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Category>());

            var service = new ReportService(repoMock.Object, itemRepoMock.Object, catRepoMock.Object);
            var report  = await service.GetSalesReportAsync();

            // Null PaymentMode should default to "Cash"
            Assert.Equal("Cash", report.RecentOrders[0].PaymentMode);
        }

        [Fact]
        public async Task ReportService_RecentOrders_MapsAllCustomerFields()
        {
            var repoMock     = new Mock<IOrderRepository>();
            var itemRepoMock = new Mock<IItemRepository>();
            var catRepoMock  = new Mock<ICategoryRepository>();

            var orders = new List<Order>
            {
                new Order
                {
                    Id = 3, TableNumber = 2, CreatedAt = DateTime.UtcNow,
                    PaymentMode    = "UPI",
                    DiscountAmount = 10m,
                    TotalAmount    = 90m,
                    CustomerName   = "Ananya",
                    CustomerPhone  = "9000000001",
                    CustomerGstin  = "GSTIN_A",
                    Items = new List<OrderItem>
                    {
                        new OrderItem { ItemId = 1, ItemName = "Dosa", Quantity = 1, Price = 100, Total = 100 }
                    }
                }
            };

            repoMock.Setup(r => r.GetPagedWithItemsAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<int?>()))
                .ReturnsAsync((orders, orders.Count));
            itemRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Item>());
            catRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Category>());

            var service = new ReportService(repoMock.Object, itemRepoMock.Object, catRepoMock.Object);
            var report  = await service.GetSalesReportAsync();

            var row = report.RecentOrders[0];
            Assert.Equal("UPI",        row.PaymentMode);
            Assert.Equal(10m,          row.DiscountAmount);
            Assert.Equal("Ananya",     row.CustomerName);
            Assert.Equal("9000000001", row.CustomerPhone);
            Assert.Equal("GSTIN_A",    row.CustomerGstin);
        }

        // ── BillingViewModel.LoadOrderForEdit uses all fields correctly ───────

        [Fact]
        public void LoadOrderForEdit_PaymentMode_IsPrePopulated()
        {
            var cartSvc  = new Mock<ICartService>();
            var orderSvc = new Mock<IOrderService>();
            var itemSvc  = new Mock<IItemService>();
            var settSvc  = new Mock<ISettingService>();
            var catSvc   = new Mock<ICategoryService>();
            var notifSvc = new Mock<INotificationService>();
            var cashSvc  = new Mock<ICashService>();

            cartSvc.Setup(s => s.GetHeldOrders()).Returns(new List<HeldOrder>());
            cartSvc.Setup(s => s.GetItems(It.IsAny<int>())).Returns(new List<OrderItem>());

            var vm = new HotelPOS.ViewModels.BillingViewModel(
                itemSvc.Object, cartSvc.Object, orderSvc.Object,
                settSvc.Object, catSvc.Object, notifSvc.Object, cashSvc.Object);

            // This is what DashboardView.EditOrder_Click now builds:
            var order = new Order
            {
                Id             = 42,
                TableNumber    = 5,
                PaymentMode    = "UPI",
                DiscountAmount = 30m,
                CustomerName   = "Ravi Kumar",
                CustomerPhone  = "9876543210",
                CustomerGstin  = "GSTIN_TEST",
                Items = new List<OrderItem>
                {
                    new OrderItem { ItemId = 1, ItemName = "Biryani", Quantity = 2, Price = 150, Total = 300 }
                }
            };

            vm.LoadOrderForEdit(order);

            Assert.Equal("UPI",         vm.PaymentMode);
            Assert.Equal(30m,           vm.DiscountAmount);
            Assert.Equal("Ravi Kumar",  vm.CustomerName);
            Assert.Equal("9876543210",  vm.CustomerPhone);
            Assert.Equal("GSTIN_TEST",  vm.CustomerGstin);
            Assert.Equal(5,             vm.TableNumber);
            Assert.True(vm.IsEditMode);
        }

        [Fact]
        public void LoadOrderForEdit_NullCustomerFields_DoNotCrash()
        {
            var cartSvc  = new Mock<ICartService>();
            var orderSvc = new Mock<IOrderService>();
            var itemSvc  = new Mock<IItemService>();
            var settSvc  = new Mock<ISettingService>();
            var catSvc   = new Mock<ICategoryService>();
            var notifSvc = new Mock<INotificationService>();
            var cashSvc  = new Mock<ICashService>();

            cartSvc.Setup(s => s.GetHeldOrders()).Returns(new List<HeldOrder>());
            cartSvc.Setup(s => s.GetItems(It.IsAny<int>())).Returns(new List<OrderItem>());

            var vm = new HotelPOS.ViewModels.BillingViewModel(
                itemSvc.Object, cartSvc.Object, orderSvc.Object,
                settSvc.Object, catSvc.Object, notifSvc.Object, cashSvc.Object);

            var order = new Order
            {
                Id          = 10,
                TableNumber = 1,
                PaymentMode = "Cash",
                CustomerName = null, CustomerPhone = null, CustomerGstin = null,
                Items = new List<OrderItem>
                {
                    new OrderItem { ItemId = 1, ItemName = "Tea", Quantity = 1, Price = 30, Total = 30 }
                }
            };

            var ex = Record.Exception(() => vm.LoadOrderForEdit(order));
            Assert.Null(ex);
            Assert.Equal("Cash", vm.PaymentMode);
        }
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// Navigation-after-update tests — separate file-level namespace block so
// the class names don't conflict with the outer namespace's classes.
// ─────────────────────────────────────────────────────────────────────────────
namespace HotelPOS.Tests
{
    using HotelPOS.Application;
    using HotelPOS.Application.Interface;
    using HotelPOS.Application.Interfaces;
    using HotelPOS.Domain;
    using Moq;
    using Xunit;

    /// <summary>
    /// Tests that BillingViewModel.OrderUpdated event is raised correctly
    /// after a successful order update, enabling the shell to navigate back
    /// to the Dashboard and refresh it.
    /// </summary>
    public class PostUpdateNavigationTests
    {
        private (HotelPOS.ViewModels.BillingViewModel vm,
                 Mock<IOrderService>                  orderSvc,
                 Mock<ICartService>                   cartSvc,
                 Mock<INotificationService>           notifSvc)
            BuildVm()
        {
            var cartSvc  = new Mock<ICartService>();
            var orderSvc = new Mock<IOrderService>();
            var itemSvc  = new Mock<IItemService>();
            var settSvc  = new Mock<ISettingService>();
            var catSvc   = new Mock<ICategoryService>();
            var notifSvc = new Mock<INotificationService>();
            var cashSvc  = new Mock<ICashService>();

            cartSvc.Setup(s => s.GetHeldOrders()).Returns(new List<HeldOrder>());
            cartSvc.Setup(s => s.GetItems(It.IsAny<int>())).Returns(new List<OrderItem>
            {
                new OrderItem { ItemId = 1, ItemName = "Burger", Quantity = 1, Price = 100, Total = 100 }
            });
            cartSvc.Setup(s => s.GetSubtotal(It.IsAny<int>())).Returns(100m);

            settSvc.Setup(s => s.GetSettingsAsync()).ReturnsAsync(new SystemSetting());
            orderSvc.Setup(s => s.GetAllOrdersWithItemsAsync()).ReturnsAsync(new List<Order>());
            orderSvc.Setup(s => s.UpdateOrderAsync(It.IsAny<Order>())).Returns(Task.CompletedTask);

            var vm = new HotelPOS.ViewModels.BillingViewModel(
                itemSvc.Object, cartSvc.Object, orderSvc.Object,
                settSvc.Object, catSvc.Object, notifSvc.Object, cashSvc.Object);

            return (vm, orderSvc, cartSvc, notifSvc);
        }

        private Order MakeOrder(int id = 10) => new Order
        {
            Id          = id,
            TableNumber = 3,
            PaymentMode = "Card",
            Items = new List<OrderItem>
            {
                new OrderItem { ItemId = 1, ItemName = "Burger", Quantity = 1, Price = 100, Total = 100 }
            }
        };

        // ── Core: event fires after a successful update ───────────────────────

        [Fact]
        public async Task OrderUpdated_FiredExactlyOnce_AfterSuccessfulEditSave()
        {
            var (vm, _, _, _) = BuildVm();
            vm.LoadOrderForEdit(MakeOrder());

            int firedCount = 0;
            vm.OrderUpdated += () => firedCount++;

            await vm.SaveOrderCommand.ExecuteAsync(null);

            Assert.Equal(1, firedCount);
        }

        [Fact]
        public async Task OrderUpdated_NotFired_ForNewOrderSave()
        {
            var (vm, orderSvc, cartSvc, _) = BuildVm();

            // New save (not edit mode): save should work but NOT fire OrderUpdated
            orderSvc.Setup(s => s.SaveOrderAsync(
                It.IsAny<List<OrderItem>>(), It.IsAny<int>(),
                It.IsAny<decimal>(), It.IsAny<string>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>()))
                .ReturnsAsync(99);

            bool fired = false;
            vm.OrderUpdated += () => fired = true;

            // Do NOT call LoadOrderForEdit — this is a fresh order
            await vm.SaveOrderCommand.ExecuteAsync(null);

            Assert.False(fired);
        }

        [Fact]
        public async Task OrderUpdated_NotFired_WhenUpdateFails()
        {
            var (vm, orderSvc, _, _) = BuildVm();
            vm.LoadOrderForEdit(MakeOrder());

            // Make UpdateOrderAsync throw
            orderSvc.Setup(s => s.UpdateOrderAsync(It.IsAny<Order>()))
                    .ThrowsAsync(new InvalidOperationException("DB error"));

            bool fired = false;
            vm.OrderUpdated += () => fired = true;

            await vm.SaveOrderCommand.ExecuteAsync(null);

            Assert.False(fired);
        }

        [Fact]
        public async Task OrderUpdated_NotFired_WhenCartIsEmpty()
        {
            var (vm, _, cartSvc, _) = BuildVm();
            vm.LoadOrderForEdit(MakeOrder());

            // Override: empty cart
            cartSvc.Setup(s => s.GetItems(It.IsAny<int>())).Returns(new List<OrderItem>());

            bool fired = false;
            vm.OrderUpdated += () => fired = true;

            await vm.SaveOrderCommand.ExecuteAsync(null);

            Assert.False(fired);
        }

        [Fact]
        public async Task OrderUpdated_FiredAfterStateIsCleared()
        {
            // The event should fire AFTER IsEditMode=false and DiscountAmount=0
            // so the dashboard can safely refresh without stale edit state
            var (vm, _, _, _) = BuildVm();
            vm.LoadOrderForEdit(MakeOrder());
            vm.DiscountAmount = 75m;

            bool editModeWhenFired    = true;   // set to false proves cleanup ran before event
            decimal discountWhenFired = 999m;

            vm.OrderUpdated += () =>
            {
                editModeWhenFired  = vm.IsEditMode;
                discountWhenFired  = vm.DiscountAmount;
            };

            await vm.SaveOrderCommand.ExecuteAsync(null);

            Assert.False(editModeWhenFired);   // IsEditMode = false before event
            Assert.Equal(0m, discountWhenFired); // DiscountAmount reset before event
        }

        [Fact]
        public async Task OrderUpdated_MultipleSubscribers_AllReceiveEvent()
        {
            var (vm, _, _, _) = BuildVm();
            vm.LoadOrderForEdit(MakeOrder());

            int sub1 = 0, sub2 = 0, sub3 = 0;
            vm.OrderUpdated += () => sub1++;
            vm.OrderUpdated += () => sub2++;
            vm.OrderUpdated += () => sub3++;

            await vm.SaveOrderCommand.ExecuteAsync(null);

            Assert.Equal(1, sub1);
            Assert.Equal(1, sub2);
            Assert.Equal(1, sub3);
        }

        [Fact]
        public async Task OrderUpdated_CancelEdit_DoesNotFireEvent()
        {
            var (vm, _, _, _) = BuildVm();
            vm.LoadOrderForEdit(MakeOrder());

            bool fired = false;
            vm.OrderUpdated += () => fired = true;

            vm.CancelEditCommand.Execute(null);

            Assert.False(fired);
        }

        [Fact]
        public async Task SaveOrderAsync_EditMode_CallsUpdateOrderAsync_BeforeEventFires()
        {
            var (vm, orderSvc, _, _) = BuildVm();
            vm.LoadOrderForEdit(MakeOrder(id: 55));

            bool updateCalledBeforeEvent = false;
            vm.OrderUpdated += () =>
            {
                updateCalledBeforeEvent = orderSvc.Invocations
                    .Any(inv => inv.Method.Name == "UpdateOrderAsync");
            };

            await vm.SaveOrderCommand.ExecuteAsync(null);

            Assert.True(updateCalledBeforeEvent);
        }

        [Fact]
        public async Task SaveOrderAsync_EditMode_ShowsSuccessNotification_BeforeNavigating()
        {
            var (vm, _, _, notifSvc) = BuildVm();
            vm.LoadOrderForEdit(MakeOrder(id: 77));

            bool notificationShownBeforeEvent = false;
            vm.OrderUpdated += () =>
            {
                notificationShownBeforeEvent = notifSvc.Invocations
                    .Any(inv => inv.Method.Name == "ShowSuccess");
            };

            await vm.SaveOrderCommand.ExecuteAsync(null);

            Assert.True(notificationShownBeforeEvent);
        }

        [Fact]
        public async Task SaveOrderAsync_EditMode_CompletesAndNavigates_EvenIfPrintPreviewEnabledInSettings()
        {
            // Verifies that the update flow (which now skips preview) completes 
            // successfully even when the global system setting has Preview=True.
            var (vm, _, _, _) = BuildVm();
            
            // Note: SystemSetting.ShowPrintPreview defaults to True, 
            // so our standard BuildVm() already tests this path.
            
            vm.LoadOrderForEdit(MakeOrder(id: 99));

            bool fired = false;
            vm.OrderUpdated += () => fired = true;

            await vm.SaveOrderCommand.ExecuteAsync(null);

            Assert.True(fired); // Proves the flow reached the end successfully
        }
    }
}
