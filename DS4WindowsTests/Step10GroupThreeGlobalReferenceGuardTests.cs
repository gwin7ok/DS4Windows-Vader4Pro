using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;
using DS4Windows;
using DS4Windows.DI;
using DS4WinWPF.DS4Forms.ViewModels;
using DS4WinWPF.DS4Forms.ViewModels.SpecialActions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase6-Step10-3: グループ3（スペシャルアクション系）の View／ViewModel の Global 直接参照解消の回帰ガード
    /// （<c>Phase6-Step10-3-Plan.md</c>）。Step10-3 の各マイクロステップで、置換済みのファイルを一覧へ追加していく。
    /// - 置換済みのファイルに、温存項目以外の <c>Global.</c> が再混入していないこと。
    /// - <c>AppHost</c>（Service Locator）・<c>Program.rootHub</c> を持たないこと。
    /// - コンストラクタの依存が必須であり、null を拒否すること。
    /// ソースファイルが見つからない環境では、ソース走査を行わず合格とする。
    /// </summary>
    public class Step10GroupThreeGlobalReferenceGuardTests
    {
        // ファイルごとに、使ってよい Global メンバー（温存項目）。
        // configFileDecimalCulture は不変の静的値、getX360ControlString はボタン名を返す純粋関数
        private static readonly Dictionary<string, string[]> AllowedGlobalMembers = new Dictionary<string, string[]>
        {
            // Step10-3-2
            ["DS4Windows/DS4Forms/ViewModels/SpecialActEditorViewModel.cs"] = new string[0],
            ["DS4Windows/DS4Forms/ViewModels/SpecialActions/CheckBatteryViewModel.cs"] = new[] { "configFileDecimalCulture" },
            ["DS4Windows/DS4Forms/ViewModels/SpecialActions/LaunchProgramViewModel.cs"] = new[] { "configFileDecimalCulture" },
            ["DS4Windows/DS4Forms/ViewModels/SpecialActions/LoadProfileViewModel.cs"] = new string[0],
            ["DS4Windows/DS4Forms/ViewModels/SpecialActions/MacroViewModel.cs"] = new string[0],
            ["DS4Windows/DS4Forms/ViewModels/SpecialActions/MultiActButtonViewModel.cs"] = new string[0],
            ["DS4Windows/DS4Forms/ViewModels/SpecialActions/PressKeyViewModel.cs"] = new[] { "getX360ControlString" },
            ["DS4Windows/DS4Forms/ViewModels/SpecialActions/SpecialActionViewModel.cs"] = new[] { "configFileDecimalCulture" },
            ["DS4Windows/DS4Forms/SpecialActionEditor.xaml.cs"] = new string[0],
            // Step10-3-3
            ["DS4Windows/DS4Forms/ViewModels/SpecialActionsListViewModel.cs"] = new string[0],
        };

        private readonly ITestOutputHelper _output;

        public Step10GroupThreeGlobalReferenceGuardTests(ITestOutputHelper output)
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
        public void ActionTypeViewModels_RequireSpecialActionRepository()
        {
            Assert.Throws<ArgumentNullException>(() => new MacroViewModel(null));
            Assert.Throws<ArgumentNullException>(() => new LaunchProgramViewModel(null));
            Assert.Throws<ArgumentNullException>(() => new CheckBatteryViewModel(null));
            Assert.Throws<ArgumentNullException>(() => new MultiActButtonViewModel(null));
            Assert.Throws<ArgumentNullException>(() => new SpecialActionViewModel(5, null));
            Assert.Throws<ArgumentNullException>(() => new LoadProfileViewModel(null, null));
        }

        [Fact]
        public void PressKeyViewModel_RequiresAllServices()
        {
            var repo = TestRepositoryFactory.CreateSpecialActionRepository();
            var settings = TestRepositoryFactory.CreateProfileSettingsService();
            var slots = TestRepositoryFactory.CreateOutputSlotService();

            Assert.Throws<ArgumentNullException>(() => new PressKeyViewModel(null, settings, slots));
            Assert.Throws<ArgumentNullException>(() => new PressKeyViewModel(repo, null, slots));
            Assert.Throws<ArgumentNullException>(() => new PressKeyViewModel(repo, settings, null));
        }

        [Fact]
        public void SpecialActionsListViewModel_RequiresAllServices()
        {
            var repo = TestRepositoryFactory.CreateSpecialActionRepository();
            var profileRepo = TestRepositoryFactory.CreateProfileRepository();

            Assert.Throws<ArgumentNullException>(() => new SpecialActionsListViewModel(0, null, profileRepo));
            Assert.Throws<ArgumentNullException>(() => new SpecialActionsListViewModel(0, repo, null));
        }

        [Fact]
        public void RemoveActionFlow_ConfirmsAndPropagatesToAllProfiles_ThroughSingleReapplyPath()
        {
            string editor = ReadSourceWithoutComments("DS4Windows/DS4Forms/ProfileEditor.xaml.cs");
            string main = ReadSourceWithoutComments("DS4Windows/DS4Forms/MainWindow.xaml.cs");
            if (editor == null || main == null) return;

            // 確認のプロンプトを出し、全プロファイルの参照を取り除き、MainWindow へ通知する
            Assert.Contains("MessageBoxButton.YesNo", editor);
            Assert.Contains("profileRepository.RemoveActionReference(", editor);
            Assert.Contains("ProfilesUpdatedExternally?.Invoke(", editor);
            // 名前変更も、同じ通知の経路を使う
            Assert.Contains("profileRepository.ReplaceActionReference(", editor);

            // 再適用の実装は SyncProfileListAndControllers の 1 か所だけ（外部更新は、その経路を呼ぶだけ）
            Assert.Contains("SyncProfileListAndControllers(targetProfiles: profiles)", main);
            Assert.Equal(2, System.Text.RegularExpressions.Regex.Matches(main,
                System.Text.RegularExpressions.Regex.Escape("profileSlotApplier.ApplyToSlot(")).Count);
        }

        [Fact]
        public void SpecialActEditorViewModel_RequiresSpecialActionRepository()
        {
            Assert.Throws<ArgumentNullException>(() => new SpecialActEditorViewModel(0, null, null));
        }
    }
}
