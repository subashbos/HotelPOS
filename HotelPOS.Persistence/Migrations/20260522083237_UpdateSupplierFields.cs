using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelPOS.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSupplierFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "Suppliers",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CreditLimit",
                table: "Suppliers",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "OpeningBalance",
                table: "Suppliers",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "PaymentTerms",
                table: "Suppliers",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Pincode",
                table: "Suppliers",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "State",
                table: "Suppliers",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Suppliers",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "City", "CreditLimit", "OpeningBalance", "PaymentTerms", "Pincode", "State" },
                values: new object[] { "Mumbai", 50000m, 0m, "Credit", "400001", "Maharashtra" });

            migrationBuilder.UpdateData(
                table: "Suppliers",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "City", "CreditLimit", "OpeningBalance", "PaymentTerms", "Pincode", "State" },
                values: new object[] { "Pune", 100000m, 5000m, "30 Days", "411001", "Maharashtra" });

            migrationBuilder.UpdateData(
                table: "Suppliers",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "City", "CreditLimit", "OpeningBalance", "PaymentTerms", "Pincode", "State" },
                values: new object[] { "Mumbai", 25000m, 0m, "Cash", "400002", "Maharashtra" });

            migrationBuilder.UpdateData(
                table: "Suppliers",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "City", "CreditLimit", "OpeningBalance", "PaymentTerms", "Pincode", "State" },
                values: new object[] { "Nashik", 30000m, 1500m, "Credit", "422001", "Maharashtra" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "City",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "CreditLimit",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "OpeningBalance",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "PaymentTerms",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "Pincode",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "State",
                table: "Suppliers");
        }
    }
}
