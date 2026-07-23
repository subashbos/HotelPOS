using HotelPOS.Application.Interfaces;
using HotelPOS.Application.UseCases.Purchases.Commands;
using HotelPOS.Application.UseCases.Purchases.Queries;
using HotelPOS.Application.UseCases.Suppliers.Queries;
using HotelPOS.Domain.Entities;
using HotelPOS.Domain.Events;
using MediatR;

using Microsoft.Extensions.DependencyInjection;

namespace HotelPOS.Application.UseCases
{
    public class PurchaseService : IPurchaseService
    {
        private const string PurchaseEntityType = "Purchase";

        private readonly IMediator? _mediator;
        private readonly IPurchaseRepository? _purchaseRepository;
        private readonly IItemRepository? _itemRepository;

        /// <summary>DI constructor — uses MediatR pipeline (validators + handlers).</summary>
        public PurchaseService(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>Legacy constructor for unit tests that inject repositories directly.</summary>
        public PurchaseService(IPurchaseRepository purchaseRepository, IItemRepository itemRepository)
        {
            _purchaseRepository = purchaseRepository;
            _itemRepository = itemRepository;
        }

        public async Task<List<Supplier>> GetSuppliersAsync()
        {
            if (_mediator != null)
                return await _mediator.Send(new GetAllSuppliersQuery());

            return await _purchaseRepository!.GetSuppliersAsync();
        }

        public async Task<List<Purchase>> GetPurchasesAsync()
        {
            if (_mediator != null)
                return await _mediator.Send(new GetAllPurchasesQuery());

            return await _purchaseRepository!.GetPurchasesAsync();
        }

        public async Task SavePurchaseAsync(Purchase purchase)
        {
            if (purchase == null) throw new ArgumentNullException(nameof(purchase));

            if (_mediator != null)
            {
                await _mediator.Send(new SavePurchaseCommand(purchase));
                await _mediator.Publish(new EntityActionEvent(
                    PurchaseEntityType, purchase.Id, "Create",
                    $"Supplier: {purchase.SupplierId}, Invoice: {purchase.InvoiceNumber}, Total: {purchase.GrandTotal:N2}"));
                return;
            }

            // Legacy path
            ValidatePurchase(purchase);

            await _purchaseRepository!.BeginTransactionAsync();
            try
            {
                await _purchaseRepository.AddAsync(purchase);
                await ApplyPurchaseToStockAsync(purchase);
                await _purchaseRepository.CommitTransactionAsync();
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Transaction failed while creating purchase");
                try
                {
                    await _purchaseRepository.RollbackTransactionAsync();
                }
                catch (Exception rollbackEx)
                {
                    Serilog.Log.Error(rollbackEx, "Transaction rollback failed while creating purchase");
                    throw new AggregateException("Transaction failed and rollback also failed.", ex, rollbackEx);
                }
                throw;
            }
        }

        private static void ValidatePurchase(Purchase purchase)
        {
            if (purchase.SupplierId <= 0)
                throw new ArgumentException("A valid supplier must be selected.");
            if (string.IsNullOrWhiteSpace(purchase.InvoiceNumber))
                throw new ArgumentException("Invoice number is required.");
            if (purchase.PurchaseItems == null || !purchase.PurchaseItems.Any())
                throw new ArgumentException("Purchase must contain at least one item.");
            foreach (var item in purchase.PurchaseItems)
            {
                if (item.Quantity <= 0)
                    throw new ArgumentException("Each item quantity must be greater than zero.");
                if (item.UnitPrice < 0)
                    throw new ArgumentException("Each item unit price cannot be negative.");
            }
        }

        private async Task ApplyPurchaseToStockAsync(Purchase purchase)
        {
            var itemIds = purchase.PurchaseItems.Select(i => i.ItemId).Distinct().ToList();
            var catalogItems = await _itemRepository!.GetByIdsAsync(itemIds);
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
        }
    }
}
