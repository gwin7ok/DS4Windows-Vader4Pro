using System;
using System.Collections.Generic;
using Xunit;
using DS4Windows;
using DS4Windows.Services;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase6-Step2-2 (PR-2): ControlService の Global 直接参照解消のために追加した薄い委譲シム
    /// （UDP 平滑化、初回接続判定、リンクプロファイル解決、スロットへのプロファイル適用ポート）が、
    /// Global（m_Config / BackingStore）と完全に同一の実体を共有していることを検証する。
    /// 各テストは変更した Global 状態を必ず元に戻す。
    /// </summary>
    public class ControlServicePr2ShimEquivalenceTests
    {
        private readonly AppSettingsService _appSettings = TestRepositoryFactory.CreateAppSettingsService();
        private readonly DeviceStateService _deviceState = TestRepositoryFactory.CreateDeviceStateService();
        private readonly ProfileRepository _profileRepository = TestRepositoryFactory.CreateProfileRepository();
        private readonly ProfileSlotApplier _slotApplier = TestRepositoryFactory.CreateProfileSlotApplier();

        [Fact]
        public void UDPServerSmoothingMincutoff_SharesStateWithGlobal()
        {
            double original = Global.store.udpSmoothingMincutoff;
            try
            {
                Global.store.udpSmoothingMincutoff = original + 0.5;
                Assert.Equal(original + 0.5, _appSettings.UDPServerSmoothingMincutoff);

                _appSettings.UDPServerSmoothingMincutoff = original + 0.25;
                Assert.Equal(original + 0.25, Global.store.udpSmoothingMincutoff);
            }
            finally
            {
                Global.store.udpSmoothingMincutoff = original;
            }
        }

        [Fact]
        public void UDPServerSmoothingBeta_SharesStateWithGlobal()
        {
            double original = Global.store.udpSmoothingBeta;
            try
            {
                Global.store.udpSmoothingBeta = original + 0.5;
                Assert.Equal(original + 0.5, _appSettings.UDPServerSmoothingBeta);

                _appSettings.UDPServerSmoothingBeta = original + 0.25;
                Assert.Equal(original + 0.25, Global.store.udpSmoothingBeta);
            }
            finally
            {
                Global.store.udpSmoothingBeta = original;
            }
        }

        [Fact]
        public void UDPServerSmoothing_SetThroughService_RaisesChangedEvents()
        {
            // サービス経由の書込みでも、サービスの Changed イベントが発火すること。
            int mincutoffFired = 0;
            int betaFired = 0;
            EventHandler onMincutoff = (s, e) => mincutoffFired++;
            EventHandler onBeta = (s, e) => betaFired++;
            double originalMincutoff = Global.store.udpSmoothingMincutoff;
            double originalBeta = Global.store.udpSmoothingBeta;
            _appSettings.UDPServerSmoothingMincutoffChanged += onMincutoff;
            _appSettings.UDPServerSmoothingBetaChanged += onBeta;
            try
            {
                _appSettings.UDPServerSmoothingMincutoff = originalMincutoff + 1.0;
                _appSettings.UDPServerSmoothingBeta = originalBeta + 1.0;

                Assert.Equal(1, mincutoffFired);
                Assert.Equal(1, betaFired);
            }
            finally
            {
                _appSettings.UDPServerSmoothingMincutoffChanged -= onMincutoff;
                _appSettings.UDPServerSmoothingBetaChanged -= onBeta;
                Global.store.udpSmoothingMincutoff = originalMincutoff;
                Global.store.udpSmoothingBeta = originalBeta;
            }
        }

        [Fact]
        public void IsFirstConnection_And_MarkConnected_ShareStateWithGlobal()
        {
            const int slot = 0;
            bool original = Global.store.firstConnectionAfterStartup[slot];
            try
            {
                Global.store.firstConnectionAfterStartup[slot] = true;
                Assert.True(_deviceState.IsFirstConnection(slot));
                Assert.Equal(Global.store.firstConnectionAfterStartup[slot], _deviceState.IsFirstConnection(slot));

                _deviceState.MarkConnected(slot);
                Assert.False(Global.store.firstConnectionAfterStartup[slot]);
                Assert.False(_deviceState.IsFirstConnection(slot));
            }
            finally
            {
                Global.store.firstConnectionAfterStartup[slot] = original;
            }
        }

        [Fact]
        public void LinkedProfile_ContainsAndGet_ShareStateWithGlobal_AndIgnoreColons()
        {
            // 旧 Global.containsLinkedProfile/getLinkedProfile と同じく、MAC アドレスのコロンを除去して照合する（Phase7-Step3 W5 でサービスへ移した）。
            const string key = "A1B2C3D4E5F6";
            var linked = Global.store.linkedProfiles;
            Assert.False(linked.ContainsKey(key));
            try
            {
                linked[key] = "PR2TestProfile";

                Assert.True(_profileRepository.ContainsLinkedProfile("A1:B2:C3:D4:E5:F6"));
                Assert.Equal("PR2TestProfile", _profileRepository.GetLinkedProfile("A1:B2:C3:D4:E5:F6"));
            }
            finally
            {
                linked.Remove(key);
            }

            Assert.False(_profileRepository.ContainsLinkedProfile("A1:B2:C3:D4:E5:F6"));
            Assert.Equal(string.Empty, _profileRepository.GetLinkedProfile("A1:B2:C3:D4:E5:F6"));
        }

        [Fact]
        public void ProfileSlotApplier_InvalidArguments_ReturnsFalseWithoutSideEffects()
        {
            // 範囲外スロット・空のプロファイル名は、副作用なしで false を返す（旧 Global.ApplyProfileToSlot と同じ。
            // Phase7-Step3c W4 で旧メンバーを削除したため、ポートだけを確認する）。
            Assert.False(_slotApplier.ApplyToSlot(-1, "x", ProfileChangeSource.ControlService));

            Assert.False(_slotApplier.ApplyToSlot(0, "  ", ProfileChangeSource.ControlService));

            Assert.False(_slotApplier.ApplyToSlot(int.MaxValue, "x", ProfileChangeSource.ControlService));
        }

        [Fact]
        public void ProfileSettingsService_RefreshExtrasButtons_WithNull_DoesNotThrow()
        {
            var service = TestRepositoryFactory.CreateProfileSettingsService();

            var ex = Record.Exception(() => service.RefreshExtrasButtons(0, null));

            Assert.Null(ex);
        }
    }
}