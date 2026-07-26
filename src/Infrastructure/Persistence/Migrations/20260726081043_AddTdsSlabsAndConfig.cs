using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace HotelPOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTdsSlabsAndConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TdsConfigs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FinancialYearStart = table.Column<int>(type: "int", nullable: false),
                    StandardDeduction = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    RebateIncomeLimit = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CessRatePercent = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TdsConfigs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TdsSlabs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FinancialYearStart = table.Column<int>(type: "int", nullable: false),
                    IncomeFrom = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    IncomeTo = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    RatePercent = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TdsSlabs", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "TdsConfigs",
                columns: new[] { "Id", "CessRatePercent", "FinancialYearStart", "RebateIncomeLimit", "StandardDeduction" },
                values: new object[] { 1, 4m, 2025, 1200000m, 75000m });

            migrationBuilder.InsertData(
                table: "TdsSlabs",
                columns: new[] { "Id", "DisplayOrder", "FinancialYearStart", "IncomeFrom", "IncomeTo", "RatePercent" },
                values: new object[,]
                {
                    { 1, 1, 2025, 0m, 400000m, 0m },
                    { 2, 2, 2025, 400000m, 800000m, 5m },
                    { 3, 3, 2025, 800000m, 1200000m, 10m },
                    { 4, 4, 2025, 1200000m, 1600000m, 15m },
                    { 5, 5, 2025, 1600000m, 2000000m, 20m },
                    { 6, 6, 2025, 2000000m, 2400000m, 25m },
                    { 7, 7, 2025, 2400000m, null, 30m }
                });

            migrationBuilder.CreateIndex(
                name: "IX_TdsConfigs_FinancialYearStart",
                table: "TdsConfigs",
                column: "FinancialYearStart",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TdsSlabs_FinancialYearStart_DisplayOrder",
                table: "TdsSlabs",
                columns: new[] { "FinancialYearStart", "DisplayOrder" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TdsConfigs");

            migrationBuilder.DropTable(
                name: "TdsSlabs");
        }
    }
}
