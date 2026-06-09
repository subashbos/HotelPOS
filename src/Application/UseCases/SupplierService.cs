using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace HotelPOS.Application.UseCases
{
    public class SupplierService : ISupplierService
    {
        private readonly ISupplierRepository _supplierRepository;

        public SupplierService(ISupplierRepository supplierRepository)
        {
            _supplierRepository = supplierRepository;
        }

        public async Task<List<Supplier>> GetSuppliersAsync()
        {
            return await _supplierRepository.GetAllAsync() ?? new List<Supplier>();
        }

        public async Task<Supplier?> GetSupplierByIdAsync(int id)
        {
            return await _supplierRepository.GetByIdAsync(id);
        }

        public async Task SaveSupplierAsync(Supplier supplier)
        {
            if (supplier == null)
                throw new ArgumentNullException(nameof(supplier));

            // Validate Name
            if (string.IsNullOrWhiteSpace(supplier.Name))
                throw new ArgumentException("Supplier Name is required.");

            // Validate Phone (optional)
            if (!string.IsNullOrWhiteSpace(supplier.Phone))
            {
                // Standard clean-up & basic validation of phone: 10 to 15 digits
                var cleanPhone = Regex.Replace(supplier.Phone, @"[^\d\+\-\(\)\s]", "");
                var digitCount = Regex.Replace(cleanPhone, @"[^\d]", "").Length;
                if (digitCount < 10 || digitCount > 15)
                    throw new ArgumentException("Phone number must be a valid number between 10 and 15 digits.");

                supplier.Phone = cleanPhone.Trim();
            }
            else
            {
                supplier.Phone = string.Empty;
            }

            // Validate Email (if provided)
            if (!string.IsNullOrWhiteSpace(supplier.Email))
            {
                var emailRegex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
                if (!emailRegex.IsMatch(supplier.Email.Trim()))
                    throw new ArgumentException("Email ID is invalid.");
                supplier.Email = supplier.Email.Trim();
            }

            // Validate GSTIN (if provided)
            if (!string.IsNullOrWhiteSpace(supplier.Gstin))
            {
                var gstinRegex = new Regex(@"^[0-9]{2}[A-Z]{5}[0-9]{4}[A-Z]{1}[1-9A-Z]{1}Z[0-9A-Z]{1}$");
                if (!gstinRegex.IsMatch(supplier.Gstin.Trim().ToUpperInvariant()))
                    throw new ArgumentException("GSTIN format is invalid.");
                supplier.Gstin = supplier.Gstin.Trim().ToUpperInvariant();
            }

            // Check Duplicate Name
            if (await _supplierRepository.ExistsByNameAsync(supplier.Name.Trim(), supplier.Id))
                throw new ArgumentException($"A supplier named '{supplier.Name}' already exists.");

            // Trim fields
            supplier.Name = supplier.Name.Trim();
            supplier.ContactPerson = supplier.ContactPerson?.Trim();
            supplier.Address = supplier.Address?.Trim();
            supplier.Gstin = supplier.Gstin?.Trim();
            supplier.City = supplier.City?.Trim();
            supplier.State = supplier.State?.Trim();
            supplier.Pincode = supplier.Pincode?.Trim();
            supplier.PaymentTerms = supplier.PaymentTerms?.Trim();

            if (supplier.Id == 0)
            {
                await _supplierRepository.AddAsync(supplier);
            }
            else
            {
                await _supplierRepository.UpdateAsync(supplier);
            }
        }

        public async Task DeleteSupplierAsync(int id)
        {
            var existing = await _supplierRepository.GetByIdAsync(id);
            if (existing == null)
                throw new KeyNotFoundException($"Supplier #{id} not found.");

            await _supplierRepository.DeleteAsync(id);
        }

        public async Task<bool> ValidateSupplierNameUniqueAsync(string name, int excludeId = 0)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;
            return !await _supplierRepository.ExistsByNameAsync(name.Trim(), excludeId);
        }
    }
}
