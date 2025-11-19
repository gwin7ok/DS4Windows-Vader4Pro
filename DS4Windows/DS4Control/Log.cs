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

namespace DS4Windows
{
    public class AppLogger
    {
        public static event EventHandler<DebugEventArgs> TrayIconLog;
        public static event EventHandler<DebugEventArgs> GuiLog;
        // 型付きプロファイル変更イベント
        public static event EventHandler<ProfileChangedEventArgs> ProfileChanged;

        public static void LogToGui(string data, bool warning, bool temporary = false)
        {
            if (GuiLog != null)
            {
                GuiLog(null, new DebugEventArgs(data, warning, temporary));
            }
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

        public static void LogProfileChanged(int deviceIndex, string profileName, bool isTemp, ProfileChangeSource source = ProfileChangeSource.Unknown)
        {
            try
            {
                ProfileChanged?.Invoke(null, new ProfileChangedEventArgs(deviceIndex, profileName, isTemp, source));
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

        public ProfileChangedEventArgs(int deviceIndex, string profileName, bool isTemp, ProfileChangeSource source = ProfileChangeSource.Unknown)
        {
            DeviceIndex = deviceIndex;
            ProfileName = profileName ?? string.Empty;
            IsTemp = isTemp;
            Source = source;
        }
    }
}

