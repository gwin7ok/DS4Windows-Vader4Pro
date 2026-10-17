using System.IO;
using System.Text.RegularExpressions;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Touchpad Button Activation Mode が Release のとき、Left/Right Touch 等のフラグを
    /// 離した後 100ms 保持してから下ろすこと（上流の比較反転の是正）。
    /// </summary>
    public class MouseReleaseButtonHoldTests
    {
        [Fact]
        public void ReleaseButtonFlags_AreClearedOnlyAfterHoldTime()
        {
            string path = SourceFileLocator.Find("DS4Windows/DS4Control/Mouse.cs");
            if (path == null) return;
            string text = File.ReadAllText(path);
            text = Regex.Replace(text, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);
            text = Regex.Replace(text, @"//[^\r\n]*", string.Empty);

            Assert.Matches(@"current\s*>=\s*\(onReleaseTime\s*\+\s*TimeSpan\.FromMilliseconds\(100\.0\)\)", text);
            Assert.DoesNotMatch(@"current\s*<\s*\(onReleaseTime", text);
        }
    }
}
