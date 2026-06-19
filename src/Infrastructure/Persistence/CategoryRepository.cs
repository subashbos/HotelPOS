using HotelPOS.Domain.Entities;
using HotelPOS.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace HotelPOS.Infrastructure.Persistence
{
    public class CategoryRepository : GenericRepository<Category>, ICategoryRepository
    {
        public CategoryRepository(HotelDbContext context) : base(context)
        {
        }

        public override async Task<List<Category>> GetAllAsync()
        {
            return await _dbSet.OrderBy(c => c.Name).ToListAsync();
        }
    }
}
