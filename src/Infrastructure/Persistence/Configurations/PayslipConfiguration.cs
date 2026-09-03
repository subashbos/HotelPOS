using HotelPOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelPOS.Infrastructure.Persistence.Configurations
{
    public class PayslipConfiguration : IEntityTypeConfiguration<Payslip>
    {
        public void Configure(EntityTypeBuilder<Payslip> builder)
        {
            builder.HasOne(p => p.PayrollRun)
                .WithMany(r => r.Payslips)
                .HasForeignKey(p => p.PayrollRunId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(p => p.Employee)
                .WithMany()
                .HasForeignKey(p => p.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(p => new { p.PayrollRunId, p.EmployeeId })
                .IsUnique();
        }
    }
}
