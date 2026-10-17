using System;

namespace DS4Windows
{
    // Phase7-Step7 W4: 旧 Mapping の入れ子の型をトップレベルへ移した（Q5＝A。中身は変えていない）。
    public class PostMapStickData
    {
        public bool dirty;
        public byte LX = 128;
        public byte LY = 128;
        public byte RX = 128;
        public byte RY = 128;

        public void Reset()
        {
            dirty = false;
            LX = 128;
            LY = 128;
            RX = 128;
            RY = 128;
        }
    }
}
