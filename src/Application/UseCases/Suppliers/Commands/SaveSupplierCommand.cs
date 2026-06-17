using HotelPOS.Application.DTOs.Supplier;
using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Entities;
using MediatR;
using System.Text.RegularExpressions;

namespace HotelPOS.Application.UseCases.Suppliers.Commands
{
    public record SaveSupplierCommand(SaveSupplierDto Dto) : IRequest<int>;

    public class SaveSupplierCommandHandler : IRequestHandler<SaveSupplierCommand, int>
    {
        private readonly ISupplierRepository _repository;

        public SaveSupplierCommandHandler(ISupplierRepository repository)
        {
            _repository = repository;
        }

        public async Task<int> Handle(SaveSupplierCommand request, CancellationToken cancellationToken)
        {
            var dto = request.Dto;

            if (await _repository.ExistsByNameAsync(dto.Name.Trim(), dto.Id))
                throw new InvalidOperationException($"A supplier named '{dto.Name}' already exists.");

            // Sanitize phone
            string? cleanPhone = null;
            if (!string.IsNullOrWhiteSpace(dto.Phone))
                cleanPhone = Regex.Replace(dto.Phone, @"[^\d\+\-\(\)\s]", "").Trim();

            if (dto.Id == 0)
            {
                var supplier = new Supplier
                {
                    Name = dto.Name.Trim(),
                    ContactPerson = dto.ContactPerson?.Trim(),
                    Phone = cleanPhone,
                    Email = dto.Email?.Trim(),
                    Address = dto.Address?.Trim(),
                    Gstin = dto.Gstin?.Trim().ToUpperInvariant(),
                    City = dto.City?.Trim(),
                    State = dto.State?.Trim(),
                    Pincode = dto.Pincode?.Trim(),
                    OpeningBalance = dto.OpeningBalance,
                    CreditLimit = dto.CreditLimit,
                    PaymentTerms = dto.PaymentTerms?.Trim()
                };
                await _repository.AddAsync(supplier);
                return supplier.Id;
            }
            else
            {
                var existing = await _repository.GetByIdAsync(dto.Id)
                    ?? throw new KeyNotFoundException($"Supplier #{dto.Id} not found.");

                existing.Name = dto.Name.Trim();
                existing.ContactPerson = dto.ContactPerson?.Trim();
                existing.Phone = cleanPhone;
                existing.Email = dto.Email?.Trim();
                existing.Address = dto.Address?.Trim();
                existing.Gstin = dto.Gstin?.Trim().ToUpperInvariant();
                existing.City = dto.City?.Trim();
                existing.State = dto.State?.Trim();
                existing.Pincode = dto.Pincode?.Trim();
                existing.OpeningBalance = dto.OpeningBalance;
                existing.CreditLimit = dto.CreditLimit;
                existing.PaymentTerms = dto.PaymentTerms?.Trim();

                await _repository.UpdateAsync(existing);
                return existing.Id;
            }
        }
    }
}
