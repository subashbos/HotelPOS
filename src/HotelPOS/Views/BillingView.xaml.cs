using Microsoft.Extensions.DependencyInjection;
using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Entities; // Domain entities for binding
using HotelPOS.ViewModels; // Main ViewModels for DataContext
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace HotelPOS.Views
{
    public partial class BillingView : UserControl
    {
        private readonly BillingViewModel _viewModel;

        public BillingView(BillingViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = _viewModel;


            Loaded += async (s, e) =>
            {
                try
                {
                    await _viewModel.InitializeAsync();
                    SyncOrderTypeButtons(_viewModel.OrderType);
                    FocusSearch();
                }
                catch (Exception ex)
                {
                    App.CurrentApp!.ServiceProvider.GetRequiredService<HotelPOS.Application.Interfaces.IDialogService>().ShowMessage($"Error initializing BillingView: {ex.Message}\n{ex.StackTrace}", "Error", HotelPOS.Application.Interfaces.DialogButton.OK, HotelPOS.Application.Interfaces.DialogIcon.Error);
                }
            };

            // Keep buttons in sync when OrderType changes from code (e.g. LoadOrderForEdit)
            _viewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(BillingViewModel.OrderType))
                    SyncOrderTypeButtons(_viewModel.OrderType);
            };

            // Relay the ViewModel's navigation event outward to the shell
            _viewModel.OrderUpdated += () => OrderUpdated?.Invoke();
            _viewModel.OrderEditCancelled += () => OrderEditCancelled?.Invoke();
            _viewModel.PrintPreviewClosed += () => FocusSearch();
            _viewModel.CartCleared += () => FocusSearch();
            _viewModel.CheckoutCancelled += () => FocusSearch();
        }

        public event Action? OrderUpdated;
        public event Action? OrderEditCancelled;

        public void LoadOrderForEdit(Order order)
        {
            _viewModel.LoadOrderForEdit(order);
        }

        public void FocusSearch()
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                SearchBox.Focus();
                Keyboard.Focus(SearchBox);
                SearchBox.SelectAll();
            }), System.Windows.Threading.DispatcherPriority.Input);
        }

        public void TriggerCheckout()
        {
            if (_viewModel.SaveOrderCommand.CanExecute(null))
            {
                _viewModel.SaveOrderCommand.Execute(null);
            }
        }

        private void UserControl_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F4)
            {
                _viewModel.SaveOrderCommand.Execute(null);
            }
            else if (e.Key == Key.F1 || e.Key == Key.F3 || (e.Key == Key.F && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control))
            {
                e.Handled = true;
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    SearchBox.Focus();
                    Keyboard.Focus(SearchBox);
                    SearchBox.SelectAll();
                }), System.Windows.Threading.DispatcherPriority.Input);
            }
            else if (e.Key == Key.Enter)
            {
                // If focus is on PaymentMode ComboBox, trigger checkout
                var focused = FocusManager.GetFocusedElement(this);
                if (focused == PaymentModeCombo || (focused is ComboBoxItem cbi && VisualTreeHelper.GetParent(cbi) != null))
                {
                    e.Handled = true;
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        CheckoutButton.Focus();
                        Keyboard.Focus(CheckoutButton);
                    }), System.Windows.Threading.DispatcherPriority.Input);
                }
            }
        }

        private void OrderTypeBtn_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string orderType)
            {
                _viewModel.OrderType = orderType;
                SyncOrderTypeButtons(orderType);
            }
        }

        private void SyncOrderTypeButtons(string orderType)
        {
            // Active button: primary colour + white text; inactive: surface colour
            var activeBackground = (System.Windows.Media.Brush)FindResource("PrimaryBrush");
            var inactiveBackground = (System.Windows.Media.Brush)FindResource("SurfaceBrush");
            var activeText = System.Windows.Media.Brushes.White;
            var inactiveText = (System.Windows.Media.Brush)FindResource("TextPrimary");

            foreach (var btn in new[] { BtnDineIn, BtnTakeaway, BtnOnline })
            {
                bool isActive = btn.Tag?.ToString() == orderType;
                btn.Background = isActive ? activeBackground : inactiveBackground;
                btn.Foreground = isActive ? activeText : inactiveText;
                btn.BorderBrush = isActive ? activeBackground : (System.Windows.Media.Brush)FindResource("BorderBrush");
            }
        }

        private void PaymentModeCombo_DropDownClosed(object sender, EventArgs e)
        {
            // Use ApplicationIdle priority so WPF finishes all selection/focus
            // processing before we move focus to the Checkout button.
            Dispatcher.BeginInvoke(new Action(() =>
            {
                CheckoutButton.IsDefault = true;
                CheckoutButton.Focus();
                Keyboard.Focus(CheckoutButton);
                FocusManager.SetFocusedElement(this, CheckoutButton);
            }), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
        }
    }
}
