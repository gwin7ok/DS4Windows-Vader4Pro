using System;
using DS4Windows.DS4Control;

namespace DS4Windows.Actions
{
    public class KeyOutputAction : IOutputAction
    {
        private readonly SpecialAction sa;

        public KeyOutputAction(SpecialAction sa)
        {
            this.sa = sa;
        }

        public string Id => sa?.name ?? "KeyOutput";

        public void Execute(IOutputContext ctx)
        {
            try
            {
                if (sa == null) return;
                ushort logical = 0;
                if (!string.IsNullOrEmpty(sa.details)) ushort.TryParse(sa.details, out logical);
                uint native = 0;
                try { native = SyntheticDispatcher.ResolveNativeKey(logical); } catch { native = 0; }

                var trigger = new TriggerContextImpl
                {
                    Device = ctx.Device,
                    IsEdgeEstablished = true,
                    LogicalValue = logical,
                    NativeValue = native,
                    OutputHandler = ctx.OutputHandler,
                    Timestamp = DateTime.UtcNow
                };

                var binding = new KeyActionBinding(sa);
                var ctrl = ActionManager.GetOrCreateControllerForAction(ctx.Device, sa);
                try { ctrl?.Handle(binding, trigger); } catch { }
            }
            catch { }
        }

        public void Stop(IOutputContext ctx)
        {
            try
            {
                if (sa == null) return;
                ushort logical = 0;
                if (!string.IsNullOrEmpty(sa.details)) ushort.TryParse(sa.details, out logical);
                uint native = 0;
                try { native = SyntheticDispatcher.ResolveNativeKey(logical); } catch { native = 0; }

                var trigger = new TriggerContextImpl
                {
                    Device = ctx.Device,
                    IsEdgeEstablished = false,
                    LogicalValue = logical,
                    NativeValue = native,
                    OutputHandler = ctx.OutputHandler,
                    Timestamp = DateTime.UtcNow
                };

                var binding = new KeyActionBinding(sa);
                var ctrl = ActionManager.GetOrCreateControllerForAction(ctx.Device, sa);
                try { ctrl?.Handle(binding, trigger); } catch { }
            }
            catch { }
        }
    }
}
