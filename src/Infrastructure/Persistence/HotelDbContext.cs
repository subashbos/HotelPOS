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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ── Global soft-delete query filter (prevents deleted orders appearing in any query) ──
            modelBuilder.Entity<Order>().HasQueryFilter(o => !o.IsDeleted);

            // ── Indexes for Performance ──────────────────────────────────────────
            modelBuilder.Entity<Order>()
                .HasIndex(o => o.CreatedAt);

            modelBuilder.Entity<Order>()
                .HasIndex(o => o.IsDeleted);

            modelBuilder.Entity<Order>()
                .HasIndex(o => new { o.FiscalYear, o.InvoiceNumber })
                .IsUnique();

            modelBuilder.Entity<AuditLog>()
                .HasIndex(a => a.Timestamp);

            // Prevent deleting a unit of measurement that's still referenced by menu items
            // (the application layer also blocks this, but this is a DB-level backstop).
            modelBuilder.Entity<Item>()
                .HasOne(i => i.Unit)
                .WithMany()
                .HasForeignKey(i => i.UnitId)
                .OnDelete(DeleteBehavior.Restrict);

            // ── Security indexes for auth-critical lookups ───────────────────────
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();

            modelBuilder.Entity<RefreshToken>()
                .HasIndex(t => t.TokenHash)
                .IsUnique();

            modelBuilder.Entity<RememberMeToken>()
                .HasIndex(t => t.TokenHash)
                .IsUnique();

            modelBuilder.Entity<PasswordResetRequest>()
                .HasIndex(r => r.CodeHash);

            modelBuilder.Entity<Supplier>()
                .HasIndex(s => s.Name)
                .IsUnique();

            modelBuilder.Entity<Expense>()
                .HasIndex(e => e.Date);

            // ── Decimal Precision ─────────────────────────────────────────────
            foreach (var property in modelBuilder.Model.GetEntityTypes()
                .SelectMany(t => t.GetProperties())
                .Where(p => p.ClrType == typeof(decimal)))
            {
                property.SetPrecision(18);
                property.SetScale(2);
            }

            // ── BOM Relationships ─────────────────────────────────────────────
            modelBuilder.Entity<BomEntry>()
                .HasOne(b => b.Item)
                .WithMany()
                .HasForeignKey(b => b.ItemId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<BomEntry>()
                .HasOne(b => b.RawMaterial)
                .WithMany(r => r.BomEntries)
                .HasForeignKey(b => b.RawMaterialId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<BomEntry>()
                .HasIndex(b => new { b.ItemId, b.RawMaterialId })
                .IsUnique();

            modelBuilder.Entity<RawMaterial>()
                .HasIndex(r => r.Name)
                .IsUnique();

            // ── Table uniqueness (DB-level backstop for the app-level duplicate check) ──
            modelBuilder.Entity<Table>()
                .HasIndex(t => t.Number)
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");

            // ── Cash session uniqueness (DB-level backstop closing the open/open TOCTOU race) ──
            modelBuilder.Entity<CashSession>()
                .HasIndex(s => s.Status)
                .IsUnique()
                .HasFilter("[Status] = 'Open'");

            // ── Human Resources Relationships ─────────────────────────────────
            modelBuilder.Entity<Designation>()
                .HasOne(d => d.Department)
                .WithMany()
                .HasForeignKey(d => d.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Employee>()
                .HasOne(e => e.Department)
                .WithMany()
                .HasForeignKey(e => e.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Employee>()
                .HasOne(e => e.Designation)
                .WithMany()
                .HasForeignKey(e => e.DesignationId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Employee>()
                .HasOne(e => e.ReportingManager)
                .WithMany()
                .HasForeignKey(e => e.ReportingManagerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Employee>()
                .HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Employee>()
                .HasIndex(e => e.EmployeeCode)
                .IsUnique();

            // ── PII encryption at rest ────────────────────────────────────────
            var piiConverter = new EncryptedStringConverter();
            modelBuilder.Entity<Employee>().Property(e => e.Pan).HasConversion(piiConverter);
            modelBuilder.Entity<Employee>().Property(e => e.Aadhaar).HasConversion(piiConverter);
            modelBuilder.Entity<Employee>().Property(e => e.Uan).HasConversion(piiConverter);
            modelBuilder.Entity<Employee>().Property(e => e.EsicNumber).HasConversion(piiConverter);
            modelBuilder.Entity<Employee>().Property(e => e.BankAccountNumber).HasConversion(piiConverter);
            modelBuilder.Entity<Employee>().Property(e => e.BankIfsc).HasConversion(piiConverter);

            modelBuilder.Entity<Order>()
                .HasOne(o => o.Customer)
                .WithMany()
                .HasForeignKey(o => o.CustomerId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Customer>()
                .HasIndex(c => c.Phone);

            modelBuilder.Entity<Attendance>()
                .HasOne(a => a.Employee)
                .WithMany()
                .HasForeignKey(a => a.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Attendance>()
                .HasIndex(a => new { a.EmployeeId, a.Date })
                .IsUnique();

            modelBuilder.Entity<LeaveBalance>()
                .HasOne(b => b.Employee)
                .WithMany()
                .HasForeignKey(b => b.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<LeaveBalance>()
                .HasOne(b => b.LeaveType)
                .WithMany()
                .HasForeignKey(b => b.LeaveTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<LeaveBalance>()
                .HasIndex(b => new { b.EmployeeId, b.LeaveTypeId, b.Year })
                .IsUnique();

            modelBuilder.Entity<LeaveRequest>()
                .HasOne(r => r.Employee)
                .WithMany()
                .HasForeignKey(r => r.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<LeaveRequest>()
                .HasOne(r => r.LeaveType)
                .WithMany()
                .HasForeignKey(r => r.LeaveTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<LeaveRequest>()
                .HasOne(r => r.ApprovedByEmployee)
                .WithMany()
                .HasForeignKey(r => r.ApprovedByEmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SalaryStructure>()
                .HasOne(s => s.Employee)
                .WithMany()
                .HasForeignKey(s => s.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PayrollRun>()
                .HasIndex(p => new { p.Month, p.Year })
                .IsUnique();

            modelBuilder.Entity<Payslip>()
                .HasOne(p => p.PayrollRun)
                .WithMany(r => r.Payslips)
                .HasForeignKey(p => p.PayrollRunId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Payslip>()
                .HasOne(p => p.Employee)
                .WithMany()
                .HasForeignKey(p => p.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Payslip>()
                .HasIndex(p => new { p.PayrollRunId, p.EmployeeId })
                .IsUnique();

            modelBuilder.Entity<TdsConfig>()
                .HasIndex(c => c.FinancialYearStart)
                .IsUnique();

            modelBuilder.Entity<TdsSlab>()
                .HasIndex(s => new { s.FinancialYearStart, s.DisplayOrder })
                .IsUnique();

            // ── Seed data (loaded from embedded JSON resources) ─────────────
            SeedData.SeedDataLoader.ApplySeedData(modelBuilder);
        }
    }
}
