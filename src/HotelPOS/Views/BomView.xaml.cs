#nullable enable

using HotelPOS.Domain.Entities;
using HotelPOS.ViewModels;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;

namespace HotelPOS.Views
{
    public partial class BomView : UserControl
    {
        public BomView()
        {
            InitializeComponent();
            DataContext = App.CurrentApp!.ServiceProvider.GetRequiredService<BomViewModel>();
            Loaded += async (_, _) => await ((BomViewModel)DataContext).LoadAsync();
        }

        /// <summary>
        /// When the user picks an ingredient from the ComboBox, update the BomEntryRow on the ViewModel.
        /// </summary>
        private void IngredientComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) // NOSONAR
        {
            if (sender is ComboBox cb
                && cb.Tag is BomEntryRow row
                && cb.SelectedItem is RawMaterial material
                && DataContext is BomViewModel vm)
            {
                vm.OnRawMaterialSelected(row, material);
            }
        }
    }
}
