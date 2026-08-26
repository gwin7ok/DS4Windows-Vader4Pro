using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using DS4Windows;
using DS4Windows.Actions;
using DS4Windows.DI;

namespace DS4WindowsTests
{
    /// <summary>
    /// C3 MacroAction 単体テスト (T1〜T5)
    /// </summary>
    public class MacroActionTests : IDisposable
    {
        private readonly MockMacroPlayer _mockPlayer;

        public MacroActionTests()
        {
            _mockPlayer = new MockMacroPlayer();
            var services = new ServiceCollection();
            services.AddSingleton<IMacroPlayer>(_mockPlayer);
            ServiceProviderHolder.SetProvider(services.BuildServiceProvider());
        }

        public void Dispose()
        {
            ServiceProviderHolder.SetProvider(null);
        }

        private static SpecialAction CreateMacroAction(string name, List<int> macroSteps)
        {
            return new SpecialAction(name, "None", "Macro", string.Empty, 0.0, string.Empty)
            {
                typeID = SpecialAction.ActionTypeId.Macro,
                macro = macroSteps
            };
        }

        [Fact]
        public void T1_Execute_CallsMacroPlayerWithCorrectDeviceAndAction()
        {
            // Arrange
            var steps = new List<int> { 65, 1000, 1065 }; // Key 'A' press, delay 700ms, release
            var sa = CreateMacroAction("TestMacro1", steps);
            var action = new MacroAction(sa, 0);

            // Act
            action.Execute(null);

            // Assert
            Assert.Single(_mockPlayer.PlayCalls);
            Assert.Equal(0, _mockPlayer.PlayCalls[0].DeviceIndex);
            Assert.Equal("TestMacro1", _mockPlayer.PlayCalls[0].Action.name);
            Assert.Equal(steps, _mockPlayer.PlayCalls[0].Action.macro);
        }

        [Fact]
        public void T2_Execute_PassesTargetDeviceIndexCorrectly()
        {
            // Arrange
            var sa = CreateMacroAction("TestMacroDevice2", new List<int> { 66 });
            var action = new MacroAction(sa, 2); // Device 2

            // Act
            action.Execute(null);

            // Assert
            Assert.Single(_mockPlayer.PlayCalls);
            Assert.Equal(2, _mockPlayer.PlayCalls[0].DeviceIndex);
        }

        [Fact]
        public void T3_Stop_CallsMacroPlayerStopWithCorrectDevice()
        {
            // Arrange
            var sa = CreateMacroAction("TestMacroStop", new List<int> { 67 });
            var action = new MacroAction(sa, 1);

            // Act
            action.Stop(null);

            // Assert
            Assert.Single(_mockPlayer.StopCalls);
            Assert.Equal(1, _mockPlayer.StopCalls[0]);
        }

        [Fact]
        public void T4_MultipleExecutionsAndReset_TracksCorrectly()
        {
            // Arrange
            var sa = CreateMacroAction("TestRepeat", new List<int> { 68 });
            var action = new MacroAction(sa, 0);

            // Act
            action.Execute(null);
            action.Execute(null);
            action.Stop(null);

            // Assert
            Assert.Equal(2, _mockPlayer.PlayCalls.Count);
            Assert.Single(_mockPlayer.StopCalls);

            // Reset 検証
            _mockPlayer.Reset();
            Assert.Empty(_mockPlayer.PlayCalls);
            Assert.Empty(_mockPlayer.StopCalls);
        }

        [Fact]
        public void T5_Execute_WithNullSpecialAction_DoesNotThrow()
        {
            // Arrange
            var action = new MacroAction(null, 0);

            // Act & Assert
            var ex = Record.Exception(() => action.Execute(null));
            Assert.Null(ex);
            Assert.Empty(_mockPlayer.PlayCalls);
        }
    }
}