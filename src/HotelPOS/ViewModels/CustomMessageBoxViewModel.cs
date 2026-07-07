using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HotelPOS.Application.Interfaces;
using System.Windows;

namespace HotelPOS.ViewModels
{
    public partial class CustomMessageBoxViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _message = string.Empty;

        [ObservableProperty]
        private string _title = string.Empty;

        [ObservableProperty]
        private string _iconEmoji = "ℹ️"; // Default icon

        [ObservableProperty]
        private DialogResult _result = DialogResult.None;

        [ObservableProperty]
        private Visibility _okButtonVisibility = Visibility.Collapsed;

        [ObservableProperty]
        private Visibility _cancelButtonVisibility = Visibility.Collapsed;

        [ObservableProperty]
        private Visibility _yesButtonVisibility = Visibility.Collapsed;

        [ObservableProperty]
        private Visibility _noButtonVisibility = Visibility.Collapsed;

        public void Setup(string message, string title, DialogButton button, DialogIcon icon)
        {
            Message = message;
            Title = title;
            
            // Set Icon
            IconEmoji = icon switch
            {
                DialogIcon.Information => "ℹ️",
                DialogIcon.Question => "❓",
                DialogIcon.Warning => "⚠️",
                DialogIcon.Error => "❌",
                _ => ""
            };

            // Set Buttons
            OkButtonVisibility = Visibility.Collapsed;
            CancelButtonVisibility = Visibility.Collapsed;
            YesButtonVisibility = Visibility.Collapsed;
            NoButtonVisibility = Visibility.Collapsed;

            switch (button)
            {
                case DialogButton.OK:
                    OkButtonVisibility = Visibility.Visible;
                    break;
                case DialogButton.OKCancel:
                    OkButtonVisibility = Visibility.Visible;
                    CancelButtonVisibility = Visibility.Visible;
                    break;
                case DialogButton.YesNo:
                    YesButtonVisibility = Visibility.Visible;
                    NoButtonVisibility = Visibility.Visible;
                    break;
                case DialogButton.YesNoCancel:
                    YesButtonVisibility = Visibility.Visible;
                    NoButtonVisibility = Visibility.Visible;
                    CancelButtonVisibility = Visibility.Visible;
                    break;
            }
        }

        public Action? CloseAction { get; set; }

        [RelayCommand]
        private void Ok()
        {
            Result = DialogResult.OK;
            CloseAction?.Invoke();
        }

        [RelayCommand]
        private void Cancel()
        {
            Result = DialogResult.Cancel;
            CloseAction?.Invoke();
        }

        [RelayCommand]
        private void Yes()
        {
            Result = DialogResult.Yes;
            CloseAction?.Invoke();
        }

        [RelayCommand]
        private void No()
        {
            Result = DialogResult.No;
            CloseAction?.Invoke();
        }
    }
}
