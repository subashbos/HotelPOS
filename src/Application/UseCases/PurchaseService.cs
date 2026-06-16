using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HotelPOS.Application.UseCases
{
    public class PurchaseService : IPurchaseService
    {
        private readonly IPurchaseRepository _purchaseRepository;
        private readonly IItemRepository _itemRepository;

        public PurchaseService(IPurchaseRepository purchaseRepository, IItemRepository itemRepository)
        {
            _purchaseRepository = purchaseRepository;
            _itemRepository = itemRepository;
        }

        public async Task<List<Supplier>> GetSuppliersAsync()
        {
            return await _purchaseRepository.GetSuppliersAsync();
        }

        public async Task<List<Purchase>> GetPurchasesAsync()
        {
            return await _purchaseRepository.GetPurchasesAsync();
        }

        public async Task SavePurchaseAsync(Purchase purchase)
        {
            if (purchase == null)
                throw new ArgumentNullException(nameof(purchase));

            if (purchase.SupplierId <= 0)
                throw new ArgumentException("Supplier is required.", nameof(purchase));

            if (string.IsNullOrWhiteSpace(purchase.InvoiceNumber))
                throw new ArgumentException("Invoice number is required.", nameof(purchase));

            if (purchase.PurchaseItems == null || purchase.PurchaseItems.Count == 0)
                throw new ArgumentException("Purchase must contain at least one item.", nameof(purchase));

            // Validate all items
            foreach (var item in purchase.PurchaseItems)
            {
                if (item.ItemId <= 0)
                    throw new ArgumentException("Invalid item selected.", nameof(purchase));
                if (item.Quantity <= 0)
                    throw new ArgumentException($"Quantity for item '{item.ItemName}' must be greater than zero.", nameof(purchase));
                if (item.UnitPrice < 0)
                    throw new ArgumentException($"Price for item '{item.ItemName}' cannot be negative.", nameof(purchase));
            }

            await _purchaseRepository.BeginTransactionAsync();
            try
            {
                // Save purchase header and items
                await _purchaseRepository.AddAsync(purchase);

                // Increment Stock for each item
                foreach (var item in purchase.PurchaseItems)
                {
                    var catalogItem = await _itemRepository.GetByIdAsync(item.ItemId);
                    if (catalogItem != null)
                    {
                        if (catalogItem.TrackInventory)
                        {
                            catalogItem.StockQuantity += item.Quantity;
                            await _itemRepository.UpdateAsync(catalogItem);
                        }
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
