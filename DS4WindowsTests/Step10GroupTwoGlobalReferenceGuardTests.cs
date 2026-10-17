using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;
using DS4Windows;
using DS4Windows.DI;
using DS4WinWPF;
using DS4WinWPF.DS4Forms.ViewModels;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase6-Step10-2: グループ2（プロファイル・マッピング系）の View／ViewModel の Global 直接参照解消の回帰ガード
    /// （<c>Phase6-Step10-2-Plan.md</c>、<c>Phase6-Step10-Plan.md</c> §0A.3）。Step10-2 の各マイクロステップで、
    /// 置換済みのファイルを本クラスの一覧へ追加していく。
    /// - 置換済みのファイルに、温存項目（定数・不変の静的表）以外の <c>Global.</c> が再混入していないこと。
    /// - <c>AppHost</c>（Service Locator）・<c>Program.rootHub</c> を持たないこと。
    /// - コンストラクタの依存が必須であり、null を拒否すること。
    /// ソースファイルが見つからない環境では、ソース走査を行わず合格とする。
    /// </summary>
    public class Step10GroupTwoGlobalReferenceGuardTests
    {
        // ファイルごとに、使ってよい Global メンバー（温存項目）
        private static readonly Dictionary<string, string[]> AllowedGlobalMembers = new Dictionary<string, string[]>
        {
            // Step10-2-2
            ["DS4Windows/DS4Forms/ViewModels/ControllerListViewModel.cs"] = new[] { "RESOURCES_PREFIX" },
            ["DS4Windows/DS4Forms/ViewModels/ControllerRegDeviceOptsViewModel.cs"] = new string[0],
            ["DS4Windows/DS4Forms/ControllerRegisterOptionsWindow.xaml.cs"] = new string[0],
            // Step10-2-3
            ["DS4Windows/DS4Forms/AutoProfiles.xaml.cs"] = new string[0],
            ["DS4Windows/DS4Forms/ViewModels/AutoProfilesViewModel.cs"] = new string[0],
            ["DS4Windows/DS4Forms/DupBox.xaml.cs"] = new string[0],
            ["DS4Windows/DS4Forms/RenameProfileWindow.xaml.cs"] = new string[0],
            ["DS4Windows/DS4Forms/ViewModels/RenameProfileViewModel.cs"] = new string[0],
            // Step10-2-4b（RESOURCES_PREFIX・ASSEMBLY_RESOURCE_PREFIX は定数、ds4inputNames などの名前表と macroDS4Values は不変の静的表）
            ["DS4Windows/DS4Forms/BindingWindow.xaml.cs"] = new[] { "RESOURCES_PREFIX", "ASSEMBLY_RESOURCE_PREFIX", "ds4inputNames", "xboxDefaultNames", "ds4DefaultNames" },
            ["DS4Windows/DS4Forms/ViewModels/BindingWindowViewModel.cs"] = new string[0],
            ["DS4Windows/DS4Forms/RecordBox.xaml.cs"] = new[] { "RESOURCES_PREFIX" },
            ["DS4Windows/DS4Forms/ViewModels/RecordBoxViewModel.cs"] = new[] { "RESOURCES_PREFIX", "macroDS4Values" },
            ["DS4Windows/DS4Forms/StickCalibrationWindow.xaml.cs"] = new string[0],
            ["DS4Windows/DS4Control/Services/EditorWindowFactory.cs"] = new string[0],
            // Step10-2-5（getX360ControlString は、名前表を引くだけの純粋関数のため温存）
            ["DS4Windows/DS4Forms/ViewModels/MappingListViewModel.cs"] = new[] { "getX360ControlString" },
            ["DS4Windows/DS4Forms/ViewModels/TouchButtonUserControlViewModel.cs"] = new string[0],
            ["DS4Windows/DS4Forms/TouchButtonUserControl.xaml.cs"] = new string[0],
            ["DS4Windows/DS4Control/PresetOption.cs"] = new string[0],
            ["DS4Windows/DS4Forms/PresetOptionWindow.xaml.cs"] = new string[0],
            ["DS4Windows/DS4Forms/ViewModels/PresetOptionViewModel.cs"] = new string[0],
            ["DS4Windows/DS4Forms/ControllerReadingsControl.xaml.cs"] = new string[0],
        };

        private readonly ITestOutputHelper _output;

        public Step10GroupTwoGlobalReferenceGuardTests(ITestOutputHelper output)
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
        public void ReplacedFiles_DoNotReferenceGlobalExceptRetainedMembers()
        {
            foreach (var entry in AllowedGlobalMembers)
            {
                string text = ReadSourceWithoutComments(entry.Key);
                if (text == null) continue;

                var unexpected = Regex.Matches(text, @"\bGlobal\.(\w+)")
                    .Cast<Match>()
                    .Select(m => m.Groups[1].Value)
                    .Where(name => !entry.Value.Contains(name))
                    .Distinct()
                    .ToList();

                Assert.True(unexpected.Count == 0,
                    $"{entry.Key} に温存項目以外の Global 参照がある: {string.Join(", ", unexpected)}");
            }
        }

        [Fact]
        public void ReplacedFiles_DoNotUseServiceLocatorOrRootHubFallbacks()
        {
            foreach (string file in AllowedGlobalMembers.Keys)
            {
                string text = ReadSourceWithoutComments(file);
                if (text == null) continue;

                Assert.DoesNotContain("AppHost", text);
                Assert.DoesNotContain("Program.rootHub", text);
            }
        }

        [Fact]
        public void ControllerListViewModel_RequiresAllServices()
        {
            var settings = TestRepositoryFactory.CreateProfileSettingsService();
            var repo = TestRepositoryFactory.CreateProfileRepository();
            var app = TestRepositoryFactory.CreateAppSettingsService();

            // 依存の null 検査は、ControlService を使う処理より前に行われる
            Assert.Throws<ArgumentNullException>(() => new ControllerListViewModel(null, null, null, repo, app));
            Assert.Throws<ArgumentNullException>(() => new ControllerListViewModel(null, null, settings, null, app));
            Assert.Throws<ArgumentNullException>(() => new ControllerListViewModel(null, null, settings, repo, null));
        }

        [Fact]
        public void AutoProfilesViewModel_RequiresAllServices()
        {
            // 依存の null 検査は、AutoProfileHolder を使う処理より前に行われる（holder／list は null で検証できる）
            var app = TestRepositoryFactory.CreateAppSettingsService();

            Assert.Throws<ArgumentNullException>(() => new AutoProfilesViewModel(null, null, null, null, null, null));
            Assert.Throws<ArgumentNullException>(() => new AutoProfilesViewModel(null, null, app, null, null, new EnvironmentService()));
        }

        [Fact]
        public void RenameProfileViewModel_RequiresPathService_AndUsesItsAppDataPath()
        {
            Assert.Throws<ArgumentNullException>(() => new RenameProfileViewModel(null));

            string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "Phase6Step10_Rename_" + Guid.NewGuid().ToString("N"));
            System.IO.Directory.CreateDirectory(System.IO.Path.Combine(dir, "Profiles"));
            try
            {
                System.IO.File.WriteAllText(System.IO.Path.Combine(dir, "Profiles", "Existing.xml"), "x");
                var vm = new RenameProfileViewModel(new PathService(dir));

                vm.ProfileName = "Existing";
                Assert.True(vm.ProfileFileExists());

                vm.ProfileName = "Missing";
                Assert.False(vm.ProfileFileExists());
            }
            finally
            {
                System.IO.Directory.Delete(dir, true);
            }
        }

        [Fact]
        public void EditorWindowFactory_RequiresAllServices()
        {
            var settings = TestRepositoryFactory.CreateProfileSettingsService();
            var slots = TestRepositoryFactory.CreateOutputSlotService();
            var path = new PathService();
            Func<ControlService> cs = () => null;
            var loader = TestRepositoryFactory.CreateProfilePresetLoader();
            var repo = TestRepositoryFactory.CreateSpecialActionRepository();
            // Phase7-Step5 W5: CreateProfileEditor のために、アプリ設定・プロファイルリポジトリ・プロファイル切替の 3 つを追加した
            var app = TestRepositoryFactory.CreateAppSettingsService();
            var profiles = TestRepositoryFactory.CreateProfileRepository(settings);
            var switcher = new DS4Windows.Actions.DefaultProfileSwitcher();
            var vm = new NullViewModelFactory();

            Assert.Throws<ArgumentNullException>(() => new DS4Windows.Services.EditorWindowFactory(null, slots, path, vm, repo, loader, cs, app, profiles, switcher));
            Assert.Throws<ArgumentNullException>(() => new DS4Windows.Services.EditorWindowFactory(settings, null, path, vm, repo, loader, cs, app, profiles, switcher));
            Assert.Throws<ArgumentNullException>(() => new DS4Windows.Services.EditorWindowFactory(settings, slots, null, vm, repo, loader, cs, app, profiles, switcher));
            Assert.Throws<ArgumentNullException>(() => new DS4Windows.Services.EditorWindowFactory(settings, slots, path, null, repo, loader, cs, app, profiles, switcher));
            Assert.Throws<ArgumentNullException>(() => new DS4Windows.Services.EditorWindowFactory(settings, slots, path, vm, null, loader, cs, app, profiles, switcher));
            Assert.Throws<ArgumentNullException>(() => new DS4Windows.Services.EditorWindowFactory(settings, slots, path, vm, repo, null, cs, app, profiles, switcher));
            Assert.Throws<ArgumentNullException>(() => new DS4Windows.Services.EditorWindowFactory(settings, slots, path, vm, repo, loader, null, app, profiles, switcher));
            Assert.Throws<ArgumentNullException>(() => new DS4Windows.Services.EditorWindowFactory(settings, slots, path, vm, repo, loader, cs, null, profiles, switcher));
            Assert.Throws<ArgumentNullException>(() => new DS4Windows.Services.EditorWindowFactory(settings, slots, path, vm, repo, loader, cs, app, null, switcher));
            Assert.Throws<ArgumentNullException>(() => new DS4Windows.Services.EditorWindowFactory(settings, slots, path, vm, repo, loader, cs, app, profiles, null));
        }

        private class NullViewModelFactory : IViewModelFactory
        {
            public ProfileSettingsViewModel CreateProfileSettingsViewModel(int device, int targetDevice = -1) => null;
            public RecordBoxViewModel CreateRecordBoxViewModel(int device, DS4ControlSettings controlSettings, bool recordMacro = true, bool extraHold = false, int targetDevice = -1) => null;
            public SpecialActEditorViewModel CreateSpecialActEditorViewModel(int device, SpecialAction action = null) => null;
            public AutoProfilesViewModel CreateAutoProfilesViewModel(AutoProfileHolder autoProfileHolder, ProfileList profileList) => null;
            public SpecialActionsListViewModel CreateSpecialActionsListViewModel(int device) => null;
        }

        [Fact]
        public void PresetAndMappingViewModels_RequireServices()
        {
            Assert.Throws<ArgumentNullException>(() => new PresetOptionViewModel(null, null));
            Assert.Throws<ArgumentNullException>(() => new PresetOptionViewModel(TestRepositoryFactory.CreateProfilePresetLoader(), null));
            Assert.Throws<ArgumentNullException>(() => new MappingListViewModel(0, OutContType.X360, null));
            Assert.Throws<ArgumentNullException>(() => new TouchButtonUserControlViewModel(0, null));
        }

        [Fact]
        public void BindingAndRecordViewModels_RequireProfileSettingsService()
        {
            Assert.Throws<ArgumentNullException>(() => new BindingWindowViewModel(0, new DS4ControlSettings(DS4Controls.Cross), null, TestRepositoryFactory.CreateOutputSlotService()));
            Assert.Throws<ArgumentNullException>(() => new BindingWindowViewModel(0, new DS4ControlSettings(DS4Controls.Cross), TestRepositoryFactory.CreateProfileSettingsService(), null));
            Assert.Throws<ArgumentNullException>(() => new RecordBoxViewModel(0, new DS4ControlSettings(DS4Controls.Cross), false, true, null, null));
        }

        [Fact]
        public void ControllerRegDeviceOptsViewModel_RequiresAppSettingsAndXmlStore()
        {
            var app = TestRepositoryFactory.CreateAppSettingsService();
            var xmlStore = TestRepositoryFactory.CreateProfileXmlStore();

            Assert.Throws<ArgumentNullException>(() => new ControllerRegDeviceOptsViewModel(null, null, null, xmlStore));
            Assert.Throws<ArgumentNullException>(() => new ControllerRegDeviceOptsViewModel(null, null, app, null));
        }
    }
}
