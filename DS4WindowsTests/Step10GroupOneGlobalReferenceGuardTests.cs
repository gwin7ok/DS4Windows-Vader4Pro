using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase6-Step10-1: グループ1（起動・環境系）の View／ViewModel の Global 直接参照解消の回帰ガード
    /// （<c>Phase6-Step10-1-Plan.md</c>、<c>Phase6-Step10-Plan.md</c> §0A.2）。
    /// - 置換済みのファイルに、温存項目（定数・Pre-Host の手動分岐）以外の <c>Global.</c> が再混入していないこと。
    /// - <c>AppHost</c>（Service Locator）・<c>Program.rootHub</c>・静的な <c>App.logHolder</c>／<c>App.Current</c> を持たないこと。
    /// - 削除した呼出元 0 件のメソッドが戻っていないこと。
    /// ソースファイルが見つからない環境では、ソース走査を行わず合格とする。
    /// </summary>
    public class Step10GroupOneGlobalReferenceGuardTests
    {
        // ファイルごとに、使ってよい Global メンバー（温存項目。理由は各ファイルの TODO／計画書 §0A）
        private static readonly Dictionary<string, string[]> AllowedGlobalMembers = new Dictionary<string, string[]>
        {
            // WelcomeDialog: Pre-Host の手動分岐（FindConfigLocation／Load）と定数
            ["DS4Windows/DS4Forms/WelcomeDialog.xaml.cs"] = new[] { "FindConfigLocation", "Load", "RESOURCES_PREFIX" },
            ["DS4Windows/DS4Forms/SaveWhere.xaml.cs"] = new string[0],
            ["DS4Windows/DS4Forms/LanguageSelectDialog.xaml.cs"] = new string[0],
            ["DS4Windows/DS4Forms/About.xaml.cs"] = new string[0],
            ["DS4Windows/DS4Forms/UpdaterWindow.xaml.cs"] = new string[0],
            ["DS4Windows/DS4Forms/LanguagePackControl.xaml.cs"] = new string[0],
            ["DS4Windows/DS4Forms/ViewModels/AboutViewModel.cs"] = new string[0],
            ["DS4Windows/DS4Forms/ViewModels/LogViewModel.cs"] = new string[0],
            ["DS4Windows/DS4Forms/ViewModels/UpdaterWindowViewModel.cs"] = new string[0],
            // LanguagePackViewModel: 定数のみ
            ["DS4Windows/DS4Forms/ViewModels/LanguagePackViewModel.cs"] = new[] { "PROBING_PATH", "LANGUAGE_ASSEMBLY_NAME" },
            ["DS4Windows/StartupMethods.cs"] = new string[0],
            ["DS4Windows/DS4Forms/ViewModels/TrayIconViewModel.cs"] = new[] { "RESOURCES_PREFIX" },
        };

        // ログ文言として残している文字列（コードの参照ではない）
        private static readonly string[] LogLiteralsKeptForCompatibility =
        {
            "Global.UseLang set to",
            "Global.UseLang='",
        };

        // Service Locator・静的フォールバックを持たないこと（検査対象）
        private static readonly string[] NoServiceLocatorFiles =
        {
            "DS4Windows/DS4Forms/SaveWhere.xaml.cs",
            "DS4Windows/DS4Forms/LanguageSelectDialog.xaml.cs",
            "DS4Windows/DS4Forms/About.xaml.cs",
            "DS4Windows/DS4Forms/UpdaterWindow.xaml.cs",
            "DS4Windows/DS4Forms/WelcomeDialog.xaml.cs",
            "DS4Windows/DS4Forms/ViewModels/AboutViewModel.cs",
            "DS4Windows/DS4Forms/ViewModels/LogViewModel.cs",
            "DS4Windows/DS4Forms/ViewModels/UpdaterWindowViewModel.cs",
            "DS4Windows/DS4Forms/ViewModels/LanguagePackViewModel.cs",
            "DS4Windows/DS4Forms/ViewModels/SettingsViewModel.cs",
            "DS4Windows/DS4Forms/ViewModels/TrayIconViewModel.cs",
            "DS4Windows/StartupMethods.cs",
        };

        private readonly ITestOutputHelper _output;

        public Step10GroupOneGlobalReferenceGuardTests(ITestOutputHelper output)
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

                // ログ文言の文字列リテラル（LanguageSelectDialog の "Global.UseLang set to"、LanguagePackViewModel の
                // "Global.UseLang='"）は、置換前のログ内容を維持する（§2.3）ためのもので、コードの参照ではない。
                foreach (string logLiteral in LogLiteralsKeptForCompatibility)
                {
                    text = text.Replace(logLiteral, string.Empty);
                }

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
        public void ReplacedFiles_DoNotUseServiceLocatorOrStaticFallbacks()
        {
            foreach (string file in NoServiceLocatorFiles)
            {
                string text = ReadSourceWithoutComments(file);
                if (text == null) continue;

                Assert.DoesNotContain("AppHost", text);
                Assert.DoesNotContain("Program.rootHub", text);
                Assert.DoesNotContain("logHolder", text);
                Assert.DoesNotContain("App.Current", text);
                Assert.DoesNotContain("Application.Current", text);
            }
        }

        [Fact]
        public void LanguagePackControl_DoesNotUseServiceLocator()
        {
            // Application.Current as App（ApplyLanguageSettingPublic の呼び出し）は温存（Global ではない。App の言語適用処理は Phase7 で整理する）
            string text = ReadSourceWithoutComments("DS4Windows/DS4Forms/LanguagePackControl.xaml.cs");
            if (text == null) return;

            Assert.DoesNotContain("AppHost", text);
            Assert.DoesNotContain("Program.rootHub", text);
        }

        [Fact]
        public void StartupMethods_TakesPathsAsArguments()
        {
            string text = ReadSourceWithoutComments("DS4Windows/StartupMethods.cs");
            if (text == null) return;

            Assert.Contains("WriteStartProgEntry(string exeLocation, string programDirectory)", text);
            Assert.Contains("DeleteOldTaskEntry(string programDirectory)", text);
            Assert.Contains("WriteTaskEntry(string programDirectory, string exeFileName)", text);
            Assert.Contains("CheckStartupExeLocation(string exeLocation)", text);
        }

        [Fact]
        public void RemovedOrphanMethods_DoNotComeBack()
        {
            string util = ReadSourceWithoutComments("DS4Windows/DS4Control/Util.cs");
            if (util == null) return;

            // Phase6-Step10-1-5: 呼出元 0 件の旧 DS4Updater 差し替え処理（コミット b55d160b で UpdaterWindow の方式へ置き換え済み）
            Assert.DoesNotContain("ElevatedCopyUpdater", util);
            Assert.DoesNotContain("Global.exedirpath", util);
        }

        [Fact]
        public void LanguagePackControl_IsInitializedByMainWindow()
        {
            string xaml = SourceFileLocator.Find("DS4Windows/DS4Forms/MainWindow.xaml");
            string code = ReadSourceWithoutComments("DS4Windows/DS4Forms/MainWindow.xaml.cs");
            if (xaml == null || code == null) return;

            Assert.Contains("x:Name=\"languagePackControl\"", File.ReadAllText(xaml));
            Assert.Contains("languagePackControl.Initialize(appSettingsService);", code);
        }
    }
}
