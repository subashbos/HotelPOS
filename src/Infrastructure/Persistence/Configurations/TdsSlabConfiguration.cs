using HotelPOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelPOS.Infrastructure.Persistence.Configurations
{
    public class TdsSlabConfiguration : IEntityTypeConfiguration<TdsSlab>
    {
        public void Configure(EntityTypeBuilder<TdsSlab> builder)
        {
            builder.HasIndex(s => new { s.FinancialYearStart, s.DisplayOrder })
                .IsUnique();
        }
    }
}
