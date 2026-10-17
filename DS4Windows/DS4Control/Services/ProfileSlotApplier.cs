using System;
using DS4Windows.DI;

namespace DS4Windows.Services
{
    /// <summary>
    /// <see cref="IProfileSlotApplier"/> の既定実装。接続時などに、指定スロットへ通常プロファイルを適用する。
    /// Phase7-Step3c W4: 旧 <c>Global.ApplyProfileToSlot</c> を削除し、<see cref="IProfileApplicationService.ApplyProfileWithoutHalt"/> へ委譲する。
    /// 旧実装と同じく、スロット範囲・名前の検査で副作用なしに <c>false</c> を返し、Halt はしない
    /// （接続処理中の Halt の入れ子を避けるため。呼び出し元の ControlService は接続処理で、すでに入力を停止している）。
    /// </summary>
    public class ProfileSlotApplier : IProfileSlotApplier
    {
        private readonly IProfileApplicationService _profileApplication;

        public ProfileSlotApplier(IProfileApplicationService profileApplication)
        {
            _profileApplication = profileApplication ?? throw new ArgumentNullException(nameof(profileApplication));
        }

        public bool ApplyToSlot(int slotIndex, string profileName, ProfileChangeSource source)
        {
            if (slotIndex < 0 || slotIndex >= ControlService.CURRENT_DS4_CONTROLLER_LIMIT)
                return false;

            if (string.IsNullOrWhiteSpace(profileName))
                return false;

            // 引数仕様: (deviceIndex, profileName, isTemp: false, launchProgram: false, source, prolog: null)
            return _profileApplication.ApplyProfileWithoutHalt(slotIndex, profileName, false, false, source, null);
        }
    }
}
