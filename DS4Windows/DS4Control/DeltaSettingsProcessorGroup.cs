

namespace DS4Windows
{
    // Phase7-Step7 W4: 旧 Mapping の入れ子の型をトップレベルへ移した（Q5＝A。中身は変えていない）。
    public class DeltaSettingsProcessorGroup
    {
        public DeltaSettingsProcessor LSProcessor = new DeltaSettingsProcessor();
        public DeltaSettingsProcessor RSProcessor = new DeltaSettingsProcessor();
    }
}
