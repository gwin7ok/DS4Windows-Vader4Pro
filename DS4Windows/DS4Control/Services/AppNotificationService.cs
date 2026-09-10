using System;
using DS4Windows.DI;

namespace DS4Windows
{
    public class AppNotificationService : INotificationService
    {
        public event EventHandler<NotificationEventArgs> NotificationTriggered;

        public bool NotificationsEnabled
        {
            get => Global.Notifications != 0;
            set => Global.Notifications = value ? (Global.Notifications != 0 ? Global.Notifications : 1) : 0;
        }

        public bool FlashTaskbar
        {
            get => Global.FlashWhenLate;
            set => Global.FlashWhenLate = value;
        }

        public void SendNotification(string title, string message, bool isToast = true)
        {
            if (!NotificationsEnabled)
                return;

            if (AppLogger.IsTraceEnabled)
                AppLogger.LogTrace($"[DI] AppNotificationService.SendNotification: '{title}' - '{message}'");

            NotificationTriggered?.Invoke(this, new NotificationEventArgs(title, message, isToast));
        }
    }
}
