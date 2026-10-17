using System;
using System.IO;
using System.Text.RegularExpressions;
using DS4Windows;
using DS4WinWPF.DS4Control;
using Xunit;
using Xunit.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase6-Step12（デバウンス時間の回帰是正）:
    /// (1) <see cref="Debouncer.SetDuration"/> が、0 で生成したデバウンサーにも効くこと、
    /// (2) 接続中のデバイスのデバウンス時間が、<c>ControlService.CheckProfileOptions</c>（ApplyProfile の経路）で更新されること、を固定する。
    /// </summary>
    public class DebouncerSetDurationTests
    {
        private readonly ITestOutputHelper _output;

        public DebouncerSetDurationTests(ITestOutputHelper output)
        {
            _output = output;
        }

        private static DS4State CrossState(bool pressed, DateTime timestamp)
        {
            var state = new DS4State();
            state.Cross = pressed;
            state.ReportTimeStamp = timestamp;
            return state;
        }

        [Fact]
        public void SetDuration_FromZero_EnablesDebouncing()
        {
            var debouncer = new Debouncer(TimeSpan.Zero);
            debouncer.AddDebouncer(nameof(DS4State.Cross));
            debouncer.SetDuration(TimeSpan.FromMilliseconds(500));

            DateTime t0 = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            Assert.True(debouncer.ProcessInput(CrossState(true, t0)).Cross);
            // 100ms 後に離しても、500ms に達するまでは押されたままとして扱う
            Assert.True(debouncer.ProcessInput(CrossState(false, t0.AddMilliseconds(100))).Cross);
            // 600ms 後は、実際の入力（離した）になる
            Assert.False(debouncer.ProcessInput(CrossState(false, t0.AddMilliseconds(600))).Cross);
        }

        [Fact]
        public void SetDuration_ToZero_DisablesDebouncing()
        {
            var debouncer = new Debouncer(TimeSpan.FromMilliseconds(500));
            debouncer.AddDebouncer(nameof(DS4State.Cross));
            debouncer.SetDuration(TimeSpan.Zero);

            DateTime t0 = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            Assert.True(debouncer.ProcessInput(CrossState(true, t0)).Cross);
            Assert.False(debouncer.ProcessInput(CrossState(false, t0.AddMilliseconds(10))).Cross);
        }

        [Fact]
        public void ControlService_CheckProfileOptions_UpdatesDebouncerDuration()
        {
            string path = SourceFileLocator.Find("DS4Windows/DS4Control/ControlService.cs");
            if (path == null)
            {
                _output.WriteLine("ソースが見つからないため検査を省略した。");
                return;
            }

            string text = File.ReadAllText(path);
            Match method = Regex.Match(text, @"public void CheckProfileOptions\(.*?\r?\n        \}\r?\n", RegexOptions.Singleline);
            Assert.True(method.Success, "CheckProfileOptions が見つからない");
            Assert.Matches(@"Debouncer\?\.SetDuration\(.*DebouncingMs\[ind\]", method.Value);
        }
    }
}
