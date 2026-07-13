using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Entities;
using MediatR;

namespace HotelPOS.Application.UseCases.Expenses.Queries
{
    public record GetExpensesQuery(DateTime? From, DateTime? To) : IRequest<List<Expense>>;

    public class GetExpensesQueryHandler : IRequestHandler<GetExpensesQuery, List<Expense>>
    {
        private readonly IExpenseRepository _repository;

        public GetExpensesQueryHandler(IExpenseRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<Expense>> Handle(GetExpensesQuery request, CancellationToken cancellationToken)
        {
            return await _repository.GetAllAsync(request.From, request.To) ?? new List<Expense>();
        }
    }
}
