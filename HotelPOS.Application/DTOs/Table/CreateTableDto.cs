namespace HotelPOS.Application.DTOs.Table
{
    public class CreateTableDto
    {
        public int Number { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Capacity { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
