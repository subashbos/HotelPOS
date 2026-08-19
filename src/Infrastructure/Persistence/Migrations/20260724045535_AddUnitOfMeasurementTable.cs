using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace HotelPOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUnitOfMeasurementTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Create the lookup table and seed the standard units first, so the
            //    Items.Unit -> UnitId backfill below has rows to match against.
            migrationBuilder.CreateTable(
                name: "UnitOfMeasurements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnitOfMeasurements", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "UnitOfMeasurements",
                columns: new[] { "Id", "DisplayOrder", "Name" },
                values: new object[,]
                {
                    { 1, 0, "Pcs" },
                    { 2, 1, "Kg" },
                    { 3, 2, "Gram" },
                    { 4, 3, "Litre" },
                    { 5, 4, "Ml" },
                    { 6, 5, "Plate" },
                    { 7, 6, "Box" },
                    { 8, 7, "Packet" },
                    { 9, 8, "Bottle" },
                    { 10, 9, "Dozen" }
                });

            migrationBuilder.InsertData(
                table: "RolePermissions",
                columns: new[] { "Id", "CanAccess", "CanDelete", "CanEdit", "ModuleName", "RoleId" },
                values: new object[,]
                {
                    { 41, true, true, true, "Units", 1 },
                    { 42, false, true, true, "Units", 2 }
                });

            // 2. Add UnitId as nullable for now so existing rows can be backfilled
            //    from the old Unit string column before it is dropped.
            migrationBuilder.AddColumn<int>(
                name: "UnitId",
                table: "Items",
                type: "int",
                nullable: true);

            migrationBuilder.Sql(@"
                UPDATE i
                SET i.UnitId = u.Id
                FROM Items i
                INNER JOIN UnitOfMeasurements u ON u.Name = i.Unit;
            ");

            // Fallback for any row whose old Unit string didn't match a seeded name.
            migrationBuilder.Sql(@"UPDATE Items SET UnitId = 1 WHERE UnitId IS NULL;");

            migrationBuilder.DropColumn(
                name: "Unit",
                table: "Items");

            migrationBuilder.AlterColumn<int>(
                name: "UnitId",
                table: "Items",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Items_UnitId",
                table: "Items",
                column: "UnitId");

            migrationBuilder.AddForeignKey(
                name: "FK_Items_UnitOfMeasurements_UnitId",
                table: "Items",
                column: "UnitId",
                principalTable: "UnitOfMeasurements",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Items_UnitOfMeasurements_UnitId",
                table: "Items");

            migrationBuilder.DropIndex(
                name: "IX_Items_UnitId",
                table: "Items");

            migrationBuilder.AddColumn<string>(
                name: "Unit",
                table: "Items",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.Sql(@"
                UPDATE i
                SET i.Unit = u.Name
                FROM Items i
                INNER JOIN UnitOfMeasurements u ON u.Id = i.UnitId;
            ");

            migrationBuilder.Sql(@"UPDATE Items SET Unit = 'Pcs' WHERE Unit IS NULL;");

            migrationBuilder.AlterColumn<string>(
                name: "Unit",
                table: "Items",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.DropColumn(
                name: "UnitId",
                table: "Items");

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: 41);

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: 42);

            migrationBuilder.DropTable(
                name: "UnitOfMeasurements");
        }
    }
}
