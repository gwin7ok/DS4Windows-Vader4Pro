using System;
using Xunit;
using DS4Windows;
using DS4Windows.Actions;
using DS4Windows.DS4Control;
using DS4Windows.Services;

namespace DS4WindowsTests
{
    public class ControlTabAndSpecialActionKeyTests
    {
        [Fact]
        public void SpecialAction_PressMode_KeyRepeat_IntegrationTest()
        {
            var mockKbm = new MockVirtualKBM();
            var inner = new KeyButtonActionController(0, KeyButtonActionController.Mode.Press, "Key_Press_Test");
            var adapter = new KeyButtonActionControllerAdapter(inner);

            // 押下エッジ (isEstablished: true)
            bool handledDown = adapter.Dispatch(0, 0x1E, 0x1E, false, mockKbm, true);
            Assert.True(handledDown);

            // 解放エッジ (isEstablished: false)
            bool handledUp = adapter.Dispatch(0, 0x1E, 0x1E, false, mockKbm, false);
            Assert.True(handledUp);
        }

        [Fact]
        public void SpecialAction_ToggleMode_TogglesStateCorrectly()
        {
            var mockKbm = new MockVirtualKBM();
            var inner = new KeyButtonActionController(0, KeyButtonActionController.Mode.Toggle, "Key_Toggle_Test");
            var toggleAdapter = new KeyButtonActionControllerAdapter(inner);

            // 1回目の押下 -> トグルON (isEstablished: true)
            bool handled1 = toggleAdapter.Dispatch(0, 0x1E, 0x1E, false, mockKbm, true);
            Assert.True(handled1);

            // 1回目の物理ボタン解放 (isEstablished: false) -> トグル状態維持
            toggleAdapter.Dispatch(0, 0x1E, 0x1E, false, mockKbm, false);

            // 2回目の押下 -> トグルOFF (isEstablished: true)
            bool handled2 = toggleAdapter.Dispatch(0, 0x1E, 0x1E, false, mockKbm, true);
            Assert.True(handled2);
        }

        [Fact]
        public void ControlTab_SyntheticSpecialAction_Construction()
        {
            Mapping.ClearSyntheticActionCache();

            // 通常キー（Toggle OFF）の合成SpecialAction
            var pressSa = Mapping.GetOrCreateSyntheticKeyAction(0, 1, 0x41, toggle: false, useScan: false);
            Assert.Equal("Synthetic_Key_0_1", pressSa.name);
            Assert.Equal("65", pressSa.details);
            Assert.Equal(SpecialAction.ActionTypeId.Key, pressSa.typeID);
            Assert.Equal((DS4KeyType)0, pressSa.keyType);

            // トグルキー（Toggle ON）の合成SpecialAction
            var toggleSa = Mapping.GetOrCreateSyntheticKeyAction(0, 2, 0x42, toggle: true, useScan: true);
            Assert.Equal("Synthetic_Key_0_2", toggleSa.name);
            Assert.Equal("66", toggleSa.details);
            Assert.Equal(SpecialAction.ActionTypeId.Key, toggleSa.typeID);
            Assert.Equal(DS4KeyType.ScanCode, toggleSa.keyType);

            // キャッシュ再利用の検証
            var cachedSa = Mapping.GetOrCreateSyntheticKeyAction(0, 1, 0x41, toggle: false, useScan: false);
            Assert.Same(pressSa, cachedSa);
        }
    }
}
