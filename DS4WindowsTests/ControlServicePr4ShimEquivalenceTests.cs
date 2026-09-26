using System;
using Xunit;
using DS4Windows;
using DS4Windows.Services;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase6-Step2-4 (PR-4): ControlService の Global 直接参照解消のために追加した薄い委譲シム
    /// （遅延警告の点滅設定、デバイスのシリアル変更通知）が、Global と同一の実体・同一の挙動であることを検証する。
    /// あわせて、On_Report の初回処理などで呼ばれるようになった取得系のシムが、
    /// ヒープ割り当て（GC Alloc）を行わないことを検証する（入力ループのゼロアロケーション方針）。
    /// </summary>
    public class ControlServicePr4ShimEquivalenceTests
    {
        private readonly AppSettingsService _appSettings = new AppSettingsService();
        private readonly DeviceStateService _deviceState = new DeviceStateService();
        private readonly ProfileSettingsService _profileSettings = new ProfileSettingsService();
        private readonly ProfileRepository _profileRepository = new ProfileRepository();

        [Fact]
        public void FlashWhenLate_SharesStateWithGlobal()
        {
            bool original = Global.FlashWhenLate;
            try
            {
                Global.FlashWhenLate = !original;
                Assert.Equal(!original, _appSettings.FlashWhenLate);

                _appSettings.FlashWhenLate = original;
                Assert.Equal(original, Global.FlashWhenLate);
                Assert.Equal(Global.getFlashWhenLate(), _appSettings.FlashWhenLate);
            }
            finally
            {
                Global.FlashWhenLate = original;
            }
        }

        [Fact]
        public void OnDeviceSerialChange_RaisesGlobalDeviceSerialChangeWithSameArguments()
        {
            object receivedSender = null;
            SerialChangeArgs receivedArgs = null;
            int fired = 0;
            EventHandler<SerialChangeArgs> handler = (sender, args) =>
            {
                fired++;
                receivedSender = sender;
                receivedArgs = args;
            };
            object caller = new object();
            Global.DeviceSerialChange += handler;
            try
            {
                _deviceState.OnDeviceSerialChange(caller, 2, "AA:BB:CC:DD:EE:FF");

                Assert.Equal(1, fired);
                Assert.Same(caller, receivedSender);
                Assert.Equal(2, receivedArgs.getIndex());
                Assert.Equal("AA:BB:CC:DD:EE:FF", receivedArgs.getSerial());
            }
            finally
            {
                Global.DeviceSerialChange -= handler;
            }
        }

        [Fact]
        public void GetterShims_UsedOnReportPath_DoNotAllocate()
        {
            // 初回呼び出しの JIT・初期化の影響を除くため、ウォームアップしてから計測する。
            const int iterations = 10000;
            GyroOutMode gyro = _profileSettings.GetGyroOutMode(0);
            SASteeringWheelEmulationAxisType axis = _profileSettings.GetSASteeringWheelEmulationAxis(0);
            string[] profilePath = _profileRepository.ProfilePath;

            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < iterations; i++)
            {
                gyro = _profileSettings.GetGyroOutMode(0);
                axis = _profileSettings.GetSASteeringWheelEmulationAxis(0);
                profilePath = _profileRepository.ProfilePath;
            }
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

            Assert.Equal(0L, allocated);
            Assert.NotNull(profilePath);
            GC.KeepAlive(gyro);
            GC.KeepAlive(axis);
        }
    }
}