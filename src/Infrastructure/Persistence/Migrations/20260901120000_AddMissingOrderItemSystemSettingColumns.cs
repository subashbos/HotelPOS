using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelPOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMissingOrderItemSystemSettingColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Guarded: App.Database.cs's InitializeDatabase() also backfills every one of these
            // columns itself via raw SQL (after Migrate() runs) as a database-agnostic fallback.
            // Unguarded AddColumn calls fail with "column already exists" on any database where
            // that fallback got there first - which, for a column this old, is most of them.
            AddColumnIfMissing(migrationBuilder, "Orders", "AmountPaid", "decimal(18,2) NOT NULL DEFAULT 0.00");
            AddColumnIfMissing(migrationBuilder, "Orders", "CashPaid", "decimal(18,2) NOT NULL DEFAULT 0.00");
            AddColumnIfMissing(migrationBuilder, "Orders", "CardPaid", "decimal(18,2) NOT NULL DEFAULT 0.00");
            AddColumnIfMissing(migrationBuilder, "Orders", "UpiPaid", "decimal(18,2) NOT NULL DEFAULT 0.00");
            AddColumnIfMissing(migrationBuilder, "Orders", "RefundedAmount", "decimal(18,2) NOT NULL DEFAULT 0.00");
            AddColumnIfMissing(migrationBuilder, "Orders", "RefundReason", "nvarchar(max) NULL");
            AddColumnIfMissing(migrationBuilder, "Orders", "VoidReason", "nvarchar(max) NULL");
            AddColumnIfMissing(migrationBuilder, "Items", "CostPrice", "decimal(18,2) NOT NULL DEFAULT 0.00");
            AddColumnIfMissing(migrationBuilder, "Items", "MinStockThreshold", "int NOT NULL DEFAULT 10");
            AddColumnIfMissing(migrationBuilder, "SystemSettings", "EnableAutomatedBackups", "bit NOT NULL DEFAULT 1");
            AddColumnIfMissing(migrationBuilder, "SystemSettings", "OffsiteBackupPath", "nvarchar(max) NULL");
        }

        private static void AddColumnIfMissing(MigrationBuilder migrationBuilder, string table, string column, string columnDefinition)
        {
            migrationBuilder.Sql($@"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('{table}') AND name = '{column}')
                BEGIN
                    ALTER TABLE [{table}] ADD [{column}] {columnDefinition};
                END");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            DropColumnIfExists(migrationBuilder, "Orders", "AmountPaid");
            DropColumnIfExists(migrationBuilder, "Orders", "CashPaid");
            DropColumnIfExists(migrationBuilder, "Orders", "CardPaid");
            DropColumnIfExists(migrationBuilder, "Orders", "UpiPaid");
            DropColumnIfExists(migrationBuilder, "Orders", "RefundedAmount");
            DropColumnIfExists(migrationBuilder, "Orders", "RefundReason");
            DropColumnIfExists(migrationBuilder, "Orders", "VoidReason");
            DropColumnIfExists(migrationBuilder, "Items", "CostPrice");
            DropColumnIfExists(migrationBuilder, "Items", "MinStockThreshold");
            DropColumnIfExists(migrationBuilder, "SystemSettings", "EnableAutomatedBackups");
            DropColumnIfExists(migrationBuilder, "SystemSettings", "OffsiteBackupPath");
        }

        private static void DropColumnIfExists(MigrationBuilder migrationBuilder, string table, string column)
        {
            migrationBuilder.Sql($@"
                IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('{table}') AND name = '{column}')
                BEGIN
                    ALTER TABLE [{table}] DROP COLUMN [{column}];
                END");
        }
    }
}
