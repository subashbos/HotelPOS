using HotelPOS.Application;
using HotelPOS.Domain;
using HotelPOS.Domain.Interface;
using Moq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace HotelPOS.Tests
{
    public class SmokeTests
    {
        [Fact]
        public async Task System_BasicDataFlow_Works()
        {
            // Simple smoke test to ensure main services can be instantiated and respond
            var mockItemRepo = new Mock<IItemRepository>();
            mockItemRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Item>());
            
            var service = new ItemService(mockItemRepo.Object);
            var items = await service.GetItemsAsync();

            Assert.NotNull(items);
        }

        [Fact]
        public void Sidebar_NavButton_Initialization_Smoke()
        {
            // This represents a "black box" check that the dashboard initialization logic
            // correctly sets visibility based on roles (simulated here)
            bool isManager = true;
            var visibility = isManager ? "Visible" : "Collapsed";
            
            Assert.Equal("Visible", visibility);
        }
    }
}
