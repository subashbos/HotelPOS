using FluentValidation;
using HotelPOS.Application.Common.Validators;
using HotelPOS.Application.DTOs.Customer;
using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Common.Constants;
using HotelPOS.Domain.Entities;

namespace HotelPOS.Application.UseCases
{
    public class CustomerService : ICustomerService
    {
        private readonly ICustomerRepository _repository;
        private readonly IOrderRepository _orderRepository;
        private readonly IValidator<Customer> _validator;

        public CustomerService(ICustomerRepository repository, IOrderRepository orderRepository, IValidator<Customer>? validator = null)
        {
            _repository = repository;
            _orderRepository = orderRepository;
            _validator = validator ?? new CustomerValidator();
        }

        public async Task<List<Customer>> GetCustomersAsync(bool includeInactive = false)
        {
            return await _repository.GetAllAsync(includeInactive) ?? new List<Customer>();
        }

        public async Task<Customer?> GetCustomerByIdAsync(int id)
        {
            return await _repository.GetByIdAsync(id);
        }

        public async Task<Customer?> GetCustomerByPhoneAsync(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone)) return null;
            return await _repository.GetByPhoneAsync(phone.Trim());
        }

        public async Task SaveCustomerAsync(Customer customer)
        {
            if (customer == null) throw new ArgumentNullException(nameof(customer));

            customer.Name = customer.Name?.Trim() ?? string.Empty;
            customer.Phone = string.IsNullOrWhiteSpace(customer.Phone) ? null : customer.Phone.Trim();
            customer.Email = string.IsNullOrWhiteSpace(customer.Email) ? null : customer.Email.Trim();
            customer.Gstin = string.IsNullOrWhiteSpace(customer.Gstin) ? null : customer.Gstin.Trim().ToUpperInvariant();
            customer.Address = string.IsNullOrWhiteSpace(customer.Address) ? null : customer.Address.Trim();
            customer.Notes = string.IsNullOrWhiteSpace(customer.Notes) ? null : customer.Notes.Trim();

            var result = _validator.Validate(customer);
            if (!result.IsValid)
                throw new ArgumentException(result.Errors[0].ErrorMessage);

            if (!string.IsNullOrWhiteSpace(customer.Phone) && await _repository.ExistsByPhoneAsync(customer.Phone, customer.Id))
                throw new ArgumentException($"A customer with phone '{customer.Phone}' already exists.");

            if (customer.Id == 0)
            {
                customer.CreatedAt = DateTime.UtcNow;
                customer.IsActive = true;
                await _repository.AddAsync(customer);
            }
            else
            {
                var existing = await _repository.GetByIdAsync(customer.Id)
                    ?? throw new KeyNotFoundException($"Customer #{customer.Id} not found.");
                customer.CreatedAt = existing.CreatedAt;
                customer.IsActive = existing.IsActive;
                customer.UpdatedAt = DateTime.UtcNow;
                await _repository.UpdateAsync(customer);
            }
        }

        public async Task DeleteCustomerAsync(int id)
        {
            _ = await _repository.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"Customer #{id} not found.");
            await _repository.DeactivateAsync(id);
        }

        public async Task<CustomerHistoryDto> GetCustomerHistoryAsync(int id)
        {
            var customer = await _repository.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"Customer #{id} not found.");

            var (orders, _) = await _orderRepository.GetPagedWithItemsAsync(1, -1, new OrderQueryFilter(CustomerId: id));
            var paidOrders = orders.Where(o => o.Status != OrderStatuses.Void).ToList();

            return new CustomerHistoryDto
            {
                CustomerId = customer.Id,
                CustomerName = customer.Name,
                TotalOrders = orders.Count,
                TotalSpent = paidOrders.Sum(o => o.TotalAmount),
                FirstOrderDate = orders.Count > 0 ? orders.Min(o => o.CreatedAt) : null,
                LastOrderDate = orders.Count > 0 ? orders.Max(o => o.CreatedAt) : null,
                Orders = orders
                    .OrderByDescending(o => o.CreatedAt)
                    .Select(o => new CustomerOrderSummaryDto
                    {
                        OrderId = o.Id,
                        InvoiceNumber = o.InvoiceNumber,
                        CreatedAt = o.CreatedAt,
                        TotalAmount = o.TotalAmount,
                        Status = o.Status,
                        OrderType = o.OrderType
                    })
                    .ToList()
            };
        }
    }
}
