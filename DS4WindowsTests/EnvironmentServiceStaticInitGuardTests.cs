using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase6-Step2 (PR-1b の是正): EnvironmentService の静的初期化（コントローラースロット上限の計算）が、
    /// Global の静的初期化を起こさないことを固定する回帰ガード。
    ///
    /// 背景: Global は明示的な静的コンストラクタを持ち、その初期化中に OutputSlotManager が生成され、
    /// OutputSlotManager は ControlService.CURRENT_DS4_CONTROLLER_LIMIT（= EnvironmentService.ProcessControllerSlotLimit）を読む。
    /// EnvironmentService の静的初期化から Global のメソッドを呼ぶと、EnvironmentService が最初に触れられた場合に、
    /// 循環の途中の値 0 が ControlService.CURRENT_DS4_CONTROLLER_LIMIT に確定し、スロットが 0 個になる
    /// （テストの実行順序によって OutputSlotServiceTests などが失敗した）。
    /// 上限の計算は const（インライン展開され、Global の静的初期化を起こさない）以外の Global メンバを参照してはならない。
    /// ソースファイルが見つからない環境では、検査を行わず合格とする。
    /// </summary>
    public class EnvironmentServiceStaticInitGuardTests
    {
        private static readonly HashSet<string> AllowedConstMembers = new HashSet<string>
        {
            "MAX_DS4_CONTROLLER_COUNT",
            "OLD_XINPUT_CONTROLLER_COUNT",
        };

        private readonly ITestOutputHelper _output;

        public EnvironmentServiceStaticInitGuardTests(ITestOutputHelper output)
        {
            _output = output;
        }

        /// <summary>指定メソッドの本体（最初の { から対応する } まで）を返す。見つからなければ null。</summary>
        private static string ExtractMethodBody(string source, string signature)
        {
            int start = source.IndexOf(signature, System.StringComparison.Ordinal);
            if (start < 0)
            {
                return null;
            }

            int open = source.IndexOf('{', start);
            if (open < 0)
            {
                return null;
            }

            int depth = 0;
            for (int i = open; i < source.Length; i++)
            {
                if (source[i] == '{') depth++;
                else if (source[i] == '}')
                {
                    depth--;
                    if (depth == 0)
                    {
                        return source.Substring(open, i - open + 1);
                    }
                }
            }

            return null;
        }

        [Fact]
        public void SlotLimitCalculation_DoesNotReferenceNonConstGlobalMembers()
        {
            string path = SourceFileLocator.Find("DS4Windows/DS4Control/Services/EnvironmentService.cs");
            if (path == null)
            {
                _output.WriteLine("EnvironmentService.cs が見つからないため検査を省略した。");
                return;
            }

            string source = File.ReadAllText(path);
            string calculate = ExtractMethodBody(source, "private static int CalculateControllerSlotLimit()");
            string osCheck = ExtractMethodBody(source, "private static bool IsWin8OrGreaterCore()");

            Assert.NotNull(calculate);
            Assert.NotNull(osCheck);

            foreach (string body in new[] { calculate, osCheck })
            {
                foreach (Match match in Regex.Matches(body, @"(?<![\w.])Global\.(\w+)"))
                {
                    string member = match.Groups[1].Value;
                    Assert.True(AllowedConstMembers.Contains(member),
                        $"上限の計算が Global.{member} を参照している（Global の静的初期化を起こすため循環の原因になる）");
                }
            }
        }

        [Fact]
        public void ProcessControllerSlotLimit_IsInitializedFromTheSelfContainedCalculation()
        {
            string path = SourceFileLocator.Find("DS4Windows/DS4Control/Services/EnvironmentService.cs");
            if (path == null)
            {
                _output.WriteLine("EnvironmentService.cs が見つからないため検査を省略した。");
                return;
            }

            string source = File.ReadAllText(path);

            Assert.Contains("ProcessControllerSlotLimit = CalculateControllerSlotLimit();", source);
        }
    }
}