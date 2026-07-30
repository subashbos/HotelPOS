using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Entities;
using HotelPOS.ViewModels;
using Moq;
using Xunit;

namespace HotelPOS.Tests.Unit.ViewModels
{
    public class AccountViewModelTests
    {
        private readonly Mock<IUserService> _userServiceMock = new();
        private readonly Mock<INotificationService> _notificationServiceMock = new();
        private readonly AccountViewModel _viewModel;

        public AccountViewModelTests()
        {
            _viewModel = new AccountViewModel(_userServiceMock.Object, _notificationServiceMock.Object);
        }

        [Fact]
        public void Initialize_LoadsUserDataFromAppSession()
        {
            AppSession.CurrentUser = new User
            {
                Username = "testuser",
                Role = "Admin",
                Email = "test@example.com",
                TwoFactorEnabled = true
            };

            _viewModel.Initialize();

            Assert.Equal("testuser", _viewModel.Username);
            Assert.Equal("Admin", _viewModel.Role);
            Assert.Equal("test@example.com", _viewModel.Email);
            Assert.True(_viewModel.IsTwoFactorEnabled);
        }

        [Fact]
        public async Task UpdateEmailAsync_Success_UpdatesSessionAndShowsSuccess()
        {
            AppSession.CurrentUser = new User { Id = 1, Email = "old@example.com" };
            _viewModel.Email = "new@example.com";

            await _viewModel.UpdateEmailAsync();

            _userServiceMock.Verify(s => s.SetEmailAsync(1, "new@example.com"), Times.Once);
            Assert.Equal("new@example.com", AppSession.CurrentUser.Email);
            _notificationServiceMock.Verify(n => n.ShowSuccess(It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task UpdateEmailAsync_Error_ShowsErrorNotification()
        {
            AppSession.CurrentUser = new User { Id = 1 };
            _viewModel.Email = "new@example.com";
            _userServiceMock.Setup(s => s.SetEmailAsync(1, "new@example.com")).ThrowsAsync(new System.Exception("DB error"));

            await _viewModel.UpdateEmailAsync();

            _notificationServiceMock.Verify(n => n.ShowError(It.Is<string>(s => s.Contains("DB error"))), Times.Once);
        }

        [Fact]
        public async Task ChangePasswordAsync_MissingCurrentPassword_ShowsError()
        {
            AppSession.CurrentUser = new User { Id = 1 };
            _viewModel.CurrentPassword = "";

            await _viewModel.ChangePasswordAsync();

            _notificationServiceMock.Verify(n => n.ShowError("Please enter your current password."), Times.Once);
        }

        [Fact]
        public async Task ChangePasswordAsync_ShortNewPassword_ShowsError()
        {
            AppSession.CurrentUser = new User { Id = 1 };
            _viewModel.CurrentPassword = "Password123";
            _viewModel.NewPassword = "123";

            await _viewModel.ChangePasswordAsync();

            _notificationServiceMock.Verify(n => n.ShowError(It.Is<string>(s => s.Contains("at least 6 characters"))), Times.Once);
        }

        [Fact]
        public async Task ChangePasswordAsync_PasswordMismatch_ShowsError()
        {
            AppSession.CurrentUser = new User { Id = 1 };
            _viewModel.CurrentPassword = "OldPassword1";
            _viewModel.NewPassword = "NewPassword1";
            _viewModel.ConfirmPassword = "NewPassword2";

            await _viewModel.ChangePasswordAsync();

            _notificationServiceMock.Verify(n => n.ShowError("New password and confirm password do not match."), Times.Once);
        }

        [Fact]
        public async Task ChangePasswordAsync_Success_ClearsInputsAndShowsSuccess()
        {
            AppSession.CurrentUser = new User { Id = 1 };
            _viewModel.CurrentPassword = "OldPassword1";
            _viewModel.NewPassword = "NewPassword123";
            _viewModel.ConfirmPassword = "NewPassword123";

            _userServiceMock.Setup(s => s.ResetPasswordAsync(1, "NewPassword123", "OldPassword1"))
                .ReturnsAsync((true, string.Empty));

            await _viewModel.ChangePasswordAsync();

            Assert.Equal(string.Empty, _viewModel.CurrentPassword);
            Assert.Equal(string.Empty, _viewModel.NewPassword);
            Assert.Equal(string.Empty, _viewModel.ConfirmPassword);
            _notificationServiceMock.Verify(n => n.ShowSuccess("Password changed successfully."), Times.Once);
        }

        [Fact]
        public async Task ToggleTwoFactorAsync_Success_TogglesStateAndShowsNotification()
        {
            AppSession.CurrentUser = new User { Id = 1, TwoFactorEnabled = false };
            _viewModel.IsTwoFactorEnabled = false;

            await _viewModel.ToggleTwoFactorAsync();

            _userServiceMock.Verify(s => s.SetTwoFactorAsync(1, true, null), Times.Once);
            Assert.True(_viewModel.IsTwoFactorEnabled);
            Assert.True(AppSession.CurrentUser.TwoFactorEnabled);
            _notificationServiceMock.Verify(n => n.ShowSuccess(It.Is<string>(s => s.Contains("Enabled"))), Times.Once);
        }
    }
}
