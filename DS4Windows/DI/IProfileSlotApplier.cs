namespace DS4Windows.Services
{
    /// <summary>
    /// 接続時などに、指定スロットへプロファイルを適用するための <see cref="ControlService"/> 側の出力ポート。
    /// ControlService は <c>IProfileApplicationService</c> に直接依存せず、本インターフェース（依存の逆転）のみを知る。
    /// 戻り値は適用の成否で、呼び出し側が後続処理（ログ・プロファイル既定値の反映）に使う。
    /// </summary>
    public interface IProfileSlotApplier
    {
        bool ApplyToSlot(int slotIndex, string profileName, ProfileChangeSource source);
    }
}