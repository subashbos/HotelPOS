using HotelPOS.Domain.Entities;

namespace HotelPOS.Application.Interfaces
{
    public interface IEstimationService
    {
        Task<List<Estimation>> GetEstimationsAsync();
        Task<Estimation?> GetEstimationByIdAsync(int id);
        Task SaveEstimationAsync(Estimation estimation);
        Task UpdateEstimationAsync(Estimation estimation);
        Task DeleteEstimationAsync(int id);

        /// <summary>
        /// Converts an <see cref="EstimationStatuses.Accepted"/> estimation into a real <see cref="Order"/>
        /// (via <see cref="IOrderService.SaveOrderAsync"/>, which reprices lines from the authoritative
        /// item catalog), stamps the estimation as <see cref="EstimationStatuses.Converted"/>, and returns
        /// the new order's ID.
        /// </summary>
        Task<int> ConvertToOrderAsync(int estimationId);
    }
}
