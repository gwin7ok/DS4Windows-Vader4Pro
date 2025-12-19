using System;
using DS4Windows.DS4Control;

namespace DS4Windows.Actions
{
    /// <summary>
    /// Adapter that wraps existing <see cref="RepeatHelper"/> instances
    /// and exposes the <see cref="IRepeater"/> interface.
    /// Note: <see cref="RepeatHelper"/> encapsulates SyntheticDispatcher-based
    /// key sends and does not accept an external tick Action. If an external
    /// tick action is supplied to Start(...) it will be ignored and a trace
    /// will be emitted.
    /// </summary>
    public class RepeatHelperToIRepeaterAdapter : IRepeater
    {
        private RepeatHelper helper;
        private IRepeater helperIRepeater;
        private readonly Func<RepeatHelper> helperFactory;

        // Test hook: if non-null, invoked to create an IRepeater in place of the underlying RepeatHelper.
        // Signature: (originalHelperFactory) => IRepeater
        public static Func<Func<RepeatHelper>, IRepeater> RepeaterFactoryOverride = null;

        public RepeatHelperToIRepeaterAdapter(RepeatHelper existing)
        {
            helper = existing ?? throw new ArgumentNullException(nameof(existing));
        }

        public RepeatHelperToIRepeaterAdapter(Func<RepeatHelper> factory)
        {
            helperFactory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        public void Start(TimeSpan initialDelay, TimeSpan interval, System.Action tickAction)
        {
            try
            {
                if (tickAction != null)
                {
                    try { DS4Windows.AppLogger.LogTrace("RepeatHelperToIRepeaterAdapter: supplied tickAction will be ignored; using RepeatHelper behavior"); } catch { }
                }

                // If tests provided an override factory, prefer creating an IRepeater wrapper
                if (RepeaterFactoryOverride != null)
                {
                    try
                    {
                        helperIRepeater ??= RepeaterFactoryOverride(helperFactory);
                        helperIRepeater?.Start(initialDelay, interval, tickAction);
                        return;
                    }
                    catch { }
                }

                if (helper == null && helperFactory != null)
                {
                    helper = helperFactory();
                }

                helper?.Start();
            }
            catch { }
        }

        public void Stop()
        {
            try
            {
                if (helperIRepeater != null) try { helperIRepeater.Stop(); } catch { }
                else try { helper?.Stop(); } catch { }
            }
            catch { }
        }

        public void Dispose()
        {
            try
            {
                try { helperIRepeater?.Dispose(); } catch { }
                try { helper?.Dispose(); } catch { }
                helperIRepeater = null;
                helper = null;
            }
            catch { }
        }
    }
}
