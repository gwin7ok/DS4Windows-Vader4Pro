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