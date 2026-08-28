using System;
using DS4Windows.Services;

namespace DS4Windows.Actions
{
    public class ProfileSwitchActionAdapter : Action, IOutputAction
    {
        private readonly ProfileSwitchAction _inner;

        public ProfileSwitchActionAdapter(SpecialAction sa, int deviceIndex = -1, IProfileSwitcher profileSwitcher = null)
            : base(sa)
        {
            _inner = new ProfileSwitchAction(sa, deviceIndex, profileSwitcher);
        }

        public ProfileSwitchActionAdapter(SpecialAction sa, IProfileSwitcher profileSwitcher)
            : this(sa, -1, profileSwitcher)
        {
        }

        public string Id => _inner.Id;

        public override void OnTrigger(int device, MappingContext ctx)
        {
            _inner.Execute(ctx);
        }

        public override void OnRelease(int device, MappingContext ctx)
        {
            _inner.Stop(ctx);
        }

        public void Execute(IOutputContext ctx) => _inner.Execute(ctx);
        public void Stop(IOutputContext ctx) => _inner.Stop(ctx);
    }
}