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
        private Window? _notifierParent;

        private Notifier GetNotifier()
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

            // The window we last bound to (e.g. the login/registration window) may since have
            // closed as the app navigated on (login -> dashboard, registration -> login). A stale
            // Notifier keeps positioning against that closed window and WPF throws
            // "Cannot set Visibility ... after a Window has closed" the next time a toast fires.
            // Rebuild against whichever window is current whenever the bound window has changed.
            if (_notifier == null || !ReferenceEquals(_notifierParent, parent))
            {
                _notifier?.Dispose();
                _notifierParent = parent;
                _notifier = new Notifier(cfg =>
                {
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
