namespace DS4Windows.Services
{
    /// <summary>
    /// <see cref="DS4Windows.DI.IAppearanceSettingsService"/> の実装。
    /// Global(ScpUtil.cs) が保持する実体（テーマ・トレイアイコン設定）への薄いシムであり、
    /// 独自フィールドを持たない（Notifications 等と同一の Strangler Fig パターン）。
    /// </summary>
    public class AppearanceSettingsService : DS4Windows.DI.IAppearanceSettingsService
    {
        // Phase7-Step3 W5: Global 経由をやめ、注入された BackingStore（設定の唯一の実体）を直接使う。
        private readonly BackingStore _config;

        public AppearanceSettingsService(BackingStore config)
        {
            _config = config ?? throw new System.ArgumentNullException(nameof(config));
        }

        public AppThemeChoice UseCurrentTheme
        {
            get => _config.useCurrentTheme;
            set => _config.useCurrentTheme = value;
        }

        public TrayIconChoice UseIconChoice
        {
            get => _config.useIconChoice;
            set => _config.useIconChoice = value;
        }

        public string GetIconResourcePath(TrayIconChoice choice) => DS4WinWPF.DS4Forms.TrayIconResources.iconChoiceResources[choice];

        // ---- Phase6-Step2-5 (PR-5): バッテリー残量の変化通知 ----
        // Global.BatteryChanged（static イベント）の購読者へ、従来と同一の送信元・引数で通知する。
        public void InvokeBatteryChanged(byte percentage) => Global.InvokeBatteryChanged(percentage);

        // ---- Phase6-Step9-1: 購読は Global.BatteryChanged へそのまま転送する（送信元・引数は従来と同一）----
        public event System.EventHandler<byte> BatteryChanged
        {
            add => Global.BatteryChanged += value;
            remove => Global.BatteryChanged -= value;
        }
    }
}