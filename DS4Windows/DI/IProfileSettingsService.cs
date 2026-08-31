using System;
using System.Globalization;

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

        event EventHandler<ProfileSettingChangedEventArgs> ProfileSettingChanged;

        void ResetToDefaults(int deviceIndex);
        void ResetAllToDefaults();
    }
}
