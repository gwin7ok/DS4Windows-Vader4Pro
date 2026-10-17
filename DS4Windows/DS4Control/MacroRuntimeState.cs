using System.Collections.Generic;
using System.Threading.Tasks;

namespace DS4Windows
{
    /// <summary>
    /// Phase7-Step7 W3（Q2＝A）: マクロの実行時状態の保持クラス（旧: <c>Mapping</c> の静的フィールド
    /// <c>macrodone</c>／<c>macroDispatchInFlight</c>／<c>macroDispatchLock</c>／<c>macroControl</c>／<c>macroCount</c>／<c>macroTaskQueue</c>）。
    /// <c>Mapping</c>（<c>PlayMacro</c> ほか）と <c>ActionsLoadedRuntimeReset</c> が使う。
    /// 配列の参照は再代入しない（要素の読み書きのみ）。要素の読み書きにロックはない（従来どおり。<c>DispatchInFlight</c> だけは
    /// <c>DispatchLock</c> で保護される）。マクロは別スレッドの <c>Task</c> から更新されるため、構造を変えずに移しただけである。
    /// </summary>
    public class MacroRuntimeState
    {
        /// <summary>DS4 のコントロールを添字にする配列の長さ（旧: <c>Mapping.DS4_CONTROL_MACRO_ARRAY_LEN</c>）。</summary>
        public const int ControlArrayLength = 52;

        /// <summary>マクロが送出するボタン／スティック／トリガーの出力フラグの数（旧: <c>macroControl</c> の長さ）。</summary>
        public const int MacroControlLength = 26;

        /// <summary>コントロールごとの「マクロの実行を済ませた」フラグ（旧: <c>macrodone</c>）。</summary>
        public bool[] MacroDone { get; } = new bool[ControlArrayLength];

        // TODO(Phase8、K3): PlayMacro の二重実行防止の暫定対策（旧: macroDispatchInFlight／macroDispatchLock）。
        // Phase7-Step9 W3（Q3＝A）で、ロックと配列の直接操作を TryBeginDispatch／EndDispatch に包んだ（挙動は同一）。
        // 恒久対応は、Mapping.cs に書かれていた申し送りのとおり、トリガー判定層とマクロ実行層の分離（Controls／SpecialActions に
        // 共通の単一の実行層への一本化。モデル図 02／03 の 2-d マクロの分解）とあわせて、Phase8（Actions サブシステム）で再設計する。
        // 配列はテストが読み取るために公開（要素の更新は TryBeginDispatch／EndDispatch を使うこと）。
        public bool[] DispatchInFlight { get; } = new bool[ControlArrayLength];

        private readonly object _dispatchLock = new object();

        /// <summary>
        /// control 単位の「マクロ実行中」フラグを確保する。既に実行中なら false（フラグは変えない）、未実行なら true にして true を返す。
        /// 添字が範囲外のときは、旧実装と同じく例外を呼び出し元へ伝える（<c>Mapping.PlayMacro</c> が握りつぶす）。
        /// </summary>
        public bool TryBeginDispatch(int controlIndex)
        {
            lock (_dispatchLock)
            {
                if (DispatchInFlight[controlIndex])
                    return false;
                DispatchInFlight[controlIndex] = true;
                return true;
            }
        }

        /// <summary>control 単位の「マクロ実行中」フラグを解除する（マクロ実行の終了時に必ず呼ぶ）。</summary>
        public void EndDispatch(int controlIndex)
        {
            lock (_dispatchLock) { DispatchInFlight[controlIndex] = false; }
        }

        /// <summary>マクロが出力中のボタン等のフラグ（旧: <c>macroControl</c>）。</summary>
        public bool[] MacroControl { get; } = new bool[MacroControlLength];

        /// <summary>出力中のマクロのキー数（旧: <c>macroCount</c>）。0 より大きい間、<c>MapCustom</c> が <c>MacroControl</c> を出力へ重ねる。</summary>
        public uint MacroCount { get; set; }

        /// <summary>デバイスごと、特殊アクション名ごとの、マクロの実行 Task の連鎖（旧: <c>macroTaskQueue</c>）。</summary>
        public Dictionary<string, Task>[] TaskQueue { get; }

        public MacroRuntimeState()
        {
            TaskQueue = new Dictionary<string, Task>[ControllerLimits.MaxControllerCount];
            for (int i = 0; i < TaskQueue.Length; i++) TaskQueue[i] = new Dictionary<string, Task>();
        }

        /// <summary>「マクロの実行を済ませた」フラグをすべて解除する（Actions.xml の読み込み後のリセット用）。</summary>
        public void ClearMacroDone()
        {
            for (int i = 0; i < MacroDone.Length; i++) MacroDone[i] = false;
        }
    }
}
