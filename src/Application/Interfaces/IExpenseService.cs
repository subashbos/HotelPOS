using HotelPOS.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HotelPOS.Application.Interfaces
{
    public interface IExpenseService
    {
        Task<List<Expense>> GetExpensesAsync(DateTime? from, DateTime? to);
        Task<Expense?> GetExpenseByIdAsync(int id);
        Task SaveExpenseAsync(Expense expense);
        Task DeleteExpenseAsync(int id);
    }
}
