using HotelPOS.Domain;
using Microsoft.EntityFrameworkCore;

namespace HotelPOS.Persistence
{
    public class HotelDbContext : DbContext
    {
        public HotelDbContext(DbContextOptions<HotelDbContext> options)
            : base(options) { }

        public DbSet<Order> Orders { get; set; }
        public DbSet<Item> Items { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<SystemSetting> SystemSettings { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<CashSession> CashSessions { get; set; }
        public DbSet<Expense> Expenses { get; set; }
        public DbSet<Table> Tables { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<RolePermission> RolePermissions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Indexes for Performance
            modelBuilder.Entity<Order>()
                .HasIndex(o => o.CreatedAt);

            modelBuilder.Entity<Order>()
                .HasIndex(o => o.IsDeleted);
 
            modelBuilder.Entity<Order>()
                .HasIndex(o => new { o.FiscalYear, o.InvoiceNumber })
                .IsUnique();

            modelBuilder.Entity<AuditLog>()
                .HasIndex(a => a.Timestamp);

            // ── Decimal Precision ─────────────────────────────────────────────
            foreach (var property in modelBuilder.Model.GetEntityTypes()
                .SelectMany(t => t.GetProperties())
                .Where(p => p.ClrType == typeof(decimal)))
            {
                property.SetPrecision(18);
                property.SetScale(2);
            }

            // ── Role & Permission Seed ──────────────────────────────────────
            modelBuilder.Entity<Role>().HasData(
                new Role { Id = 1, Name = "Admin", Description = "Full system access" },
                new Role { Id = 2, Name = "Cashier", Description = "Standard POS operations" }
            );

            modelBuilder.Entity<RolePermission>().HasData(
                // Admin: All access
                new RolePermission { Id = 1, RoleId = 1, ModuleName = "Dashboard", CanAccess = true },
                new RolePermission { Id = 2, RoleId = 1, ModuleName = "Billing", CanAccess = true },
                new RolePermission { Id = 3, RoleId = 1, ModuleName = "Items", CanAccess = true },
                new RolePermission { Id = 4, RoleId = 1, ModuleName = "Categories", CanAccess = true },
                new RolePermission { Id = 5, RoleId = 1, ModuleName = "Tables", CanAccess = true },
                new RolePermission { Id = 6, RoleId = 1, ModuleName = "Ledger", CanAccess = true },
                new RolePermission { Id = 7, RoleId = 1, ModuleName = "Journal", CanAccess = true },
                new RolePermission { Id = 8, RoleId = 1, ModuleName = "Settings", CanAccess = true },
                new RolePermission { Id = 9, RoleId = 1, ModuleName = "Audit", CanAccess = true },
                new RolePermission { Id = 10, RoleId = 1, ModuleName = "Shift", CanAccess = true },
                new RolePermission { Id = 21, RoleId = 1, ModuleName = "Roles", CanAccess = true },
                new RolePermission { Id = 23, RoleId = 1, ModuleName = "SalesReport", CanAccess = true },

                // Cashier: Restricted access
                new RolePermission { Id = 11, RoleId = 2, ModuleName = "Dashboard", CanAccess = false },
                new RolePermission { Id = 12, RoleId = 2, ModuleName = "Billing", CanAccess = true },
                new RolePermission { Id = 13, RoleId = 2, ModuleName = "Items", CanAccess = false },
                new RolePermission { Id = 14, RoleId = 2, ModuleName = "Categories", CanAccess = false },
                new RolePermission { Id = 15, RoleId = 2, ModuleName = "Tables", CanAccess = false },
                new RolePermission { Id = 16, RoleId = 2, ModuleName = "Ledger", CanAccess = false },
                new RolePermission { Id = 17, RoleId = 2, ModuleName = "Journal", CanAccess = false },
                new RolePermission { Id = 18, RoleId = 2, ModuleName = "Settings", CanAccess = false },
                new RolePermission { Id = 19, RoleId = 2, ModuleName = "Audit", CanAccess = false },
                new RolePermission { Id = 20, RoleId = 2, ModuleName = "Shift", CanAccess = true },
                new RolePermission { Id = 22, RoleId = 2, ModuleName = "Roles", CanAccess = false },
                new RolePermission { Id = 24, RoleId = 2, ModuleName = "SalesReport", CanAccess = false }
            );

            // ── User seed (admin / admin) ─────────────────────────────────────
            modelBuilder.Entity<User>().HasData(
                new User
                {
                    Id = 1,
                    Username = "admin",
                    PasswordHash = "j0ELYUC68BKe6srtcJVHNf0i2poprPPid/Q4Q6A+Ayc=",
                    Salt = "cUDnxEUZDYmisbvUU2zu1Q==",
                    Role = "Admin",
                    RoleId = 1,
                    IsActive = true,
                    MustChangePassword = true
                }
            );

            // ── Default system settings ──────────────────────────────────────
            modelBuilder.Entity<SystemSetting>().HasData(
                new SystemSetting
                {
                    Id = 1,
                    HotelName = "New Hotel",
                    HotelAddress = "Main Road, City, India",
                    HotelPhone = string.Empty,
                    HotelGst = "27AAAAA0000A1Z5",
                    DefaultPrinter = "Microsoft Print to PDF",
                    ReceiptFormat = "Thermal",
                    ShowPrintPreview = true,
                    ShowGstBreakdown = true,
                    ShowItemsOnBill = true,
                    ShowDiscountLine = false,
                    ShowPhoneOnReceipt = true,
                    ShowThankYouFooter = true
                }
            );
        }
    }
}
