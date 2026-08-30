﻿namespace DS4Windows.Services
{
    /// <summary>
    /// 指定した実行ファイルパスのプロセスが既に起動しているかを調べる抽象化。
    /// Global.LoadProfile 内の LaunchProgram（プロファイル関連付けアプリの自動起動）
    /// における多重起動防止チェック専用。Phase 3 Step 3-6.
    /// </summary>
    public interface IProcessInspector
    {
        /// <summary>
        /// 実行中の全プロセスを走査し、MainModule.FileName が exePath と一致するものが
        /// あるかどうかを返す。情報取得に失敗したプロセス（アクセス権限不足等）は無視する。
        /// </summary>
        bool IsProcessRunning(string exePath);
    }
}
