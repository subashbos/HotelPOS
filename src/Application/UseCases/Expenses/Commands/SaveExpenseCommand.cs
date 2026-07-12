using HotelPOS.Application.DTOs.Expense;
using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Entities;
using MediatR;

namespace HotelPOS.Application.UseCases.Expenses.Commands
{
    public record SaveExpenseCommand(SaveExpenseDto Dto) : IRequest<int>;

    public class SaveExpenseCommandHandler : IRequestHandler<SaveExpenseCommand, int>
    {
        private readonly IExpenseRepository _repository;

        public SaveExpenseCommandHandler(IExpenseRepository repository)
        {
            _repository = repository;
        }

        public async Task<int> Handle(SaveExpenseCommand request, CancellationToken cancellationToken)
        {
            var dto = request.Dto;

            if (dto.Id == 0)
            {
                var expense = new Expense
                {
                    Date = dto.Date,
                    Title = dto.Title.Trim(),
                    Description = dto.Description?.Trim(),
                    Amount = dto.Amount,
                    Category = dto.Category.Trim(),
                    PaymentMode = dto.PaymentMode?.Trim(),
                    CreatedBy = dto.CreatedBy
                };
                await _repository.AddAsync(expense);
                return expense.Id;
            }
            else
            {
                var existing = await _repository.GetByIdAsync(dto.Id)
                    ?? throw new KeyNotFoundException($"Expense #{dto.Id} not found.");

                existing.Date = dto.Date;
                existing.Title = dto.Title.Trim();
                existing.Description = dto.Description?.Trim();
                existing.Amount = dto.Amount;
                existing.Category = dto.Category.Trim();
                existing.PaymentMode = dto.PaymentMode?.Trim();

                await _repository.UpdateAsync(existing);
                return existing.Id;
            }
        }
    }
}
