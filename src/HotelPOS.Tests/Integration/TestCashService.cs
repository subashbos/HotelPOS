using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Common.Constants;
using HotelPOS.Domain.Entities;
using Moq;

namespace HotelPOS.Tests
{
    internal static class TestCashService
    {
        public static Mock<ICashService> WithOpenSession()
        {
            var mock = new Mock<ICashService>();
            mock.Setup(c => c.GetCurrentSessionAsync())
                .ReturnsAsync(new CashSession { Status = CashSessionStatuses.Open, OpenedBy = "test" });
            return mock;
        }

        public static Mock<ICashService> WithNoOpenSession()
        {
            var mock = new Mock<ICashService>();
            mock.Setup(c => c.GetCurrentSessionAsync()).ReturnsAsync((CashSession?)null);
            return mock;
        }
    }
}
