using DS4Windows.DI;

namespace DS4Windows
{
    /// <summary>
    /// <see cref="IEnvironmentService"/> の実装。
    ///
    /// 【Phase5-Step14 FormSettings統一】
    /// 以前は本クラスが RunAtStartup / StartMinimized / CloseMinimizes / UseLang /
    /// FormWidth / FormHeight / FormLocationX / FormLocationY を、Global/m_Config(BackingStore)
    /// と一切連動しない独自の private field として孤立保持していた。これは
    /// <see cref="Services.AppSettingsService"/> が正規に保持する同名設定と意味的に重複しており、
    /// 実害はなかったものの将来の誤接続を誘発する地雷であったため撤去した。
    /// 永続設定は必ず <see cref="IAppSettingsService"/> 経由でアクセスすること。
    /// </summary>
    public class EnvironmentService : IEnvironmentService
    {
        // 読み取り専用のシステム状態プローブ／実行時操作であり、永続化設定ではないため、
        // 独自フィールドを持たずGlobalへ直接委譲する。
        public bool IsAdministrator() => Global.IsAdministrator();

        public string ApplicationVersion => Global.exeversion;

        public void RefreshHidHideInfo() => Global.RefreshHidHideInfo();

        public void RefreshFakerInputInfo() => Global.RefreshFakerInputInfo();
    }
}