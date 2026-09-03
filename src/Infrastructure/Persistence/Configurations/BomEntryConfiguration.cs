using HotelPOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelPOS.Infrastructure.Persistence.Configurations
{
    public class BomEntryConfiguration : IEntityTypeConfiguration<BomEntry>
    {
        public void Configure(EntityTypeBuilder<BomEntry> builder)
        {
            builder.HasOne(b => b.Item)
                .WithMany()
                .HasForeignKey(b => b.ItemId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(b => b.RawMaterial)
                .WithMany(r => r.BomEntries)
                .HasForeignKey(b => b.RawMaterialId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(b => new { b.ItemId, b.RawMaterialId })
                .IsUnique();
        }
    }
}
