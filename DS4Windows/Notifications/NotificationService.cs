using System;

namespace DS4WinWPF
{
    // Simple facade kept for compatibility. Use AppLogger.LogToTray to route
    // notifications through the existing notifyIcon / tray pathway used elsewhere.
    public static class NotificationService
    {
        public static void ShowToast(string title, string message)
        {
            try
            {
                DS4Windows.AppLogger.LogToTray(message);
            }
            catch { }
        }
    }
}
