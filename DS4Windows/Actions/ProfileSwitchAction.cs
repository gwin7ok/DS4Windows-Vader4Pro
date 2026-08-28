using System;
using DS4Windows.Services;
using DS4WinWPF;

namespace DS4Windows.Actions
{
    public class ProfileSwitchAction : IOutputAction
    {
        private readonly SpecialAction _sa;
        private readonly int _deviceIndex;
        private readonly IProfileSwitcher _profileSwitcher;

        public ProfileSwitchAction(SpecialAction sa) : this(sa, -1, null) { }

        public ProfileSwitchAction(SpecialAction sa, int deviceIndex) : this(sa, deviceIndex, null) { }

        public ProfileSwitchAction(SpecialAction sa, IProfileSwitcher profileSwitcher) : this(sa, -1, profileSwitcher) { }

        public ProfileSwitchAction(SpecialAction sa, int deviceIndex, IProfileSwitcher profileSwitcher)
        {
            _sa = sa;
            _deviceIndex = deviceIndex;
            _profileSwitcher = profileSwitcher ?? AppHost.GetService<IProfileSwitcher>() ?? new DefaultProfileSwitcher();
        }

        public string Id => _sa?.name ?? "ProfileSwitch";

        public void Execute(IOutputContext ctx)
        {
            if (_sa == null || _profileSwitcher == null) return;
            int dev = (_deviceIndex >= 0) ? _deviceIndex : (ctx != null ? ctx.Device : 0);
            _profileSwitcher.SwitchProfile(dev, _sa);
            try { AppLogger.LogTrace($"ProfileSwitchAction.Execute: id={Id} device={dev}"); } catch { }
        }

        public void Stop(IOutputContext ctx)
        {
            if (_sa == null || _profileSwitcher == null) return;
            int dev = (_deviceIndex >= 0) ? _deviceIndex : (ctx != null ? ctx.Device : 0);
            _profileSwitcher.RestoreProfile(dev);
            try { AppLogger.LogTrace($"ProfileSwitchAction.Stop: id={Id} device={dev}"); } catch { }
        }
    }
}