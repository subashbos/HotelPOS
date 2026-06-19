namespace HotelPOS.Application.DTOs.Table
{
    public class TableDto
    {
        public int SNo { get; set; }
        public int Id { get; set; }
        public int Number { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Capacity { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsDeleted { get; set; }
    }
}
