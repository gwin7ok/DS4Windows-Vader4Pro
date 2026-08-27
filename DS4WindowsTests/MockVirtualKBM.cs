using System;
using System.Collections.Generic;
using DS4Windows.Services;

namespace DS4WindowsTests
{
    /// <summary>
    /// IVirtualKBM のテスト用モッククラス。呼び出し履歴を記録・検証可能。
    /// </summary>
    public class MockVirtualKBM : IVirtualKBM
    {
        public string ErrorMessage { get; set; } = string.Empty;
        public string Version { get; set; } = "1.0.0.0";
        public bool fakeKeyRepeat { get; set; } = false;

        public bool ConnectResult { get; set; } = true;
        public bool DisconnectResult { get; set; } = true;

        public int ConnectCallCount { get; private set; }
        public int DisconnectCallCount { get; private set; }
        public int SyncCallCount { get; private set; }

        public List<(int x, int y)> MoveRelativeCalls { get; } = new List<(int, int)>();
        public List<(double x, double y)> MoveAbsoluteCalls { get; } = new List<(double, double)>();
        public List<(int v, int h)> MouseWheelCalls { get; } = new List<(int, int)>();
        public List<uint> MouseButtonPressCalls { get; } = new List<uint>();
        public List<uint> MouseButtonReleaseCalls { get; } = new List<uint>();
        public List<uint> KeyPressCalls { get; } = new List<uint>();
        public List<uint> KeyReleaseCalls { get; } = new List<uint>();

        public bool Connect()
        {
            ConnectCallCount++;
            return ConnectResult;
        }

        public bool Disconnect()
        {
            DisconnectCallCount++;
            return DisconnectResult;
        }

        public void MoveRelativeMouse(int x, int y) => MoveRelativeCalls.Add((x, y));
        public void MoveAbsoluteMouse(double x, double y) => MoveAbsoluteCalls.Add((x, y));

        public void PerformMouseWheelEvent(int vertical, int horizontal) => MouseWheelCalls.Add((vertical, horizontal));
        public void PerformMouseButtonEvent(uint mouseButton) => MouseButtonPressCalls.Add(mouseButton);
        public void PerformMouseButtonEventAlt(uint mouseButton, int type) => MouseButtonPressCalls.Add(mouseButton);

        public void PerformMouseButtonPress(uint mouseButton) => MouseButtonPressCalls.Add(mouseButton);
        public void PerformMouseButtonRelease(uint mouseButton) => MouseButtonReleaseCalls.Add(mouseButton);

        public void PerformKeyPress(uint key) => KeyPressCalls.Add(key);
        public void PerformKeyPressAlt(uint key) => KeyPressCalls.Add(key);
        public void PerformKeyRelease(uint key) => KeyReleaseCalls.Add(key);
        public void PerformKeyReleaseAlt(uint key) => KeyReleaseCalls.Add(key);

        public void Sync() => SyncCallCount++;

        public string GetDisplayName() => "MockVirtualKBM";
        public string GetIdentifier() => "MockIdentifier";
        public string GetFullDisplayName() => "Mock Virtual KBM Device";
    }
}
