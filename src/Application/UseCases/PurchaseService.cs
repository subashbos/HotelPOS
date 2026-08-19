#nullable enable

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
        private readonly IMediator? _mediator;
        private readonly IPurchaseRepository? _purchaseRepository;
        private readonly IItemRepository? _itemRepository;
        private readonly IAuthorizationService? _authorization;

        /// <summary>DI constructor — uses MediatR pipeline (validators + handlers).</summary>
        public PurchaseService(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>Legacy constructor for unit tests that inject repositories directly.</summary>
        public PurchaseService(IPurchaseRepository purchaseRepository, IItemRepository itemRepository, IAuthorizationService authorization)
        {
            _purchaseRepository = purchaseRepository;
            _itemRepository = itemRepository;
            _authorization = authorization;
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

        public async Task<Purchase?> GetPurchaseByIdAsync(int id)
        {
            if (_mediator != null)
                return await _mediator.Send(new GetPurchaseByIdQuery(id));

            return await _purchaseRepository!.GetByIdAsync(id);
        }

        public async Task SavePurchaseAsync(Purchase purchase)
        {
            if (purchase == null) throw new ArgumentNullException(nameof(purchase));

            if (_mediator != null)
            {
                // The MediatR ValidationBehavior pipeline is expected to reject an invalid
                // command before it ever reaches SavePurchaseCommandHandler, but that hasn't
                // proven reliable for this void (non-generic) IRequest - validate explicitly so
                // a bad SupplierId/GrandTotal/etc. surfaces as a clean 400 instead of an
                // unhandled DbUpdateException (500) from a downstream FK violation.
                ValidatePurchase(purchase);
                await _mediator.Send(new SavePurchaseCommand(purchase));
                await _mediator.Publish(new EntityActionEvent("Purchase", purchase.Id, "Create",
                    $"Supplier: {purchase.SupplierId}, Invoice: {purchase.InvoiceNumber}, Items: {purchase.PurchaseItems.Count}"));
                return;
            }

            // Legacy path: validate and delegate to command handler
            ValidatePurchase(purchase);
            var handler = new SavePurchaseCommandHandler(_purchaseRepository!, _itemRepository!, _authorization!);
            await handler.Handle(new SavePurchaseCommand(purchase), CancellationToken.None);
        }

        public async Task UpdatePurchaseAsync(Purchase purchase)
        {
            if (purchase == null) throw new ArgumentNullException(nameof(purchase));

            if (_mediator != null)
            {
                // See the same comment in SavePurchaseAsync above.
                ValidatePurchase(purchase);
                await _mediator.Send(new UpdatePurchaseCommand(purchase));
                await _mediator.Publish(new EntityActionEvent("Purchase", purchase.Id, "Update",
                    $"Supplier: {purchase.SupplierId}, Invoice: {purchase.InvoiceNumber}, Items: {purchase.PurchaseItems.Count}"));
                return;
            }

            // Legacy path: validate and delegate to command handler
            ValidatePurchase(purchase);
            var handler = new UpdatePurchaseCommandHandler(_purchaseRepository!, _itemRepository!, _authorization!);
            await handler.Handle(new UpdatePurchaseCommand(purchase), CancellationToken.None);
        }

        public async Task DeletePurchaseAsync(int id)
        {
            if (_mediator != null)
            {
                await _mediator.Send(new DeletePurchaseCommand(id));
                await _mediator.Publish(new EntityActionEvent("Purchase", id, "Delete", "Deleted"));
                return;
            }

            // Legacy path: delegate to command handler
            var handler = new DeletePurchaseCommandHandler(_purchaseRepository!, _itemRepository!, _authorization!);
            await handler.Handle(new DeletePurchaseCommand(id), CancellationToken.None);
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
    }
}
