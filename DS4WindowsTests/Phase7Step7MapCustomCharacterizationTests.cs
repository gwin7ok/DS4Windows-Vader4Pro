using DS4Windows;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase7-Step7 W1（Q1＝A）: <c>Mapping.MapCustom</c>（通常入力の評価の本体）の特性化テスト。
    /// 現在の挙動を固定することが目的で、Step7〜9 で状態の置き場所や呼び出し元を変えても、入力 → 出力が変わらないことを保証する。
    /// 対象: 既定（素通し）、ボタン → ボタン、軸の向き → ボタン、トリガー → ボタン、ボタン → マウスボタン。
    /// 範囲外: ボタン → キー（<c>ActionManager</c> 経由。Phase8 の対象）、マクロ、ジャイロ、フリックスティック、タッチ（実機確認 U-17）。
    /// <c>Mapping</c> の静的な状態を共有するため、<c>ServiceProviderHolder</c> のコレクションで直列に実行する。
    /// </summary>
    [Collection("ServiceProviderHolder")]
    public class Phase7Step7MapCustomCharacterizationTests
    {
        [Fact]
        public void Default_PassesButtonsSticksAndTriggersThrough()
        {
            using var scope = new MappingCharacterizationScope();
            DS4State input = MappingCharacterizationScope.NeutralState();
            input.Cross = true;
            input.DpadUp = true;
            input.L1 = true;
            input.LX = 200;
            input.LY = 60;
            input.L2 = 180;

            DS4State output = scope.RunMapCustom(input);

            Assert.True(output.Cross);
            Assert.True(output.DpadUp);
            Assert.True(output.L1);
            Assert.False(output.Circle);
            Assert.False(output.Square);
            Assert.Equal(200, output.LX);
            Assert.Equal(60, output.LY);
            Assert.Equal(180, output.L2);
            Assert.Equal(128, output.RX);
            Assert.Equal(128, output.RY);
            Assert.Equal(0, output.R2);
        }

        [Fact]
        public void ButtonToButton_MovesThePressToTheTarget_AndErasesTheOriginal()
        {
            using var scope = new MappingCharacterizationScope();
            scope.RemapToButton(DS4Controls.Cross, X360Controls.B); // Cross → B（B の標準の割り当ては Circle）
            DS4State input = MappingCharacterizationScope.NeutralState();
            input.Cross = true;

            DS4State output = scope.RunMapCustom(input);

            Assert.True(output.Circle);
            Assert.False(output.Cross);
        }

        [Fact]
        public void ButtonToButton_ReleasedInput_OutputsNothing()
        {
            using var scope = new MappingCharacterizationScope();
            scope.RemapToButton(DS4Controls.Cross, X360Controls.B);

            DS4State output = scope.RunMapCustom(MappingCharacterizationScope.NeutralState());

            Assert.False(output.Circle);
            Assert.False(output.Cross);
        }

        [Fact]
        public void AxisDirectionToButton_PressesTheButton_AndNeutralizesTheAxis()
        {
            using var scope = new MappingCharacterizationScope();
            scope.RemapToButton(DS4Controls.LXPos, X360Controls.A); // 左スティック右 → A（A の標準の割り当ては Cross）
            DS4State input = MappingCharacterizationScope.NeutralState();
            input.LX = 255;

            DS4State output = scope.RunMapCustom(input);

            Assert.True(output.Cross);
            Assert.Equal(128, output.LX);
        }

        [Fact]
        public void TriggerToButton_PressesTheButton_AndZeroesTheTrigger()
        {
            using var scope = new MappingCharacterizationScope();
            scope.RemapToButton(DS4Controls.L2, X360Controls.Y); // L2 → Y（Y の標準の割り当ては Triangle）
            DS4State input = MappingCharacterizationScope.NeutralState();
            input.L2 = 220;

            DS4State output = scope.RunMapCustom(input);

            Assert.True(output.Triangle);
            Assert.Equal(0, output.L2);
        }

        [Fact]
        public void ButtonToLeftMouse_CountsTheClickOnTheDeviceState_AndErasesTheOriginal()
        {
            using var scope = new MappingCharacterizationScope();
            scope.RemapToButton(DS4Controls.Square, X360Controls.LeftMouse);
            DS4State input = MappingCharacterizationScope.NeutralState();
            input.Square = true;

            DS4State output = scope.RunMapCustom(input);

            Assert.False(output.Square);
            Assert.Equal(1, scope.Synthetic.DeviceState[MappingCharacterizationScope.Device].currentClicks.leftCount);
            Assert.Equal(0, scope.Synthetic.DeviceState[MappingCharacterizationScope.Device].currentClicks.rightCount);
        }

        [Fact]
        public void ButtonToRightMouse_ReleasedInput_DoesNotCountAClick()
        {
            using var scope = new MappingCharacterizationScope();
            scope.RemapToButton(DS4Controls.Square, X360Controls.RightMouse);

            scope.RunMapCustom(MappingCharacterizationScope.NeutralState());

            Assert.Equal(0, scope.Synthetic.DeviceState[MappingCharacterizationScope.Device].currentClicks.rightCount);
        }
    }
}
