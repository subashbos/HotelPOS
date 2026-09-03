using HotelPOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HotelPOS.Infrastructure.Persistence
{
    public class HotelDbContext : DbContext
    {
        public HotelDbContext(DbContextOptions<HotelDbContext> options)
            : base(options) { }

        public DbSet<Order> Orders { get; set; }
        public DbSet<Item> Items { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<UnitOfMeasurement> UnitOfMeasurements { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<LoginLockout> LoginLockouts { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<RememberMeToken> RememberMeTokens { get; set; }
        public DbSet<PasswordResetRequest> PasswordResetRequests { get; set; }
        public DbSet<SystemSetting> SystemSettings { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<CashSession> CashSessions { get; set; }
        public DbSet<Expense> Expenses { get; set; }
        public DbSet<Table> Tables { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<RolePermission> RolePermissions { get; set; }
        public DbSet<Supplier> Suppliers { get; set; }
        public DbSet<Purchase> Purchases { get; set; }
        public DbSet<PurchaseItem> PurchaseItems { get; set; }
        public DbSet<WastageEntry> WastageEntries { get; set; }
        public DbSet<RawMaterial> RawMaterials { get; set; }
        public DbSet<BomEntry> BomEntries { get; set; }
        public DbSet<Department> Departments { get; set; }
        public DbSet<Designation> Designations { get; set; }
        public DbSet<Employee> Employees { get; set; }
        public DbSet<Attendance> Attendances { get; set; }
        public DbSet<LeaveType> LeaveTypes { get; set; }
        public DbSet<LeaveBalance> LeaveBalances { get; set; }
        public DbSet<LeaveRequest> LeaveRequests { get; set; }
        public DbSet<SalaryStructure> SalaryStructures { get; set; }
        public DbSet<PayrollRun> PayrollRuns { get; set; }
        public DbSet<Payslip> Payslips { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<TdsSlab> TdsSlabs { get; set; }
        public DbSet<TdsConfig> TdsConfigs { get; set; }
        public DbSet<Estimation> Estimations { get; set; }
        public DbSet<EstimationItem> EstimationItems { get; set; }
        public DbSet<Reservation> Reservations { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Each entity's indexes, FKs, query filters, and conversions live in their own
            // IEntityTypeConfiguration<T> class under Configurations/.
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(HotelDbContext).Assembly);

            // ── Decimal Precision (applies to every entity, so it can't be a per-entity config) ──
            foreach (var property in modelBuilder.Model.GetEntityTypes()
                .SelectMany(t => t.GetProperties())
                .Where(p => p.ClrType == typeof(decimal)))
            {
                property.SetPrecision(18);
                property.SetScale(2);
            }

            // ── Seed data (loaded from embedded JSON resources) ─────────────
            SeedData.SeedDataLoader.ApplySeedData(modelBuilder);
        }
    }
}
