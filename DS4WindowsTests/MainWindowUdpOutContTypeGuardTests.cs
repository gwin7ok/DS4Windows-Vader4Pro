using System.IO;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase6-Step5-3: UDP 診断コマンド `query.&lt;device&gt;.outconttype` の分岐（`MainWindow.xaml.cs`）が、
    /// 正本である永続設定 `profileSettingsService.OutContType[tdevice]` を返し、
    /// 削除済みの孤立 API（`GetOutputDeviceType`）へ戻っていないことを固定する回帰ガード。
    /// UDP 受信処理は `MainWindow` の中にあり単体で駆動できないため、ソース走査で担保する。
    /// ソースファイルが見つからない環境では、検査を行わず合格とする。
    /// </summary>
    public class MainWindowUdpOutContTypeGuardTests
    {
        private readonly ITestOutputHelper _output;

        public MainWindowUdpOutContTypeGuardTests(ITestOutputHelper output)
        {
            _output = output;
        }

        private string ReadSourceWithoutComments()
        {
            string path = SourceFileLocator.Find("DS4Windows/DS4Forms/MainWindow.xaml.cs");
            if (path == null)
            {
                _output.WriteLine("MainWindow.xaml.cs が見つからないため検査を省略した。");
                return null;
            }

            string text = File.ReadAllText(path);
            text = Regex.Replace(text, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);
            text = Regex.Replace(text, @"//[^\r\n]*", string.Empty);
            return text;
        }

        [Fact]
        public void OutContTypeQuery_ReturnsPersistentProfileSetting()
        {
            string text = ReadSourceWithoutComments();
            if (text == null) return;

            Match m = Regex.Match(text,
                @"propName\s*==\s*""outconttype""\s*\)\s*propValue\s*=\s*([^;]+);");
            Assert.True(m.Success, "outconttype の分岐が見つからない（分岐の書き方の変更？ガードの見直しが必要）");
            Assert.Equal("profileSettingsService.OutContType[tdevice].ToString()", m.Groups[1].Value.Trim());
        }

        [Fact]
        public void MainWindow_DoesNotUseRemovedGetOutputDeviceType()
        {
            string text = ReadSourceWithoutComments();
            if (text == null) return;

            Assert.DoesNotContain("GetOutputDeviceType", text);
        }
    }
}
