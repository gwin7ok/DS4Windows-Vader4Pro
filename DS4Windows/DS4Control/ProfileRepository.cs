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
using System.IO;
using System.Xml.Serialization;
using DS4WinWPF.DS4Control.DTOXml;

namespace DS4Windows.DS4Control
{
    public interface IProfileRepository
    {
        bool LoadProfile(string filePath, int deviceIndex, BackingStore destination);
        bool SaveProfile(string filePath, int deviceIndex, BackingStore source);
    }

    public class ProfileRepository : IProfileRepository
    {
        public bool LoadProfile(string filePath, int deviceIndex, BackingStore destination)
        {
            try
            {
                if (!File.Exists(filePath)) return false;

                XmlSerializer serializer = new XmlSerializer(typeof(ProfileDTO), ProfileDTO.GetAttributeOverrides());

                using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    if (serializer.Deserialize(fs) is ProfileDTO dto)
                    {
                        dto.DeviceIndex = deviceIndex;
                        dto.MapTo(destination);
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                AppLogger.LogError($"Failed to load profile from {filePath}: {ex.Message}");
            }
            return false;
        }

        public bool SaveProfile(string filePath, int deviceIndex, BackingStore source)
        {
            try
            {
                ProfileDTO dto = new ProfileDTO
                {
                    DeviceIndex = deviceIndex
                };

                dto.MapFrom(source);

                XmlSerializer serializer = new XmlSerializer(typeof(ProfileDTO), ProfileDTO.GetAttributeOverrides());

                string dir = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                using (FileStream fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    serializer.Serialize(fs, dto);
                    return true;
                }
            }
            catch (Exception ex)
            {
                AppLogger.LogError($"Failed to save profile to {filePath}: {ex.Message}");
            }
            return false;
        }
    }
}