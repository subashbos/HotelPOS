using HotelPOS.TestCommon;
using Xunit;

namespace HotelPOS.Tests
{
    [CollectionDefinition("SharedDatabase")]
    public class SharedDatabaseCollection : ICollectionFixture<SharedSqliteDatabaseFixture>
    {
    }
}
