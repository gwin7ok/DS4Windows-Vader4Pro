using System;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using DS4Windows;
using DS4Windows.Actions;
using DS4Windows.DI;

namespace DS4WindowsTests
{
    /// <summary>
    /// C5 LaunchProcessAction 単体テスト (T1〜T6)
    /// </summary>
    public class LaunchProcessActionTests : IDisposable
    {
        private readonly MockProcessLauncher _mockLauncher;

        public LaunchProcessActionTests()
        {
            _mockLauncher = new MockProcessLauncher();
            var services = new ServiceCollection();
            services.AddSingleton<IProcessLauncher>(_mockLauncher);
            ServiceProviderHolder.SetProvider(services.BuildServiceProvider());
        }

        public void Dispose()
        {
            ServiceProviderHolder.SetProvider(null);
        }

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
            var sa = CreateSpecialAction("notepad.exe", string.Empty);
            var action = new LaunchProcessAction(sa);

            // Act
            action.Execute(null);

            // Assert
            Assert.True(_mockLauncher.LaunchCalled);
            Assert.Equal("notepad.exe", _mockLauncher.LastPath);
            Assert.False(_mockLauncher.LastHidden);
        }

        [Fact]
        public void T2_LaunchWithArguments_SeparatesPathAndArguments()
        {
            // Arrange
            var sa = CreateSpecialAction("notepad.exe", "C:\\test\\document.txt");
            var action = new LaunchProcessAction(sa);

            // Act
            action.Execute(null);

            // Assert
            Assert.True(_mockLauncher.LaunchCalled);
            Assert.Equal("notepad.exe", _mockLauncher.LastPath);
            Assert.Equal("C:\\test\\document.txt", _mockLauncher.LastArguments);
        }

        [Fact]
        public void T3_LaunchWithHiddenFlag_SetsHiddenTrueAndStripsPlaceholder()
        {
            // Arrange
            var sa = CreateSpecialAction("notepad.exe", "$hidden arg1");
            var action = new LaunchProcessAction(sa);

            // Act
            action.Execute(null);

            // Assert
            Assert.True(_mockLauncher.LaunchCalled);
            Assert.True(_mockLauncher.LastHidden);
            // $hidden が引数文字列から除外されていること
            Assert.DoesNotContain("$hidden", _mockLauncher.LastArguments ?? string.Empty);
        }

        [Fact]
        public void T4_LaunchBatchFile_WrapsWithCmdExe()
        {
            // Arrange
            var sa = CreateSpecialAction("C:\\scripts\\test.bat", string.Empty);
            var action = new LaunchProcessAction(sa);

            // Act
            action.Execute(null);

            // Assert
            Assert.True(_mockLauncher.LaunchCalled);
            // .bat は cmd.exe (COMSPEC) 経由でラップされて起動されること
            Assert.Contains("cmd.exe", _mockLauncher.LastPath, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("test.bat", _mockLauncher.LastArguments);
        }

        [Fact]
        public void T5_LaunchInvalidOrEmptyPath_DoesNotThrow()
        {
            // Arrange
            var sa = CreateSpecialAction(string.Empty, string.Empty);
            var action = new LaunchProcessAction(sa);

            // Act & Assert (例外がスローされず安全に終了し、Launch は呼ばれないこと)
            var exception = Record.Exception(() => action.Execute(null));
            Assert.Null(exception);
            Assert.False(_mockLauncher.LaunchCalled);
        }

        [Fact]
        public void T6_MultipleExecutions_RecordsAllCallsCorrectly()
        {
            // Arrange
            var sa = CreateSpecialAction("notepad.exe", string.Empty);
            var action = new LaunchProcessAction(sa);

            // Act
            action.Execute(null);
            action.Execute(null);

            // Assert
            Assert.Equal(2, _mockLauncher.Calls.Count);
            Assert.Equal("notepad.exe", _mockLauncher.Calls[0].FileName);
            Assert.Equal("notepad.exe", _mockLauncher.Calls[1].FileName);

            // Reset 検証
            _mockLauncher.Reset();
            Assert.False(_mockLauncher.LaunchCalled);
            Assert.Empty(_mockLauncher.Calls);
        }
    }
}