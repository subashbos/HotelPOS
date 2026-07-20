using HotelPOS.Application.DTOs.Expense;
using HotelPOS.Application.Interfaces;
using HotelPOS.Application.UseCases.Expenses.Commands;
using HotelPOS.Application.UseCases.Expenses.Queries;
using HotelPOS.Domain.Entities;
using MediatR;
using AutoMapper;

namespace HotelPOS.Application.UseCases
{
    public class ExpenseService : IExpenseService
    {
        private readonly IMediator? _mediator;
        private readonly IExpenseRepository? _expenseRepository;
        private readonly IMapper _mapper;

        /// <summary>DI constructor — uses MediatR pipeline (validators + handlers).</summary>
        public ExpenseService(IMediator mediator, IMapper mapper)
        {
            _mediator = mediator;
            _mapper = mapper;
        }

        /// <summary>Legacy constructor for unit tests that inject a repository directly.</summary>
        public ExpenseService(IExpenseRepository expenseRepository, IMapper? mapper = null)
        {
            _expenseRepository = expenseRepository;
            _mapper = mapper ?? CreateDefaultMapper();
        }

        private static IMapper CreateDefaultMapper()
        {
            var cfg = new MapperConfiguration(
                expr => expr.AddProfile(new HotelPOS.Application.Common.Mappings.MappingProfile()),
                Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
            return cfg.CreateMapper();
        }

        public async Task<List<Expense>> GetExpensesAsync(DateTime? from, DateTime? to)
        {
            if (_mediator != null)
                return await _mediator.Send(new GetExpensesQuery(from, to));

            return await _expenseRepository!.GetAllAsync(from, to) ?? new List<Expense>();
        }

        public async Task<Expense?> GetExpenseByIdAsync(int id)
        {
            if (_mediator != null)
                return await _mediator.Send(new GetExpenseByIdQuery(id));

            return await _expenseRepository!.GetByIdAsync(id);
        }

        public async Task SaveExpenseAsync(Expense expense)
        {
            if (expense == null) throw new ArgumentNullException(nameof(expense));

            if (_mediator != null)
            {
                var dto = _mapper.Map<SaveExpenseDto>(expense);
                await _mediator.Send(new SaveExpenseCommand(dto));
                return;
            }

            // Legacy path
            if (string.IsNullOrWhiteSpace(expense.Title))
                throw new ArgumentException("Title is required.");
            if (string.IsNullOrWhiteSpace(expense.Category))
                throw new ArgumentException("Category is required.");
            if (expense.Amount <= 0)
                throw new ArgumentException("Amount must be greater than zero.");

            expense.Title = expense.Title.Trim();
            expense.Category = expense.Category.Trim();

            if (expense.Id == 0)
                await _expenseRepository!.AddAsync(expense);
            else
                await _expenseRepository!.UpdateAsync(expense);
        }

        public async Task DeleteExpenseAsync(int id)
        {
            if (_mediator != null)
            {
                await _mediator.Send(new DeleteExpenseCommand(id));
                return;
            }

            _ = await _expenseRepository!.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"Expense #{id} not found.");
            await _expenseRepository.DeleteAsync(id);
        }
    }
}
