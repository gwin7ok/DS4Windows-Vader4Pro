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
    /// Phase7-Step10c W2（Q1・Q3＝A）: Actions.xml・LinkedProfiles.xml・ControllerConfigs.xml の保存・読込を
    /// <c>BackingStore</c> から 3 つのシリアライザーへ移したことの回帰ガード。
    /// 内容（保存結果と読込後の状態）の同一性は <c>Phase7Step10cGoldenPersistenceTests</c> が固定する。
    /// ソースが見つからない環境では、ソースの検査を行わず合格とする。
    /// </summary>
    [Collection("ServiceProviderHolder")]
    public class Phase7Step10cW2SerializersGuardTests
    {
        private static readonly string[] MovedMethods =
        {
            "CreateAction", "PrepareActionsXml", "SaveActions", "SaveActionNew", "SaveAction", "RemoveAction", "LoadActions",
            "createLinkedProfiles", "LoadLinkedProfiles", "SaveLinkedProfiles",
            "createControllerConfigs", "LoadControllerConfigsForDevice", "SaveControllerConfigsForDevice",
        };

        private readonly ITestOutputHelper _output;

        public Phase7Step10cW2SerializersGuardTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void BackingStore_NoLongerDeclaresTheMovedMethods()
        {
            const BindingFlags all = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
            var remaining = typeof(BackingStore).GetMethods(all).Select(m => m.Name).Where(n => MovedMethods.Contains(n)).ToArray();
            Assert.True(remaining.Length == 0, "BackingStore に移動済みのメソッドが残っている: " + string.Join(", ", remaining));
        }

        [Fact]
        public void TheSerializers_RequireTheBackingStore()
        {
            Assert.Throws<ArgumentNullException>(() => new SpecialActionsXmlSerializer(null));
            Assert.Throws<ArgumentNullException>(() => new LinkedProfilesXmlSerializer(null));
            Assert.Throws<ArgumentNullException>(() => new ControllerConfigsXmlSerializer(null));
        }

        [Fact]
        public void TheContainer_ProvidesSingletonSerializers_ToTheServices()
        {
            DS4WinWPF.AppHost.CreateHost();
            Assert.Same(DS4WinWPF.AppHost.GetService<SpecialActionsXmlSerializer>(), DS4WinWPF.AppHost.GetService<SpecialActionsXmlSerializer>());
            Assert.NotNull(DS4WinWPF.AppHost.GetService<LinkedProfilesXmlSerializer>());
            Assert.NotNull(DS4WinWPF.AppHost.GetService<ControllerConfigsXmlSerializer>());
            Assert.NotNull(DS4WinWPF.AppHost.GetService<ISpecialActionRepository>());
            Assert.NotNull(DS4WinWPF.AppHost.GetService<IProfileRepository>());
            Assert.NotNull(DS4WinWPF.AppHost.GetService<IProfileXmlStore>());
        }

        [Theory]
        [InlineData("DS4Windows/DS4Control/Services/SpecialActionsXmlSerializer.cs")]
        [InlineData("DS4Windows/DS4Control/Services/LinkedProfilesXmlSerializer.cs")]
        [InlineData("DS4Windows/DS4Control/Services/ControllerConfigsXmlSerializer.cs")]
        public void TheSerializer_DoesNotReferenceGlobal_AndKeepsTheStoreAsTheOnlyState(string relativePath)
        {
            string path = SourceFileLocator.Find(relativePath);
            if (path == null)
            {
                _output.WriteLine("ソースが見つからないため検査を省略した。");
                return;
            }

            string text = Regex.Replace(Regex.Replace(File.ReadAllText(path), @"/\*.*?\*/", string.Empty, RegexOptions.Singleline), @"//[^\r\n]*", string.Empty);
            Assert.False(Regex.IsMatch(text, @"(?<![\w.])Global\.\w+"), relativePath + " が Global を参照している");
            Assert.False(Regex.IsMatch(text, @"\bthis\b(?!\.store\b)"), relativePath + " が this を直接使っている（BackingStore は store で渡すこと）");
        }

        [Fact]
        public void TheRepositories_UseTheSerializers_NotTheStore()
        {
            string repo = SourceFileLocator.Find("DS4Windows/DS4Control/Services/ProfileRepository.cs");
            string actions = SourceFileLocator.Find("DS4Windows/DS4Control/Services/SpecialActionRepository.cs");
            string xmlStore = SourceFileLocator.Find("DS4Windows/DS4Control/Services/ProfileXmlStore.cs");
            if (repo == null || actions == null || xmlStore == null)
            {
                _output.WriteLine("ソースが見つからないため検査を省略した。");
                return;
            }

            string Strip(string t) => Regex.Replace(Regex.Replace(t, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline), @"//[^\r\n]*", string.Empty);
            string r = Strip(File.ReadAllText(repo));
            Assert.DoesNotMatch(@"_config\.(SaveLinkedProfiles|LoadLinkedProfiles|SaveActions)\(", r);
            string a = Strip(File.ReadAllText(actions));
            Assert.DoesNotMatch(@"_config\.(LoadActions|SaveAction|RemoveAction)\(", a);
            Assert.DoesNotMatch(@"Global\.(SaveAction|RemoveAction|CreateStdActions|LoadActions)\(", a);
            string x = Strip(File.ReadAllText(xmlStore));
            Assert.DoesNotMatch(@"_backingStore\.(Load|Save)ControllerConfigsForDevice\(", x);
        }
    }
}
