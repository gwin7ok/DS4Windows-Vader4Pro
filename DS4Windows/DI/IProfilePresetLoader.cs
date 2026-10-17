namespace DS4Windows.DI
{
    /// <summary>
    /// プリセット（プロファイル編集画面の「プリセット」）の読み込み。
    /// Phase6-Step10-2（決定 E2＝A）: <c>PresetOption</c> が <c>Global.Load…Profile</c> を直接呼んでいたため、
    /// プリセットの関心事を 1 つの契約にまとめて置き換える。いずれも <c>Global</c> の同名メソッドへの薄い委譲
    /// （既定のスペシャルアクションの作成・一時プロファイル名のクリアを含む）。
    /// Phase7-Step3c W2: 実装は旧 <c>Global.Load…Profile</c> の処理を <c>ProfilePresetLoader</c> に移した（<c>BackingStore</c> と
    /// <c>IProfileSettingsService</c> を注入で受ける）。重複していた <c>IProfileRepository.LoadBlankProfile</c> は契約から削除した。
    /// 引数: <c>launchProgram</c>＝読み込み後にプログラムを起動するか、<c>xinputChange</c>＝出力デバイスの種類の変更を反映するか、
    /// <c>postLoad</c>＝読み込み後にデバイス設定を反映するか。
    /// </summary>
    public interface IProfilePresetLoader
    {
        /// <summary>旧 <c>Global.LoadBlankDevProfile</c>（空のプロファイル。Xbox 360 出力）。</summary>
        void LoadBlank(int deviceIndex, bool launchProgram, ControlService control, bool xinputChange = true, bool postLoad = true);

        /// <summary>旧 <c>Global.LoadBlankDS4Profile</c>（空のプロファイル。DualShock 4 出力）。</summary>
        void LoadBlankDS4(int deviceIndex, bool launchProgram, ControlService control, bool xinputChange = true, bool postLoad = true);

        /// <summary>旧 <c>Global.LoadDefaultGamepadGyroProfile</c>。</summary>
        void LoadDefaultGamepadGyro(int deviceIndex, bool launchProgram, ControlService control, bool xinputChange = true, bool postLoad = true);

        /// <summary>旧 <c>Global.LoadDefaultDS4GamepadGyroProfile</c>。</summary>
        void LoadDefaultDS4GamepadGyro(int deviceIndex, bool launchProgram, ControlService control, bool xinputChange = true, bool postLoad = true);

        /// <summary>旧 <c>Global.LoadDefaultMixedControlsProfile</c>。</summary>
        void LoadDefaultMixedControls(int deviceIndex, bool launchProgram, ControlService control, bool xinputChange = true, bool postLoad = true);

        /// <summary>旧 <c>Global.LoadDefaultDS4MixedControlsProfile</c>。</summary>
        void LoadDefaultDS4MixedControls(int deviceIndex, bool launchProgram, ControlService control, bool xinputChange = true, bool postLoad = true);

        /// <summary>旧 <c>Global.LoadDefaultMixedGyroMouseProfile</c>。</summary>
        void LoadDefaultMixedGyroMouse(int deviceIndex, bool launchProgram, ControlService control, bool xinputChange = true, bool postLoad = true);

        /// <summary>旧 <c>Global.LoadDefaultDS4MixedGyroMouseProfile</c>。</summary>
        void LoadDefaultDS4MixedGyroMouse(int deviceIndex, bool launchProgram, ControlService control, bool xinputChange = true, bool postLoad = true);

        /// <summary>旧 <c>Global.LoadDefaultKBMProfile</c>。</summary>
        void LoadDefaultKBM(int deviceIndex, bool launchProgram, ControlService control, bool xinputChange = true, bool postLoad = true);

        /// <summary>旧 <c>Global.LoadDefaultKBMGyroMouseProfile</c>。</summary>
        void LoadDefaultKBMGyroMouse(int deviceIndex, bool launchProgram, ControlService control, bool xinputChange = true, bool postLoad = true);
    }
}
