using HotelPOS.Domain.Common.Constants;
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
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<User> Users { get; set; }
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

            // ── Security indexes for auth-critical lookups ───────────────────────
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();

            modelBuilder.Entity<Supplier>()
                .HasIndex(s => s.Name)
                .IsUnique();

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

            // ── Role & Permission Seed ──────────────────────────────────────
            modelBuilder.Entity<Role>().HasData(
                new Role { Id = 1, Name = RoleNames.Admin, Description = "Full system access" },
                new Role { Id = 2, Name = RoleNames.Cashier, Description = "Standard POS operations" }
            );

            modelBuilder.Entity<RolePermission>().HasData(
                // Admin: All access
                new RolePermission { Id = 1, RoleId = 1, ModuleName = PermissionModules.Dashboard, CanAccess = true },
                new RolePermission { Id = 2, RoleId = 1, ModuleName = PermissionModules.Billing, CanAccess = true },
                new RolePermission { Id = 3, RoleId = 1, ModuleName = PermissionModules.Items, CanAccess = true },
                new RolePermission { Id = 4, RoleId = 1, ModuleName = PermissionModules.Categories, CanAccess = true },
                new RolePermission { Id = 5, RoleId = 1, ModuleName = PermissionModules.Tables, CanAccess = true },
                new RolePermission { Id = 6, RoleId = 1, ModuleName = PermissionModules.Ledger, CanAccess = true },
                new RolePermission { Id = 7, RoleId = 1, ModuleName = PermissionModules.Journal, CanAccess = true },
                new RolePermission { Id = 8, RoleId = 1, ModuleName = PermissionModules.Settings, CanAccess = true },
                new RolePermission { Id = 9, RoleId = 1, ModuleName = PermissionModules.Audit, CanAccess = true },
                new RolePermission { Id = 10, RoleId = 1, ModuleName = PermissionModules.Shift, CanAccess = true },
                new RolePermission { Id = 21, RoleId = 1, ModuleName = PermissionModules.Roles, CanAccess = true },
                new RolePermission { Id = 23, RoleId = 1, ModuleName = PermissionModules.SalesReport, CanAccess = true },
                new RolePermission { Id = 25, RoleId = 1, ModuleName = "Purchase", CanAccess = true },

                // Cashier: Restricted access
                new RolePermission { Id = 11, RoleId = 2, ModuleName = PermissionModules.Dashboard, CanAccess = false },
                new RolePermission { Id = 12, RoleId = 2, ModuleName = PermissionModules.Billing, CanAccess = true },
                new RolePermission { Id = 13, RoleId = 2, ModuleName = PermissionModules.Items, CanAccess = false },
                new RolePermission { Id = 14, RoleId = 2, ModuleName = PermissionModules.Categories, CanAccess = false },
                new RolePermission { Id = 15, RoleId = 2, ModuleName = PermissionModules.Tables, CanAccess = false },
                new RolePermission { Id = 16, RoleId = 2, ModuleName = PermissionModules.Ledger, CanAccess = false },
                new RolePermission { Id = 17, RoleId = 2, ModuleName = PermissionModules.Journal, CanAccess = false },
                new RolePermission { Id = 18, RoleId = 2, ModuleName = PermissionModules.Settings, CanAccess = false },
                new RolePermission { Id = 19, RoleId = 2, ModuleName = PermissionModules.Audit, CanAccess = false },
                new RolePermission { Id = 20, RoleId = 2, ModuleName = PermissionModules.Shift, CanAccess = true },
                new RolePermission { Id = 22, RoleId = 2, ModuleName = PermissionModules.Roles, CanAccess = false },
                new RolePermission { Id = 24, RoleId = 2, ModuleName = PermissionModules.SalesReport, CanAccess = false },
                new RolePermission { Id = 26, RoleId = 2, ModuleName = "Purchase", CanAccess = false }
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

            // ── Suppliers seed ────────────────────────────────────────────────
            modelBuilder.Entity<Supplier>().HasData(
                new Supplier { Id = 1, Name = "Metro Wholesalers", Phone = "9876543210", Gstin = "27AAAAA1111A1Z1", City = "Mumbai", State = "Maharashtra", Pincode = "400001", OpeningBalance = 0, CreditLimit = 50000, PaymentTerms = "Credit" },
                new Supplier { Id = 2, Name = "Apex Food Distributors", Phone = "9876543211", Gstin = "27BBBBB2222B2Z2", City = "Pune", State = "Maharashtra", Pincode = "411001", OpeningBalance = 5000, CreditLimit = 100000, PaymentTerms = "30 Days" },
                new Supplier { Id = 3, Name = "Supreme Dairy Partners", Phone = "9876543212", Gstin = "27CCCCC3333C3Z3", City = "Mumbai", State = "Maharashtra", Pincode = "400002", OpeningBalance = 0, CreditLimit = 25000, PaymentTerms = PaymentModes.Cash },
                new Supplier { Id = 4, Name = "Standard Kitchen Supplies", Phone = "9876543213", Gstin = "27DDDDD4444D4Z4", City = "Nashik", State = "Maharashtra", Pincode = "422001", OpeningBalance = 1500, CreditLimit = 30000, PaymentTerms = "Credit" }
            );
        }
    }
}
