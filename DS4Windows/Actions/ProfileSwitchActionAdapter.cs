using System;
using DS4Windows.Services;

namespace DS4Windows.Actions
{
    public class ProfileSwitchActionAdapter : SpecialActionBase, IOutputAction
    {
        private readonly ProfileSwitchAction _inner;

        public ProfileSwitchActionAdapter(SpecialAction sa, int index) : base(sa, index)
        {
            _inner = new ProfileSwitchAction(sa, index, null);
        }

        public ProfileSwitchActionAdapter(SpecialAction sa, IProfileSwitcher profileSwitcher) : base(sa, -1)
        {
            _inner = new ProfileSwitchAction(sa, -1, profileSwitcher);
        }

        public ProfileSwitchActionAdapter(SpecialAction sa, int index, IProfileSwitcher profileSwitcher) : base(sa, index)
        {
            _inner = new ProfileSwitchAction(sa, index, profileSwitcher);
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