using System;
using DS4Windows;
using DS4WinWPF;

namespace DS4Windows.DI
{
    public interface IAutoProfileService
    {
        bool Running { get; set; }
        int AutoProfileDebugLogLevel { get; set; }

        event Action<bool> RequestServiceChange;

        /// <summary>
        /// 自動プロファイルの監視チェックを1回実行します（直列化保護内包）。
        /// </summary>
        void CheckProfiles();

        /// <summary>
        /// 内部のウィンドウ・プロセス追跡キャッシュをクリアします。
        /// </summary>
        void ClearState();

        // ---- Phase5-Step13-4: AutoProfileHolder二重インスタンス問題の是正 ----
        /// <summary>
        /// 本サービスが監視に使用している唯一の <see cref="AutoProfileHolder"/> インスタンスを公開する。
        /// UI層（AutoProfiles画面）はこの参照を共有すること。独自に new AutoProfileHolder() を生成すると、
        /// 画面での編集・保存内容がバックグラウンド監視（CheckProfiles）に反映されない二重インスタンス問題が発生する。
        /// </summary>
        AutoProfileHolder Holder { get; }

        /// <summary>
        /// AutoProfile切替時の通知表示方法（Global.autoProfileSwitchNotifyChoiceの薄い委譲）。
        /// </summary>
        AutoProfileDisplayProfileSwitchChoices AutoProfileSwitchNotifyChoice { get; set; }

        // ---- Phase6-Step10-2-1: AutoProfiles 画面の Global.CreateAutoProfiles 参照の置換先（Global への薄い委譲）----
        /// <summary>
        /// 空の自動プロファイル設定ファイル（Auto Profiles.xml）を作る（<c>Global.CreateAutoProfiles(path)</c> と同じ動作）。
        /// 作成に失敗したら false。
        /// </summary>
        bool CreateAutoProfilesFile(string path);
    }
}