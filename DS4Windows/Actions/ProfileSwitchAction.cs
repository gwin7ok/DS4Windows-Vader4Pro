using System;
using DS4Windows.Services;

namespace DS4Windows.Actions
{
    public class ProfileSwitchAction : IOutputAction
    {
        public SpecialAction sa { get; }
        public int deviceIndex { get; }
        public IProfileSwitcher profileSwitcher { get; }

        // Phase6-Step10-4-4: プロファイル切替役は必須の依存（AppHost／new DefaultProfileSwitcher のフォールバックを廃止）
        public ProfileSwitchAction(SpecialAction sa, IProfileSwitcher profileSwitcher) : this(sa, -1, profileSwitcher) { }

        public ProfileSwitchAction(SpecialAction sa, int deviceIndex, IProfileSwitcher profileSwitcher)
        {
            this.sa = sa;
            this.deviceIndex = deviceIndex;
            this.profileSwitcher = profileSwitcher ?? throw new ArgumentNullException(nameof(profileSwitcher));
        }

        public string Id => sa?.name ?? "ProfileSwitch";

        public void Execute(IOutputContext ctx)
        {
            if (sa == null) return;
            int dev = (deviceIndex >= 0) ? deviceIndex : (ctx != null ? ctx.Device : 0);
            profileSwitcher.SwitchProfile(dev, sa);
        }

        public void Stop(IOutputContext ctx)
        {
            if (sa == null) return;
            int dev = (deviceIndex >= 0) ? deviceIndex : (ctx != null ? ctx.Device : 0);
            profileSwitcher.RestoreProfile(dev);
        }
    }
}
