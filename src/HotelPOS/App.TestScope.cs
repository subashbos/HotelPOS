#nullable enable

using Microsoft.Extensions.DependencyInjection;

namespace HotelPOS
{
    // Test-only DI scope support. Views/ViewModels resolve dependencies via
    // App.CreateDbScope() rather than constructor injection; outside a running WPF
    // Application (i.e. under xunit, where System.Windows.Application.Current is null)
    // that falls back to this dummy scope so tests can supply their own fakes via
    // RegisterTestService instead of touching a real database.
    public partial class App
    {
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
    }
}
