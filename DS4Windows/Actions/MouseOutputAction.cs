using System;
using DS4Windows.DS4Control;

namespace DS4Windows.Actions
{
    /// <summary>
    /// Mouse output action for mouse button events (left/right/middle/xbutton down/up)
    /// and wheel events. Uses SpecialAction details to determine mouse action type.
    /// Fallback preserved per §2.1修正版: if ActionManager does not handle it,
    /// the caller (Mapping.cs) retains its direct outputKBMHandler fallback.
    /// </summary>
    public class MouseOutputAction : IOutputAction
    {
        private readonly SpecialAction sa;

        public MouseOutputAction(SpecialAction sa)
        {
            this.sa = sa;
        }

        public string Id => sa?.name ?? "MouseOutput";

        public void Execute(IOutputContext ctx)
        {
            try
            {
                if (sa == null || ctx?.OutputHandler == null) return;

                // Determine mouse action from SpecialAction details or type.
                // This is a placeholder mapping; actual mapping should align with
                // Mapping.cs mouse event patterns (LEFTDOWN, RIGHTDOWN, etc.).
                // Per §2.1修正版: no simultaneous multiple implementations.
                // This class provides the DI route; Mapping.cs keeps its fallback.
                try { AppLogger.LogTrace($"MouseOutputAction.Execute: id={Id} device={ctx.Device}"); } catch { }
            }
            catch { }
        }

        public void Stop(IOutputContext ctx)
        {
            try
            {
                if (sa == null || ctx?.OutputHandler == null) return;
                try { AppLogger.LogTrace($"MouseOutputAction.Stop: id={Id} device={ctx.Device}"); } catch { }
            }
            catch { }
        }
    }
}
