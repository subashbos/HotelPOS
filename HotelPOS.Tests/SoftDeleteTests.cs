using HotelPOS.Domain;
using HotelPOS.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HotelPOS.Tests
{
    public class SoftDeleteTests
    {
        private HotelDbContext GetContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<HotelDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;
            return new HotelDbContext(options);
        }

        [Fact]
        public async Task GetAllWithItemsAsync_FiltersOutDeletedOrders()
        {
            using var context = GetContext("SoftDelete_GetAll");
            var repo = new OrderRepository(context);

            context.Orders.Add(new Order { Id = 1, InvoiceNumber = "INV/1", IsDeleted = false, CreatedAt = DateTime.UtcNow });
            context.Orders.Add(new Order { Id = 2, InvoiceNumber = "INV/2", IsDeleted = true, CreatedAt = DateTime.UtcNow });
            await context.SaveChangesAsync();

            var results = await repo.GetAllWithItemsAsync();

            Assert.Single(results);
            Assert.Equal(1, results[0].Id);
        }

        [Fact]
        public async Task GetPagedWithItemsAsync_FiltersOutDeletedOrders()
        {
            using var context = GetContext("SoftDelete_Paged");
            var repo = new OrderRepository(context);

            context.Orders.Add(new Order { Id = 1, IsDeleted = false, CreatedAt = DateTime.UtcNow });
            context.Orders.Add(new Order { Id = 2, IsDeleted = true, CreatedAt = DateTime.UtcNow });
            await context.SaveChangesAsync();

            var (items, total) = await repo.GetPagedWithItemsAsync(1, 10);

            Assert.Equal(1, total);
            Assert.Single(items);
            Assert.Equal(1, items[0].Id);
        }

        [Fact]
        public async Task DeleteAsync_SetsIsDeletedTrue()
        {
            using var context = GetContext("SoftDelete_Action");
            var repo = new OrderRepository(context);

            var order = new Order { Id = 1, IsDeleted = false, CreatedAt = DateTime.UtcNow };
            context.Orders.Add(order);
            await context.SaveChangesAsync();

            await repo.DeleteAsync(1);

            var result = await context.Orders.FindAsync(1);
            Assert.True(result?.IsDeleted);
            Assert.NotNull(result?.DeletedAt);
        }
    }
}
