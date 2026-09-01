using System;
using System.Globalization;
using DS4Windows.DI;
using static DS4Windows.Mouse;

namespace DS4Windows
{
    public class ProfileSettingsService : IProfileSettingsService
    {
        private readonly object _syncLock = new object();
        public const int TEST_PROFILE_ITEM_COUNT = 9;
        public const int MAX_DS4_CONTROLLER_COUNT = 8;

        // Step10-2-A: m_Config(BackingStore)委譲用。Global.storeと同一インスタンスを参照する
        // (データの二重管理を避けるため、専用バックアップ配列は持たない)
        private readonly BackingStore _config;

        public ProfileSettingsService(BackingStore config = null)
        {
            _config = config ?? Global.store;
        }

        public CultureInfo ConfigDecimalCulture { get; } = new CultureInfo("en-US");

        private bool[] _touchpadActive = new bool[TEST_PROFILE_ITEM_COUNT] { true, true, true, true, true, true, true, true, true };
        private bool[] _useTempProfile = new bool[TEST_PROFILE_ITEM_COUNT] { false, false, false, false, false, false, false, false, false };
        private string[] _tempProfileName = new string[TEST_PROFILE_ITEM_COUNT] { string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty };
        private bool[] _tempProfileDistance = new bool[TEST_PROFILE_ITEM_COUNT] { false, false, false, false, false, false, false, false, false };
        private bool[] _useDInputOnly = new bool[TEST_PROFILE_ITEM_COUNT] { true, true, true, true, true, true, true, true, true };
        private bool[] _linkedProfileCheck = new bool[MAX_DS4_CONTROLLER_COUNT] { false, false, false, false, false, false, false, false };

        private int _profileEditorLeftWidth = 0;
        private int _profileEditorRightWidth = 0;
        private int _controllerSelectProfileColWidth = 0;
        private int _controllerLinkedProfileColWidth = 0;
        private int _controllerLinkProfIdColWidth = 0;

        public event EventHandler<ProfileSettingChangedEventArgs> ProfileSettingChanged;

        public bool[] TouchpadActiveArray
        {
            get => _touchpadActive;
            set
            {
                lock (_syncLock)
                {
                    _touchpadActive = value ?? new bool[TEST_PROFILE_ITEM_COUNT];
                }
            }
        }

        public bool[] UseTempProfileArray
        {
            get => _useTempProfile;
            set
            {
                lock (_syncLock)
                {
                    _useTempProfile = value ?? new bool[TEST_PROFILE_ITEM_COUNT];
                }
            }
        }

        public string[] TempProfileNameArray
        {
            get => _tempProfileName;
            set
            {
                lock (_syncLock)
                {
                    _tempProfileName = value ?? new string[TEST_PROFILE_ITEM_COUNT];
                }
            }
        }

        public bool[] TempProfileDistanceArray
        {
            get => _tempProfileDistance;
            set
            {
                lock (_syncLock)
                {
                    _tempProfileDistance = value ?? new bool[TEST_PROFILE_ITEM_COUNT];
                }
            }
        }

        public bool[] UseDInputOnlyArray
        {
            get => _useDInputOnly;
            set
            {
                lock (_syncLock)
                {
                    _useDInputOnly = value ?? new bool[TEST_PROFILE_ITEM_COUNT];
                }
            }
        }

        public bool[] LinkedProfileCheckArray
        {
            get => _linkedProfileCheck;
            set
            {
                lock (_syncLock)
                {
                    _linkedProfileCheck = value ?? new bool[MAX_DS4_CONTROLLER_COUNT];
                }
            }
        }

        public int ProfileEditorLeftWidth
        {
            get => _profileEditorLeftWidth;
            set
            {
                lock (_syncLock)
                {
                    if (_profileEditorLeftWidth != value)
                    {
                        var old = _profileEditorLeftWidth;
                        _profileEditorLeftWidth = value;
                        OnProfileSettingChanged(-1, nameof(ProfileEditorLeftWidth), old, value);
                    }
                }
            }
        }

        public int ProfileEditorRightWidth
        {
            get => _profileEditorRightWidth;
            set
            {
                lock (_syncLock)
                {
                    if (_profileEditorRightWidth != value)
                    {
                        var old = _profileEditorRightWidth;
                        _profileEditorRightWidth = value;
                        OnProfileSettingChanged(-1, nameof(ProfileEditorRightWidth), old, value);
                    }
                }
            }
        }

        public int ControllerSelectProfileColWidth
        {
            get => _controllerSelectProfileColWidth;
            set
            {
                lock (_syncLock)
                {
                    if (_controllerSelectProfileColWidth != value)
                    {
                        var old = _controllerSelectProfileColWidth;
                        _controllerSelectProfileColWidth = value;
                        OnProfileSettingChanged(-1, nameof(ControllerSelectProfileColWidth), old, value);
                    }
                }
            }
        }

        public int ControllerLinkedProfileColWidth
        {
            get => _controllerLinkedProfileColWidth;
            set
            {
                lock (_syncLock)
                {
                    if (_controllerLinkedProfileColWidth != value)
                    {
                        var old = _controllerLinkedProfileColWidth;
                        _controllerLinkedProfileColWidth = value;
                        OnProfileSettingChanged(-1, nameof(ControllerLinkedProfileColWidth), old, value);
                    }
                }
            }
        }

        public int ControllerLinkProfIdColWidth
        {
            get => _controllerLinkProfIdColWidth;
            set
            {
                lock (_syncLock)
                {
                    if (_controllerLinkProfIdColWidth != value)
                    {
                        var old = _controllerLinkProfIdColWidth;
                        _controllerLinkProfIdColWidth = value;
                        OnProfileSettingChanged(-1, nameof(ControllerLinkProfIdColWidth), old, value);
                    }
                }
            }
        }

        public bool GetTouchpadActive(int deviceIndex)
        {
            if (deviceIndex >= 0 && deviceIndex < _touchpadActive.Length)
                return _touchpadActive[deviceIndex];
            return true;
        }

        public void SetTouchpadActive(int deviceIndex, bool value)
        {
            if (deviceIndex >= 0 && deviceIndex < _touchpadActive.Length)
            {
                lock (_syncLock)
                {
                    var old = _touchpadActive[deviceIndex];
                    if (old != value)
                    {
                        _touchpadActive[deviceIndex] = value;
                        OnProfileSettingChanged(deviceIndex, nameof(TouchpadActiveArray), old, value);
                    }
                }
            }
        }

        public bool GetUseTempProfile(int deviceIndex)
        {
            if (deviceIndex >= 0 && deviceIndex < _useTempProfile.Length)
                return _useTempProfile[deviceIndex];
            return false;
        }

        public void SetUseTempProfile(int deviceIndex, bool value)
        {
            if (deviceIndex >= 0 && deviceIndex < _useTempProfile.Length)
            {
                lock (_syncLock)
                {
                    var old = _useTempProfile[deviceIndex];
                    if (old != value)
                    {
                        _useTempProfile[deviceIndex] = value;
                        OnProfileSettingChanged(deviceIndex, nameof(UseTempProfileArray), old, value);
                    }
                }
            }
        }

        public string GetTempProfileName(int deviceIndex)
        {
            if (deviceIndex >= 0 && deviceIndex < _tempProfileName.Length)
                return _tempProfileName[deviceIndex];
            return string.Empty;
        }

        public void SetTempProfileName(int deviceIndex, string value)
        {
            if (deviceIndex >= 0 && deviceIndex < _tempProfileName.Length)
            {
                lock (_syncLock)
                {
                    var old = _tempProfileName[deviceIndex];
                    if (old != value)
                    {
                        _tempProfileName[deviceIndex] = value ?? string.Empty;
                        OnProfileSettingChanged(deviceIndex, nameof(TempProfileNameArray), old, value);
                    }
                }
            }
        }

        public bool GetTempProfileDistance(int deviceIndex)
        {
            if (deviceIndex >= 0 && deviceIndex < _tempProfileDistance.Length)
                return _tempProfileDistance[deviceIndex];
            return false;
        }

        public void SetTempProfileDistance(int deviceIndex, bool value)
        {
            if (deviceIndex >= 0 && deviceIndex < _tempProfileDistance.Length)
            {
                lock (_syncLock)
                {
                    var old = _tempProfileDistance[deviceIndex];
                    if (old != value)
                    {
                        _tempProfileDistance[deviceIndex] = value;
                        OnProfileSettingChanged(deviceIndex, nameof(TempProfileDistanceArray), old, value);
                    }
                }
            }
        }

        public bool GetUseDInputOnly(int deviceIndex)
        {
            if (deviceIndex >= 0 && deviceIndex < _useDInputOnly.Length)
                return _useDInputOnly[deviceIndex];
            return true;
        }

        public void SetUseDInputOnly(int deviceIndex, bool value)
        {
            if (deviceIndex >= 0 && deviceIndex < _useDInputOnly.Length)
            {
                lock (_syncLock)
                {
                    var old = _useDInputOnly[deviceIndex];
                    if (old != value)
                    {
                        _useDInputOnly[deviceIndex] = value;
                        OnProfileSettingChanged(deviceIndex, nameof(UseDInputOnlyArray), old, value);
                    }
                }
            }
        }

        public bool GetLinkedProfileCheck(int deviceIndex)
        {
            if (deviceIndex >= 0 && deviceIndex < _linkedProfileCheck.Length)
                return _linkedProfileCheck[deviceIndex];
            return false;
        }

        public void SetLinkedProfileCheck(int deviceIndex, bool value)
        {
            if (deviceIndex >= 0 && deviceIndex < _linkedProfileCheck.Length)
            {
                lock (_syncLock)
                {
                    var old = _linkedProfileCheck[deviceIndex];
                    if (old != value)
                    {
                        _linkedProfileCheck[deviceIndex] = value;
                        OnProfileSettingChanged(deviceIndex, nameof(LinkedProfileCheckArray), old, value);
                    }
                }
            }
        }

        // ---- Step10-2-A-1: スティック関連 (m_Config委譲) ----
        public StickDeadZoneInfo[] LSModInfo => _config.lsModInfo;
        public StickDeadZoneInfo[] RSModInfo => _config.rsModInfo;
        public double[] LSRotation => _config.LSRotation;
        public double[] RSRotation => _config.RSRotation;
        public double[] LSSens => _config.LSSens;
        public double[] RSSens => _config.RSSens;
        public SquareStickInfo[] SquStickInfo => _config.squStickInfo;
        public StickAntiSnapbackInfo[] LSAntiSnapbackInfo => _config.lsAntiSnapbackInfo;
        public StickAntiSnapbackInfo[] RSAntiSnapbackInfo => _config.rsAntiSnapbackInfo;
        public StickOutputSetting[] LSOutputSettings => _config.lsOutputSettings;
        public StickOutputSetting[] RSOutputSettings => _config.rsOutputSettings;
        public BezierCurve[] LsOutBezierCurveObj => _config.lsOutBezierCurveObj;
        public BezierCurve[] RsOutBezierCurveObj => _config.rsOutBezierCurveObj;

        public int GetLsOutCurveMode(int index) => _config.getLsOutCurveMode(index);

        public void SetLsOutCurveMode(int index, int value)
        {
            _config.setLsOutCurveMode(index, value);
            AppLogger.LogToGui($"[DI] ProfileSettingsService.SetLsOutCurveMode: Slot {index} = {value}", false, true);
        }

        public int GetRsOutCurveMode(int index) => _config.getRsOutCurveMode(index);

        public void SetRsOutCurveMode(int index, int value)
        {
            _config.setRsOutCurveMode(index, value);
            AppLogger.LogToGui($"[DI] ProfileSettingsService.SetRsOutCurveMode: Slot {index} = {value}", false, true);
        }

        // ---- Step10-2-A-2: トリガー(L2/R2)関連 (m_Config委譲) ----
        public TriggerDeadZoneZInfo[] L2ModInfo => _config.l2ModInfo;
        public TriggerDeadZoneZInfo[] R2ModInfo => _config.r2ModInfo;
        public double[] L2Sens => _config.l2Sens;
        public double[] R2Sens => _config.r2Sens;
        public TriggerOutputSettings[] L2OutputSettings => _config.l2OutputSettings;
        public TriggerOutputSettings[] R2OutputSettings => _config.r2OutputSettings;
        public BezierCurve[] L2OutBezierCurveObj => _config.l2OutBezierCurveObj;
        public BezierCurve[] R2OutBezierCurveObj => _config.r2OutBezierCurveObj;
        public bool[] OutputVirtualTriggerButton => _config.outputVirtualTriggerButtons;
        public DS4TriggerOutputMode[] OutputDS4TriggerMode => _config.outputDS4TriggerMode;

        public int GetL2OutCurveMode(int index) => _config.getL2OutCurveMode(index);

        public void SetL2OutCurveMode(int index, int value)
        {
            _config.setL2OutCurveMode(index, value);
            AppLogger.LogToGui($"[DI] ProfileSettingsService.SetL2OutCurveMode: Slot {index} = {value}", false, true);
        }

        public int GetR2OutCurveMode(int index) => _config.getR2OutCurveMode(index);

        public void SetR2OutCurveMode(int index, int value)
        {
            _config.setR2OutCurveMode(index, value);
            AppLogger.LogToGui($"[DI] ProfileSettingsService.SetR2OutCurveMode: Slot {index} = {value}", false, true);
        }

        // ---- Step10-2-A-3: タッチパッド関連 (m_Config委譲) ----
        public byte[] TouchSensitivity => _config.touchSensitivity;
        public byte[] TapSensitivity => _config.tapSensitivity;
        public int[] TouchpadInvert => _config.touchpadInvert;
        public bool[] TouchpadJitterCompensation => _config.touchpadJitterCompensation;
        public bool[] TouchClickPassthru => _config.touchClickPassthru;
        public TouchButtonActivationMode[] TouchpadButtonMode => _config.touchpadButtonMode;
        public bool[] StartTouchpadOff => _config.startTouchpadOff;
        public TouchpadOutMode[] TouchOutMode => _config.touchOutMode;
        public int[][] TouchDisInvertTriggers => _config.touchDisInvertTriggers;
        public TouchMouseStickInfo[] TouchMouseStickInf => _config.touchMStickInfo;
        public TouchpadAbsMouseSettings[] TouchAbsMouse => _config.touchpadAbsMouse;
        public TouchpadRelMouseSettings[] TouchRelMouse => _config.touchpadRelMouse;

        public X360Controls[] GetDefaultButtonMapping()
        {
            return (X360Controls[])Global.defaultButtonMapping.Clone();
        }

        public DS4Controls[] GetReverseX360ButtonMapping()
        {
            return (DS4Controls[])Global.reverseX360ButtonMapping.Clone();
        }

        public void ResetToDefaults(int deviceIndex)
        {
            if (deviceIndex >= 0 && deviceIndex < TEST_PROFILE_ITEM_COUNT)
            {
                lock (_syncLock)
                {
                    _touchpadActive[deviceIndex] = true;
                    _useTempProfile[deviceIndex] = false;
                    _tempProfileName[deviceIndex] = string.Empty;
                    _tempProfileDistance[deviceIndex] = false;
                    _useDInputOnly[deviceIndex] = true;
                    if (deviceIndex < MAX_DS4_CONTROLLER_COUNT)
                    {
                        _linkedProfileCheck[deviceIndex] = false;
                    }
                    OnProfileSettingChanged(deviceIndex, "ResetToDefaults", null, null);
                }
            }
        }

        public void ResetAllToDefaults()
        {
            lock (_syncLock)
            {
                for (int i = 0; i < TEST_PROFILE_ITEM_COUNT; i++)
                {
                    ResetToDefaults(i);
                }
            }
        }

        protected virtual void OnProfileSettingChanged(int deviceIndex, string settingName, object oldValue, object newValue)
        {
            AppLogger.LogToGui($"[DI] ProfileSettingsService.SettingChanged: Slot {deviceIndex}, {settingName}", false, true);
            ProfileSettingChanged?.Invoke(this, new ProfileSettingChangedEventArgs(deviceIndex, settingName, oldValue, newValue));
        }
    }
}
