using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Common.Constants;
using HotelPOS.Domain.Entities;
using HotelPOS.ViewModels;
using Moq;
using Xunit;

namespace HotelPOS.Tests.Unit.ViewModels
{
    public class EstimationViewModelTests
    {
        private readonly Mock<IEstimationService> _mockEstimationService = new();
        private readonly Mock<ICustomerService> _mockCustomerService = new();
        private readonly Mock<IItemService> _mockItemService = new();
        private readonly Mock<INotificationService> _mockNotif = new();
        private readonly EstimationViewModel _vm;

        public EstimationViewModelTests()
        {
            _mockCustomerService.Setup(s => s.GetCustomersAsync(It.IsAny<bool>())).ReturnsAsync(new List<Customer>());
            _mockItemService.Setup(s => s.GetItemsAsync()).ReturnsAsync(new List<Item>());
            _mockEstimationService.Setup(s => s.GetEstimationsAsync()).ReturnsAsync(new List<Estimation>());

            _vm = new EstimationViewModel(
                _mockEstimationService.Object,
                _mockCustomerService.Object,
                _mockItemService.Object,
                _mockNotif.Object
            );
        }

        [Fact]
        public async Task LoadDataAsync_LoadsCustomersItemsAndEstimations()
        {
            var customers = new List<Customer> { new() { Id = 1, Name = "Cust1" } };
            var items = new List<Item> { new() { Id = 1, Name = "Item1", Price = 100m, TaxPercentage = 5m } };
            var estimations = new List<Estimation> { new() { Id = 1, EstimationNumber = "EST-1" } };

            _mockCustomerService.Setup(s => s.GetCustomersAsync(It.IsAny<bool>())).ReturnsAsync(customers);
            _mockItemService.Setup(s => s.GetItemsAsync()).ReturnsAsync(items);
            _mockEstimationService.Setup(s => s.GetEstimationsAsync()).ReturnsAsync(estimations);

            await _vm.LoadDataAsync();

            Assert.Single(_vm.Customers);
            Assert.Single(_vm.CatalogItems);
            Assert.Single(_vm.Estimations);
            Assert.Single(_vm.EstimationRows); // initializes with one empty row
        }

        [Fact]
        public async Task AddRow_IncreasesRowCountAndSetsSNo()
        {
            await _vm.InitializationTask;

            _vm.AddRowCommand.Execute(null);

            Assert.Equal(2, _vm.EstimationRows.Count);
            Assert.Equal(1, _vm.EstimationRows[0].SNo);
            Assert.Equal(2, _vm.EstimationRows[1].SNo);
        }

        [Fact]
        public async Task RemoveRow_RemovesSelectedRow()
        {
            await _vm.InitializationTask;

            _vm.AddRowCommand.Execute(null);
            var secondRow = _vm.EstimationRows[1];

            _vm.RemoveRowCommand.Execute(secondRow);

            Assert.Single(_vm.EstimationRows);
        }

        [Fact]
        public async Task RowPropertiesChanged_RecalculatesTotals()
        {
            await _vm.InitializationTask;

            _vm.CatalogItems.Add(new Item { Id = 1, Name = "Item1", Price = 100m, TaxPercentage = 10m });
            var row = _vm.EstimationRows[0];

            row.ItemId = 1;
            row.Quantity = 2; // 2 * 100 = 200 subtotal, 20 tax, 0 discount

            Assert.Equal("Item1", row.ItemName);
            Assert.Equal(220m, row.Total);
            Assert.Equal(200m, _vm.Subtotal);
            Assert.Equal(20m, _vm.TotalTax);
            Assert.Equal(220m, _vm.GrandTotal);
        }

        [Fact]
        public async Task SaveEstimationCommand_EmptyNumber_ShowsWarning()
        {
            await _vm.InitializationTask;

            _vm.EstimationNumber = string.Empty;

            await _vm.SaveEstimationCommand.ExecuteAsync(null);

            _mockNotif.Verify(n => n.ShowWarning(It.IsAny<string>()), Times.Once);
            _mockEstimationService.Verify(s => s.SaveEstimationAsync(It.IsAny<Estimation>()), Times.Never);
        }

        [Fact]
        public async Task SaveEstimationCommand_NoItems_ShowsWarning()
        {
            await _vm.InitializationTask;

            _vm.EstimationNumber = "EST-1";
            // Default row has ItemId == 0, so it doesn't count as an active row.

            await _vm.SaveEstimationCommand.ExecuteAsync(null);

            _mockNotif.Verify(n => n.ShowWarning(It.IsAny<string>()), Times.Once);
            _mockEstimationService.Verify(s => s.SaveEstimationAsync(It.IsAny<Estimation>()), Times.Never);
        }

        [Fact]
        public async Task SaveEstimationCommand_Valid_SavesAndResetsForm()
        {
            await _vm.InitializationTask;

            _vm.CatalogItems.Add(new Item { Id = 1, Name = "Item1", Price = 100m, TaxPercentage = 5m });
            _vm.EstimationNumber = "EST-1";
            _vm.EstimationRows[0].ItemId = 1;
            _vm.EstimationRows[0].Quantity = 1;

            await _vm.SaveEstimationCommand.ExecuteAsync(null);

            _mockEstimationService.Verify(s => s.SaveEstimationAsync(It.Is<Estimation>(e => e.EstimationNumber == "EST-1")), Times.Once);
            _mockNotif.Verify(n => n.ShowSuccess(It.IsAny<string>()), Times.Once);
            Assert.Empty(_vm.EstimationNumber);
        }

        [Fact]
        public async Task Cancel_ResetsFormToDefault()
        {
            await _vm.InitializationTask;

            _vm.EstimationNumber = "EST-1";
            _vm.Notes = "Some notes";

            _vm.CancelCommand.Execute(null);

            Assert.Empty(_vm.EstimationNumber);
            Assert.Empty(_vm.Notes);
            Assert.Single(_vm.EstimationRows);
        }

        [Fact]
        public async Task MarkAsSentCommand_UpdatesStatusAndRefreshes()
        {
            await _vm.InitializationTask;
            var estimation = new Estimation { Id = 5, EstimationNumber = "EST-5", Status = EstimationStatuses.Draft };

            await _vm.MarkAsSentCommand.ExecuteAsync(estimation);

            _mockEstimationService.Verify(
                s => s.UpdateEstimationAsync(It.Is<Estimation>(e => e.Id == 5 && e.Status == EstimationStatuses.Sent)),
                Times.Once);
            _mockNotif.Verify(n => n.ShowSuccess(It.IsAny<string>()), Times.Once);
            _mockEstimationService.Verify(s => s.GetEstimationsAsync(), Times.AtLeast(2)); // initial load + refresh after
        }

        [Fact]
        public async Task MarkAsSentCommand_ServiceThrows_DoesNotMutateBoundEstimationStatus()
        {
            // Regression test: the estimation object passed in is the same instance bound to the
            // Estimations grid. ChangeStatusAsync must send a copy with the new status to the
            // service, never mutate this shared instance directly - otherwise a failed update
            // would leave the UI showing a status the server never actually accepted.
            await _vm.InitializationTask;
            var estimation = new Estimation { Id = 8, EstimationNumber = "EST-8", Status = EstimationStatuses.Draft };
            _mockEstimationService.Setup(s => s.UpdateEstimationAsync(It.IsAny<Estimation>()))
                .ThrowsAsync(new InvalidOperationException("Server rejected the update."));

            await _vm.MarkAsSentCommand.ExecuteAsync(estimation);

            Assert.Equal(EstimationStatuses.Draft, estimation.Status);
            _mockNotif.Verify(n => n.ShowError(It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task ConvertToOrderCommand_ServiceThrows_ShowsError()
        {
            await _vm.InitializationTask;
            var estimation = new Estimation { Id = 6, EstimationNumber = "EST-6", Status = EstimationStatuses.Accepted };
            _mockEstimationService.Setup(s => s.ConvertToOrderAsync(6))
                .ThrowsAsync(new InvalidOperationException("Only an Accepted estimation can be converted."));

            await _vm.ConvertToOrderCommand.ExecuteAsync(estimation);

            _mockNotif.Verify(n => n.ShowError(It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task ConvertToOrderCommand_Success_ShowsSuccessWithOrderId()
        {
            await _vm.InitializationTask;
            var estimation = new Estimation { Id = 7, EstimationNumber = "EST-7", Status = EstimationStatuses.Accepted };
            _mockEstimationService.Setup(s => s.ConvertToOrderAsync(7)).ReturnsAsync(99);

            await _vm.ConvertToOrderCommand.ExecuteAsync(estimation);

            _mockNotif.Verify(n => n.ShowSuccess(It.Is<string>(msg => msg.Contains("99"))), Times.Once);
        }
    }
}
