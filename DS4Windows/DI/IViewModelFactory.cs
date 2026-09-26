using System;
using DS4Windows;
using DS4WinWPF;
using DS4WinWPF.DS4Forms;
using DS4WinWPF.DS4Forms.ViewModels;

namespace DS4Windows.DI
{
    public interface IViewModelFactory
    {
        /// <param name="device">設定を読み書きするスロット（プロファイル編集画面では編集用の作業スロット）</param>
        /// <param name="targetDevice">
        /// Phase6-Step7b: ランブルテスト・ライトバーのプレビュー・校正などで使う実機のスロット番号。-1 は実機なし。
        /// </param>
        ProfileSettingsViewModel CreateProfileSettingsViewModel(int device, int targetDevice = -1);
        /// <param name="targetDevice">
        /// Phase6-Step7b: ライトバーのプレビューと記録中のタッチパッド Passthru で使う実機のスロット番号。-1 は実機なし。
        /// </param>
        RecordBoxViewModel CreateRecordBoxViewModel(int device, DS4ControlSettings controlSettings, bool recordMacro = true, bool extraHold = false, int targetDevice = -1);
        SpecialActEditorViewModel CreateSpecialActEditorViewModel(int device, SpecialAction action = null);
        AutoProfilesViewModel CreateAutoProfilesViewModel(AutoProfileHolder autoProfileHolder, ProfileList profileList);
    }
}
