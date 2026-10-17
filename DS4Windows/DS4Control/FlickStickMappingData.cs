using System;
using Sensorit.Base;

namespace DS4Windows
{
    // Phase7-Step7 W4: 旧 Mapping の入れ子の型をトップレベルへ移した（Q5＝A。中身は変えていない）。
    public class FlickStickMappingData
    {
        public const double DEFAULT_MINCUTOFF = 0.4;
        public const double DEFAULT_BETA = 0.4;

        public const double DEFAULT_FLICK_PROGRESS = 0.0;
        public const double DEFAULT_FLICK_SIZE = 0.0;
        public const double DEFAULT_FLICK_ANGLE_REMAINDER = 0.0;

        public OneEuroFilter flickFilter = new OneEuroFilter(DEFAULT_MINCUTOFF, DEFAULT_BETA);
        public double flickProgress = DEFAULT_FLICK_PROGRESS;
        public double flickSize = DEFAULT_FLICK_SIZE;
        public double flickAngleRemainder = DEFAULT_FLICK_ANGLE_REMAINDER;

        public void Reset()
        {
            flickFilter = new OneEuroFilter(DEFAULT_MINCUTOFF, DEFAULT_BETA);
            flickProgress = DEFAULT_FLICK_PROGRESS;
            flickSize = DEFAULT_FLICK_SIZE;
            flickAngleRemainder = DEFAULT_FLICK_ANGLE_REMAINDER;
        }
    }
}
