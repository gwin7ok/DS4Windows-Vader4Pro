using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;
using DS4Windows.DI;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase6-Step7: `App.xaml.cs` の Global 直接参照を、領域（メソッド、および Application_Startup の
    /// Pre-Host／Post-Host）ごとに固定する回帰ガード（`Phase6-Step7-Plan.md` §2・§5）。
    /// - Pre-Host 領域（Application_Startup の CreateControlService より前、CheckOptions）と共用ヘルパー
    ///   ApplyLanguageSetting は温存（決定4）なので、参照が変わっていないことを確認する。
    /// - Post-Host 領域は、置き換え済みの参照が再混入していないこと、残っているのが計画どおりの箇所だけであることを確認する。
    /// 行番号ではなくメソッド単位で判定する。ソースファイルが見つからない環境では、検査を行わず合格とする。
    /// </summary>
    public class AppPostHostGlobalReferenceGuardTests
    {
        private const string AppSourcePath = "DS4Windows/App.xaml.cs";
        private const string PostHostAnchor = "CreateControlService(parser);";

        private readonly ITestOutputHelper _output;

        public AppPostHostGlobalReferenceGuardTests(ITestOutputHelper output)
        {
            _output = output;
        }

        // ---- 期待値（領域ごとの Global メンバー参照の件数）----

        private static readonly Dictionary<string, int> ExpectedStartupPreHost = new Dictionary<string, int>
        {
            // E7-01〜E7-05（起動時ログローテーション・ViGEmBus 情報。温存）
            ["FindConfigLocation"] = 1,
            ["appdatapath"] = 1,
            ["LogMaxArchiveFiles"] = 1,
            ["LogMinLevel"] = 1,
            ["RefreshViGEmBusInfo"] = 1,
        };

        private static readonly Dictionary<string, int> ExpectedCheckOptions = new Dictionary<string, int>
        {
            // E7-06〜E7-12（-driverinstall 分岐。温存）
            ["RefreshViGEmBusInfo"] = 1,
            ["FindConfigLocation"] = 1,
            ["Load"] = 1,
            ["UseLang"] = 2,
            ["UseCurrentTheme"] = 2,
        };

        private static readonly Dictionary<string, int> ExpectedApplyLanguageSetting = new Dictionary<string, int>
        {
            // E7-13〜E7-14（共用ヘルパー。決定4＝K で温存）
            ["UseLang"] = 1,
            ["SetCulture"] = 1,
        };

        // Application_Startup の Post-Host（P7-01〜P7-25）は、Step7-2・7-3 ですべて DI サービス経由に置き換えた。
        private static readonly Dictionary<string, int> ExpectedStartupPostHost = new Dictionary<string, int>();

        private static readonly Dictionary<string, int> ExpectedCreateConfDirSkeleton = new Dictionary<string, int>();

        private static readonly Dictionary<string, int> ExpectedAttemptSave = new Dictionary<string, int>
        {
            // P7-38（決定3＝P1 で温存。TODO 付き）
            ["appdatapath"] = 1,
        };

        // P7-39（Save）は Step7-3 で置き換えた。残るのは const の MAX_DS4_CONTROLLER_COUNT（E7-15）だけ。
        private static readonly Dictionary<string, int> ExpectedCleanShutdown = new Dictionary<string, int>
        {
            ["MAX_DS4_CONTROLLER_COUNT"] = 1,
        };

        // ---- ソースの読み取りと領域の切り出し ----

        private string ReadSourceWithoutComments()
        {
            string path = SourceFileLocator.Find(AppSourcePath);
            if (path == null)
            {
                _output.WriteLine("App.xaml.cs が見つからないため検査を省略した。");
                return null;
            }

            string text = File.ReadAllText(path);
            text = Regex.Replace(text, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);
            text = Regex.Replace(text, @"//[^\r\n]*", string.Empty);
            return text;
        }

        /// <summary>「void 名前(」で始まるメソッドの本体（最初の { から対応する } まで）を返す。</summary>
        private static string ExtractMethodBody(string text, string methodName)
        {
            Match m = Regex.Match(text, @"\bvoid\s+" + Regex.Escape(methodName) + @"\s*\(");
            if (!m.Success)
                m = Regex.Match(text, @"\bbool\s+" + Regex.Escape(methodName) + @"\s*\(");
            Assert.True(m.Success, $"メソッド {methodName} が見つからない。");

            int open = text.IndexOf('{', m.Index);
            int depth = 0;
            for (int i = open; i < text.Length; i++)
            {
                if (text[i] == '{') depth++;
                else if (text[i] == '}')
                {
                    depth--;
                    if (depth == 0)
                        return text.Substring(open, i - open + 1);
                }
            }

            throw new InvalidOperationException($"メソッド {methodName} の終わりが見つからない。");
        }

        private static Dictionary<string, int> CountGlobalMembers(string region)
        {
            return Regex.Matches(region, @"\bGlobal\.(\w+)")
                .Cast<Match>()
                .GroupBy(m => m.Groups[1].Value)
                .ToDictionary(g => g.Key, g => g.Count());
        }

        private void AssertRegion(string regionName, string region, Dictionary<string, int> expected)
        {
            Dictionary<string, int> actual = CountGlobalMembers(region);
            string Format(Dictionary<string, int> d) =>
                d.Count == 0 ? "(なし)" : string.Join(", ", d.OrderBy(kv => kv.Key, StringComparer.Ordinal).Select(kv => $"{kv.Key}×{kv.Value}"));

            _output.WriteLine($"{regionName}: {Format(actual)}");
            Assert.True(
                expected.Count == actual.Count && expected.All(kv => actual.TryGetValue(kv.Key, out int n) && n == kv.Value),
                $"{regionName} の Global 参照が計画と一致しない。期待: {Format(expected)} ／ 実際: {Format(actual)}");
        }

        // ---- テスト ----

        [Fact]
        public void InitializePostHostServices_IsCalledRightAfterCreateControlService()
        {
            string text = ReadSourceWithoutComments();
            if (text == null) return;

            string startup = ExtractMethodBody(text, "Application_Startup");
            Assert.Matches(@"CreateControlService\(parser\);\s*InitializePostHostServices\(\);", startup);
            Assert.Single(Regex.Matches(text, @"\bInitializePostHostServices\(\);"));
        }

        [Fact]
        public void GlobalReferences_MatchPlan_PerRegion()
        {
            string text = ReadSourceWithoutComments();
            if (text == null) return;

            string startup = ExtractMethodBody(text, "Application_Startup");
            int anchor = startup.IndexOf(PostHostAnchor, StringComparison.Ordinal);
            Assert.True(anchor >= 0, "Application_Startup に CreateControlService(parser); が見つからない。");
            string startupPreHost = startup.Substring(0, anchor);
            string startupPostHost = startup.Substring(anchor);

            string checkOptions = ExtractMethodBody(text, "CheckOptions");
            string applyLanguage = ExtractMethodBody(text, "ApplyLanguageSetting");
            string confDir = ExtractMethodBody(text, "CreateConfDirSkeleton");
            string attemptSave = ExtractMethodBody(text, "AttemptSave");
            string cleanShutdown = ExtractMethodBody(text, "CleanShutdown");

            AssertRegion("Application_Startup（Pre-Host）", startupPreHost, ExpectedStartupPreHost);
            AssertRegion("CheckOptions（Pre-Host）", checkOptions, ExpectedCheckOptions);
            AssertRegion("ApplyLanguageSetting（共用ヘルパー）", applyLanguage, ExpectedApplyLanguageSetting);
            AssertRegion("Application_Startup（Post-Host）", startupPostHost, ExpectedStartupPostHost);
            AssertRegion("CreateConfDirSkeleton", confDir, ExpectedCreateConfDirSkeleton);
            AssertRegion("AttemptSave", attemptSave, ExpectedAttemptSave);
            AssertRegion("CleanShutdown", cleanShutdown, ExpectedCleanShutdown);

            // 上記以外の場所に Global 参照が増えていないこと（ファイル全体の件数が各領域の合計と一致する）。
            int expectedTotal = new[]
            {
                ExpectedStartupPreHost, ExpectedCheckOptions, ExpectedApplyLanguageSetting, ExpectedStartupPostHost,
                ExpectedCreateConfDirSkeleton, ExpectedAttemptSave, ExpectedCleanShutdown,
            }.Sum(d => d.Values.Sum());
            int actualTotal = Regex.Matches(text, @"\bGlobal\.\w+").Count;
            Assert.Equal(expectedTotal, actualTotal);
        }

        [Fact]
        public void RetainedGlobalReferences_HaveTodoComments()
        {
            // 温存を決めた箇所（決定3＝P1、決定4＝K）に、copilot-instructions.md §3.3 原則4 の TODO が付いていること。
            string path = SourceFileLocator.Find(AppSourcePath);
            if (path == null)
            {
                _output.WriteLine("App.xaml.cs が見つからないため検査を省略した。");
                return;
            }

            string raw = File.ReadAllText(path);
            Assert.Contains("TODO(Phase6-Step7 決定3＝P1)", raw);
            Assert.Contains("TODO(Phase6-Step7 決定4＝K)", raw);
        }

        [Fact]
        public void PostHostServices_CanBeResolvedFromAppHost()
        {
            // InitializePostHostServices が解決する 7 サービスが、DI コンテナに登録されていること。
            Assert.NotNull(DS4WinWPF.AppHost.GetService<IAppSettingsService>());
            Assert.NotNull(DS4WinWPF.AppHost.GetService<IPathService>());
            Assert.NotNull(DS4WinWPF.AppHost.GetService<IEnvironmentService>());
            Assert.NotNull(DS4WinWPF.AppHost.GetService<IProfileRepository>());
            Assert.NotNull(DS4WinWPF.AppHost.GetService<ISpecialActionRepository>());
            Assert.NotNull(DS4WinWPF.AppHost.GetService<IAppearanceSettingsService>());
            Assert.NotNull(DS4WinWPF.AppHost.GetService<IDeviceStateService>());
        }
    }
}
