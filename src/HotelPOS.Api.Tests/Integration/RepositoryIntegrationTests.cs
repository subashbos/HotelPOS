using HotelPOS.Domain.Common.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HotelPOS.Domain.Entities;
using HotelPOS.Infrastructure.Persistence;
using HotelPOS.Application.Interfaces;
using HotelPOS.Application.UseCases;
using HotelPOS.TestCommon;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;

namespace HotelPOS.Tests
{
    /// <summary>
    /// Ids used here for entities HotelDbContext seeds via HasData (UnitOfMeasurement, Supplier,
    /// SystemSetting) start at 101 / update the existing row rather than reusing 1: the shared
    /// fixture's EnsureCreated() applies that baseline, so low ids would collide with it.
    /// </summary>
    [Collection("SharedDatabase")]
    public class RepositoryIntegrationTests : DatabaseCollectionTestBase
    {
        public RepositoryIntegrationTests(SharedSqliteDatabaseFixture fixture) : base(fixture)
        {
        }

        // 1. AuditRepository Tests
        [Fact]
        public async Task AuditRepository_Integration()
        {
            var context = Context;
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
            var context = Context;
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
            var context = Context;
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
            var context = Context;
            var repo = new ItemRepository(context);

            context.UnitOfMeasurements.Add(new UnitOfMeasurement { Id = 101, Name = "Pcs" });
            await context.SaveChangesAsync();

            var item1 = new Item { Id = 1, Name = "Burger", Price = 150, TaxPercentage = 5, UnitId = 101 };
            var item2 = new Item { Id = 2, Name = "Fries", Price = 80, TaxPercentage = 5, UnitId = 101 };

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
        // (simulating separate concurrent requests) can all see the same data. They intentionally
        // keep their own private connection rather than the shared fixture's, since each needs
        // exclusive control over its own database for the concurrency scenario under test.
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
                context.Items.Add(new Item { Id = 1, Name = "Burger", StockQuantity = 10, UnitId = 1 });
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
                context.Items.Add(new Item { Id = 1, Name = "Burger", StockQuantity = 3, UnitId = 1 });
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
                context.Items.Add(new Item { Id = 1, Name = "Burger", StockQuantity = 0, UnitId = 1 });
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
                seedContext.Items.Add(new Item { Id = 1, Name = "LastUnit", StockQuantity = 10, UnitId = 1 });
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
                seedContext.Items.Add(new Item { Id = 1, Name = "Coffee", Price = 50, StockQuantity = 10, TrackInventory = true, UnitId = 1 });
                seedContext.Items.Add(new Item { Id = 2, Name = "Cake", Price = 80, StockQuantity = 0, TrackInventory = true, UnitId = 1 });
                await seedContext.SaveChangesAsync();
            }

            using var context = new HotelDbContext(options);
            var orderRepo = new OrderRepository(context);
            var itemRepo = new ItemRepository(context);
            var itemService = new ItemService(itemRepo);
            var orderService = new OrderService(orderRepo, mediator: null, itemService, TestCashService.WithOpenSession().Object, TestAuthorization.AllowAll().Object);

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

        // Regression tests for the cash-session open/open TOCTOU race: the DB-level unique
        // filtered index (HasFilter("[Status] = 'Open'") in HotelDbContext) is the actual
        // correctness guarantee, since two concurrent callers can both pass the handler's
        // in-memory GetCurrentSessionAsync() pre-check before either has committed. The
        // InMemory provider doesn't support filtered/relational indexes, so these run against
        // real SQLite like the ItemRepository concurrency tests above.
        [Fact]
        public async Task CashRepository_AddAsync_SecondOpenSession_ThrowsInvalidOperationException()
        {
            var connectionString = $"DataSource=file:{Guid.NewGuid():N}?mode=memory&cache=shared";
            using var anchor = new Microsoft.Data.Sqlite.SqliteConnection(connectionString);
            await anchor.OpenAsync();
            var options = SqliteOptions(connectionString);

            using (var context = new HotelDbContext(options))
            {
                await context.Database.EnsureCreatedAsync();
            }

            using var context1 = new HotelDbContext(options);
            var repo1 = new CashRepository(context1);
            await repo1.AddAsync(new CashSession { OpenedBy = "admin", OpenedAt = DateTime.UtcNow, Status = CashSessionStatuses.Open, OpeningBalance = 1000 });

            using var context2 = new HotelDbContext(options);
            var repo2 = new CashRepository(context2);
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                repo2.AddAsync(new CashSession { OpenedBy = "cashier", OpenedAt = DateTime.UtcNow, Status = CashSessionStatuses.Open, OpeningBalance = 500 }));

            using var verifyContext = new HotelDbContext(options);
            var openSessions = await verifyContext.CashSessions.Where(s => s.Status == CashSessionStatuses.Open).ToListAsync();
            Assert.Single(openSessions);
            Assert.Equal("admin", openSessions[0].OpenedBy);
        }

        [Fact]
        public async Task CashRepository_AddAsync_ConcurrentOpenSessions_OnlyOneSucceeds()
        {
            // 10 concurrent callers each try to open a session, each through its own
            // connection/DbContext against the same shared-cache SQLite database, mirroring
            // separate concurrent requests racing to open the till. Only exactly 1 must succeed.
            var connectionString = $"DataSource=file:{Guid.NewGuid():N}?mode=memory&cache=shared";
            using var anchor = new Microsoft.Data.Sqlite.SqliteConnection(connectionString);
            await anchor.OpenAsync();
            var options = SqliteOptions(connectionString);

            using (var context = new HotelDbContext(options))
            {
                await context.Database.EnsureCreatedAsync();
            }

            var tasks = Enumerable.Range(0, 10).Select(async i =>
            {
                using var context = new HotelDbContext(options);
                var repo = new CashRepository(context);
                try
                {
                    await repo.AddAsync(new CashSession { OpenedBy = $"user{i}", OpenedAt = DateTime.UtcNow, Status = CashSessionStatuses.Open, OpeningBalance = 100 });
                    return true;
                }
                catch (InvalidOperationException)
                {
                    return false;
                }
            });

            var results = await Task.WhenAll(tasks);

            Assert.Equal(1, results.Count(r => r));

            using var verifyContext = new HotelDbContext(options);
            var openSessions = await verifyContext.CashSessions.Where(s => s.Status == CashSessionStatuses.Open).ToListAsync();
            Assert.Single(openSessions);
        }

        // 5. OrderRepository Tests
        [Fact]
        public async Task OrderRepository_Integration()
        {
            var context = Context;
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
            var context = Context;
            var repo = new PurchaseRepository(context);

            var supplier = new Supplier { Id = 101, Name = "Veggies Supplier", Phone = "9876543210" };
            context.Suppliers.Add(supplier);
            // PurchaseItem.ItemId is a required FK; UnitId=1 reuses HotelDbContext's HasData "Pcs" unit.
            context.Items.Add(new Item { Id = 101, Name = "Onions", Price = 50, UnitId = 1 });
            await context.SaveChangesAsync();

            var purchase = new Purchase
            {
                Id = 1,
                SupplierId = 101,
                InvoiceNumber = "PUR123",
                PurchaseDate = DateTime.UtcNow,
                GrandTotal = 5000,
                PaymentType = PaymentModes.Cash,
                PurchaseItems = new List<PurchaseItem> { new PurchaseItem { Id = 1, ItemId = 101, ItemName = "Onions", Quantity = 100, UnitPrice = 50 } }
            };

            await repo.AddAsync(purchase);

            // 4 more suppliers come from HotelDbContext's HasData baseline.
            var suppliers = await repo.GetSuppliersAsync();
            Assert.Contains(suppliers, s => s.Id == 101 && s.Name == "Veggies Supplier");

            var purchases = await repo.GetPurchasesAsync();
            Assert.Single(purchases);

            var paged = await repo.GetPagedPurchasesAsync(1, 10);
            Assert.Equal(1, paged.totalCount);
            Assert.Single(paged.purchases);

            var found = await repo.GetByIdAsync(1);
            Assert.NotNull(found);
            Assert.Single(found.PurchaseItems);

            // A fresh, untracked instance - matching how the real caller (a controller mapping
            // a request DTO) builds the "new desired state" object, rather than mutating the
            // already-tracked entity returned by GetByIdAsync above.
            var updatePayload = new Purchase
            {
                Id = 1,
                SupplierId = 101,
                InvoiceNumber = "PUR123",
                PurchaseDate = purchase.PurchaseDate,
                GrandTotal = 9000,
                PaymentType = PaymentModes.Cash,
                PurchaseItems = new List<PurchaseItem> { new PurchaseItem { ItemId = 101, ItemName = "Onions", Quantity = 200, UnitPrice = 45 } }
            };
            await repo.UpdateAsync(updatePayload);

            var updated = await repo.GetByIdAsync(1);
            Assert.NotNull(updated);
            Assert.Equal(9000, updated.GrandTotal);
            Assert.Single(updated.PurchaseItems);
            Assert.Equal(200, updated.PurchaseItems[0].Quantity);

            await repo.DeleteAsync(1);
            var deleted = await repo.GetByIdAsync(1);
            Assert.Null(deleted);
        }

        [Fact]
        public async Task PurchaseRepository_GetByIdAsync_UnknownId_ReturnsNull()
        {
            var context = Context;
            var repo = new PurchaseRepository(context);

            var result = await repo.GetByIdAsync(999);

            Assert.Null(result);
        }

        [Fact]
        public async Task PurchaseRepository_UpdateAsync_UnknownId_ThrowsKeyNotFoundException()
        {
            var context = Context;
            var repo = new PurchaseRepository(context);

            var purchase = new Purchase { Id = 999, InvoiceNumber = "X", PurchaseItems = new List<PurchaseItem>() };

            await Assert.ThrowsAsync<KeyNotFoundException>(() => repo.UpdateAsync(purchase));
        }

        [Fact]
        public async Task PurchaseRepository_DeleteAsync_UnknownId_IsNoOp()
        {
            var context = Context;
            var repo = new PurchaseRepository(context);

            var exception = await Record.ExceptionAsync(() => repo.DeleteAsync(999));

            Assert.Null(exception);
        }

        // 7. RoleRepository Tests
        [Fact]
        public async Task RoleRepository_Integration()
        {
            var context = Context;
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

        [Fact]
        public async Task RoleRepository_UpdatePermissionsAsync_UpdatesInPlaceAndPreservesRowIds()
        {
            // Regression test: UpdatePermissionsAsync used to delete every row and reinsert with a
            // fresh identity, which could leave stale rows behind on a partial delete and collide
            // with fixed Ids that later EF migrations hardcode for new permission modules
            // (PK_RolePermissions violation on Id 45 after AddTdsPermission). It must now update
            // matching modules in place, add only genuinely new ones, and remove only modules that
            // are no longer present in the incoming set.
            var context = Context;
            var repo = new RoleRepository(context);

            var role = new Role { Id = 20, Name = "PermUpsertRole", Description = "Test" };
            await repo.AddRoleAsync(role);

            await repo.UpdatePermissionsAsync(20, new List<RolePermission>
            {
                new RolePermission { ModuleName = "Billing", CanAccess = true, CanEdit = false, CanDelete = false },
                new RolePermission { ModuleName = "Items", CanAccess = true, CanEdit = true, CanDelete = false }
            });

            var firstSave = await repo.GetPermissionsByRoleIdAsync(20);
            var billingId = firstSave.Single(p => p.ModuleName == "Billing").Id;

            // Second save: "Billing" gets CanEdit/CanDelete promoted, "Items" is dropped, and a
            // brand-new module "Categories" is added.
            await repo.UpdatePermissionsAsync(20, new List<RolePermission>
            {
                new RolePermission { ModuleName = "Billing", CanAccess = true, CanEdit = true, CanDelete = true },
                new RolePermission { ModuleName = "Categories", CanAccess = true, CanEdit = false, CanDelete = false }
            });

            var secondSave = await repo.GetPermissionsByRoleIdAsync(20);
            Assert.Equal(2, secondSave.Count);

            var billing = secondSave.Single(p => p.ModuleName == "Billing");
            Assert.Equal(billingId, billing.Id); // updated in place, not deleted/reinserted
            Assert.True(billing.CanEdit);
            Assert.True(billing.CanDelete);

            Assert.DoesNotContain(secondSave, p => p.ModuleName == "Items"); // removed
            Assert.Contains(secondSave, p => p.ModuleName == "Categories"); // added
        }

        // 8. SettingRepository Tests
        [Fact]
        public async Task SettingRepository_Integration()
        {
            var context = Context;
            var repo = new SettingRepository(context);

            // SystemSetting Id=1 already exists via HotelDbContext's HasData baseline (the shared
            // fixture applies it via EnsureCreated), matching production's real singleton-settings
            // row - so this updates that row rather than adding a fresh one.
            var existing = await repo.GetByIdAsync(1);
            Assert.NotNull(existing);

            existing!.HotelName = "Standard POS Hotel";
            existing.HotelAddress = "Address";
            await repo.UpdateAsync(existing);

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
            var context = Context;
            var repo = new SupplierRepository(context);

            var sup = new Supplier { Id = 101, Name = "Bread Factory", Phone = "9876543210" };
            await repo.AddAsync(sup);

            // 4 more suppliers come from HotelDbContext's HasData baseline.
            var all = await repo.GetAllAsync();
            Assert.Contains(all, s => s.Id == 101 && s.Name == "Bread Factory");

            var found = await repo.GetByIdAsync(101);
            Assert.NotNull(found);

            var foundByName = await repo.GetByNameAsync("Bread Factory");
            Assert.NotNull(foundByName);

            sup.City = "Pune";
            await repo.UpdateAsync(sup);

            var exists = await repo.ExistsByNameAsync("Bread Factory");
            Assert.True(exists);

            await repo.DeleteAsync(101);
            var deleted = await repo.GetByIdAsync(101);
            Assert.Null(deleted);
        }

        // 10. TableRepository Tests
        [Fact]
        public async Task TableRepository_Integration()
        {
            var context = Context;
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
            var context = Context;
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
        public async Task RefreshTokenRepository_RevokeFamilyAsync_RevokesOnlyMatchingUnrevokedTokens()
        {
            var context = Context;
            var repo = new RefreshTokenRepository(context);

            // RefreshToken.UserId is a required FK.
            context.Users.AddRange(
                new User { Id = 1, Username = "reftok-user1", Role = RoleNames.Cashier, PasswordHash = "hash", Salt = "salt" },
                new User { Id = 2, Username = "reftok-user2", Role = RoleNames.Cashier, PasswordHash = "hash", Salt = "salt" });
            await context.SaveChangesAsync();

            var familyId = Guid.NewGuid();
            var otherFamilyId = Guid.NewGuid();
            var activeInFamily = new RefreshToken { Id = 1, UserId = 1, FamilyId = familyId, TokenHash = "a", ExpiresUtc = DateTime.UtcNow.AddDays(1) };
            var alreadyRevokedInFamily = new RefreshToken { Id = 2, UserId = 1, FamilyId = familyId, TokenHash = "b", ExpiresUtc = DateTime.UtcNow.AddDays(1), RevokedUtc = DateTime.UtcNow.AddDays(-1) };
            var differentFamily = new RefreshToken { Id = 3, UserId = 2, FamilyId = otherFamilyId, TokenHash = "c", ExpiresUtc = DateTime.UtcNow.AddDays(1) };

            await repo.AddAsync(activeInFamily);
            await repo.AddAsync(alreadyRevokedInFamily);
            await repo.AddAsync(differentFamily);

            var previousRevokedUtc = alreadyRevokedInFamily.RevokedUtc;
            await repo.RevokeFamilyAsync(familyId, DateTime.UtcNow);

            Assert.NotNull(activeInFamily.RevokedUtc);
            Assert.Equal(previousRevokedUtc, alreadyRevokedInFamily.RevokedUtc); // untouched, was already revoked
            Assert.Null(differentFamily.RevokedUtc); // different family, must be unaffected
        }

        [Fact]
        public async Task LoginLockoutRepository_Integration()
        {
            var context = Context;
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

        // Exercises OrderRepository's own Begin/Commit/Rollback transaction pass-through, which
        // would conflict with the shared fixture's per-test ambient transaction (SQLite only
        // supports one active transaction per connection at a time) - so this test keeps its own
        // private, unwrapped context instead of the inherited Context.
        private static HotelDbContext GetIsolatedContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<HotelDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;
            return new HotelDbContext(options);
        }

        [Fact]
        public async Task OrderRepository_Integration_Extended()
        {
            using var context = GetIsolatedContext(nameof(OrderRepository_Integration_Extended));
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
