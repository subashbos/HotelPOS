using System.Reflection;
using System.Text.Json;
using HotelPOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HotelPOS.Infrastructure.Persistence.SeedData
{
    /// <summary>
    /// Loads seed data from embedded JSON resources and applies them via
    /// <see cref="ModelBuilder.Entity{TEntity}()"/> HasData calls.
    /// </summary>
    internal static class SeedDataLoader
    {
        private static readonly Assembly Assembly = typeof(SeedDataLoader).Assembly;
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip
        };

        /// <summary>Applies all JSON-based seed data to the model builder.</summary>
        public static void ApplySeedData(ModelBuilder modelBuilder)
        {
            // ── Human Resources Seed ──────────────────────────────────────────
            modelBuilder.Entity<Department>().HasData(Load<Department>("departments.json"));
            modelBuilder.Entity<Designation>().HasData(Load<Designation>("designations.json"));

            // Common Indian leave entitlements (Shops & Establishments Acts vary by state; these
            // are widely-used defaults and can be adjusted per LeaveType after seeding).
            modelBuilder.Entity<LeaveType>().HasData(Load<LeaveType>("leave_types.json"));

            // ── TDS (new regime) seed for FY2025-26 — Union Budget 2025 slabs, admin-editable thereafter ──
            modelBuilder.Entity<TdsConfig>().HasData(Load<TdsConfig>("tds_config.json"));
            modelBuilder.Entity<TdsSlab>().HasData(Load<TdsSlab>("tds_slabs.json"));

            // ── Catalog: Unit of Measurement Seed ────────────────────────────
            modelBuilder.Entity<UnitOfMeasurement>().HasData(Load<UnitOfMeasurement>("units_of_measurement.json"));

            // ── Role & Permission Seed ──────────────────────────────────────
            modelBuilder.Entity<Role>().HasData(Load<Role>("roles.json"));
            modelBuilder.Entity<RolePermission>().HasData(Load<RolePermission>("role_permissions.json"));

            // ── Default system settings ──────────────────────────────────────
            modelBuilder.Entity<SystemSetting>().HasData(Load<SystemSetting>("system_settings.json"));

            // ── Suppliers seed ────────────────────────────────────────────────
            modelBuilder.Entity<Supplier>().HasData(Load<Supplier>("suppliers.json"));
        }

        /// <summary>
        /// Reads an embedded JSON resource file and deserializes it into an array of <typeparamref name="T"/>.
        /// </summary>
        private static T[] Load<T>(string fileName)
        {
            var resourceName = $"HotelPOS.Infrastructure.Persistence.SeedData.{fileName}";

            using var stream = Assembly.GetManifestResourceStream(resourceName)
                ?? throw new InvalidOperationException(
                    $"Embedded seed data resource '{resourceName}' not found. " +
                    $"Ensure the file is marked as EmbeddedResource in the .csproj.");

            return JsonSerializer.Deserialize<T[]>(stream, JsonOptions)
                ?? throw new InvalidOperationException(
                    $"Failed to deserialize seed data from '{resourceName}'. The JSON may be empty or malformed.");
        }
    }
}
