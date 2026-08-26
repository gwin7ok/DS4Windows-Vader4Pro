using System;
using DS4Windows;
using DS4Windows.DI;

namespace DS4Windows.Actions
{
    /// <summary>
    /// プロファイル切り替えアクション（IOutputAction 実装）
    /// DI（IProfileSwitcher）経由で切り替え/復帰を実行し、未解決時は Mapping.ApplyProfileDirect / RestoreProfileDirect へフォールバック
    /// </summary>
    public class ProfileSwitchAction : IOutputAction
    {
        private readonly SpecialAction sa;
        private readonly int deviceIndex;

        public ProfileSwitchAction(SpecialAction sa, int deviceIndex = 0)
        {
            this.sa = sa;
            this.deviceIndex = deviceIndex;
        }

        public string Id => sa?.name ?? "ProfileSwitch";
        public SpecialAction SpecialAction => sa;
        public int DeviceIndex => deviceIndex;

        public void Execute(IOutputContext ctx)
        {
            if (sa == null) return;

            int dev = deviceIndex;
            bool executedViaDI = false;

            // DI コンテナからの IProfileSwitcher 解決試行
            var sp = ServiceProviderHolder.Provider;
            if (sp != null)
            {
                var switcher = sp.GetService(typeof(IProfileSwitcher)) as IProfileSwitcher;
                if (switcher != null)
                {
                    switcher.SwitchProfile(dev, sa);
                    executedViaDI = true;
                }
            }

            // フォールバック: DI未登録時は従来の直接呼び出し
            if (!executedViaDI)
            {
                Mapping.ApplyProfileDirect(dev, sa);
            }
        }

        public void Stop(IOutputContext ctx)
        {
            int dev = deviceIndex;
            bool restoredViaDI = false;

            var sp = ServiceProviderHolder.Provider;
            if (sp != null)
            {
                var switcher = sp.GetService(typeof(IProfileSwitcher)) as IProfileSwitcher;
                if (switcher != null)
                {
                    switcher.RestoreProfile(dev);
                    restoredViaDI = true;
                }
            }

            if (!restoredViaDI)
            {
                Mapping.RestoreProfileDirect(dev);
            }
        }
    }
}