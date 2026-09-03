using HotelPOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelPOS.Infrastructure.Persistence.Configurations
{
    public class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
    {
        public void Configure(EntityTypeBuilder<Employee> builder)
        {
            builder.HasOne(e => e.Department)
                .WithMany()
                .HasForeignKey(e => e.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(e => e.Designation)
                .WithMany()
                .HasForeignKey(e => e.DesignationId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(e => e.ReportingManager)
                .WithMany()
                .HasForeignKey(e => e.ReportingManagerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(e => e.EmployeeCode)
                .IsUnique();

            // PII encryption at rest.
            var piiConverter = new EncryptedStringConverter();
            builder.Property(e => e.Pan).HasConversion(piiConverter);
            builder.Property(e => e.Aadhaar).HasConversion(piiConverter);
            builder.Property(e => e.Uan).HasConversion(piiConverter);
            builder.Property(e => e.EsicNumber).HasConversion(piiConverter);
            builder.Property(e => e.BankAccountNumber).HasConversion(piiConverter);
            builder.Property(e => e.BankIfsc).HasConversion(piiConverter);
        }
    }
}
