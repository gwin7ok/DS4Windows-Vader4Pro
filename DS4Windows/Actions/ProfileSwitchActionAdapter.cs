using System;
using DS4Windows.Services;

namespace DS4Windows.Actions
{
    public class ProfileSwitchActionAdapter : SpecialActionBase, IOutputAction
    {
        private readonly ProfileSwitchAction _inner;

        public ProfileSwitchActionAdapter(SpecialAction sa, int index) : base(sa, index)
        {
            _inner = new ProfileSwitchAction(sa, index);
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