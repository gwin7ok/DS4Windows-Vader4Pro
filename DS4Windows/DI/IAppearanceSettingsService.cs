using DS4Windows;

namespace DS4Windows.DI
{
    /// <summary>
    /// アプリテーマおよびトレイアイコン表示設定を提供するサービス。
    /// 全体移行計画書 §5.4 #9 で計画されていたが未実装のまま残っていたため、
    /// Phase5-Step14前クリーンアップの一環として新設する。
    /// </summary>
    public interface IAppearanceSettingsService
    {
        AppThemeChoice UseCurrentTheme { get; set; }
        TrayIconChoice UseIconChoice { get; set; }

        /// <summary>
        /// 指定したトレイアイコン種別に対応するリソースパスを取得します。
        /// </summary>
        string GetIconResourcePath(TrayIconChoice choice);
    }
}