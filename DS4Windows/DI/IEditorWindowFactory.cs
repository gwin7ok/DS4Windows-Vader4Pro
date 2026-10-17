using DS4WinWPF;
using DS4WinWPF.DS4Forms;
using DS4WinWPF.DS4Forms.ViewModels;

namespace DS4Windows.DI
{
    /// <summary>
    /// プロファイル編集画面などから開く、補助ウィンドウ（ボタン割り当て、マクロ記録、プリセット、スティック校正、
    /// スペシャルアクション編集）を生成する窓口。
    /// Phase6-Step10-2（決定 E1＝A）: 呼び出し側（<c>ProfileEditor</c> など）が <c>new</c> で生成し、必要なサービスを
    /// 連鎖して渡す代わりに、この窓口へ生成を依頼する。必要なサービスは実装（<c>EditorWindowFactory</c>）が必須引数で保持する。
    /// ウィンドウは UI スレッドで生成すること。
    /// </summary>
    public interface IEditorWindowFactory
    {
        /// <param name="deviceNum">設定を読み書きするスロット（プロファイル編集画面では編集用の作業スロット）</param>
        /// <param name="settings">編集対象のボタン設定</param>
        /// <param name="targetDevice">ランブルテスト・ライトバーのプレビュー・マクロ記録で使う実機のスロット番号。-1 は実機なし</param>
        BindingWindow CreateBindingWindow(int deviceNum, DS4ControlSettings settings, int targetDevice,
            BindingWindow.ExposeMode expose = BindingWindow.ExposeMode.Full);

        RecordBox CreateRecordBox(int deviceNum, DS4ControlSettings controlSettings, bool shift, int targetDevice,
            bool showscan = true, bool repeatable = true);

        RecordBoxWindow CreateRecordBoxWindow(int deviceNum, DS4ControlSettings settings, int targetDevice,
            bool repeatable = true);

        PresetOptionWindow CreatePresetOptionWindow();

        StickCalibrationWindow CreateStickCalibrationWindow(Stick stick, int device,
            ProfileSettingsViewModel profileSettingsViewModel);

        SpecialActionEditor CreateSpecialActionEditor(int deviceNum, ProfileList profileList, int targetDevice,
            SpecialAction specialAction = null);

        /// <summary>
        /// プロファイル編集画面（メイン画面に埋め込む）を生成する（Phase7-Step5 W5）。
        /// 旧: <c>MainWindow</c> が <c>new ProfileEditor(...)</c> で生成し、<c>ProfileEditor</c> が <c>AppHost</c> から 4 つのサービスを解決していた。
        /// </summary>
        /// <param name="targetDevice">編集の対象にする実機のスロット番号。-1 は実機なし</param>
        ProfileEditor CreateProfileEditor(int targetDevice);
    }
}
