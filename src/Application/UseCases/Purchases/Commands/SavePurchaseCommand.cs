using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Common.Constants;
using HotelPOS.Domain.Entities;
using MediatR;

namespace HotelPOS.Application.UseCases.Purchases.Commands
{
    public record SavePurchaseCommand(Purchase Purchase) : IRequest;

    public class SavePurchaseCommandHandler : IRequestHandler<SavePurchaseCommand>
    {
        private readonly IPurchaseRepository _purchaseRepository;
        private readonly IItemRepository _itemRepository;
        private readonly IAuthorizationService _authorization;

        public SavePurchaseCommandHandler(IPurchaseRepository purchaseRepository, IItemRepository itemRepository, IAuthorizationService authorization)
        {
            _purchaseRepository = purchaseRepository;
            _itemRepository = itemRepository;
            _authorization = authorization;
        }

        public async Task Handle(SavePurchaseCommand request, CancellationToken cancellationToken)
        {
            _authorization.EnsureEditPermission(PermissionModules.Purchase);

            var purchase = request.Purchase;

            await _purchaseRepository.BeginTransactionAsync();
            try
            {
                await _purchaseRepository.AddAsync(purchase);

                var itemIds = purchase.PurchaseItems.Select(i => i.ItemId).Distinct().ToList();
                var catalogItems = await _itemRepository.GetByIdsAsync(itemIds);
                var itemsById = catalogItems.ToDictionary(i => i.Id);

                // Atomic per-item SQL UPDATE rather than read-modify-write on a tracked entity, so
                // two purchases for the same item submitted concurrently can't have one's stock
                // credit silently overwritten by the other (lost update).
                foreach (var item in purchase.PurchaseItems)
                {
                    if (itemsById.TryGetValue(item.ItemId, out var catalogItem) && catalogItem.TrackInventory)
                    {
                        await _itemRepository.AdjustStockAsync(item.ItemId, item.Quantity);
                    }
                }

                await _purchaseRepository.CommitTransactionAsync();
            }
            catch (Exception ex) // NOSONAR: intentional - log with operation context at failure site, preserve stack trace for global handler
            {
                Serilog.Log.Error(ex, "Transaction failed while saving purchase");
                try
                {
                    await _purchaseRepository.RollbackTransactionAsync();
                }
                catch (Exception rollbackEx)
                {
                    Serilog.Log.Error(rollbackEx, "Transaction rollback failed while saving purchase");
                    throw new AggregateException("Transaction failed and rollback also failed.", ex, rollbackEx);
                }
                throw;
            }
        }
    }
}
