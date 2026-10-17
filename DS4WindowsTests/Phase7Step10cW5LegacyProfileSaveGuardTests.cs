using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using DS4Windows;
using DS4Windows.Services;
using Xunit;
using Xunit.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase7-Step10c W5（Q1・Q3・Q5＝A）: プロファイル（旧形式の XML）の保存を <c>BackingStore</c> から <c>LegacyProfileXmlSerializer</c> へ移したことの回帰ガード。
    /// 保存結果の同一性は <c>Phase7Step10cGoldenPersistenceTests</c>（プロファイルの既定／変化させた状態）と
    /// <c>ProfileLegacyXmlCompletenessTests</c> が固定する。ソースが見つからない環境では、ソースの検査を行わず合格とする。
    /// </summary>
    [Collection("ServiceProviderHolder")]
    public class Phase7Step10cW5LegacyProfileSaveGuardTests
    {
        private static readonly string[] MovedMethods =
        {
            "SaveAsProfile", "SaveProfile", "SaveLegacyExtraSettings", "AppendLegacyElement",
            "axisOutputCurveString", "OutContDeviceString", "GetLightbarModeString",
        };

        private readonly ITestOutputHelper _output;

        public Phase7Step10cW5LegacyProfileSaveGuardTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void BackingStore_NoLongerDeclaresTheMovedMethods_NorTheWorkingXmlDocument()
        {
            const BindingFlags all = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
            var names = typeof(BackingStore).GetMethods(all).Select(m => m.Name).ToArray();
            foreach (string moved in MovedMethods)
                Assert.DoesNotContain(moved, names);

            Assert.Null(typeof(BackingStore).GetField("m_Xdoc", all));
        }

        [Fact]
        public void TheSerializer_HasTheProfileMethods_AndRequiresTheEnvironment()
        {
            foreach (string name in new[] { "SaveProfile", "SaveAsProfile", "LoadProfile", "SaveLegacyExtraSettings", "LoadLegacyExtraSettings" })
                Assert.NotNull(typeof(LegacyProfileXmlSerializer).GetMethod(name, BindingFlags.Instance | BindingFlags.Public));

            var store = new BackingStore();
            Assert.Throws<ArgumentNullException>(() => new LegacyProfileXmlSerializer(store, new PathService(), Global.ProfileSettingsServiceInstance, null));
        }

        [Fact]
        public void TheConstants_AreUnchanged_AndGlobalPointsToTheSameValues()
        {
            Assert.Equal(5, LegacyProfileXmlSerializer.ConfigVersion);
            Assert.Equal(".xml", LegacyProfileXmlSerializer.XmlExtension);
            Assert.Equal(LegacyProfileXmlSerializer.ConfigVersion, Global.CONFIG_VERSION);
            Assert.Equal(LegacyProfileXmlSerializer.XmlExtension, Global.XML_EXTENSION);
        }

        [Fact]
        public void TheSource_UsesTheSerializerEverywhere()
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
            Assert.DoesNotMatch(@"store\.SaveProfile\(", s); // 同じクラスの SaveProfile を直接呼ぶ（BackingStore へ戻らない）
            Assert.DoesNotMatch(@"_backingStore\.SaveProfile\(", Strip(File.ReadAllText(xmlStore)));
            Assert.DoesNotMatch(@"_config\.SaveAsProfile\(", Strip(File.ReadAllText(repository)));
        }
    }
}
