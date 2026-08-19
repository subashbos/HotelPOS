using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Common.Constants;
using HotelPOS.Domain.Entities;
using MediatR;

namespace HotelPOS.Application.UseCases.Purchases.Commands
{
    public record UpdatePurchaseCommand(Purchase Purchase) : IRequest;

    public class UpdatePurchaseCommandHandler : IRequestHandler<UpdatePurchaseCommand>
    {
        private readonly IPurchaseRepository _purchaseRepository;
        private readonly IItemRepository _itemRepository;
        private readonly IAuthorizationService _authorization;

        public UpdatePurchaseCommandHandler(IPurchaseRepository purchaseRepository, IItemRepository itemRepository, IAuthorizationService authorization)
        {
            _purchaseRepository = purchaseRepository;
            _itemRepository = itemRepository;
            _authorization = authorization;
        }

        public async Task Handle(UpdatePurchaseCommand request, CancellationToken cancellationToken)
        {
            _authorization.EnsureEditPermission(PermissionModules.Purchase);

            var purchase = request.Purchase;

            var oldPurchase = await _purchaseRepository.GetByIdAsync(purchase.Id)
                ?? throw new KeyNotFoundException($"Purchase #{purchase.Id} not found.");

            await _purchaseRepository.BeginTransactionAsync();
            try
            {
                // Stock reconciliation: apply only the net delta per item (new qty - old qty),
                // same shape as OrderService.UpdateOrderInternalAsync's return-then-deduct pattern.
                var oldMap = oldPurchase.PurchaseItems.GroupBy(i => i.ItemId).ToDictionary(g => g.Key, g => g.Sum(i => i.Quantity));
                var newMap = purchase.PurchaseItems.GroupBy(i => i.ItemId).ToDictionary(g => g.Key, g => g.Sum(i => i.Quantity));
                var itemIds = oldMap.Keys.Union(newMap.Keys).ToList();

                var catalogItems = await _itemRepository.GetByIdsAsync(itemIds);
                var itemsById = catalogItems.ToDictionary(i => i.Id);

                // Atomic per-item SQL UPDATE rather than read-modify-write on a tracked entity, so
                // a concurrent purchase save/delete for the same item can't be silently overwritten
                // (lost update). Clamped to zero server-side: some of the originally purchased
                // stock may already have been sold, so a shrinking edit can't go negative.
                foreach (var itemId in itemIds)
                {
                    if (!itemsById.TryGetValue(itemId, out var catalogItem) || !catalogItem.TrackInventory) continue;

                    var delta = newMap.GetValueOrDefault(itemId) - oldMap.GetValueOrDefault(itemId);
                    if (delta != 0)
                    {
                        await _itemRepository.AdjustStockAsync(itemId, delta);
                    }
                }

                await _purchaseRepository.UpdateAsync(purchase);
                await _purchaseRepository.CommitTransactionAsync();
            }
            catch (Exception ex) // NOSONAR: intentional - log with operation context at failure site, preserve stack trace for global handler
            {
                Serilog.Log.Error(ex, "Transaction failed while updating purchase");
                try
                {
                    await _purchaseRepository.RollbackTransactionAsync();
                }
                catch (Exception rollbackEx)
                {
                    Serilog.Log.Error(rollbackEx, "Transaction rollback failed while updating purchase");
                    throw new AggregateException("Transaction failed and rollback also failed.", ex, rollbackEx);
                }
                throw;
            }
        }
    }
}
