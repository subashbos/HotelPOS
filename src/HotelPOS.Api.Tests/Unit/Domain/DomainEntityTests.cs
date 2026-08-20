using HotelPOS.Domain.Common.Constants;
using HotelPOS.Domain.Entities;
using Xunit;

namespace HotelPOS.Tests;

/// <summary>
/// Sanity tests for Domain entity default values and initializers.
/// These guard against regression if nullable defaults are accidentally removed.
/// </summary>
public class DomainEntityTests
{
    // ========== Item ==========

    [Fact]
    public void Item_DefaultName_IsEmptyString()
    {
        var item = new Item();
        Assert.Equal(string.Empty, item.Name);
    }

    [Fact]
    public void Item_DefaultPrice_IsZero()
    {
        var item = new Item();
        Assert.Equal(0m, item.Price);
    }

    [Fact]
    public void Item_DefaultId_IsZero()
    {
        var item = new Item();
        Assert.Equal(0, item.Id);
    }

    [Fact]
    public void Item_SetName_RetainsValue()
    {
        var item = new Item { Name = "Biryani" };
        Assert.Equal("Biryani", item.Name);
    }

    [Fact]
    public void Item_SetPrice_RetainsValue()
    {
        var item = new Item { Price = 199.99m };
        Assert.Equal(199.99m, item.Price);
    }

    // ========== OrderItem ==========

    [Fact]
    public void OrderItem_DefaultItemName_IsEmptyString()
    {
        var oi = new OrderItem();
        Assert.Equal(string.Empty, oi.ItemName);
    }

    [Fact]
    public void OrderItem_DefaultOrder_IsNull()
    {
        var oi = new OrderItem();
        Assert.Null(oi.Order);
    }

    [Fact]
    public void OrderItem_DefaultQuantity_IsZero()
    {
        var oi = new OrderItem();
        Assert.Equal(0, oi.Quantity);
    }

    [Fact]
    public void OrderItem_DefaultPrice_IsZero()
    {
        var oi = new OrderItem();
        Assert.Equal(0m, oi.Price);
    }

    [Fact]
    public void OrderItem_DefaultTotal_IsZero()
    {
        var oi = new OrderItem();
        Assert.Equal(0m, oi.Total);
    }

    [Fact]
    public void OrderItem_SetValues_RetainsAllValues()
    {
        var oi = new OrderItem
        {
            Id = 5,
            OrderId = 10,
            ItemId = 3,
            ItemName = "Coffee",
            Quantity = 2,
            Price = 50m,
            Total = 100m
        };

        Assert.Equal(5, oi.Id);
        Assert.Equal(10, oi.OrderId);
        Assert.Equal(3, oi.ItemId);
        Assert.Equal("Coffee", oi.ItemName);
        Assert.Equal(2, oi.Quantity);
        Assert.Equal(50m, oi.Price);
        Assert.Equal(100m, oi.Total);
    }

    // ========== Order ==========

    [Fact]
    public void Order_DefaultItems_IsNotNull()
    {
        var order = new Order();
        Assert.NotNull(order.Items);
    }

    [Fact]
    public void Order_DefaultItems_IsEmptyList()
    {
        var order = new Order();
        Assert.Empty(order.Items);
    }

    [Fact]
    public void Order_DefaultTotalAmount_IsZero()
    {
        var order = new Order();
        Assert.Equal(0m, order.TotalAmount);
    }

    [Fact]
    public void Order_DefaultTableNumber_IsZero()
    {
        var order = new Order();
        Assert.Equal(0, order.TableNumber);
    }

    [Fact]
    public void Order_ItemsCollection_CanAddItems()
    {
        var order = new Order();
        order.Items.Add(new OrderItem { ItemName = "Tea", Price = 30m, Total = 30m });

        Assert.Single(order.Items);
        Assert.Equal("Tea", order.Items[0].ItemName);
    }

    [Fact]
    public void Order_SetValues_RetainsAllValues()
    {
        var now = DateTime.UtcNow;
        var order = new Order
        {
            Id = 1,
            CreatedAt = now,
            TableNumber = 3,
            TotalAmount = 250m
        };

        Assert.Equal(1, order.Id);
        Assert.Equal(now, order.CreatedAt);
        Assert.Equal(3, order.TableNumber);
        Assert.Equal(250m, order.TotalAmount);
    }

    // ========== SystemSetting ==========

    [Fact]
    public void SystemSetting_DefaultHotelName_IsHotelPOS()
    {
        var setting = new SystemSetting();
        Assert.Equal("Hotel POS", setting.HotelName);
    }

    [Fact]
    public void SystemSetting_DefaultReceiptFormat_IsThermal()
    {
        var setting = new SystemSetting();
        Assert.Equal("Thermal", setting.ReceiptFormat);
    }

    [Fact]
    public void SystemSetting_DefaultIdleTimeoutMinutes_Is15()
    {
        var setting = new SystemSetting();
        Assert.Equal(15, setting.IdleTimeoutMinutes);
    }

    [Fact]
    public void SystemSetting_DefaultEnableRoundOff_IsFalse()
    {
        var setting = new SystemSetting();
        Assert.False(setting.EnableRoundOff);
    }

    [Fact]
    public void SystemSetting_DefaultIsCompositionScheme_IsFalse()
    {
        var setting = new SystemSetting();
        Assert.False(setting.IsCompositionScheme);
    }

    [Fact]
    public void SystemSetting_DefaultEnableAutomatedBackups_IsTrue()
    {
        var setting = new SystemSetting();
        Assert.True(setting.EnableAutomatedBackups);
    }

    [Fact]
    public void SystemSetting_DefaultId_IsZero()
    {
        var setting = new SystemSetting();
        Assert.Equal(0, setting.Id);
    }

    // ========== Employee ==========

    [Fact]
    public void Employee_DefaultEmploymentType_IsPermanent()
    {
        var employee = new Employee();
        Assert.Equal(EmploymentTypes.Permanent, employee.EmploymentType);
    }

    [Fact]
    public void Employee_DefaultStatus_IsActive()
    {
        var employee = new Employee();
        Assert.Equal(EmployeeStatuses.Active, employee.Status);
    }

    [Fact]
    public void Employee_DefaultEmployeeCode_IsEmptyString()
    {
        var employee = new Employee();
        Assert.Equal(string.Empty, employee.EmployeeCode);
    }

    [Fact]
    public void Employee_DefaultFirstName_IsEmptyString()
    {
        var employee = new Employee();
        Assert.Equal(string.Empty, employee.FirstName);
    }

    [Fact]
    public void Employee_DefaultId_IsZero()
    {
        var employee = new Employee();
        Assert.Equal(0, employee.Id);
    }

    // ========== Reservation ==========

    [Fact]
    public void Reservation_DefaultStatus_IsReserved()
    {
        var reservation = new Reservation();
        Assert.Equal(ReservationStatuses.Reserved, reservation.Status);
    }

    [Fact]
    public void Reservation_DefaultId_IsZero()
    {
        var reservation = new Reservation();
        Assert.Equal(0, reservation.Id);
    }

    [Fact]
    public void Reservation_DefaultPartySize_IsZero()
    {
        var reservation = new Reservation();
        Assert.Equal(0, reservation.PartySize);
    }

    // ========== CashSession ==========

    [Fact]
    public void CashSession_DefaultStatus_IsOpen()
    {
        var session = new CashSession();
        Assert.Equal(CashSessionStatuses.Open, session.Status);
    }

    [Fact]
    public void CashSession_DefaultOpenedBy_IsEmptyString()
    {
        var session = new CashSession();
        Assert.Equal(string.Empty, session.OpenedBy);
    }

    [Fact]
    public void CashSession_DefaultOpeningBalance_IsZero()
    {
        var session = new CashSession();
        Assert.Equal(0m, session.OpeningBalance);
    }

    [Fact]
    public void CashSession_DefaultClosedAt_IsNull()
    {
        var session = new CashSession();
        Assert.Null(session.ClosedAt);
    }

    // ========== Expense ==========

    [Fact]
    public void Expense_DefaultCategory_IsGeneral()
    {
        var expense = new Expense();
        Assert.Equal("General", expense.Category);
    }

    [Fact]
    public void Expense_DefaultPaymentMode_IsCash()
    {
        var expense = new Expense();
        Assert.Equal(PaymentModes.Cash, expense.PaymentMode);
    }

    [Fact]
    public void Expense_DefaultTitle_IsEmptyString()
    {
        var expense = new Expense();
        Assert.Equal(string.Empty, expense.Title);
    }

    [Fact]
    public void Expense_DefaultAmount_IsZero()
    {
        var expense = new Expense();
        Assert.Equal(0m, expense.Amount);
    }
}

