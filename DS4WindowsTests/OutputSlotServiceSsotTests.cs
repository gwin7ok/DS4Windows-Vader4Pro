using Xunit;
using DS4Windows;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase6-Step5-3: 出力デバイス種別の三態（永続設定／実行時接続状態／UI一時状態）が、
    /// それぞれ別の配列であり、互いに干渉しないことを固定する。
    /// - 永続設定: IProfileSettingsService.OutContType（BackingStore.outContType）
    /// - 実行時接続状態: IOutputSlotService.ActiveOutDevType（裏づけは Global.activeOutDevType）
    /// - UI一時状態: IOutputSlotService.OutDevTypeTemp（裏づけは Global.outDevTypeTemp）
    /// Global の静的配列を書き換えるテストは、最後に元の値へ戻す（Phase6-Status.md §6.4 の教訓1）。
    /// </summary>
    public class OutputSlotServiceSsotTests
    {
        private const int Slot = 0;

        [Fact]
        public void ThreeStates_AreDistinctArrays()
        {
            var service = new OutputSlotService();
            var settings = new ProfileSettingsService(Global.store);

            Assert.NotSame(settings.OutContType, service.ActiveOutDevType);
            Assert.NotSame(settings.OutContType, service.OutDevTypeTemp);
            Assert.NotSame(service.ActiveOutDevType, service.OutDevTypeTemp);
        }

        [Fact]
        public void ChangingPersistentSetting_DoesNotAffectRuntimeOrTempState()
        {
            var service = new OutputSlotService();
            var settings = new ProfileSettingsService(Global.store);

            OutContType originalPersistent = settings.OutContType[Slot];
            OutContType originalActive = service.ActiveOutDevType[Slot];
            OutContType originalTemp = service.OutDevTypeTemp[Slot];
            try
            {
                OutContType changed = originalPersistent == OutContType.DS4 ? OutContType.X360 : OutContType.DS4;
                settings.OutContType[Slot] = changed;

                Assert.Equal(changed, settings.OutContType[Slot]);
                Assert.Equal(originalActive, service.ActiveOutDevType[Slot]);
                Assert.Equal(originalTemp, service.OutDevTypeTemp[Slot]);
            }
            finally
            {
                settings.OutContType[Slot] = originalPersistent;
            }
        }

        [Fact]
        public void ChangingRuntimeState_DoesNotAffectPersistentSetting()
        {
            var service = new OutputSlotService();
            var settings = new ProfileSettingsService(Global.store);

            OutContType originalPersistent = settings.OutContType[Slot];
            OutContType originalActive = service.ActiveOutDevType[Slot];
            try
            {
                service.ActiveOutDevType[Slot] = OutContType.None;

                Assert.Equal(originalPersistent, settings.OutContType[Slot]);
            }
            finally
            {
                service.ActiveOutDevType[Slot] = originalActive;
            }
        }

        [Fact]
        public void UnplugSlot_DoesNotWriteNoneToPersistentSetting()
        {
            // 旧計画（SetOutputDeviceType を永続設定へ委譲する案）で懸念された、
            // UnplugSlot 経由で永続設定へ None が書き込まれる事故が起きないことを固定する。
            var service = new OutputSlotService(new OutputSlotManager());
            var settings = new ProfileSettingsService(Global.store);

            OutContType originalPersistent = settings.OutContType[Slot];
            service.UnplugSlot(Slot);

            Assert.Equal(originalPersistent, settings.OutContType[Slot]);
        }
    }
}
