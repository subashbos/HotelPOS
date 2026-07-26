using HotelPOS.Domain.Entities;

namespace HotelPOS.Application.Common.Models
{
    /// <summary>A financial year's TDS config paired with its slab bands, as resolved by <see cref="Interfaces.IPayrollRepository.GetTdsRuleSetAsync"/>.</summary>
    public class TdsRuleSet
    {
        public required TdsConfig Config { get; init; }

        public required List<TdsSlab> Slabs { get; init; }
    }
}
