using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;
using DS4Windows;
using DS4Windows.DI;
using DS4Windows.Services;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase6-Step7-1: `App.xaml.cs`（Post-Host）の Global 直接参照解消に先立って既存サービスへ追加した契約
    /// （<see cref="IPathService.RoamingAppDataPath"/>／<see cref="IPathService.HasMultipleSaveLocations"/>、
    /// <see cref="IAppSettingsService.UseLang"/>、<see cref="IDeviceStateService.ResetConnectionFlags"/>、
    /// <see cref="IProfileRepository.SaveAsProfile"/>／<see cref="IProfileRepository.LoadLinkedProfiles"/>、
    /// <see cref="ISpecialActionRepository.CreateStandardActions"/>）が Global と同じ実体を扱うこと、
    /// および決定2（L2）で是正した <see cref="SpecialActionRepository.LoadActions"/> が、
    /// Actions.xml がないときに Global.LoadActions と同じく既定アクションを作ることを検証する。
    /// 各テストは変更した Global 状態を必ず元に戻す（Phase6-Step2 の教訓）。
    /// </summary>
    public class Phase6Step7ContractExtensionTests
    {
        private readonly ITestOutputHelper _output;

        public Phase6Step7ContractExtensionTests(ITestOutputHelper output)
        {
            _output = output;
        }

        // ---- IPathService ----

        [Fact]
        public void RoamingAppDataPath_ReturnsGlobalAppDataPpath()
        {
            var pathService = new PathService();
            Assert.Equal(Global.appDataPpath, pathService.RoamingAppDataPath);
        }

        [Fact]
        public void HasMultipleSaveLocations_FollowsGlobalMultisavespots()
        {
            var pathService = new PathService();
            bool original = Global.multisavespots;
            try
            {
                Global.multisavespots = true;
                Assert.True(pathService.HasMultipleSaveLocations);

                Global.multisavespots = false;
                Assert.False(pathService.HasMultipleSaveLocations);
            }
            finally
            {
                Global.multisavespots = original;
            }
        }

        // ---- IAppSettingsService.UseLang ----

        [Fact]
        public void UseLang_SharesStateWithGlobal_AndNotifiesOnChange()
        {
            var settings = new AppSettingsService();
            string original = Global.UseLang;
            try
            {
                Global.UseLang = "en";
                Assert.Equal("en", settings.UseLang);

                string notified = null;
                settings.SettingChanged += (s, name) => notified = name;

                settings.UseLang = "ja";
                Assert.Equal("ja", Global.UseLang);
                Assert.Equal(nameof(IAppSettingsService.UseLang), notified);

                // 同じ値の再設定では通知しない（他のプロパティと同じ規約）。
                notified = null;
                settings.UseLang = "ja";
                Assert.Null(notified);
            }
            finally
            {
                Global.UseLang = original;
            }
        }

        // ---- IDeviceStateService.ResetConnectionFlags ----

        [Fact]
        public void ResetConnectionFlags_SetsAllSlotsToFirstConnection()
        {
            var deviceState = new DeviceStateService();
            bool[] flags = Global.store.firstConnectionAfterStartup;
            bool[] original = (bool[])flags.Clone();
            try
            {
                deviceState.MarkConnected(0);
                deviceState.MarkConnected(3);
                Assert.False(deviceState.IsFirstConnection(0));
                Assert.False(deviceState.IsFirstConnection(3));

                deviceState.ResetConnectionFlags();

                for (int i = 0; i < Global.TEST_PROFILE_ITEM_COUNT; i++)
                {
                    Assert.True(deviceState.IsFirstConnection(i));
                    Assert.True(Global.IsFirstConnection(i));
                }
            }
            finally
            {
                Array.Copy(original, flags, original.Length);
            }
        }

        // ---- ISpecialActionRepository.LoadActions（決定2＝L2 の是正）----

        [Fact]
        public void LoadActions_WhenActionsFileMissing_CreatesDefaultActionLikeGlobal()
        {
            string dir = Path.Combine(Path.GetTempPath(), "DS4WinStep7_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            try
            {
                string actionsFile = Path.Combine(dir, "Actions.xml");
                var store = new BackingStore { m_Actions = actionsFile };
                var repo = new SpecialActionRepository(store);

                Assert.False(File.Exists(actionsFile));

                bool result = repo.LoadActions();

                // BackingStore.LoadActions（＝Global.LoadActions の実体）と同じく、既定アクションを作って保存し true を返す。
                // 是正前は File.Exists の判定で何もせず false を返していた。
                Assert.True(result);
                Assert.True(File.Exists(actionsFile));
                Assert.Contains(store.actions, a => a.name == "Disconnect Controller");

                // 作られたファイルを、別の BackingStore から読み込めること。
                var store2 = new BackingStore { m_Actions = actionsFile };
                var repo2 = new SpecialActionRepository(store2);
                Assert.True(repo2.LoadActions());
                Assert.Contains(store2.actions, a => a.name == "Disconnect Controller");
            }
            finally
            {
                try { Directory.Delete(dir, true); } catch { }
            }
        }

        // ---- 実ファイルを書き換える委譲（SaveAsProfile／LoadLinkedProfiles／CreateStandardActions）はソース走査で確認 ----
        // Step3-1 の SaveControllerConfigsForDevice と同じ扱い。挙動は実機確認（Phase6-Step7-Plan.md §6）で担保する。

        private string ReadSourceWithoutComments(string relativePath)
        {
            string path = SourceFileLocator.Find(relativePath);
            if (path == null)
            {
                _output.WriteLine($"{relativePath} が見つからないため検査を省略した。");
                return null;
            }

            string text = File.ReadAllText(path);
            text = Regex.Replace(text, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);
            text = Regex.Replace(text, @"//[^\r\n]*", string.Empty);
            return text;
        }

        [Fact]
        public void ProfileRepository_NewMembers_DelegateToGlobal()
        {
            string text = ReadSourceWithoutComments("DS4Windows/DS4Control/Services/ProfileRepository.cs");
            if (text == null) return;

            Assert.Matches(@"public bool SaveAsProfile\(int deviceIndex, string profileName\)\s*=>\s*Global\.store\.SaveAsProfile\(deviceIndex, profileName\);", text);
            Assert.Matches(@"public bool LoadLinkedProfiles\(\)\s*=>\s*Global\.LoadLinkedProfiles\(\);", text);
        }

        [Fact]
        public void SpecialActionRepository_CreateStandardActions_DelegatesToGlobal_AndLoadActionsHasNoFileExistsShortcut()
        {
            string text = ReadSourceWithoutComments("DS4Windows/DS4Control/Services/SpecialActionRepository.cs");
            if (text == null) return;

            Assert.Matches(@"public void CreateStandardActions\(\)\s*\{\s*Global\.CreateStdActions\(\);", text);

            // LoadActions の本体に、Actions.xml の有無で早期に false を返す判定が再混入していないこと。
            Match m = Regex.Match(text, @"public bool LoadActions\(\)(?<body>.*?)public bool SaveActions\(\)", RegexOptions.Singleline);
            Assert.True(m.Success, "LoadActions の本体を特定できなかった。");
            Assert.DoesNotContain("File.Exists", m.Groups["body"].Value);
        }
    }
}
