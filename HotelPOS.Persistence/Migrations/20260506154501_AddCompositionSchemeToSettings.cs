using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelPOS.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCompositionSchemeToSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsCompositionScheme",
                table: "SystemSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: 1,
                column: "IsCompositionScheme",
                value: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsCompositionScheme",
                table: "SystemSettings");
        }
    }
}
