using DS4Windows;

namespace DS4Windows.DI
{
    public interface IProfileApplicationService
    {
        void ApplyFromAction(int deviceIndex, SpecialAction action);
        bool RestoreFromAction(int deviceIndex);

        /// <summary>
        /// 指定されたスロットに対してプロファイルを適用します（Halt保護を内包）。
        /// </summary>
        /// <param name="deviceIndex">デバイスインデックス（0〜3）</param>
        /// <param name="profileName">適用するプロファイル名</param>
        /// <param name="isTemp">一時プロファイルフラグ（既定: false）</param>
        /// <param name="launchProgram">関連プログラムを起動するか（既定: false）</param>
        /// <param name="source">プロファイル切替要因（既定: Manual）</param>
        /// <param name="prolog">通知ログ等の前置テキスト（既定: null）</param>
        /// <returns>適用の成否（true: 成功, false: 失敗）</returns>
        bool ApplyProfile(int deviceIndex, string profileName, bool isTemp = false, bool launchProgram = false,
                    ProfileChangeSource source = ProfileChangeSource.Manual,
                    string prolog = null);

        /// <summary>
        /// 入力の読み取りを一時停止（Halt）せずにプロファイルを適用します（接続処理中など、呼び出し元がすでに停止している経路用。
        /// Phase7-Step3c W3 で追加。旧 <c>Global.ApplyProfileToSlot</c> と同じく例外は握りません）。
        /// </summary>
        bool ApplyProfileWithoutHalt(int deviceIndex, string profileName, bool isTemp = false, bool launchProgram = false,
                    ProfileChangeSource source = ProfileChangeSource.Manual,
                    string prolog = null);

        /// <summary>
        /// 切断時等に指定スロットの一時プロファイル復帰予約状態をクリアします（§5.6 ガードレール）。
        /// </summary>
        /// <param name="deviceIndex">デバイスインデックス（0〜3）</param>
        void ClearPendingRestore(int deviceIndex);
    }
}
