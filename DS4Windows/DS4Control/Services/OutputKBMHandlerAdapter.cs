using DS4Windows.DS4Control;

namespace DS4Windows.Services
{
    /// <summary>
    /// Global.outputKBMHandler への遅延委譲アダプタ。
    /// DIコンテナ経由で注入され、実行時に初期化される Global.outputKBMHandler に処理を転送する。
    /// </summary>
    public class OutputKBMHandlerAdapter : IVirtualKBM
    {
        public string ErrorMessage => Global.outputKBMHandler?.ErrorMessage ?? string.Empty;
        public string Version => Global.outputKBMHandler?.Version ?? "0.0.0.0";
        public bool fakeKeyRepeat
        {
            get => Global.outputKBMHandler?.fakeKeyRepeat ?? false;
            set { if (Global.outputKBMHandler != null) Global.outputKBMHandler.fakeKeyRepeat = value; }
        }

        public bool Connect() => Global.outputKBMHandler?.Connect() ?? false;
        public bool Disconnect() => Global.outputKBMHandler?.Disconnect() ?? false;

        public void MoveRelativeMouse(int x, int y) => Global.outputKBMHandler?.MoveRelativeMouse(x, y);
        public void MoveAbsoluteMouse(double x, double y) => Global.outputKBMHandler?.MoveAbsoluteMouse(x, y);

        public void PerformMouseWheelEvent(int vertical, int horizontal) => Global.outputKBMHandler?.PerformMouseWheelEvent(vertical, horizontal);
        public void PerformMouseButtonEvent(uint mouseButton) => Global.outputKBMHandler?.PerformMouseButtonEvent(mouseButton);
        public void PerformMouseButtonEventAlt(uint mouseButton, int type) => Global.outputKBMHandler?.PerformMouseButtonEventAlt(mouseButton, type);

        public void PerformMouseButtonPress(uint mouseButton) => Global.outputKBMHandler?.PerformMouseButtonPress(mouseButton);
        public void PerformMouseButtonRelease(uint mouseButton) => Global.outputKBMHandler?.PerformMouseButtonRelease(mouseButton);

        public void PerformKeyPress(uint key) => Global.outputKBMHandler?.PerformKeyPress(key);
        public void PerformKeyPressAlt(uint key) => Global.outputKBMHandler?.PerformKeyPressAlt(key);
        public void PerformKeyRelease(uint key) => Global.outputKBMHandler?.PerformKeyRelease(key);
        public void PerformKeyReleaseAlt(uint key) => Global.outputKBMHandler?.PerformKeyReleaseAlt(key);

        public void Sync() => Global.outputKBMHandler?.Sync();

        public string GetDisplayName() => Global.outputKBMHandler?.GetDisplayName() ?? string.Empty;
        public string GetIdentifier() => Global.outputKBMHandler?.GetIdentifier() ?? string.Empty;
        public string GetFullDisplayName() => Global.outputKBMHandler?.GetFullDisplayName() ?? string.Empty;
    }
}
