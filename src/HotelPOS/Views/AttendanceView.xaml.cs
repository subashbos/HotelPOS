using HotelPOS.ViewModels;
using System.Windows.Controls;

namespace HotelPOS.Views
{
    public partial class AttendanceView : UserControl
    {
        private readonly AttendanceViewModel _viewModel;

        public AttendanceView(AttendanceViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = _viewModel;

            Loaded += async (s, e) => await _viewModel.InitializeAsync();
        }
    }
}
