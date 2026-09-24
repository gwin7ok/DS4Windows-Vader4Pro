using System.Collections.Generic;
using DS4Windows.Services;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase6-Step4 のテスト用スタブ。マウス移動・ホイールの送出を記録するだけで、実際の入力は送出しない。
    /// </summary>
    public sealed class RecordingVirtualKbm : IVirtualKBM
    {
        public readonly List<(int X, int Y)> MoveEvents = new List<(int, int)>();
        public readonly List<(int Vertical, int Horizontal)> WheelEvents = new List<(int, int)>();

        public string ErrorMessage => string.Empty;
        public string Version => "0.0.0.0";
        public bool fakeKeyRepeat { get; set; }
        public bool Connect() => true;
        public bool Disconnect() => true;
        public void MoveRelativeMouse(int x, int y) => MoveEvents.Add((x, y));
        public void PerformMouseWheelEvent(int vertical, int horizontal) => WheelEvents.Add((vertical, horizontal));
        public void PerformMouseButtonEvent(uint mouseButton) { }
        public void PerformMouseButtonEventAlt(uint mouseButton, int type) { }
        public void PerformMouseButtonPress(uint mouseButton) { }
        public void PerformMouseButtonRelease(uint mouseButton) { }
        public void PerformKeyPress(uint key) { }
        public void PerformKeyPressAlt(uint key) { }
        public void PerformKeyRelease(uint key) { }
        public void PerformKeyReleaseAlt(uint key) { }
        public void Sync() { }
        public string GetDisplayName() => string.Empty;
        public string GetIdentifier() => string.Empty;
        public string GetFullDisplayName() => string.Empty;
    }
}
