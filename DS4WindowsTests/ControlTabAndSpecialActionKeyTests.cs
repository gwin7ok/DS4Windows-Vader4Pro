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
        public void SpecialAction_PressMode_KeyRepeat_StartsCorrectly()
        {
            var mockKbm = new MockVirtualKBM();
            var sa = new SpecialAction("Key_Press_Test", "Cross", "Key", "Key", 0, "");
            sa.typeID = SpecialAction.ActionTypeId.Key;
            sa.details = "30"; // 0x1E

            var controller = new KeyButtonActionController(0, sa, mockKbm);

            // 押下エッジ
            controller.Process(0, true, 0x1E, false, mockKbm, false);
            Assert.Contains((uint)0x1E, mockKbm.KeyPressCalls);

            // 解放エッジ
            controller.Process(0, false, 0x1E, false, mockKbm, false);
            Assert.Contains((uint)0x1E, mockKbm.KeyReleaseCalls);
        }

        [Fact]
        public void SpecialAction_ToggleMode_TogglesStateCorrectly()
        {
            var mockKbm = new MockVirtualKBM();
            var sa = new SpecialAction("Key_Toggle_Test", "Cross", "Key", "Key", 0, "");
            sa.typeID = SpecialAction.ActionTypeId.Key;
            sa.details = "30";

            var toggleController = new ToggleController(0, sa, mockKbm);

            // 1回目の押下 -> トグルON
            toggleController.Process(0, true, 0x1E, false, mockKbm, false);
            Assert.True(toggleController.isToggledOn);
            Assert.Contains((uint)0x1E, mockKbm.KeyPressCalls);

            // 1回目の物理ボタン解放 -> トグル状態維持
            toggleController.Process(0, false, 0x1E, false, mockKbm, false);
            Assert.True(toggleController.isToggledOn);

            // 2回目の押下 -> トグルOFF
            toggleController.Process(0, true, 0x1E, false, mockKbm, false);
            Assert.False(toggleController.isToggledOn);
            Assert.Contains((uint)0x1E, mockKbm.KeyReleaseCalls);
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
