using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelPOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddHeldOrdersTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // HeldOrders isn't part of the EF model - HeldOrderRepository manages it entirely via
            // raw SQL (held/parked cart state, not tracked change history) - but no migration ever
            // created the table, so it never existed on any real database.
            migrationBuilder.CreateTable(
                name: "HeldOrders",
                columns: table => new
                {
                    Id = table.Column<System.Guid>(type: "uniqueidentifier", nullable: false),
                    HoldName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    HeldAt = table.Column<System.DateTime>(type: "datetime2", nullable: false),
                    TableNumber = table.Column<int>(type: "int", nullable: false),
                    SerializedItems = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HeldOrders", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HeldOrders");
        }
    }
}
