using HotelPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Storage;
using Xunit;

namespace HotelPOS.TestCommon
{
    /// <summary>
    /// Base class for tests that share SharedSqliteDatabaseFixture's once-seeded database.
    /// Each test runs inside its own transaction, rolled back on dispose, so a test's
    /// inserts/updates never leak into the next test despite reusing the same connection.
    /// </summary>
    public abstract class DatabaseCollectionTestBase : IAsyncLifetime
    {
        private readonly SharedSqliteDatabaseFixture _fixture;
        private IDbContextTransaction _transaction = null!;

        protected HotelDbContext Context { get; private set; } = null!;

        protected DatabaseCollectionTestBase(SharedSqliteDatabaseFixture fixture)
        {
            _fixture = fixture;
        }

        public async Task InitializeAsync()
        {
            Context = _fixture.CreateContext();
            _transaction = await Context.Database.BeginTransactionAsync();
        }

        public async Task DisposeAsync()
        {
            await _transaction.RollbackAsync();
            await Context.DisposeAsync();
        }
    }
}
