using System.Reflection;
using Xunit;
using DS4Windows;

namespace DS4WindowsTests
{
    // Issue8-1(3)是正（仕様⑤: トリガー構成ボタンの出力抑制期間の延長）の回帰防止テスト。
    // 詳細: docs-forDIMG/MadeByAgent/Phase5-Step14-Issue8-1-3-Fix-Plan.md
    //
    // Mapping は static かつ内部状態を多く持つため、実運用の入力評価（MapCustomAction）自体を
    // 直接駆動するテストは困難だが、本Issueの是正対象である
    // suppressedTriggerButtons（public static フィールド）と
    // CheckForSpecialActionSuppression（private static、reflection経由で呼び出す）の
    // 2点に絞って、抑制ロジックそのものの正しさを検証する。
    public class MappingSpecialActionSuppressionTests
    {
        // テスト間の副作用（他テストが同じデバイス添字・ボタンを使っている可能性）を避けるため、
        // 本テスト専用のデバイス添字・ボタンの組み合わせを使用する。
        private const int TestDevice = 3;

        private static bool InvokeCheckForSpecialActionSuppression(int device, DS4Controls control)
        {
            MethodInfo method = typeof(Mapping).GetMethod(
                "CheckForSpecialActionSuppression",
                BindingFlags.Static | BindingFlags.NonPublic);

            Assert.NotNull(method); // メソッド名変更等でシグネチャが失われた場合に検出する

            // Issue8-1(3)是正後の実装は cState/eState/tp/fieldMap を一切参照しないため、
            // null を渡しても安全に呼び出せる（suppressedTriggerButtons の内容のみで判定する）。
            object result = method.Invoke(null, new object[] { device, control, null, null, null, null });
            return (bool)result;
        }

        [Fact]
        public void CheckForSpecialActionSuppression_ReturnsFalse_WhenButtonNotSuppressed()
        {
            Mapping.suppressedTriggerButtons[TestDevice].Remove(DS4Controls.L2);

            bool suppressed = InvokeCheckForSpecialActionSuppression(TestDevice, DS4Controls.L2);

            Assert.False(suppressed);
        }

        [Fact]
        public void CheckForSpecialActionSuppression_ReturnsTrue_WhenButtonAddedToSuppressedSet()
        {
            // トリガー成立時（MapCustomAction内）に相当する操作を模擬する。
            Mapping.suppressedTriggerButtons[TestDevice].Add(DS4Controls.L2);

            try
            {
                bool suppressed = InvokeCheckForSpecialActionSuppression(TestDevice, DS4Controls.L2);
                Assert.True(suppressed);
            }
            finally
            {
                Mapping.suppressedTriggerButtons[TestDevice].Remove(DS4Controls.L2);
            }
        }

        [Fact]
        public void SuppressedTriggerButtons_ReleasingOneButton_DoesNotAffectOtherStillHeldButton()
        {
            // Issue8-1(3)是正の核心シナリオ: L2+PSのコンボが成立し、PSだけが先に離された場合、
            // まだ押され続けているL2自身の抑制は解除されない（L2は個別に離されるまで抑制継続）。
            var suppressSet = Mapping.suppressedTriggerButtons[TestDevice];
            suppressSet.Add(DS4Controls.L2);
            suppressSet.Add(DS4Controls.PS);

            try
            {
                // PSのみを解除（実際のMapCustomAction冒頭の解除パスでは、
                // getBoolSpecialActionMappingがfalseを返したボタンのみをRemoveする）。
                suppressSet.Remove(DS4Controls.PS);

                bool l2StillSuppressed = InvokeCheckForSpecialActionSuppression(TestDevice, DS4Controls.L2);
                bool psStillSuppressed = InvokeCheckForSpecialActionSuppression(TestDevice, DS4Controls.PS);

                Assert.True(l2StillSuppressed);  // L2はまだ押され続けているため抑制継続
                Assert.False(psStillSuppressed); // PSは離されたため抑制解除
            }
            finally
            {
                suppressSet.Remove(DS4Controls.L2);
                suppressSet.Remove(DS4Controls.PS);
            }
        }

        [Fact]
        public void SuppressedTriggerButtons_ArrayIsSizedForAllDevices()
        {
            Assert.Equal(Global.MAX_DS4_CONTROLLER_COUNT, Mapping.suppressedTriggerButtons.Length);
            for (int i = 0; i < Mapping.suppressedTriggerButtons.Length; i++)
            {
                Assert.NotNull(Mapping.suppressedTriggerButtons[i]);
            }
        }
    }
}