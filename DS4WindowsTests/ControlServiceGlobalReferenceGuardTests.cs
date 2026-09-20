using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase6-Step2-6 (PR-6): ControlService.cs の Global 直接参照が、明示的除外リスト
    /// （Phase6-Step2-Plan.md 表4、および決定D2 で温存する C2-02）だけであることを固定する回帰ガード。
    /// `using static DS4Windows.Global;` の再導入や、DI サービスを経由しない Global 参照の混入を検出する。
    /// ソースファイルが見つからない環境（ソースなしで実行される場合）では、検査を行わず合格とする。
    /// </summary>
    public class ControlServiceGlobalReferenceGuardTests
    {
        private readonly ITestOutputHelper _output;

        public ControlServiceGlobalReferenceGuardTests(ITestOutputHelper output)
        {
            _output = output;
        }

        /// <summary>許容される Global 参照（メンバ名 → 出現回数）。</summary>
        private static readonly Dictionary<string, int> AllowedGlobalReferences = new Dictionary<string, int>
        {
            // C2-EX01: 真の定数
            { "MAX_DS4_CONTROLLER_COUNT", 1 },
            // C2-EX03: 真の定数
            { "TEST_PROFILE_INDEX", 1 },
            // C2-02（決定D2）: 過渡期の防御コード。Phase6-Step12 で削除判断
            { "ProfileSettingsServiceInstance", 1 },
            // C2-EX04〜EX07: ViGEm クライアント直接依存（Phase6 の除外対象）
            { "RefreshViGEmBusInfo", 1 },
            { "IsRunningSupportedViGEmBus", 2 },
            { "vigembusVersion", 2 },
            { "vigemInstalled", 1 },
        };

        private static string FindControlServiceSource()
            => SourceFileLocator.Find("DS4Windows/DS4Control/ControlService.cs");

        private static string StripComment(string line)
        {
            int index = line.IndexOf("//", StringComparison.Ordinal);
            return index >= 0 ? line.Substring(0, index) : line;
        }

        [Fact]
        public void ControlService_HasNoUsingStaticGlobal()
        {
            string path = FindControlServiceSource();
            if (path == null)
            {
                _output.WriteLine("ControlService.cs が見つからないため検査を省略した。");
                return;
            }

            foreach (string line in File.ReadAllLines(path))
            {
                Assert.DoesNotContain("using static DS4Windows.Global", line);
            }
        }

        [Fact]
        public void ControlService_GlobalReferences_MatchExplicitExclusionList()
        {
            string path = FindControlServiceSource();
            if (path == null)
            {
                _output.WriteLine("ControlService.cs が見つからないため検査を省略した。");
                return;
            }

            var actual = new Dictionary<string, int>();
            var pattern = new Regex(@"(?<![\w.])Global\.(\w+)");
            foreach (string rawLine in File.ReadAllLines(path))
            {
                foreach (Match match in pattern.Matches(StripComment(rawLine)))
                {
                    string member = match.Groups[1].Value;
                    actual.TryGetValue(member, out int count);
                    actual[member] = count + 1;
                }
            }

            foreach (var pair in actual)
            {
                _output.WriteLine($"Global.{pair.Key}: {pair.Value} 箇所");
            }

            Assert.Equal(AllowedGlobalReferences.Count, actual.Count);
            foreach (var allowed in AllowedGlobalReferences)
            {
                Assert.True(actual.ContainsKey(allowed.Key), $"許容リストの Global.{allowed.Key} が見つからない");
                Assert.Equal(allowed.Value, actual[allowed.Key]);
            }
        }
    }
}