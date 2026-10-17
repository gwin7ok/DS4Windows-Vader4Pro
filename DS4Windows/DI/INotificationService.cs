using System;

namespace DS4Windows.DI
{

    public interface INotificationService
    {

        void SendNotification(string title, string message, bool warning = false, bool temporary = false, bool isToast = true);

        event EventHandler<NotificationEventArgs> NotificationTriggered;
    }
}