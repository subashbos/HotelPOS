using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelPOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SplitHumanResourcesPermission : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: 29);

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: 30);

            migrationBuilder.InsertData(
                table: "RolePermissions",
                columns: new[] { "Id", "CanAccess", "CanDelete", "CanEdit", "ModuleName", "RoleId" },
                values: new object[,]
                {
                    { 31, true, true, true, "HrEmployees", 1 },
                    { 32, true, true, true, "HrAttendance", 1 },
                    { 33, true, true, true, "HrLeave", 1 },
                    { 34, true, true, true, "HrPayroll", 1 },
                    { 35, false, true, true, "HrEmployees", 2 },
                    { 36, false, true, true, "HrAttendance", 2 },
                    { 37, false, true, true, "HrLeave", 2 },
                    { 38, false, true, true, "HrPayroll", 2 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValues: new object[] { 31, 32, 33, 34, 35, 36, 37, 38 });

            migrationBuilder.InsertData(
                table: "RolePermissions",
                columns: new[] { "Id", "CanAccess", "CanDelete", "CanEdit", "ModuleName", "RoleId" },
                values: new object[,]
                {
                    { 29, true, true, true, "HumanResources", 1 },
                    { 30, false, true, true, "HumanResources", 2 }
                });
        }
    }
}
