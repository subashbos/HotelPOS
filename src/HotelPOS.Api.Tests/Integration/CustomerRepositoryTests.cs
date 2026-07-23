using HotelPOS.Domain.Entities;
using HotelPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HotelPOS.Tests.Integration
{
    /// <summary>
    /// Exercises the Customer persistence layer against the EF InMemory provider,
    /// mirroring HrRepositoryTests / RepositoryIntegrationTests.
    /// </summary>
    public class CustomerRepositoryTests
    {
        private static HotelDbContext GetContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<HotelDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;
            return new HotelDbContext(options);
        }

        [Fact]
        public async Task CustomerRepository_CrudAndLookups()
        {
            using var context = GetContext(nameof(CustomerRepository_CrudAndLookups));
            var repo = new CustomerRepository(context);

            await repo.AddAsync(new Customer { Name = "Zara", Phone = "9876543210" });
            await repo.AddAsync(new Customer { Name = "Anil", Phone = "9876543211" });

            var all = await repo.GetAllAsync();
            Assert.Equal(2, all.Count);
            Assert.Equal("Anil", all[0].Name); // ordered by name

            var byPhone = await repo.GetByPhoneAsync("9876543210");
            Assert.NotNull(byPhone);
            Assert.Equal("Zara", byPhone!.Name);

            Assert.True(await repo.ExistsByPhoneAsync("9876543210"));
            Assert.False(await repo.ExistsByPhoneAsync("9876543210", excludeId: byPhone.Id));
            Assert.False(await repo.ExistsByPhoneAsync("0000000000"));

            byPhone.Name = "Zara Khan";
            await repo.UpdateAsync(byPhone);
            Assert.Equal("Zara Khan", (await repo.GetByIdAsync(byPhone.Id))!.Name);

            await repo.DeactivateAsync(byPhone.Id);
            var afterDeactivate = await repo.GetByIdAsync(byPhone.Id);
            Assert.False(afterDeactivate!.IsActive);
            Assert.NotNull(afterDeactivate.UpdatedAt);

            // Deactivated customers are excluded by default, included when requested
            var activeOnly = await repo.GetAllAsync();
            Assert.Single(activeOnly);
            var includingInactive = await repo.GetAllAsync(includeInactive: true);
            Assert.Equal(2, includingInactive.Count);

            await repo.DeactivateAsync(999); // deactivating a missing customer is a no-op
        }
    }
}
