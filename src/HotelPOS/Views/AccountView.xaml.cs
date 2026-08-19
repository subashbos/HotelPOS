using HotelPOS.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace HotelPOS.Views
{
    public partial class AccountView : UserControl
    {
        private readonly AccountViewModel _viewModel;

        public AccountView(AccountViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = _viewModel;
            Loaded += AccountView_Loaded;
        }

        private void AccountView_Loaded(object sender, RoutedEventArgs e)
        {
            _viewModel.Initialize();
        }

        private void CurrentPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (sender is PasswordBox pb)
            {
                _viewModel.CurrentPassword = pb.Password;
            }
        }

        private void NewPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (sender is PasswordBox pb)
            {
                _viewModel.NewPassword = pb.Password;
            }
        }

        private void ConfirmPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (sender is PasswordBox pb)
            {
                _viewModel.ConfirmPassword = pb.Password;
            }
        }
    }
}
