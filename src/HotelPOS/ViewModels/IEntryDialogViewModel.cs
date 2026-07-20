using System.Windows.Input;

namespace HotelPOS.ViewModels
{
    /// <summary>
    /// Contract the shared <c>EntryDialogWindow</c> base class needs from an entry-dialog
    /// view model: a way to close the owning window and a command to invoke on Ctrl+S.
    /// </summary>
    public interface IEntryDialogViewModel
    {
        event EventHandler<bool>? RequestClose;

        ICommand SaveCommand { get; }
    }
}
