using AutoMapper;
using HotelPOS.Application.DTOs.Expense;
using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Common.Constants;
using HotelPOS.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelPOS.Api.Controllers
{
    /// <summary>Business expenses — requires a valid JWT token on all endpoints.</summary>
    [Authorize]
    public class ExpensesController : BaseApiController
    {
        private readonly IExpenseService _expenseService;
        private readonly IUserContext _userContext;
        private readonly IMapper _mapper;

        public ExpensesController(IExpenseService expenseService, IUserContext userContext, IMapper mapper)
        {
            _expenseService = expenseService;
            _userContext = userContext;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ExpenseDto>>> GetExpenses([FromQuery] DateTime? from, [FromQuery] DateTime? to)
        {
            var expenses = await _expenseService.GetExpensesAsync(from, to);
            return Ok(_mapper.Map<IEnumerable<ExpenseDto>>(expenses));
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<ExpenseDto>> GetExpense(int id)
        {
            if (id <= 0) return BadRequest("Invalid expense ID.");
            var expense = await _expenseService.GetExpenseByIdAsync(id);
            if (expense == null) return NotFound();
            return Ok(_mapper.Map<ExpenseDto>(expense));
        }

        [HttpPost]
        [Authorize(Roles = $"{RoleNames.Admin},{RoleNames.Manager}")]
        public async Task<ActionResult<ExpenseDto>> CreateExpense([FromBody] SaveExpenseDto request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var expense = _mapper.Map<Expense>(request);
            expense.Id = 0;
            expense.CreatedBy = _userContext.CurrentUserId;
            try
            {
                await _expenseService.SaveExpenseAsync(expense);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }

            return CreatedAtAction(nameof(GetExpense), new { id = expense.Id }, _mapper.Map<ExpenseDto>(expense));
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = $"{RoleNames.Admin},{RoleNames.Manager}")]
        public async Task<IActionResult> UpdateExpense(int id, [FromBody] SaveExpenseDto request)
        {
            if (id <= 0) return BadRequest("Invalid expense ID.");
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var existing = await _expenseService.GetExpenseByIdAsync(id);
            if (existing == null) return NotFound();

            var expense = _mapper.Map<Expense>(request);
            expense.Id = id;
            expense.CreatedBy = existing.CreatedBy;
            try
            {
                await _expenseService.SaveExpenseAsync(expense);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }

            return NoContent();
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = RoleNames.Admin)]
        public async Task<IActionResult> DeleteExpense(int id)
        {
            if (id <= 0) return BadRequest("Invalid expense ID.");
            try
            {
                await _expenseService.DeleteExpenseAsync(id);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }

            return NoContent();
        }
    }
}
