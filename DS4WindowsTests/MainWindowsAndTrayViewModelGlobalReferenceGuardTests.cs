using System;
using System.IO;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;
using DS4Windows;
using DS4Windows.DI;
using DS4WinWPF.DS4Forms.ViewModels;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase6-Step9-2: <c>MainWindowsViewModel</c> と <c>TrayIconViewModel</c> の Global 直接参照解消の回帰ガード
    /// （<c>Phase6-Step9-Plan.md</c> §0A.2 表B・表C、決定1＝S9-A、決定6＝D1）。
    /// - 非定数の <c>Global.</c> 参照と Service Locator（<c>AppHost</c>）が再混入していないこと。
    /// - コンストラクタの依存が必須（null で <see cref="ArgumentNullException"/>）であること。
    /// - 削除した呼出元 0 件のメンバーが戻っていないこと。
    /// ソースファイルが見つからない環境では、ソース走査を行わず合格とする。
    /// </summary>
    public class MainWindowsAndTrayViewModelGlobalReferenceGuardTests
    {
        private const string MainVmPath = "DS4Windows/DS4Forms/ViewModels/MainWindowsViewModel.cs";
        private const string TrayVmPath = "DS4Windows/DS4Forms/ViewModels/TrayIconViewModel.cs";

        private readonly ITestOutputHelper _output;

        public MainWindowsAndTrayViewModelGlobalReferenceGuardTests(ITestOutputHelper output)
        {
            _output = output;
        }

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
        public void MainWindowsViewModel_HasNoGlobalOrServiceLocatorReferences()
        {
            string text = ReadSourceWithoutComments(MainVmPath);
            if (text == null) return;

            Assert.DoesNotMatch(@"\bGlobal\.", text);
            Assert.DoesNotContain("AppHost", text);
            Assert.DoesNotContain("Program.rootHub", text);
        }

        [Fact]
        public void MainWindowsViewModel_RemovedUnreferencedUpdaterMembers_AreNotReintroduced()
        {
            string text = ReadSourceWithoutComments(MainVmPath);
            if (text == null) return;

            foreach (string removed in new[]
            {
                "RunUpdaterCheck", "DownloadUpstreamVersionInfo", "LauchDS4Updater",
                "IsNET8Available", "DownloadUpstreamUpdaterVersion", "updaterExe",
            })
            {
                Assert.DoesNotContain(removed, text);
            }
        }

        [Fact]
        public void TrayIconViewModel_HasOnlyConstGlobalReferences_AndNoServiceLocator()
        {
            string text = ReadSourceWithoutComments(TrayVmPath);
            if (text == null) return;

            var members = Regex.Matches(text, @"\bGlobal\.(\w+)");
            foreach (Match m in members)
            {
                Assert.True(m.Groups[1].Value == "RESOURCES_PREFIX",
                    $"TrayIconViewModel に定数以外の Global 参照が残っている: Global.{m.Groups[1].Value}");
            }

            Assert.DoesNotContain("AppHost", text);
            Assert.DoesNotContain("Program.rootHub", text);
        }

        [Fact]
        public void TrayIconViewModel_SubscribesBatteryChangedViaAppearanceService()
        {
            string text = ReadSourceWithoutComments(TrayVmPath);
            if (text == null) return;

            Assert.Contains("appearanceSettings.BatteryChanged += UpdateTrayBattery;", text);
            Assert.DoesNotMatch(@"static\s+string\s+trayTitle", text);
        }

        [Fact]
        public void MainWindowsViewModel_RequiresServices()
        {
            var env = new EnvironmentService();
            var path = new PathService();

            Assert.Throws<ArgumentNullException>(() => new MainWindowsViewModel(null, path));
            Assert.Throws<ArgumentNullException>(() => new MainWindowsViewModel(env, null));
            Assert.NotNull(new MainWindowsViewModel(env, path));
        }

        [Fact]
        public void TrayIconViewModel_RequiresServices_BeforeCreatingAnyWpfObject()
        {
            // 依存の null 検査は WPF オブジェクト（ContextMenu 等）の生成より前に行われるため、STA でなくても検証できる
            var env = new EnvironmentService();
            var path = new PathService();
            var appearance = TestRepositoryFactory.CreateAppearanceSettingsService();
            var repo = TestRepositoryFactory.CreateProfileRepository();

            Assert.Throws<ArgumentNullException>(() =>
                new TrayIconViewModel(null, null, null, repo, env, path, System.Windows.Threading.Dispatcher.CurrentDispatcher));
            Assert.Throws<ArgumentNullException>(() =>
                new TrayIconViewModel(null, null, appearance, null, env, path, System.Windows.Threading.Dispatcher.CurrentDispatcher));
            Assert.Throws<ArgumentNullException>(() =>
                new TrayIconViewModel(null, null, appearance, repo, null, path, System.Windows.Threading.Dispatcher.CurrentDispatcher));
            Assert.Throws<ArgumentNullException>(() =>
                new TrayIconViewModel(null, null, appearance, repo, env, null, System.Windows.Threading.Dispatcher.CurrentDispatcher));
            Assert.Throws<ArgumentNullException>(() =>
                new TrayIconViewModel(null, null, appearance, repo, env, path, null));
        }

        [Fact]
        public void MainWindowsViewModel_ResolvesFromContainer_WithInjectedServices()
        {
            DS4WinWPF.AppHost.CreateHost();

            var vm = DS4WinWPF.AppHost.GetService<MainWindowsViewModel>();

            Assert.NotNull(vm);
        }
    }
}
