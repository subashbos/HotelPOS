using HotelPOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelPOS.Infrastructure.Persistence.Configurations
{
    public class RememberMeTokenConfiguration : IEntityTypeConfiguration<RememberMeToken>
    {
        public void Configure(EntityTypeBuilder<RememberMeToken> builder)
        {
            builder.HasIndex(t => t.TokenHash)
                .IsUnique();
        }
    }
}
