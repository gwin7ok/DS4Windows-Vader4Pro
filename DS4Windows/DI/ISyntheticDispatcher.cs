using DS4Windows.Services;

namespace DS4Windows.DI
{
    /// <summary>
    /// Phase7-Step8 W3（Q6＝A）: 合成キーの送出（押下・解放・タイミングのリセット）の窓口。
    /// 旧: 静的クラス <c>SyntheticDispatcher</c>（<c>SyntheticInputState.Shared</c>／<c>Global.outputKBMHandler</c>／<c>Global.outputKBMMapping</c> を参照）。
    /// 実装は <c>DS4Windows.Services.SyntheticDispatcherService</c>（DI の Singleton）。<c>Mapping</c> がコンストラクターで受ける。
    /// </summary>
    public interface ISyntheticDispatcher
    {
        /// <summary>論理キーを、現在の KBM マッピングに従った実キーへ変換する（マッピングが無いときは論理キーのまま）。</summary>
        uint ResolveNativeKey(ushort kvpKey);

        /// <summary>キーの押下を送り、合成状態の送出時刻・回数を更新する。出力先が無い（ハンドラ未決定）ときは何もしない。</summary>
        void SendPress(int device, ushort kvpKey, uint nativeKey, bool useScanCode, IVirtualKBM handler = null);

        /// <summary>キーの解放を送り、合成状態のカウントをクリアする。出力先が無い（ハンドラ未決定）ときは何もしない。</summary>
        void SendRelease(int device, ushort kvpKey, uint nativeKey, bool useScanCode, IVirtualKBM handler = null);

        /// <summary>キーの送出時刻と連打回数をリセットする。</summary>
        void ResetKeyTiming(int device, ushort kvpKey);
    }
}
