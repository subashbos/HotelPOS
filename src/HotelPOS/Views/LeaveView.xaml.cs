using HotelPOS.ViewModels;
using System.Windows.Controls;

namespace HotelPOS.Views
{
    public partial class LeaveView : UserControl
    {
        private readonly LeaveViewModel _viewModel;

        public LeaveView(LeaveViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = _viewModel;

            Loaded += async (s, e) => await _viewModel.InitializeAsync();
        }
    }
}
