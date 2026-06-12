using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HotelPOS.Tests
{
    public class AppScopeTests
    {
        [Fact]
        public void CreateDbScope_WhenApplicationCurrentIsNull_ReturnsDummyScope()
        {
            // Act
            using (var scope = App.CreateDbScope())
            {
                // Assert
                Assert.NotNull(scope);
                Assert.NotNull(scope.ServiceProvider);

                // Should return null rather than throwing or resolving during unit tests
                var result = scope.ServiceProvider.GetService<object>();
                Assert.Null(result);
            }
        }
    }
}
