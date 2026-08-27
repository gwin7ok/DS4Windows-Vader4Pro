using System;
using DS4Windows.Services;

namespace DS4Windows.Actions
{
    public class MacroAction : IOutputAction
    {
        private readonly SpecialAction sa;
        private readonly IMacroPlayer _macroPlayer;

        public MacroAction(SpecialAction sa, IMacroPlayer macroPlayer = null)
        {
            this.sa = sa;
            this._macroPlayer = macroPlayer ?? new DefaultMacroPlayer();
        }

        public string Id => sa?.name ?? "MacroAction";

        public void Execute(IOutputContext ctx)
        {
            try
            {
                if (sa == null) return;
                int device = ctx?.Device ?? 0;
                _macroPlayer.Play(device, sa);
                try { AppLogger.LogTrace($"MacroAction.Execute: id={Id} device={device}"); } catch { }
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
                int device = ctx?.Device ?? 0;
                _macroPlayer.Stop(device);
                try { AppLogger.LogTrace($"MacroAction.Stop: id={Id} device={device}"); } catch { }
            }
            catch (Exception ex)
            {
                try { AppLogger.LogTrace($"MacroAction.Stop failed: {ex}"); } catch { }
            }
        }
    }
}
