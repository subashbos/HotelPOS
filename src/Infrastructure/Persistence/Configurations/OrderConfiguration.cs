using HotelPOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelPOS.Infrastructure.Persistence.Configurations
{
    public class OrderConfiguration : IEntityTypeConfiguration<Order>
    {
        public void Configure(EntityTypeBuilder<Order> builder)
        {
            // Prevents deleted orders appearing in any query.
            builder.HasQueryFilter(o => !o.IsDeleted);

            builder.HasIndex(o => o.CreatedAt);

            builder.HasIndex(o => o.IsDeleted);

            builder.HasIndex(o => new { o.FiscalYear, o.InvoiceNumber })
                .IsUnique();

            // Filtered on directly in BI report aggregations (shift closure, staff performance,
            // stock valuation, P&L) and by ApplyBasicFilters for table-scoped order lookups.
            builder.HasIndex(o => o.Status);

            builder.HasIndex(o => o.TableNumber);

            builder.HasOne(o => o.Customer)
                .WithMany()
                .HasForeignKey(o => o.CustomerId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
