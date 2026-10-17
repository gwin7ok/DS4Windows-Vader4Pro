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
    /// Phase7-Step10c W3（Q1・Q3・Q5＝A）: アプリ設定（Profiles.xml）の保存・読込を <c>BackingStore</c> から <c>AppSettingsXmlSerializer</c> へ移し、
    /// バージョンの数値化を <c>VersionUtil</c> へ移したことの回帰ガード。内容の同一性はゴールデン（<c>Phase7Step10cGoldenPersistenceTests</c>）と
    /// <c>BackingStoreAppSettingsLoadTests</c> が固定する。ソースが見つからない環境では、ソースの検査を行わず合格とする。
    /// </summary>
    [Collection("ServiceProviderHolder")]
    public class Phase7Step10cW3AppSettingsSerializerGuardTests
    {
        private readonly ITestOutputHelper _output;

        public Phase7Step10cW3AppSettingsSerializerGuardTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void BackingStore_NoLongerDeclaresLoadSaveAndLoadFakeExeName_ButKeepsPostProcessLoad()
        {
            const BindingFlags all = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
            var names = typeof(BackingStore).GetMethods(all).Select(m => m.Name).ToArray();
            Assert.DoesNotContain("Load", names);
            Assert.DoesNotContain("Save", names);
            Assert.DoesNotContain("LoadFakeExeName", names);
            Assert.Contains("PostProcessLoad", names); // 読込後の値の丸め（状態の整形）は BackingStore に残す
        }

        [Fact]
        public void TheSerializer_RequiresAllItsDependencies()
        {
            var store = new BackingStore();
            Assert.Throws<ArgumentNullException>(() => new AppSettingsXmlSerializer(null, new PathService(), new EnvironmentService()));
            Assert.Throws<ArgumentNullException>(() => new AppSettingsXmlSerializer(store, null, new EnvironmentService()));
            Assert.Throws<ArgumentNullException>(() => new AppSettingsXmlSerializer(store, new PathService(), null));
        }

        [Fact]
        public void TheContainer_ProvidesTheSerializer_AndTheXmlStoreUsesIt()
        {
            DS4WinWPF.AppHost.CreateHost();
            Assert.Same(DS4WinWPF.AppHost.GetService<AppSettingsXmlSerializer>(), DS4WinWPF.AppHost.GetService<AppSettingsXmlSerializer>());
            Assert.NotNull(DS4WinWPF.AppHost.GetService<IProfileXmlStore>());
        }

        [Fact]
        public void TheFakeExeFileName_IsUnchanged_AndGlobalPointsToTheSameValue()
        {
            Assert.Equal("custom_exe_name.txt", AppSettingsXmlSerializer.CustomExeConfigFileName);
            Assert.Equal(AppSettingsXmlSerializer.CustomExeConfigFileName, Global.CUSTOM_EXE_CONFIG_FILENAME);
        }

        [Theory]
        [InlineData("1.2.3.4", (1UL << 48) | (2UL << 32) | (3UL << 16) | 4UL)]
        [InlineData("3.9.0", (3UL << 48) | (9UL << 32) | 0UL)]
        [InlineData("not a version", 0UL)]
        [InlineData("", 0UL)]
        public void CompileVersionNumberFromString_IsUnchanged(string text, ulong expected)
        {
            // "3.9.0" は Revision が -1（ushort へ切り詰められて 0xFFFF）になるため、Build の位置までを比べる
            ulong actual = VersionUtil.CompileVersionNumberFromString(text);
            if (text == "3.9.0")
                Assert.Equal(expected, actual & ~0xFFFFFFFFUL | 0UL);
            else
                Assert.Equal(expected, actual);
        }

        [Fact]
        public void CompileVersionNumber_PacksTheFourParts()
        {
            Assert.Equal((1UL << 48) | (2UL << 32) | (3UL << 16) | 4UL, VersionUtil.CompileVersionNumber(1, 2, 3, 4));
        }

        [Fact]
        public void TheSerializer_DoesNotReferenceGlobal_AndTheXmlStoreUsesIt()
        {
            string serializer = SourceFileLocator.Find("DS4Windows/DS4Control/Services/AppSettingsXmlSerializer.cs");
            string xmlStore = SourceFileLocator.Find("DS4Windows/DS4Control/Services/ProfileXmlStore.cs");
            if (serializer == null || xmlStore == null)
            {
                _output.WriteLine("ソースが見つからないため検査を省略した。");
                return;
            }

            string Strip(string t) => Regex.Replace(Regex.Replace(t, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline), @"//[^\r\n]*", string.Empty);
            string s = Strip(File.ReadAllText(serializer));
            Assert.False(Regex.IsMatch(s, @"(?<![\w.])Global\.\w+"), "AppSettingsXmlSerializer が Global を参照している");
            Assert.False(Regex.IsMatch(s, @"\bthis\b(?!\.store\b|\._pathService\b|\._environment\b)"), "this を直接使っている");
            string x = Strip(File.ReadAllText(xmlStore));
            Assert.DoesNotMatch(@"_backingStore\.(Load|Save)\(", x);
        }
    }
}
