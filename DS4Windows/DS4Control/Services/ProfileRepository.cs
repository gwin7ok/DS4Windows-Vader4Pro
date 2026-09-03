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
        private readonly IProfileXmlStore _profileXmlStore;
        private IPathService _pathService;

        public ProfileRepository(IProfileSettingsService profileSettings = null,
            IProfileXmlStore profileXmlStore = null, IPathService pathService = null)
        {
            _profileSettings = profileSettings ?? Global.ProfileSettingsServiceInstance;
            _profileXmlStore = profileXmlStore ?? Global.ProfileXmlStoreInstance;
            // Phase5-Step2: IPathServiceは静的フィールド初期化順序ハザード回避のため
            // コンストラクタでは解決せず、初回プロパティアクセス時に遅延解決する
            _pathService = pathService;
        }

        private IPathService PathSvc => _pathService ??= Global.PathServiceInstance;

        public string ProfilesPath => PathSvc.ProfilesPath;

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

                    // Phase5-Step2: Global.LoadProfileへの委譲をやめ、XML I/O(IProfileXmlStore)と
                    // 状態調整ロジック(旧Global.LoadProfile内で行っていたもの)をここに直接内包する。
                    // 挙動はGlobal.LoadProfileと完全に同一に維持する。
                    Global.loggedInvalidActions.Clear();
                    _profileXmlStore.LoadProfileXml(deviceIndex, false, control, "", false, true);
                    _profileSettings?.SetTempProfileName(deviceIndex, string.Empty);
                    _profileSettings?.SetUseTempProfile(deviceIndex, false);
                    _profileSettings?.SetTempProfileDistance(deviceIndex, false);

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

                    // Phase5-Step2: IProfileXmlStore.SaveProfileXmlの成否(bool)をそのまま呼び出し元へ伝播する
                    // (従来はGlobal.SaveProfileがvoidで成否を握りつぶしていたため常にtrueを返していた)
                    bool saveSuccess = _profileXmlStore.SaveProfileXml(deviceIndex, profileName);
                    if (AppLogger.IsTraceEnabled)
                        AppLogger.LogTrace($"[DI] ProfileRepository.SaveProfile: Slot {deviceIndex}, Profile '{profileName}' saved via DI, success={saveSuccess}");
                    return saveSuccess;
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