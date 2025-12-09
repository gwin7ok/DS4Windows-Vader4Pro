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

        // Errorレベルログ専用メソッド（エラー情報用）
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
            if (TrayIconLog != null)
            {
                if (ignoreSettings)
                    TrayIconLog(ignoreSettings, new DebugEventArgs(data, warning));
                else
                    TrayIconLog(null, new DebugEventArgs(data, warning));
            }
        }

        public static void LogProfileChanged(int deviceIndex, string profileName, bool isTemp, ProfileChangeSource source = ProfileChangeSource.Unknown, string originalMessage = null, DateTime? timestamp = null, bool displayNotification = true)
        {
            // NLog Debugレベルで出力（NLog.configで制御可能）
            Logger.Debug($"LogProfileChanged CALLED: device={deviceIndex}, profile={profileName}, isTemp={isTemp}, source={source}, display={displayNotification}");
            
            try
            {
                // originalMessageは呼び出し側で出力されるのでここでは出力しない（重複防止）

                if (displayNotification)
                {
                    Logger.Debug("LogProfileChanged: Invoking ProfileChanged event");
                    ProfileChanged?.Invoke(null, new ProfileChangedEventArgs(deviceIndex, profileName, isTemp, source, originalMessage, timestamp ?? DateTime.UtcNow));
                }
                else
                {
                    Logger.Debug("LogProfileChanged: Skipping notification (displayNotification=false)");
                }
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

