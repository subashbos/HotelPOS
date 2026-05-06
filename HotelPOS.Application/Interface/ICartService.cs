using HotelPOS.Domain;

namespace HotelPOS.Application.Interface
{
    public interface ICartService
    {
        void AddItem(int tableNumber, Item item);
        void AddItem(int tableNumber, int itemId, int quantity);
        void RemoveItem(int tableNumber, int itemId);
        void UpdateQuantity(int tableNumber, int itemId, int change);
        void SetQuantity(int tableNumber, int itemId, int quantity);
        void Clear(int tableNumber);
        List<OrderItem> GetItems(int tableNumber);
        decimal GetSubtotal(int tableNumber);
        decimal GetGstAmount(int tableNumber);
        decimal GetGrandTotal(int tableNumber);
        void LoadItems(int tableNumber, List<OrderItem> items);
        void UpdatePrice(int tableNumber, int itemId, decimal newPrice);
        
        // Hold Support
        void HoldOrder(int tableNumber, string holdName);
        List<HeldOrder> GetHeldOrders();
        void ResumeHeldOrder(Guid heldOrderId, int targetTableNumber);
        void TransferTable(int sourceTableNumber, int targetTableNumber);
        List<int> GetActiveTables();
    }

    public class HeldOrder
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string HoldName { get; set; } = string.Empty;
        public DateTime HeldAt { get; set; }
        public int TableNumber { get; set; }
        public List<OrderItem> Items { get; set; } = new();
    }
}
