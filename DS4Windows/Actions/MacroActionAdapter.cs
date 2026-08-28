using System;
using DS4Windows.Services;

namespace DS4Windows.Actions
{
    public class MacroActionAdapter : SpecialActionBase, IOutputAction
    {
        private readonly MacroAction _inner;

        public MacroActionAdapter(SpecialAction sa, int index) : base(sa, index)
        {
            _inner = new MacroAction(sa, index, null);
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
            try
            {
                if (_inner == null) return;
                var outCtx = new OutputContextImpl(device, ctx?.OutputHandler);
                _inner.Execute(outCtx);
            }
            catch { }
        }

        public override void OnRelease(int device, MappingContext ctx)
        {
            try
            {
                if (_inner == null) return;
                var outCtx = new OutputContextImpl(device, ctx?.OutputHandler);
                _inner.Stop(outCtx);
            }
            catch { }
        }

        public void Execute(IOutputContext ctx) => _inner.Execute(ctx);
        public void Stop(IOutputContext ctx) => _inner.Stop(ctx);
    }
}