using System;
using System.Collections.Generic;
using DS4Windows.DI;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase5-Step14 通知経路統合検証用の INotificationService モック。
    /// AppLogger.LogToTray から Global.NotificationServiceInstance.SendNotification への
    /// 委譲が、期待した引数で行われることを検証するために使用する。
    /// </summary>
    public class MockNotificationService : INotificationService
    {
        public class SendCall
        {
            public string Title { get; set; }
            public string Message { get; set; }
            public bool Warning { get; set; }
            public bool Temporary { get; set; }
            public bool IsToast { get; set; }
        }

        public List<SendCall> SendCalls { get; } = new List<SendCall>();

        public bool NotificationsEnabled { get; set; } = true;
        public bool FlashTaskbar { get; set; }

        public event EventHandler<NotificationEventArgs> NotificationTriggered;

        public void SendNotification(string title, string message, bool warning = false, bool temporary = false, bool isToast = true)
        {
            SendCalls.Add(new SendCall
            {
                Title = title,
                Message = message,
                Warning = warning,
                Temporary = temporary,
                IsToast = isToast
            });

            NotificationTriggered?.Invoke(this, new NotificationEventArgs(title, message, warning, temporary, isToast));
        }

        public void Reset()
        {
            SendCalls.Clear();
        }
    }
}