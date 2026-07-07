using System.ComponentModel.DataAnnotations.Schema;

namespace HotelPOS.Domain.Entities
{
    public class BomEntry
    {
        public int Id { get; set; }

        public int ItemId { get; set; }
        public Item? Item { get; set; }

        public int RawMaterialId { get; set; }
        public RawMaterial? RawMaterial { get; set; }

        /// <summary>How much raw material is needed per one serving (in the raw material's unit).</summary>
        public decimal QuantityRequired { get; set; }

        /// <summary>
        /// Wastage percentage during preparation (peeling, trimming, cooking).
        /// E.g., 20 means 20% wastage → effective quantity = QuantityRequired × 1.20
        /// </summary>
        public decimal WastagePercentage { get; set; } = 0;

        /// <summary>Effective quantity after wastage (computed, not stored).</summary>
        [NotMapped]
        public decimal EffectiveQuantity => QuantityRequired * (1 + WastagePercentage / 100m);

        /// <summary>Ingredient cost including wastage (computed, not stored).</summary>
        [NotMapped]
        public decimal IngredientCost => EffectiveQuantity * (RawMaterial?.CostPerUnit ?? 0);
    }
}
