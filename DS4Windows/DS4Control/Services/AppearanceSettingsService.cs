namespace DS4Windows.Services
{
    /// <summary>
    /// <see cref="DS4Windows.DI.IAppearanceSettingsService"/> の実装。
    /// Global(ScpUtil.cs) が保持する実体（テーマ・トレイアイコン設定）への薄いシムであり、
    /// 独自フィールドを持たない（Notifications 等と同一の Strangler Fig パターン）。
    /// </summary>
    public class AppearanceSettingsService : DS4Windows.DI.IAppearanceSettingsService
    {
        public AppThemeChoice UseCurrentTheme
        {
            get => Global.UseCurrentTheme;
            set => Global.UseCurrentTheme = value;
        }

        public TrayIconChoice UseIconChoice
        {
            get => Global.UseIconChoice;
            set => Global.UseIconChoice = value;
        }

        public string GetIconResourcePath(TrayIconChoice choice) => Global.iconChoiceResources[choice];
    }
}