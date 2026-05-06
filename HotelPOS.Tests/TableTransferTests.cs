using HotelPOS.Application;
using HotelPOS.Domain;
using Xunit;
using System.Collections.Generic;
using System.Linq;

namespace HotelPOS.Tests
{
    public class TableTransferTests
    {
        [Fact]
        public void TransferTable_MovesItemsToEmptyTable()
        {
            // Arrange
            var service = new CartService();
            service.AddItem(1, new Item { Id = 101, Name = "Item 101", Price = 100 });
            service.AddItem(1, new Item { Id = 101, Name = "Item 101", Price = 100 }); // qty 2
            service.AddItem(1, new Item { Id = 102, Name = "Item 102", Price = 200 });

            // Act
            service.TransferTable(1, 5);

            // Assert
            var table1 = service.GetItems(1);
            var table5 = service.GetItems(5);

            Assert.Empty(table1);
            Assert.Equal(2, table5.Count);
            Assert.Equal(2, table5.First(i => i.ItemId == 101).Quantity);
            Assert.Equal(1, table5.First(i => i.ItemId == 102).Quantity);
        }

        [Fact]
        public void TransferTable_MergesItemsWithOccupiedTable()
        {
            // Arrange
            var service = new CartService();
            service.AddItem(1, new Item { Id = 101, Name = "A", Price = 10 });
            service.AddItem(1, new Item { Id = 101, Name = "A", Price = 10 });

            service.AddItem(5, new Item { Id = 101, Name = "A", Price = 10 });
            service.AddItem(5, new Item { Id = 101, Name = "A", Price = 10 });
            service.AddItem(5, new Item { Id = 101, Name = "A", Price = 10 });
            service.AddItem(5, new Item { Id = 103, Name = "C", Price = 30 });

            // Act
            service.TransferTable(1, 5);

            // Assert
            var table5 = service.GetItems(5);
            Assert.Equal(2, table5.Count); // Item 101 and 103
            Assert.Equal(5, table5.First(i => i.ItemId == 101).Quantity); // 2 + 3
            Assert.Equal(1, table5.First(i => i.ItemId == 103).Quantity);
        }

        [Fact]
        public void TransferTable_DoesNothing_WhenSourceIsEmpty()
        {
            // Arrange
            var service = new CartService();
            service.AddItem(5, new Item { Id = 101, Name = "A", Price = 10 });

            // Act
            service.TransferTable(1, 5);

            // Assert
            var table5 = service.GetItems(5);
            Assert.Single(table5);
            Assert.Equal(1, table5[0].Quantity);
        }

        [Fact]
        public void TransferTable_DoesNothing_WhenSourceIsTarget()
        {
            // Arrange
            var service = new CartService();
            service.AddItem(1, new Item { Id = 101, Name = "A", Price = 10 });

            // Act
            service.TransferTable(1, 1);

            // Assert
            var table1 = service.GetItems(1);
            Assert.Single(table1);
            Assert.Equal(1, table1[0].Quantity);
        }

        [Fact]
        public void GetActiveTables_IncludesTablesWithItemsAndHeldOrders()
        {
            // Arrange
            var service = new CartService();
            service.AddItem(1, new Item { Id = 101, Name = "A", Price = 10 });
            service.AddItem(3, new Item { Id = 102, Name = "B", Price = 20 });
            
            // Hold table 3
            service.HoldOrder(3, "Guest A");
            
            // Add items to table 5
            service.AddItem(5, new Item { Id = 103, Name = "C", Price = 30 });

            // Act
            var active = service.GetActiveTables();

            // Assert
            Assert.Equal(3, active.Count);
            Assert.Contains(1, active);
            Assert.Contains(3, active);
            Assert.Contains(5, active);
        }
    }
}
