using System;
using DS4Windows.Actions;
using DS4Windows.DS4Control;
using Xunit;

namespace DS4Windows.Actions.Tests
{

    // Minimal fake KBM handler that reports fakeKeyRepeat=true and no-ops on key events.
    class FakeKBMHandler : VirtualKBMBase
    {
        public FakeKBMHandler()
        {
            // Use false so controllers create internal RepeatHelper paths
            fakeKeyRepeat = false;
        }

        public override bool Connect() => true;
        public override bool Disconnect() => true;
        public override void MoveRelativeMouse(int x, int y) { }
        public override void PerformMouseWheelEvent(int vertical, int horizontal) { }
        public override void PerformMouseButtonEvent(uint mouseButton) { }
        public override void PerformMouseButtonPress(uint mouseButton) { }
        public override void PerformMouseButtonRelease(uint mouseButton) { }
        public override void PerformKeyPress(uint key) { }
        public override void PerformKeyPressAlt(uint key) { }
        public override void PerformKeyRelease(uint key) { }
        public override void PerformKeyReleaseAlt(uint key) { }
        public override string GetDisplayName() => "FakeKBM";
        public override string GetIdentifier() => "FakeKBM";
        public override string GetFullDisplayName() => "FakeKBM";
    }
}