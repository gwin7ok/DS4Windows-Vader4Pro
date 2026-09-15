using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace DS4Windows
{
    /// <summary>
    /// ポータブル実行時でもWindows通知システムにアプリを認識させ、
    /// Windows 10/11 ネイティブのトースト通知を発行するヘルパー
    /// </summary>
    public static class AppNotificationRegistration
    {
        public const string AppId = "DS4Windows";

        [DllImport("shell32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern int SetCurrentProcessExplicitAppUserModelID([MarshalAs(UnmanagedType.LPWStr)] string AppID);

        /// <summary>
        /// アプリ起動時に呼び出し、プロセスへのAUMID割り当てとHKCUレジストリ登録を行います。
        /// </summary>
        public static void Initialize()
        {
            try
            {
                // 1. プロセスに AUMID を明示設定
                SetCurrentProcessExplicitAppUserModelID(AppId);

                // 2. HKCU レジストリに AUMID 情報を自動登録（管理者権限不要、設定の通知一覧に常時表示）
                RegisterInRegistry();
            }
            catch
            {
                // レジストリ書き込み失敗等でもアプリの起動自体は妨げない
            }
        }

        private static void RegisterInRegistry()
        {
            string exePath = Environment.ProcessPath;
            if (string.IsNullOrEmpty(exePath) || !File.Exists(exePath))
                return;

            string subKey = $@"Software\Classes\AppUserModelId\{AppId}";
            using RegistryKey key = Registry.CurrentUser.CreateSubKey(subKey);
            if (key != null)
            {
                key.SetValue("DisplayName", "DS4Windows", RegistryValueKind.String);
                key.SetValue("IconUri", exePath, RegistryValueKind.String);
                key.SetValue("IconBackgroundColor", "0", RegistryValueKind.String);

                // ShowInSettings = 1 により Windows 10/11 の「設定 > システム > 通知」一覧に登録される
                key.SetValue("ShowInSettings", 1, RegistryValueKind.DWord);
            }
        }

        /// <summary>
        /// Windows 10 / 11 のモダン トースト通知を発行します。
        /// </summary>
        public static void ShowModernToast(string title, string message)
        {
            string toastXmlString = $@"
<toast>
    <visual>
        <binding template=""ToastGeneric"">
            <text>{EscapeXml(title)}</text>
            <text>{EscapeXml(message)}</text>
        </binding>
    </visual>
</toast>";

            var xmlDoc = new Windows.Data.Xml.Dom.XmlDocument();
            xmlDoc.LoadXml(toastXmlString);

            var toast = new Windows.UI.Notifications.ToastNotification(xmlDoc);
            var notifier = Windows.UI.Notifications.ToastNotificationManager.CreateToastNotifier(AppId);
            notifier.Show(toast);
        }

        private static string EscapeXml(string input)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            return input
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;")
                .Replace("'", "&apos;");
        }
    }
}