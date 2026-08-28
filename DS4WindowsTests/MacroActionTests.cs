using System;
using System.Collections.Generic;
using System.Threading;
using Xunit;
using DS4Windows;
using DS4Windows.Actions;
using DS4Windows.Services;

namespace DS4WindowsTests
{
    public class MacroActionTests
    {
        private class MockOutputContext : IOutputContext
        {
            public int Device { get; }
            public IVirtualKBM OutputHandler { get; }
            public MockOutputContext(int device) { Device = device; }
        }

        [Fact]
        public void T1_Execute_CallsMacroPlayerWithCorrectDeviceAndAction()
        {
            var mockPlayer = new MockMacroPlayer();
            var sa = new SpecialAction("TestMacro1", "Cross", "Macro", "Macro", 0, "");
            sa.typeID = SpecialAction.ActionTypeId.Macro;
            var action = new MacroActionAdapter(sa, 0, mockPlayer);
            var ctx = new MockOutputContext(device: 0);

            action.Execute(ctx);

            Assert.Single(mockPlayer.PlayCalls);
            Assert.Equal(0, mockPlayer.PlayCalls[0].deviceIndex);
            Assert.Same(sa, mockPlayer.PlayCalls[0].action);
        }

        [Fact]
        public void T2_Execute_PassesTargetDeviceIndexCorrectly()
        {
            var mockPlayer = new MockMacroPlayer();
            var sa = new SpecialAction("TestMacro2", "Cross", "Macro", "Macro", 0, "");
            sa.typeID = SpecialAction.ActionTypeId.Macro;
            var action = new MacroActionAdapter(sa, 2, mockPlayer);
            var ctx = new MockOutputContext(device: 0);

            action.Execute(ctx);

            Assert.Single(mockPlayer.PlayCalls);
            Assert.Equal(2, mockPlayer.PlayCalls[0].deviceIndex);
        }

        [Fact]
        public void T3_Stop_CallsMacroPlayerStopWithCorrectDevice()
        {
            var mockPlayer = new MockMacroPlayer();
            var sa = new SpecialAction("TestMacro3", "Cross", "Macro", "Macro", 0, "");
            sa.typeID = SpecialAction.ActionTypeId.Macro;
            var action = new MacroActionAdapter(sa, 1, mockPlayer);
            var ctx = new MockOutputContext(device: 1);

            action.Stop(ctx);

            Assert.Single(mockPlayer.StopCalls);
            Assert.Equal(1, mockPlayer.StopCalls[0]);
        }

        [Fact]
        public void T4_MultipleExecutionsAndReset_TracksCorrectly()
        {
            var mockPlayer = new MockMacroPlayer();
            var sa = new SpecialAction("TestMacro4", "Cross", "Macro", "Macro", 0, "");
            sa.typeID = SpecialAction.ActionTypeId.Macro;
            var action = new MacroActionAdapter(sa, 0, mockPlayer);
            var ctx = new MockOutputContext(device: 0);

            action.Execute(ctx);
            action.Execute(ctx);

            Assert.Equal(2, mockPlayer.PlayCalls.Count);

            mockPlayer.Reset();
            Assert.Empty(mockPlayer.PlayCalls);
            Assert.Empty(mockPlayer.StopCalls);
        }
    }
}