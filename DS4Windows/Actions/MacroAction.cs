using System;
using DS4Windows.Services;
using DS4WinWPF;

namespace DS4Windows.Actions
{
    public class MacroAction : IOutputAction
    {
        public SpecialAction sa { get; }
        public int deviceIndex { get; }
        public IMacroPlayer macroPlayer { get; }

        public MacroAction(SpecialAction sa) : this(sa, -1, null) { }

        public MacroAction(SpecialAction sa, int deviceIndex) : this(sa, deviceIndex, null) { }

        public MacroAction(SpecialAction sa, IMacroPlayer macroPlayer) : this(sa, -1, macroPlayer) { }

        public MacroAction(SpecialAction sa, int deviceIndex, IMacroPlayer macroPlayer)
        {
            this.sa = sa;
            this.deviceIndex = deviceIndex;
            this.macroPlayer = macroPlayer ?? AppHost.GetService<IMacroPlayer>() ?? new DefaultMacroPlayer();
        }

        public string Id => sa?.name ?? "MacroAction";

        public void Execute(IOutputContext ctx)
        {
            if (sa == null) return;
            var player = macroPlayer ?? AppHost.GetService<IMacroPlayer>() ?? new DefaultMacroPlayer();
            int dev = (deviceIndex >= 0) ? deviceIndex : (ctx != null ? ctx.Device : 0);
            player.Play(dev, sa);
        }

        public void Stop(IOutputContext ctx)
        {
            if (sa == null) return;
            var player = macroPlayer ?? AppHost.GetService<IMacroPlayer>() ?? new DefaultMacroPlayer();
            int dev = (deviceIndex >= 0) ? deviceIndex : (ctx != null ? ctx.Device : 0);
            player.Stop(dev);
        }
    }
}