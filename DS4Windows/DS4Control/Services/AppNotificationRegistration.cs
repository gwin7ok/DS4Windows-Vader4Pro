using System;
using System.IO;
using System.Reflection;
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
        /// （TagにユニークIDを付与することで、Windows標準の最大3個までのスタック積み上げ表示に対応）
        /// </summary>
        public static void ShowModernToast(string title, string message)
        {
            string toastXmlString = $@"
<toast duration=""short"">
    <visual>
        <binding template=""ToastGeneric"">
            <text>{EscapeXml(title)}</text>
            <text>{EscapeXml(message)}</text>
        </binding>
    </visual>
</toast>";

            Type xmlDocType = ResolveWinRTType("Windows.Data.Xml.Dom.XmlDocument");
            Type toastType = ResolveWinRTType("Windows.UI.Notifications.ToastNotification");
            Type toastManagerType = ResolveWinRTType("Windows.UI.Notifications.ToastNotificationManager");

            if (xmlDocType == null || toastType == null || toastManagerType == null)
            {
                throw new PlatformNotSupportedException("WinRT Notification types could not be resolved.");
            }

            object xmlDoc = Activator.CreateInstance(xmlDocType);
            xmlDocType.GetMethod("LoadXml", new[] { typeof(string) })?.Invoke(xmlDoc, new object[] { toastXmlString });

            object toast = Activator.CreateInstance(toastType, new object[] { xmlDoc });

            // ★ユニークなTagとGroupを付与し、OSに別通知としてスタック（最大3個の積み上げ）認識させる
            string uniqueTag = Guid.NewGuid().ToString("N");
            toastType.GetProperty("Tag")?.SetValue(toast, uniqueTag);
            toastType.GetProperty("Group")?.SetValue(toast, "DS4WNotifications");

            object notifier = toastManagerType.GetMethod("CreateToastNotifier", new[] { typeof(string) })?.Invoke(null, new object[] { AppId });
            notifier?.GetType().GetMethod("Show", new[] { toastType })?.Invoke(notifier, new object[] { toast });
        }

        private static Type ResolveWinRTType(string fullName)
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type type = assembly.GetType(fullName);
                if (type != null)
                    return type;
            }

            try
            {
                Assembly sdkAssembly = Assembly.Load("Microsoft.Windows.SDK.NET");
                Type type = sdkAssembly?.GetType(fullName);
                if (type != null)
                    return type;
            }
            catch { }

            return Type.GetType(fullName);
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