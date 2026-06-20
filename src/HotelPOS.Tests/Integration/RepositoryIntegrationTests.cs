using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HotelPOS.Domain.Entities;
using HotelPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HotelPOS.Tests
{
    public class RepositoryIntegrationTests
    {
        private HotelDbContext GetContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<HotelDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
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

            var session1 = new CashSession { Id = 1, OpenedBy = "admin", OpenedAt = DateTime.UtcNow.AddHours(-2), Status = "Closed", OpeningBalance = 1000 };
            var session2 = new CashSession { Id = 2, OpenedBy = "cashier", OpenedAt = DateTime.UtcNow, Status = "Open", OpeningBalance = 2000 };

            await repo.AddAsync(session1);
            await repo.AddAsync(session2);

            var current = await repo.GetCurrentSessionAsync();
            Assert.NotNull(current);
            Assert.Equal(2, current.Id);

            current.Status = "Closed";
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
                PaymentType = "Cash",
                PurchaseItems = new List<PurchaseItem> { new PurchaseItem { Id = 1, ItemName = "Onions", Quantity = 100, UnitPrice = 50 } }
            };

            await repo.AddAsync(purchase);

            var suppliers = await repo.GetSuppliersAsync();
            Assert.Single(suppliers);

            var purchases = await repo.GetPurchasesAsync();
            Assert.Single(purchases);

            var paged = await repo.GetPagedPurchasesAsync(1, 10, null, null, null, null, null, null);
            Assert.Equal(1, paged.totalCount);
            Assert.Single(paged.purchases);
        }

        // 7. RoleRepository Tests
        [Fact]
        public async Task RoleRepository_Integration()
        {
            using var context = GetContext("RoleRepoDb");
            var repo = new RoleRepository(context);

            var role = new Role { Id = 10, Name = "Manager", Description = "Store Manager" };
            await repo.AddRoleAsync(role);

            var all = await repo.GetAllRolesAsync();
            Assert.Contains(all, r => r.Name == "Manager");

            var found = await repo.GetRoleByIdAsync(10);
            Assert.NotNull(found);

            var foundByName = await repo.GetRoleByNameAsync("Manager");
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

            var user = new User { Id = 1, Username = "john", Role = "Cashier", IsActive = true, PasswordHash = "hash", Salt = "salt" };
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
    }
}

