using HotelPOS.ViewModels;
using System.Windows.Controls;

namespace HotelPOS.Views
{
    public partial class SessionView : UserControl
    {
        private readonly SessionViewModel _viewModel;

        public SessionView(SessionViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = _viewModel;

            Loaded += async (s, e) => await _viewModel.InitializeAsync();
        }
    }
}
