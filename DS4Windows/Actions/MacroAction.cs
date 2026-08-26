using System;
using DS4Windows;
using DS4Windows.DI;

namespace DS4Windows.Actions
{
    /// <summary>
    /// マクロ再生アクション（IOutputAction 実装）
    /// DI（IMacroPlayer）経由で再生を実行し、未解決時は Mapping.PlayMacroDirect へフォールバック
    /// </summary>
    public class MacroAction : IOutputAction
    {
        private readonly SpecialAction sa;

        public MacroAction(SpecialAction sa)
        {
            this.sa = sa;
        }

        public string Id => sa?.name ?? "Macro";
        public SpecialAction SpecialAction => sa;

        public void Execute(IOutputContext ctx)
        {
            if (sa == null) return;

            // コンテキストからデバイスインデックスを取得
            int device = 0;
            if (ctx is ActionContext actCtx)
            {
                device = actCtx.Device;
            }

            bool executedViaDI = false;

            // DI コンテナからの IMacroPlayer 解決試行
            var sp = ServiceProviderHolder.Provider;
            if (sp != null)
            {
                var player = sp.GetService(typeof(IMacroPlayer)) as IMacroPlayer;
                if (player != null)
                {
                    player.Play(device, sa);
                    executedViaDI = true;
                }
            }

            // フォールバック: DI未登録時は従来の直接呼び出し
            if (!executedViaDI)
            {
                Mapping.PlayMacroDirect(device, sa);
            }
        }
    }
}