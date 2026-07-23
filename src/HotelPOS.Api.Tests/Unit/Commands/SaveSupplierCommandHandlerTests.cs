using HotelPOS.Application.DTOs.Supplier;
using HotelPOS.Application.Interfaces;
using HotelPOS.Application.UseCases.Suppliers.Commands;
using HotelPOS.Domain.Entities;
using Moq;
using Xunit;

namespace HotelPOS.Tests.Unit.Commands
{
    public class SaveSupplierCommandHandlerTests
    {
        private readonly Mock<ISupplierRepository> _repo = new();
        private readonly SaveSupplierCommandHandler _handler;

        public SaveSupplierCommandHandlerTests()
        {
            _handler = new SaveSupplierCommandHandler(_repo.Object);
        }

        private static SaveSupplierDto NewSupplierDto() => new()
        {
            Id = 0,
            Name = "  Fresh Farms  ",
            ContactPerson = " Ravi ",
            Phone = "+91 (44) 1234-5678 ext9",
            Email = " sales@freshfarms.example ",
            Gstin = " 33aaaaa0000a1z5 ",
            City = " Chennai ",
            State = " TN ",
            Pincode = " 600001 ",
            OpeningBalance = 1500m,
            CreditLimit = 50000m,
            PaymentTerms = " Net 30 "
        };

        [Fact]
        public async Task Handle_DuplicateName_Throws()
        {
            _repo.Setup(r => r.ExistsByNameAsync("Fresh Farms", 0)).ReturnsAsync(true);

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _handler.Handle(new SaveSupplierCommand(NewSupplierDto()), CancellationToken.None));

            _repo.Verify(r => r.AddAsync(It.IsAny<Supplier>()), Times.Never);
        }

        [Fact]
        public async Task Handle_NewSupplier_TrimsSanitizesAndAdds()
        {
            Supplier? added = null;
            _repo.Setup(r => r.ExistsByNameAsync(It.IsAny<string>(), 0)).ReturnsAsync(false);
            _repo.Setup(r => r.AddAsync(It.IsAny<Supplier>()))
                .Callback<Supplier>(s => { s.Id = 42; added = s; })
                .Returns(Task.CompletedTask);

            var id = await _handler.Handle(new SaveSupplierCommand(NewSupplierDto()), CancellationToken.None);

            Assert.Equal(42, id);
            Assert.NotNull(added);
            Assert.Equal("Fresh Farms", added!.Name);
            Assert.Equal("Ravi", added.ContactPerson);
            // Letters are stripped from the phone; digits, +, -, (), spaces survive.
            Assert.Equal("+91 (44) 1234-5678 9", added.Phone);
            Assert.Equal("sales@freshfarms.example", added.Email);
            Assert.Equal("33AAAAA0000A1Z5", added.Gstin);
            Assert.Equal("Net 30", added.PaymentTerms);
            Assert.Equal(1500m, added.OpeningBalance);
            Assert.Equal(50000m, added.CreditLimit);
        }

        [Fact]
        public async Task Handle_NewSupplier_NullOptionalsStayNull()
        {
            Supplier? added = null;
            _repo.Setup(r => r.ExistsByNameAsync(It.IsAny<string>(), 0)).ReturnsAsync(false);
            _repo.Setup(r => r.AddAsync(It.IsAny<Supplier>()))
                .Callback<Supplier>(s => added = s)
                .Returns(Task.CompletedTask);

            await _handler.Handle(new SaveSupplierCommand(new SaveSupplierDto { Name = "Solo" }), CancellationToken.None);

            Assert.NotNull(added);
            Assert.Null(added!.Phone);
            Assert.Null(added.ContactPerson);
            Assert.Null(added.Gstin);
        }

        [Fact]
        public async Task Handle_UpdateMissingSupplier_ThrowsKeyNotFound()
        {
            _repo.Setup(r => r.ExistsByNameAsync(It.IsAny<string>(), 9)).ReturnsAsync(false);
            _repo.Setup(r => r.GetByIdAsync(9)).ReturnsAsync((Supplier?)null);

            var dto = NewSupplierDto();
            dto.Id = 9;

            await Assert.ThrowsAsync<KeyNotFoundException>(
                () => _handler.Handle(new SaveSupplierCommand(dto), CancellationToken.None));
        }

        [Fact]
        public async Task Handle_UpdateExistingSupplier_OverwritesFieldsAndUpdates()
        {
            var existing = new Supplier { Id = 9, Name = "Old Name", Phone = "000" };
            _repo.Setup(r => r.ExistsByNameAsync(It.IsAny<string>(), 9)).ReturnsAsync(false);
            _repo.Setup(r => r.GetByIdAsync(9)).ReturnsAsync(existing);

            var dto = NewSupplierDto();
            dto.Id = 9;

            var id = await _handler.Handle(new SaveSupplierCommand(dto), CancellationToken.None);

            Assert.Equal(9, id);
            Assert.Equal("Fresh Farms", existing.Name);
            Assert.Equal("+91 (44) 1234-5678 9", existing.Phone);
            Assert.Equal("33AAAAA0000A1Z5", existing.Gstin);
            _repo.Verify(r => r.UpdateAsync(existing), Times.Once);
            _repo.Verify(r => r.AddAsync(It.IsAny<Supplier>()), Times.Never);
        }
    }
}
