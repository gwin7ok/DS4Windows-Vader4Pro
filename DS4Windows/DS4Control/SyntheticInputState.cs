using System;
using System.Threading;

namespace DS4Windows
{
    /// <summary>
    /// Phase7-Step7 W2（Q2＝A）: 合成キー・マウス入力の状態の保持クラス（旧: <c>Mapping</c> の静的フィールド <c>globalState</c>／<c>deviceState</c>／<c>syncStateLock</c>）。
    /// <c>Mapping</c>（<c>Commit</c> ほか）、<c>SyntheticDispatcher</c>、<c>ActionsLoadedRuntimeReset</c> が使う。
    /// 配列の参照は再代入しない（要素の差し替えのみ。<c>GlobalState</c> は読み込み後のリセットで差し替える）。要素の読み書きにロックはない（従来どおり）。
    /// </summary>
    public class SyntheticInputState
    {
        public ReaderWriterLockSlim SyncStateLock { get; } = new ReaderWriterLockSlim();

        public SyntheticState GlobalState { get; set; } = new SyntheticState();

        public SyntheticState[] DeviceState { get; }

        public SyntheticInputState()
        {
            DeviceState = new SyntheticState[ControllerLimits.MaxControllerCount];
            for (int i = 0; i < DeviceState.Length; i++) DeviceState[i] = new SyntheticState();
        }

        /// <summary>
        /// Phase7-Step8 W1: 旧 <c>Mapping.MapClick</c>。このティックで押されたマウスボタンの回数を数える（<c>Commit</c> が前ティックとの差で押下／解放を送る）。
        /// 配列の要素を更新するだけで、割り当てはない。
        /// </summary>
        public void CountClick(int device, Click mouseClick)
        {
            switch (mouseClick)
            {
                case Click.Left:
                    DeviceState[device].currentClicks.leftCount++;
                    break;
                case Click.Middle:
                    DeviceState[device].currentClicks.middleCount++;
                    break;
                case Click.Right:
                    DeviceState[device].currentClicks.rightCount++;
                    break;
                case Click.Fourth:
                    DeviceState[device].currentClicks.fourthCount++;
                    break;
                case Click.Fifth:
                    DeviceState[device].currentClicks.fifthCount++;
                    break;
                case Click.WUP:
                    DeviceState[device].currentClicks.wUpCount++;
                    break;
                case Click.WDOWN:
                    DeviceState[device].currentClicks.wDownCount++;
                    break;
                default: break;
            }
        }

        /// <summary>
        /// Phase7-Step8 W1: 旧 <c>Mapping.ResetRuntimeStateForKey</c>。キー型スペシャルアクションの保存直後に、当該キーの合成キーの記録と
        /// トグル状態を全デバイスから消す（古い合成キューや押下フラグの残留を防ぐ）。ログは従来どおり。
        /// </summary>
        public void ResetRuntimeStateForKey(ushort key)
        {
            try
            {
                for (int d = 0; d < DeviceState.Length; d++)
                {
                    // keyPresses から該当キーを削除
                    if (DeviceState[d].keyPresses.ContainsKey(key))
                        DeviceState[d].keyPresses.Remove(key);

                    // native alias も削除
                    if (DeviceState[d].nativeKeyAlias.ContainsKey(key))
                        DeviceState[d].nativeKeyAlias.Remove(key);
                }

                // 押下済みフラグをクリア（ActionManager が持つ IsToggledOn）
                try { ActionManager.ClearToggledOnForKey(key); } catch { }

                AppLogger.LogToGui($"Runtime state for key {key} cleared on all devices.", false);
            }
            catch (Exception ex)
            {
                AppLogger.LogToGui($"Failed to reset runtime key state for {key}: {ex.Message}", true);
            }
        }

        /// <summary>全体と全デバイスの合成状態を新品へ戻す（Actions.xml の読み込み後のリセット用）。</summary>
        public void ResetAll()
        {
            GlobalState = new SyntheticState();
            for (int d = 0; d < DeviceState.Length; d++)
            {
                DeviceState[d] = new SyntheticState();
            }
        }
    }
}
