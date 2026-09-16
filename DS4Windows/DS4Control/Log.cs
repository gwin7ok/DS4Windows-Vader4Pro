/*
DS4Windows
Copyright (C) 2023  Travis Nickles

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/

using System;
using NLog;

namespace DS4Windows
{
    public class AppLogger
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        // 2026-09: MainWindowの購読先を INotificationService.NotificationTriggered へ移行済みのため、
        // このイベント自体は発火されなくなった。次回クリーンアップで削除予定（Phase5-Step14 通知経路統合参照）。
        [Obsolete("MainWindowの購読先をINotificationService.NotificationTriggeredへ移行済み。次回クリーンアップで削除予定。")]
        public static event EventHandler<DebugEventArgs> TrayIconLog;
        public static event EventHandler<DebugEventArgs> GuiLog;
        // 型付きプロファイル変更イベント
        public static event EventHandler<ProfileChangedEventArgs> ProfileChanged;

        // GUI表示用ログ（LoggerHolderがGuiLogイベントを受け取ってファイル出力）
        public static void LogToGui(string data, bool warning, bool temporary = false)
        {
            if (GuiLog != null)
            {
                GuiLog(null, new DebugEventArgs(data, warning, temporary));
            }
        }

        // Debugレベルログ専用メソッド（GUI表示なし、ログファイルのみ）
        public static void LogDebug(string data)
        {
            Logger.Debug(data);
        }

        // Traceレベルログ専用メソッド（より詳細なデバッグ情報用）
        public static void LogTrace(string data)
        {
            Logger.Trace(data);
        }

        // Expose trace-enabled flag to avoid expensive string formatting when trace is off
        public static bool IsTraceEnabled => Logger.IsTraceEnabled;

        // Errorレベルログ専用メソッド（エラー情報用）
        // Warnレベルログ専用メソッド（警告情報用）
        public static void LogWarn(string data)
        {
            Logger.Warn(data);
        }
        public static void LogError(string data)
        {
            Logger.Error(data);
        }

        // Infoレベルログ専用メソッド（情報ログ用）
        public static void LogInfo(string data)
        {
            Logger.Info(data);
        }

        public static void LogToTray(string data, bool warning = false, bool ignoreSettings = false)
        {
            // ignoreSettings: 現状も無効（無機能）のパラメータ。旧実装でも受信側(MainWindow)が
            // sender引数を一切参照していなかったため実質未配線だった。今回のリファクタでも
            // 意図的に配線しない（挙動を変えないため）。将来対応が必要になった場合は、
            // INotificationService.SendNotification 側にも同等の引数追加を検討すること。
            Logger.Debug($"[Diag-Toast] LogToTray 呼び出し: data='{data}', warning={warning}, ignoreSettings={ignoreSettings}");

            try
            {
                // title は意図的に空文字を渡す。表示本体(MainWindow.ShowSystemNotification)は
                // イベント側のタイトルを使わず常に TrayIconViewModel.ballonTitle を自前解決するため、
                // ここでUI層のクラスへ依存を持ち込む必要がない（下位層→上位層参照の禁止に抵触しないため）。
                // temporary は旧実装でも LogToTray からは常に false 固定だった（このメソッド自体に
                // temporary パラメータが存在しなかったため）。挙動を変えないためここでも false 固定とする。
                Global.NotificationServiceInstance.SendNotification(string.Empty, data, warning: warning, temporary: false);
            }
            catch (Exception ex)
            {
                Logger.Debug($"[Diag-Toast] LogToTray から SendNotification 呼び出しで例外発生: {ex.GetType().Name}: {ex.Message}");
            }
        }

        /// <summary>
        /// プロファイル適用イベントを記録し、ProfileChanged イベントを発火する。
        /// </summary>
        /// <param name="displayNotification">
        /// このプロファイル変更をイベントとして通知対象とするか否か（サイレント内部処理を除外するためのフラグ。既定は true）。
        /// UIへの実際の表示可否（トースト／独自ウィンドウのどちらで出すか、あるいは出さないか）は、
        /// 本イベントの購読側（MainWindow.OnProfileChanged）が ProfileChangedNotification 設定を見て別途判定する。
        /// </param>
        public static void LogProfileChanged(int deviceIndex, string profileName, bool isTemp, ProfileChangeSource source = ProfileChangeSource.Unknown, string originalMessage = null, DateTime? timestamp = null)
        {
            Logger.Debug($"LogProfileChanged CALLED: device={deviceIndex}, profile={profileName}, isTemp={isTemp}, source={source}");

            try
            {
                // ★ displayNotification による不要なスキップを完全撤廃し、プロファイル適用イベントを常に発行する
                Logger.Debug("LogProfileChanged: Invoking ProfileChanged event");
                ProfileChanged?.Invoke(null, new ProfileChangedEventArgs(deviceIndex, profileName, isTemp, source, originalMessage, timestamp ?? DateTime.UtcNow));
            }
            catch { }
        }
    }

    public enum ProfileChangeSource : byte
    {
        Unknown = 0,
        ControlService,
        AutoProfile,
        MappingAction,
        Hotkey,
        Manual,
        Other
    }

    public class ProfileChangedEventArgs : EventArgs
    {
        public int DeviceIndex { get; }
        public string ProfileName { get; }
        public bool IsTemp { get; }
        public ProfileChangeSource Source { get; }
        public string OriginalMessage { get; }
        public DateTime Timestamp { get; }

        public ProfileChangedEventArgs(int deviceIndex, string profileName, bool isTemp, ProfileChangeSource source = ProfileChangeSource.Unknown, string originalMessage = null, DateTime? timestamp = null)
        {
            DeviceIndex = deviceIndex;
            ProfileName = profileName ?? string.Empty;
            IsTemp = isTemp;
            Source = source;
            OriginalMessage = originalMessage ?? string.Empty;
            Timestamp = timestamp ?? DateTime.UtcNow;
        }
    }
}