using System;
using System.Collections.Generic;

namespace DS4Windows
{
    // Phase7-Step7 W2: 旧 Mapping.SyntheticState（入れ子の型）をトップレベルへ移した（Q5＝A。中身は変えていない）。
    /*
     * Represent the synthetic keyboard and mouse events.  Maintain counts for each so we don't duplicate events.
     */
    public class SyntheticState
    {
        public struct MouseClick
        {
            public int leftCount, middleCount, rightCount, fourthCount, fifthCount, wUpCount, wDownCount, toggleCount;
            public bool toggle;
        }
        public MouseClick previousClicks, currentClicks;
        public struct KeyPress
        {
            public int vkCount, scanCodeCount, repeatCount, toggleCount; // repeat takes priority over non-, and scancode takes priority over non-
            public bool toggle;
            // pending: indicates a toggle occurred and should be prioritized when committing
            public bool pending;
            // timestamp of last toggle (UTC ticks)
            public long lastToggleTimeUtcTicks;
            // timestamp of last synthetic send (UTC ticks) used to throttle excessive sends
            public long lastSyntheticSendUtcTicks;
        }
        public class KeyPresses
        {
            public KeyPress previous, current;
        }
        public Dictionary<UInt16, KeyPresses> keyPresses = new Dictionary<UInt16, KeyPresses>();
        public Dictionary<ushort, uint> nativeKeyAlias = new Dictionary<ushort, uint>();

        public void SaveToPrevious(bool performClear)
        {
            previousClicks = currentClicks;
            if (performClear)
                currentClicks.leftCount = currentClicks.middleCount = currentClicks.rightCount = currentClicks.fourthCount = currentClicks.fifthCount = currentClicks.wUpCount = currentClicks.wDownCount = currentClicks.toggleCount = 0;

            //foreach (KeyPresses kp in keyPresses.Values)
            Dictionary<ushort, KeyPresses>.ValueCollection keyValues = keyPresses.Values;
            for (var keyEnum = keyValues.GetEnumerator(); keyEnum.MoveNext();)
            //for (int i = 0, kpCount = keyValues.Count; i < kpCount; i++)
            {
                //KeyPresses kp = keyValues.ElementAt(i);
                KeyPresses kp = keyEnum.Current;
                kp.previous = kp.current;
                if (performClear)
                {
                    kp.current.repeatCount = kp.current.scanCodeCount = kp.current.vkCount = kp.current.toggleCount = 0;
                    //kp.current.toggle = false;
                }
            }
        }
    }
}
