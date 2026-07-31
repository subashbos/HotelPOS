using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Common.Constants;
using MediatR;

namespace HotelPOS.Application.UseCases.Expenses.Commands
{
    public record DeleteExpenseCommand(int Id) : IRequest;

    public class DeleteExpenseCommandHandler : IRequestHandler<DeleteExpenseCommand>
    {
        private readonly IExpenseRepository _repository;
        private readonly IAuthorizationService _authorization;

        public DeleteExpenseCommandHandler(IExpenseRepository repository, IAuthorizationService authorization)
        {
            _repository = repository;
            _authorization = authorization;
        }

        public async Task Handle(DeleteExpenseCommand request, CancellationToken cancellationToken)
        {
            _authorization.EnsurePermission(PermissionModules.Expenses);

            _ = await _repository.GetByIdAsync(request.Id)
                ?? throw new KeyNotFoundException($"Expense #{request.Id} not found.");

            await _repository.DeleteAsync(request.Id);
        }
    }
}
