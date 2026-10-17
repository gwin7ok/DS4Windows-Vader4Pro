using System.IO;
using System.Text.RegularExpressions;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// ProfileEditor のホバー表示用インデックスに、タッチパッド 4 領域がすべて登録されていること
    /// （Left Touch の登録欠落の回帰防止。2026-10-15）。
    /// </summary>
    public class ProfileEditorHoverIndexTests
    {
        private static string LoadSource()
        {
            string path = SourceFileLocator.Find("DS4Windows/DS4Forms/ProfileEditor.xaml.cs");
            if (path == null) return null;
            string text = File.ReadAllText(path);
            text = Regex.Replace(text, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);
            text = Regex.Replace(text, @"//[^\r\n]*", string.Empty);
            return text;
        }

        [Fact]
        public void HoverIndexes_RegisterAllFourTouchpadRegions()
        {
            string text = LoadSource();
            if (text == null) return;

            Assert.Matches(@"hoverIndexes\[leftTouchConBtn\]\s*=\s*mappingListVM\.ControlIndexMap\[DS4Controls\.TouchLeft\]", text);
            Assert.Matches(@"hoverIndexes\[rightTouchConBtn\]\s*=\s*mappingListVM\.ControlIndexMap\[DS4Controls\.TouchRight\]", text);
            Assert.Matches(@"hoverIndexes\[multiTouchConBtn\]\s*=\s*mappingListVM\.ControlIndexMap\[DS4Controls\.TouchMulti\]", text);
            Assert.Matches(@"hoverIndexes\[topTouchConBtn\]\s*=\s*mappingListVM\.ControlIndexMap\[DS4Controls\.TouchUpper\]", text);
        }

        [Fact]
        public void HoverIndexes_BrpConBtn_IsAssignedOnlyOnce()
        {
            string text = LoadSource();
            if (text == null) return;

            Assert.Single(Regex.Matches(text, @"hoverIndexes\[brpConBtn\]\s*="));
        }
    }
}
