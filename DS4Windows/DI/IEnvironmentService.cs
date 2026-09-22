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
    ///
    /// 【Phase6-Step3-1】<c>PrepareAbsMonitorBounds</c> は、同じ画面座標状態
    /// （absDisplayBounds/fullDesktopBounds/absUseAllMonitors）を扱う
    /// <see cref="IDisplayCoordinateService"/> へ移設した（決定D1）。
    /// </summary>
    public interface IEnvironmentService
    {
        bool IsAdministrator();
        string ApplicationVersion { get; }
        void RefreshHidHideInfo();
        void RefreshFakerInputInfo();

        // ---- Phase6-Step2-1 (PR-1): ControlService の Global 直接参照解消（Global への薄い委譲）----
        /// <summary>HidHide が導入済みかどうか（RefreshHidHideInfo で更新される現在値）。</summary>
        bool HidHideInstalled { get; }
        string GetInstanceIdFromDevicePath(string devicePath);
        bool CheckHidHideAffectedStatus(string deviceInstanceId,
            System.Collections.Generic.HashSet<string> affectedDevs,
            System.Collections.Generic.HashSet<string> exemptedDevices, bool force = false);

        // ---- Phase6-Step2-1b (決定O2=C): コントローラースロット上限のサービス化 ----
        /// <summary>
        /// 同時に扱えるコントローラースロット数の上限（<b>現在接続中の台数ではない</b>）。
        /// OS 判定（Windows 8 以上で 8、未満で 4）とビルド定義 FORCE_4_INPUT から決まり、プロセス内で不変。
        /// </summary>
        int ControllerSlotLimit { get; }
        /// <summary><see cref="ControllerSlotLimit"/> が拡張スロット数（8）であるか。</summary>
        bool UsingMaxControllers { get; }

        // ---- Phase6-Step2-3 (PR-3): FakerInput 導入バージョン（Global への薄い委譲。利用者は IVirtualKBMLifecycle 実装）----
        string FakerInputVersion { get; }
    }
}