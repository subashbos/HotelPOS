using MediatR;
using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Entities;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace HotelPOS.Application.UseCases.UnitOfMeasurements.Commands
{
    public record DeleteUnitOfMeasurementCommand(int Id) : IRequest;

    public class DeleteUnitOfMeasurementCommandHandler : IRequestHandler<DeleteUnitOfMeasurementCommand>
    {
        private readonly IUnitOfMeasurementRepository _repo;
        private readonly IItemRepository _itemRepo;

        public DeleteUnitOfMeasurementCommandHandler(IUnitOfMeasurementRepository repo, IItemRepository itemRepo)
        {
            _repo = repo;
            _itemRepo = itemRepo;
        }

        public async Task Handle(DeleteUnitOfMeasurementCommand request, CancellationToken cancellationToken)
        {
            var items = await _itemRepo.GetAllAsync() ?? new List<Item>();
            if (items.Any(i => i.UnitId == request.Id))
                throw new InvalidOperationException("Cannot delete unit because it is used by existing menu items. Please reassign the items first.");

            await _repo.DeleteAsync(request.Id);
        }
    }
}
