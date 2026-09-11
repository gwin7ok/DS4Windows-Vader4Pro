using System;
using System.Windows;
using Xunit;
using DS4Windows;
using DS4Windows.DI;
using DS4WinWPF.DS4Forms.ViewModels;

namespace DS4WindowsTests
{
    // Issue7是正（Phase5-Step14-Issue7-Fix-Plan.md タスク7）の回帰防止テスト。
    //
    // 修正前は ControllerTypeIndex / ContType / UpdateLateProperties() が
    // IOutputSlotService.GetOutputDeviceType（Global/BackingStoreと非連動の孤立配列）を
    // 参照しており、プロファイルに実際に永続化された OutputContDevice の値に関わらず
    // 常に既定値（X360）が返る「孤立バグ」を抱えていた。
    //
    // 本テストは、修正後の参照先である IProfileSettingsService.OutContType
    // （Global.OutContType と同一実体）の値を変更した際に、ProfileSettingsViewModel の
    // ControllerTypeIndex / ContType が正しく追従することを確認し、この孤立バグの再発を検知する。
    //
    // なお PatternCViewModelTests.cs には IViewModelFactory 経由での
    // ProfileSettingsViewModel 生成確認（Null チェックのみ）が既に存在するが、
    // OutContType 連動という具体的な振る舞いまでは検証していないため、本ファイルを独立して新設する。
    public class ProfileSettingsViewModelTests
    {
        // ProfileSettingsViewModel のコンストラクタは、パック URI
        // (pack://application:,,,/DS4Windows;component/Resources/rainbowCCrop.png) から
        // BitmapImage を読み込む処理を含む。これには WPF の Application.Current /
        // Application.ResourceAssembly が設定されている必要があるため、
        // PatternCViewModelTests.cs と同一の最小限のブートストラップパターンを踏襲する。
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
        public void ControllerTypeIndex_ShouldReflectProfileSettingsOutContType()
        {
            var service = new ProfileSettingsService();
            Global.ProfileSettingsServiceInstance = service;

            const int device = 0;
            OutContType original = service.OutContType[device];
            try
            {
                service.OutContType[device] = OutContType.DS4;
                var vm = new ProfileSettingsViewModel(device, service);

                Assert.Equal(1, vm.ControllerTypeIndex);

                service.OutContType[device] = OutContType.X360;
                Assert.Equal(0, vm.ControllerTypeIndex);
            }
            finally
            {
                service.OutContType[device] = original;
            }
        }

        [Fact]
        public void ContType_ShouldReflectProfileSettingsOutContType()
        {
            var service = new ProfileSettingsService();
            Global.ProfileSettingsServiceInstance = service;

            const int device = 0;
            OutContType original = service.OutContType[device];
            try
            {
                service.OutContType[device] = OutContType.DS4;
                var vm = new ProfileSettingsViewModel(device, service);

                Assert.Equal(OutContType.DS4, vm.ContType);

                service.OutContType[device] = OutContType.X360;
                Assert.Equal(OutContType.X360, vm.ContType);
            }
            finally
            {
                service.OutContType[device] = original;
            }
        }

        [Fact]
        public void UpdateLateProperties_ShouldSyncOutDevTypeTempFromProfileSettingsOutContType()
        {
            // UpdateLateProperties() 内の読み取り側（Fix-Plan タスク2-3）の回帰防止テスト。
            // outputSlotService.OutDevTypeTemp[device] が、修正後の参照先である
            // profileSettings.OutContType の現在値と一致することを確認する。
            var service = new ProfileSettingsService();
            var outputSlotService = new OutputSlotService();
            Global.ProfileSettingsServiceInstance = service;
            Global.OutputSlotServiceInstance = outputSlotService;

            const int device = 0;
            OutContType original = service.OutContType[device];
            try
            {
                service.OutContType[device] = OutContType.DS4;
                var vm = new ProfileSettingsViewModel(device, service, outputSlotService: outputSlotService);

                vm.UpdateLateProperties();

                Assert.Equal(OutContType.DS4, outputSlotService.OutDevTypeTemp[device]);
            }
            finally
            {
                service.OutContType[device] = original;
            }
        }
    }
}