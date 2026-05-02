using HotelPOS.Application;
using HotelPOS.Application.Interface;
using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Interface;
using HotelPOS.Infrastructure;
using HotelPOS.Persistence;
using HotelPOS.ViewModels;
using HotelPOS.Views;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MediatR;
using System.Windows;
using Serilog;

namespace HotelPOS
{
    public partial class App : System.Windows.Application
    {
        public ServiceProvider ServiceProvider { get; private set; } = null!;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.File("logs/pos-log-.txt", rollingInterval: RollingInterval.Day)
                .CreateLogger();

            Log.Information("=== HotelPOS Starting ===");

            DispatcherUnhandledException += (s, args) =>
            {
                HandleException(args.Exception, "UI Thread");
                args.Handled = true;
            };

            AppDomain.CurrentDomain.UnhandledException += (s, args) =>
            {
                HandleException(args.ExceptionObject as Exception, "AppDomain");
            };

            TaskScheduler.UnobservedTaskException += (s, args) =>
            {
                Log.Error(args.Exception, "Unobserved Task Exception");
                args.SetObserved();
            };

            var environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production";

            var config = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                .AddJsonFile("appsettings.Development.json", optional: true)
                .AddJsonFile($"appsettings.{environment}.json", optional: true)
                .AddEnvironmentVariables(prefix: "HOTELPOS_")
                .Build();

            var connectionString = config.GetConnectionString("DefaultConnection")
                ?? config["DEFAULT_CONNECTION"];

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "Connection string is missing. Configure 'ConnectionStrings:DefaultConnection' in appsettings or set HOTELPOS_DEFAULT_CONNECTION.");
            }

            var services = new ServiceCollection();

            services.AddLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddDebug();
                logging.SetMinimumLevel(LogLevel.Warning);
            });

            // ── Database ──────────────────────────────────────────────────────
            // Single Scoped registration — one DbContext per DI scope (= one session)
            services.AddDbContext<HotelDbContext>(options =>
            {
                options.UseSqlServer(connectionString);
                // Suppress pending model changes warning during runtime migration
                options.ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));
            });

            // ── Services ──────────────────────────────────────────────────────
            services.AddSingleton<IUserContext, UserContext>();

            // ── Repositories (Scoped) ─────────────────────────────────────────
            services.AddScoped<IOrderRepository, OrderRepository>();
            services.AddScoped<IItemRepository, ItemRepository>();
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<ISettingRepository, SettingRepository>();
            services.AddScoped<ICategoryRepository, CategoryRepository>();
            services.AddScoped<IAuditRepository, AuditRepository>();
            services.AddScoped<ICashRepository, CashRepository>();

            // ── Services (Scoped) ─────────────────────────────────────────────
            services.AddScoped<IOrderService, OrderService>();
            services.AddScoped<IItemService, ItemService>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IReportService, ReportService>();
            services.AddScoped<ISettingService, SettingService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IAuditService, AuditService>();
            services.AddScoped<ICashService, CashService>();
            services.AddScoped<ICategoryService, CategoryService>();
            
            services.AddSingleton<ICartService, CartService>();
            services.AddSingleton<INotificationService, NotificationService>();
            services.AddSingleton<IThemeService, ThemeService>();
            services.AddSingleton<IBackupService, BackupService>();

            // ── ViewModels ────────────────────────────────────────────────────
            services.AddTransient<BillingViewModel>();
            services.AddTransient<SessionViewModel>();

            // ── Views & Windows ───────────────────────────────────────────────
            services.AddTransient<SessionView>();
            services.AddTransient<DashboardView>();
            services.AddTransient<ItemView>();
            services.AddTransient<CategoryView>();
            services.AddTransient<LedgerView>();
            services.AddTransient<JournalView>();
            services.AddTransient<SettingsView>();
            services.AddTransient<AuditView>();
            services.AddTransient<BillingView>();

            services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(OrderService).Assembly));

            // ── Windows & Views ──────────────────────────────────────────────
            services.AddScoped<LoginWindow>();
            services.AddScoped<DashboardWindow>();
            services.AddTransient<AddItemWindow>();
            services.AddTransient<MainWindow>();

            // Register individual views used by DashboardWindow caching
            services.AddTransient<DashboardView>();
            services.AddTransient<BillingView>();
            services.AddTransient<ItemView>();
            services.AddTransient<CategoryView>();
            services.AddTransient<LedgerView>();
            services.AddTransient<JournalView>();
            services.AddTransient<SettingsView>();
            services.AddTransient<AuditView>();

            ServiceProvider = services.BuildServiceProvider();

            // ── Database Initialization ──────────────────────────────────────
            InitializeDatabase();

            // Show the first login screen in its own scope
            ShowLoginWindow();
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
                    Log.Information("Database synchronization complete.");
                }
                catch (Exception ex)
                {
                    Log.Fatal(ex, "Database synchronization failed.");
                    MessageBox.Show(
                        $"Failed to synchronize the database:\n{ex.Message}\n\nPlease ensure SQL Server is running.",
                        "Database Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    Shutdown();
                }
            }
        }

        /// <summary>
        /// Opens a fresh login window in its own DI scope.
        /// Call this after logout to present a clean login screen.
        /// </summary>
        public void ShowLoginWindow()
        {
            // ── Background Backup ─────────────────────────────────────────────
            _ = Task.Run(async () =>
            {
                using var scope = ServiceProvider.CreateScope();
                var backup = scope.ServiceProvider.GetRequiredService<IBackupService>();
                await backup.CreateBackupAsync();
            });

            var scope = ServiceProvider.CreateScope();
            var login = scope.ServiceProvider.GetRequiredService<LoginWindow>();
            login.Tag = scope;   // store scope on the window so we can dispose it on close

            login.Closed += (_, __) =>
            {
                // Only dispose the scope if we are NOT transitioning to the dashboard
                if (login.Tag is IServiceScope s) { s.Dispose(); }
            };

            login.Show();
        }

        /// <summary>
        /// Creates a new DI scope and resolves DashboardWindow within it.
        /// The scope lives for the entire logged-in session.
        /// </summary>
        public (IServiceScope scope, DashboardWindow window) CreateDashboardScope()
        {
            var scope = ServiceProvider.CreateScope();
            var dashboard = scope.ServiceProvider.GetRequiredService<DashboardWindow>();
            return (scope, dashboard);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            ServiceProvider?.Dispose();
            base.OnExit(e);
        }

        private void HandleException(Exception? ex, string source)
        {
            if (ex == null) return;
            Log.Fatal(ex, "Unhandled Exception from {Source}: {Message}", source, ex.Message);

            Dispatcher.Invoke(() =>
            {
                MessageBox.Show(
                    $"A critical error occurred in the {source}:\n{ex.Message}\n\nThe application will try to continue, but you should restart it if problems persist.",
                    "Reliability Alert",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            });
        }
    }
}