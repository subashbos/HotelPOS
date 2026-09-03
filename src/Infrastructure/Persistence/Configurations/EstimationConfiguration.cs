using HotelPOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelPOS.Infrastructure.Persistence.Configurations
{
    public class EstimationConfiguration : IEntityTypeConfiguration<Estimation>
    {
        public void Configure(EntityTypeBuilder<Estimation> builder)
        {
            builder.HasOne(e => e.Customer)
                .WithMany()
                .HasForeignKey(e => e.CustomerId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasOne(e => e.ConvertedOrder)
                .WithMany()
                .HasForeignKey(e => e.ConvertedOrderId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasIndex(e => e.EstimationNumber)
                .IsUnique();

            // EstimationItem -> Item and EstimationItem -> Estimation relationships are left to
            // convention (required FK => Cascade), same as PurchaseItem's equivalent relationships.
        }
    }
}
