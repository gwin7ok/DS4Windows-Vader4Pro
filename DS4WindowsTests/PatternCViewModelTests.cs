using System;
using System.IO.Packaging;
using System.Windows;
using Xunit;
using DS4Windows;
using DS4Windows.DI;
using DS4WinWPF;
using DS4WinWPF.DS4Forms.ViewModels;

namespace DS4WindowsTests
{
    public class PatternCViewModelTests
    {
        public PatternCViewModelTests()
        {
            if (Application.Current == null)
            {
                try
                {
                    new Application();
                }
                catch { }
            }
        }

        [Fact]
        public void AppHost_ShouldResolve_IViewModelFactory()
        {
            DS4WinWPF.AppHost.CreateHost();

            var factory = DS4WinWPF.AppHost.GetService<IViewModelFactory>();
            Assert.NotNull(factory);
        }

        [Fact]
        public void ViewModelFactory_ShouldCreate_ProfileSettingsViewModel()
        {
            DS4WinWPF.AppHost.CreateHost();

            var factory = DS4WinWPF.AppHost.GetService<IViewModelFactory>();
            Assert.NotNull(factory);

            var vm = factory.CreateProfileSettingsViewModel(0);
            Assert.NotNull(vm);
        }

        [Fact]
        public void ViewModelFactory_ShouldCreate_RecordBoxViewModel()
        {
            DS4WinWPF.AppHost.CreateHost();

            var factory = DS4WinWPF.AppHost.GetService<IViewModelFactory>();
            Assert.NotNull(factory);

            var settings = new DS4ControlSettings(DS4Controls.Cross);
            var vm = factory.CreateRecordBoxViewModel(0, settings, true, false);
            Assert.NotNull(vm);
        }

        [Fact]
        public void ViewModelFactory_ShouldCreate_AutoProfilesViewModel()
        {
            DS4WinWPF.AppHost.CreateHost();

            var factory = DS4WinWPF.AppHost.GetService<IViewModelFactory>();
            Assert.NotNull(factory);

            var profileList = new ProfileList();
            var holder = new AutoProfileHolder(new ControlService());
            var vm = factory.CreateAutoProfilesViewModel(holder, profileList);
            Assert.NotNull(vm);
        }
    }
}
