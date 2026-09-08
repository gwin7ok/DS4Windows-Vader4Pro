using System;
using System.Threading.Tasks;
using Xunit;
using DS4Windows;
using DS4Windows.DI;
using DS4WinWPF.DS4Forms.ViewModels;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase 5 Step 13-9 / Watchpoint 2 核心振る舞いテスト:
    /// ① ワーカースレッドからのイベント発火に対するスレッド安全性
    /// ② Dispose 呼び出しによるイベント購読解除 (Unsubscribe) とゴースト発火抑止
    /// を外部モックライブラリ非依存の Fake サービスを用いて厳密に検証する。
    /// </summary>
    public class SettingsViewModelWatchpoint2Tests
    {
        static SettingsViewModelWatchpoint2Tests()
        {
            if (string.IsNullOrEmpty(Global.appdatapath))
            {
                Global.appdatapath = AppContext.BaseDirectory;
            }
        }

        #region Fake Services
        private class FakeAppSettingsService : IAppSettingsService
        {
            public event Action<string> SettingChanged;

            public bool UseExclusiveMode { get; set; }
            public bool StartMinimized { get; set; }
            public bool MinimizeToTaskbar { get; set; }
            public bool CloseMinimizes { get; set; }
            public bool QuickCharge { get; set; }
            public int CustomSteamFolder { get; set; }
            public bool AutoProfileRevertDefaultProfile { get; set; }
            public bool DeviceOptionsAutoOpen { get; set; }
            public int FormWidth { get; set; }
            public int FormHeight { get; set; }
            public int FormLocationX { get; set; }
            public int FormLocationY { get; set; }

            public void RaiseSettingChanged(string settingName)
            {
                SettingChanged?.Invoke(settingName);
            }

            public bool Load(IProfileXmlStore xmlStore = null) => true;
            public bool Save(IProfileXmlStore xmlStore = null) => true;
        }

        private class FakeOutputSlotService : IOutputSlotService
        {
            public event Action<int, OutSlotDevice> OutputSlotChanged;
            public OutSlotDevice[] OutputSlots => Array.Empty<OutSlotDevice>();

            public void SetOutputDeviceType(int slotNum, OutContType devType) { }
            public OutContType FindExistEventSlotDevType(int slotNum) => OutContType.None;
            public bool Load(IOutputSlotStore store = null) => true;
            public bool Save(IOutputSlotStore store = null) => true;
            public void RaiseSlotChanged(int slotNum, OutSlotDevice dev)
            {
                OutputSlotChanged?.Invoke(slotNum, dev);
            }
        }
        #endregion

        [Fact]
        public async Task Watchpoint2_WorkerThread_EventFiring_HandlesThreadSafetyWithoutCrash()
        {
            // Arrange
            var fakeSettings = new FakeAppSettingsService();
            var fakeSlots = new FakeOutputSlotService();
            var vm = new SettingsViewModel(fakeSettings, fakeSlots);

            // Act: ワーカースレッド（非UIスレッド）から SettingChanged イベントを発火
            var exception = await Record.ExceptionAsync(() => Task.Run(() =>
            {
                fakeSettings.RaiseSettingChanged(nameof(IAppSettingsService.UseExclusiveMode));
                fakeSettings.RaiseSettingChanged(nameof(IAppSettingsService.StartMinimized));
            }));

            // Assert: スレッド違反例外等が発生せず、安全に処理されること
            Assert.Null(exception);

            vm.Dispose();
        }

        [Fact]
        public void Watchpoint2_Dispose_UnsubscribesFromEvents_PreventsGhostFiring()
        {
            // Arrange
            var fakeSettings = new FakeAppSettingsService();
            var fakeSlots = new FakeOutputSlotService();
            fakeSettings.UseExclusiveMode = false;

            var vm = new SettingsViewModel(fakeSettings, fakeSlots);

            // 初期状態の同期確認: サービス側の値を変更してイベント発火
            fakeSettings.UseExclusiveMode = true;
            fakeSettings.RaiseSettingChanged(nameof(IAppSettingsService.UseExclusiveMode));
            Assert.True(vm.UseExclusiveMode, "事前検証: Dispose 前はイベント受信により ViewModel が更新されるべき");

            // Act: ViewModel を破棄 (Dispose)
            vm.Dispose();

            // 破棄後にサービス側の値を変更してイベント再発火
            fakeSettings.UseExclusiveMode = false;
            fakeSettings.RaiseSettingChanged(nameof(IAppSettingsService.UseExclusiveMode));

            // Assert: Dispose 済みのためハンドラがアンフックされており、ViewModel のプロパティが更新されないこと (ゴースト発火抑止)
            Assert.True(vm.UseExclusiveMode, "検証成功: Dispose 後はイベント購読が解除されているため、プロパティが更新されてはならない");
        }
    }
}