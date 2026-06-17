using MediatR;
using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Entities;
using System.Threading;
using System.Threading.Tasks;
using System;

namespace HotelPOS.Application.UseCases.Tables.Commands
{
    public record DeleteTableCommand(int Id) : IRequest;

    public class DeleteTableCommandHandler : IRequestHandler<DeleteTableCommand>
    {
        private readonly ITableRepository _repo;

        public DeleteTableCommandHandler(ITableRepository repo)
        {
            _repo = repo;
        }

        public async Task Handle(DeleteTableCommand request, CancellationToken cancellationToken)
        {
            var table = await _repo.GetByIdAsync(request.Id);
            if (table is null || table.IsDeleted)
                throw new KeyNotFoundException($"Table #{request.Id} not found or already deleted.");

            await _repo.DeleteAsync(request.Id);
        }
    }
}
