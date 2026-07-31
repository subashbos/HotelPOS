using HotelPOS.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HotelPOS.TestCommon
{
    /// <summary>
    /// Builds the HotelDbContext schema (including its baked-in HasData rows) once per xUnit
    /// collection, against a single open SQLite ":memory:" connection shared by every test class
    /// in the collection. Pair with DatabaseCollectionTestBase so each test's own inserts/updates
    /// roll back afterward instead of leaking into the next test.
    /// </summary>
    public sealed class SharedSqliteDatabaseFixture : IAsyncLifetime
    {
        private SqliteConnection _connection = null!;

        public async Task InitializeAsync()
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            await _connection.OpenAsync();

            using var context = CreateContext();
            await context.Database.EnsureCreatedAsync();
            await TestDataSeeder.SeedBaselineAsync(context);
        }

        public HotelDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<HotelDbContext>()
                .UseSqlite(_connection)
                .Options;
            return new HotelDbContext(options);
        }

        public async Task DisposeAsync()
        {
            await _connection.DisposeAsync();
        }
    }
}
