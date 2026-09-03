using HotelPOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelPOS.Infrastructure.Persistence.Configurations
{
    public class SalaryStructureConfiguration : IEntityTypeConfiguration<SalaryStructure>
    {
        public void Configure(EntityTypeBuilder<SalaryStructure> builder)
        {
            builder.HasOne(s => s.Employee)
                .WithMany()
                .HasForeignKey(s => s.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
