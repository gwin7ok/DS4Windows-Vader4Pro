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

            var adapter = new KeyButtonActionControllerAdapter(0, sa);
            var binding = new KeyActionBinding(sa);

            var triggerDown = new TriggerContextImpl
            {
                Device = 0,
                IsEdgeEstablished = true,
                LogicalValue = 0x1E,
                NativeValue = 0x1E,
                OutputHandler = mockKbm,
                Timestamp = DateTime.UtcNow
            };

            // 押下エッジ (Start)
            adapter.Start(binding, triggerDown);
            var state = ActionManager.GetStateFor(sa, 0);
            Assert.NotNull(state);

            var triggerUp = new TriggerContextImpl
            {
                Device = 0,
                IsEdgeEstablished = false,
                LogicalValue = 0x1E,
                NativeValue = 0x1E,
                OutputHandler = mockKbm,
                Timestamp = DateTime.UtcNow
            };

            // 解放エッジ (Handle: IsEdgeEstablished = false)
            adapter.Handle(binding, triggerUp);
        }

        [Fact]
        public void SpecialAction_ToggleMode_TogglesStateCorrectly()
        {
            var mockKbm = new MockVirtualKBM();
            var sa = new SpecialAction("Key_Toggle_Test", "Cross", "Key", "Key", 0, "");
            sa.typeID = SpecialAction.ActionTypeId.Key;
            sa.details = "30";

            var inner = new KeyButtonActionController(0, KeyButtonActionController.Mode.Toggle, "Key_Toggle_Test");
            var adapter = new KeyButtonActionControllerAdapter(inner);
            var binding = new KeyActionBinding(sa);

            var triggerDown = new TriggerContextImpl
            {
                Device = 0,
                IsEdgeEstablished = true,
                LogicalValue = 0x1E,
                NativeValue = 0x1E,
                OutputHandler = mockKbm,
                Timestamp = DateTime.UtcNow
            };

            // 1回目の押下 (Start)
            adapter.Start(binding, triggerDown);

            var triggerUp = new TriggerContextImpl
            {
                Device = 0,
                IsEdgeEstablished = false,
                LogicalValue = 0x1E,
                NativeValue = 0x1E,
                OutputHandler = mockKbm,
                Timestamp = DateTime.UtcNow
            };

            // 1回目の解放 (Handle) -> トグル維持
            adapter.Handle(binding, triggerUp);

            // 2回目の押下/停止 (Stop) -> トグル解除
            adapter.Stop(binding, triggerDown);
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
