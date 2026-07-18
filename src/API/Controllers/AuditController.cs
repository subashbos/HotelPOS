using HotelPOS.Application.DTOs.Audit;
using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Common.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelPOS.Api.Controllers
{
    /// <summary>Security audit log — requires a valid JWT token on all endpoints.</summary>
    [Authorize(Roles = RoleNames.Admin)]
    public class AuditController : BaseApiController
    {
        private readonly IAuditService _auditService;

        public AuditController(IAuditService auditService)
        {
            _auditService = auditService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<AuditLogDto>>> GetLogs([FromQuery] DateTime? from, [FromQuery] DateTime? to)
        {
            var logs = await _auditService.GetLogsAsync(from, to);
            return Ok(logs);
        }
    }
}
