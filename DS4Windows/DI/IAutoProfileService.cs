using System;

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
    }
}
