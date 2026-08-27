using Xunit;
using DS4Windows;
using DS4Windows.Actions;

namespace DS4WindowsTests
{
    public class LaunchProcessActionTests
    {
        [Fact]
        public void T4_LaunchBatchFile_WrapsWithCmdExe()
        {
            var mock = new MockProcessLauncher();
            var sa = new SpecialAction("LaunchProg", "Cross", "Key", "Key", 0, "");
            sa.details = "C:\\scripts\\test.bat";

            var action = new LaunchProcessAction(sa, mock);
            var ctx = new OutputContextImpl(0, null);

            action.Execute(ctx);

            Assert.Single(mock.Calls);
            Assert.Contains("cmd.exe", mock.Calls[0].FileName);
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
