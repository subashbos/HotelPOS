using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelPOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPayrollRunVoidReason : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "VoidReason",
                table: "PayrollRuns",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VoidReason",
                table: "PayrollRuns");
        }
    }
}
