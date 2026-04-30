using HotelPOS.Infrastructure;
using HotelPOS.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Moq;
using Xunit;

namespace HotelPOS.Tests
{
    public class BackupServiceTests
    {
        [Fact]
        public async Task CreateBackupAsync_DoesNotThrow_WhenProviderIsUnknown()
        {
            // We can't easily mock DatabaseFacade in a way that allows ExecuteSqlRawAsync 
            // without a lot of setup, but we can verify it handles the branching logic.
            
            var options = new DbContextOptionsBuilder<HotelDbContext>()
                .UseInMemoryDatabase(databaseName: "BackupTest")
                .Options;
            
            using var context = new HotelDbContext(options);
            var service = new BackupService(context);

            // InMemory provider is "Microsoft.EntityFrameworkCore.InMemory"
            // It should skip both SQLite and SQL Server blocks and complete silently
            await service.CreateBackupAsync();
        }
    }
}
