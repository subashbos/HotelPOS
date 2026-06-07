using HotelPOS.Application;
using HotelPOS.Application.UseCases;
using HotelPOS.Domain.Entities;
using HotelPOS.Application.Interfaces;
using Moq;
using Xunit;

namespace HotelPOS.Tests
{
    /// <summary>
    /// Covers SettingService edge cases missing from SaveUpdateRegressionTests.cs:
    /// EnableRoundOff and IsCompositionScheme persistence, GetSettings when
    /// settings already exist, and all billing option flags.
    /// </summary>
    public class SettingServiceLoopholeTests
    {
        private readonly Mock<ISettingRepository> _repo = new();
        private readonly SettingService _service;

        public SettingServiceLoopholeTests()
        {
            _service = new SettingService(_repo.Object);
        }

        // ── EnableRoundOff persisted ─────────────────────────────────────────

        [Fact]
        public async Task SaveSettingsAsync_EnableRoundOffTrue_IsPersisted()
        {
            var existing = new SystemSetting { Id = 1 };
            _repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existing);

            await _service.SaveSettingsAsync(new SystemSetting { EnableRoundOff = true });

            _repo.Verify(r => r.UpdateAsync(It.Is<SystemSetting>(s => s.EnableRoundOff == true)), Times.Once);
        }

        [Fact]
        public async Task SaveSettingsAsync_EnableRoundOffFalse_IsPersisted()
        {
            var existing = new SystemSetting { Id = 1, EnableRoundOff = true };
            _repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existing);

            await _service.SaveSettingsAsync(new SystemSetting { EnableRoundOff = false });

            _repo.Verify(r => r.UpdateAsync(It.Is<SystemSetting>(s => s.EnableRoundOff == false)), Times.Once);
        }

        // ── IsCompositionScheme persisted ────────────────────────────────────

        [Fact]
        public async Task SaveSettingsAsync_IsCompositionSchemeTrue_IsPersisted()
        {
            var existing = new SystemSetting { Id = 1 };
            _repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existing);

            await _service.SaveSettingsAsync(new SystemSetting { IsCompositionScheme = true });

            _repo.Verify(r => r.UpdateAsync(It.Is<SystemSetting>(s => s.IsCompositionScheme == true)), Times.Once);
        }

        [Fact]
        public async Task SaveSettingsAsync_IsCompositionSchemeFalse_IsPersisted()
        {
            var existing = new SystemSetting { Id = 1, IsCompositionScheme = true };
            _repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existing);

            await _service.SaveSettingsAsync(new SystemSetting { IsCompositionScheme = false });

            _repo.Verify(r => r.UpdateAsync(It.Is<SystemSetting>(s => s.IsCompositionScheme == false)), Times.Once);
        }

        // ── All billing flags in one save ────────────────────────────────────

        [Fact]
        public async Task SaveSettingsAsync_AllBillingFlags_AllPersisted()
        {
            var existing = new SystemSetting { Id = 1 };
            _repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existing);

            await _service.SaveSettingsAsync(new SystemSetting
            {
                ShowGstBreakdown = true,
                ShowItemsOnBill = false,
                ShowDiscountLine = true,
                ShowPhoneOnReceipt = false,
                ShowThankYouFooter = true,
                EnableRoundOff = true,
                IsCompositionScheme = false
            });

            _repo.Verify(r => r.UpdateAsync(It.Is<SystemSetting>(s =>
                s.ShowGstBreakdown == true &&
                s.ShowItemsOnBill == false &&
                s.ShowDiscountLine == true &&
                s.ShowPhoneOnReceipt == false &&
                s.ShowThankYouFooter == true &&
                s.EnableRoundOff == true &&
                s.IsCompositionScheme == false
            )), Times.Once);
        }

        // ── GetSettingsAsync when settings already exist ─────────────────────

        [Fact]
        public async Task GetSettingsAsync_WhenSettingsExist_ReturnsExistingWithoutAdding()
        {
            var existing = new SystemSetting { Id = 1, HotelName = "Grand Hotel" };
            _repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existing);

            var result = await _service.GetSettingsAsync();

            Assert.Equal("Grand Hotel", result.HotelName);
            _repo.Verify(r => r.AddAsync(It.IsAny<SystemSetting>()), Times.Never);
        }

        [Fact]
        public async Task GetSettingsAsync_WhenNoneExist_CreatesDefaultAndReturnsIt()
        {
            _repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((SystemSetting?)null);

            var result = await _service.GetSettingsAsync();

            Assert.NotNull(result);
            Assert.Equal(1, result.Id);
            _repo.Verify(r => r.AddAsync(It.Is<SystemSetting>(s => s.Id == 1)), Times.Once);
        }

        // ── SaveSettingsAsync — no existing record creates new ───────────────

        [Fact]
        public async Task SaveSettingsAsync_NoExistingRecord_CallsAddAsync()
        {
            _repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((SystemSetting?)null);
            var settings = new SystemSetting { HotelName = "New Hotel" };

            await _service.SaveSettingsAsync(settings);

            _repo.Verify(r => r.AddAsync(settings), Times.Once);
            _repo.Verify(r => r.UpdateAsync(It.IsAny<SystemSetting>()), Times.Never);
        }

        // ── SaveSettingsAsync — hotel profile fields ─────────────────────────

        [Fact]
        public async Task SaveSettingsAsync_HotelProfileFields_AllPersisted()
        {
            var existing = new SystemSetting { Id = 1 };
            _repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existing);

            await _service.SaveSettingsAsync(new SystemSetting
            {
                HotelName = "Sunrise Inn",
                HotelAddress = "12 Park Road",
                HotelPhone = "044-99999",
                HotelGst = "GSTIN_TEST"
            });

            _repo.Verify(r => r.UpdateAsync(It.Is<SystemSetting>(s =>
                s.HotelName == "Sunrise Inn" &&
                s.HotelAddress == "12 Park Road" &&
                s.HotelPhone == "044-99999" &&
                s.HotelGst == "GSTIN_TEST"
            )), Times.Once);
        }

        // ── SaveSettingsAsync — printer fields ───────────────────────────────

        [Fact]
        public async Task SaveSettingsAsync_PrinterFields_AllPersisted()
        {
            var existing = new SystemSetting { Id = 1 };
            _repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existing);

            await _service.SaveSettingsAsync(new SystemSetting
            {
                DefaultPrinter = "Thermal80",
                ReceiptFormat = "Thermal",
                ShowPrintPreview = false
            });

            _repo.Verify(r => r.UpdateAsync(It.Is<SystemSetting>(s =>
                s.DefaultPrinter == "Thermal80" &&
                s.ReceiptFormat == "Thermal" &&
                s.ShowPrintPreview == false
            )), Times.Once);
        }
    }
}
