using HotelPOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelPOS.Infrastructure.Persistence.Configurations
{
    public class TableConfiguration : IEntityTypeConfiguration<Table>
    {
        public void Configure(EntityTypeBuilder<Table> builder)
        {
            // Previously only enforced by TableRepository's manual .Where(!t.IsDeleted) - this
            // closes the gap for any query against the Tables DbSet that forgets to add it.
            builder.HasQueryFilter(t => !t.IsDeleted);

            // DB-level backstop for the app-level duplicate check.
            builder.HasIndex(t => t.Number)
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");
        }
    }
}
