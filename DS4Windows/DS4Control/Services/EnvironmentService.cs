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

        // ---- Phase6-Step2-1 (PR-1): ControlService の Global 直接参照解消 ----
        public bool HidHideInstalled => Global.hidHideInstalled;

        public string GetInstanceIdFromDevicePath(string devicePath) =>
            Global.GetInstanceIdFromDevicePath(devicePath);

        public bool CheckHidHideAffectedStatus(string deviceInstanceId,
            System.Collections.Generic.HashSet<string> affectedDevs,
            System.Collections.Generic.HashSet<string> exemptedDevices, bool force = false) =>
            Global.CheckHidHideAffectedStatus(deviceInstanceId, affectedDevs, exemptedDevices, force);

        public void PrepareAbsMonitorBounds(string edid) => Global.PrepareAbsMonitorBounds(edid);

        // ---- Phase6-Step2-1b (決定O2=C): コントローラースロット上限 ----
        // 旧 ControlService.CURRENT_DS4_CONTROLLER_LIMIT の計算（純粋な OS 判定＋ビルド定義）をここへ移設した。
        // TODO(技術的負債): ControlService の static 互換シム（CURRENT_DS4_CONTROLLER_LIMIT / USING_MAX_CONTROLLERS）が
        // 本値を共有するため static のまま保持している。互換シム撤去後（Phase6-Step12）はインスタンスフィールドへ戻す。
        internal static readonly int ProcessControllerSlotLimit = CalculateControllerSlotLimit();

        public int ControllerSlotLimit => ProcessControllerSlotLimit;

        public bool UsingMaxControllers => ProcessControllerSlotLimit == ControlService.EXPANDED_CONTROLLER_COUNT;

        // ---- Phase6-Step2-3 (PR-3): FakerInput 導入バージョン ----
        public string FakerInputVersion => Global.fakerInputVersion;

        private static int CalculateControllerSlotLimit()
        {
#if FORCE_4_INPUT
            return Global.OLD_XINPUT_CONTROLLER_COUNT;
#else
            return Global.IsWin8OrGreater() ? Global.MAX_DS4_CONTROLLER_COUNT : Global.OLD_XINPUT_CONTROLLER_COUNT;
#endif
        }
    }
}