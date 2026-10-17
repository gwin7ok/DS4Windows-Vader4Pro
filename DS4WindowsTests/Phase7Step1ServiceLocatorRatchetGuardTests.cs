using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase7-Step1 W4: Service Locator の 2 系統目 <c>ServiceProviderHolder.Provider</c>／<c>GetRequiredService</c> の退行ガード（ラチェット）。
    /// 1 系統目の <c>AppHost.GetService|Services</c> と <c>Program.rootHub</c> は <see cref="Step10FourGlobalRatchetGuardTests"/> が固定している。
    /// <c>ServiceProviderHolder</c> は台帳・ラチェットの対象外だったため、Phase7-0 の調査（<c>Phase7-0-Survey.md</c> §1.5 F-A）で追加した。
    /// - 件数が減るのは許可する（基準値を更新して、さらに固定すること）。
    /// - 件数が増える、または基準値に無いファイルに現れる場合は失敗する。
    /// 基準値は 2026-10-17 の走査値（計 23 件）。<c>SetProvider</c>（Composition Root が設定する側。<c>DI/AppHost.cs</c>）は数えない。
    /// <c>Actions/</c> の 17 件は Phase8（Controls／SpecialActions の統合ディスパッチ）の持ち越し。
    /// ソースが見つからない環境では、検査を行わず合格とする。
    /// </summary>
    public class Phase7Step1ServiceLocatorRatchetGuardTests
    {
        private static readonly Dictionary<string, int> Baseline = new Dictionary<string, int>
        {
            // Phase8 の持ち越し（Actions サブシステム。計 17）
            ["DS4Windows/Actions/ActionFactory.cs"] = 1,
            ["DS4Windows/Actions/ActionManager.cs"] = 13,
            ["DS4Windows/Actions/DefaultKeyButtonActionControllerFactory.cs"] = 2,
            ["DS4Windows/Actions/KeyActionAdapter.cs"] = 1,
            // Phase7 の対象（Step5 で、コンストラクター注入または App の保持へ置き換える。計 6）
            // Phase7-Step5 W4: CleanShutdown のレジストリ破棄を、InitializePostHostServices で解決した IControllerRegistry に置き換え（1 → 0）
            ["DS4Windows/App.xaml.cs"] = 0,
            // Phase7-Step8 W2: Mapping のコンストラクター注入（登録簿 KeyButtonControllerRegistry、MouseOutputAction の無効な検索の削除）で、5 → 0
            ["DS4Windows/DS4Control/Mapping.cs"] = 0,
        };

        private static readonly Regex LocatorPattern =
            new Regex(@"ServiceProviderHolder\s*\.\s*(?:Provider|GetRequiredService)\b", RegexOptions.Compiled);

        private readonly ITestOutputHelper _output;

        public Phase7Step1ServiceLocatorRatchetGuardTests(ITestOutputHelper output)
        {
            _output = output;
        }

        private static string StripComments(string text)
        {
            text = Regex.Replace(text, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);
            text = Regex.Replace(text, @"//[^\r\n]*", string.Empty);
            return text;
        }

        [Fact]
        public void ServiceProviderHolderReferences_DoNotExceedBaseline()
        {
            string anchor = SourceFileLocator.Find("DS4Windows/DS4Control/Global.cs");
            if (anchor == null)
            {
                _output.WriteLine("ソースが見つからないため検査を省略した。");
                return;
            }

            // <root>/DS4Windows/DS4Control/Global.cs → <root>
            string root = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(anchor), "..", ".."));
            string sourceDir = Path.Combine(root, "DS4Windows");

            var violations = new List<string>();
            foreach (string file in Directory.GetFiles(sourceDir, "*.cs", SearchOption.AllDirectories))
            {
                string normalized = file.Replace('\\', '/');
                if (normalized.Contains("/obj/") || normalized.Contains("/bin/")) continue;

                string relative = "DS4Windows/" + Path.GetRelativePath(sourceDir, file).Replace('\\', '/');
                int count = LocatorPattern.Matches(StripComments(File.ReadAllText(file))).Count;
                if (count == 0) continue;

                Baseline.TryGetValue(relative, out int allowed);
                if (count > allowed)
                    violations.Add($"{relative}: ServiceProviderHolder.Provider|GetRequiredService {count} 件（基準 {allowed}）");
            }

            foreach (string v in violations) _output.WriteLine(v);
            Assert.True(violations.Count == 0,
                "基準値を超える Service Locator の参照がある（新しい ServiceProviderHolder の参照を持ち込まず、依存を注入すること）:\n" +
                string.Join("\n", violations));
        }
    }
}
