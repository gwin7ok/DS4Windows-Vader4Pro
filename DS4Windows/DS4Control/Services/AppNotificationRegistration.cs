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

        // Phase5-Step14フォローアップ: ShowModernToast はプロファイル切換のたびにUIスレッド上で
        // 同期実行されるため、リフレクション解決（型・メソッド・プロパティ）を初回のみ行いキャッシュする。
        // 解決に失敗した場合（WinRT非対応環境等）はキャッシュを確定させず、次回呼び出し時に再試行する
        // （従来どおり毎回解決を試みるフォールバック挙動を維持し、機能低下がないようにする）。
        private static readonly object _cacheLock = new object();
        private static bool _cacheReady = false;
        private static Type _xmlDocType;
        private static Type _toastType;
        private static Type _toastManagerType;
        private static Type _toastNotifierType;
        private static MethodInfo _loadXmlMethod;
        private static PropertyInfo _tagProperty;
        private static MethodInfo _createToastNotifierMethod;
        private static MethodInfo _showMethod;

        private static void EnsureWinRTMembersResolved()
        {
            if (_cacheReady)
                return;

            lock (_cacheLock)
            {
                if (_cacheReady)
                    return;

                Type xmlDocType = ResolveWinRTType("Windows.Data.Xml.Dom.XmlDocument");
                Type toastType = ResolveWinRTType("Windows.UI.Notifications.ToastNotification");
                Type toastManagerType = ResolveWinRTType("Windows.UI.Notifications.ToastNotificationManager");
                Type toastNotifierType = ResolveWinRTType("Windows.UI.Notifications.ToastNotifier");

                if (xmlDocType == null || toastType == null || toastManagerType == null)
                {
                    // 解決失敗時はキャッシュを確定させない（次回呼び出しで再試行させるため _cacheReady は立てない）
                    _xmlDocType = null;
                    _toastType = null;
                    _toastManagerType = null;
                    _toastNotifierType = null;
                    _loadXmlMethod = null;
                    _tagProperty = null;
                    _createToastNotifierMethod = null;
                    _showMethod = null;
                    return;
                }

                _xmlDocType = xmlDocType;
                _toastType = toastType;
                _toastManagerType = toastManagerType;
                _toastNotifierType = toastNotifierType;
                _loadXmlMethod = xmlDocType.GetMethod("LoadXml", new[] { typeof(string) });
                _tagProperty = toastType.GetProperty("Tag");
                _createToastNotifierMethod = toastManagerType.GetMethod("CreateToastNotifier", new[] { typeof(string) });
                // ToastNotifier 型が解決できた場合はそちらから、できない場合は従来どおり実行時の
                // GetType() から Show メソッドを取得する（フォールバック、挙動は変えない）。
                _showMethod = toastNotifierType?.GetMethod("Show", new[] { toastType });

                _cacheReady = true;
            }
        }

        /// <summary>
        /// Windows 10 / 11 のモダン トースト通知を発行します。
        /// （TagにユニークIDを付与し、Groupを指定しないことでWindows標準の最大3個までのスタック積み上げ表示に対応）
        /// </summary>
        public static void ShowModernToast(string title, string message)
        {
            string uniqueTag = Guid.NewGuid().ToString("N");

            string toastXmlString = $@"
<toast duration=""short"">
    <visual>
        <binding template=""ToastGeneric"">
            <text>{EscapeXml(title)}</text>
            <text>{EscapeXml(message)}</text>
        </binding>
    </visual>
</toast>";

            EnsureWinRTMembersResolved();

            if (_xmlDocType == null || _toastType == null || _toastManagerType == null)
            {
                throw new PlatformNotSupportedException("WinRT Notification types could not be resolved.");
            }

            object xmlDoc = Activator.CreateInstance(_xmlDocType);
            _loadXmlMethod?.Invoke(xmlDoc, new object[] { toastXmlString });

            object toast = Activator.CreateInstance(_toastType, new object[] { xmlDoc });

            // ★ユニークなTagのみを付与（Groupは未指定にすることで、OSに上方向スタック積み上げを実行させる）
            _tagProperty?.SetValue(toast, uniqueTag);

            string actualTag = _tagProperty?.GetValue(toast)?.ToString() ?? "(null)";

            try
            {
                AppLogger.LogTrace($"[ModernToast] Dispatch Stacked Toast: ToastID(Tag)='{actualTag}', Title='{title}', Message='{message}'");
            }
            catch { }

            object notifier = _createToastNotifierMethod?.Invoke(null, new object[] { AppId });
            MethodInfo showMethod = _showMethod ?? notifier?.GetType().GetMethod("Show", new[] { _toastType });
            showMethod?.Invoke(notifier, new object[] { toast });
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