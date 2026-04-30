namespace HotelPOS.Application
{
    public class CreateOrderDto
    {
        public decimal TotalAmount { get; set; }
        public string CustomerName { get; set; } = string.Empty;
    }

}
