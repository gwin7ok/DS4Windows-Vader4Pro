using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;
using DS4Windows;
using DS4Windows.DI;
using DS4WinWPF.DS4Forms.ViewModels;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase6-Step9-3: <c>SettingsViewModel</c> の Global 直接参照解消の回帰ガード
    /// （<c>Phase6-Step9-Plan.md</c> §0A.2 表A、決定1＝S9-A、決定6＝D1）。
    /// - Step9-3a・3b で DI サービス経由へ置換した 89 件のメンバーが、<c>Global.</c> として再混入していないこと。
    /// - コンストラクタの依存が必須であり、Service Locator（<c>AppHost</c>）を持たないこと。
    /// - 削除した呼出元 0 件のハンドラが戻っていないこと。
    /// - 置換後も、設定の読み書きが <c>Global</c> と同じ実体に届くこと（薄い委譲の動作確認）。
    /// ソースファイルが見つからない環境では、ソース走査を行わず合格とする。
    /// </summary>
    public class SettingsViewModelGlobalReferenceGuardTests
    {
        private const string VmPath = "DS4Windows/DS4Forms/ViewModels/SettingsViewModel.cs";

        // Step9-3a・3b で置換済み（SettingsViewModel の非定数 Global 参照は 0 件。定数も使っていない）。
        private static readonly string[] ResolvedMembers =
        {
            "UseExclusiveMode", "SwipeProfiles", "ProfileChangedNotification", "DCBTatStop",
            "FlashWhenLate", "FlashWhenLateAt", "QuickCharge", "UseIconChoice", "UseCurrentTheme",
            "CheckUpdateStartupEnabled", "CheckEveryValue", "CheckEveryUnit",
            "isUsingOSCServer", "setUsingOSCServer", "getOSCServerPortNum", "setOSCServerPort",
            "isInterpretingOscMonitoring", "setInterpretingOscMonitoring",
            "isUsingOSCSender", "setUsingOSCSender", "getOSCSenderPortNum", "setOSCSenderPort",
            "getOSCSenderAddress", "setOSCSenderAddress",
            "isUsingUDPServer", "setUsingUDPServer", "getUDPServerListenAddress", "setUDPServerListenAddress",
            "getUDPServerPortNum", "setUDPServerPort", "UseUDPSeverSmoothing",
            "UDPServerSmoothingMincutoff", "UDPServerSmoothingBeta",
            "hidHideInstalled", "ProcessPriority", "IsAdministrator", "LogMinLevel",
            "UseCustomSteamFolder", "CustomSteamFolder", "FakeExeName", "LogMaxArchiveFiles",
            "exedirpath", "exelocation",
        };

        private readonly ITestOutputHelper _output;

        public SettingsViewModelGlobalReferenceGuardTests(ITestOutputHelper output)
        {
            _output = output;
        }

        private string ReadSourceWithoutComments()
        {
            string path = SourceFileLocator.Find(VmPath);
            if (path == null)
            {
                _output.WriteLine($"{VmPath} が見つからないため検査を省略した。");
                return null;
            }

            string text = File.ReadAllText(path);
            text = Regex.Replace(text, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);
            text = Regex.Replace(text, @"//[^\r\n]*", string.Empty);
            return text;
        }

        [Fact]
        public void ResolvedMembers_AreNotReferencedThroughGlobal()
        {
            string text = ReadSourceWithoutComments();
            if (text == null) return;

            var remaining = Regex.Matches(text, @"\bGlobal\.(\w+)")
                .Cast<Match>()
                .Select(m => m.Groups[1].Value)
                .Where(name => ResolvedMembers.Contains(name))
                .ToList();

            Assert.True(remaining.Count == 0,
                "SettingsViewModel に、置換済みの Global 参照が再混入している: " + string.Join(", ", remaining));
        }

        [Fact]
        public void NoGlobalReferencesRemain()
        {
            string text = ReadSourceWithoutComments();
            if (text == null) return;

            Assert.DoesNotMatch(@"Global\.", text);
        }

        [Fact]
        public void Constructor_HasNoServiceLocator_AndRequiresServices()
        {
            string text = ReadSourceWithoutComments();
            if (text == null) return;

            Assert.DoesNotContain("AppHost", text);
            Assert.DoesNotContain("appSettings = null", text);

            var env = new EnvironmentService();
            var appearance = TestRepositoryFactory.CreateAppearanceSettingsService();
            var app = TestRepositoryFactory.CreateAppSettingsService();
            var path = new PathService();
            var logApplier = new DS4Windows.Services.LogSettingsApplierProxy();
            Assert.Throws<ArgumentNullException>(() => new SettingsViewModel(null, appearance, env, path, logApplier));
            Assert.Throws<ArgumentNullException>(() => new SettingsViewModel(app, null, env, path, logApplier));
            Assert.Throws<ArgumentNullException>(() => new SettingsViewModel(app, appearance, null, path, logApplier));
            Assert.Throws<ArgumentNullException>(() => new SettingsViewModel(app, appearance, env, null, logApplier));
            Assert.Throws<ArgumentNullException>(() => new SettingsViewModel(app, appearance, env, path, null));
        }

        [Fact]
        public void RemovedUnreferencedHandler_IsNotReintroduced()
        {
            string text = ReadSourceWithoutComments();
            if (text == null) return;

            Assert.DoesNotContain("SettingsViewModel_CheckForUpdatesChanged", text);
        }

        [Fact]
        public void Properties_ReadAndWriteThroughToGlobal()
        {
            DS4WinWPF.AppHost.CreateHost();
            var vm = DS4WinWPF.AppHost.GetService<SettingsViewModel>();
            Assert.NotNull(vm);

            bool originalQuick = Global.store.quickCharge;
            int originalOscPort = Global.getOSCServerPortNum();
            double originalBeta = Global.store.udpSmoothingBeta;
            string originalLevel = Global.LogMinLevel;
            try
            {
                vm.QuickCharge = !originalQuick;
                Assert.Equal(!originalQuick, Global.store.quickCharge);
                Assert.Equal(!originalQuick, vm.QuickCharge);

                vm.OscPort = originalOscPort + 1;
                Assert.Equal(originalOscPort + 1, Global.getOSCServerPortNum());

                vm.UdpSmoothBeta = originalBeta + 0.25;
                Assert.Equal(originalBeta + 0.25, Global.store.udpSmoothingBeta);
            }
            finally
            {
                Global.store.quickCharge = originalQuick;
                Global.store.oscServPort = originalOscPort;
                Global.store.udpSmoothingBeta = originalBeta;
                Global.LogMinLevel = originalLevel;
                vm.Dispose();
            }
        }
    }
}
