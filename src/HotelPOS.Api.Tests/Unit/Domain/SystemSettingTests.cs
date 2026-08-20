using HotelPOS.Domain.Entities;
using Xunit;

namespace HotelPOS.Tests;

public class SystemSettingTests
{
    private static SystemSetting BuildSource() => new()
    {
        Id = 99,
        HotelName = "Grand Hotel",
        HotelAddress = "123 Main St",
        HotelPhone = "9876543210",
        HotelGst = "29ABCDE1234F1Z5",
        DefaultPrinter = "Epson TM-T88",
        ShowPrintPreview = false,
        ReceiptFormat = "A4",
        ShowGstBreakdown = false,
        ShowItemsOnBill = false,
        ShowDiscountLine = true,
        ShowPhoneOnReceipt = false,
        ShowThankYouFooter = false,
        EnableRoundOff = true,
        IsCompositionScheme = true,
        EnableAutomatedBackups = false,
        OffsiteBackupPath = @"D:\Backups",
        IdleTimeoutMinutes = 30,
        ProfessionalTaxThreshold = 20000m,
        ProfessionalTaxAmount = 300m,
        SmtpHost = "smtp.example.com",
        SmtpPort = 465,
        SmtpUsername = "user@example.com",
        SmtpPassword = "secret",
        SmtpUseSsl = false,
        SmtpFromAddress = "noreply@example.com"
    };

    [Fact]
    public void UpdateFrom_CopiesAllEditableFields()
    {
        var source = BuildSource();
        var target = new SystemSetting { Id = 1 };

        target.UpdateFrom(source);

        Assert.Equal(source.HotelName, target.HotelName);
        Assert.Equal(source.HotelAddress, target.HotelAddress);
        Assert.Equal(source.HotelPhone, target.HotelPhone);
        Assert.Equal(source.HotelGst, target.HotelGst);
        Assert.Equal(source.DefaultPrinter, target.DefaultPrinter);
        Assert.Equal(source.ShowPrintPreview, target.ShowPrintPreview);
        Assert.Equal(source.ReceiptFormat, target.ReceiptFormat);
        Assert.Equal(source.ShowGstBreakdown, target.ShowGstBreakdown);
        Assert.Equal(source.ShowItemsOnBill, target.ShowItemsOnBill);
        Assert.Equal(source.ShowDiscountLine, target.ShowDiscountLine);
        Assert.Equal(source.ShowPhoneOnReceipt, target.ShowPhoneOnReceipt);
        Assert.Equal(source.ShowThankYouFooter, target.ShowThankYouFooter);
        Assert.Equal(source.EnableRoundOff, target.EnableRoundOff);
        Assert.Equal(source.IsCompositionScheme, target.IsCompositionScheme);
        Assert.Equal(source.EnableAutomatedBackups, target.EnableAutomatedBackups);
        Assert.Equal(source.OffsiteBackupPath, target.OffsiteBackupPath);
        Assert.Equal(source.IdleTimeoutMinutes, target.IdleTimeoutMinutes);
        Assert.Equal(source.ProfessionalTaxThreshold, target.ProfessionalTaxThreshold);
        Assert.Equal(source.ProfessionalTaxAmount, target.ProfessionalTaxAmount);
        Assert.Equal(source.SmtpHost, target.SmtpHost);
        Assert.Equal(source.SmtpPort, target.SmtpPort);
        Assert.Equal(source.SmtpUsername, target.SmtpUsername);
        Assert.Equal(source.SmtpPassword, target.SmtpPassword);
        Assert.Equal(source.SmtpUseSsl, target.SmtpUseSsl);
        Assert.Equal(source.SmtpFromAddress, target.SmtpFromAddress);
    }

    [Fact]
    public void UpdateFrom_DoesNotOverwriteTargetId()
    {
        var source = BuildSource();
        var target = new SystemSetting { Id = 1 };

        target.UpdateFrom(source);

        Assert.Equal(1, target.Id);
        Assert.NotEqual(source.Id, target.Id);
    }
}
