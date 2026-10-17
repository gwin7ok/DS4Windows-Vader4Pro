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
    /// Phase7-Step10c W4（Q1・Q3・Q5＝A）: プロファイル（旧形式の XML）の読込を <c>BackingStore</c> から <c>LegacyProfileXmlSerializer</c> へ移したことの回帰ガード。
    /// （計画書では W4＝保存側、W5＝読込側だったが、<c>LoadProfile</c> が <c>SaveProfile</c> を呼ぶため、逆向きの依存を作らないよう入れ替えた。）
    /// 読込後の状態の同一性は <c>Phase7Step10cGoldenPersistenceTests</c>（プロファイル・プリセット）が固定する。
    /// ソースが見つからない環境では、ソースの検査を行わず合格とする。
    /// </summary>
    [Collection("ServiceProviderHolder")]
    public class Phase7Step10cW4LegacyProfileLoadGuardTests
    {
        private static readonly string[] MovedMethods =
        {
            "LoadProfile", "LoadLegacyExtraSettings", "ReadLegacyElement", "axisOutputCurveId", "OutContDeviceId", "GetLightbarModeType", "PortOldGyroSettings",
        };

        private readonly ITestOutputHelper _output;

        public Phase7Step10cW4LegacyProfileLoadGuardTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void BackingStore_NoLongerDeclaresTheMovedMethods_ButKeepsTheSharedHelpersPublic()
        {
            const BindingFlags all = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
            var names = typeof(BackingStore).GetMethods(all).Select(m => m.Name).ToArray();
            foreach (string moved in MovedMethods)
                Assert.DoesNotContain(moved, names);

            // プリセット（W6 まで BackingStore）と読込の両方が使う補助は、BackingStore に残し、公開にした
            foreach (string kept in new[] { "ResetProfile", "ResetMouseProperties", "PostLoadSnippet", "CheckOldDevicestatus" })
            {
                MethodInfo m = typeof(BackingStore).GetMethod(kept, BindingFlags.Instance | BindingFlags.Public);
                Assert.NotNull(m);
            }
        }

        [Fact]
        public void TheSerializer_RequiresAllItsDependencies()
        {
            var store = new BackingStore();
            var settings = Global.ProfileSettingsServiceInstance;
            Assert.Throws<ArgumentNullException>(() => new LegacyProfileXmlSerializer(null, new PathService(), settings, new EnvironmentService()));
            Assert.Throws<ArgumentNullException>(() => new LegacyProfileXmlSerializer(store, null, settings, new EnvironmentService()));
            Assert.Throws<ArgumentNullException>(() => new LegacyProfileXmlSerializer(store, new PathService(), null, new EnvironmentService()));
            Assert.Throws<ArgumentNullException>(() => new LegacyProfileXmlSerializer(store, new PathService(), settings, null));
        }

        [Fact]
        public void TheContainer_ProvidesTheSerializer_ToTheXmlStoreAndTheRepository()
        {
            DS4WinWPF.AppHost.CreateHost();
            Assert.Same(DS4WinWPF.AppHost.GetService<LegacyProfileXmlSerializer>(), DS4WinWPF.AppHost.GetService<LegacyProfileXmlSerializer>());
            Assert.NotNull(DS4WinWPF.AppHost.GetService<IProfileXmlStore>());
            Assert.NotNull(DS4WinWPF.AppHost.GetService<IProfileRepository>());
        }

        [Fact]
        public void TheSerializer_DoesNotReferenceGlobal_AndTheCallersUseIt()
        {
            string serializer = SourceFileLocator.Find("DS4Windows/DS4Control/Services/LegacyProfileXmlSerializer.cs");
            string xmlStore = SourceFileLocator.Find("DS4Windows/DS4Control/Services/ProfileXmlStore.cs");
            string repository = SourceFileLocator.Find("DS4Windows/DS4Control/Services/ProfileRepository.cs");
            if (serializer == null || xmlStore == null || repository == null)
            {
                _output.WriteLine("ソースが見つからないため検査を省略した。");
                return;
            }

            string Strip(string t) => Regex.Replace(Regex.Replace(t, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline), @"//[^\r\n]*", string.Empty);
            string s = Strip(File.ReadAllText(serializer));
            Assert.False(Regex.IsMatch(s, @"(?<![\w.])Global\.\w+"), "LegacyProfileXmlSerializer が Global を参照している");
            Assert.False(Regex.IsMatch(s, @"\bthis\b(?!\.store\b|\._pathService\b|\._profileSettings\b)"), "this を直接使っている");
            Assert.DoesNotMatch(@"_backingStore\.LoadProfile\(", Strip(File.ReadAllText(xmlStore)));
            Assert.DoesNotMatch(@"_config\.LoadProfile\(", Strip(File.ReadAllText(repository)));
        }
    }
}
