using System;

using DS4Windows;
namespace DS4Windows.Actions
{
    public static class ActionFactory
    {
        // Backwards-compatible static factory. If DI Provider is available, delegate to registered IActionFactory.
        public static Action CreateFrom(SpecialAction sa, int index)
        {
            if (sa == null) return null;

            try
            {
                var sp = DS4Windows.DI.ServiceProviderHolder.Provider;
                if (sp != null)
                {
                    try
                    {
                        var impl = sp.GetService(typeof(DS4Windows.Actions.IActionFactory)) as DS4Windows.Actions.IActionFactory;
                        if (impl != null) return impl.CreateFrom(sa, index);
                    }
                    catch { }
                }
            }
            catch { }

            // Fallback to minimal static behavior（DI ホストが無い場合のみ。テスト等に限られる）
            // Phase6-Step10-4-4: マクロ再生役・プログラム起動役・プロファイル切替役は、DI の部品が必要なため、
            // ホストが無い場合は作らない（null）。DI を要しないキー押下（Key 型）のみ返す。
            // TODO(Phase7/Phase8): 静的な ActionFactory 自体を、DefaultActionFactory へ統合して廃止する（D4-4）。
            switch (sa.typeID)
            {
                case SpecialAction.ActionTypeId.Key:
                    return new KeyActionAdapter(sa, index);
                default:
                    return null;
            }
        }
    }
}
