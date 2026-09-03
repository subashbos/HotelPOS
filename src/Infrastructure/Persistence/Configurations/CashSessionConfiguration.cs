using HotelPOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelPOS.Infrastructure.Persistence.Configurations
{
    public class CashSessionConfiguration : IEntityTypeConfiguration<CashSession>
    {
        public void Configure(EntityTypeBuilder<CashSession> builder)
        {
            // DB-level backstop closing the open/open TOCTOU race.
            builder.HasIndex(s => s.Status)
                .IsUnique()
                .HasFilter("[Status] = 'Open'");

            // Date-range filtered in GetShiftClosureReportAsync.
            builder.HasIndex(s => s.OpenedAt);
        }
    }
}
