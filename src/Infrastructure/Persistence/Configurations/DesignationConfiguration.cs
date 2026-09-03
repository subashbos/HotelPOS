using HotelPOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelPOS.Infrastructure.Persistence.Configurations
{
    public class DesignationConfiguration : IEntityTypeConfiguration<Designation>
    {
        public void Configure(EntityTypeBuilder<Designation> builder)
        {
            builder.HasOne(d => d.Department)
                .WithMany()
                .HasForeignKey(d => d.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
