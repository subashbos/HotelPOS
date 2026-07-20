using HotelPOS.Domain.Entities;
using HotelPOS.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HotelPOS.Infrastructure.Persistence
{
    public class ExpenseRepository : IExpenseRepository
    {
        private readonly HotelDbContext _context;

        public ExpenseRepository(HotelDbContext context)
        {
            _context = context;
        }

        public async Task<List<Expense>> GetAllAsync(DateTime? from, DateTime? to)
        {
            var query = _context.Expenses.AsQueryable();

            if (from.HasValue)
                query = query.Where(e => e.Date >= from.Value);

            if (to.HasValue)
                query = query.Where(e => e.Date < to.Value);

            return await query.OrderByDescending(e => e.Date).ThenByDescending(e => e.Id).ToListAsync();
        }

        public async Task<Expense?> GetByIdAsync(int id)
        {
            return await _context.Expenses.FindAsync(id);
        }

        public async Task AddAsync(Expense expense)
        {
            _context.Expenses.Add(expense);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Expense expense)
        {
            _context.Expenses.Update(expense);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var expense = await GetByIdAsync(id);
            if (expense != null)
            {
                _context.Expenses.Remove(expense);
                await _context.SaveChangesAsync();
            }
        }
    }
}
