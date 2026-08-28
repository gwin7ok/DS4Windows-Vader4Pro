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
            var sa = new SpecialAction("Key_Press_Test", "Cross", "Key", "Key", 0, "");
            sa.typeID = SpecialAction.ActionTypeId.Key;
            sa.details = "30"; // 0x1E ('A')

            var inner = new KeyButtonActionController(0, KeyButtonActionController.Mode.Press, "Key_Press_Test");
            var adapter = new KeyButtonActionControllerAdapter(inner);

            var ctxDown = new TriggerContextImpl(0, 0x1E, 0x1E, false, mockKbm);

            // 押下エッジ (OnTrigger)
            bool handledDown = adapter.OnTrigger(ctxDown);
            Assert.True(handledDown);

            // 解放エッジ (OnUntrigger)
            bool handledUp = adapter.OnUntrigger(ctxDown);
            Assert.True(handledUp);
        }

        [Fact]
        public void SpecialAction_ToggleMode_TogglesStateCorrectly()
        {
            var mockKbm = new MockVirtualKBM();
            var sa = new SpecialAction("Key_Toggle_Test", "Cross", "Key", "Key", 0, "");
            sa.typeID = SpecialAction.ActionTypeId.Key;
            sa.details = "30";

            var inner = new KeyButtonActionController(0, KeyButtonActionController.Mode.Toggle, "Key_Toggle_Test");
            var toggleAdapter = new KeyButtonActionControllerAdapter(inner);

            var ctx = new TriggerContextImpl(0, 0x1E, 0x1E, false, mockKbm);

            // 1回目の押下 -> トグルON (OnTrigger)
            bool handled1 = toggleAdapter.OnTrigger(ctx);
            Assert.True(handled1);

            // 1回目の物理ボタン解放 -> トグル状態維持 (OnUntrigger)
            toggleAdapter.OnUntrigger(ctx);

            // 2回目の押下 -> トグルOFF (OnTrigger)
            bool handled2 = toggleAdapter.OnTrigger(ctx);
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
