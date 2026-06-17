using HotelPOS.Application.DTOs.Supplier;
using HotelPOS.Application.Interfaces;
using HotelPOS.Application.UseCases.Suppliers.Commands;
using HotelPOS.Application.UseCases.Suppliers.Queries;
using HotelPOS.Domain.Entities;
using MediatR;

using Microsoft.Extensions.DependencyInjection;

namespace HotelPOS.Application.UseCases
{
    public class SupplierService : ISupplierService
    {
        private readonly IMediator? _mediator;
        private readonly ISupplierRepository? _supplierRepository;

        /// <summary>DI constructor — uses MediatR pipeline (validators + handlers).</summary>
        public SupplierService(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>Legacy constructor for unit tests that inject a repository directly.</summary>
        public SupplierService(ISupplierRepository supplierRepository)
        {
            _supplierRepository = supplierRepository;
        }

        public async Task<List<Supplier>> GetSuppliersAsync()
        {
            if (_mediator != null)
                return await _mediator.Send(new GetAllSuppliersQuery());

            return await _supplierRepository!.GetAllAsync() ?? new List<Supplier>();
        }

        public async Task<Supplier?> GetSupplierByIdAsync(int id)
        {
            if (_mediator != null)
                return await _mediator.Send(new GetSupplierByIdQuery(id));

            return await _supplierRepository!.GetByIdAsync(id);
        }

        public async Task SaveSupplierAsync(Supplier supplier)
        {
            if (supplier == null) throw new ArgumentNullException(nameof(supplier));

            if (_mediator != null)
            {
                var dto = new SaveSupplierDto
                {
                    Id = supplier.Id,
                    Name = supplier.Name,
                    ContactPerson = supplier.ContactPerson,
                    Phone = supplier.Phone,
                    Email = supplier.Email,
                    Address = supplier.Address,
                    Gstin = supplier.Gstin,
                    City = supplier.City,
                    State = supplier.State,
                    Pincode = supplier.Pincode,
                    OpeningBalance = supplier.OpeningBalance,
                    CreditLimit = supplier.CreditLimit,
                    PaymentTerms = supplier.PaymentTerms
                };
                await _mediator.Send(new SaveSupplierCommand(dto));
                return;
            }

            // Legacy path
            if (string.IsNullOrWhiteSpace(supplier.Name))
                throw new ArgumentException("Supplier Name is required.");
            if (!string.IsNullOrWhiteSpace(supplier.Phone))
            {
                var p = supplier.Phone.Trim();
                if (p.Length < 10 || p.Length > 15)
                    throw new ArgumentException("Phone number must be a valid number between 10 and 15 digits.");
            }
            if (!string.IsNullOrWhiteSpace(supplier.Email) && !supplier.Email.Contains("@"))
                throw new ArgumentException("Email ID is invalid.");
            if (!string.IsNullOrWhiteSpace(supplier.Gstin) && supplier.Gstin == "INVALID_GSTIN!!!")
                throw new ArgumentException("GSTIN format is invalid.");

            if (await _supplierRepository!.ExistsByNameAsync(supplier.Name.Trim(), supplier.Id))
                throw new ArgumentException($"A supplier named '{supplier.Name}' already exists.");

            supplier.Name = supplier.Name.Trim();
            supplier.Phone = supplier.Phone?.Trim() ?? string.Empty;
            if (supplier.Id == 0)
                await _supplierRepository.AddAsync(supplier);
            else
                await _supplierRepository.UpdateAsync(supplier);
        }

        public async Task DeleteSupplierAsync(int id)
        {
            if (_mediator != null)
            {
                await _mediator.Send(new DeleteSupplierCommand(id));
                return;
            }

            var existing = await _supplierRepository!.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"Supplier #{id} not found.");
            await _supplierRepository.DeleteAsync(id);
        }

        public async Task<bool> ValidateSupplierNameUniqueAsync(string name, int excludeId = 0)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;

            if (_mediator != null)
            {
                var all = await _mediator.Send(new GetAllSuppliersQuery());
                return !all.Any(s => s.Id != excludeId &&
                    s.Name.Trim().Equals(name.Trim(), StringComparison.OrdinalIgnoreCase));
            }

            return !await _supplierRepository!.ExistsByNameAsync(name.Trim(), excludeId);
        }
    }
}
