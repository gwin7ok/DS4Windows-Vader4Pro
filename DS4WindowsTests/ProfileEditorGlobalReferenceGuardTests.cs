using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase6-Step8: <c>ProfileEditor.xaml.cs</c> の Global 直接参照の解消を固定するソース走査ガード。
    /// 解消したグループを Step8-2 以降で順に <see cref="ResolvedMembers"/> へ追加していく
    /// （最終的に、定数［TEST_PROFILE_INDEX／ASSEMBLY_RESOURCE_PREFIX／RESOURCES_PREFIX］以外の
    /// <c>Global.</c> 参照が 0 件になる。Phase6-Step8-Plan.md §0A.2）。
    /// </summary>
    public class ProfileEditorGlobalReferenceGuardTests
    {
        private readonly ITestOutputHelper _output;

        public ProfileEditorGlobalReferenceGuardTests(ITestOutputHelper output)
        {
            _output = output;
        }

        /// <summary>解消済みのメンバー（Global 経由で再び参照してはならないもの）。</summary>
        private static readonly string[] ResolvedMembers =
        {
            // Step8-2（G1: レイアウト・言語）→ IAppSettingsService
            "ProfileEditorLeftWidth",
            "ProfileEditorRightWidth",
            "SpecialActionNameColWidth",
            "SpecialActionTriggerColWidth",
            "SpecialActionDetailColWidth",
            "FormWidth",
            "FormHeight",
            "UseLang",
            // Step8-3（G2〜G8）→ IProfileRepository／IProfileSettingsService／各 ViewModel
            "ProfilePath",
            "LoadBlankDevProfile",
            "ProfileActions",
            "LSModInfo",
            "RSModInfo",
            "outDevTypeTemp",
            "SaveProfile",
            "InverseRumbleMotors",
            "OutContType",
            "ProfileSettingsServiceInstance",
            "GetAction",
            "CacheExtraProfileInfo",
            "CacheProfileCustomsFlags",
            "appdatapath",
        };

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
        public void ProfileEditor_DoesNotReferenceResolvedGlobalMembers()
        {
            string text = ReadSourceWithoutComments("DS4Windows/DS4Forms/ProfileEditor.xaml.cs");
            if (text == null) return;

            // `Global.X` と `DS4Windows.Global.X` の両方を検出する（単語境界で、別名のメンバーと区別する）。
            string pattern = @"\bGlobal\.(" + string.Join("|", ResolvedMembers) + @")\b";
            var hits = Regex.Matches(text, pattern).Cast<Match>().Select(m => m.Value).Distinct().ToArray();

            Assert.True(hits.Length == 0,
                "解消済みの Global メンバーが ProfileEditor.xaml.cs に再び現れた: " + string.Join(", ", hits));
        }

        [Fact]
        public void ProfileEditor_ReceivesAppSettingsServiceByConstructor_AndMainWindowPassesIt()
        {
            string editor = ReadSourceWithoutComments("DS4Windows/DS4Forms/ProfileEditor.xaml.cs");
            if (editor == null) return;

            // 引数が複数行に折り返されていても一致するよう、空白を正規化して比較する
            string normalizedEditor = Regex.Replace(editor, @"\s+", " ");
            // Phase7-Step5 W5: 旧 AppHost.GetService の 4 件をコンストラクター引数に替えた
            Assert.Contains("public ProfileEditor(int targetDevice, IAppSettingsService appSettingsService, IProfileSettingsService profileSettingsService, DS4Windows.DI.IEditorWindowFactory editorWindowFactory, IProfileRepository profileRepository, IProfileSwitcher profileSwitcher, ControlService controlService, IViewModelFactory viewModelFactory)", normalizedEditor);
            Assert.Contains("throw new ArgumentNullException(nameof(appSettingsService))", editor);
            Assert.Contains("appSettingsService.UseLang", editor);

            string main = ReadSourceWithoutComments("DS4Windows/DS4Forms/MainWindow.xaml.cs");
            if (main == null) return;

            Assert.Contains("editorWindowFactory.CreateProfileEditor(targetDevice)", main);
        }
    }
}
