using HotelPOS.Application.Interfaces;
using MediatR;

namespace HotelPOS.Application.UseCases.Expenses.Commands
{
    public record DeleteExpenseCommand(int Id) : IRequest;

    public class DeleteExpenseCommandHandler : IRequestHandler<DeleteExpenseCommand>
    {
        private readonly IExpenseRepository _repository;

        public DeleteExpenseCommandHandler(IExpenseRepository repository)
        {
            _repository = repository;
        }

        public async Task Handle(DeleteExpenseCommand request, CancellationToken cancellationToken)
        {
            _ = await _repository.GetByIdAsync(request.Id)
                ?? throw new KeyNotFoundException($"Expense #{request.Id} not found.");

            await _repository.DeleteAsync(request.Id);
        }
    }
}
