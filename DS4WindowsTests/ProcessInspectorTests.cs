using System;
using System.Diagnostics;
using Xunit;
using DS4Windows.Services;

namespace DS4WindowsTests
{
    public class ProcessInspectorTests
    {
        [Fact]
        public void IsProcessRunning_CurrentProcess_ReturnsTrue()
        {
            var inspector = new DefaultProcessInspector();
            string currentExe = Process.GetCurrentProcess().MainModule.FileName;

            bool result = inspector.IsProcessRunning(currentExe);

            Assert.True(result);
        }

        [Fact]
        public void IsProcessRunning_NonExistentPath_ReturnsFalse()
        {
            var inspector = new DefaultProcessInspector();
            bool result = inspector.IsProcessRunning(@"C:\non_existent_process_12345.exe");

            Assert.False(result);
        }

        [Fact]
        public void IsProcessRunning_EmptyPath_ReturnsFalse()
        {
            var inspector = new DefaultProcessInspector();
            bool result = inspector.IsProcessRunning("");

            Assert.False(result);
        }

        [Fact]
        public void GetForegroundProcessInfo_ReturnsBooleanWithoutException()
        {
            var inspector = new DefaultProcessInspector();
            string path;
            string title;

            var ex = Record.Exception(() => inspector.GetForegroundProcessInfo(out path, out title));

            Assert.Null(ex);
        }
    }
}
