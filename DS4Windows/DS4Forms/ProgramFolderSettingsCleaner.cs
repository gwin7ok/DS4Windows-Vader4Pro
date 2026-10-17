using System;
using System.IO;

namespace DS4WinWPF.DS4Forms
{
    /// <summary>
    /// 保存先に「Appdata」を選んだとき、プログラムフォルダに残っている設定ファイルを削除する。
    /// Phase6-Step10-1: 置換前は Profiles フォルダ・Profiles.xml・Auto Profiles.xml の 3 つだけを削除しており、
    /// Actions.xml／LinkedProfiles.xml／ControllerConfigs.xml／OutputSlots.xml が残っていた（実機確認で判明）。
    /// 設定フォルダに保存されるアプリ管理のファイルを削除対象に加えた。Macros や Logs などのフォルダには触れない
    /// （ユーザーのマクロや実行中のログを誤って消さないため）。
    /// </summary>
    public static class ProgramFolderSettingsCleaner
    {
        /// <summary>削除するプロファイルのフォルダ名。</summary>
        public const string ProfilesDirectoryName = "Profiles";

        /// <summary>削除する設定ファイル名（設定フォルダ直下。Global.SaveWhere が決めるパスと OutputSlotPersist のファイル名に対応）。</summary>
        public static readonly string[] SettingFileNames =
        {
            "Profiles.xml",
            "Auto Profiles.xml",
            "Actions.xml",
            "LinkedProfiles.xml",
            "ControllerConfigs.xml",
            "OutputSlots.xml",
        };

        /// <summary>
        /// 指定フォルダの設定ファイルを削除する。存在しない項目はスキップする。
        /// 1 つの削除に失敗しても、残りの項目の削除を続ける。
        /// </summary>
        /// <returns>すべて削除できたか（権限不足・使用中などで 1 つでも削除できなければ false）。</returns>
        public static bool DeleteSettings(string directory)
        {
            bool allDeleted = true;

            try
            {
                string profilesDir = Path.Combine(directory, ProfilesDirectoryName);
                if (Directory.Exists(profilesDir))
                {
                    Directory.Delete(profilesDir, true);
                }
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException || ex is IOException)
            {
                allDeleted = false;
            }

            foreach (string fileName in SettingFileNames)
            {
                try
                {
                    string path = Path.Combine(directory, fileName);
                    if (File.Exists(path))
                    {
                        File.Delete(path);
                    }
                }
                catch (Exception ex) when (ex is UnauthorizedAccessException || ex is IOException)
                {
                    allDeleted = false;
                }
            }

            return allDeleted;
        }
    }
}
