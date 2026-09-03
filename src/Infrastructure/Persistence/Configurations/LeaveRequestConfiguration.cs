using HotelPOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelPOS.Infrastructure.Persistence.Configurations
{
    public class LeaveRequestConfiguration : IEntityTypeConfiguration<LeaveRequest>
    {
        public void Configure(EntityTypeBuilder<LeaveRequest> builder)
        {
            builder.HasOne(r => r.Employee)
                .WithMany()
                .HasForeignKey(r => r.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(r => r.LeaveType)
                .WithMany()
                .HasForeignKey(r => r.LeaveTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(r => r.ApprovedByEmployee)
                .WithMany()
                .HasForeignKey(r => r.ApprovedByEmployeeId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
