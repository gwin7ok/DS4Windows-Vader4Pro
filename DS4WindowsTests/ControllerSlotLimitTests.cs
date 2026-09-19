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
            int expected = Global.IsWin8OrGreater()
                ? Global.MAX_DS4_CONTROLLER_COUNT
                : Global.OLD_XINPUT_CONTROLLER_COUNT;

            Assert.Equal(expected, _environment.ControllerSlotLimit);
        }

        [Fact]
        public void ControllerSlotLimit_IsWithinSupportedRange()
        {
            int limit = _environment.ControllerSlotLimit;

            Assert.True(limit >= Global.OLD_XINPUT_CONTROLLER_COUNT);
            Assert.True(limit <= Global.MAX_DS4_CONTROLLER_COUNT);
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
            Assert.Equal(Global.MAX_DS4_CONTROLLER_COUNT, ControlService.EXPANDED_CONTROLLER_COUNT);
        }

        [Fact]
        public void ControlServiceStaticShim_SharesValueWithService()
        {
            Assert.Equal(_environment.ControllerSlotLimit, ControlService.CURRENT_DS4_CONTROLLER_LIMIT);
            Assert.Equal(_environment.UsingMaxControllers, ControlService.USING_MAX_CONTROLLERS);
        }
    }
}