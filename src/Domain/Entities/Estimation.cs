using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using HotelPOS.Domain.Common.Constants;

namespace HotelPOS.Domain.Entities
{
    /// <summary>A customer quotation/cost estimate that can later be converted into a real <see cref="Order"/>.</summary>
    public class Estimation
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string EstimationNumber { get; set; } = string.Empty;

        public DateTime EstimationDate { get; set; }

        /// <summary>Optional expiry date after which the quotation should no longer be accepted.</summary>
        public DateTime? ValidUntil { get; set; }

        /// <summary>Optional link to a CRM <see cref="Customer"/> profile. Null for a prospect without a saved profile.</summary>
        public int? CustomerId { get; set; }
        public Customer? Customer { get; set; }

        [MaxLength(150)]
        public string? CustomerName { get; set; }

        [MaxLength(20)]
        public string? CustomerPhone { get; set; }

        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = EstimationStatuses.Draft;

        [MaxLength(500)]
        public string? Notes { get; set; }

        public decimal Subtotal { get; set; }
        public decimal TotalTax { get; set; }
        public decimal TotalDiscount { get; set; }
        public decimal GrandTotal { get; set; }

        /// <summary>Set once this estimation has been converted into a real <see cref="Order"/>.</summary>
        public int? ConvertedOrderId { get; set; }
        public Order? ConvertedOrder { get; set; }

        public List<EstimationItem> EstimationItems { get; set; } = new();
    }
}
