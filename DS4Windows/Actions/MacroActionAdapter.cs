using System;
using DS4Windows.Services;

namespace DS4Windows.Actions
{
    public class MacroActionAdapter : IOutputAction
    {
        private readonly MacroAction _inner;

        public MacroActionAdapter(SpecialAction sa, int deviceIndex = -1, IMacroPlayer macroPlayer = null)
        {
            _inner = new MacroAction(sa, deviceIndex, macroPlayer);
        }

        public MacroActionAdapter(SpecialAction sa, IMacroPlayer macroPlayer)
            : this(sa, -1, macroPlayer)
        {
        }

        public string Id => _inner.Id;

        public void Execute(IOutputContext ctx)
        {
            _inner.Execute(ctx);
        }

        public void Stop(IOutputContext ctx)
        {
            _inner.Stop(ctx);
        }
    }
}