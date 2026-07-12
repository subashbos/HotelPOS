using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Entities;
using MediatR;

namespace HotelPOS.Application.UseCases.Expenses.Queries
{
    public record GetExpenseByIdQuery(int Id) : IRequest<Expense?>;

    public class GetExpenseByIdQueryHandler : IRequestHandler<GetExpenseByIdQuery, Expense?>
    {
        private readonly IExpenseRepository _repository;

        public GetExpenseByIdQueryHandler(IExpenseRepository repository)
        {
            _repository = repository;
        }

        public async Task<Expense?> Handle(GetExpenseByIdQuery request, CancellationToken cancellationToken)
        {
            return await _repository.GetByIdAsync(request.Id);
        }
    }
}
