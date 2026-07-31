using HotelPOS.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace HotelPOS.Views
{
    public partial class DepartmentDesignationView : UserControl
    {
        private readonly DepartmentDesignationViewModel _viewModel;

        public DepartmentDesignationView(DepartmentDesignationViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = _viewModel;
            Loaded += DepartmentDesignationView_Loaded;
        }

        private async void DepartmentDesignationView_Loaded(object sender, RoutedEventArgs e)
        {
            await _viewModel.InitializeAsync();
        }
    }
}
