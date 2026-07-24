using HotelPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System.IO;
using System.Windows;

namespace HotelPOS
{
    public partial class App
    {
        private async Task InitializeDatabaseAsync()
        {
            try
            {
                var needsAdminRegistration = await Task.Run(InitializeDatabase);
                _isDatabaseInitialized = true;
                TriggerBackgroundBackup();

                if (needsAdminRegistration)
                {
                    await Dispatcher.InvokeAsync(ShowRegistrationWindowIfStillOnLogin);
                }
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Database initialization failed on background thread.");
                await Dispatcher.InvokeAsync(() =>
                {
                    MessageBox.Show(
                        $"Failed to synchronize the database:\n{ex.Message}\n\n" +
                        "Please verify that the SQL Server instance named in appsettings.json's " +
                        "\"DefaultConnection\" is installed, running, and reachable, and that the configured " +
                        "login has permission to create the database if it doesn't already exist.",
                        "Database Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    Shutdown();
                });
            }
        }

        /// <returns>True if this was a genuinely fresh database and the operator must be prompted to
        /// register the initial admin account.</returns>
        private bool InitializeDatabase()
        {
            using (var scope = ServiceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<HotelDbContext>();

                // Deliberately uncaught: InitializeDatabaseAsync's catch block already logs the
                // full context, surfaces the error to the user, and shuts down - avoid double-logging here.
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

                // Captured before Migrate() applies anything: a database that has never had a single
                // migration recorded is a genuinely fresh install, as opposed to an existing database
                // just picking up new migrations. Determines whether the migration-seeded 'admin' row
                // (from Phase4AuthUpdate) should route to first-run registration instead of the usual
                // one-time-password remediation below - see the admin bootstrap block after Migrate().
                var wasFreshDatabase = context.Database.SqlQueryRaw<int>("SELECT COUNT(*) FROM __EFMigrationsHistory").AsEnumerable().FirstOrDefault() == 0;

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

                // Ensure Items table has its CostPrice/MinStockThreshold columns. No EF migration actually
                // adds these (only this runtime patch does - they only ever exist in the model snapshot),
                // so it must run after Migrate() - not before - to guarantee Items already exists, including
                // on a genuinely fresh database.
                context.Database.ExecuteSqlRaw(@"
                    IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Items')
                    BEGIN
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Items') AND name = 'CostPrice')
                        BEGIN
                            ALTER TABLE [Items] ADD [CostPrice] decimal(18,2) NOT NULL DEFAULT 0.00;
                        END
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Items') AND name = 'MinStockThreshold')
                        BEGIN
                            ALTER TABLE [Items] ADD [MinStockThreshold] int NOT NULL DEFAULT 10;
                        END
                    END");

                // Ensure Orders table has its billing/refund/void columns. No EF migration actually adds
                // these (only this runtime patch does - they only ever exist in the model snapshot), so it
                // must run after Migrate() - not before - to guarantee Orders already exists, including on
                // a genuinely fresh database.
                context.Database.ExecuteSqlRaw(@"
                    IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Orders')
                    BEGIN
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
                        END
                    END");

                // Ensure SystemSettings table has its backup columns. No EF migration actually adds these
                // (only this runtime patch does - they only ever exist in the model snapshot), so it must
                // run after Migrate() - not before - to guarantee SystemSettings already exists, including
                // on a genuinely fresh database.
                context.Database.ExecuteSqlRaw(@"
                    IF EXISTS (SELECT * FROM sys.tables WHERE name = 'SystemSettings')
                    BEGIN
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('SystemSettings') AND name = 'EnableAutomatedBackups')
                        BEGIN
                            ALTER TABLE [SystemSettings] ADD [EnableAutomatedBackups] bit NOT NULL DEFAULT 1;
                        END
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('SystemSettings') AND name = 'OffsiteBackupPath')
                        BEGIN
                            ALTER TABLE [SystemSettings] ADD [OffsiteBackupPath] nvarchar(max) NULL;
                        END
                    END");

                // Ensure WastageEntries table exists. No EF migration actually creates this table (only
                // this runtime patch does), so it must run after Migrate() - not before - to guarantee
                // Items (its FK target) already exists, including on a genuinely fresh database.
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

                // Ensure 'admin' user exists in the database. Known historical seed hashes
                // (from earlier migrations that shipped a fixed password: "admin" for every
                // install) are treated the same as "missing" so already-deployed databases
                // get remediated on next startup, not just fresh installs.
                var knownDefaultHashes = new[]
                {
                    "ZxXEc9YNfli38Nb+Xl7bjQG7defoGXYkZ0YJX6aWmKA=",
                    "j0ELYUC68BKe6srtcJVHNf0i2poprPPid/Q4Q6A+Ayc="
                };

                var adminUser = context.Users.FirstOrDefault(u => u.Username == "admin");
                var needsAdminRegistration = false;

                if (wasFreshDatabase && adminUser != null && knownDefaultHashes.Contains(adminUser.PasswordHash))
                {
                    // Genuinely fresh install: Migrate() just applied the migration-seeded 'admin' row
                    // (from Phase4AuthUpdate) with one of the known hardcoded hashes. Rather than silently
                    // rotating it to a password the operator has to go dig out of a file, remove the seed
                    // and have them register the account themselves. See ShowRegistrationWindowIfStillOnLogin.
                    context.Users.Remove(adminUser);
                    context.SaveChanges();
                    adminUser = null;
                    needsAdminRegistration = true;
                    Log.Information("Fresh database detected; removed the migration-seeded 'admin' account and will prompt the operator to register the initial administrator account.");
                }

                if (adminUser == null && !needsAdminRegistration)
                {
                    // Existing database with no 'admin' row at all (shouldn't normally happen once past
                    // Phase4AuthUpdate, but keep this as a safety net for unusual states).
                    var generatedPassword = GenerateRandomPassword();
                    var (hash, salt) = HashPassword(generatedPassword);

                    adminUser = new HotelPOS.Domain.Entities.User
                    {
                        Username = "admin",
                        PasswordHash = hash,
                        Salt = salt,
                        Role = HotelPOS.Domain.Common.Constants.RoleNames.Admin,
                        RoleId = 1,
                        IsActive = true,
                        MustChangePassword = true
                    };
                    context.Users.Add(adminUser);
                    context.SaveChanges();
                    WriteInitialAdminCredentialFile(generatedPassword);
                    Log.Information("Default Admin user was missing; seeded 'admin' with a randomly generated one-time password. See {Path} to retrieve it.", GetInitialAdminCredentialPath());
                }
                else if (adminUser != null && knownDefaultHashes.Contains(adminUser.PasswordHash))
                {
                    // Existing (previously-deployed) database still carrying a known hardcoded hash -
                    // remediate in place rather than deleting a possibly-in-use account.
                    var generatedPassword = GenerateRandomPassword();
                    var (hash, salt) = HashPassword(generatedPassword);
                    adminUser.PasswordHash = hash;
                    adminUser.Salt = salt;
                    adminUser.MustChangePassword = true;
                    adminUser.IsActive = true;
                    context.SaveChanges();
                    WriteInitialAdminCredentialFile(generatedPassword);
                    Log.Information("Detected known default admin password hash; reset to a randomly generated one-time password. See {Path} to retrieve it.", GetInitialAdminCredentialPath());
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
                return needsAdminRegistration;
            }
        }

        /// <summary>
        /// Swaps the currently-shown login window for the registration window, if the app is still
        /// sitting on the login screen (i.e. the user hasn't already logged in via a remember-me token
        /// in the brief window before database initialization completed).
        /// </summary>
        private void ShowRegistrationWindowIfStillOnLogin()
        {
            if (System.Windows.Application.Current.MainWindow is LoginWindow loginWindow)
                ShowRegistrationWindow(loginWindow);
        }

        /// <summary>
        /// Opens the registration window as the app's main window, in its own DI scope. Used both for the
        /// automatic first-run prompt on a genuinely fresh database (<see cref="ShowRegistrationWindowIfStillOnLogin"/>)
        /// and for the manual "Register admin account" link on the login screen.
        /// </summary>
        /// <param name="windowToClose">The currently-shown window (typically the login window) to close once
        /// the registration window is up.</param>
        public void ShowRegistrationWindow(Window? windowToClose = null)
        {
            var scope = ServiceProvider.CreateScope();
            var registration = scope.ServiceProvider.GetRequiredService<RegistrationWindow>();
            registration.Tag = scope;
            registration.Closed += (_, __) =>
            {
                if (registration.Tag is IServiceScope s) s.Dispose();
            };

            System.Windows.Application.Current.MainWindow = registration;
            registration.Show();
            windowToClose?.Close();
        }

        /// <summary>Creates the initial administrator account chosen by the operator during first-run registration.</summary>
        internal async Task<(bool Success, string Error)> CreateInitialAdminAsync(string username, string password)
        {
            username = username.Trim();
            using var scope = ServiceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<HotelDbContext>();

            if (await context.Users.AnyAsync(u => u.Username == username))
                return (false, $"Username '{username}' already exists.");

            var (hash, salt) = HashPassword(password);
            var user = new HotelPOS.Domain.Entities.User
            {
                Username = username,
                PasswordHash = hash,
                Salt = salt,
                Role = HotelPOS.Domain.Common.Constants.RoleNames.Admin,
                RoleId = 1,
                IsActive = true,
                MustChangePassword = false
            };
            context.Users.Add(user);
            await context.SaveChangesAsync();
            Log.Information("Initial administrator account '{Username}' registered by operator.", username);
            return (true, string.Empty);
        }

        private static string GenerateRandomPassword()
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789!@#$%^&*";
            var bytes = new byte[20];
            using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
            rng.GetBytes(bytes);

            var sb = new System.Text.StringBuilder(bytes.Length);
            foreach (var b in bytes)
                sb.Append(chars[b % chars.Length]);
            return sb.ToString();
        }

        private static (string Hash, string Salt) HashPassword(string password)
        {
            var saltBytes = new byte[HotelPOS.Domain.Common.Constants.ValidationLimits.SaltByteSize];
            using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
            rng.GetBytes(saltBytes);
            var hashBytes = System.Security.Cryptography.Rfc2898DeriveBytes.Pbkdf2(
                password,
                saltBytes,
                HotelPOS.Domain.Common.Constants.ValidationLimits.Pbkdf2Iterations,
                System.Security.Cryptography.HashAlgorithmName.SHA256,
                HotelPOS.Domain.Common.Constants.ValidationLimits.HashByteSize);
            return (Convert.ToBase64String(hashBytes), Convert.ToBase64String(saltBytes));
        }

        internal static string GetInitialAdminCredentialPath() => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "HotelPOS", "initial-admin-password.txt");

        // Written once per remediation so whoever runs first-run setup can retrieve the
        // one-time password; MustChangePassword forces it to be replaced before the account
        // can be used for anything else. Removed automatically by LoginWindow once that
        // forced change succeeds (see HandleMustChangePasswordAsync), so it doesn't rely on
        // the user remembering to delete it themselves.
        private static void WriteInitialAdminCredentialFile(string password)
        {
            var path = GetInitialAdminCredentialPath();
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path,
                $"Username: admin{Environment.NewLine}" +
                $"Password: {password}{Environment.NewLine}" +
                $"This is a one-time password for first login only.{Environment.NewLine}");
        }

        /// <summary>Deletes the one-time admin credential file, if present. Safe to call unconditionally.</summary>
        internal static void DeleteInitialAdminCredentialFileIfExists()
        {
            var path = GetInitialAdminCredentialPath();
            if (File.Exists(path))
                File.Delete(path);
        }
    }
}
