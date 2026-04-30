using HotelPOS.Application;
using HotelPOS.Application.Interface;
using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Interface;
using HotelPOS.Infrastructure;
using HotelPOS.Persistence;
using HotelPOS.ViewModels;
using HotelPOS.Views;
using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
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
                options.UseSqlServer(connectionString));

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

            // Show the first login screen in its own scope
            ShowLoginWindow();
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