using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelPOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLoginLockouts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LoginLockouts",
                columns: table => new
                {
                    NormalizedUsername = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FailedAttempts = table.Column<int>(type: "int", nullable: false),
                    LockedUntilUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastAttemptUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoginLockouts", x => x.NormalizedUsername);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LoginLockouts");
        }
    }
}
