using System;
using Xunit;
using DS4Windows;
using DS4Windows.DI;
using DS4WinWPF;
using DS4WinWPF.DS4Forms.ViewModels;

namespace DS4WindowsTests
{
    public class PatternAViewModelTests
    {
        [Fact]
        public void AppHost_ShouldResolve_SettingsViewModel()
        {
            DS4WinWPF.AppHost.CreateHost();

            var vm = DS4WinWPF.AppHost.GetService<SettingsViewModel>();
            Assert.NotNull(vm);
        }

        [Fact]
        public void AppHost_ShouldResolve_LogViewModel()
        {
            DS4WinWPF.AppHost.CreateHost();

            var vm = DS4WinWPF.AppHost.GetService<LogViewModel>();
            Assert.NotNull(vm);
        }

        [Fact]
        public void AppHost_ShouldResolve_AboutViewModel()
        {
            DS4WinWPF.AppHost.CreateHost();

            var vm = DS4WinWPF.AppHost.GetService<AboutViewModel>();
            Assert.NotNull(vm);
            Assert.Contains("DS4Windows", vm.AppTitle);
            Assert.False(string.IsNullOrWhiteSpace(vm.VersionText));
            Assert.False(string.IsNullOrWhiteSpace(vm.GithubUrl));
        }

        [Fact]
        public void PatternAViewModels_ShouldBeTransient()
        {
            DS4WinWPF.AppHost.CreateHost();

            var vm1 = DS4WinWPF.AppHost.GetService<SettingsViewModel>();
            var vm2 = DS4WinWPF.AppHost.GetService<SettingsViewModel>();

            Assert.NotNull(vm1);
            Assert.NotNull(vm2);
            Assert.NotSame(vm1, vm2);
        }

        // Phase5-Watchpoints-Investigation-Report Watchpoint 2対応:
        // SettingsViewModelはTransient(上のテストで確認済み)であり、画面を開くたびに
        // 新しいインスタンスが生成される。SystemEvents.DisplaySettingsChanged
        // (Microsoft.Win32のアプリ生存期間の静的イベント)への購読を解除しないと、
        // 開くたびに古いインスタンスがGCされずメモリリークする。
        // SystemEventsはOSからのみ発火可能で単体テストから直接再発火できないため、
        // ここではDispose()の存在・冪等性(複数回呼び出しても例外を投げないこと)を検証する。
        [Fact]
        public void SettingsViewModel_ShouldImplementIDisposable()
        {
            Assert.True(typeof(IDisposable).IsAssignableFrom(typeof(SettingsViewModel)));
        }

        [Fact]
        public void Dispose_CalledMultipleTimes_ShouldNotThrow()
        {
            DS4WinWPF.AppHost.CreateHost();
            var vm = DS4WinWPF.AppHost.GetService<SettingsViewModel>();

            var ex1 = Record.Exception(() => vm.Dispose());
            var ex2 = Record.Exception(() => vm.Dispose());

            Assert.Null(ex1);
            Assert.Null(ex2);
        }
    }
}