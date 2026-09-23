using System;
using Xunit;
using DS4Windows;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase6-Step3-4: `Mapping.SetCurveAndDeadzone` が、`Global`（静的な BackingStore）ではなく、
    /// 呼び出し元から引数で渡された `IProfileSettingsService` の値を参照していることを検証する。
    /// 検証方法: 互いに独立した BackingStore を持つ2つの `ProfileSettingsService` を用意し、
    /// 片方だけ設定値を変えて同じ入力を与え、出力が変わることを確認する。
    /// もしメソッドが `Global` 側の（両者に共通の）設定を読んでいれば、2つの出力は一致してしまうため検出できる。
    /// 期待値の絶対値（ゴールデン値）は固定せず、相対比較のみとする（既定値の変更に強くするため）。
    /// Step3-5・Step3-6 で同様の引数渡しに変更するメソッドの検証も、このクラスへ追加していく。
    /// </summary>
    public class MappingArgumentPassThroughTests
    {
        private const int Slot = 0;

        private static ProfileSettingsService CreateIsolatedService(out BackingStore store)
        {
            store = new BackingStore();
            return new ProfileSettingsService(store);
        }

        private static DS4State Run(ProfileSettingsService settings,
            byte lx = 128, byte ly = 128, byte rx = 128, byte ry = 128, byte l2 = 0, byte r2 = 0)
        {
            var cState = new DS4State { LX = lx, LY = ly, RX = rx, RY = ry, L2 = l2, R2 = r2 };
            var dState = new DS4State();
            return Mapping.SetCurveAndDeadzone(Slot, cState, dState, settings);
        }

        [Fact]
        public void SetCurveAndDeadzone_UsesLsRotationFromPassedSettings()
        {
            var neutral = CreateIsolatedService(out _);
            var rotated = CreateIsolatedService(out BackingStore rotatedStore);
            rotatedStore.LSRotation[Slot] = Math.PI / 2.0; // 90度回転（右入力が上下方向へ移る）

            DS4State a = Run(neutral, lx: 200, ly: 128);
            DS4State b = Run(rotated, lx: 200, ly: 128);

            Assert.True(a.LX != b.LX || a.LY != b.LY,
                $"LS回転が反映されていない: neutral=({a.LX},{a.LY}) rotated=({b.LX},{b.LY})");
        }

        [Fact]
        public void SetCurveAndDeadzone_UsesRsRotationFromPassedSettings()
        {
            var neutral = CreateIsolatedService(out _);
            var rotated = CreateIsolatedService(out BackingStore rotatedStore);
            rotatedStore.RSRotation[Slot] = Math.PI / 2.0;

            DS4State a = Run(neutral, rx: 200, ry: 128);
            DS4State b = Run(rotated, rx: 200, ry: 128);

            Assert.True(a.RX != b.RX || a.RY != b.RY,
                $"RS回転が反映されていない: neutral=({a.RX},{a.RY}) rotated=({b.RX},{b.RY})");
        }

        [Fact]
        public void SetCurveAndDeadzone_UsesLsDeadZoneFromPassedSettings()
        {
            var neutral = CreateIsolatedService(out _);
            var wideDead = CreateIsolatedService(out BackingStore wideStore);
            wideStore.lsModInfo[Slot].deadZone = 127; // 入力（中心から72）を完全にデッドゾーン内へ収める

            DS4State a = Run(neutral, lx: 200, ly: 128);
            DS4State b = Run(wideDead, lx: 200, ly: 128);

            Assert.Equal(128, b.LX);
            Assert.Equal(128, b.LY);
            Assert.NotEqual(128, a.LX); // 既定のデッドゾーンでは中心へ潰れない（引数の値が効いている対照）
        }

        [Fact]
        public void SetCurveAndDeadzone_UsesLsSensFromPassedSettings()
        {
            var neutral = CreateIsolatedService(out _);
            var halfSens = CreateIsolatedService(out BackingStore halfStore);
            halfStore.LSSens[Slot] = 0.5;

            DS4State a = Run(neutral, lx: 200, ly: 128);
            DS4State b = Run(halfSens, lx: 200, ly: 128);

            Assert.True(Math.Abs(b.LX - 128) < Math.Abs(a.LX - 128),
                $"LS感度が反映されていない: neutral.LX={a.LX} halfSens.LX={b.LX}");
        }

        [Fact]
        public void SetCurveAndDeadzone_UsesL2SensFromPassedSettings()
        {
            var neutral = CreateIsolatedService(out _);
            var halfSens = CreateIsolatedService(out BackingStore halfStore);
            halfStore.l2Sens[Slot] = 0.5;

            DS4State a = Run(neutral, l2: 200);
            DS4State b = Run(halfSens, l2: 200);

            Assert.True(b.L2 < a.L2, $"L2感度が反映されていない: neutral.L2={a.L2} halfSens.L2={b.L2}");
        }

        [Fact]
        public void SetCurveAndDeadzone_UsesR2SensFromPassedSettings()
        {
            var neutral = CreateIsolatedService(out _);
            var halfSens = CreateIsolatedService(out BackingStore halfStore);
            halfStore.r2Sens[Slot] = 0.5;

            DS4State a = Run(neutral, r2: 200);
            DS4State b = Run(halfSens, r2: 200);

            Assert.True(b.R2 < a.R2, $"R2感度が反映されていない: neutral.R2={a.R2} halfSens.R2={b.R2}");
        }
    }
}