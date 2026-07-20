using HotelPOS.ViewModels;
using HotelPOS.Views.Common;

namespace HotelPOS.Views
{
    public partial class CustomerEntryDialog : EntryDialogWindow
    {
        public CustomerEntryDialog(CustomerEntryViewModel viewModel)
        {
            InitializeComponent();
            InitializeEntryDialog(viewModel, NameInput);
        }
    }
}
