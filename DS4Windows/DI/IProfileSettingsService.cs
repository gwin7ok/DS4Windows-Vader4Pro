using System;
using System.Globalization;
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

        event EventHandler<ProfileSettingChangedEventArgs> ProfileSettingChanged;

        void ResetToDefaults(int deviceIndex);
        void ResetAllToDefaults();
    }
}
