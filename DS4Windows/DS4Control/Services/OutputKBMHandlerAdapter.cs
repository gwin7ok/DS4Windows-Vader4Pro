using DS4Windows.DS4Control;

namespace DS4Windows.Services
{
    /// <summary>
    /// 出力 KBM ハンドラの所有者であり、<see cref="IVirtualKBM"/> の実装。
    /// Phase7-Step8 W3（Q6＝A）: 旧 <c>Global.outputKBMHandler</c>（public static フィールド）の実体を、本クラスの <see cref="Handler"/> が持つ。
    /// 書き込むのは <c>OutputKBMHandlerLifecycle</c>（決定・破棄・フォールバック）だけで、読み取りは各メソッドが呼ぶたびに行う
    /// （実行時に初期化されるハンドラへ転送する遅延委譲の動作は従来と同じ）。ハンドラが無い間は、各操作は何もせず、戻り値は既定値になる。
    /// </summary>
    public class OutputKBMHandlerAdapter : IVirtualKBM
    {
        /// <summary>現在の出力ハンドラ（未決定、または解放済みのときは null）。</summary>
        public VirtualKBMBase Handler { get; set; }

        public bool fakeKeyRepeat
        {
            get => Handler?.fakeKeyRepeat ?? false;
            set { var handler = Handler; if (handler != null) handler.fakeKeyRepeat = value; }
        }

        public bool Connect() => Handler?.Connect() ?? false;
        public bool Disconnect() => Handler?.Disconnect() ?? false;

        public void MoveRelativeMouse(int x, int y) => Handler?.MoveRelativeMouse(x, y);

        public void PerformMouseWheelEvent(int vertical, int horizontal) => Handler?.PerformMouseWheelEvent(vertical, horizontal);
        public void PerformMouseButtonEvent(uint mouseButton) => Handler?.PerformMouseButtonEvent(mouseButton);
        public void PerformMouseButtonEventAlt(uint mouseButton, int type) => Handler?.PerformMouseButtonEventAlt(mouseButton, type);

        public void PerformMouseButtonPress(uint mouseButton) => Handler?.PerformMouseButtonPress(mouseButton);
        public void PerformMouseButtonRelease(uint mouseButton) => Handler?.PerformMouseButtonRelease(mouseButton);

        public void PerformKeyPress(uint key) => Handler?.PerformKeyPress(key);
        public void PerformKeyPressAlt(uint key) => Handler?.PerformKeyPressAlt(key);
        public void PerformKeyRelease(uint key) => Handler?.PerformKeyRelease(key);
        public void PerformKeyReleaseAlt(uint key) => Handler?.PerformKeyReleaseAlt(key);

        public void Sync() => Handler?.Sync();

        public string GetDisplayName() => Handler?.GetDisplayName() ?? string.Empty;
        public string GetIdentifier() => Handler?.GetIdentifier() ?? string.Empty;
        public string GetFullDisplayName() => Handler?.GetFullDisplayName() ?? string.Empty;
    }
}
