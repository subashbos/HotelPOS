using HotelPOS.Domain.Entities;
using HotelPOS.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HotelPOS.Infrastructure.Persistence
{
    public class EstimationRepository : IEstimationRepository
    {
        private readonly HotelDbContext _context;

        public EstimationRepository(HotelDbContext context)
        {
            _context = context;
        }

        public async Task<List<Estimation>> GetEstimationsAsync()
        {
            return await _context.Estimations
                .Include(e => e.Customer)
                .Include(e => e.EstimationItems)
                .OrderByDescending(e => e.EstimationDate)
                .ThenByDescending(e => e.Id)
                .ToListAsync();
        }

        public async Task<Estimation?> GetByIdAsync(int id)
        {
            return await _context.Estimations
                .Include(e => e.Customer)
                .Include(e => e.EstimationItems)
                .FirstOrDefaultAsync(e => e.Id == id);
        }

        public async Task AddAsync(Estimation estimation)
        {
            _context.Estimations.Add(estimation);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Estimation estimation)
        {
            var existing = await _context.Estimations
                .Include(e => e.EstimationItems)
                .FirstOrDefaultAsync(e => e.Id == estimation.Id);
            if (existing == null) throw new KeyNotFoundException($"Estimation #{estimation.Id} not found.");

            existing.EstimationNumber = estimation.EstimationNumber;
            existing.EstimationDate = estimation.EstimationDate;
            existing.ValidUntil = estimation.ValidUntil;
            existing.CustomerId = estimation.CustomerId;
            existing.CustomerName = estimation.CustomerName;
            existing.CustomerPhone = estimation.CustomerPhone;
            existing.Status = estimation.Status;
            existing.Notes = estimation.Notes;
            existing.Subtotal = estimation.Subtotal;
            existing.TotalTax = estimation.TotalTax;
            existing.TotalDiscount = estimation.TotalDiscount;
            existing.GrandTotal = estimation.GrandTotal;
            existing.ConvertedOrderId = estimation.ConvertedOrderId;

            // Replace items wholesale (simpler than syncing individual rows), same pattern as
            // PurchaseRepository.UpdateAsync / OrderRepository.UpdateAsync.
            _context.EstimationItems.RemoveRange(existing.EstimationItems);
            existing.EstimationItems = estimation.EstimationItems;

            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var existing = await _context.Estimations.FirstOrDefaultAsync(e => e.Id == id);
            if (existing == null) return;

            _context.Estimations.Remove(existing);
            await _context.SaveChangesAsync();
        }
    }
}
