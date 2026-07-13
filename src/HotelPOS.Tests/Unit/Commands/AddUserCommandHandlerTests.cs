using AutoMapper;
using HotelPOS.Application.Common.Mappings;
using HotelPOS.Application.Interfaces;
using HotelPOS.Application.UseCases.Users.Commands;
using HotelPOS.Domain.Entities;
using Moq;
using Xunit;

namespace HotelPOS.Tests.Unit.Commands
{
    public class AddUserCommandHandlerTests
    {
        private readonly Mock<IUserRepository> _userRepo = new();
        private readonly AddUserCommandHandler _handler;

        public AddUserCommandHandlerTests()
        {
            var mapper = new MapperConfiguration(
                cfg => cfg.AddProfile(new MappingProfile()),
                Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance).CreateMapper();
            _handler = new AddUserCommandHandler(_userRepo.Object, mapper);
        }

        [Fact]
        public async Task Handle_ExistingUsername_ReturnsError()
        {
            _userRepo.Setup(r => r.GetUserByUsernameAsync("admin")).ReturnsAsync(new User { Username = "admin" });

            var (success, error) = await _handler.Handle(
                new AddUserCommand("admin", "Sup3rSecret!x", "Admin", 1), CancellationToken.None);

            Assert.False(success);
            Assert.Contains("already exists", error);
            _userRepo.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Never);
        }

        [Fact]
        public async Task Handle_NewUsername_HashesPasswordAndAdds()
        {
            User? added = null;
            _userRepo.Setup(r => r.GetUserByUsernameAsync(It.IsAny<string>())).ReturnsAsync((User?)null);
            _userRepo.Setup(r => r.AddAsync(It.IsAny<User>()))
                .Callback<User>(u => added = u)
                .Returns(Task.CompletedTask);

            var (success, error) = await _handler.Handle(
                new AddUserCommand("newcashier", "Sup3rSecret!x", "Cashier", 2), CancellationToken.None);

            Assert.True(success);
            Assert.Equal(string.Empty, error);
            Assert.NotNull(added);
            Assert.False(string.IsNullOrEmpty(added!.PasswordHash));
            Assert.False(string.IsNullOrEmpty(added.Salt));
            // Never store the raw password.
            Assert.NotEqual("Sup3rSecret!x", added.PasswordHash);
        }

        [Fact]
        public async Task Handle_TrimsUsernameForDuplicateLookup()
        {
            _userRepo.Setup(r => r.GetUserByUsernameAsync(It.IsAny<string>())).ReturnsAsync((User?)null);

            await _handler.Handle(new AddUserCommand("  spaced  ", "Sup3rSecret!x", "Cashier", 2), CancellationToken.None);

            _userRepo.Verify(r => r.GetUserByUsernameAsync("spaced"), Times.Once);
        }

        [Fact]
        public async Task Handle_SaltsAreUniquePerUser()
        {
            var salts = new List<string>();
            _userRepo.Setup(r => r.GetUserByUsernameAsync(It.IsAny<string>())).ReturnsAsync((User?)null);
            _userRepo.Setup(r => r.AddAsync(It.IsAny<User>()))
                .Callback<User>(u => salts.Add(u.Salt))
                .Returns(Task.CompletedTask);

            await _handler.Handle(new AddUserCommand("userA", "Sup3rSecret!x", "Cashier", 2), CancellationToken.None);
            await _handler.Handle(new AddUserCommand("userB", "Sup3rSecret!x", "Cashier", 2), CancellationToken.None);

            Assert.Equal(2, salts.Count);
            Assert.NotEqual(salts[0], salts[1]);
        }
    }
}
