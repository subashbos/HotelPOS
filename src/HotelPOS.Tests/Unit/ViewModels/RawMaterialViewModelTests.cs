using System.Collections.Generic;
using System.Threading.Tasks;
using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Entities;
using HotelPOS.ViewModels;
using Moq;
using Xunit;

namespace HotelPOS.Tests.Unit.ViewModels
{
    public class RawMaterialViewModelTests
    {
        private readonly Mock<IBomService> _mockBomService = new();
        private readonly Mock<IDialogService> _mockDialogService = new();
        private readonly RawMaterialViewModel _vm;

        public RawMaterialViewModelTests()
        {
            _vm = new RawMaterialViewModel(_mockBomService.Object, _mockDialogService.Object);
        }

        [Fact]
        public async Task LoadAsync_PopulatesRawMaterials()
        {
            _mockBomService.Setup(s => s.GetAllRawMaterialsAsync()).ReturnsAsync(new List<RawMaterial>
            {
                new RawMaterial { Id = 1, Name = "Chicken", Unit = "kg" },
                new RawMaterial { Id = 2, Name = "Onion", Unit = "kg" }
            });

            await _vm.LoadAsync();

            Assert.Equal(2, _vm.RawMaterials.Count);
        }

        [Fact]
        public async Task LoadAsync_WithSearchText_FiltersByName()
        {
            _mockBomService.Setup(s => s.GetAllRawMaterialsAsync()).ReturnsAsync(new List<RawMaterial>
            {
                new RawMaterial { Id = 1, Name = "Chicken", Unit = "kg" },
                new RawMaterial { Id = 2, Name = "Onion", Unit = "kg" }
            });

            _vm.SearchText = "chick";
            await _vm.LoadAsync();

            Assert.Single(_vm.RawMaterials);
            Assert.Equal("Chicken", _vm.RawMaterials[0].Name);
        }

        [Fact]
        public async Task SaveAsync_BlankName_ShowsValidationAndDoesNotSave()
        {
            _vm.Name = "   ";

            await _vm.SaveCommand.ExecuteAsync(null);

            _mockBomService.Verify(s => s.SaveRawMaterialAsync(It.IsAny<RawMaterial>()), Times.Never);
            _mockDialogService.Verify(d => d.ShowMessageAsync(
                It.Is<string>(msg => msg == "Name is required."), "Validation",
                DialogButton.OK, DialogIcon.Warning), Times.Once);
        }

        [Theory]
        [InlineData(-1, 0, 0)]
        [InlineData(0, -1, 0)]
        [InlineData(0, 0, -1)]
        public async Task SaveAsync_NegativeNumericFields_ShowsValidationAndDoesNotSave(decimal cost, decimal stock, decimal threshold)
        {
            _vm.Name = "Chicken";
            _vm.CostPerUnit = cost;
            _vm.CurrentStock = stock;
            _vm.MinStockThreshold = threshold;

            await _vm.SaveCommand.ExecuteAsync(null);

            _mockBomService.Verify(s => s.SaveRawMaterialAsync(It.IsAny<RawMaterial>()), Times.Never);
        }

        [Fact]
        public async Task SaveAsync_ValidData_SavesAndClearsForm()
        {
            _mockBomService.Setup(s => s.SaveRawMaterialAsync(It.IsAny<RawMaterial>()))
                .ReturnsAsync((RawMaterial r) => r);
            _mockBomService.Setup(s => s.GetAllRawMaterialsAsync()).ReturnsAsync(new List<RawMaterial>());

            _vm.Name = "Chicken";
            _vm.CostPerUnit = 250;
            _vm.CurrentStock = 10;

            await _vm.SaveCommand.ExecuteAsync(null);

            _mockBomService.Verify(s => s.SaveRawMaterialAsync(It.Is<RawMaterial>(r => r.Name == "Chicken" && r.CostPerUnit == 250)), Times.Once);
            Assert.Equal(string.Empty, _vm.Name);
        }

        [Fact]
        public async Task DeleteAsync_NoSelection_DoesNothing()
        {
            await _vm.DeleteCommand.ExecuteAsync(null);
            _mockBomService.Verify(s => s.DeleteRawMaterialAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task DeleteAsync_UserDeclinesConfirmation_DoesNotDelete()
        {
            _mockDialogService.Setup(d => d.ShowMessageAsync(It.IsAny<string>(), It.IsAny<string>(), DialogButton.YesNo, DialogIcon.Warning))
                .ReturnsAsync(DialogResult.No);

            _vm.SelectedRawMaterial = new RawMaterial { Id = 1, Name = "Chicken", Unit = "kg" };

            await _vm.DeleteCommand.ExecuteAsync(null);

            _mockBomService.Verify(s => s.DeleteRawMaterialAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task DeleteAsync_ServiceThrowsInvalidOperation_ShowsFriendlyMessage()
        {
            _mockDialogService.Setup(d => d.ShowMessageAsync(It.IsAny<string>(), It.IsAny<string>(), DialogButton.YesNo, DialogIcon.Warning))
                .ReturnsAsync(DialogResult.Yes);
            _mockBomService.Setup(s => s.DeleteRawMaterialAsync(1))
                .ThrowsAsync(new System.InvalidOperationException("Cannot delete a raw material that is used in a recipe."));

            _vm.SelectedRawMaterial = new RawMaterial { Id = 1, Name = "Chicken", Unit = "kg" };

            await _vm.DeleteCommand.ExecuteAsync(null);

            _mockDialogService.Verify(d => d.ShowMessageAsync(
                "Cannot delete a raw material that is used in a recipe.", "Cannot Delete",
                DialogButton.OK, DialogIcon.Warning), Times.Once);
        }
    }
}
