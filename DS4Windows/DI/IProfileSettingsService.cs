using System;
using System.Collections.Generic;
using System.Globalization;
using DS4Windows.DS4Control;
using DS4Windows.InputDevices;
using static DS4Windows.Mouse;

namespace DS4Windows.DI
{

    public interface IProfileSettingsService
    {

        bool[] TouchpadActiveArray { get; set; }
        bool[] UseTempProfileArray { get; set; }
        bool[] TempProfileDistanceArray { get; set; }
        bool[] UseDInputOnlyArray { get; set; }

        bool GetTouchpadActive(int deviceIndex);

        bool GetUseTempProfile(int deviceIndex);
        void SetUseTempProfile(int deviceIndex, bool value);

        string GetTempProfileName(int deviceIndex);
        void SetTempProfileName(int deviceIndex, string value);

        void SetTempProfileDistance(int deviceIndex, bool value);

        bool GetLinkedProfileCheck(int deviceIndex);
        void SetLinkedProfileCheck(int deviceIndex, bool value);

        X360Controls[] GetDefaultButtonMapping();

        // ---- Phase6-Step9-1: PresetMenuHelper／BindingWindowViewModel の Global.RefreshActionAlias 参照の置換先 ----
        /// <summary>
        /// 仮想キーの別名（actionAlias）を、現在の出力 KBM マッピング（<see cref="OutputKBMMapping"/>）で更新する
        /// （<c>Global.RefreshActionAlias</c> と同じ動作）。
        /// </summary>
        /// <param name="setting">編集中の <see cref="DS4ControlSettings"/></param>
        /// <param name="shift">シフト側のアクションを更新するか</param>
        void RefreshActionAlias(DS4ControlSettings setting, bool shift);
        /// <summary>
        /// Xbox360 ボタン → DS4 ボタンの逆引き表そのもの（<c>Global.reverseX360ButtonMapping</c>）を、コピーせずに返す。
        /// 起動時に1回作られて以後変化しない定数表で、毎入力レポートの経路（<c>Mapping.ProcessControlSettingAction</c>）から引かれるため、
        /// 呼び出しごとに <c>Clone()</c> するコピー版は使わず、本プロパティを使う（ゼロアロケーション方針、Phase6-Step3-6a の決定 R1）。コピー版 <c>GetReverseX360ButtonMapping()</c> は、呼び出し元が 0 件のため Phase7-Step2 W1 で削除した。
        /// 返された配列は呼び出し側で書き換えないこと。書き換える必要がある場合は、呼び出し側で <c>Clone()</c> して使う。
        /// </summary>
        DS4Controls[] ReverseX360ButtonMapping { get; }

        // ---- Step10-2-A-1: スティック関連 (m_Config委譲) ----
        StickDeadZoneInfo[] LSModInfo { get; }
        StickDeadZoneInfo[] RSModInfo { get; }
        double[] LSRotation { get; }
        double[] RSRotation { get; }
        double[] LSSens { get; }
        double[] RSSens { get; }
        SquareStickInfo[] SquStickInfo { get; }
        StickAntiSnapbackInfo[] LSAntiSnapbackInfo { get; }
        StickAntiSnapbackInfo[] RSAntiSnapbackInfo { get; }
        StickOutputSetting[] LSOutputSettings { get; }
        StickOutputSetting[] RSOutputSettings { get; }
        BezierCurve[] LsOutBezierCurveObj { get; }
        BezierCurve[] RsOutBezierCurveObj { get; }
        int GetLsOutCurveMode(int index);
        void SetLsOutCurveMode(int index, int value);
        int GetRsOutCurveMode(int index);
        void SetRsOutCurveMode(int index, int value);

        // ---- Step10-2-A-2: トリガー(L2/R2)関連 (m_Config委譲) ----
        TriggerDeadZoneZInfo[] L2ModInfo { get; }
        TriggerDeadZoneZInfo[] R2ModInfo { get; }
        double[] L2Sens { get; }
        double[] R2Sens { get; }
        TriggerOutputSettings[] L2OutputSettings { get; }
        TriggerOutputSettings[] R2OutputSettings { get; }
        BezierCurve[] L2OutBezierCurveObj { get; }
        BezierCurve[] R2OutBezierCurveObj { get; }
        bool[] OutputVirtualTriggerButton { get; }
        DS4TriggerOutputMode[] OutputDS4TriggerMode { get; }
        int GetL2OutCurveMode(int index);
        void SetL2OutCurveMode(int index, int value);
        int GetR2OutCurveMode(int index);
        void SetR2OutCurveMode(int index, int value);

        // ---- Step10-2-A-3: タッチパッド関連 (m_Config委譲) ----
        byte[] TouchSensitivity { get; }
        byte[] TapSensitivity { get; }
        int[] TouchpadInvert { get; }
        bool[] TouchpadJitterCompensation { get; }
        bool[] TouchClickPassthru { get; }
        TouchButtonActivationMode[] TouchpadButtonMode { get; }
        bool[] StartTouchpadOff { get; }
        TouchpadOutMode[] TouchOutMode { get; }
        int[][] TouchDisInvertTriggers { get; }
        TouchMouseStickInfo[] TouchMouseStickInf { get; }
        TouchpadRelMouseSettings[] TouchRelMouse { get; }

        // ---- Step10-2-A-4: ジャイロ関連 (m_Config委譲) ----
        GyroMouseStickInfo[] GyroMouseStickInf { get; }
        GyroMouseInfo[] GyroMouseInfo { get; }
        GyroDirectionalSwipeInfo[] GyroSwipeInf { get; }
        GyroControlsInfo[] GyroControlsInf { get; }
        int[] GyroInvert { get; }
        int[] GyroSensitivity { get; }
        int[] GyroSensVerticalScale { get; }
        GyroOutMode[] GyroOutputMode { get; }
        bool[] GyroTriggerTurns { get; }
        bool[] GyroMouseStickTriggerTurns { get; }
        int[] GyroMouseHorizontalAxis { get; }
        int[] GyroMouseStickHorizontalAxis { get; }
        int[] GyroMouseDeadZone { get; }
        bool[] GyroMouseToggle { get; }
        bool[] GyroMouseStickToggle { get; }
        GyroOutMode GetGyroOutMode(int deviceIndex);
        bool GetGyroMouseStickTriggerTurns(int deviceIndex);
        int GetGyroMouseStickHorizontalAxis(int deviceIndex);
        GyroMouseStickInfo GetGyroMouseStickInfo(int deviceIndex);
        GyroDirectionalSwipeInfo GetGyroSwipeInfo(int deviceIndex);
        int GetGyroSensitivity(int deviceIndex);
        int GetGyroSensVerticalScale(int deviceIndex);
        int GetGyroInvert(int deviceIndex);
        bool GetGyroTriggerTurns(int deviceIndex);
        int GetGyroMouseHorizontalAxis(int deviceIndex);
        GyroControlsInfo GetGyroControlsInfo(int deviceIndex);
        void SetGyroMouseDeadZone(int index, int value, ControlService control);
        void SetGyroMouseToggle(int index, bool value, ControlService control);
        void SetGyroControlsToggle(int index, bool value, ControlService control);
        void SetGyroMouseStickToggle(int index, bool value, ControlService control);

        // ---- Step10-2-A-5: ライトバー・ランブル関連 (m_Config委譲) ----
        LightbarSettingInfo[] LightbarSettingsInfo { get; }
        bool[] InverseRumbleMotors { get; }
        byte[] RumbleBoost { get; }
        DualSenseDevice.RumbleEmulationMode[] DualSenseRumbleEmulationMode { get; set; }
        bool[] UseGenericRumbleStrRescaleForDualSenses { get; set; }
        byte[] DualSenseHapticPowerLevel { get; set; }
        LightbarSettingInfo GetLightbarSettingsInfo(int deviceIndex);
        byte GetRumbleBoost(int deviceIndex);
        int GetRumbleAutostopTime(int deviceIndex);
        ref DS4Color GetMainColor(int deviceIndex);
        void SetRumbleAutostopTime(int index, int value);

        // ---- Step10-2-A-6: ボタン/マウス出力関連 (m_Config委譲) ----
        ButtonMouseInfo[] ButtonMouseInfos { get; }
        bool[] EnableTouchToggle { get; }
        SteeringWheelSmoothingInfo[] WheelSmoothInfo { get; }
        bool[] DoubleTap { get; }
        int[] ScrollSensitivity { get; }
        bool[] TrackballMode { get; }
        double[] TrackballFriction { get; }
        bool GetEnableTouchToggle(int deviceIndex);
        bool GetDoubleTap(int deviceIndex);
        bool GetTrackballMode(int deviceIndex);
        double GetTrackballFriction(int deviceIndex);

        // ---- Step10-2-A-7: SA/デッドゾーン関連 (m_Config委譲) ----
        string[] SATriggers { get; }
        bool[] SATriggerCond { get; }
        string[] SAMousestickTriggers { get; }
        SASteeringWheelEmulationAxisType[] SASteeringWheelEmulationAxis { get; }
        int[] SASteeringWheelEmulationRange { get; }
        int[] SAWheelFuzzValues { get; }
        double[] SXDeadzone { get; }
        double[] SZDeadzone { get; }
        double[] SXSens { get; }
        double[] SZSens { get; }
        double[] SXMaxzone { get; }
        double[] SZMaxzone { get; }
        double[] SXAntiDeadzone { get; }
        double[] SZAntiDeadzone { get; }
        BezierCurve[] SxOutBezierCurveObj { get; }
        BezierCurve[] SzOutBezierCurveObj { get; }
        string GetSATriggers(int deviceIndex);
        bool GetSATriggerCond(int deviceIndex);
        string GetSAMouseStickTriggers(int deviceIndex);
        bool GetSAMouseStickTriggerCond(int deviceIndex);
        SASteeringWheelEmulationAxisType GetSASteeringWheelEmulationAxis(int deviceIndex);
        int GetSASteeringWheelEmulationRange(int deviceIndex);
        void SetSaTriggerCond(int index, string text);
        void SetSaMouseStickTriggerCond(int index, string text);
        int GetSxOutCurveMode(int index);
        void SetSxOutCurveMode(int index, int value);
        int GetSzOutCurveMode(int index);
        void SetSzOutCurveMode(int index, int value);

        // ---- Step10-2-A-8: 残余設定・デバイスオプション (m_Config委譲) ----
        int[] BTPollRate { get; }
        bool DS4Mapping { get; set; }
        bool[] DinputOnly { get; }
        int[] IdleDisconnectTimeout { get; }
        sbyte[] RightStickDriftXAxis { get; }
        sbyte[] RightStickDriftYAxis { get; }
        sbyte[] LeftStickDriftXAxis { get; }
        sbyte[] LeftStickDriftYAxis { get; }
        bool[] EnableOutputDataToDS4 { get; }

        /// <summary>
        /// プロファイルに永続化されているエミュレートコントローラー種別（&lt;OutputContDevice&gt;）。
        /// Global.OutContType（m_Config.outputDevType）への読み取り専用の薄い委譲。
        /// </summary>
        OutContType[] OutContType { get; }

        // ---- 課題④: ProfileActions インターフェースプロパティ ----
        List<string>[] ProfileActions { get; set; }

        bool UseDs3PitchRollSim { get; set; }
        bool[] LowerRCOn { get; }
        string[] LaunchProgram { get; }
        int GetBTPollRate(int deviceIndex);
        bool GetDInputOnly(int deviceIndex);
        int GetIdleDisconnectTimeout(int deviceIndex);
        bool GetEnableOutputDataToDS4(int deviceIndex);
        DS4ControlSettings GetDS4CSetting(int deviceIndex, string control);
        DS4ControlSettings GetDS4CSetting(int deviceIndex, DS4Controls control);

        // ---- Step10-2-A-9: Mapping.cs専用 ----
        bool ProfileChangedNotification { get; set; }
        int[] DebouncingMs { get; }
        VirtualKBMMapping OutputKBMMapping { get; set; }

        // ---- Phase6-Step2-2 (PR-2): 接続時のエクストラボタン再構築（Global への薄い委譲）----
        void RefreshExtrasButtons(int deviceIndex, List<DS4Controls> buttons);

        // ---- Phase6-Step2-5 (PR-5): カスタムアクション／エクストラの有無（毎レポート評価。割り当てなし）----
        bool ContainsCustomAction(int deviceIndex);
        bool ContainsCustomExtras(int deviceIndex);

        // ---- Phase6-Step3-1: デバイスの入力→出力割り当てグループ（毎レポート評価。割り当てなし）----
        ControlSettingsGroup GetControlSettingsGroup(int deviceIndex);
    }
}