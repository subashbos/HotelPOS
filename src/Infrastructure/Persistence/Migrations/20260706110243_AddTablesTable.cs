using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelPOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTablesTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Guarded: App.Database.cs's InitializeDatabase() has always created this table itself,
            // via raw SQL, before Migrate() ever runs - on both fresh installs and any database that
            // predates this migration. An unguarded CreateTable here collides with it every time,
            // failing with "There is already an object named 'Tables' in the database."
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Tables')
                BEGIN
                    CREATE TABLE [Tables] (
                        [Id] int NOT NULL IDENTITY(1,1),
                        [Number] int NOT NULL,
                        [Name] nvarchar(max) NOT NULL,
                        [Capacity] int NOT NULL,
                        [IsActive] bit NOT NULL,
                        [IsDeleted] bit NOT NULL,
                        CONSTRAINT [PK_Tables] PRIMARY KEY ([Id])
                    );
                END");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Tables') DROP TABLE [Tables];");
        }
    }
}
