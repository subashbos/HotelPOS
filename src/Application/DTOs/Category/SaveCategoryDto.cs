namespace HotelPOS.Application.DTOs.Category
{
    /// <summary>DTO for creating or updating a Category.</summary>
    public class SaveCategoryDto
    {
        public string Name { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }
    }
}
