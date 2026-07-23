using HotelPOS.Domain.Common.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HotelPOS.Domain.Entities;
using HotelPOS.Infrastructure.Persistence;
using HotelPOS.Application.Interfaces;
using HotelPOS.Application.UseCases;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;

namespace HotelPOS.Tests
{
    public class RepositoryIntegrationTests
    {
        private HotelDbContext GetContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<HotelDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;
            return new HotelDbContext(options);
        }

        // 1. AuditRepository Tests
        [Fact]
        public async Task AuditRepository_Integration()
        {
            using var context = GetContext("AuditRepoDb");
            var repo = new AuditRepository(context);

            var log1 = new AuditLog { Id = 1, Username = "user1", Action = "Create", Details = "Details 1", Timestamp = DateTime.UtcNow.AddMinutes(-10) };
            var log2 = new AuditLog { Id = 2, Username = "user2", Action = "Delete", Details = "Details 2", Timestamp = DateTime.UtcNow };

            await repo.AddAsync(log1);
            await repo.AddAsync(log2);

            var logs = await repo.GetLogsAsync(DateTime.UtcNow.AddMinutes(-15), DateTime.UtcNow.AddMinutes(5));
            Assert.Equal(2, logs.Count);
            Assert.Equal("Delete", logs[0].Action); // OrderByDescending timestamp
        }

        // 2. CashRepository Tests
        [Fact]
        public async Task CashRepository_Integration()
        {
            using var context = GetContext("CashRepoDb");
            var repo = new CashRepository(context);

            var session1 = new CashSession { Id = 1, OpenedBy = "admin", OpenedAt = DateTime.UtcNow.AddHours(-2), Status = CashSessionStatuses.Closed, OpeningBalance = 1000 };
            var session2 = new CashSession { Id = 2, OpenedBy = "cashier", OpenedAt = DateTime.UtcNow, Status = CashSessionStatuses.Open, OpeningBalance = 2000 };

            await repo.AddAsync(session1);
            await repo.AddAsync(session2);

            var current = await repo.GetCurrentSessionAsync();
            Assert.NotNull(current);
            Assert.Equal(2, current.Id);

            current.Status = CashSessionStatuses.Closed;
            current.ClosedAt = DateTime.UtcNow;
            await repo.UpdateAsync(current);

            var history = await repo.GetHistoryAsync(5);
            Assert.Equal(2, history.Count);

            context.Orders.Add(new Order { Id = 10, TotalAmount = 500, CreatedAt = DateTime.UtcNow.AddMinutes(-5), InvoiceNumber = "INV/A/1" });
            context.Orders.Add(new Order { Id = 11, TotalAmount = 300, CreatedAt = DateTime.UtcNow.AddMinutes(-15), InvoiceNumber = "INV/A/2" });
            await context.SaveChangesAsync();

            var salesTotal = await repo.GetSalesTotalAsync(DateTime.UtcNow.AddMinutes(-10));
            Assert.Equal(500, salesTotal);
        }

        // 3. CategoryRepository Tests
        [Fact]
        public async Task CategoryRepository_Integration()
        {
            using var context = GetContext("CategoryRepoDb");
            var repo = new CategoryRepository(context);

            var cat1 = new Category { Id = 1, Name = "Drinks" };
            var cat2 = new Category { Id = 2, Name = "Appetizers" };

            await repo.AddAsync(cat1);
            await repo.AddAsync(cat2);

            var all = await repo.GetAllAsync();
            Assert.Equal(2, all.Count);
            Assert.Equal("Appetizers", all[0].Name); // OrderBy Name

            var found = await repo.GetByIdAsync(1);
            Assert.NotNull(found);
            Assert.Equal("Drinks", found.Name);

            found.Name = "Soft Drinks";
            await repo.UpdateAsync(found);

            await repo.DeleteAsync(2);
            var remaining = await repo.GetAllAsync();
            Assert.Single(remaining);
            Assert.Equal("Soft Drinks", remaining[0].Name);
        }

        // 4. ItemRepository Tests
        [Fact]
        public async Task ItemRepository_Integration()
        {
            using var context = GetContext("ItemRepoDb");
            var repo = new ItemRepository(context);

            var item1 = new Item { Id = 1, Name = "Burger", Price = 150, TaxPercentage = 5 };
            var item2 = new Item { Id = 2, Name = "Fries", Price = 80, TaxPercentage = 5 };

            await repo.AddAsync(item1);
            await repo.AddAsync(item2);

            var all = await repo.GetAllAsync();
            Assert.Equal(2, all.Count);

            var found = await repo.GetByIdAsync(item1.Id);
            Assert.NotNull(found);
            Assert.Equal("Burger", found.Name);

            found.Price = 160;
            await repo.UpdateAsync(found);

            await repo.DeleteAsync(item2.Id);
            var remaining = await repo.GetAllAsync();
            Assert.Single(remaining);
        }

        // TryDeductStockAsync uses EF Core's ExecuteUpdateAsync, which the InMemory provider (used
        // by every other test in this file) doesn't support at all. These tests run against real
        // SQLite instead — a shared-cache in-memory database keyed by a unique name per test, kept
        // alive by one open "anchor" connection, so multiple independent DbContext/connection pairs
        // (simulating separate concurrent requests) can all see the same data.
        private static DbContextOptions<HotelDbContext> SqliteOptions(string connectionString) =>
            new DbContextOptionsBuilder<HotelDbContext>().UseSqlite(connectionString).Options;

        [Fact]
        public async Task ItemRepository_TryDeductStockAsync_SufficientStock_DecrementsAndReturnsTrue()
        {
            var connectionString = $"DataSource=file:{Guid.NewGuid():N}?mode=memory&cache=shared";
            using var anchor = new Microsoft.Data.Sqlite.SqliteConnection(connectionString);
            await anchor.OpenAsync();
            var options = SqliteOptions(connectionString);

            using (var context = new HotelDbContext(options))
            {
                await context.Database.EnsureCreatedAsync();
                context.Items.Add(new Item { Id = 1, Name = "Burger", StockQuantity = 10 });
                await context.SaveChangesAsync();
            }

            using var repoContext = new HotelDbContext(options);
            var repo = new ItemRepository(repoContext);
            var result = await repo.TryDeductStockAsync(1, 4);

            Assert.True(result);
            var item = await repo.GetByIdAsync(1);
            Assert.Equal(6, item!.StockQuantity);
        }

        [Fact]
        public async Task ItemRepository_TryDeductStockAsync_InsufficientStock_LeavesStockUnchangedAndReturnsFalse()
        {
            var connectionString = $"DataSource=file:{Guid.NewGuid():N}?mode=memory&cache=shared";
            using var anchor = new Microsoft.Data.Sqlite.SqliteConnection(connectionString);
            await anchor.OpenAsync();
            var options = SqliteOptions(connectionString);

            using (var context = new HotelDbContext(options))
            {
                await context.Database.EnsureCreatedAsync();
                context.Items.Add(new Item { Id = 1, Name = "Burger", StockQuantity = 3 });
                await context.SaveChangesAsync();
            }

            using var repoContext = new HotelDbContext(options);
            var repo = new ItemRepository(repoContext);
            var result = await repo.TryDeductStockAsync(1, 10);

            Assert.False(result);
            var item = await repo.GetByIdAsync(1);
            Assert.Equal(3, item!.StockQuantity); // unchanged, not partially deducted
        }

        [Fact]
        public async Task ItemRepository_TryDeductStockAsync_NegativeQuantity_AlwaysSucceeds()
        {
            var connectionString = $"DataSource=file:{Guid.NewGuid():N}?mode=memory&cache=shared";
            using var anchor = new Microsoft.Data.Sqlite.SqliteConnection(connectionString);
            await anchor.OpenAsync();
            var options = SqliteOptions(connectionString);

            using (var context = new HotelDbContext(options))
            {
                await context.Database.EnsureCreatedAsync();
                context.Items.Add(new Item { Id = 1, Name = "Burger", StockQuantity = 0 });
                await context.SaveChangesAsync();
            }

            using var repoContext = new HotelDbContext(options);
            var repo = new ItemRepository(repoContext);

            // Returning stock (void/refund) must succeed even from zero.
            var result = await repo.TryDeductStockAsync(1, -5);

            Assert.True(result);
            var item = await repo.GetByIdAsync(1);
            Assert.Equal(5, item!.StockQuantity);
        }

        [Fact]
        public async Task ItemRepository_TryDeductStockAsync_UnknownItem_ReturnsFalse()
        {
            var connectionString = $"DataSource=file:{Guid.NewGuid():N}?mode=memory&cache=shared";
            using var anchor = new Microsoft.Data.Sqlite.SqliteConnection(connectionString);
            await anchor.OpenAsync();
            var options = SqliteOptions(connectionString);

            using (var context = new HotelDbContext(options))
            {
                await context.Database.EnsureCreatedAsync();
            }

            using var repoContext = new HotelDbContext(options);
            var repo = new ItemRepository(repoContext);
            var result = await repo.TryDeductStockAsync(999, 1);

            Assert.False(result);
        }

        [Fact]
        public async Task ItemRepository_TryDeductStockAsync_ConcurrentDeductions_NeverOversells()
        {
            // Regression test for the check-then-act overselling race: 20 concurrent callers each
            // try to take 1 unit from a stock of 10, each through its own connection/DbContext
            // against the same shared-cache SQLite database, mirroring separate concurrent requests
            // sharing one real database. Only exactly 10 should succeed, and stock must never go
            // negative or be under/over-deducted.
            var connectionString = $"DataSource=file:{Guid.NewGuid():N}?mode=memory&cache=shared";
            using var anchor = new Microsoft.Data.Sqlite.SqliteConnection(connectionString);
            await anchor.OpenAsync();
            var options = SqliteOptions(connectionString);

            using (var seedContext = new HotelDbContext(options))
            {
                await seedContext.Database.EnsureCreatedAsync();
                seedContext.Items.Add(new Item { Id = 1, Name = "LastUnit", StockQuantity = 10 });
                await seedContext.SaveChangesAsync();
            }

            var tasks = Enumerable.Range(0, 20).Select(async _ =>
            {
                using var context = new HotelDbContext(options);
                var repo = new ItemRepository(context);
                return await repo.TryDeductStockAsync(1, 1);
            });

            var results = await Task.WhenAll(tasks);

            Assert.Equal(10, results.Count(r => r));
            Assert.Equal(10, results.Count(r => !r));

            using var verifyContext = new HotelDbContext(options);
            var finalItem = await verifyContext.Items.FindAsync(1);
            Assert.Equal(0, finalItem!.StockQuantity);
        }

        [Fact]
        public async Task OrderService_SaveOrder_InsufficientStockOnSecondItem_RollsBackFirstItemsAtomicDeduction()
        {
            // End-to-end proof that TryDeductStockAsync's ExecuteUpdateAsync (which bypasses EF's
            // normal change tracking) still participates correctly in OrderService's ambient
            // transaction: item A's stock must NOT stay decremented once item B fails and the
            // whole order rolls back.
            var connectionString = $"DataSource=file:{Guid.NewGuid():N}?mode=memory&cache=shared";
            using var anchor = new Microsoft.Data.Sqlite.SqliteConnection(connectionString);
            await anchor.OpenAsync();
            var options = SqliteOptions(connectionString);

            using (var seedContext = new HotelDbContext(options))
            {
                await seedContext.Database.EnsureCreatedAsync();
                seedContext.Items.Add(new Item { Id = 1, Name = "Coffee", Price = 50, StockQuantity = 10, TrackInventory = true });
                seedContext.Items.Add(new Item { Id = 2, Name = "Cake", Price = 80, StockQuantity = 0, TrackInventory = true });
                await seedContext.SaveChangesAsync();
            }

            using var context = new HotelDbContext(options);
            var orderRepo = new OrderRepository(context);
            var itemRepo = new ItemRepository(context);
            var itemService = new ItemService(itemRepo);
            var orderService = new OrderService(orderRepo, mediator: null, itemService);

            var request = new SaveOrderRequest(
                new List<OrderItem>
                {
                    new OrderItem { ItemId = 1, ItemName = "Coffee", Quantity = 2 },
                    new OrderItem { ItemId = 2, ItemName = "Cake", Quantity = 5 } // exceeds available stock (0)
                },
                TableNumber: 1);

            await Assert.ThrowsAsync<InvalidOperationException>(() => orderService.SaveOrderAsync(request));

            using var verifyContext = new HotelDbContext(options);
            var coffee = await verifyContext.Items.FindAsync(1);
            Assert.Equal(10, coffee!.StockQuantity); // must be rolled back, not left at 8

            var orders = await verifyContext.Orders.ToListAsync();
            Assert.Empty(orders); // the order itself must not have been persisted either
        }

        // 5. OrderRepository Tests
        [Fact]
        public async Task OrderRepository_Integration()
        {
            using var context = GetContext("OrderRepoDb");
            var repo = new OrderRepository(context);

            var order = new Order
            {
                Id = 1,
                InvoiceNumber = "INV/2026-27/0001",
                FiscalYear = "2026-27",
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false,
                TotalAmount = 300,
                Items = new List<OrderItem> { new OrderItem { Id = 1, ItemName = "Steak", Quantity = 1, Price = 300 } }
            };

            var nextInvoice = await repo.GetNextInvoiceNumberAsync("2026-27");
            Assert.Equal("INV/2026-27/0001", nextInvoice);

            await repo.AddAsync(order);

            var nextInvoiceAfterAdd = await repo.GetNextInvoiceNumberAsync("2026-27");
            Assert.Equal("INV/2026-27/0002", nextInvoiceAfterAdd);

            var found = await repo.GetByIdWithItemsAsync(1);
            Assert.NotNull(found);
            Assert.Single(found.Items);

            found.TotalAmount = 350;
            await repo.UpdateAsync(found);

            var all = await repo.GetAllWithItemsAsync();
            Assert.Single(all);

            var paged = await repo.GetPagedWithItemsAsync(1, 10);
            Assert.Equal(1, paged.TotalCount);

            await repo.DeleteAsync(1);
            var deleted = await repo.GetByIdWithItemsAsync(1);
            Assert.Null(deleted);
        }

        // 6. PurchaseRepository Tests
        [Fact]
        public async Task PurchaseRepository_Integration()
        {
            using var context = GetContext("PurchaseRepoDb");
            var repo = new PurchaseRepository(context);

            var supplier = new Supplier { Id = 1, Name = "Veggies Supplier", Phone = "9876543210" };
            context.Suppliers.Add(supplier);
            await context.SaveChangesAsync();

            var purchase = new Purchase
            {
                Id = 1,
                SupplierId = 1,
                InvoiceNumber = "PUR123",
                PurchaseDate = DateTime.UtcNow,
                GrandTotal = 5000,
                PaymentType = PaymentModes.Cash,
                PurchaseItems = new List<PurchaseItem> { new PurchaseItem { Id = 1, ItemName = "Onions", Quantity = 100, UnitPrice = 50 } }
            };

            await repo.AddAsync(purchase);

            var suppliers = await repo.GetSuppliersAsync();
            Assert.Single(suppliers);

            var purchases = await repo.GetPurchasesAsync();
            Assert.Single(purchases);

            var paged = await repo.GetPagedPurchasesAsync(1, 10);
            Assert.Equal(1, paged.totalCount);
            Assert.Single(paged.purchases);
        }

        // 7. RoleRepository Tests
        [Fact]
        public async Task RoleRepository_Integration()
        {
            using var context = GetContext("RoleRepoDb");
            var repo = new RoleRepository(context);

            var role = new Role { Id = 10, Name = RoleNames.Manager, Description = "Store Manager" };
            await repo.AddRoleAsync(role);

            var all = await repo.GetAllRolesAsync();
            Assert.Contains(all, r => r.Name == RoleNames.Manager);

            var found = await repo.GetRoleByIdAsync(10);
            Assert.NotNull(found);

            var foundByName = await repo.GetRoleByNameAsync(RoleNames.Manager);
            Assert.NotNull(foundByName);

            found.Description = "Updated Manager";
            await repo.UpdateRoleAsync(found);

            var perms = new List<RolePermission> { new RolePermission { ModuleName = "Billing", CanAccess = true } };
            await repo.UpdatePermissionsAsync(10, perms);

            var savedPerms = await repo.GetPermissionsByRoleIdAsync(10);
            Assert.Single(savedPerms);

            await repo.DeleteRoleAsync(10);
            var deleted = await repo.GetRoleByIdAsync(10);
            Assert.Null(deleted);
        }

        // 8. SettingRepository Tests
        [Fact]
        public async Task SettingRepository_Integration()
        {
            using var context = GetContext("SettingRepoDb");
            var repo = new SettingRepository(context);

            var setting = new SystemSetting { Id = 1, HotelName = "Standard POS Hotel", HotelAddress = "Address" };
            await repo.AddAsync(setting);

            var found = await repo.GetByIdAsync(1);
            Assert.NotNull(found);
            Assert.Equal("Standard POS Hotel", found.HotelName);

            found.HotelName = "New Name";
            await repo.UpdateAsync(found);

            var updated = await repo.GetByIdAsync(1);
            Assert.Equal("New Name", updated?.HotelName);
        }

        // 9. SupplierRepository Tests
        [Fact]
        public async Task SupplierRepository_Integration()
        {
            using var context = GetContext("SupplierRepoDb");
            var repo = new SupplierRepository(context);

            var sup = new Supplier { Id = 1, Name = "Bread Factory", Phone = "9876543210" };
            await repo.AddAsync(sup);

            var all = await repo.GetAllAsync();
            Assert.Single(all);

            var found = await repo.GetByIdAsync(1);
            Assert.NotNull(found);

            var foundByName = await repo.GetByNameAsync("Bread Factory");
            Assert.NotNull(foundByName);

            sup.City = "Pune";
            await repo.UpdateAsync(sup);

            var exists = await repo.ExistsByNameAsync("Bread Factory");
            Assert.True(exists);

            await repo.DeleteAsync(1);
            var deleted = await repo.GetByIdAsync(1);
            Assert.Null(deleted);
        }

        // 10. TableRepository Tests
        [Fact]
        public async Task TableRepository_Integration()
        {
            using var context = GetContext("TableRepoDb");
            var repo = new TableRepository(context);

            var table = new Table { Id = 1, Number = 5, Capacity = 4, IsDeleted = false };
            await repo.AddAsync(table);

            var all = await repo.GetAllAsync();
            Assert.Single(all);

            var found = await repo.GetByIdAsync(1);
            Assert.NotNull(found);

            table.Capacity = 6;
            await repo.UpdateAsync(table);

            await repo.DeleteAsync(1);
            var deleted = await repo.GetByIdAsync(1);
            Assert.Null(deleted);
        }

        // 11. UserRepository Tests
        [Fact]
        public async Task UserRepository_Integration()
        {
            using var context = GetContext("UserRepoDb");
            var repo = new UserRepository(context);

            var user = new User { Id = 1, Username = "john", Role = RoleNames.Cashier, IsActive = true, PasswordHash = "hash", Salt = "salt" };
            await repo.AddAsync(user);

            var all = await repo.GetAllAsync();
            Assert.Single(all);

            var found = await repo.GetByIdAsync(1);
            Assert.NotNull(found);

            var foundByUsername = await repo.GetUserByUsernameAsync("john");
            Assert.NotNull(foundByUsername);

            user.Username = "john_doe";
            await repo.UpdateAsync(user);

            await repo.DeleteAsync(1);
            var softDeleted = await repo.GetByIdAsync(1);
            Assert.NotNull(softDeleted);
            Assert.False(softDeleted.IsActive);
        }

        [Fact]
        public async Task LoginLockoutRepository_Integration()
        {
            using var context = GetContext("LoginLockoutRepoDb");
            var repo = new LoginLockoutRepository(context);

            var lockout = new LoginLockout
            {
                NormalizedUsername = "ADMIN",
                FailedAttempts = 3,
                LockedUntilUtc = DateTime.UtcNow.AddMinutes(15),
                LastAttemptUtc = DateTime.UtcNow
            };

            await repo.SaveAsync(lockout);

            var retrieved = await repo.GetAsync("ADMIN");
            Assert.NotNull(retrieved);
            Assert.Equal(3, retrieved.FailedAttempts);

            // Update existing
            lockout.FailedAttempts = 5;
            await repo.SaveAsync(lockout);
            
            retrieved = await repo.GetAsync("ADMIN");
            Assert.Equal(5, retrieved!.FailedAttempts);

            await repo.ClearAsync("ADMIN");
            retrieved = await repo.GetAsync("ADMIN");
            Assert.Null(retrieved);
        }

        [Fact]
        public async Task OrderRepository_Integration_Extended()
        {
            using var context = GetContext("OrderRepoExtendedDb");
            var repo = new OrderRepository(context);

            var order = new Order
            {
                Id = 1,
                InvoiceNumber = "INV/2026-27/0001",
                FiscalYear = "2026-27",
                TotalAmount = 500,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false,
                Items = new List<OrderItem>
                {
                    new OrderItem { ItemId = 10, ItemName = "Burger", Quantity = 2, Price = 100, Total = 200 }
                }
            };

            await repo.AddAsync(order);

            // Get next invoice
            var nextInvoice = await repo.GetNextInvoiceNumberAsync("2026-27");
            Assert.Equal("INV/2026-27/0002", nextInvoice);

            // Get by ID with items
            var retrieved = await repo.GetByIdWithItemsAsync(1);
            Assert.NotNull(retrieved);
            Assert.Single(retrieved.Items);

            // Get paged
            var filter = new OrderQueryFilter(Search: "INV/2026-27");
            var (items, total) = await repo.GetPagedWithItemsAsync(1, 10, filter);
            Assert.Single(items);
            Assert.Equal(1, total);

            // Begin/Commit/Rollback transactions
            await repo.BeginTransactionAsync();
            await repo.CommitTransactionAsync();
            await repo.BeginTransactionAsync();
            await repo.RollbackTransactionAsync();

            // Delete
            await repo.DeleteAsync(1);
            var deleted = await repo.GetByIdWithItemsAsync(1);
            Assert.Null(deleted);
        }
    }
}

