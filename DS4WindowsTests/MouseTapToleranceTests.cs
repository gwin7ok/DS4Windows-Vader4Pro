using System.IO;
using System.Text.RegularExpressions;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// タップ判定の位置ずれ許容が 100（上流の 10 から拡大。2026-10-15 承認）であること。
    /// </summary>
    public class MouseTapToleranceTests
    {
        [Fact]
        public void TapTolerance_Is100_AndUsedByTapJudgement()
        {
            string path = SourceFileLocator.Find("DS4Windows/DS4Control/Mouse.cs");
            if (path == null) return;
            string text = File.ReadAllText(path);
            text = Regex.Replace(text, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);
            text = Regex.Replace(text, @"//[^\r\n]*", string.Empty);

            Assert.Matches(@"const\s+int\s+TapMaxMoveHw\s*=\s*100\s*;", text);
            Assert.Contains("< TapMaxMoveHw", text);
            Assert.DoesNotMatch(@"firstTouch\.HwX\s*-\s*arg\.Touches\[0\]\.HwX\)\s*<\s*10\b", text);
        }
    }
}
