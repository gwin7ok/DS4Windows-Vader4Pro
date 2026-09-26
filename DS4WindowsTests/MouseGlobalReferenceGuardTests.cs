using System.IO;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase6-Step4-5: `Mouse.cs`／`MouseCursor.cs`／`MouseWheel.cs` に、`Global.Clamp`（純粋計算のため除外）以外の
    /// `Global.` 参照と `using static DS4Windows.Global;` が再混入していないことを固定する回帰ガード。
    /// コメント（ブロック・行）は検査対象から除く（既存の Global 参照を含むコメントアウトされたコードを温存しているため）。
    /// ソースファイルが見つからない環境（ソースなしで実行される場合）では、検査を行わず合格とする。
    /// </summary>
    public class MouseGlobalReferenceGuardTests
    {
        private static readonly string[] Files =
        {
            "DS4Windows/DS4Control/Mouse.cs",
            "DS4Windows/DS4Control/MouseCursor.cs",
            "DS4Windows/DS4Control/MouseWheel.cs",
        };

        private static readonly Regex GlobalReference = new Regex(@"\bGlobal\.(\w+)");
        private static readonly Regex UsingStaticGlobal = new Regex(@"using\s+static\s+DS4Windows\.Global\s*;");

        private readonly ITestOutputHelper _output;

        public MouseGlobalReferenceGuardTests(ITestOutputHelper output)
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
        public void MouseFiles_HaveNoGlobalReferenceOtherThanClamp()
        {
            foreach (string file in Files)
            {
                string text = ReadSourceWithoutComments(file);
                if (text == null) continue;

                foreach (Match m in GlobalReference.Matches(text))
                {
                    Assert.True(m.Groups[1].Value == "Clamp",
                        $"{file} に Global.{m.Groups[1].Value} の参照が再混入している（Global.Clamp のみ許可）");
                }
            }
        }

        [Fact]
        public void MouseFiles_DoNotUseStaticGlobalImport()
        {
            foreach (string file in Files)
            {
                string text = ReadSourceWithoutComments(file);
                if (text == null) continue;

                Assert.False(UsingStaticGlobal.IsMatch(text),
                    $"{file} に using static DS4Windows.Global; が追加されている（非修飾の Global 参照の再混入経路）");
            }
        }

        [Fact]
        public void MouseFiles_KeepOnlyTheFourExcludedClampReferences()
        {
            int total = 0;
            bool anyRead = false;
            foreach (string file in Files)
            {
                string text = ReadSourceWithoutComments(file);
                if (text == null) continue;
                anyRead = true;
                total += Regex.Matches(text, @"\bGlobal\.Clamp\b").Count;
            }

            // 台帳（Phase6-Step4-Plan.md §2）の除外は Mouse.cs 2行 + MouseCursor.cs 2行。増減があれば台帳の見直しが必要
            if (anyRead) Assert.Equal(4, total);
        }
    }
}
