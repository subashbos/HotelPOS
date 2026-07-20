using System.ComponentModel.DataAnnotations;

namespace HotelPOS.Domain.Entities
{
    public class RawMaterial
    {
        public int Id { get; set; }

        /// <summary>
        /// Optimistic concurrency token: detects lost updates from concurrent stock/cost edits.
        /// App-managed (incremented on every update) so it behaves identically across providers.
        /// </summary>
        [ConcurrencyCheck]
        public int Version { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        /// <summary>Unit of measurement: kg, g, litre, ml, pcs, etc.</summary>
        [Required]
        [MaxLength(20)]
        public string Unit { get; set; } = "kg";

        /// <summary>Cost per one unit (e.g., cost per kg).</summary>
        public decimal CostPerUnit { get; set; }

        /// <summary>Current stock level in the specified unit.</summary>
        public decimal CurrentStock { get; set; }

        /// <summary>Reorder alert threshold.</summary>
        public decimal MinStockThreshold { get; set; } = 0;

        // Navigation
        public ICollection<BomEntry> BomEntries { get; set; } = new List<BomEntry>();
    }
}
