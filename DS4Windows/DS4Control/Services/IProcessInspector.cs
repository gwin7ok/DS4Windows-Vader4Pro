namespace DS4Windows.Services
{
    /// <summary>
    /// プロセス状態およびアクティブウィンドウを調査するサービスのインターフェース。
    /// </summary>
    public interface IProcessInspector
    {
        /// <summary>
        /// 実行中の全プロセスを走査し、MainModule.FileName が exePath と一致するものが
        /// あるかどうかを返す。情報取得に失敗したプロセス（アクセス権限不足等）は無視する。
        /// </summary>
        bool IsProcessRunning(string exePath);

        /// <summary>
        /// 現在フォアグラウンド（最前面）にあるウィンドウの実行ファイルパスおよびタイトルを取得します。
        /// </summary>
        /// <param name="processPath">プロセス実行ファイルパス（小文字・バックスラッシュ正規化）</param>
        /// <param name="windowTitle">ウィンドウタイトル（小文字）</param>
        /// <returns>取得できた場合 true</returns>
        bool GetForegroundProcessInfo(out string processPath, out string windowTitle);
    }
}
