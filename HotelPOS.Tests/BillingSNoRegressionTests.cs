using HotelPOS.Application.Interface;
using HotelPOS.Application.Interfaces;
using HotelPOS.Domain;
using HotelPOS.ViewModels;
using Moq;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace HotelPOS.Tests
{
    public class BillingSNoRegressionTests
    {
        private readonly Mock<IItemService> _itemServiceMock;
        private readonly Mock<ICartService> _cartServiceMock;
        private readonly Mock<IOrderService> _orderServiceMock;
        private readonly Mock<ISettingService> _settingServiceMock;
        private readonly Mock<ICategoryService> _categoryServiceMock;
        private readonly Mock<INotificationService> _notificationServiceMock;
        private readonly Mock<ICashService> _cashServiceMock;

        public BillingSNoRegressionTests()
        {
            _itemServiceMock = new Mock<IItemService>();
            _cartServiceMock = new Mock<ICartService>();
            _orderServiceMock = new Mock<IOrderService>();
            _settingServiceMock = new Mock<ISettingService>();
            _categoryServiceMock = new Mock<ICategoryService>();
            _notificationServiceMock = new Mock<INotificationService>();
            _cashServiceMock = new Mock<ICashService>();

            // Setup default empty returns
            _categoryServiceMock.Setup(m => m.GetCategoriesAsync()).ReturnsAsync(new List<Category>());
            _itemServiceMock.Setup(m => m.GetItemsAsync()).ReturnsAsync(new List<Item>());
            _cartServiceMock.Setup(c => c.GetHeldOrders()).Returns(new List<HeldOrder>());
        }

        [Fact]
        public void UpdateCart_AssignsSequentialSNo_WhenItemsAdded()
        {
            // Arrange
            var items = new List<OrderItem>
            {
                new OrderItem { ItemId = 1, ItemName = "Item A", Quantity = 1, Price = 100 },
                new OrderItem { ItemId = 2, ItemName = "Item B", Quantity = 1, Price = 200 }
            };
            _cartServiceMock.Setup(c => c.GetItems(It.IsAny<int>())).Returns(items);

            var viewModel = new BillingViewModel(
                _itemServiceMock.Object,
                _cartServiceMock.Object,
                _orderServiceMock.Object,
                _settingServiceMock.Object,
                _categoryServiceMock.Object,
                _notificationServiceMock.Object,
                _cashServiceMock.Object);

            // Act - Select table to trigger UpdateCart
            viewModel.SelectTableCommand.Execute(1); 

            // Assert
            Assert.Equal(2, viewModel.Cart.Count);
            Assert.Equal(1, viewModel.Cart[0].SNo);
            Assert.Equal(2, viewModel.Cart[1].SNo);
        }

        [Fact]
        public void UpdateCart_ReassignsSNoSequentially_AfterItemRemoved()
        {
            // Arrange
            var initialItems = new List<OrderItem>
            {
                new OrderItem { ItemId = 1, ItemName = "Item A" },
                new OrderItem { ItemId = 2, ItemName = "Item B" },
                new OrderItem { ItemId = 3, ItemName = "Item C" }
            };
            
            var viewModel = new BillingViewModel(
                _itemServiceMock.Object,
                _cartServiceMock.Object,
                _orderServiceMock.Object,
                _settingServiceMock.Object,
                _categoryServiceMock.Object,
                _notificationServiceMock.Object,
                _cashServiceMock.Object);

            _cartServiceMock.Setup(c => c.GetItems(1)).Returns(initialItems);
            viewModel.SelectTableCommand.Execute(1);

            Assert.Equal(3, viewModel.Cart.Count);
            Assert.Equal(1, viewModel.Cart[0].SNo);
            Assert.Equal(2, viewModel.Cart[1].SNo);
            Assert.Equal(3, viewModel.Cart[2].SNo);

            // Act - Remove item 2
            var remainingItems = new List<OrderItem>
            {
                new OrderItem { ItemId = 1, ItemName = "Item A" },
                new OrderItem { ItemId = 3, ItemName = "Item C" }
            };
            _cartServiceMock.Setup(c => c.GetItems(1)).Returns(remainingItems);
            
            viewModel.GetType().GetMethod("UpdateCart", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.Invoke(viewModel, null);

            // Assert
            Assert.Equal(2, viewModel.Cart.Count);
            Assert.Equal(1, viewModel.Cart[0].SNo);
            Assert.Equal(2, viewModel.Cart[1].SNo); // Item C gets SNo 2
        }

        [Fact]
        public void UpdateCart_MaintainsSNoOrder_WhenItemsUpdated()
        {
            // Arrange
            var items = new List<OrderItem>
            {
                new OrderItem { ItemId = 1, ItemName = "Item A", Quantity = 1 },
                new OrderItem { ItemId = 2, ItemName = "Item B", Quantity = 1 }
            };
            _cartServiceMock.Setup(c => c.GetItems(1)).Returns(items);

            var viewModel = new BillingViewModel(
                _itemServiceMock.Object,
                _cartServiceMock.Object,
                _orderServiceMock.Object,
                _settingServiceMock.Object,
                _categoryServiceMock.Object,
                _notificationServiceMock.Object,
                _cashServiceMock.Object);

            viewModel.SelectTableCommand.Execute(1);

            // Act
            items[0].Quantity = 5;
            viewModel.GetType().GetMethod("UpdateCart", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.Invoke(viewModel, null);

            // Assert
            Assert.Equal(2, viewModel.Cart.Count);
            Assert.Equal(1, viewModel.Cart[0].SNo);
            Assert.Equal(5, viewModel.Cart[0].Quantity);
            Assert.Equal(2, viewModel.Cart[1].SNo);
        }

        [Fact]
        public void SelectedQty_UpdatesCartService_WhenChanged()
        {
            // Arrange
            var items = new List<OrderItem>
            {
                new OrderItem { ItemId = 1, ItemName = "Item A", Quantity = 1 }
            };
            _cartServiceMock.Setup(c => c.GetItems(1)).Returns(items);

            var viewModel = new BillingViewModel(
                _itemServiceMock.Object,
                _cartServiceMock.Object,
                _orderServiceMock.Object,
                _settingServiceMock.Object,
                _categoryServiceMock.Object,
                _notificationServiceMock.Object,
                _cashServiceMock.Object);

            viewModel.SelectTableCommand.Execute(1);
            viewModel.SelectedCartRow = viewModel.Cart[0];

            // Act - Simulate typing 10 in the Qty TextBox
            items[0].Quantity = 10; // Update the source list that the mock returns
            viewModel.SelectedQty = 10;

            // Assert
            _cartServiceMock.Verify(c => c.SetQuantity(1, 1, 10), Times.Once);
            Assert.Equal(10, viewModel.Cart[0].Quantity);
        }

        [Fact]
        public void UpdateRowCommand_SyncsChangesToService()
        {
            // Arrange
            var items = new List<OrderItem>
            {
                new OrderItem { ItemId = 1, ItemName = "Item A", Quantity = 1, Price = 100 }
            };
            _cartServiceMock.Setup(c => c.GetItems(1)).Returns(items);

            var viewModel = new BillingViewModel(
                _itemServiceMock.Object,
                _cartServiceMock.Object,
                _orderServiceMock.Object,
                _settingServiceMock.Object,
                _categoryServiceMock.Object,
                _notificationServiceMock.Object,
                _cashServiceMock.Object);

            viewModel.SelectTableCommand.Execute(1);
            var row = viewModel.Cart[0];

            // Act - Simulate user changing Qty to 20 and Price to 150 in the grid
            row.Quantity = 20;
            row.Price = 150;
            
            // Re-setup mock for the refresh call in UpdateRow
            _cartServiceMock.Setup(c => c.GetItems(1)).Returns(new List<OrderItem> {
                new OrderItem { ItemId = 1, ItemName = "Item A", Quantity = 20, Price = 150, Total = 3000 }
            });

            viewModel.UpdateRowCommand.Execute(row);

            // Assert
            _cartServiceMock.Verify(c => c.SetQuantity(1, 1, 20), Times.Once);
            _cartServiceMock.Verify(c => c.UpdatePrice(1, 1, 150), Times.Once);
            Assert.Equal(20, viewModel.Cart[0].Quantity);
            Assert.Equal(150, viewModel.Cart[0].Price);
        }
    }
}
