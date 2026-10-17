using System.IO;
using System.Text.RegularExpressions;
using DS4Windows;
using Xunit;
using Xunit.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase7-Step5 W1: DS3 コントローラーのジャイロトリガー判定（<see cref="DS4SixAxis.handleDS3Sixaxis"/>）の特性化テストと回帰ガード。
    /// 不具合: DI 移行（コミット 78ae8687）以降、<c>DS4SixAxis</c> の <c>_controlService</c> は常に null で、DS3 の接続中に
    /// ジャイロの処理が <c>NullReferenceException</c> になり得た。移行前は <c>App.rootHub.touchPad[device]</c> を静的に参照して動いていた。
    /// 修正後は、<c>ControlService</c> が設定する関数 <see cref="DS4SixAxis.GyroControlsTriggerActive"/> を使う。
    /// </summary>
    public class Phase7Step5Ds3GyroTriggerTests
    {
        private readonly ITestOutputHelper _output;

        public Phase7Step5Ds3GyroTriggerTests(ITestOutputHelper output)
        {
            _output = output;
        }

        private static unsafe void FeedDs3Report(DS4SixAxis sixAxis, int slot)
        {
            // gyro 2 バイト、accel 6 バイト（DS3Device が渡す形）。加速度は中立値（512）と異なる値にして、イベントの発火まで通す
            byte[] gyro = { 0x10, 0x02, 0, 0, 0, 0 };
            byte[] accel = { 0x00, 0x01, 0x00, 0x01, 0x00, 0x01 }; // 256（中立値 512 と異なる）
            var state = new DS4State();
            fixed (byte* pGyro = gyro, pAccel = accel)
            {
                sixAxis.handleDS3Sixaxis(pGyro, pAccel, state, 0.004, slot);
            }
        }

        [Fact]
        public void HandleDs3Sixaxis_WithoutTheTriggerFunction_DoesNotThrow()
        {
            var sixAxis = new DS4SixAxis();

            var ex = Record.Exception(() => FeedDs3Report(sixAxis, 0));

            Assert.Null(ex);
        }

        [Fact]
        public void HandleDs3Sixaxis_AsksTheTriggerFunctionOnce_WithTheSlotNumber()
        {
            var sixAxis = new DS4SixAxis();
            var asked = new System.Collections.Generic.List<int>();
            sixAxis.GyroControlsTriggerActive = slot => { asked.Add(slot); return false; };

            FeedDs3Report(sixAxis, 3);

            Assert.Equal(new[] { 3 }, asked);
        }

        [Fact]
        public void HandleDs3Sixaxis_WithAnActiveTrigger_StillProcessesTheReport()
        {
            var sixAxis = new DS4SixAxis();
            sixAxis.GyroControlsTriggerActive = slot => true;
            int moved = 0;
            sixAxis.SixAccelMoved += (sender, args) => moved++;

            var ex = Record.Exception(() => FeedDs3Report(sixAxis, 0));

            Assert.Null(ex);
            Assert.Equal(1, moved);
        }

        // ---- 回帰ガード（ソース）----

        private string Read(string relativePath)
        {
            string path = SourceFileLocator.Find(relativePath);
            if (path == null)
            {
                _output.WriteLine("ソースが見つからないため検査を省略した: " + relativePath);
                return null;
            }

            string text = File.ReadAllText(path);
            text = Regex.Replace(text, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);
            return Regex.Replace(text, @"//[^\r\n]*", string.Empty);
        }

        [Fact]
        public void DS4SixAxis_DoesNotReferenceControlServiceOrRootHub()
        {
            string text = Read("DS4Windows/DS4Library/DS4SixAxis.cs");
            if (text == null) return;

            Assert.DoesNotContain("Program.rootHub", text);
            Assert.DoesNotContain("_controlService", text);
            Assert.DoesNotMatch(@"public\s+DS4SixAxis\s*\(\s*(DS4Windows\.)?ControlService", text);
            Assert.Contains("GyroControlsTriggerActive", text);
        }

        [Fact]
        public void ControlService_WiresTheTriggerFunctionWhenConnectingADevice()
        {
            string text = Read("DS4Windows/DS4Control/ControlService.cs");
            if (text == null) return;

            Assert.Matches(@"device\.SixAxis\.GyroControlsTriggerActive\s*=", text);
        }
    }
}
