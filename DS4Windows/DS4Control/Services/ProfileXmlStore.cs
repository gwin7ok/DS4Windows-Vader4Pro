using System;
using DS4Windows.DI;

namespace DS4Windows
{
    public class ProfileXmlStore : IProfileXmlStore
    {
        private readonly BackingStore _backingStore;

        // Phase 5 ガードレール: Profiles.xml に対する同一プロセス内の並行アクセス競合を防ぐための静的排他ロックオブジェクト
        // (Step6: AppSettingsService との同一ファイル排他ロック共有)
        public static readonly object XmlIoLock = new object();

        public ProfileXmlStore(BackingStore backingStore = null)
        {
            _backingStore = backingStore ?? Global.store;
        }

        public bool LoadProfileXml(int deviceIndex, bool launchProgram, ControlService control,
            string overridePath = "", bool xinputChange = true, bool postLoad = true)
        {
            lock (XmlIoLock)
            {
                try
                {
                    return _backingStore.LoadProfile(deviceIndex, launchProgram, control,
                        overridePath, xinputChange, postLoad);
                }
                catch (Exception ex)
                {
                    AppLogger.LogToGui($"Failed to load profile XML: {ex.Message}", true);
                    return false;
                }
            }
        }

        public bool SaveProfileXml(int deviceIndex, string profileName)
        {
            lock (XmlIoLock)
            {
                try
                {
                    return _backingStore.SaveProfile(deviceIndex, profileName);
                }
                catch (Exception ex)
                {
                    AppLogger.LogToGui($"Failed to save profile XML: {ex.Message}", true);
                    return false;
                }
            }
        }

        public bool LoadAppSettingsXml()
        {
            lock (XmlIoLock)
            {
                try
                {
                    bool result = _backingStore.Load();
                    if (AppLogger.IsTraceEnabled)
                        AppLogger.LogTrace($"[DI] ProfileXmlStore.LoadAppSettingsXml: loaded={result}");
                    return result;
                }
                catch (Exception ex)
                {
                    AppLogger.LogToGui($"Failed to load application settings XML: {ex.Message}", true);
                    if (AppLogger.IsTraceEnabled)
                        AppLogger.LogTrace($"[DI] ProfileXmlStore.LoadAppSettingsXml failed: {ex}");
                    return false;
                }
            }
        }

        public bool SaveAppSettingsXml()
        {
            lock (XmlIoLock)
            {
                try
                {
                    bool result = _backingStore.Save();
                    if (AppLogger.IsTraceEnabled)
                        AppLogger.LogTrace($"[DI] ProfileXmlStore.SaveAppSettingsXml: saved={result}");
                    return result;
                }
                catch (Exception ex)
                {
                    AppLogger.LogToGui($"Failed to save application settings XML: {ex.Message}", true);
                    if (AppLogger.IsTraceEnabled)
                        AppLogger.LogTrace($"[DI] ProfileXmlStore.SaveAppSettingsXml failed: {ex}");
                    return false;
                }
            }
        }
    }
}
