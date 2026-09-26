using System;
using DS4Windows.Services;
using DS4WinWPF;

namespace DS4Windows.Actions
{
    public class ProfileSwitchAction : IOutputAction
    {
        public SpecialAction sa { get; }
        public int deviceIndex { get; }
        public IProfileSwitcher profileSwitcher { get; }

        public ProfileSwitchAction(SpecialAction sa) : this(sa, -1, null) { }

        public ProfileSwitchAction(SpecialAction sa, int deviceIndex) : this(sa, deviceIndex, null) { }

        public ProfileSwitchAction(SpecialAction sa, IProfileSwitcher profileSwitcher) : this(sa, -1, profileSwitcher) { }

        public ProfileSwitchAction(SpecialAction sa, int deviceIndex, IProfileSwitcher profileSwitcher)
        {
            this.sa = sa;
            this.deviceIndex = deviceIndex;
            this.profileSwitcher = profileSwitcher ?? AppHost.GetService<IProfileSwitcher>() ?? new DefaultProfileSwitcher();
        }

        public string Id => sa?.name ?? "ProfileSwitch";

        public void Execute(IOutputContext ctx)
        {
            if (sa == null) return;
            var switcher = profileSwitcher ?? AppHost.GetService<IProfileSwitcher>() ?? new DefaultProfileSwitcher();
            int dev = (deviceIndex >= 0) ? deviceIndex : (ctx != null ? ctx.Device : 0);
            switcher.SwitchProfile(dev, sa);
        }

        public void Stop(IOutputContext ctx)
        {
            if (sa == null) return;
            var switcher = profileSwitcher ?? AppHost.GetService<IProfileSwitcher>() ?? new DefaultProfileSwitcher();
            int dev = (deviceIndex >= 0) ? deviceIndex : (ctx != null ? ctx.Device : 0);
            switcher.RestoreProfile(dev);
        }
    }
}