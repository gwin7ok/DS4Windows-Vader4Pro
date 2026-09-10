namespace DS4Windows.DI
{
    /// <summary>
    /// 実行時の環境情報プローブ（管理者権限判定、アプリバージョン、HidHide/FakerInput状態の再取得）を
    /// 提供するサービス。
    ///
    /// 【Phase5-Step14 FormSettings統一】
    /// 以前は本インターフェースに RunAtStartup / StartMinimized / CloseMinimizes / UseLang /
    /// FormWidth / FormHeight / FormLocationX / FormLocationY という、
    /// <see cref="IAppSettingsService"/> と同名・同意味の永続設定プロパティが独自定義されていた。
    /// これらは Global/m_Config(BackingStore) と一切連動しない孤立した private field として
    /// 実装されており（Phase5-Step13で是正済みの「IAppSettingsService 7プロパティ孤立バグ」と
    /// 同一パターンの重複）、実害はなかったものの将来の誤接続を誘発する地雷であったため撤去した。
    /// 永続設定は必ず <see cref="IAppSettingsService"/> 経由でアクセスすること。
    /// </summary>
    public interface IEnvironmentService
    {
        bool IsAdministrator();
        string ApplicationVersion { get; }
        void RefreshHidHideInfo();
        void RefreshFakerInputInfo();
    }
}