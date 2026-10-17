namespace DS4Windows.DI
{
    /// <summary>
    /// ログ設定（保存するログの数・最小ログレベル）の変更を、ログ機能（NLog の構成）へ反映する窓口。
    /// Phase6-Step10-1-5（決定D7＝A）: 設定画面（SettingsViewModel）が静的フィールド <c>App.logHolder</c> を
    /// 直接参照していたため、実装（LoggerHolder）への依存を、この契約へ置き換えた。
    /// </summary>
    public interface ILogSettingsApplier
    {
        /// <summary>設定（Profiles.xml）のログ設定を NLog の構成へ反映する。</summary>
        void UpdateLogSettings();

        /// <summary>NLog.config の maxArchiveFiles を更新する。</summary>
        void UpdateNLogConfig(int maxArchiveFiles);

        /// <summary>すべての設定更新の後に、archiveOldFileOnStartup を元へ戻す。</summary>
        void RestoreArchiveSetting();
    }
}
