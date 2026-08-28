using System;
using DS4Windows.Services;
using DS4WinWPF;

namespace DS4Windows.Actions
{
    public class MacroAction : IOutputAction
    {
        private readonly SpecialAction _sa;
        private readonly int _deviceIndex;
        private readonly IMacroPlayer _macroPlayer;

        public MacroAction(SpecialAction sa) : this(sa, -1, null) { }

        public MacroAction(SpecialAction sa, int deviceIndex) : this(sa, deviceIndex, null) { }

        public MacroAction(SpecialAction sa, IMacroPlayer macroPlayer) : this(sa, -1, macroPlayer) { }

        public MacroAction(SpecialAction sa, int deviceIndex, IMacroPlayer macroPlayer)
        {
            _sa = sa;
            _deviceIndex = deviceIndex;
            _macroPlayer = macroPlayer ?? AppHost.GetService<IMacroPlayer>() ?? new DefaultMacroPlayer();
        }

        public string Id => _sa?.name ?? "MacroAction";

        public void Execute(IOutputContext ctx)
        {
            if (_sa == null || _macroPlayer == null) return;
            int dev = (_deviceIndex >= 0) ? _deviceIndex : (ctx != null ? ctx.Device : 0);
            _macroPlayer.Play(dev, _sa);
            try { AppLogger.LogTrace($"MacroAction.Execute: id={Id} device={dev}"); } catch { }
        }

        public void Stop(IOutputContext ctx)
        {
            if (_sa == null || _macroPlayer == null) return;
            int dev = (_deviceIndex >= 0) ? _deviceIndex : (ctx != null ? ctx.Device : 0);
            _macroPlayer.Stop(dev);
            try { AppLogger.LogTrace($"MacroAction.Stop: id={Id} device={dev}"); } catch { }
        }
    }
}