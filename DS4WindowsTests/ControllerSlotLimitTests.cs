using Xunit;
using DS4Windows;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase6-Step2-1b (決定O2=C): コントローラースロット上限の DI サービス化の回帰テスト。
    ///
    /// 「上限」は同時に扱えるスロット数の最大値（Windows 8 以上で 8、未満で 4）であり、
    /// 現在接続中のコントローラー台数ではない。旧 ControlService の static フィールド
    /// （CURRENT_DS4_CONTROLLER_LIMIT / USING_MAX_CONTROLLERS）と同一の値を、
    /// IEnvironmentService と互換シムの双方が返すことを検証する。
    /// </summary>
    public class ControllerSlotLimitTests
    {
        private readonly EnvironmentService _environment = new EnvironmentService();

        [Fact]
        public void ControllerSlotLimit_MatchesLegacyCalculation()
        {
            // Phase7-Step4 W1: 旧 Global.IsWin8OrGreater は EnvironmentService へ移した。ここでは旧実装と同じ式を直接書いて照合する
            var os = System.Environment.OSVersion.Version;
            bool win8OrGreater = os.Major > 6 || (os.Major == 6 && os.Minor >= 2);
            int expected = win8OrGreater
                ? DS4Windows.ControllerLimits.MaxControllerCount
                : DS4Windows.ControllerLimits.OldXInputControllerCount;

            Assert.Equal(expected, _environment.ControllerSlotLimit);
        }

        [Fact]
        public void ControllerSlotLimit_IsWithinSupportedRange()
        {
            int limit = _environment.ControllerSlotLimit;

            Assert.True(limit >= DS4Windows.ControllerLimits.OldXInputControllerCount);
            Assert.True(limit <= DS4Windows.ControllerLimits.MaxControllerCount);
        }

        [Fact]
        public void UsingMaxControllers_IsTrueOnlyWhenLimitEqualsExpandedCount()
        {
            Assert.Equal(
                _environment.ControllerSlotLimit == ControlService.EXPANDED_CONTROLLER_COUNT,
                _environment.UsingMaxControllers);
        }

        [Fact]
        public void ExpandedControllerCount_EqualsGlobalMaxControllerCount()
        {
            // EnvironmentService と ControlService の互換シムは EXPANDED_CONTROLLER_COUNT で判定するため、
            // Global 側の最大値と食い違っていないことを固定する。
            Assert.Equal(DS4Windows.ControllerLimits.MaxControllerCount, ControlService.EXPANDED_CONTROLLER_COUNT);
        }

        [Fact]
        public void ControlServiceStaticShim_SharesValueWithService() // Phase7-Step10b W4: USING_MAX_CONTROLLERS は削除済み（CURRENT_DS4_CONTROLLER_LIMIT のみ残る）
        {
            Assert.Equal(_environment.ControllerSlotLimit, ControlService.CURRENT_DS4_CONTROLLER_LIMIT);
        }
    }
}