using HotelPOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelPOS.Infrastructure.Persistence.Configurations
{
    public class PayrollRunConfiguration : IEntityTypeConfiguration<PayrollRun>
    {
        public void Configure(EntityTypeBuilder<PayrollRun> builder)
        {
            builder.HasIndex(p => new { p.Month, p.Year })
                .IsUnique();
        }
    }
}
