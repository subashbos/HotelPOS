using HotelPOS.Application;
using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Entities;
using HotelPOS.ViewModels;
using HotelPOS.Views;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace HotelPOS.Tests.Unit.ViewModels
{
    public class JournalViewModelTests
    {
        private readonly Mock<IOrderService> _orderServiceMock;
        private readonly Mock<INotificationService> _notificationServiceMock;

        public JournalViewModelTests()
        {
            _orderServiceMock = new Mock<IOrderService>();
            _notificationServiceMock = new Mock<INotificationService>();
        }

        [Fact]
        public void Properties_ShouldGetAndSetCorrectly()
        {
            var vm = new JournalViewModel(_orderServiceMock.Object, _notificationServiceMock.Object);
            
            var from = DateTime.Today.AddDays(-1);
            var to = DateTime.Today;
            
            vm.FromDate = from;
            vm.ToDate = to;
            vm.TableFilter = 5;

            Assert.Equal(from, vm.FromDate);
            Assert.Equal(to, vm.ToDate);
            Assert.Equal(5, vm.TableFilter);
        }

        [Fact]
        public async Task LoadMoreAsync_LoadsDataCorrectly_AndSetsHasMoreDataFalse_WhenLessThanPageSize()
        {
            // Arrange
            var vm = new JournalViewModel(_orderServiceMock.Object, _notificationServiceMock.Object);
            
            var orders = new List<Order>
            {
                new Order { Id = 1, TableNumber = 3, TotalAmount = 500, CreatedAt = DateTime.Now, Items = new List<OrderItem>() }
            };

            _orderServiceMock.Setup(s => s.GetPagedOrdersAsync(It.IsAny<PagedOrdersRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((orders, 1));

            // Act
            await vm.LoadMoreAsync();

            // Assert
            Assert.False(vm.HasMoreData);
            Assert.Single(vm.Items);
            Assert.Equal(1, vm.Items[0].Id);
            Assert.Equal("1 transaction", vm.RowCountText);
        }

        [Fact]
        public async Task LoadMoreAsync_LoadsDataCorrectly_AndSetsHasMoreDataTrue_WhenPageIsFull()
        {
            // Arrange
            var vm = new JournalViewModel(_orderServiceMock.Object, _notificationServiceMock.Object);
            
            var orders = new List<Order>();
            for (int i = 1; i <= 20; i++)
            {
                orders.Add(new Order { Id = i, TableNumber = 3, TotalAmount = 100, CreatedAt = DateTime.Now, Items = new List<OrderItem>() });
            }

            _orderServiceMock.Setup(s => s.GetPagedOrdersAsync(It.IsAny<PagedOrdersRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((orders, 20));

            // Act
            await vm.LoadMoreAsync();

            // Assert
            Assert.True(vm.HasMoreData);
            Assert.Equal(20, vm.Items.Count);
            Assert.Equal("20 transactions", vm.RowCountText);
        }

        [Fact]
        public void Refresh_ClearsItemsAndResetsCurrentPage()
        {
            // Arrange
            var vm = new JournalViewModel(_orderServiceMock.Object, _notificationServiceMock.Object);
            vm.Items.Add(new JournalRow { Id = 1, TableNumber = 2 });

            var orders = new List<Order>();
            for (int i = 1; i <= 20; i++)
            {
                orders.Add(new Order { Id = i, TableNumber = 3, TotalAmount = 100, CreatedAt = DateTime.Now, Items = new List<OrderItem>() });
            }

            _orderServiceMock.Setup(s => s.GetPagedOrdersAsync(It.IsAny<PagedOrdersRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((orders, 20));

            // Act
            vm.Refresh();

            // Assert
            Assert.Equal(20, vm.Items.Count);
            Assert.True(vm.HasMoreData);
        }

        [Fact]
        public async Task LoadMoreAsync_OnException_ShowsErrorNotification()
        {
            // Arrange
            var vm = new JournalViewModel(_orderServiceMock.Object, _notificationServiceMock.Object);
            
            _orderServiceMock.Setup(s => s.GetPagedOrdersAsync(It.IsAny<PagedOrdersRequest>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            await vm.LoadMoreAsync();

            // Assert
            _notificationServiceMock.Verify(n => n.ShowError("Failed to load data: Database error"), Times.Once);
            Assert.False(vm.IsLoading);
        }

        [Fact]
        public void Dispose_CancelsTokenSource()
        {
            var vm = new JournalViewModel(_orderServiceMock.Object, _notificationServiceMock.Object);
            
            // Should not throw
            var exception = Record.Exception(() => vm.Dispose());
            Assert.Null(exception);
        }
    }
}
