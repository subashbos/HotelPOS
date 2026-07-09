using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Entities;
using HotelPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HotelPOS.Services
{
    public class BomService : IBomService
    {
        private readonly HotelDbContext _db;

        public BomService(HotelDbContext db)
        {
            _db = db;
        }

        // ── Raw Materials ─────────────────────────────────────────────────────

        public async Task<List<RawMaterial>> GetAllRawMaterialsAsync()
            => await _db.RawMaterials.OrderBy(r => r.Name).ToListAsync();

        public async Task<RawMaterial?> GetRawMaterialByIdAsync(int id)
            => await _db.RawMaterials.FindAsync(id);

        public async Task<RawMaterial> SaveRawMaterialAsync(RawMaterial rawMaterial)
        {
            if (rawMaterial.Id == 0)
            {
                _db.RawMaterials.Add(rawMaterial);
            }
            else
            {
                rawMaterial.Version++;
                _db.RawMaterials.Update(rawMaterial);
            }

            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new InvalidOperationException(
                    $"Raw material '{rawMaterial.Name}' was modified by another user. Please reload and try again.");
            }

            return rawMaterial;
        }

        public async Task DeleteRawMaterialAsync(int id)
        {
            var entity = await _db.RawMaterials.FindAsync(id)
                ?? throw new KeyNotFoundException($"Raw material #{id} not found.");

            bool isUsed = await _db.BomEntries.AnyAsync(b => b.RawMaterialId == id);
            if (isUsed)
                throw new InvalidOperationException("Cannot delete a raw material that is used in a recipe. Remove it from all recipes first.");

            _db.RawMaterials.Remove(entity);
            await _db.SaveChangesAsync();
        }

        // ── BOM Entries ───────────────────────────────────────────────────────

        public async Task<List<BomEntry>> GetBomForItemAsync(int itemId)
            => await _db.BomEntries
                .Include(b => b.RawMaterial)
                .Where(b => b.ItemId == itemId)
                .ToListAsync();

        public async Task SaveBomAsync(int itemId, List<BomEntry> entries)
        {
            var existing = _db.BomEntries.Where(b => b.ItemId == itemId);
            _db.BomEntries.RemoveRange(existing);

            foreach (var entry in entries)
            {
                entry.ItemId = itemId;
                entry.Id = 0;
                _db.BomEntries.Add(entry);
            }

            await _db.SaveChangesAsync();
            await SyncItemCostPriceAsync(itemId);
        }

        public async Task DeleteBomEntryAsync(int bomEntryId)
        {
            var entry = await _db.BomEntries.FindAsync(bomEntryId)
                ?? throw new KeyNotFoundException($"BOM entry #{bomEntryId} not found.");
            var itemId = entry.ItemId;
            _db.BomEntries.Remove(entry);
            await _db.SaveChangesAsync();
            await SyncItemCostPriceAsync(itemId);
        }

        // ── Costing ───────────────────────────────────────────────────────────

        public async Task<decimal> CalculateItemCostAsync(int itemId)
        {
            var entries = await GetBomForItemAsync(itemId);
            return entries.Sum(e => e.IngredientCost);
        }

        /// <summary>Keeps Item.CostPrice (used by BI/COGS reporting) in sync with the recipe's computed cost.</summary>
        private async Task SyncItemCostPriceAsync(int itemId)
        {
            var item = await _db.Items.FindAsync(itemId);
            if (item == null) return;

            item.CostPrice = await CalculateItemCostAsync(itemId);
            await _db.SaveChangesAsync();
        }

        // ── Stock Deduction ───────────────────────────────────────────────────

        public async Task DeductIngredientStockAsync(int itemId, int quantity)
        {
            var entries = await _db.BomEntries
                .Include(b => b.RawMaterial)
                .Where(b => b.ItemId == itemId)
                .ToListAsync();

            if (entries.Count == 0) return; // item has no recipe defined - nothing to deduct

            if (quantity > 0)
            {
                foreach (var entry in entries)
                {
                    if (entry.RawMaterial == null) continue;
                    var required = entry.EffectiveQuantity * quantity;
                    if (entry.RawMaterial.CurrentStock < required)
                    {
                        throw new InvalidOperationException(
                            $"Insufficient stock for raw material: {entry.RawMaterial.Name}. Required: {required}, Available: {entry.RawMaterial.CurrentStock}");
                    }
                }
            }

            foreach (var entry in entries)
            {
                if (entry.RawMaterial == null) continue;
                entry.RawMaterial.CurrentStock -= entry.EffectiveQuantity * quantity;
            }

            await _db.SaveChangesAsync();
        }
    }
}
