using System;
using System.IO;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase6-Step3-6c（決定 P2）: `Mapping.cs` の非同期マクロ再生経路（グループ C、19件）が、
    /// `using static DS4Windows.Global;` 経由の非修飾参照（または `Global.` 修飾参照）の `outputKBMMapping` へ戻っていないことを固定する回帰ガード。
    /// マクロ再生は別スレッドで動き、入口が `ctrl` を持たない `DefaultMacroPlayer` 経由（`PlayMacroDirect`）にもあるため、
    /// 引数渡しは行わず、`Mapping` の静的 `profileSettings` から読む（Phase7 の instance 化で解消する。`Phase6-Step3-Plan.md` §3.4.1）。
    /// あわせて、Step3-6c の完了により `Mapping.cs` 全体から `outputKBMMapping` への Global 参照がなくなったことも固定する。
    /// マクロ再生自体は実際のキー・マウス入力を送出するため、挙動テストは行わず、本ガードと実機確認で担保する。
    /// ソースファイルが見つからない環境（ソースなしで実行される場合）では、検査を行わず合格とする。
    /// </summary>
    public class MappingMacroPathGlobalReferenceGuardTests
    {
        /// <summary>検査対象メソッドの宣言（一意に特定できる先頭部分）。</summary>
        private static readonly string[] MethodMarkers =
        {
            "private static bool PlayMacroCodeValue(",
            "private static void AltTabSwapping(",
            "private static void AltTabSwappingRelease(",
        };

        private static readonly Regex BareReference = new Regex(@"(?<![\w.])outputKBMMapping\b");
        private static readonly Regex QualifiedReference = new Regex(@"\bGlobal\.outputKBMMapping\b");

        private readonly ITestOutputHelper _output;

        public MappingMacroPathGlobalReferenceGuardTests(ITestOutputHelper output)
        {
            _output = output;
        }

        /// <summary>コメント（ブロック・行）を除いた Mapping.cs 全体のテキスト。見つからなければ null。</summary>
        private string ReadSourceWithoutComments()
        {
            string path = SourceFileLocator.Find("DS4Windows/DS4Control/Mapping.cs");
            if (path == null)
            {
                _output.WriteLine("Mapping.cs が見つからないため検査を省略した。");
                return null;
            }

            string text = File.ReadAllText(path);
            text = Regex.Replace(text, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);
            text = Regex.Replace(text, @"//[^\r\n]*", string.Empty);
            return text;
        }

        /// <summary>宣言の先頭から、対応する閉じ波括弧までのメソッド全体（シグネチャを含む）を返す。</summary>
        private static string ExtractMethod(string text, string marker)
        {
            int start = text.IndexOf(marker, StringComparison.Ordinal);
            Assert.True(start >= 0, $"{marker} が見つからない（メソッド名・修飾子の変更？）");
            Assert.True(text.IndexOf(marker, start + marker.Length, StringComparison.Ordinal) < 0,
                $"{marker} が複数見つかった（オーバーロードの追加？ガードの見直しが必要）");

            int open = text.IndexOf('{', start);
            Assert.True(open > start, $"{marker} の本体が見つからない");

            int depth = 0;
            for (int i = open; i < text.Length; i++)
            {
                if (text[i] == '{')
                {
                    depth++;
                }
                else if (text[i] == '}')
                {
                    depth--;
                    if (depth == 0)
                    {
                        return text.Substring(start, i - start + 1);
                    }
                }
            }

            Assert.True(false, $"{marker} の閉じ波括弧が見つからない");
            return null;
        }

        [Fact]
        public void MacroPathMethods_ReadOutputKbmMappingFromStaticProfileSettings()
        {
            string text = ReadSourceWithoutComments();
            if (text == null) return;

            foreach (string marker in MethodMarkers)
            {
                string body = ExtractMethod(text, marker);

                Assert.False(BareReference.IsMatch(body), $"outputKBMMapping への非修飾参照（Global）が {marker} に残っている");
                Assert.False(QualifiedReference.IsMatch(body), $"Global.outputKBMMapping への修飾参照が {marker} に残っている");
                Assert.Contains("profileSettings.OutputKBMMapping.", body);
            }
        }

        [Fact]
        public void MappingSource_HasNoGlobalReferenceToOutputKbmMapping()
        {
            string text = ReadSourceWithoutComments();
            if (text == null) return;

            Assert.False(BareReference.IsMatch(text), "Mapping.cs に outputKBMMapping への非修飾参照（Global）が残っている");
            Assert.False(QualifiedReference.IsMatch(text), "Mapping.cs に Global.outputKBMMapping への修飾参照が残っている");
        }
    }
}