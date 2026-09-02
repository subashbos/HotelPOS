using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelPOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStatusColumnToOrders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Guarded: App.Database.cs's InitializeDatabase() also backfills this column itself via
            // raw SQL (after Migrate() runs) as a database-agnostic fallback. An unguarded AddColumn
            // here fails with "column already exists" on any database where that fallback got there first.
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Orders') AND name = 'Status')
                BEGIN
                    ALTER TABLE [Orders] ADD [Status] nvarchar(max) NOT NULL DEFAULT 'Paid';
                END");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Orders') AND name = 'Status')
                BEGIN
                    ALTER TABLE [Orders] DROP COLUMN [Status];
                END");
        }
    }
}
