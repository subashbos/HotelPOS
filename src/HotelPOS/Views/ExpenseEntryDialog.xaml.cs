using HotelPOS.ViewModels;
using HotelPOS.Views.Common;

namespace HotelPOS.Views
{
    public partial class ExpenseEntryDialog : EntryDialogWindow
    {
        public ExpenseEntryDialog(ExpenseEntryViewModel viewModel)
        {
            InitializeComponent();
            InitializeEntryDialog(viewModel, TitleInput);
        }
    }
}
