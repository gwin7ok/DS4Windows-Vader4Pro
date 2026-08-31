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
        public void AppHost_ShouldResolve_RecordBoxViewModel()
        {
            DS4WinWPF.AppHost.CreateHost();

            var vm = DS4WinWPF.AppHost.GetService<RecordBoxViewModel>();
            Assert.NotNull(vm);
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
    }
}
