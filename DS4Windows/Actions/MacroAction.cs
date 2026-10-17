using System;
using DS4Windows.Services;

namespace DS4Windows.Actions
{
    public class MacroAction : IOutputAction
    {
        public SpecialAction sa { get; }
        public int deviceIndex { get; }
        public IMacroPlayer macroPlayer { get; }

        // Phase6-Step10-4-4: マクロ再生役は必須の依存（AppHost／new DefaultMacroPlayer のフォールバックを廃止）
        public MacroAction(SpecialAction sa, IMacroPlayer macroPlayer) : this(sa, -1, macroPlayer) { }

        public MacroAction(SpecialAction sa, int deviceIndex, IMacroPlayer macroPlayer)
        {
            this.sa = sa;
            this.deviceIndex = deviceIndex;
            this.macroPlayer = macroPlayer ?? throw new ArgumentNullException(nameof(macroPlayer));
        }

        public string Id => sa?.name ?? "MacroAction";

        public void Execute(IOutputContext ctx)
        {
            if (sa == null) return;
            int dev = (deviceIndex >= 0) ? deviceIndex : (ctx != null ? ctx.Device : 0);
            macroPlayer.Play(dev, sa);
        }

        public void Stop(IOutputContext ctx)
        {
            if (sa == null) return;
            int dev = (deviceIndex >= 0) ? deviceIndex : (ctx != null ? ctx.Device : 0);
            macroPlayer.Stop(dev);
        }
    }
}
