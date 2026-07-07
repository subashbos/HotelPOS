using HotelPOS.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System.Windows;

namespace HotelPOS
{
    public partial class App : System.Windows.Application
    {
        public static App? CurrentApp => System.Windows.Application.Current as App;
        public ServiceProvider ServiceProvider { get; private set; } = null!;

        private static readonly System.Threading.ThreadLocal<System.Collections.Generic.Dictionary<Type, object>> _testServices = new(() => new());

        public static void RegisterTestService<T>(T service) where T : class
        {
            if (service != null)
            {
                _testServices.Value![typeof(T)] = service;
            }
        }

        public static IServiceScope CreateDbScope()
        {
            if (System.Windows.Application.Current == null || CurrentApp?.ServiceProvider == null)
            {
                return new DummyScope();
            }
            return CurrentApp.ServiceProvider.CreateScope();
        }

        private sealed class DummyScope : IServiceScope
        {
            public IServiceProvider ServiceProvider => new DummyServiceProvider();
            public void Dispose()
            {
                GC.SuppressFinalize(this);
            }
        }

        private sealed class DummyServiceProvider : IServiceProvider
        {
            public object? GetService(Type serviceType)
            {
                if (_testServices.Value!.TryGetValue(serviceType, out var service))
                {
                    return service;
                }
                return null;
            }
        }

        /// <summary>
        /// Configures logging, global exception handlers, dependency injection, and the database, then opens the initial login window.
        /// </summary>
        /// <param name="e">Startup event arguments supplied by the WPF runtime.</param>
        /// <remarks>
        /// This method sets up Serilog, registers application services, viewmodels, and views into the DI container, initializes or migrates the database schema, and shows the login window in its own DI scope.
        /// </remarks>
        /// <exception cref="InvalidOperationException">Thrown when a database connection string is not found in configuration (expects 'ConnectionStrings:DefaultConnection' or the HOTELPOS_DEFAULT_CONNECTION environment variable).</exception>
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
            ConfigureServices(services, connectionString);

            ServiceProvider = services.BuildServiceProvider();

            // Show login immediately; initialize database in the background
            ShowLoginWindow(allowAutoLogin: true);
            _ = InitializeDatabaseAsync();
        }


        private bool _isDatabaseInitialized;

        public void TriggerBackgroundBackup()
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    using var scope = ServiceProvider.CreateScope();
                    var backup = scope.ServiceProvider.GetRequiredService<IBackupService>();
                    await backup.CreateBackupAsync();
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Background backup execution failed.");
                }
            });
        }

        /// <summary>
        /// Opens a fresh login window in its own DI scope.
        /// Call this after logout to present a clean login screen.
        /// </summary>
        /// <param name="allowAutoLogin">
        /// Only true for the very first window shown at process cold-start. Post-logout calls
        /// (explicit sign-out or idle timeout) must default to false — otherwise a saved "remember me"
        /// token would immediately re-authenticate the user and idle timeout would never actually lock the session.
        /// </param>
        public void ShowLoginWindow(bool allowAutoLogin = false)
        {
            // Trigger backup on subsequent logouts if database is already initialized
            if (_isDatabaseInitialized)
            {
                TriggerBackgroundBackup();
            }

            var scope = ServiceProvider.CreateScope();
            var login = scope.ServiceProvider.GetRequiredService<LoginWindow>();
            login.Tag = scope;   // store scope on the window so we can dispose it on close
            login.AllowAutoLogin = allowAutoLogin;
            System.Windows.Application.Current.MainWindow = login;

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
