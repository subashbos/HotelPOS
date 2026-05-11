namespace HotelPOS.Application.Interfaces
{
    public interface INotificationService
    {
        void ShowSuccess(string message);
        void ShowError(string message);
        void ShowInfo(string message);
        void ShowWarning(string message);
        event Action<NotificationEventArgs> NotificationReceived;
    }

    public class NotificationEventArgs : EventArgs
    {
        public string Message { get; set; } = string.Empty;
        public NotificationType Type { get; set; }
    }

    public enum NotificationType
    {
        Success,
        Error,
        Info,
        Warning
    }
}
