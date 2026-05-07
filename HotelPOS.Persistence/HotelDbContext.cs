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

            // ── User seed (admin / admin) ─────────────────────────────────────
            modelBuilder.Entity<User>().HasData(
                new User
                {
                    Id = 1,
                    Username = "admin",
                    PasswordHash = "j0ELYUC68BKe6srtcJVHNf0i2poprPPid/Q4Q6A+Ayc=",
                    Salt = "cUDnxEUZDYmisbvUU2zu1Q==",
                    Role = "Admin",
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
