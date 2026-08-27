namespace DS4Windows.Services
{
    /// <summary>
    /// 仮想キーボード・マウス（KBM）出力の抽象化インターフェース。
    /// SendInput / FakerInput / vMulti などの具象出力をカプセル化する。
    /// </summary>
    public interface IVirtualKBM
    {
        // --- 接続・ライフサイクル ---
        bool Connect();
        void Disconnect();
        bool InUse();

        // --- マウスカーソル移動 ---
        void MoveCursor(int x, int y);
        void MoveCursorBy(int x, int y);
        void MoveCursorTo(int x, int y);
        void MoveRelative(int x, int y);
        void MoveAbsolute(int x, int y);

        // --- マウスホイールスクロール ---
        void Scroll(int delta);
        void HScroll(int delta);
        void MouseWheel(int delta);
        void MouseHWheel(int delta);

        // --- マウスボタン ---
        void MouseDown(int mouseButton);
        void MouseUp(int mouseButton);
        void MouseClick(int mouseButton);
        void MouseDoubleClick(int mouseButton);

        // --- キーボード入力 ---
        void KeyDown(uint keyScanCode, bool extended = false);
        void KeyUp(uint keyScanCode, bool extended = false);
        void KeyPress(uint keyScanCode, bool extended = false);

        // --- 状態管理・デバイス情報 ---
        void Sync();
        void Reset();
        string GetDeviceType();
    }
}
