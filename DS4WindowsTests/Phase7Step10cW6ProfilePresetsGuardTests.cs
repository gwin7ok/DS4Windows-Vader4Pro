using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using DS4Windows;
using DS4Windows.DI;
using DS4Windows.Services;
using Xunit;
using Xunit.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase7-Step10c W6（Q1・Q6＝A）: プロファイルのプリセット（空のプロファイルと既定の割り当て）の読込を <c>BackingStore</c> から
    /// <c>ProfilePresets</c> へ移したことの回帰ガード。読込後の状態の同一性は <c>Phase7Step10cGoldenPersistenceTests.Presets_LoadedState_MatchesGolden</c>
    /// （10 種）が固定する。ソースが見つからない環境では、ソースの検査を行わず合格とする。
    /// </summary>
    [Collection("ServiceProviderHolder")]
    public class Phase7Step10cW6ProfilePresetsGuardTests
    {
        private static readonly string[] MovedMethods =
        {
            "LoadBlankDS4Profile", "LoadBlankProfile", "LoadDefaultGamepadGyroProfile", "LoadDefaultDS4GamepadGyroProfile",
            "LoadDefaultMixedGyroMouseProfile", "LoadDefaultDS4MixedGyroMouseProfile", "LoadDefaultDS4MixedControlsProfile",
            "LoadDefaultMixedControlsProfile", "LoadDefaultKBMProfile", "LoadDefaultKBMGyroMouseProfile", "PrepareBlankingProfile",
        };

        private readonly ITestOutputHelper _output;

        public Phase7Step10cW6ProfilePresetsGuardTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void BackingStore_NoLongerDeclaresThePresetMethods()
        {
            const BindingFlags all = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
            var names = typeof(BackingStore).GetMethods(all).Select(m => m.Name).ToArray();
            foreach (string moved in MovedMethods)
                Assert.DoesNotContain(moved, names);
        }

        [Fact]
        public void ProfilePresets_HasTheTenPresets_AndRequiresTheStore()
        {
            foreach (string name in MovedMethods.Where(n => n != "PrepareBlankingProfile"))
                Assert.NotNull(typeof(ProfilePresets).GetMethod(name, BindingFlags.Instance | BindingFlags.Public));

            Assert.Throws<ArgumentNullException>(() => new ProfilePresets(null));
        }

        [Fact]
        public void TheLoader_UsesThePresets_AndRequiresThem()
        {
            Assert.Throws<ArgumentNullException>(() => new ProfilePresetLoader(new BackingStore(), Global.ProfileSettingsServiceInstance, null));

            DS4WinWPF.AppHost.CreateHost();
            Assert.Same(DS4WinWPF.AppHost.GetService<ProfilePresets>(), DS4WinWPF.AppHost.GetService<ProfilePresets>());
            Assert.NotNull(DS4WinWPF.AppHost.GetService<IProfilePresetLoader>());
        }

        [Fact]
        public void TheSource_PresetsDoNotReferenceGlobal_AndTheLoaderDoesNotCallTheStore()
        {
            string presets = SourceFileLocator.Find("DS4Windows/DS4Control/Services/ProfilePresets.cs");
            string loader = SourceFileLocator.Find("DS4Windows/DS4Control/Services/ProfilePresetLoader.cs");
            string store = SourceFileLocator.Find("DS4Windows/DS4Control/BackingStore.cs");
            if (presets == null || loader == null || store == null)
            {
                _output.WriteLine("ソースが見つからないため検査を省略した。");
                return;
            }

            string Strip(string t) => Regex.Replace(Regex.Replace(t, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline), @"//[^\r\n]*", string.Empty);
            string p = Strip(File.ReadAllText(presets));
            Assert.False(Regex.IsMatch(p, @"(?<![\w.])Global\.\w+"), "ProfilePresets が Global を参照している");
            Assert.False(Regex.IsMatch(p, @"\bthis\b(?!\.store\b)"), "this を直接使っている");
            Assert.DoesNotMatch(@"_config\.(LoadBlank|LoadDefault)", Strip(File.ReadAllText(loader)));

            // BackingStore は、設定の状態を持つ型に絞られた（永続化とプリセットを持たない）
            int lines = File.ReadAllLines(store).Length;
            Assert.True(lines < 2000, "BackingStore.cs が想定より大きい: " + lines + " 行");
        }
    }
}
