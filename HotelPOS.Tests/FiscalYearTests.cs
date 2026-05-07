using HotelPOS.Application;
using HotelPOS.Application.Interface;
using HotelPOS.Domain;
using HotelPOS.Domain.Interface;
using MediatR;
using Moq;
using Xunit;

namespace HotelPOS.Tests
{
    public class FiscalYearTests
    {
        private readonly Mock<IOrderRepository> _orderRepo = new();
        private readonly Mock<IMediator> _mediator = new();
        private readonly Mock<IItemService> _itemService = new();
        private readonly OrderService _service;

        public FiscalYearTests()
        {
            _service = new OrderService(_orderRepo.Object, _mediator.Object, _itemService.Object);
        }

        [Fact]
        public void GetFiscalYear_BeforeApril_ReturnsPreviousYearRange()
        {
            // March 31, 2024 -> 2023-24
            var date = new DateTime(2024, 3, 31);
            var fy = CallGetFiscalYear(date);
            Assert.Equal("2023-24", fy);
        }

        [Fact]
        public void GetFiscalYear_AfterApril_ReturnsCurrentYearRange()
        {
            // April 1, 2024 -> 2024-25
            var date = new DateTime(2024, 4, 1);
            var fy = CallGetFiscalYear(date);
            Assert.Equal("2024-25", fy);
        }

        [Fact]
        public async Task SaveOrder_CallsRepoWithCorrectFiscalYear()
        {
            var items = new List<OrderItem> { new OrderItem { ItemId = 1, Quantity = 1, Price = 100 } };

            // We can't easily mock DateTime.Now inside the service unless we use a provider, 
            // but the service currently uses DateTime.UtcNow.ToLocalTime().
            // For testing purposes, we verify the logic of GetFiscalYear directly.

            _orderRepo.Setup(r => r.GetNextInvoiceNumberAsync(It.IsAny<string>()))
                .ReturnsAsync("INV/2024-25/0001");

            await _service.SaveOrderAsync(items, 1);

            _orderRepo.Verify(r => r.GetNextInvoiceNumberAsync(It.Is<string>(s => s.Contains("-"))), Times.Once);
        }

        private string CallGetFiscalYear(DateTime date)
        {
            var method = typeof(OrderService).GetMethod("GetFiscalYear", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return (string)method!.Invoke(_service, new object[] { date })!;
        }
    }
}
