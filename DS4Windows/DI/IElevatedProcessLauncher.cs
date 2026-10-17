﻿namespace DS4Windows.Services
{
    /// <summary>
    /// 自プロセスをUAC昇格(runas)で子プロセス再起動し、終了を待ち合わせる抽象化。
    /// DS4Devices.RequestElevation（デバイス再有効化のための昇格要求）専用。
    /// IProcessLauncher（Actions/、単純起動用）とは責務が異なるため統合しない。
    /// Phase 3 Step 3-5.
    /// </summary>
    public interface IElevatedProcessLauncher
    {
        /// <summary>
        /// Global.exelocation を runas 昇格で子プロセスとして起動し、
        /// 最大 timeoutMs ミリ秒 WaitForExit する。タイムアウト時は子プロセスを Kill する。
        /// </summary>
        /// <param name="arguments">起動引数（例: "re-enabledevice {instanceId}"）</param>
        /// <param name="timeoutMs">WaitForExit のタイムアウト(ms)。既定30000（既存動作と同一）。</param>
        /// <returns>
        /// 時間内に終了した場合は子プロセスの ExitCode。
        /// タイムアウト(Kill)・起動失敗時は null（呼び出し元は StatusCode を更新しない＝既存の「失敗のまま」の暗黙契約を維持）。
        /// </returns>
        int? RelaunchElevated(string arguments, int timeoutMs = 30000);
    }
}
