using DS4Windows.DI;
using DS4Windows.Services;

namespace DS4Windows
{
    // TODO(Phase7-Step8 W3／Phase8 で削除): Actions サブシステム（KeyButtonActionController、KeyAction、KeyOutputAction、RepeatHelper）が
    // 静的に呼ぶための、暫定の静的な橋渡し。実体の処理は SyntheticDispatcherService（ISyntheticDispatcher。DI の Singleton）にあり、
    // ここは Composition Root が 1 度だけ束縛した instance（Bind。ServiceRegistration の登録と AppHost.CreateHost）へ委譲するだけである。
    // 旧: SyntheticInputState.Shared／Global.outputKBMHandler／Global.outputKBMMapping への直接参照とフォールバックを持っていた。
    // 本クラスは状態を持たず、フォールバックも持たない（未束縛のときは、明示的に渡されたハンドラへ送るだけで、合成状態は更新しない）。
    // Actions の利用者が生成の経路を注入に切り替えた時点（Phase8）で、本クラスごと削除する。
    public static class SyntheticDispatcher
    {
        private static volatile ISyntheticDispatcher bound;

        /// <summary>Composition Root が、DI が作った唯一の instance を束縛する（null で解除。テストの後始末用）。</summary>
        public static void Bind(ISyntheticDispatcher dispatcher)
        {
            bound = dispatcher;
        }

        public static uint ResolveNativeKey(ushort kvpKey)
        {
            var d = bound;
            return d != null ? d.ResolveNativeKey(kvpKey) : kvpKey;
        }

        public static void SendPress(int device, ushort kvpKey, uint nativeKey, bool useScanCode, IVirtualKBM handler = null)
        {
            var d = bound;
            if (d != null)
            {
                d.SendPress(device, kvpKey, nativeKey, useScanCode, handler);
                return;
            }

            if (handler == null) return;
            if (useScanCode) handler.PerformKeyPressAlt(nativeKey); else handler.PerformKeyPress(nativeKey);
        }

        public static void SendRelease(int device, ushort kvpKey, uint nativeKey, bool useScanCode, IVirtualKBM handler = null)
        {
            var d = bound;
            if (d != null)
            {
                d.SendRelease(device, kvpKey, nativeKey, useScanCode, handler);
                return;
            }

            if (handler == null) return;
            if (useScanCode) handler.PerformKeyReleaseAlt(nativeKey); else handler.PerformKeyRelease(nativeKey);
        }

        public static void ResetKeyTiming(int device, ushort kvpKey)
        {
            bound?.ResetKeyTiming(device, kvpKey);
        }
    }
}
