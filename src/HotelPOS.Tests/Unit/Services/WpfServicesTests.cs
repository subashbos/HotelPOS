using System;
using System.Threading;
using System.Windows;
using HotelPOS.Services;
using HotelPOS.Views;
using HotelPOS.ViewModels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HotelPOS.Tests.Unit.Services
{
    public class WpfServicesTests
    {
        [Fact]
        public void ThemeService_TogglesThemeState()
        {
            try
            {
                var threadEx = ExceptionCheck(() =>
                {
                    if (!IsApplicationCreated())
                    {
                        var appInstance = new HotelPOS.App();
                        appInstance.InitializeComponent();
                    }

                    var service = new ThemeService();
                    Assert.False(service.IsDarkMode);

                    service.ToggleTheme();
                    Assert.True(service.IsDarkMode);

                    service.ToggleTheme();
                    Assert.False(service.IsDarkMode);
                });

                if (threadEx != null)
                {
                    throw threadEx;
                }
            }
            finally
            {
                ClearApplicationSingleton();
            }
        }

        [Fact]
        public void Verify_BillingView_CanBeResolved_STA()
        {
            try
            {
                var threadEx = ExceptionCheck(() =>
                {
                    if (!IsApplicationCreated())
                    {
                        var appInstance = new HotelPOS.App();
                        appInstance.InitializeComponent();
                    }

                    var app = (App)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(App));
                    var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
                    var config = new Microsoft.Extensions.Configuration.ConfigurationBuilder()
                        .AddInMemoryCollection(new System.Collections.Generic.Dictionary<string, string?>
                        {
                            { "ConnectionStrings:DefaultConnection", "Server=(localdb)\\mssqllocaldb;Database=HotelPOS_DI_Test;Trusted_Connection=True;" }
                        })
                        .Build();

                    var method = typeof(App).GetMethod("ConfigureServices",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                    Assert.NotNull(method);
                    method.Invoke(null, new object[] { services, "Server=(localdb)\\mssqllocaldb;Database=HotelPOS_DI_Test;Trusted_Connection=True;" });

                    var provider = services.BuildServiceProvider();
                    using (var scope = provider.CreateScope())
                    {
                        var billing = scope.ServiceProvider.GetRequiredService<BillingView>();
                        Assert.NotNull(billing);
                    }
                });

                if (threadEx != null)
                {
                    throw threadEx;
                }
            }
            finally
            {
                ClearApplicationSingleton();
            }
        }


        [Fact]
        public void Verify_RolesView_CanBeResolved_STA()
        {
            try
            {
                var threadEx = ExceptionCheck(() =>
                {
                    if (!IsApplicationCreated())
                    {
                        var appInstance = new HotelPOS.App();
                        appInstance.InitializeComponent();
                    }

                    var notificationService = new Moq.Mock<HotelPOS.Application.Interfaces.INotificationService>();
                    var view = new RolesView(notificationService.Object);
                    Assert.NotNull(view);
                });

                if (threadEx != null)
                {
                    throw threadEx;
                }
            }
            finally
            {
                ClearApplicationSingleton();
            }
        }

        [Fact]
        public void Verify_SupplierEntryDialog_CanBeResolved_STA()
        {
            try
            {
                var threadEx = ExceptionCheck(() =>
                {
                    if (!IsApplicationCreated())
                    {
                        var appInstance = new HotelPOS.App();
                        appInstance.InitializeComponent();
                    }

                    var supplierService = new Moq.Mock<HotelPOS.Application.Interfaces.ISupplierService>();
                    var notificationService = new Moq.Mock<HotelPOS.Application.Interfaces.INotificationService>();
                    var viewModel = new SupplierEntryViewModel(supplierService.Object, notificationService.Object);
                    var dialog = new SupplierEntryDialog(viewModel);
                    Assert.NotNull(dialog);
                });

                if (threadEx != null)
                {
                    throw threadEx;
                }
            }
            finally
            {
                ClearApplicationSingleton();
            }
        }

        [Fact]
        public void Verify_AccountView_CanBeResolved_STA()
        {
            try
            {
                var threadEx = ExceptionCheck(() =>
                {
                    if (!IsApplicationCreated())
                    {
                        var appInstance = new HotelPOS.App();
                        appInstance.InitializeComponent();
                    }

                    var userService = new Moq.Mock<HotelPOS.Application.Interfaces.IUserService>();
                    var notificationService = new Moq.Mock<HotelPOS.Application.Interfaces.INotificationService>();
                    var viewModel = new AccountViewModel(userService.Object, notificationService.Object);
                    var view = new AccountView(viewModel);
                    Assert.NotNull(view);
                });

                if (threadEx != null)
                {
                    throw threadEx;
                }
            }
            finally
            {
                ClearApplicationSingleton();
            }
        }

        [Fact]
        public void Verify_DepartmentDesignationView_CanBeResolved_STA()
        {
            try
            {
                var threadEx = ExceptionCheck(() =>
                {
                    if (!IsApplicationCreated())
                    {
                        var appInstance = new HotelPOS.App();
                        appInstance.InitializeComponent();
                    }

                    var employeeService = new Moq.Mock<HotelPOS.Application.Interfaces.IEmployeeService>();
                    var notificationService = new Moq.Mock<HotelPOS.Application.Interfaces.INotificationService>();
                    var viewModel = new DepartmentDesignationViewModel(employeeService.Object, notificationService.Object);
                    var view = new DepartmentDesignationView(viewModel);
                    Assert.NotNull(view);
                });

                if (threadEx != null)
                {
                    throw threadEx;
                }
            }
            finally
            {
                ClearApplicationSingleton();
            }
        }

        [Fact]
        public void Verify_SalaryStructureView_CanBeResolved_STA()
        {
            try
            {
                var threadEx = ExceptionCheck(() =>
                {
                    if (!IsApplicationCreated())
                    {
                        var appInstance = new HotelPOS.App();
                        appInstance.InitializeComponent();
                    }

                    var payrollService = new Moq.Mock<HotelPOS.Application.Interfaces.IPayrollService>();
                    var employeeService = new Moq.Mock<HotelPOS.Application.Interfaces.IEmployeeService>();
                    var notificationService = new Moq.Mock<HotelPOS.Application.Interfaces.INotificationService>();
                    var viewModel = new SalaryStructureViewModel(payrollService.Object, employeeService.Object, notificationService.Object);
                    var view = new SalaryStructureView(viewModel);
                    Assert.NotNull(view);
                });

                if (threadEx != null)
                {
                    throw threadEx;
                }
            }
            finally
            {
                ClearApplicationSingleton();
            }
        }

        [Fact]
        public void Verify_TdsView_CanBeResolved_STA()
        {
            try
            {
                var threadEx = ExceptionCheck(() =>
                {
                    if (!IsApplicationCreated())
                    {
                        var appInstance = new HotelPOS.App();
                        appInstance.InitializeComponent();
                    }

                    var tdsService = new Moq.Mock<HotelPOS.Application.Interfaces.ITdsService>();
                    var notificationService = new Moq.Mock<HotelPOS.Application.Interfaces.INotificationService>();
                    var viewModel = new TdsViewModel(tdsService.Object, notificationService.Object);
                    var view = new TdsView(viewModel);
                    Assert.NotNull(view);
                });

                if (threadEx != null)
                {
                    throw threadEx;
                }
            }
            finally
            {
                ClearApplicationSingleton();
            }
        }

        [Fact]
        public void PrintPreview_DefaultPrinterMissing_FallsBackToDialogOrReportsCleanly()
        {
            var result = PrintPreviewWindow.TryGetPrintQueue("NonExistent_Missing_Printer_Device_9999", out var queue);
            Assert.False(result);
            Assert.Null(queue);
        }

        private static bool IsApplicationCreated()
        {
            if (System.Windows.Application.Current != null)
                return true;
            
            ClearApplicationSingleton();
            return false;
        }

        private static void ClearApplicationSingleton()
        {
            try
            {
                var appType = typeof(System.Windows.Application);
                
                var field = appType.GetField("_appInstance",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                field?.SetValue(null, null);

                var createdField = appType.GetField("_appCreatedInThisAppDomain",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                createdField?.SetValue(null, false);
            }
            catch
            {
                // Reflection cleanup is best-effort; swallow failures
            }
        }

        private static Exception? ExceptionCheck(Action action)
        {
            Exception? threadEx = null;
            var t = new Thread(() =>
            {
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    threadEx = ex;
                }
            });
            t.SetApartmentState(ApartmentState.STA);
            t.Start();
            t.Join();
            return threadEx;
        }
    }
}

