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
    /// Step3-5（ジャイロ系 SX/SZ・`ApplyStickCalibration`）の検証を追加済み。Step3-6 で同様の引数渡しに変更するメソッドの検証も、このクラスへ追加していく。
    /// なお `Mapping.Commit` は、出力先の `IVirtualKBM` を `AppHost`（実 DI ホスト）から解決しており、
    /// テストから実行すると実際にマウス・キー入力を送出してしまうため、挙動テストは行わず、
    /// ソース走査ガード（`MappingCommitAndCalibrationGlobalReferenceGuardTests`）と実機確認で担保する。
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

        // ---- Step3-5: ジャイロ系（SX/SZ） ----

        private const int GyroInput = 64;      // 中心から64（±128 の範囲）
        private const int GyroSentinel = 999;  // 「ジャイロ→コントロール変換が実行されなかった」ことを判別する初期値

        private static DS4State RunGyro(ProfileSettingsService settings)
        {
            var cState = new DS4State();
            var motion = new SixAxis(0, 0, 0, 0, 0, 0, 0.0);
            motion.accelX = GyroInput;
            motion.accelZ = GyroInput;
            motion.outputAccelX = GyroSentinel;
            motion.outputAccelZ = GyroSentinel;
            motion.outputGyroControls = true;
            cState.Motion = motion;

            var dState = new DS4State();
            // SetCurveAndDeadzone は cState.CopyTo(dState) で Motion の参照を共有するため、結果は cState.Motion から読める。
            Mapping.SetCurveAndDeadzone(Slot, cState, dState, settings);
            return cState;
        }

        private static ProfileSettingsService CreateGyroControlsService(out BackingStore store)
        {
            var service = CreateIsolatedService(out store);
            store.gyroOutMode[Slot] = GyroOutMode.Controls;
            return service;
        }

        [Fact]
        public void SetCurveAndDeadzone_UsesGyroOutputModeFromPassedSettings()
        {
            var controls = CreateGyroControlsService(out _);
            var notControls = CreateIsolatedService(out BackingStore otherStore);
            otherStore.gyroOutMode[Slot] = GyroOutMode.None;

            DS4State a = RunGyro(controls);
            DS4State b = RunGyro(notControls);

            Assert.NotEqual(GyroSentinel, a.Motion.outputAccelX); // Controls: 変換が実行される
            Assert.Equal(GyroSentinel, b.Motion.outputAccelX);    // Controls 以外: 変換されず初期値のまま
        }

        [Fact]
        public void SetCurveAndDeadzone_UsesSxSensFromPassedSettings()
        {
            var neutral = CreateGyroControlsService(out _);
            var halfSens = CreateGyroControlsService(out BackingStore halfStore);
            halfStore.SXSens[Slot] = 0.5;

            DS4State a = RunGyro(neutral);
            DS4State b = RunGyro(halfSens);

            Assert.True(Math.Abs(b.Motion.outputAccelX) < Math.Abs(a.Motion.outputAccelX),
                $"SX感度が反映されていない: neutral={a.Motion.outputAccelX} halfSens={b.Motion.outputAccelX}");
        }

        [Fact]
        public void SetCurveAndDeadzone_UsesSzSensFromPassedSettings()
        {
            var neutral = CreateGyroControlsService(out _);
            var halfSens = CreateGyroControlsService(out BackingStore halfStore);
            halfStore.SZSens[Slot] = 0.5;

            DS4State a = RunGyro(neutral);
            DS4State b = RunGyro(halfSens);

            Assert.True(Math.Abs(b.Motion.outputAccelZ) < Math.Abs(a.Motion.outputAccelZ),
                $"SZ感度が反映されていない: neutral={a.Motion.outputAccelZ} halfSens={b.Motion.outputAccelZ}");
        }

        [Fact]
        public void SetCurveAndDeadzone_UsesSxDeadzoneFromPassedSettings()
        {
            var neutral = CreateGyroControlsService(out _);
            var wideDead = CreateGyroControlsService(out BackingStore wideStore);
            wideStore.SXDeadzone[Slot] = 0.9; // 入力（64）を完全にデッドゾーン内（0.9*128=115）へ収める

            DS4State a = RunGyro(neutral);
            DS4State b = RunGyro(wideDead);

            Assert.Equal(0, b.Motion.outputAccelX);
            Assert.NotEqual(0, a.Motion.outputAccelX); // 既定のデッドゾーンでは0にならない（引数の値が効いている対照）
        }

        [Fact]
        public void SetCurveAndDeadzone_UsesSzDeadzoneFromPassedSettings()
        {
            var neutral = CreateGyroControlsService(out _);
            var wideDead = CreateGyroControlsService(out BackingStore wideStore);
            wideStore.SZDeadzone[Slot] = 0.9;

            DS4State a = RunGyro(neutral);
            DS4State b = RunGyro(wideDead);

            Assert.Equal(0, b.Motion.outputAccelZ);
            Assert.NotEqual(0, a.Motion.outputAccelZ);
        }

        [Fact]
        public void SetCurveAndDeadzone_UsesSxOutCurveModeFromPassedSettings()
        {
            var linear = CreateGyroControlsService(out _);
            var squared = CreateGyroControlsService(out BackingStore squaredStore);
            squaredStore.setSXOutCurveMode(Slot, 2); // 2乗カーブ（出力が小さくなる）

            DS4State a = RunGyro(linear);
            DS4State b = RunGyro(squared);

            Assert.True(Math.Abs(b.Motion.outputAccelX) < Math.Abs(a.Motion.outputAccelX),
                $"SX出力カーブが反映されていない: linear={a.Motion.outputAccelX} squared={b.Motion.outputAccelX}");
        }

        [Fact]
        public void SetCurveAndDeadzone_UsesSzOutCurveModeFromPassedSettings()
        {
            var linear = CreateGyroControlsService(out _);
            var squared = CreateGyroControlsService(out BackingStore squaredStore);
            squaredStore.setSZOutCurveMode(Slot, 2);

            DS4State a = RunGyro(linear);
            DS4State b = RunGyro(squared);

            Assert.True(Math.Abs(b.Motion.outputAccelZ) < Math.Abs(a.Motion.outputAccelZ),
                $"SZ出力カーブが反映されていない: linear={a.Motion.outputAccelZ} squared={b.Motion.outputAccelZ}");
        }

        // ---- Step3-5: ドリフト補正（ApplyStickCalibration） ----

        [Fact]
        public void ApplyStickCalibration_UsesDriftValuesFromPassedSettings()
        {
            var neutral = CreateIsolatedService(out _);
            var drifted = CreateIsolatedService(out BackingStore driftStore);
            driftStore.rightStickDriftXAxis[Slot] = 10;
            driftStore.rightStickDriftYAxis[Slot] = 20;
            driftStore.leftStickDriftXAxis[Slot] = 30;
            driftStore.leftStickDriftYAxis[Slot] = 40;

            DS4State a = Mapping.ApplyStickCalibration(Slot, new DS4State { LX = 100, LY = 100, RX = 100, RY = 100 }, neutral);
            DS4State b = Mapping.ApplyStickCalibration(Slot, new DS4State { LX = 100, LY = 100, RX = 100, RY = 100 }, drifted);

            // 補正なし（対照）: 入力がそのまま出力される
            Assert.Equal(100, a.RX);
            Assert.Equal(100, a.RY);
            Assert.Equal(100, a.LX);
            Assert.Equal(100, a.LY);

            // 補正あり: 各軸のドリフト量が差し引かれる（元の Global 版と同じ演算）
            Assert.Equal(90, b.RX);
            Assert.Equal(80, b.RY);
            Assert.Equal(70, b.LX);
            Assert.Equal(60, b.LY);
        }

        [Fact]
        public void ApplyStickCalibration_ClampsToByteRange()
        {
            var drifted = CreateIsolatedService(out BackingStore driftStore);
            driftStore.leftStickDriftXAxis[Slot] = 50; // 10 - 50 が負になる → 0 に丸める

            DS4State b = Mapping.ApplyStickCalibration(Slot, new DS4State { LX = 10, LY = 128, RX = 128, RY = 128 }, drifted);

            Assert.Equal(0, b.LX);
        }

    }
}