namespace HotelPOS.Domain.Common.Constants
{
    public static class OrderTypes
    {
        public const string DineIn = "DineIn";
        public const string Takeaway = "Takeaway";
        public const string Online = "Online";

        public static readonly string[] All = { DineIn, Takeaway, Online };
    }

    public static class PaymentModes
    {
        public const string Cash = "Cash";
        public const string Card = "Card";
        public const string Upi = "UPI";

        /// <summary>WPF-only mixed-tender mode (cash+card+UPI split); not part of the API-validated set.</summary>
        public const string Split = "Split";

        /// <summary>Purchase/Supplier payment term — the hotel paying a supplier on credit, not a
        /// customer order tender; not part of the API-validated order payment set.</summary>
        public const string Credit = "Credit";

        public static readonly string[] All = { Cash, Card, Upi };
    }

    public static class RoleNames
    {
        public const string Admin = "Admin";
        public const string Cashier = "Cashier";
        public const string Manager = "Manager";
    }

    public static class CashSessionStatuses
    {
        public const string Open = "Open";
        public const string Closed = "Closed";
    }

    public static class OrderStatuses
    {
        public const string Paid = "Paid";
        public const string Void = "Void";
        public const string Partial = "Partial";
        public const string Refunded = "Refunded";
        public const string PartiallyRefunded = "PartiallyRefunded";
    }

    public static class AlertLevels
    {
        public const string Critical = "Critical";
        public const string Warning = "Warning";
        public const string Normal = "Normal";
    }

    public static class PermissionModules
    {
        public const string Dashboard = "Dashboard";
        public const string Billing = "Billing";
        public const string Items = "Items";
        public const string Categories = "Categories";
        public const string Tables = "Tables";
        public const string Ledger = "Ledger";
        public const string Journal = "Journal";
        public const string Settings = "Settings";
        public const string Audit = "Audit";
        public const string Shift = "Shift";
        public const string Roles = "Roles";
        public const string SalesReport = "SalesReport";
        public const string HrEmployees = "HrEmployees";
        public const string HrAttendance = "HrAttendance";
        public const string HrLeave = "HrLeave";
        public const string HrPayroll = "HrPayroll";
        /// <summary>Salary structures, running/voiding payroll — a finer-grained action distinct from <see cref="HrPayroll"/>'s read/view access.</summary>
        public const string HrPayrollRun = "HrPayrollRun";
        public const string Expenses = "Expenses";
        public const string Customers = "Customers";
        public const string Units = "Units";
        /// <summary>Covers both purchase orders/entries and supplier management — mirrors the WPF nav grouping (Purchase, Suppliers, RawMaterials, BOM all gate on this one module).</summary>
        public const string Purchase = "Purchase";
        public const string Tds = "Tds";
        /// <summary>Voiding/refunding/editing an already-placed order — a more sensitive action than <see cref="Billing"/>'s create/pay flow, which Cashiers also hold.</summary>
        public const string OrderManagement = "OrderManagement";
        /// <summary>Deleting a customer record — a more sensitive action than <see cref="Customers"/>'s create/update/view access, which Cashiers also hold.</summary>
        public const string CustomerManagement = "CustomerManagement";
        /// <summary>Customer quotations/cost estimates and converting them into real orders.</summary>
        public const string Estimation = "Estimation";
        /// <summary>Future-dated table booking/scheduling, distinct from live Billing table status.</summary>
        public const string Reservation = "Reservation";

        public static readonly string[] All =
        {
            Dashboard, Billing, Items, Categories, Tables,
            Ledger, Journal, Settings, Audit, Shift, Roles, SalesReport,
            HrEmployees, HrAttendance, HrLeave, HrPayroll, HrPayrollRun, Expenses, Customers, Units,
            Purchase, Tds, OrderManagement, CustomerManagement, Estimation, Reservation
        };
    }

    public static class EstimationStatuses
    {
        public const string Draft = "Draft";
        public const string Sent = "Sent";
        public const string Accepted = "Accepted";
        public const string Rejected = "Rejected";
        public const string Converted = "Converted";
        public const string Expired = "Expired";

        public static readonly string[] All = { Draft, Sent, Accepted, Rejected, Converted, Expired };
    }

    public static class ReservationStatuses
    {
        public const string Reserved = "Reserved";
        public const string CheckedIn = "CheckedIn";
        public const string Completed = "Completed";
        public const string Cancelled = "Cancelled";
        public const string NoShow = "NoShow";

        public static readonly string[] All = { Reserved, CheckedIn, Completed, Cancelled, NoShow };

        /// <summary>Valid forward transitions per status, enforced by
        /// <c>ChangeReservationStatusCommandHandler</c> so a client can't skip the workflow
        /// (e.g. Reserved -&gt; Completed directly).</summary>
        public static readonly System.Collections.Generic.Dictionary<string, string[]> NextStatuses = new()
        {
            [Reserved] = new[] { CheckedIn, Cancelled, NoShow },
            [CheckedIn] = new[] { Completed, Cancelled },
            [Completed] = System.Array.Empty<string>(),
            [Cancelled] = System.Array.Empty<string>(),
            [NoShow] = System.Array.Empty<string>()
        };
    }

    public static class ExpenseCategories
    {
        public const string General = "General";
        public const string Salary = "Salary";
        public const string Rent = "Rent";
        public const string RawMaterial = "Raw Material";
        public const string Utilities = "Utilities";
        public const string Maintenance = "Maintenance";
        public const string Marketing = "Marketing";
        public const string Miscellaneous = "Miscellaneous";

        public static readonly string[] All =
        {
            General, Salary, Rent, RawMaterial, Utilities, Maintenance, Marketing, Miscellaneous
        };
    }

    public static class EmploymentTypes
    {
        public const string Permanent = "Permanent";
        public const string Probation = "Probation";
        public const string Contract = "Contract";
        public const string PartTime = "PartTime";

        public static readonly string[] All = { Permanent, Probation, Contract, PartTime };
    }

    public static class EmployeeStatuses
    {
        public const string Active = "Active";
        public const string OnLeave = "OnLeave";
        public const string Suspended = "Suspended";
        public const string Resigned = "Resigned";
        public const string Terminated = "Terminated";

        public static readonly string[] All = { Active, OnLeave, Suspended, Resigned, Terminated };
    }

    public static class AttendanceStatuses
    {
        public const string Present = "Present";
        public const string Absent = "Absent";
        public const string HalfDay = "HalfDay";
        public const string OnLeave = "OnLeave";
        public const string Holiday = "Holiday";
        public const string WeekOff = "WeekOff";

        public static readonly string[] All = { Present, Absent, HalfDay, OnLeave, Holiday, WeekOff };
    }

    public static class LeaveRequestStatuses
    {
        public const string Pending = "Pending";
        public const string Approved = "Approved";
        public const string Rejected = "Rejected";
        public const string Cancelled = "Cancelled";

        public static readonly string[] All = { Pending, Approved, Rejected, Cancelled };
    }

    /// <summary>Common Indian statutory/customary leave codes (Shops &amp; Establishments Acts vary by state).</summary>
    public static class LeaveTypeCodes
    {
        public const string CasualLeave = "CL";
        public const string SickLeave = "SL";
        public const string EarnedLeave = "EL";
        public const string MaternityLeave = "ML";
        public const string LeaveWithoutPay = "LWP";
    }

    public static class PayrollRunStatuses
    {
        public const string Draft = "Draft";
        public const string Processed = "Processed";
        public const string Paid = "Paid";
        public const string Voided = "Voided";

        public static readonly string[] All = { Draft, Processed, Paid, Voided };
    }

    public static class PayslipPaymentStatuses
    {
        public const string Pending = "Pending";
        public const string Paid = "Paid";
    }

    /// <summary>
    /// India-specific statutory payroll parameters. PF and ESI rates are set by central law
    /// (EPF Act 1952, ESI Act 1948) and apply nationwide. Professional Tax is a STATE subject
    /// (Constitution, Article 276) — several states (UP, Haryana, Delhi, Punjab, Rajasthan, ...)
    /// levy none at all. The slab below mirrors a common state (Karnataka) as a configurable
    /// default; it is not a substitute for state-specific PT compliance. Income-tax TDS is
    /// computed separately against the admin-editable TdsSlab/TdsConfig tables (new regime only;
    /// see TdsCalculator), since slabs change every budget.
    /// </summary>
    public static class IndianStatutoryDefaults
    {
        public const decimal PfEmployeeRate = 0.12m;
        public const decimal PfEmployerRate = 0.12m;
        public const decimal PfWageCeiling = 15000m;

        public const decimal EsiEmployeeRate = 0.0075m;
        public const decimal EsiEmployerRate = 0.0325m;
        public const decimal EsiWageThreshold = 21000m;

        public const decimal ProfessionalTaxThreshold = 15000m;
        public const decimal ProfessionalTaxAmount = 200m;
    }

    public static class ValidationLimits
    {
        public const int MinPasswordLength = 10;
        public const int MinPhoneLength = 10;
        public const int MaxPhoneLength = 15;
        public const int Pbkdf2Iterations = 600000;

        /// <summary>Iteration count used by password hashes written before <see cref="Pbkdf2Iterations"/>
        /// was raised and before the hash format encoded its own iteration count. Needed to keep
        /// verifying those legacy rows - see <see cref="HotelPOS.Domain.Common.PasswordHasher"/>.</summary>
        public const int Pbkdf2LegacyIterations = 100000;
        public const int SaltByteSize = 16;
        public const int HashByteSize = 32;
    }

    public static class SecurityDefaults
    {
        public const int MaxFailedLoginAttempts = 5;
        public const int LockoutWindowMinutes = 5;
        public const int MaxPasswordResetCodeAttempts = 5;
    }

    public static class AuditActions
    {
        public const string LoginSuccess = "LoginSuccess";
        public const string LoginFailed = "LoginFailed";
        public const string LoginBlocked = "LoginBlocked";
        public const string Logout = "Logout";
    }

    public static class StockAlertThresholds
    {
        public const int CriticalMarginPercent = 10;
        public const int WarningMarginPercent = 25;
        public const int CriticalDaysRemaining = 2;
        public const int WarningDaysRemaining = 7;
    }

    public static class ReportingLimits
    {
        public const int RecentEntriesLimit = 50;
        public const int TrailingSalesDays = 30;
        public const int TrailingHistoryMonths = 12;
        public const int ItemPreviewLimit = 10;
    }

    public static class MoneyPrecision
    {
        public const int CurrencyDecimals = 2;
        public const int RateDecimals = 3;
        public const decimal PercentDivisor = 100m;
    }
}
