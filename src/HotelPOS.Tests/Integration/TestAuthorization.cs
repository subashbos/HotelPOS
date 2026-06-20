using HotelPOS.Application.Interfaces;
using Moq;

namespace HotelPOS.Tests
{
    internal static class TestAuthorization
    {
        public static Mock<IAuthorizationService> AllowAll()
        {
            var mock = new Mock<IAuthorizationService>();
            mock.Setup(a => a.HasPermission(It.IsAny<string>())).Returns(true);
            return mock;
        }

        public static Mock<IAuthorizationService> DenyAll()
        {
            var mock = new Mock<IAuthorizationService>();
            mock.Setup(a => a.HasPermission(It.IsAny<string>())).Returns(false);
            mock.Setup(a => a.EnsurePermission(It.IsAny<string>()))
                .Throws(new UnauthorizedAccessException("Access denied."));
            mock.Setup(a => a.EnsureSelfOrPermission(It.IsAny<int>(), It.IsAny<string>()))
                .Throws(new UnauthorizedAccessException("Access denied."));
            return mock;
        }
    }
}

