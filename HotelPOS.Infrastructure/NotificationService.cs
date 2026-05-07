using HotelPOS.Application.Interfaces;

namespace HotelPOS.Infrastructure
{
    public class NotificationService : INotificationService
    {
        public event Action<NotificationEventArgs>? NotificationReceived;

        public void ShowSuccess(string message)
        {
            NotificationReceived?.Invoke(new NotificationEventArgs { Message = message, Type = NotificationType.Success });
        }

        public void ShowError(string message)
        {
            NotificationReceived?.Invoke(new NotificationEventArgs { Message = message, Type = NotificationType.Error });
        }

        public void ShowInfo(string message)
        {
            NotificationReceived?.Invoke(new NotificationEventArgs { Message = message, Type = NotificationType.Info });
        }

        public void ShowWarning(string message)
        {
            NotificationReceived?.Invoke(new NotificationEventArgs { Message = message, Type = NotificationType.Warning });
        }
    }
}
