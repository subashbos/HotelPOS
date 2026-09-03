using HotelPOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelPOS.Infrastructure.Persistence.Configurations
{
    public class ItemConfiguration : IEntityTypeConfiguration<Item>
    {
        public void Configure(EntityTypeBuilder<Item> builder)
        {
            // Deleting a sold item must not erase the cost-price history that past-period
            // P&L/margin reports depend on.
            builder.HasQueryFilter(i => !i.IsDeleted);

            // Prevent deleting a unit of measurement that's still referenced by menu items
            // (the application layer also blocks this, but this is a DB-level backstop).
            builder.HasOne(i => i.Unit)
                .WithMany()
                .HasForeignKey(i => i.UnitId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
