using HotelPOS.Domain.Entities;
using Xunit;

namespace HotelPOS.Tests;

public class ComputedPropertiesTests
{
    // ========== BomEntry ==========

    [Fact]
    public void BomEntry_EffectiveQuantity_ZeroWastage_EqualsQuantityRequired()
    {
        var entry = new BomEntry { QuantityRequired = 10m, WastagePercentage = 0m };

        Assert.Equal(10m, entry.EffectiveQuantity);
    }

    [Fact]
    public void BomEntry_EffectiveQuantity_TwentyPercentWastage_ReturnsOneTwoTimesQuantity()
    {
        var entry = new BomEntry { QuantityRequired = 10m, WastagePercentage = 20m };

        Assert.Equal(12m, entry.EffectiveQuantity);
    }

    [Fact]
    public void BomEntry_IngredientCost_NullRawMaterial_IsZero()
    {
        var entry = new BomEntry { QuantityRequired = 10m, WastagePercentage = 20m, RawMaterial = null };

        Assert.Equal(0m, entry.IngredientCost);
    }

    [Fact]
    public void BomEntry_IngredientCost_WithRawMaterial_UsesEffectiveQuantityTimesCostPerUnit()
    {
        var entry = new BomEntry
        {
            QuantityRequired = 10m,
            WastagePercentage = 20m,
            RawMaterial = new RawMaterial { CostPerUnit = 5m }
        };

        // EffectiveQuantity = 10 * 1.20 = 12; IngredientCost = 12 * 5 = 60
        Assert.Equal(60m, entry.IngredientCost);
    }

    // ========== LeaveBalance ==========

    [Fact]
    public void LeaveBalance_AvailableDays_SubtractsUsedAndPendingFromEntitled()
    {
        var balance = new LeaveBalance { EntitledDays = 12m, UsedDays = 3m, PendingDays = 2m };

        Assert.Equal(7m, balance.AvailableDays);
    }

    [Fact]
    public void LeaveBalance_AvailableDays_NoUsageOrPending_EqualsEntitledDays()
    {
        var balance = new LeaveBalance { EntitledDays = 12m, UsedDays = 0m, PendingDays = 0m };

        Assert.Equal(12m, balance.AvailableDays);
    }

    // ========== SalaryStructure ==========

    [Fact]
    public void SalaryStructure_GrossMonthly_SumsAllComponents()
    {
        var salary = new SalaryStructure
        {
            Basic = 20000m,
            Hra = 8000m,
            Da = 1000m,
            ConveyanceAllowance = 1600m,
            MedicalAllowance = 1250m,
            SpecialAllowance = 3000m
        };

        Assert.Equal(34850m, salary.GrossMonthly);
    }

    [Fact]
    public void SalaryStructure_GrossMonthly_ZeroAllowances_EqualsBasic()
    {
        var salary = new SalaryStructure { Basic = 15000m };

        Assert.Equal(15000m, salary.GrossMonthly);
    }
}
