using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Entities;
using HotelPOS.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HotelPOS.Tests.Integration
{
    /// <summary>
    /// HeldOrderRepository speaks raw SQL against the HeldOrders table (which the WPF app
    /// creates at startup, outside the EF model), so these tests run on real SQLite with
    /// the same table shape instead of the InMemory provider.
    /// </summary>
    public sealed class HeldOrderRepositoryTests : IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly HotelDbContext _context;
        private readonly HeldOrderRepository _repo;

        public HeldOrderRepositoryTests()
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            var options = new DbContextOptionsBuilder<HotelDbContext>()
                .UseSqlite(_connection)
                .Options;
            _context = new HotelDbContext(options);

            using var cmd = _connection.CreateCommand();
            cmd.CommandText = @"CREATE TABLE HeldOrders (
                Id TEXT PRIMARY KEY,
                HoldName TEXT NOT NULL,
                HeldAt TEXT NOT NULL,
                TableNumber INTEGER NOT NULL,
                SerializedItems TEXT NOT NULL)";
            cmd.ExecuteNonQuery();

            _repo = new HeldOrderRepository(_context);
        }

        public void Dispose()
        {
            _context.Dispose();
            _connection.Dispose();
        }

        private static HeldOrder NewHeldOrder(string name, int table = 4) => new()
        {
            HoldName = name,
            HeldAt = new DateTime(2026, 7, 13, 12, 30, 0),
            TableNumber = table,
            Items = new List<OrderItem>
            {
                new() { ItemId = 1, ItemName = "Burger", Quantity = 2, Price = 100m, Total = 200m }
            }
        };

        [Fact]
        public async Task SaveAsync_NewOrder_InsertsAndRoundTripsItems()
        {
            var held = NewHeldOrder("Table 4 - Alice");

            await _repo.SaveAsync(held);

            var all = await _repo.GetAllAsync();
            var restored = Assert.Single(all);
            Assert.Equal(held.Id, restored.Id);
            Assert.Equal("Table 4 - Alice", restored.HoldName);
            Assert.Equal(held.HeldAt, restored.HeldAt);
            Assert.Equal(4, restored.TableNumber);
            var item = Assert.Single(restored.Items);
            Assert.Equal("Burger", item.ItemName);
            Assert.Equal(2, item.Quantity);
            Assert.Equal(200m, item.Total);
        }

        [Fact]
        public async Task SaveAsync_ExistingOrder_UpdatesInPlace()
        {
            var held = NewHeldOrder("Original");
            await _repo.SaveAsync(held);

            held.HoldName = "Renamed";
            held.TableNumber = 9;
            held.Items.Add(new OrderItem { ItemId = 2, ItemName = "Coke", Quantity = 1, Price = 40m, Total = 40m });
            await _repo.SaveAsync(held);

            var all = await _repo.GetAllAsync();
            var restored = Assert.Single(all); // updated, not duplicated
            Assert.Equal("Renamed", restored.HoldName);
            Assert.Equal(9, restored.TableNumber);
            Assert.Equal(2, restored.Items.Count);
        }

        [Fact]
        public async Task DeleteAsync_RemovesOnlyThatOrder()
        {
            var first = NewHeldOrder("First");
            var second = NewHeldOrder("Second", table: 7);
            await _repo.SaveAsync(first);
            await _repo.SaveAsync(second);

            await _repo.DeleteAsync(first.Id);

            var all = await _repo.GetAllAsync();
            var remaining = Assert.Single(all);
            Assert.Equal(second.Id, remaining.Id);
        }

        [Fact]
        public async Task ClearAllAsync_EmptiesTable()
        {
            await _repo.SaveAsync(NewHeldOrder("First"));
            await _repo.SaveAsync(NewHeldOrder("Second"));

            await _repo.ClearAllAsync();

            Assert.Empty(await _repo.GetAllAsync());
        }

        [Fact]
        public async Task GetAllAsync_EmptyTable_ReturnsEmptyList()
        {
            Assert.Empty(await _repo.GetAllAsync());
        }
    }
}
