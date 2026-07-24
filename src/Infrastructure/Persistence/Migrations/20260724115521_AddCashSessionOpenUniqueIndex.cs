using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelPOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCashSessionOpenUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_CashSessions_Status",
                table: "CashSessions",
                column: "Status",
                unique: true,
                filter: "[Status] = 'Open'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CashSessions_Status",
                table: "CashSessions");
        }
    }
}
