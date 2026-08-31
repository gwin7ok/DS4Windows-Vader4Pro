using System;
using DS4Windows.DI;

namespace DS4Windows
{
    public class AppNotificationService : INotificationService
    {
        private readonly object _syncLock = new object();

        private bool _notificationsEnabled = true;
        private bool _flashTaskbar = false;

        public event EventHandler<NotificationEventArgs> NotificationTriggered;

        public bool NotificationsEnabled
        {
            get => _notificationsEnabled;
            set { lock (_syncLock) { _notificationsEnabled = value; } }
        }

        public bool FlashTaskbar
        {
            get => _flashTaskbar;
            set { lock (_syncLock) { _flashTaskbar = value; } }
        }

        public void SendNotification(string title, string message, bool isToast = true)
        {
            if (!NotificationsEnabled)
                return;

            AppLogger.LogToGui($"[DI] AppNotificationService.SendNotification: '{title}' - '{message}'", false, true);
            NotificationTriggered?.Invoke(this, new NotificationEventArgs(title, message, isToast));
        }
    }
}
