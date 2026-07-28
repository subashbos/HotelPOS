using AutoMapper;
using HotelPOS.Api.Controllers;
using HotelPOS.Application.DTOs.UnitOfMeasurement;
using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace HotelPOS.Tests.Unit.Controllers
{
    public class UnitOfMeasurementsControllerTests
    {
        private static readonly IMapper Mapper = new MapperConfiguration(
            cfg => cfg.AddProfile(new HotelPOS.Application.Common.Mappings.MappingProfile()),
            Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance).CreateMapper();

        private readonly Mock<IUnitOfMeasurementService> _unitService = new();
        private readonly UnitOfMeasurementsController _controller;

        public UnitOfMeasurementsControllerTests()
        {
            _controller = new UnitOfMeasurementsController(_unitService.Object, Mapper);
        }

        [Fact]
        public async Task GetUnitOfMeasurements_ReturnsMappedDtos()
        {
            _unitService.Setup(s => s.GetUnitsAsync()).ReturnsAsync(new List<UnitOfMeasurement>
            {
                new() { Id = 1, Name = "Kilogram", DisplayOrder = 1 }
            });

            var result = await _controller.GetUnitOfMeasurements();

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var dtos = Assert.IsAssignableFrom<IEnumerable<UnitOfMeasurementDto>>(ok.Value);
            Assert.Single(dtos);
        }

        [Fact]
        public async Task CreateUnitOfMeasurement_InvalidModelState_ReturnsBadRequest()
        {
            _controller.ModelState.AddModelError("Name", "required");

            var result = await _controller.CreateUnitOfMeasurement(new SaveUnitOfMeasurementDto());

            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task CreateUnitOfMeasurement_DuplicateName_ReturnsConflict()
        {
            _unitService.Setup(s => s.AddUnitAsync(It.IsAny<string>(), It.IsAny<int>()))
                .ThrowsAsync(new InvalidOperationException("Unit 'Kilogram' already exists."));

            var result = await _controller.CreateUnitOfMeasurement(new SaveUnitOfMeasurementDto { Name = "Kilogram" });

            var conflict = Assert.IsType<ConflictObjectResult>(result.Result);
            Assert.Equal("Unit 'Kilogram' already exists.", conflict.Value);
        }

        [Fact]
        public async Task CreateUnitOfMeasurement_ServiceThrowsArgumentException_ReturnsBadRequest()
        {
            _unitService.Setup(s => s.AddUnitAsync(It.IsAny<string>(), It.IsAny<int>()))
                .ThrowsAsync(new ArgumentException("Unit name is required."));

            var result = await _controller.CreateUnitOfMeasurement(new SaveUnitOfMeasurementDto { Name = "x" });

            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task CreateUnitOfMeasurement_Valid_ReturnsCreatedWithId()
        {
            _unitService.Setup(s => s.AddUnitAsync("Litre", 2)).ReturnsAsync(9);

            var result = await _controller.CreateUnitOfMeasurement(new SaveUnitOfMeasurementDto { Name = "Litre", DisplayOrder = 2 });

            var created = Assert.IsType<CreatedAtActionResult>(result.Result);
            var dto = Assert.IsType<UnitOfMeasurementDto>(created.Value);
            Assert.Equal(9, dto.Id);
        }

        [Fact]
        public async Task UpdateUnitOfMeasurement_InvalidId_ReturnsBadRequest()
        {
            var result = await _controller.UpdateUnitOfMeasurement(0, new SaveUnitOfMeasurementDto { Name = "x" });

            Assert.IsType<BadRequestObjectResult>(result);
            _unitService.Verify(s => s.UpdateUnitAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task UpdateUnitOfMeasurement_NotFound_ReturnsNotFound()
        {
            _unitService.Setup(s => s.UpdateUnitAsync(99, It.IsAny<string>(), It.IsAny<int>()))
                .ThrowsAsync(new KeyNotFoundException());

            var result = await _controller.UpdateUnitOfMeasurement(99, new SaveUnitOfMeasurementDto { Name = "x" });

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task UpdateUnitOfMeasurement_DuplicateName_ReturnsConflict()
        {
            _unitService.Setup(s => s.UpdateUnitAsync(1, It.IsAny<string>(), It.IsAny<int>()))
                .ThrowsAsync(new InvalidOperationException("Unit 'Kilogram' already exists."));

            var result = await _controller.UpdateUnitOfMeasurement(1, new SaveUnitOfMeasurementDto { Name = "Kilogram" });

            Assert.IsType<ConflictObjectResult>(result);
        }

        [Fact]
        public async Task UpdateUnitOfMeasurement_Valid_ReturnsNoContent()
        {
            var result = await _controller.UpdateUnitOfMeasurement(1, new SaveUnitOfMeasurementDto { Name = "Kilogram", DisplayOrder = 1 });

            Assert.IsType<NoContentResult>(result);
            _unitService.Verify(s => s.UpdateUnitAsync(1, "Kilogram", 1), Times.Once);
        }

        [Fact]
        public async Task DeleteUnitOfMeasurement_InvalidId_ReturnsBadRequest()
        {
            var result = await _controller.DeleteUnitOfMeasurement(0);

            Assert.IsType<BadRequestObjectResult>(result);
            _unitService.Verify(s => s.DeleteUnitAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task DeleteUnitOfMeasurement_InUse_ReturnsConflict()
        {
            _unitService.Setup(s => s.DeleteUnitAsync(5))
                .ThrowsAsync(new InvalidOperationException("Cannot delete unit because it is used by existing menu items. Please reassign the items first."));

            var result = await _controller.DeleteUnitOfMeasurement(5);

            Assert.IsType<ConflictObjectResult>(result);
        }

        [Fact]
        public async Task DeleteUnitOfMeasurement_Valid_ReturnsNoContent()
        {
            var result = await _controller.DeleteUnitOfMeasurement(5);

            Assert.IsType<NoContentResult>(result);
            _unitService.Verify(s => s.DeleteUnitAsync(5), Times.Once);
        }
    }
}
