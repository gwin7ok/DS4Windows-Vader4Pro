using System;
using Xunit;
using DS4Windows;
using DS4Windows.Actions;
using DS4Windows.DS4Control;
using DS4Windows.Services;

namespace DS4WindowsTests
{
    public class DefaultActionManagerTests
    {
        private class MockActionFactory : IActionFactory
        {
            public int CreateFromCalls { get; private set; }

            public DS4Windows.Actions.Action CreateFrom(SpecialAction action, int index = -1)
            {
                CreateFromCalls++;
                return null;
            }
        }

        [Fact]
        public void Constructor_WithMockActionFactory_InitializesSuccessfully()
        {
            var mockFactory = new MockActionFactory();
            var manager = new DefaultActionManager(mockFactory);

            Assert.NotNull(manager);
            Assert.NotEqual(0, manager.InstanceId);
        }

        [Fact]
        public void SetToggledOn_FiresInstanceToggledOnChangedEvent()
        {
            var manager = new DefaultActionManager();
            var action = new SpecialAction("ToggleTest", "Cross", "Key", "Key", 0);

            SpecialAction eventAction = null;
            int eventDevice = -1;
            bool eventOld = false;
            bool eventNew = false;
            bool fired = false;

            manager.ToggledOnChanged += (act, dev, oldV, newV) =>
            {
                fired = true;
                eventAction = act;
                eventDevice = dev;
                eventOld = oldV;
                eventNew = newV;
            };

            manager.SetToggledOn(action, 0, true);

            Assert.True(fired);
            Assert.Same(action, eventAction);
            Assert.Equal(0, eventDevice);
            Assert.False(eventOld);
            Assert.True(eventNew);

            // 同じ値を再設定した場合はイベントが発火しないこと
            fired = false;
            manager.SetToggledOn(action, 0, true);
            Assert.False(fired);

            // 値を解除した時の発火
            manager.SetToggledOn(action, 0, false);
            Assert.True(fired);
            Assert.True(eventOld);
            Assert.False(eventNew);
        }

        [Fact]
        public void ClearDeviceState_ResetsToggledOnStateAndFiresEvent()
        {
            var manager = new DefaultActionManager();
            var action = new SpecialAction("ClearTest", "Circle", "Key", "Key", 0);

            manager.SetToggledOn(action, 1, true);

            bool clearedFired = false;
            manager.ToggledOnChanged += (act, dev, oldV, newV) =>
            {
                if (dev == 1 && oldV == true && newV == false)
                {
                    clearedFired = true;
                }
            };

            manager.ClearDeviceState(1);

            Assert.True(clearedFired);
            var state = manager.GetStateFor(action, 1);
            Assert.NotNull(state);
            Assert.False(state.IsToggledOn);
        }

        [Fact]
        public void DefaultMacroPlayer_InitializesWithVirtualKBM_AndTracksState()
        {
            var mockKBM = new MockVirtualKBM();
            var player = new DefaultMacroPlayer(mockKBM);

            Assert.False(player.IsPlaying(0));
            Assert.False(player.IsPlaying(-1));
            Assert.False(player.IsPlaying(4));

            player.Stop(0);
            Assert.False(player.IsPlaying(0));
        }
    }
}
