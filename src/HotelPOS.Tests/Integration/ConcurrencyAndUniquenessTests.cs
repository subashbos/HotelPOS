using HotelPOS.Domain.Entities;
using HotelPOS.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HotelPOS.Tests
{
    /// <summary>
    /// Table-uniqueness tests use a real SQLite database because they exercise a filtered
    /// unique index, which the InMemory provider does not enforce. Order/RawMaterial concurrency
    /// tests use the InMemory provider, which correctly simulates concurrency-token comparisons.
    /// </summary>
    public class ConcurrencyAndUniquenessTests
    {
        private static HotelDbContext CreateSqliteContext()
        {
            var connection = new SqliteConnection("DataSource=:memory:");
            connection.Open();
            var options = new DbContextOptionsBuilder<HotelDbContext>()
                .UseSqlite(connection)
                .Options;
            var context = new HotelDbContext(options);
            context.Database.EnsureCreated();
            return context;
        }

        [Fact]
        public async Task TableRepository_AddAsync_DuplicateActiveNumber_ThrowsFriendlyInvalidOperationException()
        {
            using var context = CreateSqliteContext();
            var repo = new TableRepository(context);
            await repo.AddAsync(new Table { Number = 5, Name = "Table 5", Capacity = 4 });

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => repo.AddAsync(new Table { Number = 5, Name = "Table 5 Duplicate", Capacity = 2 }));

            Assert.Contains("Table number 5", ex.Message);
        }

        [Fact]
        public async Task TableRepository_AddAsync_ReusingNumberFromSoftDeletedTable_Succeeds()
        {
            using var context = CreateSqliteContext();
            var repo = new TableRepository(context);
            var oldId = await repo.AddAsync(new Table { Number = 5, Name = "Old Table 5", Capacity = 4 });
            await repo.DeleteAsync(oldId); // soft delete

            var newId = await repo.AddAsync(new Table { Number = 5, Name = "New Table 5", Capacity = 6 });

            Assert.True(newId > 0);
        }

        [Fact]
        public async Task TableRepository_UpdateAsync_RenumberingToAnActiveTableNumber_ThrowsFriendlyException()
        {
            using var context = CreateSqliteContext();
            var repo = new TableRepository(context);
            await repo.AddAsync(new Table { Number = 1, Name = "Table 1", Capacity = 2 });
            var table2Id = await repo.AddAsync(new Table { Number = 2, Name = "Table 2", Capacity = 2 });

            var table2 = await repo.GetByIdAsync(table2Id);
            table2!.Number = 1; // collides with Table 1

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => repo.UpdateAsync(table2));
            Assert.Contains("Table number 1", ex.Message);
        }

        private static HotelDbContext CreateInMemoryContext(string dbName)
            => new(new DbContextOptionsBuilder<HotelDbContext>().UseInMemoryDatabase(dbName).Options);

        [Fact]
        public async Task OrderRepository_UpdateAsync_StaleRowVersion_ThrowsFriendlyInvalidOperationException()
        {
            var dbName = nameof(OrderRepository_UpdateAsync_StaleRowVersion_ThrowsFriendlyInvalidOperationException);
            using (var context = CreateInMemoryContext(dbName))
            {
                context.Orders.Add(new Order { Id = 1, InvoiceNumber = "INV/2026-27/0001", FiscalYear = "2026-27", TotalAmount = 100 });
                await context.SaveChangesAsync();
            }

            // Simulate two users loading the same order in separate contexts (separate change trackers,
            // same underlying data at read time).
            using var contextA = CreateInMemoryContext(dbName);
            using var contextB = CreateInMemoryContext(dbName);

            var orderA = await contextA.Orders.FirstAsync(o => o.Id == 1);
            var orderB = await contextB.Orders.FirstAsync(o => o.Id == 1);

            var repoA = new OrderRepository(contextA);
            orderA.TotalAmount = 150;
            await repoA.UpdateAsync(orderA); // succeeds, bumps RowVersion

            var repoB = new OrderRepository(contextB);
            orderB.TotalAmount = 200; // orderB still holds the stale RowVersion from before repoA's update

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => repoB.UpdateAsync(orderB));
            Assert.Contains("Order #1", ex.Message);
            Assert.Contains("modified by another user", ex.Message);
        }

        [Fact]
        public async Task BomService_SaveRawMaterialAsync_StaleRowVersion_ThrowsFriendlyInvalidOperationException()
        {
            var dbName = nameof(BomService_SaveRawMaterialAsync_StaleRowVersion_ThrowsFriendlyInvalidOperationException);
            using (var context = CreateInMemoryContext(dbName))
            {
                context.RawMaterials.Add(new RawMaterial { Id = 1, Name = "Chicken", Unit = "kg", CostPerUnit = 200 });
                await context.SaveChangesAsync();
            }

            using var contextA = CreateInMemoryContext(dbName);
            using var contextB = CreateInMemoryContext(dbName);

            var materialA = await contextA.RawMaterials.FirstAsync(r => r.Id == 1);
            var materialB = await contextB.RawMaterials.FirstAsync(r => r.Id == 1);

            var serviceA = new HotelPOS.Services.BomService(contextA);
            materialA.CostPerUnit = 220;
            await serviceA.SaveRawMaterialAsync(materialA); // succeeds, bumps RowVersion

            var serviceB = new HotelPOS.Services.BomService(contextB);
            materialB.CostPerUnit = 250; // materialB still holds the stale RowVersion

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => serviceB.SaveRawMaterialAsync(materialB));
            Assert.Contains("Chicken", ex.Message);
            Assert.Contains("modified by another user", ex.Message);
        }
    }
}
