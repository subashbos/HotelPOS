using HotelPOS.Application.Common.Models;
using HotelPOS.Application.Interfaces;
using HotelPOS.Application.UseCases;
using HotelPOS.Domain.Entities;
using Moq;
using Xunit;

namespace HotelPOS.Tests.Unit.Services
{
    public class TdsServiceTests
    {
        private readonly Mock<ITdsRepository> _repository = new();
        private readonly TdsService _service;

        public TdsServiceTests()
        {
            _service = new TdsService(_repository.Object);
        }

        private static TdsRuleSet ValidRuleSet() => new()
        {
            Config = new TdsConfig { FinancialYearStart = 2025, StandardDeduction = 75000, RebateIncomeLimit = 700000, CessRatePercent = 4 },
            Slabs = new List<TdsSlab>
            {
                new() { IncomeFrom = 0, IncomeTo = 300000, RatePercent = 0, DisplayOrder = 1 },
                new() { IncomeFrom = 300000, IncomeTo = 700000, RatePercent = 5, DisplayOrder = 2 },
                new() { IncomeFrom = 700000, IncomeTo = null, RatePercent = 10, DisplayOrder = 3 }
            }
        };

        [Fact]
        public async Task SaveRuleSetAsync_NullRuleSet_Throws()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() => _service.SaveRuleSetAsync(null!));
        }

        [Fact]
        public async Task SaveRuleSetAsync_ValidRuleSet_SavesToRepository()
        {
            var ruleSet = ValidRuleSet();

            await _service.SaveRuleSetAsync(ruleSet);

            _repository.Verify(r => r.SaveRuleSetAsync(ruleSet), Times.Once);
        }

        [Theory]
        [InlineData(-1, 700000, 4)]
        [InlineData(75000, -1, 4)]
        [InlineData(75000, 700000, -1)]
        public async Task SaveRuleSetAsync_NegativeConfigValue_Throws(decimal standardDeduction, decimal rebateLimit, decimal cessRate)
        {
            var ruleSet = ValidRuleSet();
            ruleSet.Config.StandardDeduction = standardDeduction;
            ruleSet.Config.RebateIncomeLimit = rebateLimit;
            ruleSet.Config.CessRatePercent = cessRate;

            await Assert.ThrowsAsync<ArgumentException>(() => _service.SaveRuleSetAsync(ruleSet));
            _repository.Verify(r => r.SaveRuleSetAsync(It.IsAny<TdsRuleSet>()), Times.Never);
        }

        [Fact]
        public async Task SaveRuleSetAsync_NoSlabs_Throws()
        {
            var ruleSet = ValidRuleSet();
            ruleSet.Slabs.Clear();

            await Assert.ThrowsAsync<ArgumentException>(() => _service.SaveRuleSetAsync(ruleSet));
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(101)]
        public async Task SaveRuleSetAsync_SlabRateOutOfRange_Throws(decimal rate)
        {
            var ruleSet = ValidRuleSet();
            ruleSet.Slabs[0].RatePercent = rate;

            await Assert.ThrowsAsync<ArgumentException>(() => _service.SaveRuleSetAsync(ruleSet));
        }

        [Fact]
        public async Task SaveRuleSetAsync_NegativeIncomeFrom_Throws()
        {
            var ruleSet = ValidRuleSet();
            ruleSet.Slabs[0].IncomeFrom = -1;

            await Assert.ThrowsAsync<ArgumentException>(() => _service.SaveRuleSetAsync(ruleSet));
        }

        [Fact]
        public async Task SaveRuleSetAsync_IncomeToNotGreaterThanIncomeFrom_Throws()
        {
            var ruleSet = ValidRuleSet();
            ruleSet.Slabs[0].IncomeTo = ruleSet.Slabs[0].IncomeFrom;

            await Assert.ThrowsAsync<ArgumentException>(() => _service.SaveRuleSetAsync(ruleSet));
        }

        [Fact]
        public async Task SaveRuleSetAsync_NonContiguousSlabs_Throws()
        {
            var ruleSet = ValidRuleSet();
            ruleSet.Slabs[1].IncomeFrom = 350000; // gap after slab 1's IncomeTo of 300000

            await Assert.ThrowsAsync<ArgumentException>(() => _service.SaveRuleSetAsync(ruleSet));
        }

        [Fact]
        public async Task SaveRuleSetAsync_TopSlabHasUpperBound_Throws()
        {
            var ruleSet = ValidRuleSet();
            ruleSet.Slabs[2].IncomeTo = 1000000; // top slab must be unbounded

            await Assert.ThrowsAsync<ArgumentException>(() => _service.SaveRuleSetAsync(ruleSet));
        }

        [Fact]
        public async Task GetFinancialYearsAsync_DelegatesToRepository()
        {
            _repository.Setup(r => r.GetFinancialYearsAsync()).ReturnsAsync(new List<int> { 2024, 2025 });

            var result = await _service.GetFinancialYearsAsync();

            Assert.Equal(new List<int> { 2024, 2025 }, result);
        }

        [Fact]
        public async Task GetRuleSetAsync_DelegatesToRepository()
        {
            var ruleSet = ValidRuleSet();
            _repository.Setup(r => r.GetRuleSetAsync(2025)).ReturnsAsync(ruleSet);

            var result = await _service.GetRuleSetAsync(2025);

            Assert.Same(ruleSet, result);
        }

        [Fact]
        public async Task DeleteRuleSetAsync_DelegatesToRepository()
        {
            await _service.DeleteRuleSetAsync(2025);

            _repository.Verify(r => r.DeleteRuleSetAsync(2025), Times.Once);
        }
    }
}
