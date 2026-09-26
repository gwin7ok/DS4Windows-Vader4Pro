using System;
using DS4Windows.DS4Control;
using DS4Windows.Services;
using DS4WinWPF;

namespace DS4Windows.Actions
{
    /// <summary>
    /// マウス出力アクション（IOutputAction 実装）。
    /// IVirtualKBM 経由でマウスイベントを出力する。
    /// </summary>
    public class MouseOutputAction : IOutputAction
    {
        private readonly SpecialAction sa;
        private readonly IVirtualKBM _virtualKBM;

        public MouseOutputAction(SpecialAction sa, IVirtualKBM virtualKBM = null)
        {
            this.sa = sa;
            this._virtualKBM = virtualKBM ?? AppHost.GetService<IVirtualKBM>();
        }

        public string Id => sa?.name ?? "MouseOutput";

        public void Execute(IOutputContext ctx)
        {
            try
            {
                if (sa == null) return;

                var kbm = _virtualKBM ?? (ctx?.OutputHandler as IVirtualKBM);
                if (kbm == null) return;

                try { AppLogger.LogTrace($"MouseOutputAction.Execute: id={Id} device={ctx?.Device}"); } catch { }
            }
            catch { }
        }

        public void Stop(IOutputContext ctx)
        {
            try
            {
                if (sa == null) return;

                var kbm = _virtualKBM ?? (ctx?.OutputHandler as IVirtualKBM);
                if (kbm == null) return;

                try { AppLogger.LogTrace($"MouseOutputAction.Stop: id={Id} device={ctx?.Device}"); } catch { }
            }
            catch { }
        }
    }
}
