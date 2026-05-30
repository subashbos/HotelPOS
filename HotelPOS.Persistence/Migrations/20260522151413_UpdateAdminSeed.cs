using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelPOS.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UpdateAdminSeed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "PasswordHash", "Salt" },
                values: new object[] { "ZxXEc9YNfli38Nb+Xl7bjQG7defoGXYkZ0YJX6aWmKA=", "jwhVPO8B1u7Hqc4drt45HQ==" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "PasswordHash", "Salt" },
                values: new object[] { "j0ELYUC68BKe6srtcJVHNf0i2poprPPid/Q4Q6A+Ayc=", "cUDnxEUZDYmisbvUU2zu1Q==" });
        }
    }
}
