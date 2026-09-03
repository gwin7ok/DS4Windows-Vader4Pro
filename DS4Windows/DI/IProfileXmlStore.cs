using DS4Windows;

namespace DS4Windows.DI
{
    public interface IProfileXmlStore
    {
        bool LoadProfileXml(int deviceIndex, bool launchProgram, ControlService control,
            string overridePath = "", bool xinputChange = true, bool postLoad = true);
        bool SaveProfileXml(int deviceIndex, string profileName);

        /// <summary>
        /// アプリ全体設定（Profiles.xml）を読み込みます（同一XML排他ロック XmlIoLock で保護）。
        /// </summary>
        bool LoadAppSettingsXml();

        /// <summary>
        /// アプリ全体設定（Profiles.xml）を保存します（同一XML排他ロック XmlIoLock で保護）。
        /// </summary>
        bool SaveAppSettingsXml();
    }
}
