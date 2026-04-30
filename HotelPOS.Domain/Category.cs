using System.ComponentModel.DataAnnotations.Schema;

namespace HotelPOS.Domain
{
    public class Category
    {
        [NotMapped]
        public int SNo { get; set; }

        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
