using System.ComponentModel.DataAnnotations.Schema;

namespace HotelPOS.Domain
{
    public class Table
    {
        [NotMapped]
        public int SNo { get; set; }

        public int Id { get; set; }
        public int Number { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Capacity { get; set; }
        public bool IsActive { get; set; } = true;
        
        // For soft delete if needed
        public bool IsDeleted { get; set; }
    }
}
