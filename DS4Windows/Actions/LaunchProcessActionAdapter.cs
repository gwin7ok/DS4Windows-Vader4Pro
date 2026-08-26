using System;

namespace DS4Windows.Actions
{
    /// <summary>
    /// LaunchProcessActionAdapter（C5 — Phase 1）
    /// IOutputAction 実装（LaunchProcessAction）を、ActionFactory / ActionManager が要求する
    /// SpecialActionBase（OnTrigger / OnRelease）契約へ橋渡しするアダプタ。
    /// KeyActionAdapter と同一パターン（既存の KeyAction を OnTrigger/OnRelease でラップする形）を踏襲。
    /// </summary>
    public class LaunchProcessActionAdapter : SpecialActionBase
    {
        private readonly LaunchProcessAction inner;

        public LaunchProcessActionAdapter(SpecialAction sa, int index) : base(sa, index)
        {
            inner = new LaunchProcessAction(sa);
        }

        public override void OnTrigger(int device, MappingContext ctx)
        {
            try
            {
                if (inner == null) return;
                var outCtx = new OutputContextImpl(device, ctx?.OutputHandler);
                inner.Execute(outCtx);
            }
            catch { }
        }

        public override void OnRelease(int device, MappingContext ctx)
        {
            try
            {
                if (inner == null) return;
                var outCtx = new OutputContextImpl(device, ctx?.OutputHandler);
                inner.Stop(outCtx);
            }
            catch { }
        }
    }
}
