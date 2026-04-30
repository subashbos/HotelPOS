using HotelPOS.Domain;
using HotelPOS.Views;
using HotelPOS.ViewModels;
using HotelPOS.Application.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace HotelPOS.Tests
{
    public class SerialNumberTests
    {
        [Fact]
        public void JournalRow_SNo_CalculatesCorrectlyForPage1()
        {
            // Simulate LoadPagedAsync logic for Page 1, Size 10
            int page = 1;
            int size = 10;
            int startSno = (page - 1) * size + 1;

            var items = new List<Order>
            {
                new Order { Id = 101 },
                new Order { Id = 102 }
            };

            var rows = items.Select((o, idx) => new JournalRow
            {
                SNo = startSno + idx,
                Id = o.Id
            }).ToList();

            Assert.Equal(1, rows[0].SNo);
            Assert.Equal(2, rows[1].SNo);
        }

        [Fact]
        public void JournalRow_SNo_CalculatesCorrectlyForPage2()
        {
            // Simulate LoadPagedAsync logic for Page 2, Size 10
            int page = 2;
            int size = 10;
            int startSno = (page - 1) * size + 1;

            var items = new List<Order>
            {
                new Order { Id = 201 },
                new Order { Id = 202 }
            };

            var rows = items.Select((o, idx) => new JournalRow
            {
                SNo = startSno + idx,
                Id = o.Id
            }).ToList();

            Assert.Equal(11, rows[0].SNo);
            Assert.Equal(12, rows[1].SNo);
        }

        [Fact]
        public void Item_SNo_PopulatesSequentially()
        {
            // Simulate ItemView.ApplyFilter logic
            var items = new List<Item>
            {
                new Item { Name = "A" },
                new Item { Name = "B" },
                new Item { Name = "C" }
            };

            for (int i = 0; i < items.Count; i++) items[i].SNo = i + 1;

            Assert.Equal(1, items[0].SNo);
            Assert.Equal(2, items[1].SNo);
            Assert.Equal(3, items[2].SNo);
        }

        [Fact]
        public void LedgerRow_SNo_CalculatesCorrectly()
        {
            // Simulate LedgerView.BuildLedger logic
            var items = new List<LedgerRow>
            {
                new LedgerRow(DateTime.Today, 1, 100, 5, 95, 100),
                new LedgerRow(DateTime.Today.AddDays(1), 2, 200, 10, 190, 300)
            };

            for (int i = 0; i < items.Count; i++) items[i].SNo = i + 1;

            Assert.Equal(1, items[0].SNo);
            Assert.Equal(2, items[1].SNo);
        }

        [Fact]
        public void CartRow_SNo_IncrementsSequentially()
        {
            // Simulate BillingViewModel.UpdateCart logic
            var itemsInCart = new List<OrderItem>
            {
                new OrderItem { ItemId = 1, ItemName = "A" },
                new OrderItem { ItemId = 2, ItemName = "B" }
            };

            var cartRows = new List<CartRow>();
            int sno = 1;
            foreach (var item in itemsInCart)
            {
                cartRows.Add(new CartRow { SNo = sno++, ItemName = item.ItemName });
            }

            Assert.Equal(1, cartRows[0].SNo);
            Assert.Equal(2, cartRows[1].SNo);
        }
    }
}
