using System;
using DS4Windows.Services;

namespace DS4Windows.Actions
{
    public class MacroActionAdapter : SpecialActionBase, IOutputAction
    {
        private readonly MacroAction _inner;

        public MacroActionAdapter(SpecialAction sa, int index) : base(sa, index)
        {
            _inner = new MacroAction(sa, index);
        }

        public MacroActionAdapter(SpecialAction sa, IMacroPlayer macroPlayer) : base(sa, -1)
        {
            _inner = new MacroAction(sa, -1, macroPlayer);
        }

        public MacroActionAdapter(SpecialAction sa, int index, IMacroPlayer macroPlayer) : base(sa, index)
        {
            _inner = new MacroAction(sa, index, macroPlayer);
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