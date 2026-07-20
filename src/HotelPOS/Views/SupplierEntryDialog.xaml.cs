using HotelPOS.ViewModels;
using HotelPOS.Views.Common;

namespace HotelPOS.Views
{
    public partial class SupplierEntryDialog : EntryDialogWindow
    {
        public SupplierEntryDialog(SupplierEntryViewModel viewModel)
        {
            InitializeComponent();
            InitializeEntryDialog(viewModel, NameInput);
        }
    }
}
