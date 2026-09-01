using System;
using System.Collections.Generic;
using System.Globalization;
using DS4Windows.DS4Control;
using DS4Windows.InputDevices;
using static DS4Windows.Mouse;

namespace DS4Windows.DI
{
    public class ProfileSettingChangedEventArgs : EventArgs
    {
        public int DeviceIndex { get; }
        public string SettingName { get; }
        public object OldValue { get; }
        public object NewValue { get; }

        public ProfileSettingChangedEventArgs(int deviceIndex, string settingName, object oldValue, object newValue)
        {
            DeviceIndex = deviceIndex;
            SettingName = settingName;
            OldValue = oldValue;
            NewValue = newValue;
        }
    }

    public interface IProfileSettingsService
    {
        CultureInfo ConfigDecimalCulture { get; }

        bool[] TouchpadActiveArray { get; set; }
        bool[] UseTempProfileArray { get; set; }
        string[] TempProfileNameArray { get; set; }
        bool[] TempProfileDistanceArray { get; set; }
        bool[] UseDInputOnlyArray { get; set; }
        bool[] LinkedProfileCheckArray { get; set; }

        int ProfileEditorLeftWidth { get; set; }
        int ProfileEditorRightWidth { get; set; }
        int ControllerSelectProfileColWidth { get; set; }
        int ControllerLinkedProfileColWidth { get; set; }
        int ControllerLinkProfIdColWidth { get; set; }

        bool GetTouchpadActive(int deviceIndex);
        void SetTouchpadActive(int deviceIndex, bool value);

        bool GetUseTempProfile(int deviceIndex);
        void SetUseTempProfile(int deviceIndex, bool value);

        string GetTempProfileName(int deviceIndex);
        void SetTempProfileName(int deviceIndex, string value);

        bool GetTempProfileDistance(int deviceIndex);
        void SetTempProfileDistance(int deviceIndex, bool value);

        bool GetUseDInputOnly(int deviceIndex);
        void SetUseDInputOnly(int deviceIndex, bool value);

        bool GetLinkedProfileCheck(int deviceIndex);
        void SetLinkedProfileCheck(int deviceIndex, bool value);

        X360Controls[] GetDefaultButtonMapping();
        DS4Controls[] GetReverseX360ButtonMapping();

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
        TouchpadAbsMouseSettings[] TouchAbsMouse { get; }
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
        int GetGyroMouseDeadZone(int deviceIndex);
        GyroControlsInfo GetGyroControlsInfo(int deviceIndex);
        void SetGyroMouseDeadZone(int index, int value, ControlService control);
        void SetGyroMouseToggle(int index, bool value, ControlService control);
        void SetGyroControlsToggle(int index, bool value, ControlService control);
        void SetGyroMouseStickToggle(int index, bool value, ControlService control);

        // ---- Step10-2-A-5: ライトバー・ランブル関連 (m_Config委譲) ----
        LightbarSettingInfo[] LightbarSettingsInfo { get; }
        bool[] InverseRumbleMotors { get; }
        byte[] RumbleBoost { get; }
        int[] RumbleAutostopTime { get; }
        DualSenseDevice.RumbleEmulationMode[] DualSenseRumbleEmulationMode { get; set; }
        bool[] UseGenericRumbleStrRescaleForDualSenses { get; set; }
        byte[] DualSenseHapticPowerLevel { get; set; }
        LightbarSettingInfo GetLightbarSettingsInfo(int deviceIndex);
        byte GetRumbleBoost(int deviceIndex);
        int GetRumbleAutostopTime(int deviceIndex);
        ref DS4Color GetMainColor(int deviceIndex);
        ref DS4Color GetLowColor(int deviceIndex);
        ref DS4Color GetChargingColor(int deviceIndex);
        ref DS4Color GetCustomColor(int deviceIndex);
        bool GetUseCustomLed(int deviceIndex);
        ref DS4Color GetFlashColor(int deviceIndex);
        void SetRumbleAutostopTime(int index, int value);

        // ---- Step10-2-A-6: ボタン/マウス出力関連 (m_Config委譲) ----
        ButtonMouseInfo[] ButtonMouseInfos { get; }
        ButtonAbsMouseInfo[] ButtonAbsMouseInfos { get; }
        bool[] EnableTouchToggle { get; }
        SteeringWheelSmoothingInfo[] WheelSmoothInfo { get; }
        bool[] DoubleTap { get; }
        int[] ScrollSensitivity { get; }
        bool[] TrackballMode { get; }
        double[] TrackballFriction { get; }
        bool GetEnableTouchToggle(int deviceIndex);
        bool GetDoubleTap(int deviceIndex);
        int[] GetScrollSensitivity();
        int GetScrollSensitivity(int deviceIndex);
        bool GetTrackballMode(int deviceIndex);
        double GetTrackballFriction(int deviceIndex);

        // ---- Step10-2-A-7: SA/デッドゾーン関連 (m_Config委譲) ----
        string[] SATriggers { get; }
        bool[] SATriggerCond { get; }
        string[] SAMousestickTriggers { get; }
        bool[] SAMouseStickTriggerCond { get; }
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
        bool UseDs3PitchRollSim { get; set; }
        bool[] LowerRCOn { get; }
        string[] LaunchProgram { get; }
        int GetBTPollRate(int deviceIndex);
        bool GetDInputOnly(int deviceIndex);
        int GetIdleDisconnectTimeout(int deviceIndex);
        bool GetEnableOutputDataToDS4(int deviceIndex);
        DS4ControlSettings GetDS4CSetting(int deviceIndex, string control);
        DS4ControlSettings GetDS4CSetting(int deviceIndex, DS4Controls control);
        List<DS4ControlSettings> GetDS4CSettings(int deviceIndex);

        // ---- Step10-2-A-9: Mapping.cs専用 ----
        bool ProfileChangedNotification { get; set; }
        int[] DebouncingMs { get; }
        VirtualKBMMapping OutputKBMMapping { get; set; }
        event EventHandler DebouncingMsChanged;
        void NotifyDebouncingMsChanged();

        event EventHandler<ProfileSettingChangedEventArgs> ProfileSettingChanged;

        void ResetToDefaults(int deviceIndex);
        void ResetAllToDefaults();
    }
}
