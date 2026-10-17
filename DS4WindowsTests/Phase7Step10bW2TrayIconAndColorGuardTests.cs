using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using DS4Windows;
using DS4WinWPF.DS4Forms;
using Xunit;
using Xunit.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase7-Step10b W2（Q1・Q2＝A）: トレイアイコンの資源の表を <c>TrayIconResources</c> へ、色の純粋関数を <c>ColorUtil</c> へ
    /// 移したことの回帰ガード。内容・計算は従来と同じでなければならない。
    /// ソースが見つからない環境では、ソースの検査を行わず合格とする。
    /// </summary>
    public class Phase7Step10bW2TrayIconAndColorGuardTests
    {
        private readonly ITestOutputHelper _output;

        public Phase7Step10bW2TrayIconAndColorGuardTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void TrayIconResources_HaveTheSameFiveEntriesAsBefore()
        {
            var table = TrayIconResources.iconChoiceResources;
            Assert.Equal(5, table.Count);
            Assert.Equal("/DS4Windows;component/Resources/DS4W.ico", table[TrayIconChoice.Default]);
            Assert.Equal("/DS4Windows;component/Resources/DS4W.ico", table[TrayIconChoice.Colored]);
            Assert.Equal("/DS4Windows;component/Resources/DS4W - White.ico", table[TrayIconChoice.White]);
            Assert.Equal("/DS4Windows;component/Resources/DS4W - Black.ico", table[TrayIconChoice.Black]);
            Assert.Equal("/DS4Windows;component/Resources/DS4W.ico", table[TrayIconChoice.Battery]);
        }

        [Fact]
        public void TheAppearanceService_ResolvesThePathFromTheMovedTable()
        {
            var service = DS4WinWPF.AppHost.GetService<DS4Windows.DI.IAppearanceSettingsService>();
            Assert.Equal(TrayIconResources.iconChoiceResources[TrayIconChoice.White], service.GetIconResourcePath(TrayIconChoice.White));
        }

        [Theory]
        [InlineData(0.0, 10, 20, 30)]      // 比率 0: 1 つ目の色
        [InlineData(100.0, 200, 100, 50)]  // 比率 100: 2 つ目の色
        [InlineData(50.0, 105, 60, 40)]    // 中間（四捨五入。Math.Round の既定は偶数丸め）
        [InlineData(-10.0, 10, 20, 30)]    // 0 未満は 0 に丸める
        [InlineData(250.0, 200, 100, 50)]  // 100 超は 100 に丸める
        public void GetTransitionedColor_BlendsAsBefore(double ratio, byte red, byte green, byte blue)
        {
            var c1 = new DS4Color(10, 20, 30);
            var c2 = new DS4Color(200, 100, 50);
            DS4Color result = ColorUtil.getTransitionedColor(ref c1, ref c2, ratio);
            Assert.Equal(red, result.red);
            Assert.Equal(green, result.green);
            Assert.Equal(blue, result.blue);
        }

        [Fact]
        public void TheSource_HasNoColorOrIconMembersOnGlobalAnymore()
        {
            string anchor = SourceFileLocator.Find("DS4Windows/DS4Control/Global.cs");
            if (anchor == null)
            {
                _output.WriteLine("ソースが見つからないため検査を省略した。");
                return;
            }

            string text = Regex.Replace(Regex.Replace(File.ReadAllText(anchor), @"/\*.*?\*/", string.Empty, RegexOptions.Singleline), @"//[^\r\n]*", string.Empty);
            foreach (string name in new[] { "getTransitionedColor", "HuetoRGB", "applyRatio", "iconChoiceResources" })
                Assert.False(Regex.IsMatch(text, @"\b" + name + @"\b"), "Global.cs に " + name + " が残っている");
        }
    }
}
