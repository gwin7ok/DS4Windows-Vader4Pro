using System.Collections.Generic;
using DS4Windows.DS4Control;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase7-Step7 W1 のテスト用スタブ。<c>OutputKBMHandlerAdapter.Handler</c>（旧 Global.outputKBMHandler）（<see cref="VirtualKBMBase"/>）へ差して、
    /// <c>Mapping</c> が <c>IVirtualKBM</c>（アダプタ経由）へ送る出力を記録する。実際の入力は送出しない。
    /// </summary>
    public sealed class RecordingVirtualKbmBase : VirtualKBMBase
    {
        public readonly List<uint> MouseButtonEvents = new List<uint>();
        public readonly List<(uint Button, int Type)> MouseButtonAltEvents = new List<(uint, int)>();
        public readonly List<(int X, int Y)> MoveEvents = new List<(int, int)>();
        public readonly List<uint> KeyReleases = new List<uint>();
        public int SyncCount;

        public override bool Connect() => true;
        public override bool Disconnect() => true;
        public override void MoveRelativeMouse(int x, int y) => MoveEvents.Add((x, y));
        public override void PerformMouseWheelEvent(int vertical, int horizontal) { }
        public override void PerformMouseButtonEvent(uint mouseButton) => MouseButtonEvents.Add(mouseButton);
        public override void PerformMouseButtonEventAlt(uint mouseButton, int type) => MouseButtonAltEvents.Add((mouseButton, type));
        public override void PerformMouseButtonPress(uint mouseButton) { }
        public override void PerformMouseButtonRelease(uint mouseButton) { }
        public override void PerformKeyPress(uint key) { }
        public override void PerformKeyPressAlt(uint key) { }
        public override void PerformKeyRelease(uint key) => KeyReleases.Add(key);
        public override void PerformKeyReleaseAlt(uint key) => KeyReleases.Add(key);
        public override void Sync() => SyncCount++;
        public override string GetDisplayName() => "Recording";
        public override string GetIdentifier() => "Recording";
        public override string GetFullDisplayName() => "Recording";
    }
}
