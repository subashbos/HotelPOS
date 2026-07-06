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
    }
}

