using HotelPOS.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace HotelPOS.Views
{
    public partial class SalaryStructureView : UserControl
    {
        private readonly SalaryStructureViewModel _viewModel;

        public SalaryStructureView(SalaryStructureViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = _viewModel;
            Loaded += SalaryStructureView_Loaded;
        }

        private async void SalaryStructureView_Loaded(object sender, RoutedEventArgs e)
        {
            await _viewModel.InitializeAsync();
        }
    }
}
