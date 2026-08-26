using System;
using Xunit;
using DS4Windows;
using DS4Windows.Actions;

namespace DS4WindowsTests
{
    /// <summary>
    /// C5 LaunchProcessAction 単体テスト (T1〜T6)
    /// </summary>
    public class LaunchProcessActionTests
    {
        private static SpecialAction CreateSpecialAction(string details, string extra = "")
        {
            return new SpecialAction("TestAction", "None", "Program", details, 0.0, extra)
            {
                typeID = SpecialAction.ActionTypeId.Program
            };
        }

        [Fact]
        public void T1_LaunchSimpleExecutable_CallsLauncherWithCorrectPath()
        {
            // Arrange
            var mockLauncher = new MockProcessLauncher();
            var sa = CreateSpecialAction("notepad.exe", string.Empty);
            var action = new LaunchProcessAction(sa, mockLauncher);

            // Act
            action.Execute(null);

            // Assert
            Assert.True(mockLauncher.LaunchCalled);
            Assert.Equal("notepad.exe", mockLauncher.LastPath);
            Assert.False(mockLauncher.LastHidden);
        }

        [Fact]
        public void T2_LaunchWithArguments_SeparatesPathAndArguments()
        {
            // Arrange
            var mockLauncher = new MockProcessLauncher();
            var sa = CreateSpecialAction("notepad.exe", "C:\\test\\document.txt");
            var action = new LaunchProcessAction(sa, mockLauncher);

            // Act
            action.Execute(null);

            // Assert
            Assert.True(mockLauncher.LaunchCalled);
            Assert.Equal("notepad.exe", mockLauncher.LastPath);
            Assert.Equal("C:\\test\\document.txt", mockLauncher.LastArguments);
        }

        [Fact]
        public void T3_LaunchWithHiddenFlag_SetsHiddenTrueAndStripsPlaceholder()
        {
            // Arrange
            var mockLauncher = new MockProcessLauncher();
            var sa = CreateSpecialAction("notepad.exe", "$hidden arg1");
            var action = new LaunchProcessAction(sa, mockLauncher);

            // Act
            action.Execute(null);

            // Assert
            Assert.True(mockLauncher.LaunchCalled);
            Assert.True(mockLauncher.LastHidden);
            // $hidden が引数文字列から除外され "arg1" のみが残ること
            Assert.DoesNotContain("$hidden", mockLauncher.LastArguments ?? string.Empty);
        }

        [Fact]
        public void T4_LaunchBatchFile_SetsUseShellExecuteTrue()
        {
            // Arrange
            var mockLauncher = new MockProcessLauncher();
            var sa = CreateSpecialAction("C:\\scripts\\test.bat", string.Empty);
            var action = new LaunchProcessAction(sa, mockLauncher);

            // Act
            action.Execute(null);

            // Assert
            Assert.True(mockLauncher.LaunchCalled);
            Assert.Equal("C:\\scripts\\test.bat", mockLauncher.LastPath);
            Assert.True(mockLauncher.LastUseShellExecute);
        }

        [Fact]
        public void T5_LaunchInvalidOrEmptyPath_DoesNotThrow()
        {
            // Arrange
            var mockLauncher = new MockProcessLauncher();
            var sa = CreateSpecialAction(string.Empty, string.Empty);
            var action = new LaunchProcessAction(sa, mockLauncher);

            // Act & Assert (例外がスローされず安全に終了すること)
            var exception = Record.Exception(() => action.Execute(null));
            Assert.Null(exception);
            Assert.False(mockLauncher.LaunchCalled);
        }

        [Fact]
        public void T6_MultipleExecutions_RecordsAllCallsCorrectly()
        {
            // Arrange
            var mockLauncher = new MockProcessLauncher();
            var sa = CreateSpecialAction("notepad.exe", string.Empty);
            var action = new LaunchProcessAction(sa, mockLauncher);

            // Act
            action.Execute(null);
            action.Execute(null);

            // Assert
            Assert.Equal(2, mockLauncher.Calls.Count);
            Assert.Equal("notepad.exe", mockLauncher.Calls[0].FileName);
            Assert.Equal("notepad.exe", mockLauncher.Calls[1].FileName);

            // Reset 検証
            mockLauncher.Reset();
            Assert.False(mockLauncher.LaunchCalled);
            Assert.Empty(mockLauncher.Calls);
        }
    }
}