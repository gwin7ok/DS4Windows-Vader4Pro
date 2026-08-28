using System;
using DS4Windows.Services;
using DS4WinWPF;

namespace DS4Windows.Actions
{
    public class MacroAction : IOutputAction
    {
        public SpecialAction ActionDef { get; }
        public int DeviceIndex { get; }
        private readonly IMacroPlayer _macroPlayer;

        public MacroAction(SpecialAction sa) : this(sa, -1, null) { }

        public MacroAction(SpecialAction sa, int deviceIndex) : this(sa, deviceIndex, null) { }

        public MacroAction(SpecialAction sa, IMacroPlayer macroPlayer) : this(sa, -1, macroPlayer) { }

        public MacroAction(SpecialAction sa, int deviceIndex, IMacroPlayer macroPlayer)
        {
            this.ActionDef = sa;
            this.DeviceIndex = deviceIndex;
            this._macroPlayer = macroPlayer ?? AppHost.GetService<IMacroPlayer>() ?? new DefaultMacroPlayer();
        }

        public string Id => ActionDef?.name ?? "MacroAction";

        public void Execute(IOutputContext ctx)
        {
            if (ActionDef == null || _macroPlayer == null) return;
            int dev = (DeviceIndex >= 0) ? DeviceIndex : (ctx != null ? ctx.Device : 0);
            _macroPlayer.Play(dev, ActionDef);
            try { AppLogger.LogTrace($"MacroAction.Execute: id={Id} device={dev}"); } catch { }
        }

        public void Stop(IOutputContext ctx)
        {
            if (ActionDef == null || _macroPlayer == null) return;
            int dev = (DeviceIndex >= 0) ? DeviceIndex : (ctx != null ? ctx.Device : 0);
            _macroPlayer.Stop(dev);
            try { AppLogger.LogTrace($"MacroAction.Stop: id={Id} device={dev}"); } catch { }
        }
    }
}