using System;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using DS4Windows;
using DS4Windows.Actions;
using DS4Windows.DI;

namespace DS4WindowsTests
{
    /// <summary>
    /// C4 ProfileSwitchAction 単体テスト (T1〜T5)
    /// </summary>
    public class ProfileSwitchActionTests : IDisposable
    {
        private readonly MockProfileSwitcher _mockSwitcher;

        public ProfileSwitchActionTests()
        {
            _mockSwitcher = new MockProfileSwitcher();
            var services = new ServiceCollection();
            services.AddSingleton<IProfileSwitcher>(_mockSwitcher);
            ServiceProviderHolder.SetProvider(services.BuildServiceProvider());
        }

        public void Dispose()
        {
            ServiceProviderHolder.SetProvider(null);
        }

        private static SpecialAction CreateProfileAction(string name, string targetProfile)
        {
            return new SpecialAction(name, "None", "Profile", targetProfile, 0.0, string.Empty)
            {
                typeID = SpecialAction.ActionTypeId.Profile
            };
        }

        [Fact]
        public void T1_Execute_CallsProfileSwitcherWithCorrectDeviceAndAction()
        {
            // Arrange
            var sa = CreateProfileAction("SwitchToFPS", "FPS_Profile.xml");
            var action = new ProfileSwitchAction(sa, 0);

            // Act
            action.Execute(null);

            // Assert
            Assert.Single(_mockSwitcher.SwitchProfileCalls);
            Assert.Equal(0, _mockSwitcher.SwitchProfileCalls[0].DeviceIndex);
            Assert.Equal("SwitchToFPS", _mockSwitcher.SwitchProfileCalls[0].Action.name);
            Assert.Equal("FPS_Profile.xml", _mockSwitcher.SwitchProfileCalls[0].Action.details);
        }

        [Fact]
        public void T2_Execute_PassesTargetDeviceIndexCorrectly()
        {
            // Arrange
            var sa = CreateProfileAction("SwitchToRacing", "Racing_Profile.xml");
            var action = new ProfileSwitchAction(sa, 3); // Device 3

            // Act
            action.Execute(null);

            // Assert
            Assert.Single(_mockSwitcher.SwitchProfileCalls);
            Assert.Equal(3, _mockSwitcher.SwitchProfileCalls[0].DeviceIndex);
        }

        [Fact]
        public void T3_Stop_CallsRestoreProfileWithCorrectDevice()
        {
            // Arrange
            var sa = CreateProfileAction("TempProfile", "Temp.xml");
            var action = new ProfileSwitchAction(sa, 1);

            // Act
            action.Stop(null);

            // Assert
            Assert.Single(_mockSwitcher.RestoreProfileCalls);
            Assert.Equal(1, _mockSwitcher.RestoreProfileCalls[0]);
        }

        [Fact]
        public void T4_MultipleExecutionsAndReset_TracksCorrectly()
        {
            // Arrange
            var sa = CreateProfileAction("ProfileA", "A.xml");
            var action = new ProfileSwitchAction(sa, 0);

            // Act
            action.Execute(null);
            action.Execute(null);
            action.Stop(null);

            // Assert
            Assert.Equal(2, _mockSwitcher.SwitchProfileCalls.Count);
            Assert.Single(_mockSwitcher.RestoreProfileCalls);

            // Reset 検証
            _mockSwitcher.Reset();
            Assert.Empty(_mockSwitcher.SwitchProfileCalls);
            Assert.Empty(_mockSwitcher.RestoreProfileCalls);
        }

        [Fact]
        public void T5_Execute_WithNullSpecialAction_DoesNotThrow()
        {
            // Arrange
            var action = new ProfileSwitchAction(null, 0);

            // Act & Assert
            var ex = Record.Exception(() => action.Execute(null));
            Assert.Null(ex);
            Assert.Empty(_mockSwitcher.SwitchProfileCalls);
        }
    }
}