using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HotelPOS.Domain.Entities;
using HotelPOS.Infrastructure.Persistence;
using HotelPOS.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HotelPOS.Tests
{
    public class BomServiceTests
    {
        private HotelDbContext GetContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<HotelDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;
            return new HotelDbContext(options);
        }

        [Fact]
        public async Task SaveRawMaterialAsync_NewMaterial_AddsToDb()
        {
            using var context = GetContext(nameof(SaveRawMaterialAsync_NewMaterial_AddsToDb));
            var service = new BomService(context);

            var saved = await service.SaveRawMaterialAsync(new RawMaterial { Name = "Chicken", Unit = "kg", CostPerUnit = 250 });

            Assert.NotEqual(0, saved.Id);
            Assert.Equal(1, await context.RawMaterials.CountAsync());
        }

        [Fact]
        public async Task GetAllRawMaterialsAsync_ReturnsOrderedByName()
        {
            using var context = GetContext(nameof(GetAllRawMaterialsAsync_ReturnsOrderedByName));
            context.RawMaterials.AddRange(
                new RawMaterial { Id = 1, Name = "Salt", Unit = "kg" },
                new RawMaterial { Id = 2, Name = "Chicken", Unit = "kg" });
            await context.SaveChangesAsync();

            var service = new BomService(context);
            var result = await service.GetAllRawMaterialsAsync();

            Assert.Equal(new[] { "Chicken", "Salt" }, result.Select(r => r.Name));
        }

        [Fact]
        public async Task DeleteRawMaterialAsync_NotUsedInRecipe_DeletesSuccessfully()
        {
            using var context = GetContext(nameof(DeleteRawMaterialAsync_NotUsedInRecipe_DeletesSuccessfully));
            context.RawMaterials.Add(new RawMaterial { Id = 1, Name = "Chicken", Unit = "kg" });
            await context.SaveChangesAsync();

            var service = new BomService(context);
            await service.DeleteRawMaterialAsync(1);

            Assert.Equal(0, await context.RawMaterials.CountAsync());
        }

        [Fact]
        public async Task DeleteRawMaterialAsync_UsedInRecipe_ThrowsInvalidOperationException()
        {
            using var context = GetContext(nameof(DeleteRawMaterialAsync_UsedInRecipe_ThrowsInvalidOperationException));
            context.Items.Add(new Item { Id = 1, Name = "Chicken Curry", Price = 200 });
            context.RawMaterials.Add(new RawMaterial { Id = 1, Name = "Chicken", Unit = "kg" });
            context.BomEntries.Add(new BomEntry { Id = 1, ItemId = 1, RawMaterialId = 1, QuantityRequired = 0.5m });
            await context.SaveChangesAsync();

            var service = new BomService(context);

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.DeleteRawMaterialAsync(1));
            Assert.Equal(1, await context.RawMaterials.CountAsync());
        }

        [Fact]
        public async Task DeleteRawMaterialAsync_NotFound_ThrowsKeyNotFoundException()
        {
            using var context = GetContext(nameof(DeleteRawMaterialAsync_NotFound_ThrowsKeyNotFoundException));
            var service = new BomService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.DeleteRawMaterialAsync(999));
        }

        [Fact]
        public async Task SaveBomAsync_ReplacesExistingEntries()
        {
            using var context = GetContext(nameof(SaveBomAsync_ReplacesExistingEntries));
            context.Items.Add(new Item { Id = 1, Name = "Chicken Curry", Price = 200 });
            context.RawMaterials.AddRange(
                new RawMaterial { Id = 1, Name = "Chicken", Unit = "kg" },
                new RawMaterial { Id = 2, Name = "Onion", Unit = "kg" });
            context.BomEntries.Add(new BomEntry { Id = 1, ItemId = 1, RawMaterialId = 1, QuantityRequired = 1 });
            await context.SaveChangesAsync();

            var service = new BomService(context);
            await service.SaveBomAsync(1, new List<BomEntry>
            {
                new BomEntry { RawMaterialId = 2, QuantityRequired = 0.3m }
            });

            var entries = await context.BomEntries.Where(b => b.ItemId == 1).ToListAsync();
            Assert.Single(entries);
            Assert.Equal(2, entries[0].RawMaterialId);
        }

        [Fact]
        public async Task SaveBomAsync_SyncsItemCostPriceFromRecipeCost()
        {
            using var context = GetContext(nameof(SaveBomAsync_SyncsItemCostPriceFromRecipeCost));
            context.Items.Add(new Item { Id = 1, Name = "Chicken Curry", Price = 200, CostPrice = 0 });
            context.RawMaterials.Add(new RawMaterial { Id = 1, Name = "Chicken", Unit = "kg", CostPerUnit = 200 });
            await context.SaveChangesAsync();

            var service = new BomService(context);
            await service.SaveBomAsync(1, new List<BomEntry>
            {
                new BomEntry { RawMaterialId = 1, QuantityRequired = 0.5m, WastagePercentage = 10 } // 0.55 * 200 = 110
            });

            var item = await context.Items.FindAsync(1);
            Assert.Equal(110m, item!.CostPrice);
        }

        [Fact]
        public async Task DeleteBomEntryAsync_ResyncsItemCostPriceAfterRemoval()
        {
            using var context = GetContext(nameof(DeleteBomEntryAsync_ResyncsItemCostPriceAfterRemoval));
            context.Items.Add(new Item { Id = 1, Name = "Chicken Curry", Price = 200, CostPrice = 130 });
            context.RawMaterials.AddRange(
                new RawMaterial { Id = 1, Name = "Chicken", Unit = "kg", CostPerUnit = 200 },
                new RawMaterial { Id = 2, Name = "Onion", Unit = "kg", CostPerUnit = 30 });
            context.BomEntries.AddRange(
                new BomEntry { Id = 1, ItemId = 1, RawMaterialId = 1, QuantityRequired = 0.5m }, // 100
                new BomEntry { Id = 2, ItemId = 1, RawMaterialId = 2, QuantityRequired = 1 });    // 30
            await context.SaveChangesAsync();

            var service = new BomService(context);
            await service.DeleteBomEntryAsync(2); // remove the onion line

            var item = await context.Items.FindAsync(1);
            Assert.Equal(100m, item!.CostPrice);
        }

        [Fact]
        public async Task GetBomForItemAsync_IncludesRawMaterial()
        {
            using var context = GetContext(nameof(GetBomForItemAsync_IncludesRawMaterial));
            context.Items.Add(new Item { Id = 1, Name = "Chicken Curry", Price = 200 });
            context.RawMaterials.Add(new RawMaterial { Id = 1, Name = "Chicken", Unit = "kg", CostPerUnit = 250 });
            context.BomEntries.Add(new BomEntry { Id = 1, ItemId = 1, RawMaterialId = 1, QuantityRequired = 0.5m });
            await context.SaveChangesAsync();

            var service = new BomService(context);
            var entries = await service.GetBomForItemAsync(1);

            Assert.Single(entries);
            Assert.NotNull(entries[0].RawMaterial);
            Assert.Equal("Chicken", entries[0].RawMaterial!.Name);
        }

        [Fact]
        public async Task CalculateItemCostAsync_SumsIngredientCostsWithWastage()
        {
            using var context = GetContext(nameof(CalculateItemCostAsync_SumsIngredientCostsWithWastage));
            context.Items.Add(new Item { Id = 1, Name = "Chicken Curry", Price = 200 });
            context.RawMaterials.AddRange(
                new RawMaterial { Id = 1, Name = "Chicken", Unit = "kg", CostPerUnit = 200 },
                new RawMaterial { Id = 2, Name = "Onion", Unit = "kg", CostPerUnit = 30 });
            context.BomEntries.AddRange(
                new BomEntry { Id = 1, ItemId = 1, RawMaterialId = 1, QuantityRequired = 0.5m, WastagePercentage = 20 }, // 0.6 * 200 = 120
                new BomEntry { Id = 2, ItemId = 1, RawMaterialId = 2, QuantityRequired = 0.2m }); // 0.2 * 30 = 6
            await context.SaveChangesAsync();

            var service = new BomService(context);
            var cost = await service.CalculateItemCostAsync(1);

            Assert.Equal(126m, cost);
        }

        [Fact]
        public async Task DeductIngredientStockAsync_ItemHasNoRecipe_DoesNothing()
        {
            using var context = GetContext(nameof(DeductIngredientStockAsync_ItemHasNoRecipe_DoesNothing));
            context.Items.Add(new Item { Id = 1, Name = "Plain Water", Price = 20 });
            await context.SaveChangesAsync();

            var service = new BomService(context);
            await service.DeductIngredientStockAsync(1, 5); // no BomEntries for this item - should be a no-op, not throw
        }

        [Fact]
        public async Task DeductIngredientStockAsync_NegativeQuantity_RestoresStockWithoutCheckingAvailability()
        {
            using var context = GetContext(nameof(DeductIngredientStockAsync_NegativeQuantity_RestoresStockWithoutCheckingAvailability));
            context.Items.Add(new Item { Id = 1, Name = "Chicken Curry", Price = 200 });
            context.RawMaterials.Add(new RawMaterial { Id = 1, Name = "Chicken", Unit = "kg", CostPerUnit = 200, CurrentStock = 5 });
            context.BomEntries.Add(new BomEntry { Id = 1, ItemId = 1, RawMaterialId = 1, QuantityRequired = 1, WastagePercentage = 10 });
            await context.SaveChangesAsync();

            var service = new BomService(context);
            await service.DeductIngredientStockAsync(1, -2); // refund of 2 servings

            var material = await context.RawMaterials.FindAsync(1);
            Assert.Equal(7.2m, material!.CurrentStock); // 5 + (1 * 1.1 * 2)
        }

        [Fact]
        public async Task DeductIngredientStockAsync_SufficientStock_DeductsEffectiveQuantity()
        {
            using var context = GetContext(nameof(DeductIngredientStockAsync_SufficientStock_DeductsEffectiveQuantity));
            context.Items.Add(new Item { Id = 1, Name = "Chicken Curry", Price = 200 });
            context.RawMaterials.Add(new RawMaterial { Id = 1, Name = "Chicken", Unit = "kg", CostPerUnit = 200, CurrentStock = 10 });
            context.BomEntries.Add(new BomEntry { Id = 1, ItemId = 1, RawMaterialId = 1, QuantityRequired = 1, WastagePercentage = 10 });
            await context.SaveChangesAsync();

            var service = new BomService(context);
            await service.DeductIngredientStockAsync(1, 2);

            var material = await context.RawMaterials.FindAsync(1);
            Assert.Equal(7.8m, material!.CurrentStock); // 10 - (1 * 1.1 * 2)
        }

        [Fact]
        public async Task DeductIngredientStockAsync_InsufficientStock_ThrowsAndDoesNotDeductAnyIngredient()
        {
            using var context = GetContext(nameof(DeductIngredientStockAsync_InsufficientStock_ThrowsAndDoesNotDeductAnyIngredient));
            context.Items.Add(new Item { Id = 1, Name = "Chicken Curry", Price = 200 });
            context.RawMaterials.AddRange(
                new RawMaterial { Id = 1, Name = "Chicken", Unit = "kg", CostPerUnit = 200, CurrentStock = 10 },
                new RawMaterial { Id = 2, Name = "Onion", Unit = "kg", CostPerUnit = 30, CurrentStock = 1 });
            context.BomEntries.AddRange(
                new BomEntry { Id = 1, ItemId = 1, RawMaterialId = 1, QuantityRequired = 1 },
                new BomEntry { Id = 2, ItemId = 1, RawMaterialId = 2, QuantityRequired = 5 }); // insufficient
            await context.SaveChangesAsync();

            var service = new BomService(context);

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.DeductIngredientStockAsync(1, 1));

            var chicken = await context.RawMaterials.FindAsync(1);
            var onion = await context.RawMaterials.FindAsync(2);
            Assert.Equal(10m, chicken!.CurrentStock);
            Assert.Equal(1m, onion!.CurrentStock);
        }

        [Fact]
        public async Task DeleteBomEntryAsync_NotFound_ThrowsKeyNotFoundException()
        {
            using var context = GetContext(nameof(DeleteBomEntryAsync_NotFound_ThrowsKeyNotFoundException));
            var service = new BomService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.DeleteBomEntryAsync(999));
        }

        [Fact]
        public async Task DeleteBomEntryAsync_Found_RemovesEntry()
        {
            using var context = GetContext(nameof(DeleteBomEntryAsync_Found_RemovesEntry));
            context.Items.Add(new Item { Id = 1, Name = "Chicken Curry", Price = 200 });
            context.RawMaterials.Add(new RawMaterial { Id = 1, Name = "Chicken", Unit = "kg" });
            context.BomEntries.Add(new BomEntry { Id = 1, ItemId = 1, RawMaterialId = 1, QuantityRequired = 1 });
            await context.SaveChangesAsync();

            var service = new BomService(context);
            await service.DeleteBomEntryAsync(1);

            Assert.Equal(0, await context.BomEntries.CountAsync());
        }
    }
}
