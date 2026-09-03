using HotelPOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelPOS.Infrastructure.Persistence.Configurations
{
    public class WastageEntryConfiguration : IEntityTypeConfiguration<WastageEntry>
    {
        public void Configure(EntityTypeBuilder<WastageEntry> builder)
        {
            builder.HasIndex(w => w.WastedAt);
        }
    }
}
