using System;
using DS4Windows.Services;
using DS4WinWPF;

namespace DS4Windows.Actions
{
    public class MacroAction : IOutputAction
    {
        private readonly SpecialAction sa;
        private readonly int deviceIndex;
        private readonly IMacroPlayer _macroPlayer;

        public MacroAction(SpecialAction sa)
            : this(sa, -1, null)
        {
        }

        public MacroAction(SpecialAction sa, int deviceIndex)
            : this(sa, deviceIndex, null)
        {
        }

        public MacroAction(SpecialAction sa, IMacroPlayer macroPlayer)
            : this(sa, -1, macroPlayer)
        {
        }

        public MacroAction(SpecialAction sa, int deviceIndex, IMacroPlayer macroPlayer)
        {
            this.sa = sa;
            this.deviceIndex = deviceIndex;
            this._macroPlayer = macroPlayer ?? AppHost.GetService<IMacroPlayer>() ?? new DefaultMacroPlayer();
        }

        public string Id => sa?.name ?? "MacroAction";

        public void Execute(IOutputContext ctx)
        {
            try
            {
                if (sa == null) return;
                int dev = (ctx != null && ctx.Device >= 0) ? ctx.Device : (deviceIndex >= 0 ? deviceIndex : 0);
                _macroPlayer.Play(dev, sa);
                try { AppLogger.LogTrace($"MacroAction.Execute: id={Id} device={dev}"); } catch { }
            }
            catch (Exception ex)
            {
                try { AppLogger.LogTrace($"MacroAction.Execute failed: {ex}"); } catch { }
            }
        }

        public void Stop(IOutputContext ctx)
        {
            try
            {
                if (sa == null) return;
                int dev = (ctx != null && ctx.Device >= 0) ? ctx.Device : (deviceIndex >= 0 ? deviceIndex : 0);
                _macroPlayer.Stop(dev);
                try { AppLogger.LogTrace($"MacroAction.Stop: id={Id} device={dev}"); } catch { }
            }
            catch (Exception ex)
            {
                try { AppLogger.LogTrace($"MacroAction.Stop failed: {ex}"); } catch { }
            }
        }
    }
}