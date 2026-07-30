using HotelPOS.Application.Common.Models;
using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Entities;
using HotelPOS.ViewModels;
using Moq;
using Xunit;

namespace HotelPOS.Tests.Unit.ViewModels
{
    public class TdsViewModelTests
    {
        private readonly Mock<ITdsService> _tdsServiceMock = new();
        private readonly Mock<INotificationService> _notificationServiceMock = new();
        private readonly TdsViewModel _viewModel;

        public TdsViewModelTests()
        {
            _viewModel = new TdsViewModel(_tdsServiceMock.Object, _notificationServiceMock.Object);
        }

        [Fact]
        public async Task InitializeAsync_LoadsFinancialYearsAndRuleset()
        {
            _tdsServiceMock.Setup(s => s.GetFinancialYearsAsync()).ReturnsAsync(new List<int> { 2025, 2026 });
            _tdsServiceMock.Setup(s => s.GetRuleSetAsync(It.IsAny<int>())).ReturnsAsync((TdsRuleSet?)null);

            await _viewModel.InitializeAsync();

            Assert.Equal(2, _viewModel.FinancialYears.Count);
            Assert.Contains(2025, _viewModel.FinancialYears);
            Assert.Contains(2026, _viewModel.FinancialYears);
            Assert.Equal(4, _viewModel.Slabs.Count); // Defaults added when RuleSet is null
        }

        [Fact]
        public async Task LoadRuleSetAsync_WhenRuleSetExists_PopulatesSlabsAndConfig()
        {
            _viewModel.SelectedYear = 2026;
            var ruleSet = new TdsRuleSet
            {
                Config = new TdsConfig { FinancialYearStart = 2026, StandardDeduction = 75000, RebateIncomeLimit = 700000, CessRatePercent = 4 },
                Slabs = new List<TdsSlab>
                {
                    new() { FinancialYearStart = 2026, IncomeFrom = 0, IncomeTo = 300000, RatePercent = 0, DisplayOrder = 1 },
                    new() { FinancialYearStart = 2026, IncomeFrom = 300000, IncomeTo = 600000, RatePercent = 5, DisplayOrder = 2 }
                }
            };

            _tdsServiceMock.Setup(s => s.GetRuleSetAsync(2026)).ReturnsAsync(ruleSet);

            await _viewModel.LoadRuleSetAsync();

            Assert.Equal(75000, _viewModel.StandardDeduction);
            Assert.Equal(700000, _viewModel.RebateIncomeLimit);
            Assert.Equal(4, _viewModel.CessRatePercent);
            Assert.Equal(2, _viewModel.Slabs.Count);
            Assert.Equal(300000, _viewModel.Slabs[0].IncomeTo);
        }

        [Fact]
        public void AddSlab_AppendsNewSlabAndCapsPrevious()
        {
            _viewModel.Slabs.Add(new TdsSlab { IncomeFrom = 0, IncomeTo = null, RatePercent = 0, DisplayOrder = 1 });

            _viewModel.AddSlab();

            Assert.Equal(2, _viewModel.Slabs.Count);
            Assert.NotNull(_viewModel.Slabs[0].IncomeTo);
            Assert.Null(_viewModel.Slabs[1].IncomeTo);
            Assert.Equal(2, _viewModel.Slabs[1].DisplayOrder);
        }

        [Fact]
        public void RemoveSlab_RemovesSelectedAndReorders()
        {
            var slab1 = new TdsSlab { DisplayOrder = 1 };
            var slab2 = new TdsSlab { DisplayOrder = 2 };
            var slab3 = new TdsSlab { DisplayOrder = 3 };

            _viewModel.Slabs.Add(slab1);
            _viewModel.Slabs.Add(slab2);
            _viewModel.Slabs.Add(slab3);

            _viewModel.SelectedSlab = slab2;
            _viewModel.RemoveSlab();

            Assert.Equal(2, _viewModel.Slabs.Count);
            Assert.Equal(1, slab1.DisplayOrder);
            Assert.Equal(2, slab3.DisplayOrder);
        }

        [Fact]
        public async Task SaveAsync_Success_SavesRuleSetAndShowsSuccess()
        {
            _viewModel.SelectedYear = 2026;
            _viewModel.StandardDeduction = 75000;
            _viewModel.RebateIncomeLimit = 700000;
            _viewModel.CessRatePercent = 4;
            _viewModel.Slabs.Add(new TdsSlab { IncomeFrom = 0, IncomeTo = 400000, RatePercent = 0, DisplayOrder = 1 });

            _tdsServiceMock.Setup(s => s.GetFinancialYearsAsync()).ReturnsAsync(new List<int> { 2026 });
            _tdsServiceMock.Setup(s => s.GetRuleSetAsync(2026)).ReturnsAsync((TdsRuleSet?)null);

            await _viewModel.SaveAsync();

            _tdsServiceMock.Verify(s => s.SaveRuleSetAsync(It.Is<TdsRuleSet>(r =>
                r.Config.FinancialYearStart == 2026 && r.Slabs.Count == 1)), Times.Once);

            _notificationServiceMock.Verify(n => n.ShowSuccess(It.Is<string>(msg => msg.Contains("saved successfully"))), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_Success_DeletesRuleSetAndShowsSuccess()
        {
            _viewModel.SelectedYear = 2026;
            _tdsServiceMock.Setup(s => s.GetFinancialYearsAsync()).ReturnsAsync(new List<int> { 2026 });
            _tdsServiceMock.Setup(s => s.GetRuleSetAsync(2026)).ReturnsAsync((TdsRuleSet?)null);

            await _viewModel.DeleteAsync();

            _tdsServiceMock.Verify(s => s.DeleteRuleSetAsync(2026), Times.Once);
            _notificationServiceMock.Verify(n => n.ShowSuccess(It.Is<string>(msg => msg.Contains("deleted"))), Times.Once);
        }
    }
}
