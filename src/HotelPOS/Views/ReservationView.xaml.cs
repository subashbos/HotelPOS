using HotelPOS.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace HotelPOS.Views
{
    public partial class ReservationView : UserControl
    {
        private readonly ReservationViewModel _viewModel;

        public ReservationView(ReservationViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = _viewModel;

            PreviewKeyDown += ReservationView_PreviewKeyDown;
        }

        /// <summary>A reservation block was clicked: select it (surfaces the action bar below the
        /// timeline) and stop the click bubbling to <see cref="SchedulerCanvas_MouseLeftButtonDown"/>,
        /// which would otherwise treat it as an empty-slot click.</summary>
        private void SchedulerBlock_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            if (sender is FrameworkElement { DataContext: ReservationBlock block })
            {
                _viewModel.SelectBlockCommand.Execute(block.Reservation);
            }
        }

        /// <summary>An empty area of the timeline was clicked: work out which table's row and what
        /// time it maps to, and prefill the booking form with that slot.</summary>
        private void SchedulerCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is not Canvas canvas) return;

            var position = e.GetPosition(canvas);
            var rowIndex = (int)(position.Y / ReservationViewModel.RowHeight);
            if (rowIndex < 0 || rowIndex >= _viewModel.Tables.Count) return;

            var rawMinutes = _viewModel.RangeStartMinutes + position.X / ReservationViewModel.PxPerHour * 60;
            var snapped = (int)(Math.Round(rawMinutes / 30.0) * 30);
            snapped = Math.Min(Math.Max(snapped, _viewModel.RangeStartMinutes), _viewModel.RangeEndMinutes - 30);

            _viewModel.OpenFormAt(_viewModel.Tables[rowIndex], snapped);
        }

        private void ReservationView_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // 1. Ctrl + S to Book Table
            if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control && e.Key == Key.S)
            {
                e.Handled = true;
                if (_viewModel.SaveReservationCommand.CanExecute(null))
                {
                    _viewModel.SaveReservationCommand.Execute(null);
                }
            }
            // 2. F4 to focus the Start Time field
            else if (e.Key == Key.F4)
            {
                e.Handled = true;
                StartTimeTextBox.Focus();
            }
            // 3. Enter key -> Save (skip multi-line note inputs)
            else if (e.Key == Key.Enter)
            {
                var element = Keyboard.FocusedElement as UIElement;
                if (element is TextBox tb && tb.AcceptsReturn)
                {
                    return;
                }

                e.Handled = true;
                if (_viewModel.SaveReservationCommand.CanExecute(null))
                {
                    _viewModel.SaveReservationCommand.Execute(null);
                }
            }
        }
    }
}
