using System;
using System.Windows;
using Xunit;
using DS4Windows;
using DS4Windows.DI;
using DS4WinWPF.DS4Forms.ViewModels;

namespace DS4WindowsTests
{
    public class ProfileSettingsViewModelTests
    {
        static ProfileSettingsViewModelTests()
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
        }

        [Fact]
        public void ControllerTypeIndex_ShouldSupportBidirectionalSwitching()
        {
            var service = new ProfileSettingsService();
            Global.ProfileSettingsServiceInstance = service;

            const int device = 0;
            OutContType original = service.OutContType[device];
            try
            {
                // 1. 初期状態: X360
                service.OutContType[device] = OutContType.X360;
                var vm = new ProfileSettingsViewModel(device, service);
                Assert.Equal(0, vm.ControllerTypeIndex);
                Assert.Equal(OutContType.X360, vm.ContType);

                // 2. X360 -> DS4 への変更
                service.OutContType[device] = OutContType.DS4;
                Assert.Equal(1, vm.ControllerTypeIndex);
                Assert.Equal(OutContType.DS4, vm.ContType);

                // 3. DS4 -> X360 への変更（今回の不具合再発防止の検証）
                service.OutContType[device] = OutContType.X360;
                Assert.Equal(0, vm.ControllerTypeIndex);
                Assert.Equal(OutContType.X360, vm.ContType);
            }
            finally
            {
                service.OutContType[device] = original;
            }
        }

        [Fact]
        public void TempControllerIndex_ShouldUpdateSSOTAndBidirectionalState()
        {
            var service = new ProfileSettingsService();
            Global.ProfileSettingsServiceInstance = service;

            const int device = 0;
            OutContType original = service.OutContType[device];
            try
            {
                service.OutContType[device] = OutContType.DS4;
                var vm = new ProfileSettingsViewModel(device, service);
                vm.UpdateLateProperties();
                Assert.Equal(1, vm.TempControllerIndex);

                // UI上で Xbox 360 (0) に変更された場合
                vm.TempControllerIndex = 0;

                // SSOT (service.OutContType) および各プロパティが即座に X360 に追従すること
                Assert.Equal(OutContType.X360, service.OutContType[device]);
                Assert.Equal(0, vm.ControllerTypeIndex);
                Assert.Equal(OutContType.X360, vm.TempConType);
                Assert.Equal(OutContType.X360, vm.ContType);
            }
            finally
            {
                service.OutContType[device] = original;
            }
        }

        [Fact]
        public void UpdateLateProperties_ShouldSyncBidirectionally_RegardlessOfEnableOutputDataToDS4()
        {
            var service = new ProfileSettingsService();
            var outputSlotService = new OutputSlotService();
            Global.ProfileSettingsServiceInstance = service;
            Global.OutputSlotServiceInstance = outputSlotService;

            const int device = 0;
            OutContType original = service.OutContType[device];
            try
            {
                var vm = new ProfileSettingsViewModel(device, service, outputSlotService: outputSlotService);

                // EnableOutputDataToDS4 (DS4にデータを出力する) が有効であっても干渉しないこと
                vm.EnableOutputDataToDS4 = true;

                // DS4 -> X360 への変更
                service.OutContType[device] = OutContType.X360;
                vm.UpdateLateProperties();

                Assert.Equal(0, vm.TempControllerIndex);
                Assert.Equal(OutContType.X360, vm.TempConType);
                Assert.Equal(OutContType.X360, vm.ContType);

                // X360 -> DS4 への変更
                service.OutContType[device] = OutContType.DS4;
                vm.UpdateLateProperties();

                Assert.Equal(1, vm.TempControllerIndex);
                Assert.Equal(OutContType.DS4, vm.TempConType);
                Assert.Equal(OutContType.DS4, vm.ContType);
            }
            finally
            {
                service.OutContType[device] = original;
            }
        }
    }
}