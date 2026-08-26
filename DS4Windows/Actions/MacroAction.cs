using System;
using DS4Windows;
using DS4Windows.DI;

namespace DS4Windows.Actions
{
    /// <summary>
    /// マクロ再生アクション（IOutputAction 実装）
    /// DI（IMacroPlayer）経由で再生/停止を実行し、未解決時は Mapping.PlayMacroDirect / EndMacroDirect へフォールバック
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

            int device = ctx?.DeviceIndex ?? 0;
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

        public void Stop(IOutputContext ctx)
        {
            int device = ctx?.DeviceIndex ?? 0;
            bool stoppedViaDI = false;

            var sp = ServiceProviderHolder.Provider;
            if (sp != null)
            {
                var player = sp.GetService(typeof(IMacroPlayer)) as IMacroPlayer;
                if (player != null)
                {
                    player.Stop(device);
                    stoppedViaDI = true;
                }
            }

            if (!stoppedViaDI)
            {
                Mapping.EndMacroDirect(device);
            }
        }
    }
}