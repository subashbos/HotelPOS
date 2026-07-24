using HotelPOS.Domain.Entities;
using HotelPOS.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace HotelPOS.Infrastructure.Persistence
{
    public class UnitOfMeasurementRepository : GenericRepository<UnitOfMeasurement>, IUnitOfMeasurementRepository
    {
        public UnitOfMeasurementRepository(HotelDbContext context) : base(context)
        {
        }

        public override async Task<List<UnitOfMeasurement>> GetAllAsync()
        {
            return await _dbSet.OrderBy(u => u.DisplayOrder).ThenBy(u => u.Name).ToListAsync();
        }
    }
}
