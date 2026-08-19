using MediatR;
using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Common.Constants;
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
        private readonly IAuthorizationService _authorization;

        public DeleteTableCommandHandler(ITableRepository repo, IAuthorizationService authorization)
        {
            _repo = repo;
            _authorization = authorization;
        }

        public async Task Handle(DeleteTableCommand request, CancellationToken cancellationToken)
        {
            _authorization.EnsurePermission(PermissionModules.Tables);

            var table = await _repo.GetByIdAsync(request.Id);
            if (table is null || table.IsDeleted)
                throw new KeyNotFoundException($"Table #{request.Id} not found or already deleted.");

            await _repo.DeleteAsync(request.Id);
        }
    }
}
