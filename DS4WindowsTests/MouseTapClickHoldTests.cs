using System.IO;
using System.Text.RegularExpressions;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// タップによる左クリックが、瞬間的ではなく一定時間（50ms）保持されること（2026-10-15 承認）。
    /// </summary>
    public class MouseTapClickHoldTests
    {
        private static string LoadMouseSource()
        {
            string path = SourceFileLocator.Find("DS4Windows/DS4Control/Mouse.cs");
            if (path == null) return null;
            string text = File.ReadAllText(path);
            text = Regex.Replace(text, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);
            text = Regex.Replace(text, @"//[^\r\n]*", string.Empty);
            return text;
        }

        [Fact]
        public void TapClick_UsesHoldInsteadOfSingleFramePulse()
        {
            string text = LoadMouseSource();
            if (text == null) return;

            Assert.Matches(@"const\s+double\s+TapClickHoldMs\s*=\s*50\.0\s*;", text);

            // タップ成立（即時／Double Tap の保留）の 2 経路とも、StartTapClickHold を呼ぶ
            Assert.True(Regex.Matches(text, @"\bStartTapClickHold\(\)\s*;").Count >= 2);

            // 保持中は synthesizeMouseButtons が毎フレーム MapClick(Left) を出す
            Assert.Matches(@"DateTime\.UtcNow\s*<\s*tapClickHoldUntil\s*\)\s*\{\s*_syntheticInput\.CountClick\(deviceNum,\s*Click\.Left\)", text); // Phase7-Step7 W4: Mapping.Click は Click（トップレベル）へ移った
        }

        [Fact]
        public void TouchUnchanged_ContinuesSynthesis_WhileTapPendingOrHeld()
        {
            string text = LoadMouseSource();
            if (text == null) return;

            Assert.Matches(@"releaseButtonActive\s*\|\|\s*releaseButtonChanged\s*\|\|\s*tappedOnce\s*\|\|\s*DateTime\.UtcNow\s*<\s*tapClickHoldUntil", text);
        }
    }
}
