using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Entities;

namespace HotelPOS.ViewModels
{
    public partial class AccountViewModel : ObservableObject
    {
        private readonly IUserService _userService;
        private readonly INotificationService _notificationService;

        [ObservableProperty]
        private string _username = string.Empty;

        [ObservableProperty]
        private string _role = string.Empty;

        [ObservableProperty]
        private string _email = string.Empty;

        [ObservableProperty]
        private bool _isTwoFactorEnabled;

        [ObservableProperty]
        private string _currentPassword = string.Empty;

        [ObservableProperty]
        private string _newPassword = string.Empty;

        [ObservableProperty]
        private string _confirmPassword = string.Empty;

        public AccountViewModel(IUserService userService, INotificationService notificationService)
        {
            _userService = userService;
            _notificationService = notificationService;
        }

        public void Initialize()
        {
            LoadUserData();
        }

        public void LoadUserData()
        {
            var user = AppSession.CurrentUser;
            if (user != null)
            {
                Username = user.Username;
                Role = user.Role;
                Email = user.Email ?? string.Empty;
                IsTwoFactorEnabled = user.TwoFactorEnabled;
            }
        }

        [RelayCommand]
        public async Task UpdateEmailAsync()
        {
            if (AppSession.CurrentUser == null) return;

            try
            {
                await _userService.SetEmailAsync(AppSession.CurrentUser.Id, string.IsNullOrWhiteSpace(Email) ? null : Email);
                AppSession.CurrentUser.Email = Email;
                _notificationService.ShowSuccess("Email address updated successfully.");
            }
            catch (Exception ex)
            {
                _notificationService.ShowError($"Failed to update email: {ex.Message}");
            }
        }

        [RelayCommand]
        public async Task ChangePasswordAsync()
        {
            if (AppSession.CurrentUser == null) return;

            if (string.IsNullOrWhiteSpace(CurrentPassword))
            {
                _notificationService.ShowError("Please enter your current password.");
                return;
            }

            if (string.IsNullOrWhiteSpace(NewPassword) || NewPassword.Length < 6)
            {
                _notificationService.ShowError("New password must be at least 6 characters long.");
                return;
            }

            if (NewPassword != ConfirmPassword)
            {
                _notificationService.ShowError("New password and confirm password do not match.");
                return;
            }

            try
            {
                var result = await _userService.ResetPasswordAsync(AppSession.CurrentUser.Id, NewPassword, CurrentPassword);
                if (result.Success)
                {
                    _notificationService.ShowSuccess("Password changed successfully.");
                    CurrentPassword = string.Empty;
                    NewPassword = string.Empty;
                    ConfirmPassword = string.Empty;
                }
                else
                {
                    _notificationService.ShowError(result.Error);
                }
            }
            catch (Exception ex)
            {
                _notificationService.ShowError($"Failed to change password: {ex.Message}");
            }
        }

        [RelayCommand]
        public async Task ToggleTwoFactorAsync()
        {
            if (AppSession.CurrentUser == null) return;

            try
            {
                bool newState = !IsTwoFactorEnabled;
                await _userService.SetTwoFactorAsync(AppSession.CurrentUser.Id, newState, null);
                AppSession.CurrentUser.TwoFactorEnabled = newState;
                IsTwoFactorEnabled = newState;
                _notificationService.ShowSuccess($"Two-Factor Authentication is now {(newState ? "Enabled" : "Disabled")}.");
            }
            catch (Exception ex)
            {
                _notificationService.ShowError($"Failed to update Two-Factor Authentication: {ex.Message}");
            }
        }
    }
}
