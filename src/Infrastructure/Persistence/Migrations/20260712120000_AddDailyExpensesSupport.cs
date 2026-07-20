using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelPOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDailyExpensesSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Expenses_Date",
                table: "Expenses",
                column: "Date");

            migrationBuilder.InsertData(
                table: "RolePermissions",
                columns: new[] { "Id", "CanAccess", "CanDelete", "CanEdit", "ModuleName", "RoleId" },
                values: new object[,]
                {
                    { 27, true, true, true, "Expenses", 1 },
                    { 28, false, true, true, "Expenses", 2 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: 27);

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: 28);

            migrationBuilder.DropIndex(
                name: "IX_Expenses_Date",
                table: "Expenses");
        }
    }
}
