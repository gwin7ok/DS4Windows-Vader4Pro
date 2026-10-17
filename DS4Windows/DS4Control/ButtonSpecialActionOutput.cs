using System;

namespace DS4Windows
{
    /// <summary>
    /// Phase6-Step10-3d: スペシャルアクションの「ボタン」種別（<see cref="SpecialAction.ActionTypeId.Button"/>）の、
    /// 出力を ON にするかどうかの判定。Press は、トリガーが成立している間だけ ON。Toggle は、トリガーの立ち上がりで
    /// <c>ActionManager</c> の <c>IsToggledOn</c> を反転し、ON の間は出力する（キー種別の Toggle と同じ状態を使う）。
    /// <c>Mapping.cs</c> の Stage3（Synchronous Button SpecialAction handling）が、毎ティック呼び出す。
    /// </summary>
    public static class ButtonSpecialActionOutput
    {
        /// <param name="sa">ボタン種別のスペシャルアクション。</param>
        /// <param name="device">スロット番号。</param>
        /// <param name="triggered">現在のティックで、トリガーが成立しているか。</param>
        /// <param name="prevTriggered">直前のティックで、トリガーが成立していたか。</param>
        /// <returns>このティックで、ボタンの出力を ON にするか。</returns>
        public static bool ResolveOutputActive(SpecialAction sa, int device, bool triggered, bool prevTriggered)
        {
            bool isToggle = sa != null && sa.KeyButtonSwitchMode.HasValue &&
                sa.KeyButtonSwitchMode.Value == SpecialAction.KeyButtonSwitchModeEnum.Toggle;
            if (!isToggle) return triggered;

            ActionInstanceState state = ActionManager.GetStateFor(sa, device);
            if (state == null) return triggered;

            bool risingEdge = triggered && !prevTriggered;
            // プロファイル適用・接続の直後の押しっぱなしでは反転しない（Fresh-press ガード。Phase6-Step10-4-2e と同じ考え方）。
            // ガードの解除は、トリガーが連続して未成立になったときに、Mapping.MapCustomAction の冒頭で行われる。
            if (risingEdge && !state.RequiresFreshPressAfterReset)
            {
                bool next = !state.IsToggledOn;
                try
                {
                    ActionManager.SetToggledOn(sa, device, next);
                }
                catch
                {
                    // 管理役（IManagedActionManager）が無い場合は、状態を直接更新する
                    state.IsToggledOn = next;
                }
            }

            return state.IsToggledOn;
        }
    }
}
