using HotelPOS.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HotelPOS.Application.Interfaces
{
    public interface IExpenseRepository
    {
        Task<List<Expense>> GetAllAsync(DateTime? from, DateTime? to);
        Task<Expense?> GetByIdAsync(int id);
        Task AddAsync(Expense expense);
        Task UpdateAsync(Expense expense);
        Task DeleteAsync(int id);
    }
}
