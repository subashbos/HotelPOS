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

            // ── Human Resources Seed ──────────────────────────────────────────
            modelBuilder.Entity<Department>().HasData(
                new Department { Id = 1, Name = "Front Office", Description = "Reception, reservations and guest services" },
                new Department { Id = 2, Name = "Housekeeping", Description = "Room upkeep and laundry" },
                new Department { Id = 3, Name = "Food & Beverage", Description = "Restaurant, bar and kitchen service" },
                new Department { Id = 4, Name = "Kitchen", Description = "Culinary production" },
                new Department { Id = 5, Name = "Administration", Description = "Management and back office" }
            );

            modelBuilder.Entity<Designation>().HasData(
                new Designation { Id = 1, Title = "Front Desk Executive", DepartmentId = 1 },
                new Designation { Id = 2, Title = "Housekeeping Supervisor", DepartmentId = 2 },
                new Designation { Id = 3, Title = "Waiter", DepartmentId = 3 },
                new Designation { Id = 4, Title = "Chef", DepartmentId = 4 },
                new Designation { Id = 5, Title = "General Manager", DepartmentId = 5 }
            );

            // Common Indian leave entitlements (Shops & Establishments Acts vary by state; these
            // are widely-used defaults and can be adjusted per LeaveType after seeding).
            modelBuilder.Entity<LeaveType>().HasData(
                new LeaveType { Id = 1, Code = LeaveTypeCodes.CasualLeave, Name = "Casual Leave", AnnualQuota = 12, IsPaid = true, CarryForwardAllowed = false },
                new LeaveType { Id = 2, Code = LeaveTypeCodes.SickLeave, Name = "Sick Leave", AnnualQuota = 12, IsPaid = true, CarryForwardAllowed = false },
                new LeaveType { Id = 3, Code = LeaveTypeCodes.EarnedLeave, Name = "Earned / Privilege Leave", AnnualQuota = 15, IsPaid = true, CarryForwardAllowed = true },
                new LeaveType { Id = 4, Code = LeaveTypeCodes.MaternityLeave, Name = "Maternity Leave", AnnualQuota = 182, IsPaid = true, CarryForwardAllowed = false },
                new LeaveType { Id = 5, Code = LeaveTypeCodes.LeaveWithoutPay, Name = "Leave Without Pay", AnnualQuota = 0, IsPaid = false, CarryForwardAllowed = false }
            );

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
                new RolePermission { Id = 27, RoleId = 1, ModuleName = PermissionModules.Expenses, CanAccess = true },
                new RolePermission { Id = 31, RoleId = 1, ModuleName = PermissionModules.HrEmployees, CanAccess = true },
                new RolePermission { Id = 32, RoleId = 1, ModuleName = PermissionModules.HrAttendance, CanAccess = true },
                new RolePermission { Id = 33, RoleId = 1, ModuleName = PermissionModules.HrLeave, CanAccess = true },
                new RolePermission { Id = 34, RoleId = 1, ModuleName = PermissionModules.HrPayroll, CanAccess = true },

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
                new RolePermission { Id = 26, RoleId = 2, ModuleName = "Purchase", CanAccess = false },
                new RolePermission { Id = 28, RoleId = 2, ModuleName = PermissionModules.Expenses, CanAccess = false },
                new RolePermission { Id = 35, RoleId = 2, ModuleName = PermissionModules.HrEmployees, CanAccess = false },
                new RolePermission { Id = 36, RoleId = 2, ModuleName = PermissionModules.HrAttendance, CanAccess = false },
                new RolePermission { Id = 37, RoleId = 2, ModuleName = PermissionModules.HrLeave, CanAccess = false },
                new RolePermission { Id = 38, RoleId = 2, ModuleName = PermissionModules.HrPayroll, CanAccess = false }
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
            const string maharashtra = "Maharashtra";
            modelBuilder.Entity<Supplier>().HasData(
                new Supplier { Id = 1, Name = "Metro Wholesalers", Phone = "9876543210", Gstin = "27AAAAA1111A1Z1", City = "Mumbai", State = maharashtra, Pincode = "400001", OpeningBalance = 0, CreditLimit = 50000, PaymentTerms = "Credit" },
                new Supplier { Id = 2, Name = "Apex Food Distributors", Phone = "9876543211", Gstin = "27BBBBB2222B2Z2", City = "Pune", State = maharashtra, Pincode = "411001", OpeningBalance = 5000, CreditLimit = 100000, PaymentTerms = "30 Days" },
                new Supplier { Id = 3, Name = "Supreme Dairy Partners", Phone = "9876543212", Gstin = "27CCCCC3333C3Z3", City = "Mumbai", State = maharashtra, Pincode = "400002", OpeningBalance = 0, CreditLimit = 25000, PaymentTerms = PaymentModes.Cash },
                new Supplier { Id = 4, Name = "Standard Kitchen Supplies", Phone = "9876543213", Gstin = "27DDDDD4444D4Z4", City = "Nashik", State = maharashtra, Pincode = "422001", OpeningBalance = 1500, CreditLimit = 30000, PaymentTerms = "Credit" }
            );
        }
    }
}
