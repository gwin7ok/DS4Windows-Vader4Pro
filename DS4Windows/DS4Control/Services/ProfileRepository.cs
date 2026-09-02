using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DS4Windows.DI;

namespace DS4Windows
{
    public class ProfileRepository : IProfileRepository
    {
        private readonly object _fileLock = new object();
        private readonly IProfileSettingsService _profileSettings;

        public ProfileRepository(IProfileSettingsService profileSettings = null)
        {
            _profileSettings = profileSettings ?? Global.ProfileSettingsServiceInstance;
        }

        public string ProfilesPath
        {
            get
            {
                string baseDir = !string.IsNullOrEmpty(Global.appdatapath)
                    ? Global.appdatapath
                    : AppContext.BaseDirectory;
                string path = Path.Combine(baseDir, "Profiles");
                if (!Directory.Exists(path))
                {
                    try
                    {
                        Directory.CreateDirectory(path);
                    }
                    catch
                    {
                        // フォールバック
                    }
                }
                return path;
            }
        }

        public string GetProfilePath(string profileName)
        {
            if (string.IsNullOrWhiteSpace(profileName))
                return string.Empty;

            if (!profileName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                profileName += ".xml";

            return Path.Combine(ProfilesPath, profileName);
        }

        public bool ProfileExists(string profileName)
        {
            string path = GetProfilePath(profileName);
            return !string.IsNullOrEmpty(path) && File.Exists(path);
        }

        public IReadOnlyList<string> GetProfileNames()
        {
            lock (_fileLock)
            {
                try
                {
                    if (!Directory.Exists(ProfilesPath))
                        return Array.Empty<string>();

                    var files = Directory.GetFiles(ProfilesPath, "*.xml");
                    return files.Select(f => Path.GetFileNameWithoutExtension(f)).ToList();
                }
                catch
                {
                    return Array.Empty<string>();
                }
            }
        }

        public bool LoadProfile(int deviceIndex, string profileName)
        {
            lock (_fileLock)
            {
                try
                {
                    string path = GetProfilePath(profileName);
                    if (!File.Exists(path))
                        return false;

                    string normalizedName = Path.GetFileNameWithoutExtension(profileName);
                    Global.ProfilePath[deviceIndex] = normalizedName;
                    ControlService control = DS4WinWPF.AppHost.GetService<ControlService>();
                    if (control == null)
                        return false;

                    Global.LoadProfile(deviceIndex, false, control, false);
                    if (AppLogger.IsTraceEnabled)
                        AppLogger.LogTrace($"[DI] ProfileRepository.LoadProfile: Slot {deviceIndex}, Profile '{profileName}' loaded via DI");
                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool SaveProfile(int deviceIndex, string profileName)
        {
            lock (_fileLock)
            {
                try
                {
                    string path = GetProfilePath(profileName);
                    if (string.IsNullOrEmpty(path))
                        return false;

                    Global.SaveProfile(deviceIndex, profileName);
                    if (AppLogger.IsTraceEnabled)
                        AppLogger.LogTrace($"[DI] ProfileRepository.SaveProfile: Slot {deviceIndex}, Profile '{profileName}' saved via DI");
                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool LoadDefaultProfile(int deviceIndex)
        {
            _profileSettings?.ResetToDefaults(deviceIndex);
            if (AppLogger.IsTraceEnabled)
                AppLogger.LogTrace($"[DI] ProfileRepository.LoadDefaultProfile: Slot {deviceIndex} reset to defaults via DI");
            return true;
        }

        public bool LoadProfileToSlot(int deviceIndex, string profileName)
        {
            if (deviceIndex < 0 || deviceIndex >= ProfileSettingsService.TEST_PROFILE_ITEM_COUNT)
                return false;

            if (string.IsNullOrWhiteSpace(profileName))
            {
                return LoadDefaultProfile(deviceIndex);
            }

            return LoadProfile(deviceIndex, profileName);
        }

        public bool ApplyProfileDirect(int deviceIndex, string profileName)
        {
            if (deviceIndex < 0 || deviceIndex >= ProfileSettingsService.MAX_DS4_CONTROLLER_COUNT)
                return false;

            _profileSettings?.SetUseTempProfile(deviceIndex, true);
            _profileSettings?.SetTempProfileName(deviceIndex, profileName);
            if (AppLogger.IsTraceEnabled)
                AppLogger.LogTrace($"[DI] ProfileRepository.ApplyProfileDirect: Slot {deviceIndex}, Temp Profile '{profileName}' applied via DI");
            return LoadProfile(deviceIndex, profileName);
        }

        public bool RestoreProfileDirect(int deviceIndex)
        {
            if (deviceIndex < 0 || deviceIndex >= ProfileSettingsService.MAX_DS4_CONTROLLER_COUNT)
                return false;

            _profileSettings?.SetUseTempProfile(deviceIndex, false);
            _profileSettings?.SetTempProfileName(deviceIndex, string.Empty);
            if (AppLogger.IsTraceEnabled)
                AppLogger.LogTrace($"[DI] ProfileRepository.RestoreProfileDirect: Slot {deviceIndex} restored via DI");
            return LoadProfile(deviceIndex, string.Empty);
        }
    }
}
