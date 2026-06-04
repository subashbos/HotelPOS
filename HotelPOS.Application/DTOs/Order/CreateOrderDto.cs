namespace HotelPOS.Application.DTOs.Order
{
    public class CreateOrderDto
    {
        public decimal TotalAmount { get; set; }
        public string CustomerName { get; set; } = string.Empty;
    }

}
