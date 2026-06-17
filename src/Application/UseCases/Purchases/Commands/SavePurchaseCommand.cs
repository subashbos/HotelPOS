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

                foreach (var item in purchase.PurchaseItems)
                {
                    var catalogItem = await _itemRepository.GetByIdAsync(item.ItemId);
                    if (catalogItem != null && catalogItem.TrackInventory)
                    {
                        catalogItem.StockQuantity += item.Quantity;
                        await _itemRepository.UpdateAsync(catalogItem);
                    }
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
