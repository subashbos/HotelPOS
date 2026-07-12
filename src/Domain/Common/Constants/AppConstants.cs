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
        public const string Expenses = "Expenses";

        public static readonly string[] All =
        {
            Dashboard, Billing, Items, Categories, Tables,
            Ledger, Journal, Settings, Audit, Shift, Roles, SalesReport, Expenses
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

    public static class ValidationLimits
    {
        public const int MinPasswordLength = 10;
        public const int MinPhoneLength = 10;
        public const int MaxPhoneLength = 15;
        public const int Pbkdf2Iterations = 100000;
        public const int SaltByteSize = 16;
        public const int HashByteSize = 32;
    }

    public static class SecurityDefaults
    {
        public const int MaxFailedLoginAttempts = 5;
        public const int LockoutWindowMinutes = 5;
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
