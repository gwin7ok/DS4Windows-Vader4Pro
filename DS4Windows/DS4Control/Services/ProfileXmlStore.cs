using System;
using DS4Windows.DI;

namespace DS4Windows
{
    /// <summary>
    /// IProfileXmlStore の実装。BackingStore.LoadProfile/SaveProfile への薄い委譲ラッパー。
    /// Phase5-Plan Section5.1 / Phase5-Step2-Plan Section1.6 ガードレール対応:
    /// BackingStoreのm_Xdoc(XmlDocument)はLoadProfile/SaveProfile/Save/Load(AppSettings)間で
    /// 共有される単一インスタンスでありスレッドセーフでないため、同一XMLファイルI/O競合および
    /// ロストアップデートを防止する目的でプロセス内排他ロックにより直列化する。
    /// Step6(AppSettingsService)新設時にも同一ロックを共有できるよう public static で公開する。
    /// </summary>
    public class ProfileXmlStore : IProfileXmlStore
    {
        public static readonly object XmlIoLock = new object();

        private readonly BackingStore _backingStore;

        public ProfileXmlStore(BackingStore backingStore = null)
        {
            _backingStore = backingStore ?? Global.store;
        }

        public bool LoadProfileXml(int deviceIndex, bool launchProgram, ControlService control,
            string overridePath = "", bool xinputChange = true, bool postLoad = true)
        {
            lock (XmlIoLock)
            {
                bool result = _backingStore.LoadProfile(deviceIndex, launchProgram, control, overridePath, xinputChange, postLoad);
                if (AppLogger.IsTraceEnabled)
                    AppLogger.LogTrace($"[DI] ProfileXmlStore.LoadProfileXml: Slot {deviceIndex}, result={result}");
                return result;
            }
        }

        public bool SaveProfileXml(int deviceIndex, string profileName)
        {
            lock (XmlIoLock)
            {
                bool result = _backingStore.SaveProfile(deviceIndex, profileName);
                if (AppLogger.IsTraceEnabled)
                    AppLogger.LogTrace($"[DI] ProfileXmlStore.SaveProfileXml: Slot {deviceIndex}, Profile '{profileName}', result={result}");
                return result;
            }
        }
    }
}