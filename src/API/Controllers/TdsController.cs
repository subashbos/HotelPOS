using HotelPOS.Application.Common.Models;
using HotelPOS.Application.DTOs.Tds;
using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Common.Constants;
using HotelPOS.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelPOS.Api.Controllers
{
    /// <summary>Admin-editable income-tax TDS slab structures, one per financial year.</summary>
    [Authorize(Roles = RoleNames.Admin)]
    public class TdsController : BaseApiController
    {
        private readonly ITdsService _tdsService;

        public TdsController(ITdsService tdsService)
        {
            _tdsService = tdsService;
        }

        [HttpGet("financial-years")]
        public async Task<ActionResult<IEnumerable<int>>> GetFinancialYears()
        {
            return Ok(await _tdsService.GetFinancialYearsAsync());
        }

        [HttpGet("rule-sets/{financialYearStart:int}")]
        public async Task<ActionResult<TdsRuleSetDto>> GetRuleSet(int financialYearStart)
        {
            var ruleSet = await _tdsService.GetRuleSetAsync(financialYearStart);
            if (ruleSet == null) return NotFound();
            return Ok(ToDto(ruleSet));
        }

        [HttpPut("rule-sets/{financialYearStart:int}")]
        public async Task<IActionResult> SaveRuleSet(int financialYearStart, [FromBody] SaveTdsRuleSetDto request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var ruleSet = new TdsRuleSet
            {
                Config = new TdsConfig
                {
                    FinancialYearStart = financialYearStart,
                    StandardDeduction = request.StandardDeduction,
                    RebateIncomeLimit = request.RebateIncomeLimit,
                    CessRatePercent = request.CessRatePercent
                },
                Slabs = request.Slabs.Select(s => new TdsSlab
                {
                    FinancialYearStart = financialYearStart,
                    IncomeFrom = s.IncomeFrom,
                    IncomeTo = s.IncomeTo,
                    RatePercent = s.RatePercent,
                    DisplayOrder = s.DisplayOrder
                }).ToList()
            };

            try
            {
                await _tdsService.SaveRuleSetAsync(ruleSet);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }

            return NoContent();
        }

        [HttpDelete("rule-sets/{financialYearStart:int}")]
        public async Task<IActionResult> DeleteRuleSet(int financialYearStart)
        {
            await _tdsService.DeleteRuleSetAsync(financialYearStart);
            return NoContent();
        }

        private static TdsRuleSetDto ToDto(TdsRuleSet ruleSet) => new()
        {
            FinancialYearStart = ruleSet.Config.FinancialYearStart,
            StandardDeduction = ruleSet.Config.StandardDeduction,
            RebateIncomeLimit = ruleSet.Config.RebateIncomeLimit,
            CessRatePercent = ruleSet.Config.CessRatePercent,
            Slabs = ruleSet.Slabs.Select(s => new TdsSlabDto
            {
                IncomeFrom = s.IncomeFrom,
                IncomeTo = s.IncomeTo,
                RatePercent = s.RatePercent,
                DisplayOrder = s.DisplayOrder
            }).ToList()
        };
    }
}
