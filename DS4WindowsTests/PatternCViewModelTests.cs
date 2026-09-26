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

            if (Application.ResourceAssembly == null)
            {
                try
                {
                    Application.ResourceAssembly = typeof(DS4WinWPF.AppHost).Assembly;
                }
                catch { }
            }

            if (Application.Current != null)
            {
                try
                {
                    var uri = new Uri("pack://application:,,,/DS4Windows;component/Resources/Resources.xaml", UriKind.Absolute);
                    var dict = new ResourceDictionary { Source = uri };
                    Application.Current.Resources.MergedDictionaries.Add(dict);
                }
                catch { }

                string[] resourceKeys = new string[]
                {
                    "KeyDownImg", "KeyUpImg", "KeyWaitImg", "KeyHoldImg",
                    "ClockImg", "MouseImg", "CustomKeyImg", "TouchpadImg",
                    "GyroImg", "WheelImg", "UnboundImg", "WaitImg", "HoldImg",
                    "LeftImg", "RightImg", "MiddleImg", "Clock", "Mouse", "CustomKey"
                };

                foreach (var key in resourceKeys)
                {
                    if (!Application.Current.Resources.Contains(key))
                    {
                        Application.Current.Resources[key] = "";
                    }
                }
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
            // targetDevice を省略した場合は「実機なし」（Phase6-Step7b-1）
            Assert.Equal(-1, vm.TargetDevice);
        }

        /// <summary>
        /// Phase6-Step7b-1: 実際の DI ホストから解決したファクトリ経由でも、targetDevice が ViewModel に届き、
        /// 編集スロット（device）と別に保持されること。テスト用に new したファクトリではなく、
        /// アプリと同じ AppHost.GetService の経路で確認する（Phase6-Status.md K7-1 の教訓）。
        /// </summary>
        [Fact]
        public void ViewModelFactory_PassesTargetDevice_ToProfileSettingsViewModel()
        {
            DS4WinWPF.AppHost.CreateHost();

            var factory = DS4WinWPF.AppHost.GetService<IViewModelFactory>();
            Assert.NotNull(factory);

            OutContType savedOutDevTypeTemp = Global.outDevTypeTemp[Global.TEST_PROFILE_INDEX];
            try
            {
                // CURRENT_DS4_CONTROLLER_LIMIT は 4 または 8 のため、どちらでも範囲内の 1 を使う
                var vm = factory.CreateProfileSettingsViewModel(Global.TEST_PROFILE_INDEX, 1);
                Assert.NotNull(vm);
                Assert.Equal(Global.TEST_PROFILE_INDEX, vm.Device);
                Assert.Equal(1, vm.TargetDevice);
                Assert.Equal(1, vm.FuncDevNum);
            }
            finally
            {
                Global.outDevTypeTemp[Global.TEST_PROFILE_INDEX] = savedOutDevTypeTemp;
            }
        }

        [Fact]
        public void ViewModelFactory_ShouldCreate_RecordBoxViewModel()
        {
            DS4WinWPF.AppHost.CreateHost();

            var factory = DS4WinWPF.AppHost.GetService<IViewModelFactory>();
            Assert.NotNull(factory);

            var settings = new DS4ControlSettings(DS4Controls.Cross);
            // Phase6-Step7b-2: targetDevice を省略（-1＝実機なし）するため、以前のように
            // TouchOutMode[0] を Passthru にしたまま戻さない副作用は起きない
            var vm = factory.CreateRecordBoxViewModel(0, settings, true, false);
            Assert.NotNull(vm);
            Assert.Equal(-1, vm.TargetDevice);
        }

        /// <summary>
        /// Phase6-Step7b-2: 実際の DI ホストから解決したファクトリ経由でも、targetDevice が RecordBoxViewModel に届き、
        /// 記録中のタッチパッド Passthru が実機のスロットに設定され、RevertControlsSettings で元に戻ること
        /// （Phase6-Status.md K7-1 の教訓により、アプリと同じ AppHost.GetService の経路で確認する）。
        /// </summary>
        [Fact]
        public void ViewModelFactory_PassesTargetDevice_ToRecordBoxViewModel()
        {
            DS4WinWPF.AppHost.CreateHost();

            var factory = DS4WinWPF.AppHost.GetService<IViewModelFactory>();
            Assert.NotNull(factory);
            var profileSettings = DS4WinWPF.AppHost.GetService<IProfileSettingsService>();
            Assert.NotNull(profileSettings);

            // CURRENT_DS4_CONTROLLER_LIMIT は 4 または 8 のため、どちらでも範囲内の 1 を使う
            const int targetSlot = 1;
            TouchpadOutMode savedTargetMode = profileSettings.TouchOutMode[targetSlot];
            TouchpadOutMode savedEditSlotMode = profileSettings.TouchOutMode[Global.TEST_PROFILE_INDEX];
            RecordBoxViewModel vm = null;
            try
            {
                profileSettings.TouchOutMode[targetSlot] = TouchpadOutMode.Mouse;
                profileSettings.TouchOutMode[Global.TEST_PROFILE_INDEX] = TouchpadOutMode.Mouse;

                vm = factory.CreateRecordBoxViewModel(Global.TEST_PROFILE_INDEX,
                    new DS4ControlSettings(DS4Controls.Cross), true, false, targetSlot);
                Assert.NotNull(vm);
                Assert.Equal(Global.TEST_PROFILE_INDEX, vm.DeviceNum);
                Assert.Equal(targetSlot, vm.TargetDevice);
                Assert.Equal(TouchpadOutMode.Passthru, profileSettings.TouchOutMode[targetSlot]);
                Assert.Equal(TouchpadOutMode.Mouse, profileSettings.TouchOutMode[Global.TEST_PROFILE_INDEX]);

                vm.RevertControlsSettings();
                Assert.Equal(TouchpadOutMode.Mouse, profileSettings.TouchOutMode[targetSlot]);
            }
            finally
            {
                vm?.RevertControlsSettings();
                profileSettings.TouchOutMode[targetSlot] = savedTargetMode;
                profileSettings.TouchOutMode[Global.TEST_PROFILE_INDEX] = savedEditSlotMode;
            }
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
