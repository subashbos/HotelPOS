using HotelPOS.Application.Interfaces;
using HotelPOS.Application.UseCases;
using HotelPOS.Application.UseCases.Users.Commands;
using HotelPOS.Domain.Entities;
using HotelPOS.Tests.TestHelpers;
using Moq;
using Xunit;

namespace HotelPOS.Tests;

public class ResetPasswordCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly Mock<IUserContext> _userContext = new();
    private readonly ResetPasswordCommandHandler _handler;

    public ResetPasswordCommandHandlerTests()
    {
        _handler = new ResetPasswordCommandHandler(_userRepo.Object, _userContext.Object);
    }

    private (string Hash, string Salt) MakeHash(string password) =>
        new AuthService(_userRepo.Object, new InMemoryLoginLockoutRepository()).HashPassword(password);

    [Fact]
    public async Task Handle_SelfServiceWithCorrectCurrentPassword_Succeeds()
    {
        var (hash, salt) = MakeHash("OldPassword1!");
        var user = new User { Id = 5, PasswordHash = hash, Salt = salt };
        _userRepo.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(user);
        _userContext.Setup(c => c.CurrentUserId).Returns(5);

        var (success, error) = await _handler.Handle(
            new ResetPasswordCommand(5, "NewPassword1!", "OldPassword1!"), CancellationToken.None);

        Assert.True(success, error);
        Assert.NotEqual(hash, user.PasswordHash);
        _userRepo.Verify(r => r.UpdateAsync(user), Times.Once);
    }

    [Fact]
    public async Task Handle_SelfServiceWithWrongCurrentPassword_FailsAndDoesNotUpdate()
    {
        var (hash, salt) = MakeHash("OldPassword1!");
        var user = new User { Id = 5, PasswordHash = hash, Salt = salt };
        _userRepo.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(user);
        _userContext.Setup(c => c.CurrentUserId).Returns(5);

        var (success, error) = await _handler.Handle(
            new ResetPasswordCommand(5, "NewPassword1!", "WrongPassword!"), CancellationToken.None);

        Assert.False(success);
        Assert.Equal("Current password is incorrect.", error);
        _userRepo.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task Handle_SelfServiceWithNoCurrentPassword_Fails()
    {
        var (hash, salt) = MakeHash("OldPassword1!");
        var user = new User { Id = 5, PasswordHash = hash, Salt = salt };
        _userRepo.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(user);
        _userContext.Setup(c => c.CurrentUserId).Returns(5);

        var (success, error) = await _handler.Handle(
            new ResetPasswordCommand(5, "NewPassword1!"), CancellationToken.None);

        Assert.False(success);
        Assert.Equal("Current password is incorrect.", error);
    }

    [Fact]
    public async Task Handle_AdminResetsAnotherUser_DoesNotRequireCurrentPassword()
    {
        var user = new User { Id = 6, PasswordHash = "some-hash", Salt = "some-salt" };
        _userRepo.Setup(r => r.GetByIdAsync(6)).ReturnsAsync(user);
        _userContext.Setup(c => c.CurrentUserId).Returns(1); // caller is a different user (admin)

        var (success, error) = await _handler.Handle(
            new ResetPasswordCommand(6, "NewPassword1!"), CancellationToken.None);

        Assert.True(success, error);
        _userRepo.Verify(r => r.UpdateAsync(user), Times.Once);
    }

    [Fact]
    public async Task Handle_UserNotFound_ReturnsError()
    {
        _userRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((User?)null);

        var (success, error) = await _handler.Handle(
            new ResetPasswordCommand(99, "NewPassword1!"), CancellationToken.None);

        Assert.False(success);
        Assert.Equal("User not found.", error);
    }
}
