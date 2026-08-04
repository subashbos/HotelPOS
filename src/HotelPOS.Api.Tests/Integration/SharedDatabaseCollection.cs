using HotelPOS.TestCommon;
using Xunit;

namespace HotelPOS.Tests.Integration
{
    [CollectionDefinition("SharedDatabase")]
    public class SharedDatabaseCollection : ICollectionFixture<SharedSqliteDatabaseFixture>
    {
    }
}
