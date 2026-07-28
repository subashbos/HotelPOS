using HotelPOS.Application.Interfaces;
using HotelPOS.Application.UseCases.Suppliers.Commands;
using HotelPOS.Domain.Entities;
using Moq;
using Xunit;

namespace HotelPOS.Tests.Unit.Commands
{
    public class DeleteSupplierCommandHandlerTests
    {
        private readonly Mock<ISupplierRepository> _repo = new();
        private readonly DeleteSupplierCommandHandler _handler;

        public DeleteSupplierCommandHandlerTests()
        {
            _handler = new DeleteSupplierCommandHandler(_repo.Object);
        }

        [Fact]
        public async Task Handle_ExistingSupplier_Deletes()
        {
            _repo.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(new Supplier { Id = 5, Name = "Metro" });

            await _handler.Handle(new DeleteSupplierCommand(5), CancellationToken.None);

            _repo.Verify(r => r.DeleteAsync(5), Times.Once);
        }

        [Fact]
        public async Task Handle_SupplierNotFound_ThrowsAndDoesNotDelete()
        {
            _repo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Supplier?)null);

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _handler.Handle(new DeleteSupplierCommand(99), CancellationToken.None));

            _repo.Verify(r => r.DeleteAsync(It.IsAny<int>()), Times.Never);
        }
    }
}
