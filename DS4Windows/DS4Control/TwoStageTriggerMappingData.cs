using System;

namespace DS4Windows
{
    // Phase7-Step7 W4: 旧 Mapping の入れ子の型をトップレベルへ移した（Q5＝A。中身は変えていない）。
    public class TwoStageTriggerMappingData
    {
        public enum EngageButtonsMode : uint
        {
            None,
            SoftPullOnly,
            FullPullOnly,
            Both,
        }

        [Flags]
        public enum ActiveZoneButtons : ushort
        {
            None,
            SoftPull,
            FullPull
        }

        public bool startCheck;
        public DateTime checkTime;
        public bool outputActive;
        public bool softPullActActive;
        public bool fullPullActActive;
        public EngageButtonsMode actionStateMode = EngageButtonsMode.Both;
        public ActiveZoneButtons previousActiveButtons = ActiveZoneButtons.None;

        public void StartProcessing()
        {
            startCheck = true;
            checkTime = DateTime.Now;
            outputActive = false;
            softPullActActive = false;
            fullPullActActive = false;
            actionStateMode = EngageButtonsMode.Both;
            previousActiveButtons = ActiveZoneButtons.None;
        }

        public void Reset()
        {
            checkTime = DateTime.Now;
            startCheck = false;
            outputActive = false;
            softPullActActive = false;
            fullPullActActive = false;
            actionStateMode = EngageButtonsMode.Both;
            previousActiveButtons = ActiveZoneButtons.None;
        }
    }
}
