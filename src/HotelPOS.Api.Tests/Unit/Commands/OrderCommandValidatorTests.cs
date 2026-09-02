using Xunit;
using FluentValidation.TestHelper;
using HotelPOS.Application.UseCases.Orders.Commands;
using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Common.Constants;
using HotelPOS.Domain.Entities;
using System.Collections.Generic;

namespace HotelPOS.Tests.Unit.Commands
{
    public class OrderCommandValidatorTests
    {
        // ---------- UpdateOrderCommandValidator ----------
        [Fact]
        public void UpdateOrderCommandValidator_NullOrder_HasError()
        {
            var validator = new UpdateOrderCommandValidator();

            var result = validator.TestValidate(new UpdateOrderCommand(null!));
            result.ShouldHaveValidationErrorFor(x => x.Order);
        }

        [Fact]
        public void UpdateOrderCommandValidator_InvalidFields_HasErrors()
        {
            var validator = new UpdateOrderCommandValidator();

            var badOrder = new Order
            {
                Id = 0,
                Items = new List<OrderItem>(),
                DiscountAmount = -5,
                PaymentMode = "Bogus",
                OrderType = "Bogus"
            };
            var result = validator.TestValidate(new UpdateOrderCommand(badOrder));

            result.ShouldHaveValidationErrorFor(x => x.Order.Id);
            result.ShouldHaveValidationErrorFor(x => x.Order.Items);
            result.ShouldHaveValidationErrorFor(x => x.Order.DiscountAmount);
            result.ShouldHaveValidationErrorFor(x => x.Order.PaymentMode);
            result.ShouldHaveValidationErrorFor(x => x.Order.OrderType);
        }

        [Fact]
        public void UpdateOrderCommandValidator_InvalidItemFields_HasErrors()
        {
            var validator = new UpdateOrderCommandValidator();

            var badOrder = new Order
            {
                Id = 1,
                PaymentMode = PaymentModes.Cash,
                OrderType = OrderTypes.Takeaway,
                Items = new List<OrderItem>
                {
                    new OrderItem { ItemId = 1, ItemName = "Pizza", Price = -5, Quantity = 0 }
                }
            };
            var result = validator.TestValidate(new UpdateOrderCommand(badOrder));

            result.ShouldHaveValidationErrorFor("Order.Items[0].Price");
            result.ShouldHaveValidationErrorFor("Order.Items[0].Quantity");
        }

        [Fact]
        public void UpdateOrderCommandValidator_DineInWithoutTableNumber_HasError()
        {
            var validator = new UpdateOrderCommandValidator();

            var badOrder = new Order
            {
                Id = 1,
                PaymentMode = PaymentModes.Cash,
                OrderType = OrderTypes.DineIn,
                TableNumber = 0,
                Items = new List<OrderItem>
                {
                    new OrderItem { ItemId = 1, ItemName = "Pizza", Price = 250, Quantity = 1 }
                }
            };
            var result = validator.TestValidate(new UpdateOrderCommand(badOrder));

            result.ShouldHaveValidationErrorFor(x => x.Order);
        }

        [Fact]
        public void UpdateOrderCommandValidator_ValidOrder_HasNoErrors()
        {
            var validator = new UpdateOrderCommandValidator();

            var goodOrder = new Order
            {
                Id = 1,
                PaymentMode = PaymentModes.Cash,
                OrderType = OrderTypes.Takeaway,
                DiscountAmount = 0,
                Items = new List<OrderItem>
                {
                    new OrderItem { ItemId = 1, ItemName = "Pizza", Price = 250, Quantity = 2 }
                }
            };
            var result = validator.TestValidate(new UpdateOrderCommand(goodOrder));
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void UpdateOrderCommandValidator_ValidDineInWithTableNumber_HasNoErrors()
        {
            var validator = new UpdateOrderCommandValidator();

            var goodOrder = new Order
            {
                Id = 1,
                PaymentMode = PaymentModes.Cash,
                OrderType = OrderTypes.DineIn,
                TableNumber = 5,
                DiscountAmount = 0,
                Items = new List<OrderItem>
                {
                    new OrderItem { ItemId = 1, ItemName = "Pizza", Price = 250, Quantity = 2 }
                }
            };
            var result = validator.TestValidate(new UpdateOrderCommand(goodOrder));
            result.ShouldNotHaveAnyValidationErrors();
        }

        // ---------- DeleteOrderCommandValidator ----------
        [Fact]
        public void DeleteOrderCommandValidator_Validates_Correctly()
        {
            var validator = new DeleteOrderCommandValidator();

            var resBad = validator.TestValidate(new DeleteOrderCommand(0));
            resBad.ShouldHaveValidationErrorFor(x => x.OrderId);

            var resGood = validator.TestValidate(new DeleteOrderCommand(1));
            resGood.ShouldNotHaveAnyValidationErrors();
        }

        // ---------- RefundOrderCommandValidator ----------
        [Fact]
        public void RefundOrderCommandValidator_InvalidFields_HasErrors()
        {
            var validator = new RefundOrderCommandValidator();

            var badCmd = new RefundOrderCommand(0, new List<OrderItemRefundDto>(), "");
            var result = validator.TestValidate(badCmd);

            result.ShouldHaveValidationErrorFor(x => x.OrderId);
            result.ShouldHaveValidationErrorFor(x => x.Reason);
            result.ShouldHaveValidationErrorFor(x => x.ItemsToRefund);
        }

        [Fact]
        public void RefundOrderCommandValidator_InvalidRefundItemFields_HasErrors()
        {
            var validator = new RefundOrderCommandValidator();

            var badCmd = new RefundOrderCommand(
                1,
                new List<OrderItemRefundDto> { new OrderItemRefundDto(0, 0) },
                "Wrong item");
            var result = validator.TestValidate(badCmd);

            result.ShouldHaveValidationErrorFor("ItemsToRefund[0].ItemId");
            result.ShouldHaveValidationErrorFor("ItemsToRefund[0].QuantityToRefund");
        }

        [Fact]
        public void RefundOrderCommandValidator_ValidCommand_HasNoErrors()
        {
            var validator = new RefundOrderCommandValidator();

            var goodCmd = new RefundOrderCommand(
                1,
                new List<OrderItemRefundDto> { new OrderItemRefundDto(1, 2) },
                "Customer changed mind");
            var result = validator.TestValidate(goodCmd);
            result.ShouldNotHaveAnyValidationErrors();
        }
    }
}
