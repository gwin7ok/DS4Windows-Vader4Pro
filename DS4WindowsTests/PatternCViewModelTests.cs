using System;
using System.IO;
using System.Reflection;
using System.Windows;
using Xunit;
using DS4Windows;
using DS4Windows.DI;
using DS4WinWPF;
using DS4WinWPF.DS4Forms;
using DS4WinWPF.DS4Forms.ViewModels;

namespace DS4WindowsTests
{
    public class PatternCViewModelTests
    {
        static PatternCViewModelTests()
        {
            if (string.IsNullOrEmpty(Global.appdatapath))
            {
                Global.appdatapath = AppContext.BaseDirectory;
            }

            if (Application.Current == null)
            {
                try
                {
                    _ = new Application();
                }
                catch { }
            }

            if (Application.Current != null)
            {
                try
                {
                    string[] resourceKeys = new string[] { "KeyDownImg", "KeyUpImg", "KeyWaitImg", "KeyHoldImg", "MouseImg", "CustomKeyImg" };
                    foreach (var key in resourceKeys)
                    {
                        if (!Application.Current.Resources.Contains(key))
                        {
                            Application.Current.Resources[key] = "";
                        }
                    }
                }
                catch { }
            }

            if (Application.ResourceAssembly == null)
            {
                try
                {
                    Application.ResourceAssembly = typeof(DS4WinWPF.AppHost).Assembly;
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
            var holder = new AutoProfileHolder();
            var vm = factory.CreateAutoProfilesViewModel(holder, profileList);
            Assert.NotNull(vm);
        }
    }
}
