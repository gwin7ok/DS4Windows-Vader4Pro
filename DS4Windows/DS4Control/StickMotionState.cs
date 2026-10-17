using Sensorit.Base;

namespace DS4Windows
{
    /// <summary>
    /// Phase7-Step7 W4（Q2＝A）: スティック・ジャイロ・トリガーの処理状態のうち、<c>Mapping</c> の外から触られるものの保持クラス
    /// （旧: <c>Mapping</c> の静的フィールド <c>gyroStickX</c>／<c>gyroStickY</c>／<c>mapStickActionData</c>／<c>flickMappingData</c>／
    /// <c>l2TwoStageMappingData</c>／<c>r2TwoStageMappingData</c>／<c>wheelFilters</c>／<c>deltaAccelProcessors</c>）。
    /// <c>Mapping</c>、<c>Mouse</c>、<c>ControlService</c> が使う。<c>Mapping</c> の内部だけで使う状態（<c>stickValueHistory</c> ほか）は、Step8 まで <c>Mapping</c> に残す。
    /// 配列の参照は再代入しない（要素の読み書き、または要素の差し替えのみ）。デバイスの添字ごとに分かれており、ロックはない（従来どおり）。
    /// </summary>
    public class StickMotionState
    {
        /// <summary>ジャイロのマウススティック出力の、デバイスごとの軸の値（128＝中立）。</summary>
        public byte[] GyroStickX { get; }

        public byte[] GyroStickY { get; }

        /// <summary>プロファイルの割り当て後に重ねるスティックの出力（<c>Mouse</c> が書き、<c>MapCustom</c> が読む）。</summary>
        public PostMapStickData[] MapStickActionData { get; }

        public FlickStickMappingData[] FlickMappingData { get; }

        public TwoStageTriggerMappingData[] L2TwoStageMappingData { get; }

        public TwoStageTriggerMappingData[] R2TwoStageMappingData { get; }

        public DeltaSettingsProcessorGroup[] DeltaAccelProcessors { get; }

        /// <summary>ステアリングホイールのジャイロ用のフィルター（<c>ControlService</c> が設定の適用時に差し替える。初期値は null）。</summary>
        public OneEuroFilter[] WheelFilters { get; } = new OneEuroFilter[ControlService.MAX_DS4_CONTROLLER_COUNT];

        public StickMotionState()
        {
            int count = ControllerLimits.MaxControllerCount;
            GyroStickX = new byte[count];
            GyroStickY = new byte[count];
            MapStickActionData = new PostMapStickData[count];
            FlickMappingData = new FlickStickMappingData[count];
            L2TwoStageMappingData = new TwoStageTriggerMappingData[count];
            R2TwoStageMappingData = new TwoStageTriggerMappingData[count];
            DeltaAccelProcessors = new DeltaSettingsProcessorGroup[count];

            for (int i = 0; i < count; i++)
            {
                GyroStickX[i] = 128;
                GyroStickY[i] = 128;
                MapStickActionData[i] = new PostMapStickData();
                FlickMappingData[i] = new FlickStickMappingData();
                L2TwoStageMappingData[i] = new TwoStageTriggerMappingData();
                R2TwoStageMappingData[i] = new TwoStageTriggerMappingData();
                DeltaAccelProcessors[i] = new DeltaSettingsProcessorGroup();
            }
        }
    }
}
