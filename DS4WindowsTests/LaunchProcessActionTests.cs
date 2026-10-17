using System;
using Xunit;
using DS4Windows;
using DS4Windows.Actions;

namespace DS4WindowsTests
{
    /// <summary>
    /// LaunchProcessAction（スペシャルアクション「プログラム起動」）の単体テスト。
    /// Phase6-Step9b-3（F06）: DI 移行前（Mapping.cs の specActionLaunchProc）の仕様へ戻したことを固定する。
    /// - extra に "$hidden" を含むときだけ、その語を 1 回取り除き、非表示で起動する（.bat/.cmd は %COMSPEC% /C）。
    /// - それ以外は UseShellExecute = true、非表示にせず、extra をそのまま引数に渡す。
    /// </summary>
    public class LaunchProcessActionTests
    {
        private static MockProcessLauncher.LaunchCall Run(string details, string extra, out MockProcessLauncher mock)
        {
            mock = new MockProcessLauncher();
            var sa = new SpecialAction("LaunchProg", "Cross", "Key", "Key", 0, "");
            sa.details = details;
            sa.extra = extra;

            var action = new LaunchProcessAction(sa, mock);
            action.Execute(new OutputContextImpl(0, null));

            Assert.Single(mock.Calls);
            return mock.Calls[0];
        }

        [Fact]
        public void T4_LaunchBatchFile_WithHiddenKeyword_WrapsWithComspecAndHidesWindow()
        {
            var call = Run("C:\\scripts\\test.bat", "-a $hidden -b", out _);

            Assert.Contains("cmd.exe", call.FileName, StringComparison.OrdinalIgnoreCase);
            Assert.Equal("/C \"C:\\scripts\\test.bat\" -a  -b", call.Arguments);
            Assert.True(call.UseShellExecute);
            Assert.True(call.Hidden);
        }

        [Fact]
        public void LaunchBatchFile_WithoutHiddenKeyword_LaunchesDirectlyAndNotHidden()
        {
            var call = Run("C:\\scripts\\test.bat", "-a", out _);

            Assert.Equal("C:\\scripts\\test.bat", call.FileName);
            Assert.Equal("-a", call.Arguments);
            Assert.True(call.UseShellExecute);
            Assert.False(call.Hidden);
        }

        [Fact]
        public void LaunchExe_WithoutExtra_UsesShellExecuteAndIsNotHidden()
        {
            var call = Run("notepad.exe", null, out _);

            Assert.Equal("notepad.exe", call.FileName);
            Assert.Equal(string.Empty, call.Arguments);
            Assert.True(call.UseShellExecute);
            Assert.False(call.Hidden);
        }

        [Fact]
        public void LaunchExe_WithArgumentsOnly_PassesArgumentsAsIs()
        {
            var call = Run("C:\\app\\tool.exe", "--mode fast", out _);

            Assert.Equal("C:\\app\\tool.exe", call.FileName);
            Assert.Equal("--mode fast", call.Arguments);
            Assert.True(call.UseShellExecute);
            Assert.False(call.Hidden);
        }

        [Fact]
        public void LaunchExe_WithHiddenKeyword_RemovesKeywordOnceIgnoringCaseAndHidesWindow()
        {
            var call = Run("C:\\app\\tool.exe", "--x $HIDDEN --y", out _);

            Assert.Equal("C:\\app\\tool.exe", call.FileName);
            Assert.Equal("--x  --y", call.Arguments);
            Assert.True(call.UseShellExecute);
            Assert.True(call.Hidden);
        }

        [Fact]
        public void T6_MultipleExecutions_RecordsAllCallsCorrectly()
        {
            var mock = new MockProcessLauncher();
            var sa = new SpecialAction("LaunchProg", "Cross", "Key", "Key", 0, "");
            sa.details = "notepad.exe";

            var action = new LaunchProcessAction(sa, mock);
            var ctx = new OutputContextImpl(0, null);

            action.Execute(ctx);
            action.Execute(ctx);

            Assert.Equal(2, mock.Calls.Count);
        }
    }
}
