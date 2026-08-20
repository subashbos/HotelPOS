using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Common.Constants;
using MediatR;

namespace HotelPOS.Application.UseCases.Purchases.Commands
{
    public record DeletePurchaseCommand(int Id) : IRequest;

    public class DeletePurchaseCommandHandler : IRequestHandler<DeletePurchaseCommand>
    {
        private readonly IPurchaseRepository _purchaseRepository;
        private readonly IItemRepository _itemRepository;
        private readonly IAuthorizationService _authorization;

        public DeletePurchaseCommandHandler(IPurchaseRepository purchaseRepository, IItemRepository itemRepository, IAuthorizationService authorization)
        {
            _purchaseRepository = purchaseRepository;
            _itemRepository = itemRepository;
            _authorization = authorization;
        }

        public async Task Handle(DeletePurchaseCommand request, CancellationToken cancellationToken)
        {
            _authorization.EnsureDeletePermission(PermissionModules.Purchase);

            var purchase = await _purchaseRepository.GetByIdAsync(request.Id);
            if (purchase == null) return; // idempotent delete, same convention as OrderService.DeleteOrderInternalAsync

            await _purchaseRepository.BeginTransactionAsync();
            try
            {
                // Reverse the stock this purchase added, so deleting an erroneous entry doesn't
                // leave stock permanently inflated.
                var itemIds = purchase.PurchaseItems.Select(i => i.ItemId).Distinct().ToList();
                var catalogItems = await _itemRepository.GetByIdsAsync(itemIds);
                var itemsById = catalogItems.ToDictionary(i => i.Id);

                // Atomic per-item SQL UPDATE rather than read-modify-write on a tracked entity, so
                // a concurrent purchase save/update for the same item can't be silently overwritten
                // (lost update). Clamped to zero server-side: some of the purchased stock may
                // already have been sold, so reversing the purchase can't go negative.
                foreach (var item in purchase.PurchaseItems)
                {
                    if (itemsById.TryGetValue(item.ItemId, out var catalogItem) && catalogItem.TrackInventory)
                    {
                        await _itemRepository.AdjustStockAsync(item.ItemId, -item.Quantity);
                    }
                }

                await _purchaseRepository.DeleteAsync(request.Id);
                await _purchaseRepository.CommitTransactionAsync();
            }
            catch (Exception ex) // NOSONAR: intentional - log with operation context at failure site, preserve stack trace for global handler
            {
                Serilog.Log.Error(ex, "Transaction failed while deleting purchase");
                try
                {
                    await _purchaseRepository.RollbackTransactionAsync();
                }
                catch (Exception rollbackEx)
                {
                    Serilog.Log.Error(rollbackEx, "Transaction rollback failed while deleting purchase");
                    throw new AggregateException("Transaction failed and rollback also failed.", ex, rollbackEx);
                }
                throw;
            }
        }
    }
}
