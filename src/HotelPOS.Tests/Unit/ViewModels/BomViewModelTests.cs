using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Entities;
using HotelPOS.ViewModels;
using Moq;
using Xunit;

namespace HotelPOS.Tests.Unit.ViewModels
{
    public class BomViewModelTests
    {
        private readonly Mock<IBomService> _mockBomService = new();
        private readonly Mock<IItemService> _mockItemService = new();
        private readonly Mock<IDialogService> _mockDialogService = new();
        private readonly BomViewModel _vm;

        public BomViewModelTests()
        {
            _vm = new BomViewModel(_mockBomService.Object, _mockItemService.Object, _mockDialogService.Object);
        }

        [Fact]
        public async Task LoadAsync_PopulatesMenuItemsAndRawMaterials()
        {
            _mockItemService.Setup(s => s.GetItemsAsync()).ReturnsAsync(new List<Item>
            {
                new Item { Id = 1, Name = "Chicken Curry", Price = 200 },
                new Item { Id = 2, Name = "Fried Rice", Price = 150 }
            });
            _mockBomService.Setup(s => s.GetAllRawMaterialsAsync()).ReturnsAsync(new List<RawMaterial>
            {
                new RawMaterial { Id = 1, Name = "Chicken", Unit = "kg" }
            });

            await _vm.LoadAsync();

            Assert.Equal(2, _vm.MenuItems.Count);
            Assert.Single(_vm.AllRawMaterials);
        }

        [Fact]
        public async Task ItemSearchText_FiltersMenuItemsWithoutReloadingFromService()
        {
            _mockItemService.Setup(s => s.GetItemsAsync()).ReturnsAsync(new List<Item>
            {
                new Item { Id = 1, Name = "Chicken Curry", Price = 200 },
                new Item { Id = 2, Name = "Fried Rice", Price = 150 }
            });
            _mockBomService.Setup(s => s.GetAllRawMaterialsAsync()).ReturnsAsync(new List<RawMaterial>());

            await _vm.LoadAsync();
            _vm.ItemSearchText = "chicken";

            Assert.Single(_vm.MenuItems);
            Assert.Equal("Chicken Curry", _vm.MenuItems[0].Name);
            _mockItemService.Verify(s => s.GetItemsAsync(), Times.Once);
        }

        [Fact]
        public void AddIngredient_NoSelectedItem_DoesNothing()
        {
            _vm.AddIngredientCommand.Execute(null);
            Assert.Empty(_vm.BomRows);
        }

        [Fact]
        public async Task AddIngredient_WithSelectedItem_AddsRow()
        {
            _mockItemService.Setup(s => s.GetItemsAsync()).ReturnsAsync(new List<Item>());
            _mockBomService.Setup(s => s.GetAllRawMaterialsAsync()).ReturnsAsync(new List<RawMaterial>());
            _mockBomService.Setup(s => s.GetBomForItemAsync(It.IsAny<int>())).ReturnsAsync(new List<BomEntry>());

            _vm.SelectedItem = new Item { Id = 1, Name = "Chicken Curry", Price = 200 };
            await Task.Yield();

            _vm.AddIngredientCommand.Execute(null);

            Assert.Single(_vm.BomRows);
        }

        [Fact]
        public async Task SaveBomAsync_DuplicateRawMaterialRows_ShowsWarningAndDoesNotSave()
        {
            _mockItemService.Setup(s => s.GetItemsAsync()).ReturnsAsync(new List<Item>());
            _mockBomService.Setup(s => s.GetAllRawMaterialsAsync()).ReturnsAsync(new List<RawMaterial>());
            _mockBomService.Setup(s => s.GetBomForItemAsync(It.IsAny<int>())).ReturnsAsync(new List<BomEntry>());

            _vm.SelectedItem = new Item { Id = 1, Name = "Chicken Curry", Price = 200 };
            await Task.Yield();

            _vm.BomRows.Add(new BomEntryRow { RawMaterialId = 5, RawMaterialName = "Chicken", QuantityRequired = 1 });
            _vm.BomRows.Add(new BomEntryRow { RawMaterialId = 5, RawMaterialName = "Chicken", QuantityRequired = 2 });

            await _vm.SaveBomCommand.ExecuteAsync(null);

            _mockBomService.Verify(s => s.SaveBomAsync(It.IsAny<int>(), It.IsAny<List<BomEntry>>()), Times.Never);
            _mockDialogService.Verify(d => d.ShowMessageAsync(
                It.IsAny<string>(), It.Is<string>(title => title == "Duplicate Ingredient"),
                DialogButton.OK, DialogIcon.Warning), Times.Once);
        }

        [Fact]
        public async Task SaveBomAsync_UniqueRows_SavesAndReloads()
        {
            _mockItemService.Setup(s => s.GetItemsAsync()).ReturnsAsync(new List<Item>());
            _mockBomService.Setup(s => s.GetAllRawMaterialsAsync()).ReturnsAsync(new List<RawMaterial>());
            _mockBomService.Setup(s => s.GetBomForItemAsync(It.IsAny<int>())).ReturnsAsync(new List<BomEntry>());
            _mockDialogService.Setup(d => d.ShowMessageAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DialogButton>(), It.IsAny<DialogIcon>()))
                .ReturnsAsync(DialogResult.OK);

            _vm.SelectedItem = new Item { Id = 1, Name = "Chicken Curry", Price = 200 };
            await Task.Yield();

            _vm.BomRows.Add(new BomEntryRow { RawMaterialId = 5, RawMaterialName = "Chicken", QuantityRequired = 1 });
            _vm.BomRows.Add(new BomEntryRow { RawMaterialId = 6, RawMaterialName = "Onion", QuantityRequired = 0.5m });

            await _vm.SaveBomCommand.ExecuteAsync(null);

            _mockBomService.Verify(s => s.SaveBomAsync(1, It.Is<List<BomEntry>>(l => l.Count == 2)), Times.Once);
        }

        [Fact]
        public async Task RecalculateCosts_ComputesGrossMarginFromRows()
        {
            _mockItemService.Setup(s => s.GetItemsAsync()).ReturnsAsync(new List<Item>());
            _mockBomService.Setup(s => s.GetAllRawMaterialsAsync()).ReturnsAsync(new List<RawMaterial>());
            _mockBomService.Setup(s => s.GetBomForItemAsync(It.IsAny<int>())).ReturnsAsync(new List<BomEntry>());

            _vm.SelectedItem = new Item { Id = 1, Name = "Chicken Curry", Price = 200 };
            await Task.Yield();

            _vm.AddIngredientCommand.Execute(null);
            var row = _vm.BomRows[0];
            row.CostPerUnit = 100;
            row.WastagePercentage = 10;
            row.QuantityRequired = 1; // effective 1.1 * 100 = 110

            Assert.Equal(110m, _vm.TotalFoodCost);
            Assert.Equal(10m, _vm.TotalWastageCost);
            Assert.Equal(45.0m, _vm.GrossMargin); // (200-110)/200*100
        }
    }
}
