using System;

namespace DS4Windows.DI
{
    public class NotificationEventArgs : EventArgs
    {
        public string Title { get; }
        public string Message { get; }
        public bool IsToast { get; }

        public NotificationEventArgs(string title, string message, bool isToast = true)
        {
            Title = title;
            Message = message;
            IsToast = isToast;
        }
    }

    public interface INotificationService
    {
        bool NotificationsEnabled { get; set; }
        bool FlashTaskbar { get; set; }

        void SendNotification(string title, string message, bool isToast = true);

        event EventHandler<NotificationEventArgs> NotificationTriggered;
    }
}
