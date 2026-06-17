using HotelPOS.Application.Interfaces;
using MediatR;

namespace HotelPOS.Application.UseCases.Suppliers.Commands
{
    public record DeleteSupplierCommand(int Id) : IRequest;

    public class DeleteSupplierCommandHandler : IRequestHandler<DeleteSupplierCommand>
    {
        private readonly ISupplierRepository _repository;

        public DeleteSupplierCommandHandler(ISupplierRepository repository)
        {
            _repository = repository;
        }

        public async Task Handle(DeleteSupplierCommand request, CancellationToken cancellationToken)
        {
            var existing = await _repository.GetByIdAsync(request.Id)
                ?? throw new KeyNotFoundException($"Supplier #{request.Id} not found.");

            await _repository.DeleteAsync(request.Id);
        }
    }
}
