using HotelPOS.Domain.Entities;

namespace HotelPOS.Application.Interfaces
{
    public interface IBomService
    {
        // Raw Materials
        Task<List<RawMaterial>> GetAllRawMaterialsAsync();
        Task<RawMaterial?> GetRawMaterialByIdAsync(int id);
        Task<RawMaterial> SaveRawMaterialAsync(RawMaterial rawMaterial);
        Task DeleteRawMaterialAsync(int id);

        // BOM Entries (Recipe for a menu item)
        Task<List<BomEntry>> GetBomForItemAsync(int itemId);
        Task SaveBomAsync(int itemId, List<BomEntry> entries);
        Task DeleteBomEntryAsync(int bomEntryId);

        // Costing
        Task<decimal> CalculateItemCostAsync(int itemId);

        // Stock adjustment on order lifecycle events (deducts EffectiveQuantity per ingredient per serving).
        // Positive quantity deducts stock (order placed/increased) and throws InvalidOperationException on
        // insufficient stock; negative quantity restores stock (order voided/refunded/reduced) and never throws.
        Task DeductIngredientStockAsync(int itemId, int quantity);
    }
}
