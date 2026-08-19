using HotelPOS.ViewModels;
using HotelPOS.Views.Common;

namespace HotelPOS.Views
{
    public partial class EmployeeEntryDialog : EntryDialogWindow
    {
        public EmployeeEntryDialog(EmployeeEntryViewModel viewModel)
        {
            InitializeComponent();
            InitializeEntryDialog(viewModel, FirstNameInput);
        }
    }
}
