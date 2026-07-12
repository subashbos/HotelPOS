using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HotelPOS.Domain.Common.Constants;

namespace HotelPOS.Domain.Entities
{
    public class Employee
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(20)]
        public string EmployeeCode { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? LastName { get; set; }

        [MaxLength(10)]
        public string? Gender { get; set; }

        public DateTime? DateOfBirth { get; set; }

        public DateTime DateOfJoining { get; set; }

        public DateTime? DateOfExit { get; set; }

        public int? DepartmentId { get; set; }

        [ForeignKey(nameof(DepartmentId))]
        public Department? Department { get; set; }

        public int? DesignationId { get; set; }

        [ForeignKey(nameof(DesignationId))]
        public Designation? Designation { get; set; }

        [MaxLength(30)]
        public string EmploymentType { get; set; } = EmploymentTypes.Permanent;

        [MaxLength(20)]
        public string Status { get; set; } = EmployeeStatuses.Active;

        [MaxLength(20)]
        public string? Phone { get; set; }

        [MaxLength(150)]
        public string? Email { get; set; }

        [MaxLength(300)]
        public string? Address { get; set; }

        /// <summary>Permanent Account Number — 10-character Indian tax ID.</summary>
        [MaxLength(10)]
        public string? Pan { get; set; }

        /// <summary>12-digit Indian national ID.</summary>
        [MaxLength(12)]
        public string? Aadhaar { get; set; }

        /// <summary>EPFO Universal Account Number for Provident Fund.</summary>
        [MaxLength(22)]
        public string? Uan { get; set; }

        /// <summary>ESIC insurance number, present only when the employee is ESI-covered.</summary>
        [MaxLength(20)]
        public string? EsicNumber { get; set; }

        [MaxLength(100)]
        public string? BankName { get; set; }

        [MaxLength(30)]
        public string? BankAccountNumber { get; set; }

        [MaxLength(15)]
        public string? BankIfsc { get; set; }

        [MaxLength(100)]
        public string? EmergencyContactName { get; set; }

        [MaxLength(20)]
        public string? EmergencyContactPhone { get; set; }

        public int? ReportingManagerId { get; set; }

        [ForeignKey(nameof(ReportingManagerId))]
        public Employee? ReportingManager { get; set; }

        /// <summary>Optional link to the login account this employee uses to sign in.</summary>
        public int? UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }
    }
}
