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
    /// Phase6-Step9-4: <c>ProfileSettingsViewModel</c>・<c>PresetMenuHelper</c>・<c>ViewModelFactory</c> の
    /// Global 直接参照・Service Locator 解消の回帰ガード（<c>Phase6-Step9-Plan.md</c> §0A.2 表D、決定1＝S9-A、決定2＝C1）。
    /// - 非定数の <c>Global.</c> 参照、<c>AppHost</c>、<c>Program.rootHub</c> が再混入していないこと。
    /// - コンストラクタの依存が必須であること（<c>ControlService</c> のみ null 許容）。
    /// - 編集スロットと実機を分ける <c>targetDevice</c> の引数が維持されていること。
    /// ソースファイルが見つからない環境では、ソース走査を行わず合格とする。
    /// </summary>
    public class ProfileSettingsViewModelGlobalReferenceGuardTests
    {
        private const string VmPath = "DS4Windows/DS4Forms/ViewModels/ProfileSettingsViewModel.cs";
        private const string FactoryPath = "DS4Windows/DS4Control/Services/ViewModelFactory.cs";

        private readonly ITestOutputHelper _output;

        // ProfileSettingsViewModel のコンストラクタは pack://application URI（Uri／BitmapImage）を生成する。
        // pack スキームは System.Windows.Application の生成で登録されるため、ほかのテストクラスが先に Application を
        // 生成したかどうか（実行順）によって、このクラスの ViewModelFactory 経由のテストが
        // UriFormatException（Invalid port specified）で失敗していた。ほかの ViewModel テストと同じ準備を明示する。
        static ProfileSettingsViewModelGlobalReferenceGuardTests()
        {
            if (string.IsNullOrEmpty(Global.appdatapath))
            {
                Global.appdatapath = AppContext.BaseDirectory;
            }

            if (System.Windows.Application.Current == null)
            {
                try
                {
                    _ = new System.Windows.Application();
                }
                catch { }
            }

            if (System.Windows.Application.ResourceAssembly == null)
            {
                try
                {
                    System.Windows.Application.ResourceAssembly = typeof(DS4WinWPF.AppHost).Assembly;
                }
                catch { }
            }
        }

        public ProfileSettingsViewModelGlobalReferenceGuardTests(ITestOutputHelper output)
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
        public void ProfileSettingsViewModel_HasOnlyConstGlobalReferences_AndNoServiceLocator()
        {
            string text = ReadSourceWithoutComments(VmPath);
            if (text == null) return;

            // Phase6-Step10b W3 で分離した型（PresetMenuHelper、各 Choice）も同じ基準で検査する
            string[] relatedPaths =
            {
                VmPath,
                "DS4Windows/DS4Forms/ViewModels/PresetMenuHelper.cs",
                "DS4Windows/DS4Forms/ViewModels/TriggerModeChoice.cs",
                "DS4Windows/DS4Forms/ViewModels/TwoStageChoice.cs",
                "DS4Windows/DS4Forms/ViewModels/TriggerEffectChoice.cs",
            };
            foreach (string relative in relatedPaths)
            {
                string src = relative == VmPath ? text : ReadSourceWithoutComments(relative);
                if (src == null) continue;

                foreach (Match m in Regex.Matches(src, @"\bGlobal\.(\w+)"))
                {
                    Assert.True(m.Groups[1].Value == "ASSEMBLY_RESOURCE_PREFIX",
                        $"{relative} に定数以外の Global 参照が残っている: Global.{m.Groups[1].Value}");
                }

                Assert.DoesNotContain("AppHost", src);
                Assert.DoesNotContain("Program.rootHub", src);
            }
        }

        [Fact]
        public void Constructors_HaveNoOptionalServiceArguments_AndKeepTargetDevice()
        {
            string text = ReadSourceWithoutComments(VmPath);
            if (text == null) return;

            Match ctor = Regex.Match(text, @"public ProfileSettingsViewModel\(([^)]*)\)", RegexOptions.Singleline);
            Assert.True(ctor.Success);
            string args = ctor.Groups[1].Value;
            Assert.Contains("IEnvironmentService environmentService", args);
            Assert.Contains("IPathService pathService", args);
            Assert.Contains("int targetDevice = -1", args);
            // targetDevice 以外は省略可能にしない（既定値の指定は 1 つだけ）
            Assert.Single(Regex.Matches(args, "="));

            // PresetMenuHelper は Phase6-Step10b W3 で別ファイルへ分離した
            string helperText = ReadSourceWithoutComments("DS4Windows/DS4Forms/ViewModels/PresetMenuHelper.cs");
            if (helperText == null) return;
            Match helper = Regex.Match(helperText, @"public PresetMenuHelper\(([^)]*)\)");
            Assert.True(helper.Success);
            Assert.DoesNotContain("=", helper.Groups[1].Value);
        }

        [Fact]
        public void ViewModelFactory_HasNoServiceLocatorInConstructor()
        {
            string text = ReadSourceWithoutComments(FactoryPath);
            if (text == null) return;

            // ProfileSettingsViewModel 以外の生成（Step10 の範囲）が残っているため、コンストラクタ部分だけを検査する
            int start = text.IndexOf("public ViewModelFactory(", StringComparison.Ordinal);
            Assert.True(start >= 0);
            int end = text.IndexOf("public ProfileSettingsViewModel CreateProfileSettingsViewModel", start, StringComparison.Ordinal);
            Assert.True(end > start);
            string ctor = text.Substring(start, end - start);

            Assert.DoesNotContain("AppHost", ctor);
            Assert.DoesNotContain("= null", ctor);
            Assert.Contains("Func<ControlService>", ctor);
        }

        [Fact]
        public void Constructor_RequiresServices_ExceptControlService()
        {
            var settings = TestRepositoryFactory.CreateProfileSettingsService();
            var outputSlots = TestRepositoryFactory.CreateOutputSlotService();
            var repo = TestRepositoryFactory.CreateProfileRepository();
            var loader = TestRepositoryFactory.CreateProfilePresetLoader();
            var env = new EnvironmentService();
            var path = new PathService();

            Assert.Throws<ArgumentNullException>(() =>
                new ProfileSettingsViewModel(0, null, null, outputSlots, repo, loader, env, path));
            Assert.Throws<ArgumentNullException>(() =>
                new ProfileSettingsViewModel(0, settings, null, null, repo, loader, env, path));
            Assert.Throws<ArgumentNullException>(() =>
                new ProfileSettingsViewModel(0, settings, null, outputSlots, null, loader, env, path));
            Assert.Throws<ArgumentNullException>(() =>
                new ProfileSettingsViewModel(0, settings, null, outputSlots, repo, null, env, path));
            Assert.Throws<ArgumentNullException>(() =>
                new ProfileSettingsViewModel(0, settings, null, outputSlots, repo, loader, null, path));
            Assert.Throws<ArgumentNullException>(() =>
                new ProfileSettingsViewModel(0, settings, null, outputSlots, repo, loader, env, null));
            Assert.Throws<ArgumentNullException>(() => new PresetMenuHelper(0, null));
        }

        [Fact]
        public void ViewModelFactory_ResolvedFromContainer_CreatesViewModelWithEnvironmentService()
        {
            DS4WinWPF.AppHost.CreateHost();
            var factory = DS4WinWPF.AppHost.GetService<IViewModelFactory>();
            Assert.NotNull(factory);

            ProfileSettingsViewModel vm = factory.CreateProfileSettingsViewModel(0);

            Assert.NotNull(vm);
            Assert.Equal(DS4WinWPF.AppHost.GetService<IEnvironmentService>().IsUsingMinViGEm117333(),
                vm.UsingMinViGEm173333);
        }
    }
}
