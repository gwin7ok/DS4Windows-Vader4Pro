using System;
using System.IO;
using DS4WinWPF.DS4Forms;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// 保存先に「Appdata」を選んだとき、プログラムフォルダの設定ファイルがすべて削除されること
    /// （実機確認で、Actions.xml／ControllerConfigs.xml／OutputSlots.xml が残ることが判明したため）。
    /// </summary>
    public class ProgramFolderSettingsCleanerTests : IDisposable
    {
        private readonly string dir;

        public ProgramFolderSettingsCleanerTests()
        {
            dir = Path.Combine(Path.GetTempPath(), "ProgramFolderSettingsCleaner_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
        }

        public void Dispose()
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, true);
        }

        [Fact]
        public void DeleteSettings_RemovesProfilesFolderAndAllSettingFiles()
        {
            Directory.CreateDirectory(Path.Combine(dir, "Profiles"));
            File.WriteAllText(Path.Combine(dir, "Profiles", "a.xml"), "x");
            foreach (string name in ProgramFolderSettingsCleaner.SettingFileNames)
            {
                File.WriteAllText(Path.Combine(dir, name), "x");
            }

            bool result = ProgramFolderSettingsCleaner.DeleteSettings(dir);

            Assert.True(result);
            Assert.False(Directory.Exists(Path.Combine(dir, "Profiles")));
            foreach (string name in ProgramFolderSettingsCleaner.SettingFileNames)
            {
                Assert.False(File.Exists(Path.Combine(dir, name)), name);
            }
        }

        [Fact]
        public void DeleteSettings_IncludesFilesThatWereLeftBehindBefore()
        {
            Assert.Contains("Actions.xml", ProgramFolderSettingsCleaner.SettingFileNames);
            Assert.Contains("ControllerConfigs.xml", ProgramFolderSettingsCleaner.SettingFileNames);
            Assert.Contains("OutputSlots.xml", ProgramFolderSettingsCleaner.SettingFileNames);
            Assert.Contains("LinkedProfiles.xml", ProgramFolderSettingsCleaner.SettingFileNames);
        }

        [Fact]
        public void DeleteSettings_DoesNotThrow_WhenProfilesFolderOrFilesAreMissing()
        {
            // 置換前は Profiles フォルダが無いと DirectoryNotFoundException（未捕捉）になっていた。
            File.WriteAllText(Path.Combine(dir, "Actions.xml"), "x");

            bool result = ProgramFolderSettingsCleaner.DeleteSettings(dir);

            Assert.True(result);
            Assert.False(File.Exists(Path.Combine(dir, "Actions.xml")));
        }

        [Fact]
        public void DeleteSettings_KeepsMacrosLogsAndUnrelatedFiles()
        {
            Directory.CreateDirectory(Path.Combine(dir, "Macros"));
            File.WriteAllText(Path.Combine(dir, "Macros", "m.macro"), "x");
            Directory.CreateDirectory(Path.Combine(dir, "Logs"));
            File.WriteAllText(Path.Combine(dir, "other.txt"), "x");

            Assert.True(ProgramFolderSettingsCleaner.DeleteSettings(dir));

            Assert.True(File.Exists(Path.Combine(dir, "Macros", "m.macro")));
            Assert.True(Directory.Exists(Path.Combine(dir, "Logs")));
            Assert.True(File.Exists(Path.Combine(dir, "other.txt")));
        }

        [Fact]
        public void DeleteSettings_ContinuesAndReturnsFalse_WhenOneFileIsLocked()
        {
            string locked = Path.Combine(dir, "Actions.xml");
            string other = Path.Combine(dir, "OutputSlots.xml");
            File.WriteAllText(locked, "x");
            File.WriteAllText(other, "x");

            using (var stream = new FileStream(locked, FileMode.Open, FileAccess.Read, FileShare.None))
            {
                bool result = ProgramFolderSettingsCleaner.DeleteSettings(dir);

                Assert.False(result);
                Assert.True(File.Exists(locked));
                Assert.False(File.Exists(other));
            }
        }
    }
}
