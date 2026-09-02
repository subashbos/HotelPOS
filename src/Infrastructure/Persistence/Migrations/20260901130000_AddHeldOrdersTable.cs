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
            //
            // Guarded: App.Database.cs's InitializeDatabase() also creates this table itself via raw
            // SQL (after Migrate() runs) as a database-agnostic fallback, and multiple POS terminals
            // can start against the same shared SQL Server at once. An unguarded CreateTable here
            // fails with "already exists" in both cases - see the 2026-09-01 production incident.
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'HeldOrders')
                BEGIN
                    CREATE TABLE [HeldOrders] (
                        [Id] uniqueidentifier NOT NULL,
                        [HoldName] nvarchar(200) NOT NULL,
                        [HeldAt] datetime2 NOT NULL,
                        [TableNumber] int NOT NULL,
                        [SerializedItems] nvarchar(max) NOT NULL,
                        CONSTRAINT [PK_HeldOrders] PRIMARY KEY ([Id])
                    );
                END");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("IF EXISTS (SELECT * FROM sys.tables WHERE name = 'HeldOrders') DROP TABLE [HeldOrders];");
        }
    }
}
