using Microsoft.Xaml.Behaviors;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace HotelPOS.Behaviors
{
    /// <summary>
    /// A WPF Behavior that attaches to a DataGrid and triggers an ICommand when the 
    /// user scrolls near the bottom of its internal ScrollViewer.
    /// </summary>
    public class InfiniteScrollBehavior : Behavior<DataGrid>
    {
        private ScrollViewer? _scrollViewer;

        public static readonly DependencyProperty CommandProperty =
            DependencyProperty.Register(nameof(Command), typeof(ICommand), typeof(InfiniteScrollBehavior), new PropertyMetadata(null));

        public ICommand? Command // NOSONAR
        {
            get => GetValue(CommandProperty) as ICommand;
            set => SetValue(CommandProperty, value);
        }

        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.Loaded += OnLoaded;
        }

        protected override void OnDetaching()
        {
            AssociatedObject.Loaded -= OnLoaded;
            if (_scrollViewer != null)
            {
                _scrollViewer.ScrollChanged -= OnScrollChanged;
            }
            base.OnDetaching();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _scrollViewer = GetVisualChild<ScrollViewer>(AssociatedObject);
            if (_scrollViewer != null)
            {
                _scrollViewer.ScrollChanged += OnScrollChanged;
            }
        }

        private void OnScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (_scrollViewer == null || Command == null) return;

            // Trigger when reaching 90% of the scrollable height
            if (_scrollViewer.VerticalOffset + _scrollViewer.ViewportHeight >= _scrollViewer.ExtentHeight * 0.9
                && Command.CanExecute(null))
            {
                Command.Execute(null);
            }
        }

        /// <summary>
        /// Recursively searches for a child of type T in the visual tree.
        /// </summary>
        private static T? GetVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T typedChild)
                {
                    return typedChild;
                }

                var result = GetVisualChild<T>(child);
                if (result != null)
                {
                    return result;
                }
            }
            return null;
        }
    }
}
