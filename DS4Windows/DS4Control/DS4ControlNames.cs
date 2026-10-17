using System;
using System.Collections.Generic;

namespace DS4Windows
{
    /// <summary>
    /// Phase7-Step10b W1（Q1＝A）: DS4 入力の表示名と、マクロ記録用の値の表（旧: <c>Global</c> の静的メンバー。宣言は移動のみ）。
    /// </summary>
    public static class DS4ControlNames
    {
        public static Dictionary<DS4Controls, string> ds4inputNames = new Dictionary<DS4Controls, string>()
        {
            [DS4Controls.LXNeg] = "Left X-Axis-",
            [DS4Controls.LXPos] = "Left X-Axis+",
            [DS4Controls.LYNeg] = "Left Y-Axis-",
            [DS4Controls.LYPos] = "Left Y-Axis+",
            [DS4Controls.RXNeg] = "Right X-Axis-",
            [DS4Controls.RXPos] = "Right X-Axis+",
            [DS4Controls.RYNeg] = "Right Y-Axis-",
            [DS4Controls.RYPos] = "Right Y-Axis+",
            [DS4Controls.L1] = "L1",
            [DS4Controls.L2] = "L2",
            [DS4Controls.L3] = "L3",
            [DS4Controls.R1] = "R1",
            [DS4Controls.R2] = "R2",
            [DS4Controls.R3] = "R3",
            [DS4Controls.Square] = "Square",
            [DS4Controls.Triangle] = "Triangle",
            [DS4Controls.Circle] = "Circle",
            [DS4Controls.Cross] = "Cross",
            [DS4Controls.DpadUp] = "Dpad Up",
            [DS4Controls.DpadRight] = "Dpad Right",
            [DS4Controls.DpadDown] = "Dpad Down",
            [DS4Controls.DpadLeft] = "Dpad Left",
            [DS4Controls.PS] = "PS",
            [DS4Controls.Share] = "Share",
            [DS4Controls.Options] = "Options",
            [DS4Controls.Mute] = "Mute",
            [DS4Controls.FnL] = "Function Left",
            [DS4Controls.FnR] = "Function Right",
            [DS4Controls.BLP] = "Bottom Left Paddle",
            [DS4Controls.BRP] = "Bottom Right Paddle",
            [DS4Controls.Capture] = "Capture",
            [DS4Controls.SideL] = "Side L",
            [DS4Controls.SideR] = "Side R",
            [DS4Controls.TouchLeft] = "Left Touch",
            [DS4Controls.TouchUpper] = "Upper Touch",
            [DS4Controls.TouchMulti] = "Multitouch",
            [DS4Controls.TouchRight] = "Right Touch",

            [DS4Controls.GyroXPos] = "Gyro X+",
            [DS4Controls.GyroXNeg] = "Gyro X-",
            [DS4Controls.GyroZPos] = "Gyro Z+",
            [DS4Controls.GyroZNeg] = "Gyro Z-",
            [DS4Controls.SwipeLeft] = "Swipe Left",
            [DS4Controls.SwipeRight] = "Swipe Right",
            [DS4Controls.SwipeUp] = "Swipe Up",
            [DS4Controls.SwipeDown] = "Swipe Down",
            [DS4Controls.L2FullPull] = "L2 Full Pull",
            [DS4Controls.R2FullPull] = "R2 Full Pull",
            [DS4Controls.LSOuter] = "LS Outer",
            [DS4Controls.RSOuter] = "RS Outer",

            [DS4Controls.GyroSwipeLeft] = "Gyro Swipe Left",
            [DS4Controls.GyroSwipeRight] = "Gyro Swipe Right",
            [DS4Controls.GyroSwipeUp] = "Gyro Swipe Up",
            [DS4Controls.GyroSwipeDown] = "Gyro Swipe Down",

            [DS4Controls.TouchStarted] = "Touch Started",
            [DS4Controls.TouchEnded] = "Touch Ended",
        };

        public static Dictionary<DS4Controls, int> macroDS4Values = new Dictionary<DS4Controls, int>()
        {
            [DS4Controls.Cross] = 261,
            [DS4Controls.Circle] = 262,
            [DS4Controls.Square] = 263,
            [DS4Controls.Triangle] = 264,
            [DS4Controls.Options] = 265,
            [DS4Controls.Share] = 266,
            [DS4Controls.DpadUp] = 267,
            [DS4Controls.DpadDown] = 268,
            [DS4Controls.DpadLeft] = 269,
            [DS4Controls.DpadRight] = 270,
            [DS4Controls.PS] = 271,
            [DS4Controls.L1] = 272,
            [DS4Controls.R1] = 273,
            [DS4Controls.L2] = 274,
            [DS4Controls.R2] = 275,
            [DS4Controls.L3] = 276,
            [DS4Controls.R3] = 277,
            [DS4Controls.LXPos] = 278,
            [DS4Controls.LXNeg] = 279,
            [DS4Controls.LYPos] = 280,
            [DS4Controls.LYNeg] = 281,
            [DS4Controls.RXPos] = 282,
            [DS4Controls.RXNeg] = 283,
            [DS4Controls.RYPos] = 284,
            [DS4Controls.RYNeg] = 285,
            [DS4Controls.TouchLeft] = 286,
            [DS4Controls.TouchRight] = 286,
            [DS4Controls.TouchUpper] = 286,
            [DS4Controls.TouchMulti] = 286,
            [DS4Controls.TouchStarted] = 287,
            [DS4Controls.TouchEnded] = 288,
        };
    }
}
