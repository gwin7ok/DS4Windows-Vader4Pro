using System.IO;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// 2026-09-26（ユーザー決定、Phase6-Step7b の実機確認中に追加）: スティックの Dead Zone の入力欄 4 つ
    /// （ProfileEditor.xaml の LSDeadZone／RSDeadZone、AxialStickUserControl.xaml の DeadZoneX／DeadZoneY）は、
    /// Six Axis の Dead Zone と同じく、値が変わるたびに設定へ書き込む（UpdateSourceTrigger=LostFocus を付けない）。
    /// LostFocus だと、▲で増やしても入力欄からフォーカスが外れるまで Controller Readings の円が更新されない。
    /// その指定の再混入を検出するソース走査ガード。ソースファイルが見つからない環境では、検査を行わず合格とする。
    /// </summary>
    public class StickDeadZoneBindingGuardTests
    {
        private readonly ITestOutputHelper _output;

        public StickDeadZoneBindingGuardTests(ITestOutputHelper output)
        {
            _output = output;
        }

        private string ReadXamlWithoutComments(string relativePath)
        {
            string path = SourceFileLocator.Find(relativePath);
            if (path == null)
            {
                _output.WriteLine($"{relativePath} が見つからないため検査を省略した。");
                return null;
            }

            return Regex.Replace(File.ReadAllText(path), @"<!--.*?-->", string.Empty, RegexOptions.Singleline);
        }

        [Theory]
        [InlineData("DS4Windows/DS4Forms/ProfileEditor.xaml", "LSDeadZone")]
        [InlineData("DS4Windows/DS4Forms/ProfileEditor.xaml", "RSDeadZone")]
        [InlineData("DS4Windows/DS4Forms/AxialStickUserControl.xaml", "DeadZoneX")]
        [InlineData("DS4Windows/DS4Forms/AxialStickUserControl.xaml", "DeadZoneY")]
        public void StickDeadZone_IsWrittenOnEveryChange(string relativePath, string property)
        {
            string xaml = ReadXamlWithoutComments(relativePath);
            if (xaml == null) return;

            Assert.Contains($"Value=\"{{Binding {property}}}\"", xaml);
            Assert.DoesNotContain($"{{Binding {property},UpdateSourceTrigger=LostFocus}}", xaml);
        }
    }
}
