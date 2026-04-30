using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelPOS.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSystemSettingsAndUserFlags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "SystemSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HotelName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HotelAddress = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HotelPhone = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HotelGst = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DefaultPrinter = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ShowPrintPreview = table.Column<bool>(type: "bit", nullable: false),
                    ReceiptFormat = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ShowGstBreakdown = table.Column<bool>(type: "bit", nullable: false),
                    ShowItemsOnBill = table.Column<bool>(type: "bit", nullable: false),
                    ShowDiscountLine = table.Column<bool>(type: "bit", nullable: false),
                    ShowPhoneOnReceipt = table.Column<bool>(type: "bit", nullable: false),
                    ShowThankYouFooter = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemSettings", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "SystemSettings",
                columns: new[] { "Id", "DefaultPrinter", "HotelAddress", "HotelGst", "HotelName", "HotelPhone", "ReceiptFormat", "ShowDiscountLine", "ShowGstBreakdown", "ShowItemsOnBill", "ShowPhoneOnReceipt", "ShowPrintPreview", "ShowThankYouFooter" },
                values: new object[] { 1, "Microsoft Print to PDF", "Main Road, City, India", "27AAAAA0000A1Z5", "New Hotel", "", "Thermal", false, true, true, true, true, true });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "IsActive",
                value: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Users");
        }
    }
}
