using System;
using System.Threading;
using System.Windows;
using HotelPOS.Services;
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
                    // Ensure Application.Current exists for the duration of this test
                    if (System.Windows.Application.Current == null)
                    {
                        new HotelPOS.App();
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
                // Clear the Application instance so it doesn't affect other tests
                foreach (var field in typeof(System.Windows.Application).GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static))
                {
                    if (field.FieldType.IsSubclassOf(typeof(System.Windows.Application)) || field.FieldType == typeof(System.Windows.Application))
                    {
                        field.SetValue(null, null);
                    }
                }
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
