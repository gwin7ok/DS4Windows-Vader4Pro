using System;
using System.IO;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase6-Step3-7（温存・要判断の最終処理）: `Mapping.cs` に意図的に残る `Global.` 実利用箇所が
    /// 温存3件（`ProfileSettingsServiceInstance`／`outputKBMHandler`／`ApplyProfile`、いずれも既定 K-1・TODO付き）
    /// のみであることと、`using static DS4Windows.Global;`（31行目付近）が
    /// `getTransitionedColor`（純粋計算、Step3対象外の除外51件の1つ）の非修飾呼び出しのために
    /// 引き続き必要であることを固定する回帰ガード（`Phase6-Step3-Plan.md` §2.4.3・§3・§6）。
    /// ソースファイルが見つからない環境では、検査を行わず合格とする。
    /// </summary>
    public class MappingStep3FinalGlobalReferenceGuardTests
    {
        private readonly ITestOutputHelper _output;

        public MappingStep3FinalGlobalReferenceGuardTests(ITestOutputHelper output)
        {
            _output = output;
        }

        /// <summary>コメント（ブロック・行）を除いた Mapping.cs 全体のテキスト。見つからなければ null。</summary>
        private string ReadSourceWithoutComments(out string rawText)
        {
            string path = SourceFileLocator.Find("DS4Windows/DS4Control/Mapping.cs");
            if (path == null)
            {
                _output.WriteLine("Mapping.cs が見つからないため検査を省略した。");
                rawText = null;
                return null;
            }

            rawText = File.ReadAllText(path);
            string text = Regex.Replace(rawText, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);
            text = Regex.Replace(text, @"//[^\r\n]*", string.Empty);
            return text;
        }

        private static int CountOccurrences(string text, string needle)
        {
            int count = 0, idx = 0;
            while ((idx = text.IndexOf(needle, idx, StringComparison.Ordinal)) >= 0)
            {
                count++;
                idx += needle.Length;
            }
            return count;
        }

        [Fact]
        public void UsingStaticGlobalDirective_IsStillPresent_ForGetTransitionedColor()
        {
            string text = ReadSourceWithoutComments(out string raw);
            if (text == null) return;

            Assert.Contains("using static DS4Windows.Global;", text);

            // getTransitionedColor は Global の純粋計算メンバー（Step3 対象外の除外51件の1つ）で、
            // Mapping.cs では非修飾のまま呼ばれている。これが using static の存在理由。
            var bareCall = new Regex(@"(?<![\w.])getTransitionedColor\s*\(");
            Assert.True(bareCall.IsMatch(text),
                "getTransitionedColor の非修飾呼び出しが見つからない（using static Global の削除可否判定の前提が崩れている）");
        }

        [Fact]
        public void KeepItems_AppearExactlyOnceEachAndAreGlobalQualified()
        {
            string text = ReadSourceWithoutComments(out string raw);
            if (text == null) return;

            Assert.Equal(1, CountOccurrences(text, "Global.ProfileSettingsServiceInstance"));
            Assert.Equal(1, CountOccurrences(text, "Global.outputKBMHandler"));
            Assert.Equal(1, CountOccurrences(text, "Global.ApplyProfile("));
        }

        [Fact]
        public void KeepItems_HaveStep3_7TodoCommentsNearby()
        {
            ReadSourceWithoutComments(out string raw);
            if (raw == null) return;

            Assert.Contains("TODO(Phase6-Step3-7, 温存・決定KEEP): DI ホスト未構築時", raw);
            Assert.Contains("TODO(Phase6-Step3-7, 決定K-1):", raw);
        }
    }
}
