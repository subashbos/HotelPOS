using HotelPOS.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace HotelPOS.Views
{
    public partial class TdsView : UserControl
    {
        private readonly TdsViewModel _viewModel;

        public TdsView(TdsViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = _viewModel;
            Loaded += TdsView_Loaded;
        }

        private async void TdsView_Loaded(object sender, RoutedEventArgs e)
        {
            await _viewModel.InitializeAsync();
        }
    }
}
