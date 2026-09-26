using System.Threading;
using DS4Windows;

namespace DS4Windows.Actions
{
    /// <summary>
    /// マクロシーケンスの再生・停止・状態管理を抽象化するインターフェース
    /// </summary>
    public interface IMacroPlayer
    {
        /// <summary>
        /// 指定されたデバイスでマクロが再生中かどうかを取得します。
        /// </summary>
        /// <param name="deviceIndex">コントローラーのデバイスインデックス（0〜3）</param>
        /// <returns>再生中の場合は true</returns>
        bool IsPlaying(int deviceIndex);

        /// <summary>
        /// 指定されたデバイスで SpecialAction に定義されたマクロを再生します。
        /// </summary>
        /// <param name="deviceIndex">コントローラーのデバイスインデックス（0〜3）</param>
        /// <param name="action">マクロ定義を含む SpecialAction</param>
        /// <param name="cancellationToken">キャンセレーショントークン</param>
        void Play(int deviceIndex, SpecialAction action, CancellationToken cancellationToken = default);

        /// <summary>
        /// 指定されたデバイスで再生中のマクロを停止し、押下中キーを安全に解放します。
        /// </summary>
        /// <param name="deviceIndex">コントローラーのデバイスインデックス（0〜3）</param>
        void Stop(int deviceIndex);
    }
}