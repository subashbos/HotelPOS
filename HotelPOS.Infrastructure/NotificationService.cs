using HotelPOS.Application.Interfaces;
using ToastNotifications;
using ToastNotifications.Lifetime;
using ToastNotifications.Position;
using ToastNotifications.Messages;
using System.Windows;

namespace HotelPOS.Infrastructure
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
                    cfg.PositionProvider = new WindowPositionProvider(
                        parentWindow: System.Windows.Application.Current.MainWindow ?? throw new InvalidOperationException("Main window not found"),
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
