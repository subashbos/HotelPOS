using HotelPOS.Domain.Common.Constants;
using HotelPOS.ViewModels;
using Xunit;

namespace HotelPOS.Tests
{
    public class ConfirmCheckoutViewModelTests
    {
        [Fact]
        public void Properties_ShouldSetAndGetCorrectly()
        {
            // Arrange
            var vm = new ConfirmCheckoutViewModel();

            // Act
            vm.TotalItems = 5;
            vm.TotalAmount = 150.50m;
            vm.DiscountAmount = 10.00m;
            vm.FinalPayableAmount = 140.50m;
            vm.PaymentMode = PaymentModes.Card;

            // Assert
            Assert.Equal(5, vm.TotalItems);
            Assert.Equal(150.50m, vm.TotalAmount);
            Assert.Equal(10.00m, vm.DiscountAmount);
            Assert.Equal(140.50m, vm.FinalPayableAmount);
            Assert.Equal(PaymentModes.Card, vm.PaymentMode);
        }

        [Fact]
        public void ConfirmCommand_SetsDialogResultToTrue()
        {
            // Arrange
            var vm = new ConfirmCheckoutViewModel();

            // Act
            vm.ConfirmCommand.Execute(null);

            // Assert
            Assert.True(vm.DialogResult);
        }

        [Fact]
        public void CancelCommand_SetsDialogResultToFalse()
        {
            // Arrange
            var vm = new ConfirmCheckoutViewModel();

            // Act
            vm.CancelCommand.Execute(null);

            // Assert
            Assert.False(vm.DialogResult);
        }

        [Fact]
        public void DefaultPaymentMode_IsCash()
        {
            // Arrange
            var vm = new ConfirmCheckoutViewModel();

            // Assert
            Assert.Equal(PaymentModes.Cash, vm.PaymentMode);
        }

        [Fact]
        public void ConfirmCheckoutViewModel_ParsedPaymentAmounts_ParsedCorrectly()
        {
            var vm = new ConfirmCheckoutViewModel();
            vm.CashAmount = "150.50";
            vm.CardAmount = "200.75";
            vm.UpiAmount = "abc"; // invalid

            Assert.Equal(150.50m, vm.ParsedCash);
            Assert.Equal(200.75m, vm.ParsedCard);
            Assert.Equal(0m, vm.ParsedUpi);
        }

        [Fact]
        public void ConfirmCheckoutViewModel_OutstandingBalance_CalculatedCorrectly()
        {
            var vm = new ConfirmCheckoutViewModel();
            vm.FinalPayableAmount = 500m;
            vm.CashAmount = "150";
            vm.CardAmount = "200";
            vm.UpiAmount = "50";

            Assert.Equal(100m, vm.OutstandingBalance);
        }

        [Fact]
        public void ConfirmCheckoutViewModel_PaymentModeChanged_ClearsOrSetsInitialAmounts()
        {
            var vm = new ConfirmCheckoutViewModel();
            vm.FinalPayableAmount = 300m;
            
            // Switch to Split
            vm.PaymentMode = "Split";
            Assert.True(vm.IsSplitPayment);
            Assert.Equal("300", vm.CashAmount);
            Assert.Equal("0", vm.CardAmount);
            Assert.Equal("0", vm.UpiAmount);

            // Switch to Card
            vm.PaymentMode = "Card";
            Assert.False(vm.IsSplitPayment);
            Assert.Equal("0", vm.CashAmount);
            Assert.Equal("0", vm.CardAmount);
            Assert.Equal("0", vm.UpiAmount);
        }

        [Fact]
        public void ConfirmCheckoutViewModel_CanConfirm_DependsOnSplitPaymentAndOutstandingBalance()
        {
            var vm = new ConfirmCheckoutViewModel();
            vm.FinalPayableAmount = 300m;

            // Cash mode (not split)
            vm.PaymentMode = PaymentModes.Cash;
            Assert.True(vm.CanConfirm);

            // Split mode, outstanding balance > 0
            vm.PaymentMode = "Split";
            vm.CashAmount = "200";
            vm.CardAmount = "50"; // 50 outstanding
            Assert.False(vm.CanConfirm);

            // Split mode, outstanding balance == 0
            vm.UpiAmount = "50"; // 0 outstanding
            Assert.True(vm.CanConfirm);
        }
    }
}


