using HotelPOS.ViewModels;
using System.Windows.Controls;

namespace HotelPOS.Views
{
    public partial class PayrollView : UserControl
    {
        private readonly PayrollViewModel _viewModel;

        public PayrollView(PayrollViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = _viewModel;

            Loaded += async (s, e) => await _viewModel.InitializeAsync();
        }
    }
}
