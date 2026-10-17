using DS4WinWPF;
using DS4WinWPF.DS4Forms;

namespace DS4Windows.DI
{
    /// <summary>
    /// メイン画面（<see cref="MainWindow"/>）を生成する窓口（Phase7-Step5 W5）。
    /// 旧: <c>MainWindow</c> のコンストラクターが <c>AppHost.GetService</c> で 17 件のサービスを解決していた。
    /// 実装（<c>MainWindowFactory</c>）が必要なサービスを DI から受け取り、画面のコンストラクター引数として渡す
    /// （<see cref="IEditorWindowFactory"/>・<see cref="IViewModelFactory"/> と同じ方式）。ウィンドウは UI スレッドで生成すること。
    /// </summary>
    public interface IMainWindowFactory
    {
        /// <param name="parser">起動引数（<c>-m</c> などの最小化指定を <c>MainWindow</c> が読む）</param>
        MainWindow Create(ArgumentParser parser);
    }
}
