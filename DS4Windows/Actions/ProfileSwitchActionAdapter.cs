using System;
using DS4Windows.Services;

namespace DS4Windows.Actions
{
    public class ProfileSwitchActionAdapter : IOutputAction
    {
        private readonly ProfileSwitchAction _inner;

        public ProfileSwitchActionAdapter(SpecialAction sa, int deviceIndex = -1, IProfileSwitcher profileSwitcher = null)
        {
            _inner = new ProfileSwitchAction(sa, deviceIndex, profileSwitcher);
        }

        public ProfileSwitchActionAdapter(SpecialAction sa, IProfileSwitcher profileSwitcher)
            : this(sa, -1, profileSwitcher)
        {
        }

        public string Id => _inner.Id;

        public void Execute(IOutputContext ctx)
        {
            _inner.Execute(ctx);
        }

        public void Stop(IOutputContext ctx)
        {
            _inner.Stop(ctx);
        }
    }
}