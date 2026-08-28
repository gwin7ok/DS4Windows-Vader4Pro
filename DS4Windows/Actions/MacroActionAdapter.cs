using System;
using DS4Windows.Services;

namespace DS4Windows.Actions
{
    public class MacroActionAdapter : Action, IOutputAction
    {
        private readonly MacroAction _inner;

        public MacroActionAdapter(SpecialAction sa, int deviceIndex = -1, IMacroPlayer macroPlayer = null)
        {
            this.action = sa;
            _inner = new MacroAction(sa, deviceIndex, macroPlayer);
        }

        public MacroActionAdapter(SpecialAction sa, IMacroPlayer macroPlayer)
            : this(sa, -1, macroPlayer)
        {
        }

        public string Id => _inner.Id;

        public override void OnTrigger(int device, MappingContext ctx)
        {
            _inner.Execute(new OutputContextImpl(device, ctx?.OutputHandler));
        }

        public override void OnRelease(int device, MappingContext ctx)
        {
            _inner.Stop(new OutputContextImpl(device, ctx?.OutputHandler));
        }

        public void Execute(IOutputContext ctx) => _inner.Execute(ctx);
        public void Stop(IOutputContext ctx) => _inner.Stop(ctx);
    }
}