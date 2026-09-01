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
            migrationBuilder.AddColumn<decimal>(
                name: "AmountPaid",
                table: "Orders",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "CashPaid",
                table: "Orders",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "CardPaid",
                table: "Orders",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "UpiPaid",
                table: "Orders",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "RefundedAmount",
                table: "Orders",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "RefundReason",
                table: "Orders",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VoidReason",
                table: "Orders",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CostPrice",
                table: "Items",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "MinStockThreshold",
                table: "Items",
                type: "int",
                nullable: false,
                defaultValue: 10);

            migrationBuilder.AddColumn<bool>(
                name: "EnableAutomatedBackups",
                table: "SystemSettings",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "OffsiteBackupPath",
                table: "SystemSettings",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AmountPaid",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "CashPaid",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "CardPaid",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "UpiPaid",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "RefundedAmount",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "RefundReason",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "VoidReason",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "CostPrice",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "MinStockThreshold",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "EnableAutomatedBackups",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "OffsiteBackupPath",
                table: "SystemSettings");
        }
    }
}
