using System.IO;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using HotelPOS.Persistence;

namespace HotelPOS.Infrastructure
{
    public interface IBackupService
    {
        Task CreateBackupAsync();
    }

    public class BackupService : IBackupService
    {
        private readonly HotelDbContext _db;

        public BackupService(HotelDbContext db)
        {
            _db = db;
        }

        public async Task CreateBackupAsync()
        {
            if (!_db.Database.IsRelational()) return;

            var conn = _db.Database.GetDbConnection();
            var backupDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Backups");
            if (!Directory.Exists(backupDir)) Directory.CreateDirectory(backupDir);

            if (_db.Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite")
            {
                var dbPath = conn.DataSource;
                if (string.IsNullOrEmpty(dbPath) || !File.Exists(dbPath)) return;

                var fileName = $"HotelPOS_{DateTime.Now:yyyyMMdd_HHmmss}.db";
                var destPath = Path.Combine(backupDir, fileName);

                using (var source = new SqliteConnection(conn.ConnectionString))
                using (var destination = new SqliteConnection($"Data Source={destPath}"))
                {
                    await source.OpenAsync();
                    await destination.OpenAsync();
                    source.BackupDatabase(destination);
                }
            }
            else if (_db.Database.ProviderName == "Microsoft.EntityFrameworkCore.SqlServer")
            {
                var fileName = $"HotelPOS_{DateTime.Now:yyyyMMdd_HHmmss}.bak";
                var destPath = Path.Combine(backupDir, fileName);
                
                // SQL Server requires an absolute path for backup. 
                // Note: The SQL Server service account must have write permission to this directory.
                var sql = $"BACKUP DATABASE [{conn.Database}] TO DISK = '{destPath}' WITH FORMAT, MEDIANAME = 'HotelPOSBackup', NAME = 'Full Backup of HotelPOS'";
                
                await _db.Database.ExecuteSqlRawAsync(sql);
            }

            // Cleanup old backups (keep last 7 days)
            if (Directory.Exists(backupDir))
            {
                var oldFiles = Directory.GetFiles(backupDir, "*.*")
                    .Select(f => new FileInfo(f))
                    .Where(fi => fi.CreationTime < DateTime.Now.AddDays(-7))
                    .ToList();

                foreach (var f in oldFiles) f.Delete();
            }
        }
    }
}
