using System.ComponentModel.DataAnnotations.Schema;

namespace HotelPOS.Domain.Entities
{
    public class UnitOfMeasurement
    {
        [NotMapped]
        public int SNo { get; set; }

        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int DisplayOrder { get; set; } = 0;
    }
}
