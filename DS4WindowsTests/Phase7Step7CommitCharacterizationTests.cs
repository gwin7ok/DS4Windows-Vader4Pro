using DS4Windows;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase7-Step7 W1（Q1＝A）: <c>Mapping.Commit</c>（合成入力の出力）、<c>MapClick</c>、<c>getBool*Mapping</c>、
    /// <c>HandleDeviceDisconnect</c> の特性化テスト。出力は <see cref="RecordingVirtualKbmBase"/>（<c>OutputKBMHandlerAdapter.Handler</c>（旧 Global.outputKBMHandler）へ差す）で記録する。
    /// ホイールは <c>Commit</c> が実時間（150ms）で連続送出するため、フレーク防止のため範囲外とする。
    /// <c>Mapping</c> の静的な状態を共有するため、<c>ServiceProviderHolder</c> のコレクションで直列に実行する。
    /// </summary>
    [Collection("ServiceProviderHolder")]
    public class Phase7Step7CommitCharacterizationTests
    {
        private const int Device = MappingCharacterizationScope.Device;

        [Fact]
        public void LeftClick_SendsDownOnce_HoldsWhileHeld_AndSendsUpOnRelease()
        {
            using var scope = new MappingCharacterizationScope();
            uint down = scope.KbmMapping.MOUSEEVENTF_LEFTDOWN;
            uint up = scope.KbmMapping.MOUSEEVENTF_LEFTUP;

            // 1 ティック目: 押下
            scope.Synthetic.CountClick(Device, Click.Left);
            scope.Mapper.Commit(Device, scope.Settings);
            Assert.Equal(new[] { down }, scope.Recorder.MouseButtonEvents);

            // 2 ティック目: 押したまま（毎ティック MapClick が呼ばれる）。新しい出力はない
            scope.Synthetic.CountClick(Device, Click.Left);
            scope.Mapper.Commit(Device, scope.Settings);
            Assert.Equal(new[] { down }, scope.Recorder.MouseButtonEvents);

            // 3 ティック目: 離した
            scope.Mapper.Commit(Device, scope.Settings);
            Assert.Equal(new[] { down, up }, scope.Recorder.MouseButtonEvents);
        }

        [Fact]
        public void RightAndMiddleClick_SendTheirOwnDownEvents()
        {
            using var scope = new MappingCharacterizationScope();

            scope.Synthetic.CountClick(Device, Click.Right);
            scope.Synthetic.CountClick(Device, Click.Middle);
            scope.Mapper.Commit(Device, scope.Settings);

            Assert.Contains(scope.KbmMapping.MOUSEEVENTF_RIGHTDOWN, scope.Recorder.MouseButtonEvents);
            Assert.Contains(scope.KbmMapping.MOUSEEVENTF_MIDDLEDOWN, scope.Recorder.MouseButtonEvents);
            Assert.DoesNotContain(scope.KbmMapping.MOUSEEVENTF_LEFTDOWN, scope.Recorder.MouseButtonEvents);
        }

        [Fact]
        public void NoClicks_SendNoButtonEvents_ButStillSyncs()
        {
            using var scope = new MappingCharacterizationScope();

            scope.Mapper.Commit(Device, scope.Settings);

            Assert.Empty(scope.Recorder.MouseButtonEvents);
            Assert.Empty(scope.Recorder.MouseButtonAltEvents);
            Assert.Equal(1, scope.Recorder.SyncCount);
        }

        [Fact]
        public void MapClick_CountsEachKindOnTheDeviceState()
        {
            using var scope = new MappingCharacterizationScope();

            scope.Synthetic.CountClick(Device, Click.Left);
            scope.Synthetic.CountClick(Device, Click.Left);
            scope.Synthetic.CountClick(Device, Click.Fourth);
            scope.Synthetic.CountClick(Device, Click.WDOWN);
            scope.Synthetic.CountClick(Device, Click.None);

            var clicks = scope.Synthetic.DeviceState[Device].currentClicks;
            Assert.Equal(2, clicks.leftCount);
            Assert.Equal(1, clicks.fourthCount);
            Assert.Equal(1, clicks.wDownCount);
            Assert.Equal(0, clicks.rightCount);
            Assert.Equal(0, clicks.middleCount);
            Assert.Equal(0, clicks.fifthCount);
            Assert.Equal(0, clicks.wUpCount);
        }

        [Fact]
        public void GetBoolMappings_UseTheirThresholds()
        {
            Assert.True(Mapping.getBoolButtonMapping(true));
            Assert.False(Mapping.getBoolButtonMapping(false));
            Assert.True(Mapping.getBoolTouchMapping(true));
            Assert.False(Mapping.getBoolTouchMapping(false));

            // トリガー: 100 を超えたら押下
            Assert.True(Mapping.getBoolTriggerMapping(101));
            Assert.False(Mapping.getBoolTriggerMapping(100));

            // 軸の向き: 中央（128）から 55 を超えたら押下
            Assert.True(Mapping.getBoolAxisDirMapping(184, true));
            Assert.False(Mapping.getBoolAxisDirMapping(183, true));
            Assert.True(Mapping.getBoolAxisDirMapping(72, false));
            Assert.False(Mapping.getBoolAxisDirMapping(73, false));
        }

        [Fact]
        public void HandleDeviceDisconnect_ReleasesHeldSyntheticKeys_AndClearsTheDeviceKeyState()
        {
            using var scope = new MappingCharacterizationScope();
            const ushort logicalKey = 65;
            const uint nativeKey = 65;
            scope.Dispatcher.SendPress(Device, logicalKey, nativeKey, false, scope.Recorder);
            Assert.True(scope.Synthetic.DeviceState[Device].keyPresses.ContainsKey(logicalKey));

            scope.Mapper.HandleDeviceDisconnect(Device);

            Assert.Contains(nativeKey, scope.Recorder.KeyReleases);
            Assert.Empty(scope.Synthetic.DeviceState[Device].keyPresses);
        }
    }
}
