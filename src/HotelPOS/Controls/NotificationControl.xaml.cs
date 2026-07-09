using HotelPOS.Application.Interfaces;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace HotelPOS.Controls
{
    public partial class NotificationControl : UserControl
    {
        private DispatcherTimer _timer;

        public NotificationControl()
        {
            InitializeComponent();
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(5);
            _timer.Tick += (s, e) => Hide();
        }

        public void Show(string message, NotificationType type)
        {
            MsgText.Text = message;
            _timer.Stop();

            switch (type)
            {
                case NotificationType.Success:
                    TitleText.Text = "Success";
                    IconText.Text = "✅";
                    MainBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(39, 174, 96));
                    break;
                case NotificationType.Error:
                    TitleText.Text = "Error";
                    IconText.Text = "❌";
                    MainBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(192, 57, 43));
                    break;
                case NotificationType.Info:
                    TitleText.Text = "Information";
                    IconText.Text = "ℹ️";
                    MainBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(52, 152, 219));
                    break;
            }

            this.Visibility = Visibility.Visible;
            Storyboard sb = (Storyboard)FindResource("ShowAnim");
            sb.Begin(this);
            _timer.Start();
        }

        private void Hide()
        {
            _timer.Stop();
            Storyboard sb = (Storyboard)FindResource("HideAnim");
            sb.Completed += (s, e) => this.Visibility = Visibility.Collapsed;
            sb.Begin(this);
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Hide();
        }
    }
}
