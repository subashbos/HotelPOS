using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Common.Constants;
using MediatR;

namespace HotelPOS.Application.UseCases.Suppliers.Commands
{
    public record DeleteSupplierCommand(int Id) : IRequest;

    public class DeleteSupplierCommandHandler : IRequestHandler<DeleteSupplierCommand>
    {
        private readonly ISupplierRepository _repository;
        private readonly IAuthorizationService _authorization;

        public DeleteSupplierCommandHandler(ISupplierRepository repository, IAuthorizationService authorization)
        {
            _repository = repository;
            _authorization = authorization;
        }

        public async Task Handle(DeleteSupplierCommand request, CancellationToken cancellationToken)
        {
            _authorization.EnsureDeletePermission(PermissionModules.Purchase);

            _ = await _repository.GetByIdAsync(request.Id)
                ?? throw new KeyNotFoundException($"Supplier #{request.Id} not found.");

            await _repository.DeleteAsync(request.Id);
        }
    }
}
