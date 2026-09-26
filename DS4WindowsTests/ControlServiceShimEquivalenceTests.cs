using System;
using Xunit;
using DS4Windows;
using DS4Windows.Services;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase6-Step2-1 (PR-1): ControlService の Global 直接参照解消のために
    /// IAppSettingsService / IEnvironmentService へ追加した薄い委譲シムが、
    /// Global（m_Config / BackingStore）と完全に同一の実体を共有していることを検証する。
    ///
    /// SSOT 原則: DI サービスは独自フィールドを持たない。したがって
    /// 「Global で書いた値がサービスから読める」「サービスで書いた値が Global から読める」
    /// の双方向が成立しなければならない（孤立プロパティバグの再発防止）。
    /// 各テストは変更した Global 状態を必ず元に戻す。
    /// </summary>
    public class ControlServiceShimEquivalenceTests
    {
        private readonly AppSettingsService _appSettings = new AppSettingsService();
        private readonly EnvironmentService _environment = new EnvironmentService();

        /// <summary>Global で書いた値がサービスから読め、サービスで書いた値が Global から読めることを確認し、元に戻す。</summary>
        private static void AssertSharesStateWithGlobal<T>(
            Func<T> globalGet, Action<T> globalSet,
            Func<T> serviceGet, Action<T> serviceSet,
            T first, T second)
        {
            T original = globalGet();
            try
            {
                globalSet(first);
                Assert.Equal(first, serviceGet());

                serviceSet(second);
                Assert.Equal(second, globalGet());
            }
            finally
            {
                globalSet(original);
            }
        }

        [Fact]
        public void UseOscServer_SharesStateWithGlobal()
        {
            AssertSharesStateWithGlobal(
                Global.isUsingOSCServer, Global.setUsingOSCServer,
                () => _appSettings.UseOscServer, v => _appSettings.UseOscServer = v,
                true, false);
        }

        [Fact]
        public void UseOscSender_SharesStateWithGlobal()
        {
            AssertSharesStateWithGlobal(
                Global.isUsingOSCSender, Global.setUsingOSCSender,
                () => _appSettings.UseOscSender, v => _appSettings.UseOscSender = v,
                true, false);
        }

        [Fact]
        public void InterpretingOscMonitoring_SharesStateWithGlobal()
        {
            AssertSharesStateWithGlobal(
                Global.isInterpretingOscMonitoring, Global.setInterpretingOscMonitoring,
                () => _appSettings.InterpretingOscMonitoring, v => _appSettings.InterpretingOscMonitoring = v,
                true, false);
        }

        [Fact]
        public void OscServerPort_SharesStateWithGlobal()
        {
            AssertSharesStateWithGlobal(
                Global.getOSCServerPortNum, Global.setOSCServerPort,
                () => _appSettings.OscServerPort, v => _appSettings.OscServerPort = v,
                9101, 9102);
        }

        [Fact]
        public void OscSenderPort_SharesStateWithGlobal()
        {
            AssertSharesStateWithGlobal(
                Global.getOSCSenderPortNum, Global.setOSCSenderPort,
                () => _appSettings.OscSenderPort, v => _appSettings.OscSenderPort = v,
                9103, 9104);
        }

        [Fact]
        public void OscSenderAddress_SharesStateWithGlobal_AndKeepsTrimBehavior()
        {
            string original = Global.getOSCSenderAddress();
            try
            {
                Global.setOSCSenderAddress("10.0.0.1");
                Assert.Equal("10.0.0.1", _appSettings.OscSenderAddress);

                // Global.setOSCSenderAddress は前後の空白を除去する。サービス経由でも同じ挙動であること。
                _appSettings.OscSenderAddress = "  10.0.0.2  ";
                Assert.Equal("10.0.0.2", Global.getOSCSenderAddress());
            }
            finally
            {
                if (original != null)
                {
                    Global.setOSCSenderAddress(original);
                }
            }
        }

        [Fact]
        public void QuickCharge_SharesStateWithGlobal()
        {
            AssertSharesStateWithGlobal(
                () => Global.QuickCharge, v => Global.QuickCharge = v,
                () => _appSettings.QuickCharge, v => _appSettings.QuickCharge = v,
                true, false);
        }

        [Fact]
        public void DCBTatStop_SharesStateWithGlobal()
        {
            AssertSharesStateWithGlobal(
                () => Global.DCBTatStop, v => Global.DCBTatStop = v,
                () => _appSettings.DCBTatStop, v => _appSettings.DCBTatStop = v,
                true, false);
        }

        [Fact]
        public void ProcessPriority_SharesStateWithGlobal()
        {
            AssertSharesStateWithGlobal(
                () => Global.ProcessPriority, v => Global.ProcessPriority = v,
                () => _appSettings.ProcessPriority, v => _appSettings.ProcessPriority = v,
                1, 3);
        }

        [Fact]
        public void RunHotPlug_WriteFromService_IsVisibleToGlobal()
        {
            // ControlService.Start/Stop は runHotPlug を IAppSettingsService.RunHotPlug 経由で書き込む。
            bool original = Global.runHotPlug;
            try
            {
                _appSettings.RunHotPlug = true;
                Assert.True(Global.runHotPlug);

                _appSettings.RunHotPlug = false;
                Assert.False(Global.runHotPlug);
            }
            finally
            {
                Global.runHotPlug = original;
            }
        }

        [Fact]
        public void DeviceOptions_ReturnsSameInstanceAsGlobal()
        {
            Assert.Same(Global.DeviceOptions, _appSettings.DeviceOptions);
        }

        [Fact]
        public void UDPServerSmoothingMincutoffChanged_IsForwardedFromGlobal()
        {
            int fired = 0;
            EventHandler handler = (sender, e) => fired++;
            double original = Global.UDPServerSmoothingMincutoff;
            _appSettings.UDPServerSmoothingMincutoffChanged += handler;
            try
            {
                Global.UDPServerSmoothingMincutoff = original + 1.0;
                Assert.Equal(1, fired);

                // 購読解除が Global 側の static イベントへ正しく転送されること。
                _appSettings.UDPServerSmoothingMincutoffChanged -= handler;
                Global.UDPServerSmoothingMincutoff = original;
                Assert.Equal(1, fired);
            }
            finally
            {
                _appSettings.UDPServerSmoothingMincutoffChanged -= handler;
                Global.UDPServerSmoothingMincutoff = original;
            }
        }

        [Fact]
        public void UDPServerSmoothingBetaChanged_IsForwardedFromGlobal()
        {
            int fired = 0;
            EventHandler handler = (sender, e) => fired++;
            double original = Global.UDPServerSmoothingBeta;
            _appSettings.UDPServerSmoothingBetaChanged += handler;
            try
            {
                Global.UDPServerSmoothingBeta = original + 1.0;
                Assert.Equal(1, fired);

                _appSettings.UDPServerSmoothingBetaChanged -= handler;
                Global.UDPServerSmoothingBeta = original;
                Assert.Equal(1, fired);
            }
            finally
            {
                _appSettings.UDPServerSmoothingBetaChanged -= handler;
                Global.UDPServerSmoothingBeta = original;
            }
        }

        [Fact]
        public void HidHideInstalled_ReflectsGlobalValue()
        {
            Assert.Equal(Global.hidHideInstalled, _environment.HidHideInstalled);
        }
    }
}