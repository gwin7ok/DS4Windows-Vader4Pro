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
        private volatile bool isRunning;
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

            // Create timer but do not start it until Start() is called. This allows Stop() to pause
            // repeating without disposing the instance so it can be reused.
            timer = new Timer(TimerCallback, null, Timeout.Infinite, Timeout.Infinite);
            isRunning = false;
            if (sendFirstImmediate)
            {
                Start();
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

        // Start repeating. If configured to send first immediate, this will issue a press immediately
        // and then begin periodic sends. Safe to call multiple times.
        public void Start()
        {
            if (Interlocked.CompareExchange(ref disposed, 0, 0) == 1) return;
            if (isRunning) return;

            try
            {
                if (SendFirstImmediate)
                {
                    try { SyntheticDispatcher.SendPress(device, kvpKey, nativeKey, useScanCode, handler); } catch { }
                }
                timer.Change(IntervalMs, IntervalMs);
                isRunning = true;
            }
            catch { }
        }

        // Stop repeating and send a single KeyRelease. Safe to call multiple times. Does not dispose
        // the underlying timer so the instance can be restarted by calling Start().
        public void Stop()
        {
            if (Interlocked.CompareExchange(ref disposed, 0, 0) == 1) return;
            if (!isRunning) return;

            try
            {
                timer.Change(Timeout.Infinite, Timeout.Infinite);
                isRunning = false;
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
            if (Interlocked.Exchange(ref disposed, 1) == 1) return;
            try
            {
                try { timer.Change(Timeout.Infinite, Timeout.Infinite); } catch { }
                try { timer.Dispose(); } catch { }
            }
            catch { }
            try
            {
                if (isRunning) SyntheticDispatcher.SendRelease(device, kvpKey, nativeKey, useScanCode, handler);
            }
            catch { }
            isRunning = false;
        }
    }
}
