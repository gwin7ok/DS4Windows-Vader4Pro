using System;

namespace DS4Windows.DI
{
    /// <summary>
    /// アプリ全体設定（Profiles.xml）の永続化・状態管理および変更通知を提供するサービス。
    /// </summary>
    public interface IAppSettingsService
    {
        bool Save();
        bool Load();

        event EventHandler<string> SettingChanged;

        bool StartMinimized { get; set; }
        bool MinimizeToTaskbar { get; set; }
        bool CloseMinimizes { get; set; }
        int CheckWhen { get; set; }
        bool UseUdpServer { get; set; }
        int UdpServerPort { get; set; }
        string UdpServerListenAddress { get; set; }
        bool UseExclusiveMode { get; set; }
        bool AutoProfileRevertDefaultProfile { get; set; }

        // ---- Phase5-Step14前クリーンアップ: MainWindow.xaml.cs残存Global参照の解消 ----
        string LogMinLevel { get; set; }
        bool CheckUpdateStartupEnabled { get; set; }
        int CheckEveryValue { get; set; }
        int CheckEveryUnit { get; set; }
        DateTime LastChecked { get; set; }
        ulong LastVersionCheckedNum { get; }
        bool FirstRun { get; set; }
        bool RunHotPlug { get; set; }

        // ---- Phase5-Step13-7: ウィンドウ位置・サイズ、コントローラー一覧列幅の永続化 ----
        int FormWidth { get; set; }
        int FormHeight { get; set; }
        int FormLocationX { get; set; }
        int FormLocationY { get; set; }
        int ControllerIndexColWidth { get; set; }
        int ControllerIdColWidth { get; set; }
        int ControllerStatusColWidth { get; set; }
        int ControllerExclusiveColWidth { get; set; }
        int ControllerBatteryColWidth { get; set; }
        int ControllerSelectProfileColWidth { get; set; }
        int ControllerEditColWidth { get; set; }
        int ControllerLinkedProfileColWidth { get; set; }
        int ControllerLinkProfIdColWidth { get; set; }
        int ControllerCustomColorColWidth { get; set; }

        // ---- Phase5-Step13-7: 通知レベル・スワイプ操作設定 ----
        int Notifications { get; set; }
        bool SwipeProfiles { get; set; }
        /// <summary>
        /// プロファイル切替時に独自デスクトップ通知ウィンドウを表示するかどうか
        /// </summary>
        bool ProfileChangedNotification { get; set; }

        // ---- Phase5-Step14: プロファイル編集画面・SpecialAction カラム幅の永続化 ----
        int ProfileEditorLeftWidth { get; set; }
        int ProfileEditorRightWidth { get; set; }
        int SpecialActionNameColWidth { get; set; }
        int SpecialActionTriggerColWidth { get; set; }
        int SpecialActionDetailColWidth { get; set; }

        // ---- Phase6-Step2-1 (PR-1): ControlService の Global 直接参照解消 ----
        // いずれも Global(m_Config/BackingStore) への薄い委譲。独自フィールドは保持しない（SSOT 維持）。
        bool UseOscServer { get; set; }
        bool UseOscSender { get; set; }
        bool InterpretingOscMonitoring { get; set; }
        int OscServerPort { get; set; }
        string OscSenderAddress { get; set; }
        int OscSenderPort { get; set; }
        bool QuickCharge { get; set; }
        bool DCBTatStop { get; set; }
        /// <summary>プロセス優先度の選択インデックス（MainWindow.ProcessPriorityClasses の添字）。</summary>
        int ProcessPriority { get; set; }
        ControlServiceDeviceOptions DeviceOptions { get; }
        event EventHandler UDPServerSmoothingMincutoffChanged;
        event EventHandler UDPServerSmoothingBetaChanged;

        // ---- Phase6-Step2-2 (PR-2): UDP サーバ平滑化パラメータ（Global への薄い委譲）----
        double UDPServerSmoothingMincutoff { get; set; }
        double UDPServerSmoothingBeta { get; set; }

        // ---- Phase6-Step2-4 (PR-4): 遅延警告時のライトバー点滅（Global への薄い委譲）----
        bool FlashWhenLate { get; set; }

        // ---- Phase6-Step2-5 (PR-5): 入力処理ホットパスで参照する設定（Global への薄い委譲。独自のキャッシュは持たない）----
        bool UseUdpServerSmoothing { get; set; }
        int FlashWhenLateAt { get; set; }
    }
}