using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Entities;
using MediatR;

namespace HotelPOS.Application.UseCases.Purchases.Commands
{
    public record SavePurchaseCommand(Purchase Purchase) : IRequest;

    public class SavePurchaseCommandHandler : IRequestHandler<SavePurchaseCommand>
    {
        private readonly IPurchaseRepository _purchaseRepository;
        private readonly IItemRepository _itemRepository;

        public SavePurchaseCommandHandler(IPurchaseRepository purchaseRepository, IItemRepository itemRepository)
        {
            _purchaseRepository = purchaseRepository;
            _itemRepository = itemRepository;
        }

        public async Task Handle(SavePurchaseCommand request, CancellationToken cancellationToken)
        {
            var purchase = request.Purchase;

            await _purchaseRepository.BeginTransactionAsync();
            try
            {
                await _purchaseRepository.AddAsync(purchase);

                var itemIds = purchase.PurchaseItems.Select(i => i.ItemId).Distinct().ToList();
                var catalogItems = await _itemRepository.GetByIdsAsync(itemIds);
                var itemsById = catalogItems.ToDictionary(i => i.Id);

                foreach (var item in purchase.PurchaseItems)
                {
                    if (itemsById.TryGetValue(item.ItemId, out var catalogItem) && catalogItem.TrackInventory)
                    {
                        catalogItem.StockQuantity += item.Quantity;
                    }
                }

                var toUpdate = catalogItems.Where(i => i.TrackInventory).ToList();
                if (toUpdate.Count > 0)
                {
                    await _itemRepository.UpdateRangeAsync(toUpdate);
                }

                await _purchaseRepository.CommitTransactionAsync();
            }
            catch
            {
                await _purchaseRepository.RollbackTransactionAsync();
                throw;
            }
        }
    }
}
