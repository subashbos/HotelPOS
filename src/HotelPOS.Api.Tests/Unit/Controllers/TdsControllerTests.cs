using HotelPOS.Api.Controllers;
using HotelPOS.Application.Common.Models;
using HotelPOS.Application.DTOs.Tds;
using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace HotelPOS.Tests.Unit.Controllers
{
    public class TdsControllerTests
    {
        private readonly Mock<ITdsService> _tdsService = new();
        private readonly TdsController _controller;

        public TdsControllerTests()
        {
            _controller = new TdsController(_tdsService.Object);
        }

        private static TdsRuleSet SampleRuleSet() => new()
        {
            Config = new TdsConfig { FinancialYearStart = 2025, StandardDeduction = 75000, RebateIncomeLimit = 700000, CessRatePercent = 4 },
            Slabs = new List<TdsSlab>
            {
                new() { IncomeFrom = 0, IncomeTo = 300000, RatePercent = 0, DisplayOrder = 1 },
                new() { IncomeFrom = 300000, IncomeTo = null, RatePercent = 5, DisplayOrder = 2 }
            }
        };

        [Fact]
        public async Task GetFinancialYears_ReturnsOkWithYears()
        {
            _tdsService.Setup(s => s.GetFinancialYearsAsync()).ReturnsAsync(new List<int> { 2024, 2025 });

            var result = await _controller.GetFinancialYears();

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(new List<int> { 2024, 2025 }, ok.Value);
        }

        [Fact]
        public async Task GetRuleSet_NotFound_ReturnsNotFound()
        {
            _tdsService.Setup(s => s.GetRuleSetAsync(2025)).ReturnsAsync((TdsRuleSet?)null);

            var result = await _controller.GetRuleSet(2025);

            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task GetRuleSet_Found_ReturnsMappedDto()
        {
            _tdsService.Setup(s => s.GetRuleSetAsync(2025)).ReturnsAsync(SampleRuleSet());

            var result = await _controller.GetRuleSet(2025);

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var dto = Assert.IsType<TdsRuleSetDto>(ok.Value);
            Assert.Equal(2025, dto.FinancialYearStart);
            Assert.Equal(75000, dto.StandardDeduction);
            Assert.Equal(2, dto.Slabs.Count);
        }

        [Fact]
        public async Task SaveRuleSet_InvalidModelState_ReturnsBadRequest()
        {
            _controller.ModelState.AddModelError("Slabs", "required");

            var result = await _controller.SaveRuleSet(2025, new SaveTdsRuleSetDto());

            Assert.IsType<BadRequestObjectResult>(result);
            _tdsService.Verify(s => s.SaveRuleSetAsync(It.IsAny<TdsRuleSet>()), Times.Never);
        }

        [Fact]
        public async Task SaveRuleSet_ServiceThrowsArgumentException_ReturnsBadRequest()
        {
            _tdsService.Setup(s => s.SaveRuleSetAsync(It.IsAny<TdsRuleSet>()))
                .ThrowsAsync(new ArgumentException("At least one slab band is required."));

            var request = new SaveTdsRuleSetDto { Slabs = new List<TdsSlabDto>() };

            var result = await _controller.SaveRuleSet(2025, request);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("At least one slab band is required.", badRequest.Value);
        }

        [Fact]
        public async Task SaveRuleSet_Valid_MapsDtoAndReturnsNoContent()
        {
            TdsRuleSet? captured = null;
            _tdsService.Setup(s => s.SaveRuleSetAsync(It.IsAny<TdsRuleSet>()))
                .Callback<TdsRuleSet>(r => captured = r)
                .Returns(Task.CompletedTask);

            var request = new SaveTdsRuleSetDto
            {
                StandardDeduction = 75000,
                RebateIncomeLimit = 700000,
                CessRatePercent = 4,
                Slabs = new List<TdsSlabDto>
                {
                    new() { IncomeFrom = 0, IncomeTo = null, RatePercent = 5, DisplayOrder = 1 }
                }
            };

            var result = await _controller.SaveRuleSet(2025, request);

            Assert.IsType<NoContentResult>(result);
            Assert.NotNull(captured);
            Assert.Equal(2025, captured!.Config.FinancialYearStart);
            Assert.Single(captured.Slabs);
            Assert.Equal(2025, captured.Slabs[0].FinancialYearStart);
        }

        [Fact]
        public async Task DeleteRuleSet_CallsServiceAndReturnsNoContent()
        {
            var result = await _controller.DeleteRuleSet(2025);

            Assert.IsType<NoContentResult>(result);
            _tdsService.Verify(s => s.DeleteRuleSetAsync(2025), Times.Once);
        }
    }
}
