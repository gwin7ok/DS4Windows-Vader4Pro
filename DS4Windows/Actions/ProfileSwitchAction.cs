using System;
using DS4Windows.Services;
using DS4WinWPF;

namespace DS4Windows.Actions
{
    public class ProfileSwitchAction : IOutputAction
    {
        private readonly SpecialAction sa;
        private readonly int deviceIndex;
        private readonly IProfileSwitcher _profileSwitcher;

        public ProfileSwitchAction(SpecialAction sa, IProfileSwitcher profileSwitcher = null)
            : this(sa, -1, profileSwitcher)
        {
        }

        public ProfileSwitchAction(SpecialAction sa, int deviceIndex, IProfileSwitcher profileSwitcher = null)
        {
            this.sa = sa;
            this.deviceIndex = deviceIndex;
            this._profileSwitcher = profileSwitcher ?? AppHost.GetService<IProfileSwitcher>() ?? new DefaultProfileSwitcher();
        }

        public string Id => sa?.name ?? "ProfileSwitch";

        public void Execute(IOutputContext ctx)
        {
            try
            {
                if (sa == null) return;
                int dev = (deviceIndex >= 0) ? deviceIndex : (ctx?.Device ?? 0);
                _profileSwitcher.SwitchProfile(dev, sa);
                try { AppLogger.LogTrace($"ProfileSwitchAction.Execute: id={Id} device={dev}"); } catch { }
            }
            catch (Exception ex)
            {
                try { AppLogger.LogTrace($"ProfileSwitchAction.Execute failed: {ex}"); } catch { }
            }
        }

        public void Stop(IOutputContext ctx)
        {
        }
    }
}
