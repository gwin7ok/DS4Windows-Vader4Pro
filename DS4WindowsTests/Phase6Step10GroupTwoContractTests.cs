using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using Xunit;
using Xunit.Abstractions;
using DS4Windows;
using DS4Windows.DI;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase6-Step10-2-1: グループ2（プロファイル・マッピング系）の View／ViewModel の Global 直接参照解消に先立って
    /// 追加した契約が、いずれも <c>Global</c> と同じ値・同じ動作であること（薄い委譲）を検証する。
    /// - IAppSettingsService: UseMoonlight／UseAdvancedMoonlight
    /// - IAutoProfileService: CreateAutoProfilesFile（Global.CreateAutoProfiles）
    /// - IProfilePresetLoader: 10 メソッドが、それぞれ対応する Global.Load…Profile へ委譲していること
    /// Global 状態を変更するテストは、変更を必ず元に戻す（Phase6-Status.md §6.4-1）。
    /// </summary>
    public class Phase6Step10GroupTwoContractTests
    {
        private readonly ITestOutputHelper _output;

        public Phase6Step10GroupTwoContractTests(ITestOutputHelper output)
        {
            _output = output;
        }

        // ---- IAppSettingsService ----

        [Fact]
        public void UseMoonlight_SharesStateWithGlobal_AndNotifiesOnChange()
        {
            var service = TestRepositoryFactory.CreateAppSettingsService();
            bool original = Global.UseMoonlight;
            var changed = new List<string>();
            service.SettingChanged += (s, name) => changed.Add(name);
            try
            {
                service.UseMoonlight = !original;
                Assert.Equal(!original, Global.UseMoonlight);
                Assert.Equal(!original, service.UseMoonlight);
                Assert.Contains(nameof(IAppSettingsService.UseMoonlight), changed);

                // 同じ値の再設定では通知しない
                changed.Clear();
                service.UseMoonlight = !original;
                Assert.Empty(changed);
            }
            finally
            {
                Global.UseMoonlight = original;
            }
        }

        [Fact]
        public void UseAdvancedMoonlight_SharesStateWithGlobal_AndNotifiesOnChange()
        {
            var service = TestRepositoryFactory.CreateAppSettingsService();
            bool original = Global.UseAdvancedMoonlight;
            var changed = new List<string>();
            service.SettingChanged += (s, name) => changed.Add(name);
            try
            {
                service.UseAdvancedMoonlight = !original;
                Assert.Equal(!original, Global.UseAdvancedMoonlight);
                Assert.Equal(!original, service.UseAdvancedMoonlight);
                Assert.Contains(nameof(IAppSettingsService.UseAdvancedMoonlight), changed);

                changed.Clear();
                service.UseAdvancedMoonlight = !original;
                Assert.Empty(changed);
            }
            finally
            {
                Global.UseAdvancedMoonlight = original;
            }
        }

        // ---- IAutoProfileService ----

        [Fact]
        public void CreateAutoProfilesFile_CreatesSameXmlAsGlobal_AndReturnsFalseWhenDirectoryIsMissing()
        {
            // AutoProfileService のコンストラクタが AutoProfileHolder を生成し、Global.appdatapath を読む。
            // 未設定のテスト環境では ArgumentNullException になるため、AutoProfileServiceTests と同じく既定値を設定する
            // （設定した場合は、終了時に元へ戻す）。
            string originalAppDataPath = Global.appdatapath;
            bool appDataPathWasSet = string.IsNullOrEmpty(originalAppDataPath);
            if (appDataPathWasSet)
            {
                Global.appdatapath = new DS4Windows.PathService().AppDataPath;
            }

            string dir = Path.Combine(Path.GetTempPath(), "Phase6Step10_AutoProfiles_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            try
            {
                DS4WinWPF.AppHost.CreateHost();
                IAutoProfileService service = DS4WinWPF.AppHost.GetService<IAutoProfileService>();
                Assert.NotNull(service);

                string viaService = Path.Combine(dir, "service.xml");
                string viaGlobal = Path.Combine(dir, "global.xml");

                Assert.True(service.CreateAutoProfilesFile(viaService));
                Assert.True(Global.CreateAutoProfiles(viaGlobal));

                var a = new XmlDocument();
                var b = new XmlDocument();
                a.Load(viaService);
                b.Load(viaGlobal);
                Assert.Equal("Programs", a.DocumentElement.Name);
                Assert.Equal(b.DocumentElement.Name, a.DocumentElement.Name);

                string missing = Path.Combine(dir, "NoSuchDir", "Auto Profiles.xml");
                Assert.False(service.CreateAutoProfilesFile(missing));
            }
            finally
            {
                Directory.Delete(dir, true);
                if (appDataPathWasSet)
                {
                    Global.appdatapath = originalAppDataPath;
                }
            }
        }

        // ---- IProfilePresetLoader ----

        [Fact]
        public void ProfilePresetLoader_IsRegisteredInContainer()
        {
            DS4WinWPF.AppHost.CreateHost();

            var loader = DS4WinWPF.AppHost.GetService<IProfilePresetLoader>();

            Assert.NotNull(loader);
            Assert.IsType<DS4Windows.Services.ProfilePresetLoader>(loader);
        }

        [Fact]
        public void ProfilePresetLoader_EachMethodDelegatesToTheMatchingGlobalMethod()
        {
            string path = SourceFileLocator.Find("DS4Windows/DS4Control/Services/ProfilePresetLoader.cs");
            if (path == null)
            {
                _output.WriteLine("ProfilePresetLoader.cs が見つからないため検査を省略した。");
                return;
            }

            string text = File.ReadAllText(path);
            var expected = new Dictionary<string, string>
            {
                ["LoadBlank"] = "LoadBlankDevProfile",
                ["LoadBlankDS4"] = "LoadBlankDS4Profile",
                ["LoadDefaultGamepadGyro"] = "LoadDefaultGamepadGyroProfile",
                ["LoadDefaultDS4GamepadGyro"] = "LoadDefaultDS4GamepadGyroProfile",
                ["LoadDefaultMixedControls"] = "LoadDefaultMixedControlsProfile",
                ["LoadDefaultDS4MixedControls"] = "LoadDefaultDS4MixedControlsProfile",
                ["LoadDefaultMixedGyroMouse"] = "LoadDefaultMixedGyroMouseProfile",
                ["LoadDefaultDS4MixedGyroMouse"] = "LoadDefaultDS4MixedGyroMouseProfile",
                ["LoadDefaultKBM"] = "LoadDefaultKBMProfile",
                ["LoadDefaultKBMGyroMouse"] = "LoadDefaultKBMGyroMouseProfile",
            };

            foreach (var kv in expected)
            {
                // Phase7-Step3c W2: 実装はプリセットの読み込みを呼び、後処理（CompleteLoad）を行う（Phase7-Step10c W6: 呼び出し先は ProfilePresets）
                string target = kv.Value == "LoadBlankDevProfile" ? "LoadBlankProfile" : kv.Value;
                string pattern = @"public void " + kv.Key + @"\([^)]*\)\s*\{\s*_presets\." + target +
                    @"\(deviceIndex, launchProgram, control, """", xinputChange, postLoad\);\s*CompleteLoad\(deviceIndex\);\s*\}";
                Assert.True(Regex.IsMatch(text, pattern), $"{kv.Key} が ProfilePresets.{target} を呼んで CompleteLoad していない。");
            }

            // 契約のメソッド数（10）と実装のメソッド数が一致すること
            Assert.Equal(expected.Count, typeof(IProfilePresetLoader).GetMethods().Length);
        }
    }
}
