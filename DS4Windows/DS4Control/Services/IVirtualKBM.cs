namespace DS4Windows.Services
{
    /// <summary>
    /// 仮想キーボード・マウス（KBM）出力の抽象化インターフェース。
    /// SendInput / FakerInput 等の具象出力をカプセル化する。
    /// </summary>
    public interface IVirtualKBM
    {
        string ErrorMessage { get; }
        string Version { get; }

        bool Connect();
        bool Disconnect();

        void MoveRelativeMouse(int x, int y);
        void MoveAbsoluteMouse(double x, double y);

        void PerformMouseWheelEvent(int vertical, int horizontal);
        void PerformMouseButtonEvent(uint mouseButton);
        void PerformMouseButtonEventAlt(uint mouseButton, int type);

        void PerformMouseButtonPress(uint mouseButton);
        void PerformMouseButtonRelease(uint mouseButton);

        void PerformKeyPress(uint key);
        void PerformKeyPressAlt(uint key);
        void PerformKeyRelease(uint key);
        void PerformKeyReleaseAlt(uint key);

        void Sync();

        string GetDisplayName();
        string GetIdentifier();
        string GetFullDisplayName();
    }
}
