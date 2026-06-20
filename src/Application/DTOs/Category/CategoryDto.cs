namespace HotelPOS.Application.DTOs.Category
{
    public class CategoryDto
    {
        public int SNo { get; set; }
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int DisplayOrder { get; set; } = 0;
    }
}
