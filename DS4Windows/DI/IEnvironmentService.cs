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
    /// 【Phase6-Step3-1】<c>PrepareAbsMonitorBounds</c> は、当時同じ画面座標状態
    /// （absDisplayBounds/fullDesktopBounds/absUseAllMonitors）を扱っていた IDisplayCoordinateService へ
    /// 一旦移設した（決定D1）が、その後の機能廃止（Abs Mouse機能削除、copilot-instructions.md §2.2 例外規定）
    /// により、この画面座標系一式（IDisplayCoordinateServiceを含む）自体が丸ごと撤去された。
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
        // Phase7-Step10b W4（接続済み）: Phase7-Step2 W4 の調査で「接続」と判断した契約。ControlService.USING_MAX_CONTROLLERS（static 互換シム）の置換先で、
        // 呼び出し元 3 箇所を本プロパティの注入に切り替え、シムを削除した。利用者: AutoProfileHolder.UsingMaxControllers（保存時の 4〜8 番目の書き出し）、
        // AutoProfilesViewModel.UsingExpandedControllers、ProgramItem（生成元が値を渡す）。
        bool UsingMaxControllers { get; }

        // ---- Phase6-Step2-3 (PR-3): FakerInput 導入バージョン（Global への薄い委譲。利用者は IVirtualKBMLifecycle 実装）----
        string FakerInputVersion { get; }

        // ---- Phase6-Step9-1: MainWindowsViewModel／ProfileSettingsViewModel の Global 直接参照解消（Global への薄い委譲）----
        /// <summary>ViGEmBus が導入済みか（<c>EnvironmentService.IsViGEmBusInstalled</c>）。</summary>
        bool IsViGEmBusInstalled();
        /// <summary>導入済みの ViGEmBus が、サポートする最小バージョン以上か（<c>EnvironmentService.IsRunningSupportedViGEmBus</c>）。</summary>
        bool IsRunningSupportedViGEmBus();
        /// <summary>ViGEmBus がタッチパッド Passthru に必要な最小バージョン以上か（<c>EnvironmentService.IsUsingMinViGEm117333</c>）。</summary>
        bool IsUsingMinViGEm117333();
        /// <summary>プログラムフォルダへの書き込みに管理者権限が要るか（<c>Global.AdminNeeded</c>。テストファイルの作成・削除を試す）。</summary>
        bool AdminNeeded();

        // ---- Phase6-Step10-1-1: WelcomeDialog の Global 直接参照解消（Global への薄い委譲）----
        /// <summary>Windows 10 以降か（<c>EnvironmentService.IsWin10OrGreater</c>）。</summary>
        bool IsWin10OrGreater();
        /// <summary>Windows 8 以降か（<c>EnvironmentService.IsWin8OrGreater</c>）。</summary>
        bool IsWin8OrGreater();
        /// <summary>FakerInput ドライバが導入済みか（<c>EnvironmentService.IsFakerInputInstalled</c>。導入状態の再確認を行う）。</summary>
        bool IsFakerInputInstalled();
        /// <summary>
        /// HidHide ドライバが導入済みかを、その場で確認する（<c>EnvironmentService.IsHidHideInstalled</c>）。
        /// キャッシュ値の <see cref="HidHideInstalled"/>（<see cref="RefreshHidHideInfo"/> で更新）とは別物で、状態を更新しない。
        /// </summary>
        bool IsHidHideInstalled();
        /// <summary>導入済みの ViGEmBus の情報を取得し直す（<c>EnvironmentService.RefreshViGEmBusInfo</c>）。</summary>
        void RefreshViGEmBusInfo();
        /// <summary>導入済みの ViGEmBus のバージョン（<c>EnvironmentService</c> の static 状態。<see cref="RefreshViGEmBusInfo"/> で更新される）。</summary>
        System.Version ViGEmBusVersion { get; }
    }
}