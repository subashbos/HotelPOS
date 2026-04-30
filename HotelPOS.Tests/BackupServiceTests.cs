using System.IO;
using HotelPOS.Infrastructure;
using HotelPOS.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace HotelPOS.Tests
{
    public class BackupServiceTests
    {
        [Fact]
        public async Task CreateBackupAsync_Handles_InMemory_Database_Gracefully()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<HotelDbContext>()
                .UseInMemoryDatabase(databaseName: "BackupTest_" + Guid.NewGuid())
                .Options;
            
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddScoped(_ => new HotelDbContext(options));
            var serviceProvider = serviceCollection.BuildServiceProvider();
            var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();
            
            var service = new BackupService(scopeFactory);

            // Act & Assert
            // InMemory provider is "Microsoft.EntityFrameworkCore.InMemory"
            // It should skip both SQLite and SQL Server blocks and complete without errors
            var exception = await Record.ExceptionAsync(() => service.CreateBackupAsync());
            Assert.Null(exception);
        }

        [Fact]
        public async Task CreateBackupAsync_Creates_Directory_If_Not_Exists()
        {
            // Arrange
            // Use Sqlite in-memory so IsRelational() is true
            var connection = new Microsoft.Data.Sqlite.SqliteConnection("DataSource=:memory:");
            connection.Open();
            var options = new DbContextOptionsBuilder<HotelDbContext>()
                .UseSqlite(connection)
                .Options;
            
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddScoped(_ => {
                var context = new HotelDbContext(options);
                context.Database.EnsureCreated();
                return context;
            });
            var serviceProvider = serviceCollection.BuildServiceProvider();
            var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();
            
            var service = new BackupService(scopeFactory);
            var backupDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Backups");
            
            if (Directory.Exists(backupDir)) Directory.Delete(backupDir, true);

            // Act
            await service.CreateBackupAsync();

            // Assert
            Assert.True(Directory.Exists(backupDir));
            
            connection.Close();
        }
    }
}
