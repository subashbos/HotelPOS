using HotelPOS.Infrastructure;
using Xunit;

namespace HotelPOS.Tests
{
    public class ThemeTests
    {
        [Fact]
        public void ThemeService_InitialState_IsLight()
        {
            var service = new ThemeService();
            Assert.False(service.IsDarkMode);
        }

        // Note: Full UI theme testing requires a STA thread and a running Application instance,
        // which is handled in specialized UI tests. Here we test the logic.
    }
}
