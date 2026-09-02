using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelPOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWastageEntriesTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Guarded: App.Database.cs's InitializeDatabase() also creates this table itself via raw
            // SQL (after Migrate() runs) as a database-agnostic fallback. An unguarded CreateTable
            // here fails with "already exists" on any database where that fallback got there first.
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'WastageEntries')
                BEGIN
                    CREATE TABLE [WastageEntries] (
                        [Id] int NOT NULL IDENTITY(1,1),
                        [ItemId] int NOT NULL,
                        [Quantity] int NOT NULL,
                        [Reason] nvarchar(100) NOT NULL,
                        [WastedAt] datetime2 NOT NULL,
                        [CostPerUnit] decimal(18,2) NOT NULL,
                        [Notes] nvarchar(max) NULL,
                        CONSTRAINT [PK_WastageEntries] PRIMARY KEY ([Id]),
                        CONSTRAINT [FK_WastageEntries_Items_ItemId] FOREIGN KEY ([ItemId]) REFERENCES [Items] ([Id]) ON DELETE CASCADE
                    );
                END");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_WastageEntries_ItemId' AND object_id = OBJECT_ID('WastageEntries'))
                BEGIN
                    CREATE INDEX [IX_WastageEntries_ItemId] ON [WastageEntries] ([ItemId]);
                END");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("IF EXISTS (SELECT * FROM sys.tables WHERE name = 'WastageEntries') DROP TABLE [WastageEntries];");
        }
    }
}
