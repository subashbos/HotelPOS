using HotelPOS.Application.Interfaces;
using HotelPOS.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.IO;

namespace HotelPOS.Services
{
    public class BackupService : IBackupService
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public BackupService(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        public async Task CreateBackupAsync(string? customPath = null)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<HotelDbContext>();

            if (!db.Database.IsRelational()) return;

            // Load settings safely
            var settings = await db.SystemSettings.FirstOrDefaultAsync();
            if (settings != null && !settings.EnableAutomatedBackups && customPath == null)
            {
                // Skip automated backup if disabled in settings
                return;
            }

            var conn = db.Database.GetDbConnection();
            var backupDir = customPath ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Backups");
            if (!Directory.Exists(backupDir)) Directory.CreateDirectory(backupDir);

            string? createdFile = null;

            if (db.Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite")
            {
                createdFile = await PerformSqliteBackup(conn, backupDir);
            }
            else if (db.Database.ProviderName == "Microsoft.EntityFrameworkCore.SqlServer")
            {
                createdFile = await PerformSqlServerBackup(db, conn, backupDir);
            }

            // Automated Off-site Backup Replication
            if (createdFile != null && File.Exists(createdFile) && settings != null && !string.IsNullOrWhiteSpace(settings.OffsiteBackupPath))
            {
                try
                {
                    if (!Directory.Exists(settings.OffsiteBackupPath))
                    {
                        Directory.CreateDirectory(settings.OffsiteBackupPath);
                    }
                    var offsiteDest = Path.Combine(settings.OffsiteBackupPath, Path.GetFileName(createdFile));
                    File.Copy(createdFile, offsiteDest, overwrite: true);
                }
                catch (Exception ex)
                {
                    // Log off-site replication failure but don't crash the main pipeline
                    System.Diagnostics.Debug.WriteLine($"Failed to replicate backup off-site: {ex.Message}");
                }
            }

            CleanupOldBackups(backupDir);
        }

        public async Task RestoreBackupAsync(string backupFilePath)
        {
            if (string.IsNullOrEmpty(backupFilePath) || !File.Exists(backupFilePath))
                throw new FileNotFoundException("Backup file not found.", backupFilePath);

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<HotelDbContext>();

            if (!db.Database.IsRelational()) return;

            var conn = db.Database.GetDbConnection();

            if (db.Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite")
            {
                var dbPath = conn.DataSource;
                if (string.IsNullOrEmpty(dbPath)) return;

                await db.Database.CloseConnectionAsync();
                SqliteConnection.ClearAllPools();

                File.Copy(backupFilePath, dbPath, overwrite: true);
            }
            else if (db.Database.ProviderName == "Microsoft.EntityFrameworkCore.SqlServer")
            {
                var builder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(conn.ConnectionString)
                {
                    InitialCatalog = "master"
                };

                using var masterConn = new Microsoft.Data.SqlClient.SqlConnection(builder.ConnectionString);
                await masterConn.OpenAsync();

                var dbName = conn.Database;
                var quotedDb = new Microsoft.Data.SqlClient.SqlCommandBuilder().QuoteIdentifier(dbName);
                var sql = $@"
                    ALTER DATABASE {quotedDb} SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                    RESTORE DATABASE {quotedDb} FROM DISK = @backupPath WITH REPLACE;
                    ALTER DATABASE {quotedDb} SET MULTI_USER;";

                using var cmd = new Microsoft.Data.SqlClient.SqlCommand(sql, masterConn);
                cmd.Parameters.Add(new Microsoft.Data.SqlClient.SqlParameter("@backupPath", backupFilePath));
                cmd.CommandTimeout = 120;
                await cmd.ExecuteNonQueryAsync();
            }
        }

        private async Task<string?> PerformSqliteBackup(System.Data.Common.DbConnection conn, string backupDir)
        {
            var dbPath = conn.DataSource;
            if (string.IsNullOrEmpty(dbPath) || !File.Exists(dbPath)) return null;

            var fileName = $"HotelPOS_{DateTime.Now:yyyyMMdd_HHmmss}.db";
            var destPath = Path.Combine(backupDir, fileName);

            using var source = new SqliteConnection(conn.ConnectionString);
            using var destination = new SqliteConnection($"Data Source={destPath}");

            await source.OpenAsync();
            await destination.OpenAsync();
            source.BackupDatabase(destination);
            return destPath;
        }

        private async Task<string?> PerformSqlServerBackup(HotelDbContext db, System.Data.Common.DbConnection conn, string backupDir)
        {
            var fileName = $"HotelPOS_{DateTime.Now:yyyyMMdd_HHmmss}.bak";
            var destPath = Path.Combine(backupDir, fileName);

            var sql = $"BACKUP DATABASE [{conn.Database}] TO DISK = '{destPath}' WITH FORMAT, NAME = 'Full Backup of HotelPOS'";
            await db.Database.ExecuteSqlRawAsync(sql);
            return destPath;
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
