using HotelPOS.ViewModels;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;

namespace HotelPOS.Views
{
    public partial class RawMaterialView : UserControl
    {
        public RawMaterialView()
        {
            InitializeComponent();
            DataContext = App.CurrentApp!.ServiceProvider.GetRequiredService<RawMaterialViewModel>();
            Loaded += async (_, _) => await ((RawMaterialViewModel)DataContext).LoadAsync();
        }
    }
}
