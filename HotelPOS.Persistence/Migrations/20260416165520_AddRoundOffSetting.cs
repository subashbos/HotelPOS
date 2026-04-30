using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelPOS.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRoundOffSetting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EnableRoundOff",
                table: "SystemSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: 1,
                column: "EnableRoundOff",
                value: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EnableRoundOff",
                table: "SystemSettings");
        }
    }
}
