using System.Collections.Generic;
using DS4Windows;

namespace DS4WinWPF.DS4Forms
{
    /// <summary>
    /// Phase7-Step10b W2（Q1＝A）: トレイアイコンの選択ごとの資源パスの表（旧: <c>Global.iconChoiceResources</c>。宣言は移動のみ）。
    /// <c>IAppearanceSettingsService.GetIconResourcePath</c> の実装（<c>AppearanceSettingsService</c>）が参照する。
    /// TODO(Phase8 以降): サービスから画面側の型への参照が残っている。<c>IAppearanceSettingsService</c> は <c>TrayIconChoice</c> を返すだけにして、
    /// パスの解決を画面側に寄せる整理を、画面の責務の見直しとあわせて行う。
    /// </summary>
    public static class TrayIconResources
    {
        public static Dictionary<TrayIconChoice, string> iconChoiceResources = new Dictionary<TrayIconChoice, string>
        {
            [TrayIconChoice.Default] = $"{DS4WinWPF.DS4Forms.ResourcePaths.ResourcesPrefix}/DS4W.ico",
            [TrayIconChoice.Colored] = $"{DS4WinWPF.DS4Forms.ResourcePaths.ResourcesPrefix}/DS4W.ico",
            [TrayIconChoice.White] = $"{DS4WinWPF.DS4Forms.ResourcePaths.ResourcesPrefix}/DS4W - White.ico",
            [TrayIconChoice.Black] = $"{DS4WinWPF.DS4Forms.ResourcePaths.ResourcesPrefix}/DS4W - Black.ico",
            [TrayIconChoice.Battery] = $"{DS4WinWPF.DS4Forms.ResourcePaths.ResourcesPrefix}/DS4W.ico"
        };
    }
}
