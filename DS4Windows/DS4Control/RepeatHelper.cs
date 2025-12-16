using System;
using System.Threading;
using System.Threading.Tasks;

namespace DS4Windows.DS4Control
{
    // RepeatHelper: creates an instance that immediately starts issuing KeyPress
    // events at a fixed interval. Call Stop() to stop repeating and send a single
    // KeyRelease; the instance should then be discarded.
    public class RepeatHelper : IDisposable
    {
        private readonly int device;
        private readonly ushort kvpKey;
        private readonly uint nativeKey;
        private readonly bool useScanCode;
        private readonly VirtualKBMBase handler;
        private readonly Timer timer;
        private int disposed;
        // public exposure of configured interval
        public int IntervalMs { get; }
        public bool SendFirstImmediate { get; }

        // intervalMillis: repeat period in milliseconds (50ms typical)
        // sendFirstImmediate: if true, send one immediate KeyPress now and then start periodic sends
        //                     if false, first KeyPress will be issued after intervalMillis
        public RepeatHelper(int device, ushort kvpKey, uint nativeKey, bool useScanCode, VirtualKBMBase handler, int intervalMillis = 50, bool sendFirstImmediate = true)
        {
            this.device = device;
            this.kvpKey = kvpKey;
            this.nativeKey = nativeKey;
            this.useScanCode = useScanCode;
            this.handler = handler;
            this.IntervalMs = intervalMillis;
            this.SendFirstImmediate = sendFirstImmediate;

            if (sendFirstImmediate)
            {
                try { SyntheticDispatcher.SendPress(device, kvpKey, nativeKey, useScanCode, handler); } catch { }
                // start periodic sends after intervalMillis
                timer = new Timer(TimerCallback, null, intervalMillis, intervalMillis);
            }
            else
            {
                // start periodic sends with first due after intervalMillis
                timer = new Timer(TimerCallback, null, intervalMillis, intervalMillis);
            }
        }

        // Convenience ctor that reads the default repeat interval from KeyboardSettings
        public RepeatHelper(int device, ushort kvpKey, uint nativeKey, bool useScanCode, VirtualKBMBase handler, bool sendFirstImmediate = true)
            : this(device, kvpKey, nativeKey, useScanCode, handler, DS4Windows.KeyboardSettings.RepeatIntervalMs, sendFirstImmediate)
        {
        }

        private void TimerCallback(object state)
        {
            try
            {
                SyntheticDispatcher.SendPress(device, kvpKey, nativeKey, useScanCode, handler);
            }
            catch { }
        }

        // Stop repeating and send a single KeyRelease. Safe to call multiple times.
        public void Stop()
        {
            if (Interlocked.Exchange(ref disposed, 1) == 1) return;

            try
            {
                timer.Change(Timeout.Infinite, Timeout.Infinite);
                timer.Dispose();
            }
            catch { }

            try
            {
                SyntheticDispatcher.SendRelease(device, kvpKey, nativeKey, useScanCode, handler);
            }
            catch { }
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
