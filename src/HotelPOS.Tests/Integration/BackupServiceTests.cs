using HotelPOS.Services;
using HotelPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
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
            serviceCollection.AddScoped(_ =>
            {
                var context = new HotelDbContext(options);
                context.Database.EnsureCreated();
                return context;
            });
            var serviceProvider = serviceCollection.BuildServiceProvider();
            var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();

            var service = new BackupService(scopeFactory);
            var tempPath = Path.Combine(Path.GetTempPath(), "HotelPOS_Tests_" + Guid.NewGuid());
            
            try
            {
                // Act
                await service.CreateBackupAsync(tempPath);

                // Assert
                Assert.True(Directory.Exists(tempPath));
            }
            finally
            {
                if (Directory.Exists(tempPath)) Directory.Delete(tempPath, true);
                connection.Close();
            }
        }

        [Fact]
        public async Task RestoreBackupAsync_Throws_FileNotFoundException_If_File_Not_Exists()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<HotelDbContext>()
                .UseInMemoryDatabase(databaseName: "RestoreTest_" + Guid.NewGuid())
                .Options;

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddScoped(_ => new HotelDbContext(options));
            var serviceProvider = serviceCollection.BuildServiceProvider();
            var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();

            var service = new BackupService(scopeFactory);

            // Act & Assert
            await Assert.ThrowsAsync<FileNotFoundException>(() => service.RestoreBackupAsync("nonexistent_backup.db"));
        }

        [Fact]
        public async Task RestoreBackupAsync_Restores_Sqlite_Database_Correctly()
        {
            // Arrange
            var dbFile = Path.Combine(Path.GetTempPath(), "BackupTestDb_" + Guid.NewGuid() + ".db");
            var backupDir = Path.Combine(Path.GetTempPath(), "Backups_" + Guid.NewGuid());

            var connection = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={dbFile}");
            connection.Open();

            var options = new DbContextOptionsBuilder<HotelDbContext>()
                .UseSqlite(connection)
                .Options;

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddScoped(_ =>
            {
                var context = new HotelDbContext(options);
                context.Database.EnsureCreated();
                return context;
            });
            var serviceProvider = serviceCollection.BuildServiceProvider();
            var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();

            // Setup original state by modifying the seeded settings
            using (var scope = scopeFactory.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<HotelDbContext>();
                var settings = await context.SystemSettings.FirstAsync();
                settings.HotelName = "Original Hotel";
                settings.EnableAutomatedBackups = true;
                await context.SaveChangesAsync();
            }

            var service = new BackupService(scopeFactory);

            try
            {
                // Act - Create backup
                Directory.CreateDirectory(backupDir);
                await service.CreateBackupAsync(backupDir);
                var createdBackupPath = Directory.GetFiles(backupDir, "*.db").FirstOrDefault();
                Assert.NotNull(createdBackupPath);

                // Modify state
                using (var scope = scopeFactory.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<HotelDbContext>();
                    var settings = await context.SystemSettings.FirstAsync();
                    settings.HotelName = "Modified Hotel";
                    await context.SaveChangesAsync();
                }

                // Verify modification took place
                using (var scope = scopeFactory.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<HotelDbContext>();
                    var settings = await context.SystemSettings.FirstAsync();
                    Assert.Equal("Modified Hotel", settings.HotelName);
                }

                // Act - Restore backup
                await service.RestoreBackupAsync(createdBackupPath);

                // Assert - State is reverted to original
                using (var scope = scopeFactory.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<HotelDbContext>();
                    var settings = await context.SystemSettings.AsNoTracking().FirstAsync();
                    Assert.Equal("Original Hotel", settings.HotelName);
                }
            }
            finally
            {
                connection.Close();
                Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
                if (File.Exists(dbFile)) File.Delete(dbFile);
                if (Directory.Exists(backupDir)) Directory.Delete(backupDir, true);
            }
        }
    }
}


