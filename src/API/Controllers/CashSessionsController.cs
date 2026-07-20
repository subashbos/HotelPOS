using AutoMapper;
using HotelPOS.Application.DTOs.CashSession;
using HotelPOS.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelPOS.Api.Controllers
{
    /// <summary>Cash drawer / shift sessions — requires a valid JWT token on all endpoints.</summary>
    [Authorize]
    public class CashSessionsController : BaseApiController
    {
        private readonly ICashService _cashService;
        private readonly IUserContext _userContext;
        private readonly IMapper _mapper;

        public CashSessionsController(ICashService cashService, IUserContext userContext, IMapper mapper)
        {
            _cashService = cashService;
            _userContext = userContext;
            _mapper = mapper;
        }

        [HttpGet("current")]
        public async Task<ActionResult<CashSessionDto>> GetCurrentSession()
        {
            var session = await _cashService.GetCurrentSessionAsync();
            if (session == null) return NotFound();
            return Ok(_mapper.Map<CashSessionDto>(session));
        }

        [HttpGet("history")]
        public async Task<ActionResult<IEnumerable<CashSessionDto>>> GetHistory([FromQuery] int count = 30)
        {
            var sessions = await _cashService.GetSessionHistoryAsync(count);
            return Ok(_mapper.Map<IEnumerable<CashSessionDto>>(sessions));
        }

        [HttpGet("current/sales-total")]
        public async Task<ActionResult<decimal>> GetCurrentSessionSalesTotal()
        {
            var total = await _cashService.GetTotalSalesForCurrentSessionAsync();
            return Ok(total);
        }

        [HttpPost("open")]
        public async Task<ActionResult<int>> OpenSession([FromBody] OpenSessionRequest request)
        {
            var username = _userContext.CurrentUsername ?? "API User";

            try
            {
                var id = await _cashService.OpenSessionAsync(request.OpeningBalance ?? 0, username);
                return Ok(id);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ex.Message);
            }
        }

        [HttpPost("close")]
        public async Task<IActionResult> CloseSession([FromBody] CloseSessionRequest request)
        {
            var username = _userContext.CurrentUsername ?? "API User";

            try
            {
                await _cashService.CloseSessionAsync(request.ActualCash ?? 0, request.Notes, username);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ex.Message);
            }

            return NoContent();
        }
    }

    /// <summary>OpenedBy comes from the authenticated user, never the request body.</summary>
    public sealed class OpenSessionRequest
    {
        public decimal? OpeningBalance { get; set; }
    }

    /// <summary>ClosedBy comes from the authenticated user, never the request body.</summary>
    public sealed class CloseSessionRequest
    {
        public decimal? ActualCash { get; set; }
        public string? Notes { get; set; }
    }
}
