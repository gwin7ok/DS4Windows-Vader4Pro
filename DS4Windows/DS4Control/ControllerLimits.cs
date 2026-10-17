namespace DS4Windows
{
    /// <summary>
    /// Phase7-Step9 W2（Q2＝A）: コントローラーの台数・スティックの中点などの定数（旧: <c>Global</c> の const）。
    /// <c>Global</c> の同名の定数は、この値を指す別名として残してある（呼び出し元約 250 件は Step10 で順に移す）。
    /// </summary>
    public static class ControllerLimits
    {
        /// <summary>同時に扱うコントローラーの最大数（旧: <c>Global.MAX_DS4_CONTROLLER_COUNT</c>）。</summary>
        public const int MaxControllerCount = 8;

        /// <summary>プロファイル項目の数。テスト用プロファイルを 1 つ含む（旧: <c>Global.TEST_PROFILE_ITEM_COUNT</c>）。</summary>
        public const int TestProfileItemCount = MaxControllerCount + 1;

        /// <summary>テスト用プロファイルの添字（旧: <c>Global.TEST_PROFILE_INDEX</c>）。</summary>
        public const int TestProfileIndex = TestProfileItemCount - 1;

        /// <summary>旧 XInput のコントローラー数（旧: <c>Global.OLD_XINPUT_CONTROLLER_COUNT</c>）。</summary>
        public const int OldXInputControllerCount = 4;

        /// <summary>スティック軸の中点（旧: <c>Global.DS4_STICK_AXIS_MIDPOINT</c>）。</summary>
        public const byte StickAxisMidpoint = 128;
    }
}
