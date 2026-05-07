using HotelPOS.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.IO;

namespace HotelPOS.Infrastructure
{
    public interface IBackupService
    {
        Task CreateBackupAsync();
    }

    public class BackupService : IBackupService
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public BackupService(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        public async Task CreateBackupAsync()
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<HotelDbContext>();

            if (!db.Database.IsRelational()) return;

            var conn = db.Database.GetDbConnection();
            var backupDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Backups");
            if (!Directory.Exists(backupDir)) Directory.CreateDirectory(backupDir);

            if (db.Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite")
            {
                await PerformSqliteBackup(conn, backupDir);
            }
            else if (db.Database.ProviderName == "Microsoft.EntityFrameworkCore.SqlServer")
            {
                await PerformSqlServerBackup(db, conn, backupDir);
            }

            CleanupOldBackups(backupDir);
        }

        private async Task PerformSqliteBackup(System.Data.Common.DbConnection conn, string backupDir)
        {
            var dbPath = conn.DataSource;
            if (string.IsNullOrEmpty(dbPath) || !File.Exists(dbPath)) return;

            var fileName = $"HotelPOS_{DateTime.Now:yyyyMMdd_HHmmss}.db";
            var destPath = Path.Combine(backupDir, fileName);

            using var source = new SqliteConnection(conn.ConnectionString);
            using var destination = new SqliteConnection($"Data Source={destPath}");

            await source.OpenAsync();
            await destination.OpenAsync();
            source.BackupDatabase(destination);
        }

        private async Task PerformSqlServerBackup(HotelDbContext db, System.Data.Common.DbConnection conn, string backupDir)
        {
            var fileName = $"HotelPOS_{DateTime.Now:yyyyMMdd_HHmmss}.bak";
            var destPath = Path.Combine(backupDir, fileName);

            var sql = $"BACKUP DATABASE [{conn.Database}] TO DISK = '{destPath}' WITH FORMAT, NAME = 'Full Backup of HotelPOS'";
            await db.Database.ExecuteSqlRawAsync(sql);
        }

        private void CleanupOldBackups(string backupDir)
        {
            if (!Directory.Exists(backupDir)) return;

            var cutoff = DateTime.Now.AddDays(-7);
            var oldFiles = Directory.GetFiles(backupDir, "*.*")
                .Select(f => new FileInfo(f))
                .Where(fi => fi.CreationTime < cutoff)
                .ToList();

            foreach (var f in oldFiles)
            {
                try { f.Delete(); } catch { /* Ignore cleanup errors */ }
            }
        }
    }
}
