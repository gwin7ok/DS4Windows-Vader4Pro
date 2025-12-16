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

        // intervalMillis: repeat period in milliseconds (50ms typical)
        public RepeatHelper(int device, ushort kvpKey, uint nativeKey, bool useScanCode, VirtualKBMBase handler, int intervalMillis = 50)
        {
            this.device = device;
            this.kvpKey = kvpKey;
            this.nativeKey = nativeKey;
            this.useScanCode = useScanCode;
            this.handler = handler;

            // Start timer immediately: dueTime = 0, period = intervalMillis
            timer = new Timer(TimerCallback, null, 0, intervalMillis);
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
