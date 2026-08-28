using System;
using DS4Windows.Services;
using DS4WinWPF;

namespace DS4Windows.Actions
{
    public class ProfileSwitchAction : IOutputAction
    {
        public SpecialAction ActionDef { get; }
        public int DeviceIndex { get; }
        private readonly IProfileSwitcher _profileSwitcher;

        public ProfileSwitchAction(SpecialAction sa) : this(sa, -1, null) { }

        public ProfileSwitchAction(SpecialAction sa, int deviceIndex) : this(sa, deviceIndex, null) { }

        public ProfileSwitchAction(SpecialAction sa, IProfileSwitcher profileSwitcher) : this(sa, -1, profileSwitcher) { }

        public ProfileSwitchAction(SpecialAction sa, int deviceIndex, IProfileSwitcher profileSwitcher)
        {
            this.ActionDef = sa;
            this.DeviceIndex = deviceIndex;
            this._profileSwitcher = profileSwitcher ?? AppHost.GetService<IProfileSwitcher>() ?? new DefaultProfileSwitcher();
        }

        public string Id => ActionDef?.name ?? "ProfileSwitch";

        public void Execute(IOutputContext ctx)
        {
            if (ActionDef == null || _profileSwitcher == null) return;
            int dev = (DeviceIndex >= 0) ? DeviceIndex : (ctx != null ? ctx.Device : 0);
            _profileSwitcher.SwitchProfile(dev, ActionDef);
        }

        public void Stop(IOutputContext ctx)
        {
            if (ActionDef == null || _profileSwitcher == null) return;
            int dev = (DeviceIndex >= 0) ? DeviceIndex : (ctx != null ? ctx.Device : 0);
            _profileSwitcher.RestoreProfile(dev);
        }
    }
}