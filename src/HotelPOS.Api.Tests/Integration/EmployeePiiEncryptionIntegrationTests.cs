using System.Security.Cryptography;
using HotelPOS.Domain.Entities;
using HotelPOS.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HotelPOS.Tests.Integration
{
    /// <summary>
    /// Proves the full stack — real HotelDbContext, a real relational provider (SQLite, since a
    /// SQL Server instance isn't available in this environment), and the ambient
    /// <see cref="PiiEncryptionKeyProvider"/> exactly as production wires it via
    /// EncryptedStringConverter's parameterless constructor — actually encrypts Employee PII at
    /// rest rather than just round-tripping through EF's InMemory provider's object graph (which
    /// HrRepositoryTests already covers but wouldn't catch a bug in the on-disk envelope format).
    /// </summary>
    /// <remarks>
    /// Sets the shared, process-wide <see cref="PiiEncryptionKeyProvider"/> ambient key. Safe today
    /// because no other test in this assembly exercises Employee PII fields through
    /// CustomWebApplicationFactory's real API host concurrently — if one is added later, either
    /// route it through EncryptedStringConverter's fixed-key constructor (see
    /// EncryptedStringConverterTests) or give this class its own xUnit collection.
    /// </remarks>
    public class EmployeePiiEncryptionIntegrationTests
    {
        public EmployeePiiEncryptionIntegrationTests()
        {
            PiiEncryptionKeyProvider.SetKey(RandomNumberGenerator.GetBytes(32));
        }

        private static (HotelDbContext Context, SqliteConnection Connection) NewSqliteContext()
        {
            var connection = new SqliteConnection("DataSource=:memory:");
            connection.Open();

            var options = new DbContextOptionsBuilder<HotelDbContext>()
                .UseSqlite(connection)
                .Options;

            var context = new HotelDbContext(options);
            context.Database.EnsureCreated();
            return (context, connection);
        }

        [Fact]
        public async Task SavedEmployee_StoresPanAsCiphertext_NotPlaintext()
        {
            var (context, connection) = NewSqliteContext();
            using (context)
            using (connection)
            {
                context.Employees.Add(new Employee
                {
                    Id = 1,
                    EmployeeCode = "EMP001",
                    FirstName = "Zara",
                    DateOfJoining = DateTime.UtcNow,
                    Pan = "ABCDE1234F"
                });
                await context.SaveChangesAsync();

                using var cmd = connection.CreateCommand();
                cmd.CommandText = "SELECT Pan FROM Employees WHERE Id = 1";
                var rawStoredValue = (string)(await cmd.ExecuteScalarAsync())!;

                Assert.NotEqual("ABCDE1234F", rawStoredValue);
                Assert.StartsWith("ENCv1:", rawStoredValue);
            }
        }

        [Fact]
        public async Task SavedEmployee_RoundTripsPanThroughANewContextInstance()
        {
            var (context, connection) = NewSqliteContext();
            using (connection)
            {
                using (context)
                {
                    context.Employees.Add(new Employee
                    {
                        Id = 1,
                        EmployeeCode = "EMP001",
                        FirstName = "Zara",
                        DateOfJoining = DateTime.UtcNow,
                        Pan = "ABCDE1234F",
                        Aadhaar = "123456789012",
                        BankAccountNumber = "00998877665544"
                    });
                    await context.SaveChangesAsync();
                }

                // Fresh context/change-tracker over the same connection — proves the read path
                // decrypts correctly, not just that SaveChanges echoed back the in-memory value.
                var reloadOptions = new DbContextOptionsBuilder<HotelDbContext>().UseSqlite(connection).Options;
                using var reloadContext = new HotelDbContext(reloadOptions);
                var reloaded = await reloadContext.Employees.FindAsync(1);

                Assert.NotNull(reloaded);
                Assert.Equal("ABCDE1234F", reloaded!.Pan);
                Assert.Equal("123456789012", reloaded.Aadhaar);
                Assert.Equal("00998877665544", reloaded.BankAccountNumber);
            }
        }

        [Fact]
        public async Task LegacyPlaintextRow_IsStillReadableAfterEncryptionIsEnabled()
        {
            var (context, connection) = NewSqliteContext();
            using (context)
            using (connection)
            {
                // Simulate a pre-existing row written before this feature shipped, by inserting
                // raw plaintext directly (bypassing the converter).
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText =
                        "INSERT INTO Employees (Id, EmployeeCode, FirstName, DateOfJoining, EmploymentType, Status, Pan) " +
                        "VALUES (2, 'EMP002', 'Anil', @date, 'Permanent', 'Active', 'FGHIJ5678K')";
                    cmd.Parameters.AddWithValue("@date", DateTime.UtcNow.ToString("o"));
                    await cmd.ExecuteNonQueryAsync();
                }

                var reloaded = await context.Employees.FindAsync(2);

                Assert.NotNull(reloaded);
                Assert.Equal("FGHIJ5678K", reloaded!.Pan); // legacy plaintext tolerated, not garbled
            }
        }
    }
}
