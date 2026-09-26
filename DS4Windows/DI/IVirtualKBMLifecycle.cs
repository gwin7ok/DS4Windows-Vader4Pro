namespace DS4Windows.Services
{
    /// <summary>
    /// KBM 出力ハンドラの生成・破棄・フォールバック切替・マッピング初期化を担う（送出は <see cref="IVirtualKBM"/> の責務）。
    /// <see cref="IVirtualKBM"/> は Global.outputKBMHandler への読み取り専用委譲であり、ハンドラの生成・差し替えの口を持たないため、
    /// ライフサイクル操作を別インターフェースとして分離した（Phase6-Step2-Plan.md 決定D3）。
    /// </summary>
    public interface IVirtualKBMLifecycle
    {
        /// <summary>現行ハンドラが存在すれば Disconnect し、ハンドラを null にする。ハンドラが無い場合は何もしない。</summary>
        void ReleaseHandler();

        /// <summary>指定の識別子でハンドラを決定する（<c>Global.InitOutputKBMHandler</c> へ委譲）。</summary>
        void DetermineHandler(string identifier);

        /// <summary>ハンドラを既定のフォールバック（SendInput）へ差し替える。</summary>
        void SwitchToFallbackHandler();

        /// <summary>現行ハンドラの Version へ、FakerInput の導入バージョンを設定する。</summary>
        void ApplyFakerInputVersion();

        /// <summary>指定の識別子に対応する KBM マッピングを初期化する（<c>Global.InitOutputKBMMapping</c> へ委譲）。</summary>
        void InitializeMapping(string identifier);
    }
}