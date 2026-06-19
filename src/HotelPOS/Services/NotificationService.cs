using HotelPOS.Application.Interfaces;
using System.Windows;
using ToastNotifications;
using ToastNotifications.Lifetime;
using ToastNotifications.Messages;
using ToastNotifications.Position;

namespace HotelPOS.Services
{
    public class NotificationService : INotificationService
    {
        private Notifier? _notifier;

        private Notifier GetNotifier()
        {
            if (_notifier == null)
            {
                _notifier = new Notifier(cfg =>
                {
                    var parent = System.Windows.Application.Current.MainWindow;

                    // Fallback: If MainWindow is null or not yet loaded, find any active window
                    if (parent == null || !parent.IsLoaded)
                    {
                        parent = System.Windows.Application.Current.Windows
                            .Cast<Window>()
                            .FirstOrDefault(w => w.IsLoaded);
                    }

                    if (parent == null)
                    {
                        // If still no window, we can't show a toast. 
                        // We'll throw a slightly more descriptive error or just return a dummy.
                        // But usually, one window must be open if this is called from UI.
                        throw new InvalidOperationException("No loaded window found for notification positioning.");
                    }

                    cfg.PositionProvider = new WindowPositionProvider(
                        parentWindow: parent,
                        corner: Corner.BottomRight,
                        offsetX: 20,
                        offsetY: 20);

                    cfg.LifetimeSupervisor = new TimeAndCountBasedLifetimeSupervisor(
                        notificationLifetime: TimeSpan.FromSeconds(3),
                        maximumNotificationCount: MaximumNotificationCount.FromCount(5));

                    cfg.Dispatcher = System.Windows.Application.Current.Dispatcher;
                });
            }
            return _notifier;
        }

        public event Action<NotificationEventArgs>? NotificationReceived;

        public void ShowSuccess(string message)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() => GetNotifier().ShowSuccess(message));
            NotificationReceived?.Invoke(new NotificationEventArgs { Message = message, Type = NotificationType.Success });
        }

        public void ShowError(string message)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() => GetNotifier().ShowError(message));
            NotificationReceived?.Invoke(new NotificationEventArgs { Message = message, Type = NotificationType.Error });
        }

        public void ShowInfo(string message)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() => GetNotifier().ShowInformation(message));
            NotificationReceived?.Invoke(new NotificationEventArgs { Message = message, Type = NotificationType.Info });
        }

        public void ShowWarning(string message)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() => GetNotifier().ShowWarning(message));
            NotificationReceived?.Invoke(new NotificationEventArgs { Message = message, Type = NotificationType.Warning });
        }
    }
}
