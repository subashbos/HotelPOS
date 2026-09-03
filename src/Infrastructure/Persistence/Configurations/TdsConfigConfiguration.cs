using HotelPOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelPOS.Infrastructure.Persistence.Configurations
{
    public class TdsConfigConfiguration : IEntityTypeConfiguration<TdsConfig>
    {
        public void Configure(EntityTypeBuilder<TdsConfig> builder)
        {
            builder.HasIndex(c => c.FinancialYearStart)
                .IsUnique();
        }
    }
}
