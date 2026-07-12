using HotelPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System.Windows;

namespace HotelPOS
{
    public partial class App
    {
        private async Task InitializeDatabaseAsync()
        {
            try
            {
                await Task.Run(InitializeDatabase);
                _isDatabaseInitialized = true;
                TriggerBackgroundBackup();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Database initialization failed on background thread.");
                await Dispatcher.InvokeAsync(() =>
                {
                    MessageBox.Show(
                        $"Failed to synchronize the database:\n{ex.Message}\n\nPlease ensure SQL Server is running.",
                        "Database Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    Shutdown();
                });
            }
        }

        private void InitializeDatabase()
        {
            using (var scope = ServiceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<HotelDbContext>();
                try
                {
                    Log.Information("Synchronizing database schema and migration history...");

                    // 1. Ensure the Migrations History table exists
                    context.Database.ExecuteSqlRaw(@"
                        IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = '__EFMigrationsHistory')
                        BEGIN
                            CREATE TABLE [__EFMigrationsHistory] (
                                [MigrationId] nvarchar(150) NOT NULL,
                                [ProductVersion] nvarchar(32) NOT NULL,
                                CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
                            );
                        END");

                    // 1.1 Ensure Tables table exists
                    context.Database.ExecuteSqlRaw(@"
                        IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Tables')
                        BEGIN
                            CREATE TABLE [Tables] (
                                [Id] int NOT NULL IDENTITY(1,1),
                                [Number] int NOT NULL DEFAULT 0,
                                [Name] nvarchar(max) NOT NULL,
                                [Capacity] int NOT NULL,
                                [IsActive] bit NOT NULL,
                                [IsDeleted] bit NOT NULL DEFAULT 0,
                                CONSTRAINT [PK_Tables] PRIMARY KEY ([Id])
                            );
                        END
                        ELSE
                        BEGIN
                            IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Tables') AND name = 'Number')
                            BEGIN
                                ALTER TABLE [Tables] ADD [Number] int NOT NULL DEFAULT 0;
                            END
                        END");

                    // Ensure Items table has CostPrice and MinStockThreshold columns
                    context.Database.ExecuteSqlRaw(@"
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Items') AND name = 'CostPrice')
                        BEGIN
                            ALTER TABLE [Items] ADD [CostPrice] decimal(18,2) NOT NULL DEFAULT 0.00;
                        END
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Items') AND name = 'MinStockThreshold')
                        BEGIN
                            ALTER TABLE [Items] ADD [MinStockThreshold] int NOT NULL DEFAULT 10;
                        END");

                    // Ensure WastageEntries table exists
                    context.Database.ExecuteSqlRaw(@"
                        IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'WastageEntries')
                        BEGIN
                            CREATE TABLE [WastageEntries] (
                                [Id] int NOT NULL IDENTITY(1,1),
                                [ItemId] int NOT NULL,
                                [Quantity] int NOT NULL,
                                [Reason] nvarchar(100) NOT NULL,
                                [WastedAt] datetime2 NOT NULL,
                                [CostPerUnit] decimal(18,2) NOT NULL,
                                [Notes] nvarchar(max) NULL,
                                CONSTRAINT [PK_WastageEntries] PRIMARY KEY ([Id]),
                                CONSTRAINT [FK_WastageEntries_Items_ItemId] FOREIGN KEY ([ItemId]) REFERENCES [Items] ([Id]) ON DELETE CASCADE
                            );
                        END");

                    // Ensure Orders table has new billing columns
                    context.Database.ExecuteSqlRaw(@"
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Orders') AND name = 'Status')
                        BEGIN
                            ALTER TABLE [Orders] ADD [Status] nvarchar(50) NOT NULL DEFAULT 'Paid';
                        END
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Orders') AND name = 'AmountPaid')
                        BEGIN
                            ALTER TABLE [Orders] ADD [AmountPaid] decimal(18,2) NOT NULL DEFAULT 0.00;
                        END
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Orders') AND name = 'CashPaid')
                        BEGIN
                            ALTER TABLE [Orders] ADD [CashPaid] decimal(18,2) NOT NULL DEFAULT 0.00;
                        END
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Orders') AND name = 'CardPaid')
                        BEGIN
                            ALTER TABLE [Orders] ADD [CardPaid] decimal(18,2) NOT NULL DEFAULT 0.00;
                        END
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Orders') AND name = 'UpiPaid')
                        BEGIN
                            ALTER TABLE [Orders] ADD [UpiPaid] decimal(18,2) NOT NULL DEFAULT 0.00;
                        END
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Orders') AND name = 'RefundedAmount')
                        BEGIN
                            ALTER TABLE [Orders] ADD [RefundedAmount] decimal(18,2) NOT NULL DEFAULT 0.00;
                        END
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Orders') AND name = 'RefundReason')
                        BEGIN
                            ALTER TABLE [Orders] ADD [RefundReason] nvarchar(max) NULL;
                        END
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Orders') AND name = 'VoidReason')
                        BEGIN
                            ALTER TABLE [Orders] ADD [VoidReason] nvarchar(max) NULL;
                        END");

                    // Ensure SystemSettings table has backup columns
                    context.Database.ExecuteSqlRaw(@"
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('SystemSettings') AND name = 'EnableAutomatedBackups')
                        BEGIN
                            ALTER TABLE [SystemSettings] ADD [EnableAutomatedBackups] bit NOT NULL DEFAULT 1;
                        END
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('SystemSettings') AND name = 'OffsiteBackupPath')
                        BEGIN
                            ALTER TABLE [SystemSettings] ADD [OffsiteBackupPath] nvarchar(max) NULL;
                        END");

                    // 2. If 'Orders' table exists, baseline the history to prevent 'Already Exists' errors
                    // We check if the InitialCreate migration is already in history
                    var historyCount = context.Database.SqlQueryRaw<int>("SELECT COUNT(*) FROM __EFMigrationsHistory WHERE MigrationId = '20260414123141_InitialCreate'").AsEnumerable().FirstOrDefault();

                    if (historyCount == 0)
                    {
                        // Check if the Orders table actually exists on disk
                        var ordersExist = context.Database.SqlQueryRaw<int>("SELECT COUNT(*) FROM sys.tables WHERE name = 'Orders'").AsEnumerable().FirstOrDefault() > 0;

                        if (ordersExist)
                        {
                            Log.Warning("Existing database detected without migration history. Basclining migrations...");

                            var migrationsToBaseline = new[]
                            {
                                "20260414123141_InitialCreate",
                                "20260415161324_AddItemsTable",
                                "20260416134035_Phase2Update",
                                "20260416140401_Phase4AuthUpdate",
                                "20260416163055_AddSystemSettingsAndUserFlags",
                                "20260416165520_AddRoundOffSetting",
                                "20260417050418_AddBarcodeToItem",
                                "20260417053353_AddItemTaxPercentage",
                                "20260417151659_UpdateOrderFields",
                                "20260417161731_AddTaxPercentageToOrderItem",
                                "20260421150135_AddItemCategory",
                                "20260421151201_AddCategoryEntity",
                                "20260423070248_RemoveBarcodeFromItem",
                                "20260423073320_SoftDeleteAndAuditing",
                                "20260425164423_AddPerformanceIndexes",
                                "20260425170344_AddInventoryTracking",
                                "20260427074939_AddDiscountAndPayment",
                                "20260427082624_AddCashSessions",
                                "20260427103814_AddGstAndCustomerFields",
                                "20260427163516_RestoreBarcode",
                                "20260427170535_AddMustChangePassword",
                                "20260501140601_SyncModel"
                            };

                            foreach (var mId in migrationsToBaseline)
                            {
                                context.Database.ExecuteSqlRaw("IF NOT EXISTS (SELECT 1 FROM __EFMigrationsHistory WHERE MigrationId = {0}) INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion) VALUES ({0}, '9.0.0')", mId);
                            }
                        }
                    }

                    // 3. Now run Migrate() normally. It will only apply NEW migrations.
                    context.Database.Migrate();

                    // Ensure 'admin' user exists in the database with password 'admin'
                    var adminUser = context.Users.FirstOrDefault(u => u.Username == "admin");
                    if (adminUser == null)
                    {
                        var hVal = "j0ELYUC68BKe6srtcJVHNf0i2poprPPid/Q4Q6A+Ayc="; // default admin hash (password: admin)
                        var sVal = "cUDnxEUZDYmisbvUU2zu1Q==";

                        adminUser = new HotelPOS.Domain.Entities.User
                        {
                            Username = "admin",
                            PasswordHash = hVal,
                            Salt = sVal,
                            Role = HotelPOS.Domain.Common.Constants.RoleNames.Admin,
                            RoleId = 1,
                            IsActive = true,
                            MustChangePassword = true
                        };
                        context.Users.Add(adminUser);
                        context.SaveChanges();
                        Log.Information("Default Admin user was missing; seeded 'admin' with password 'admin'.");
                    }
                    else if (adminUser.PasswordHash == "ZxXEc9YNfli38Nb+Xl7bjQG7defoGXYkZ0YJX6aWmKA=")
                    {
                        adminUser.PasswordHash = "j0ELYUC68BKe6srtcJVHNf0i2poprPPid/Q4Q6A+Ayc=";
                        adminUser.Salt = "cUDnxEUZDYmisbvUU2zu1Q==";
                        adminUser.MustChangePassword = true;
                        adminUser.IsActive = true;
                        context.SaveChanges();
                        Log.Information("Detected unknown default admin password; reset to 'admin'.");
                    }

                    // Ensure HeldOrders table exists (database-agnostic approach)
                    if (context.Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite")
                    {
                        context.Database.ExecuteSqlRaw(@"
                            CREATE TABLE IF NOT EXISTS HeldOrders (
                                Id TEXT PRIMARY KEY,
                                HoldName TEXT NOT NULL,
                                HeldAt TEXT NOT NULL,
                                TableNumber INTEGER NOT NULL,
                                SerializedItems TEXT NOT NULL
                            );");
                    }
                    else if (context.Database.ProviderName == "Microsoft.EntityFrameworkCore.SqlServer")
                    {
                        context.Database.ExecuteSqlRaw(@"
                            IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='HeldOrders' and xtype='U')
                            CREATE TABLE HeldOrders (
                                Id UNIQUEIDENTIFIER PRIMARY KEY,
                                HoldName NVARCHAR(255) NOT NULL,
                                HeldAt DATETIME NOT NULL,
                                TableNumber INT NOT NULL,
                                SerializedItems NVARCHAR(MAX) NOT NULL
                            );");
                    }

                    Log.Information("Database synchronization complete.");
                }
                catch
                {
                    // Logged with full context by InitializeDatabaseAsync's catch block, which also
                    // surfaces the error to the user and shuts down — avoid double-logging here.
                    throw;
                }
            }
        }
    }
}
