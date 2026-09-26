using System;
using Xunit;
using DS4Windows;
using DS4Windows.DI;
using DS4WinWPF;
using DS4WinWPF.DS4Forms.ViewModels;

namespace DS4WindowsTests
{
    public class PatternBViewModelTests
    {
        [Fact]
        public void AppHost_ShouldResolve_ControllersViewModel()
        {
            DS4WinWPF.AppHost.CreateHost();

            var vm = DS4WinWPF.AppHost.GetService<ControllersViewModel>();
            Assert.NotNull(vm);
        }

        [Fact]
        public void AppHost_ShouldResolve_MainWindowsViewModel()
        {
            DS4WinWPF.AppHost.CreateHost();

            var vm = DS4WinWPF.AppHost.GetService<MainWindowsViewModel>();
            Assert.NotNull(vm);
        }

        [Fact]
        public void ControllersViewModel_ShouldBeSingleton()
        {
            DS4WinWPF.AppHost.CreateHost();

            var vm1 = DS4WinWPF.AppHost.GetService<ControllersViewModel>();
            var vm2 = DS4WinWPF.AppHost.GetService<ControllersViewModel>();

            Assert.NotNull(vm1);
            Assert.NotNull(vm2);
            Assert.Same(vm1, vm2);
        }

        [Fact]
        public void MainWindowsViewModel_ShouldBeSingleton()
        {
            DS4WinWPF.AppHost.CreateHost();

            var vm1 = DS4WinWPF.AppHost.GetService<MainWindowsViewModel>();
            var vm2 = DS4WinWPF.AppHost.GetService<MainWindowsViewModel>();

            Assert.NotNull(vm1);
            Assert.NotNull(vm2);
            Assert.Same(vm1, vm2);
        }
    }
}
