using System;

namespace DS4Windows.DI
{
    public class NotificationEventArgs : EventArgs
    {
        public string Title { get; }
        public string Message { get; }
        public bool Warning { get; }
        public bool Temporary { get; }
        public bool IsToast { get; }

        public NotificationEventArgs(string title, string message, bool warning = false, bool temporary = false, bool isToast = true)
        {
            Title = title;
            Message = message;
            Warning = warning;
            Temporary = temporary;
            IsToast = isToast;
        }
    }

    public interface INotificationService
    {
        bool NotificationsEnabled { get; set; }
        bool FlashTaskbar { get; set; }

        void SendNotification(string title, string message, bool warning = false, bool temporary = false, bool isToast = true);

        event EventHandler<NotificationEventArgs> NotificationTriggered;
    }
}