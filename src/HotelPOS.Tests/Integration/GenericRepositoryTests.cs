using HotelPOS.Domain.Entities;
using HotelPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;

namespace HotelPOS.Tests.Integration
{
    /// <summary>
    /// Exercises GenericRepository&lt;T&gt; directly (via Category, whose repository
    /// only overrides GetAllAsync) so the shared base CRUD implementation is
    /// verified independently of any subclass override.
    /// </summary>
    public class GenericRepositoryTests
    {
        private static HotelDbContext GetContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<HotelDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;
            return new HotelDbContext(options);
        }

        [Fact]
        public async Task GetAllAsync_OnEmptySet_ReturnsEmptyList()
        {
            using var context = GetContext(nameof(GetAllAsync_OnEmptySet_ReturnsEmptyList));
            var repo = new GenericRepository<Category>(context);

            var result = await repo.GetAllAsync();

            Assert.Empty(result);
        }

        [Fact]
        public async Task AddAsync_PersistsEntity_AndAssignsId()
        {
            using var context = GetContext(nameof(AddAsync_PersistsEntity_AndAssignsId));
            var repo = new GenericRepository<Category>(context);

            var added = await repo.AddAsync(new Category { Name = "Snacks" });

            Assert.NotEqual(0, added.Id);
            var all = await repo.GetAllAsync();
            Assert.Single(all);
            Assert.Equal("Snacks", all[0].Name);
        }

        [Fact]
        public async Task GetByIdAsync_MissingId_ReturnsNull()
        {
            using var context = GetContext(nameof(GetByIdAsync_MissingId_ReturnsNull));
            var repo = new GenericRepository<Category>(context);

            var found = await repo.GetByIdAsync(999);

            Assert.Null(found);
        }

        [Fact]
        public async Task GetByIdAsync_ExistingId_ReturnsEntity()
        {
            using var context = GetContext(nameof(GetByIdAsync_ExistingId_ReturnsEntity));
            var repo = new GenericRepository<Category>(context);
            var added = await repo.AddAsync(new Category { Name = "Beverages" });

            var found = await repo.GetByIdAsync(added.Id);

            Assert.NotNull(found);
            Assert.Equal("Beverages", found!.Name);
        }

        [Fact]
        public async Task UpdateAsync_PersistsModifications()
        {
            using var context = GetContext(nameof(UpdateAsync_PersistsModifications));
            var repo = new GenericRepository<Category>(context);
            var added = await repo.AddAsync(new Category { Name = "Starters", DisplayOrder = 1 });

            added.Name = "Appetizers";
            added.DisplayOrder = 2;
            await repo.UpdateAsync(added);

            var reloaded = await repo.GetByIdAsync(added.Id);
            Assert.Equal("Appetizers", reloaded!.Name);
            Assert.Equal(2, reloaded.DisplayOrder);
        }

        [Fact]
        public async Task DeleteAsync_ExistingId_RemovesEntity()
        {
            using var context = GetContext(nameof(DeleteAsync_ExistingId_RemovesEntity));
            var repo = new GenericRepository<Category>(context);
            var added = await repo.AddAsync(new Category { Name = "Desserts" });

            await repo.DeleteAsync(added.Id);

            Assert.Null(await repo.GetByIdAsync(added.Id));
            Assert.Empty(await repo.GetAllAsync());
        }

        [Fact]
        public async Task DeleteAsync_MissingId_IsNoOp()
        {
            using var context = GetContext(nameof(DeleteAsync_MissingId_IsNoOp));
            var repo = new GenericRepository<Category>(context);
            await repo.AddAsync(new Category { Name = "Mains" });

            await repo.DeleteAsync(999);

            Assert.Single(await repo.GetAllAsync());
        }
    }
}
