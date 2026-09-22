using DS4Windows.Services;
using System;
using System.Collections.Generic;
using System.Globalization;
using DS4Windows.DI;
using DS4Windows.DS4Control;
using DS4Windows.InputDevices;
using DS4WinWPF.DS4Control;
using static DS4Windows.Mouse;

namespace DS4Windows
{
    public class ProfileSettingsService : IProfileSettingsService
    {
        private readonly object _syncLock = new object();
        public const int TEST_PROFILE_ITEM_COUNT = 9;
        public const int MAX_DS4_CONTROLLER_COUNT = 8;

        // 課題①: スロットごとのサブ設定イベント購読解除アクション一覧
        private readonly Dictionary<int, List<Action>> _subSettingsUnwireMap = new Dictionary<int, List<Action>>();

        // Step10-2-A: m_Config(BackingStore)委譲用。Global.storeと同一インスタンスを参照する
        // (データの二重管理を避けるため、専用バックアップ配列は持たない)
        private readonly BackingStore _config;
        private BackingStore SafeConfig => _config ?? Global.store;

        // Phase6-Step2-1c (決定O2=C/O3=A): スロット上限（現在接続台数ではない）を IEnvironmentService から取得する。
        // 既存の生成箇所（テスト・Global のフォールバック）を変更しないよう省略可能とし、
        // 省略時は状態を持たない既定実装を用いる（AppHost.Services は使用しない）。
        private readonly IEnvironmentService _environmentService;

        public ProfileSettingsService(BackingStore backingStore = null,
            IEnvironmentService environmentService = null)
        {
            _config = backingStore ?? Global.store;
            _environmentService = environmentService ?? new EnvironmentService();
        }

        public CultureInfo ConfigDecimalCulture { get; } = new CultureInfo("en-US");

        private bool[] _touchpadActive = new bool[TEST_PROFILE_ITEM_COUNT] { true, true, true, true, true, true, true, true, true };
        private bool[] _useTempProfile = new bool[TEST_PROFILE_ITEM_COUNT] { false, false, false, false, false, false, false, false, false };
        private string[] _tempProfileName = new string[TEST_PROFILE_ITEM_COUNT] { string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty };
        private bool[] _tempProfileDistance = new bool[TEST_PROFILE_ITEM_COUNT] { false, false, false, false, false, false, false, false, false };
        private bool[] _useDInputOnly = new bool[TEST_PROFILE_ITEM_COUNT] { true, true, true, true, true, true, true, true, true };
        private bool[] _linkedProfileCheck = new bool[MAX_DS4_CONTROLLER_COUNT] { false, false, false, false, false, false, false, false };

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
        public StickDeadZoneInfo[] LSModInfo => SafeConfig?.lsModInfo;
        public StickDeadZoneInfo[] RSModInfo => SafeConfig?.rsModInfo;
        public double[] LSRotation => SafeConfig?.LSRotation;
        public double[] RSRotation => SafeConfig?.RSRotation;
        public double[] LSSens => SafeConfig?.LSSens;
        public double[] RSSens => SafeConfig?.RSSens;
        public SquareStickInfo[] SquStickInfo => SafeConfig?.squStickInfo;
        public StickAntiSnapbackInfo[] LSAntiSnapbackInfo => SafeConfig?.lsAntiSnapbackInfo;
        public StickAntiSnapbackInfo[] RSAntiSnapbackInfo => SafeConfig?.rsAntiSnapbackInfo;
        public StickOutputSetting[] LSOutputSettings => SafeConfig?.lsOutputSettings;
        public StickOutputSetting[] RSOutputSettings => SafeConfig?.rsOutputSettings;
        public BezierCurve[] LsOutBezierCurveObj => SafeConfig?.lsOutBezierCurveObj;
        public BezierCurve[] RsOutBezierCurveObj => SafeConfig?.rsOutBezierCurveObj;

        public int GetLsOutCurveMode(int index) => SafeConfig != null ? SafeConfig.getLsOutCurveMode(index) : 0;

        public void SetLsOutCurveMode(int index, int value)
        {
            if (SafeConfig != null)
            {
                SafeConfig.setLsOutCurveMode(index, value);
                AppLogger.LogToGui($"[DI] ProfileSettingsService.SetLsOutCurveMode: Slot {index} = {value}", false, true);
            }
        }

        public int GetRsOutCurveMode(int index) => SafeConfig != null ? SafeConfig.getRsOutCurveMode(index) : 0;

        public void SetRsOutCurveMode(int index, int value)
        {
            if (SafeConfig != null)
            {
                SafeConfig.setRsOutCurveMode(index, value);
                AppLogger.LogToGui($"[DI] ProfileSettingsService.SetRsOutCurveMode: Slot {index} = {value}", false, true);
            }
        }

        // ---- Step10-2-A-2: トリガー(L2/R2)関連 (m_Config委譲) ----
        public TriggerDeadZoneZInfo[] L2ModInfo => SafeConfig?.l2ModInfo;
        public TriggerDeadZoneZInfo[] R2ModInfo => SafeConfig?.r2ModInfo;
        public double[] L2Sens => SafeConfig?.l2Sens;
        public double[] R2Sens => SafeConfig?.r2Sens;
        public TriggerOutputSettings[] L2OutputSettings => SafeConfig?.l2OutputSettings;
        public TriggerOutputSettings[] R2OutputSettings => SafeConfig?.r2OutputSettings;
        public BezierCurve[] L2OutBezierCurveObj => SafeConfig?.l2OutBezierCurveObj;
        public BezierCurve[] R2OutBezierCurveObj => SafeConfig?.r2OutBezierCurveObj;
        public bool[] OutputVirtualTriggerButton => SafeConfig?.outputVirtualTriggerButtons;
        public DS4TriggerOutputMode[] OutputDS4TriggerMode => SafeConfig?.outputDS4TriggerMode;

        public int GetL2OutCurveMode(int index) => SafeConfig != null ? SafeConfig.getL2OutCurveMode(index) : 0;

        public void SetL2OutCurveMode(int index, int value)
        {
            if (SafeConfig != null)
            {
                SafeConfig.setL2OutCurveMode(index, value);
                AppLogger.LogToGui($"[DI] ProfileSettingsService.SetL2OutCurveMode: Slot {index} = {value}", false, true);
            }
        }

        public int GetR2OutCurveMode(int index) => SafeConfig != null ? SafeConfig.getR2OutCurveMode(index) : 0;

        public void SetR2OutCurveMode(int index, int value)
        {
            if (SafeConfig != null)
            {
                SafeConfig.setR2OutCurveMode(index, value);
                AppLogger.LogToGui($"[DI] ProfileSettingsService.SetR2OutCurveMode: Slot {index} = {value}", false, true);
            }
        }

        // ---- Step10-2-A-3: タッチパッド関連 (m_Config委譲) ----
        public byte[] TouchSensitivity => SafeConfig?.touchSensitivity;
        public byte[] TapSensitivity => SafeConfig?.tapSensitivity;
        public int[] TouchpadInvert => SafeConfig?.touchpadInvert;
        public bool[] TouchpadJitterCompensation => SafeConfig?.touchpadJitterCompensation;
        public bool[] TouchClickPassthru => SafeConfig?.touchClickPassthru;
        public TouchButtonActivationMode[] TouchpadButtonMode => SafeConfig?.touchpadButtonMode;
        public bool[] StartTouchpadOff => SafeConfig?.startTouchpadOff;
        public TouchpadOutMode[] TouchOutMode => SafeConfig?.touchOutMode;
        public int[][] TouchDisInvertTriggers => SafeConfig?.touchDisInvertTriggers;
        public TouchMouseStickInfo[] TouchMouseStickInf => SafeConfig?.touchMStickInfo;
        public TouchpadAbsMouseSettings[] TouchAbsMouse => SafeConfig?.touchpadAbsMouse;
        public TouchpadRelMouseSettings[] TouchRelMouse => SafeConfig?.touchpadRelMouse;

        // ---- Step10-2-A-4: ジャイロ関連 (m_Config委譲) ----
        public GyroMouseStickInfo[] GyroMouseStickInf => SafeConfig?.gyroMStickInfo;
        public GyroMouseInfo[] GyroMouseInfo => SafeConfig?.gyroMouseInfo;
        public GyroDirectionalSwipeInfo[] GyroSwipeInf => SafeConfig?.gyroSwipeInfo;
        public GyroControlsInfo[] GyroControlsInf => SafeConfig?.gyroControlsInf;
        public int[] GyroInvert => SafeConfig?.gyroInvert;
        public int[] GyroSensitivity => SafeConfig?.gyroSensitivity;
        public int[] GyroSensVerticalScale => SafeConfig?.gyroSensVerticalScale;
        public GyroOutMode[] GyroOutputMode => SafeConfig?.gyroOutMode;
        public bool[] GyroTriggerTurns => SafeConfig?.gyroTriggerTurns;
        public bool[] GyroMouseStickTriggerTurns => SafeConfig?.gyroMouseStickTriggerTurns;
        public int[] GyroMouseHorizontalAxis => SafeConfig?.gyroMouseHorizontalAxis;
        public int[] GyroMouseStickHorizontalAxis => SafeConfig?.gyroMouseStickHorizontalAxis;
        public int[] GyroMouseDeadZone => SafeConfig?.gyroMouseDZ;
        public bool[] GyroMouseToggle => SafeConfig?.gyroMouseToggle;
        public bool[] GyroMouseStickToggle => SafeConfig?.gyroMouseStickToggle;

        public GyroOutMode GetGyroOutMode(int deviceIndex) => SafeConfig != null && SafeConfig.gyroOutMode != null ? SafeConfig.gyroOutMode[deviceIndex] : GyroOutMode.None;
        public bool GetGyroMouseStickTriggerTurns(int deviceIndex) => SafeConfig != null && SafeConfig.gyroMouseStickTriggerTurns != null && SafeConfig.gyroMouseStickTriggerTurns[deviceIndex];
        public int GetGyroMouseStickHorizontalAxis(int deviceIndex) => SafeConfig != null && SafeConfig.gyroMouseStickHorizontalAxis != null ? SafeConfig.gyroMouseStickHorizontalAxis[deviceIndex] : 0;
        public GyroMouseStickInfo GetGyroMouseStickInfo(int deviceIndex) => SafeConfig?.gyroMStickInfo?[deviceIndex];
        public GyroDirectionalSwipeInfo GetGyroSwipeInfo(int deviceIndex) => SafeConfig?.gyroSwipeInfo?[deviceIndex];
        public int GetGyroSensitivity(int deviceIndex) => SafeConfig != null && SafeConfig.gyroSensitivity != null ? SafeConfig.gyroSensitivity[deviceIndex] : 0;
        public int GetGyroSensVerticalScale(int deviceIndex) => SafeConfig != null && SafeConfig.gyroSensVerticalScale != null ? SafeConfig.gyroSensVerticalScale[deviceIndex] : 0;
        public int GetGyroInvert(int deviceIndex) => SafeConfig != null && SafeConfig.gyroInvert != null ? SafeConfig.gyroInvert[deviceIndex] : 0;
        public bool GetGyroTriggerTurns(int deviceIndex) => SafeConfig != null && SafeConfig.gyroTriggerTurns != null && SafeConfig.gyroTriggerTurns[deviceIndex];
        public int GetGyroMouseHorizontalAxis(int deviceIndex) => SafeConfig != null && SafeConfig.gyroMouseHorizontalAxis != null ? SafeConfig.gyroMouseHorizontalAxis[deviceIndex] : 0;
        public int GetGyroMouseDeadZone(int deviceIndex) => SafeConfig != null && SafeConfig.gyroMouseDZ != null ? SafeConfig.gyroMouseDZ[deviceIndex] : 0;
        public GyroControlsInfo GetGyroControlsInfo(int deviceIndex) => SafeConfig?.gyroControlsInf?[deviceIndex];

        public void SetGyroMouseDeadZone(int index, int value, ControlService control)
            => SafeConfig?.SetGyroMouseDZ(index, value, control);

        public void SetGyroMouseToggle(int index, bool value, ControlService control)
            => SafeConfig?.SetGyroMouseToggle(index, value, control);

        public void SetGyroControlsToggle(int index, bool value, ControlService control)
            => SafeConfig?.SetGyroControlsToggle(index, value, control);

        public void SetGyroMouseStickToggle(int index, bool value, ControlService control)
            => SafeConfig?.SetGyroMouseStickToggle(index, value, control);

        // ---- Step10-2-A-5: ライトバー・ランブル関連 (m_Config委譲) ----
        public LightbarSettingInfo[] LightbarSettingsInfo => SafeConfig?.lightbarSettingInfo;
        public bool[] InverseRumbleMotors => SafeConfig?.inverseRumbleMotors;

        public byte[] RumbleBoost
        {
            get => SafeConfig?.rumble;
            set
            {
                if (SafeConfig != null) SafeConfig.rumble = value;
            }
        }

        public int[] RumbleAutostopTime
        {
            get => SafeConfig?.rumbleAutostopTime;
            set
            {
                if (SafeConfig != null) SafeConfig.rumbleAutostopTime = value;
            }
        }

        // 選択肢A: BackingStore への直接委譲（純粋な薄いパススルー）
        public RumbleSettings[] RumbleSettings => SafeConfig?.rumbleSettings;

        public DualSenseDevice.RumbleEmulationMode[] DualSenseRumbleEmulationMode
        {
            get => SafeConfig?.dualSenseRumbleEmulationMode;
            set
            {
                if (SafeConfig != null) SafeConfig.dualSenseRumbleEmulationMode = value;
            }
        }
        public bool[] UseGenericRumbleStrRescaleForDualSenses
        {
            get => SafeConfig?.useGenericRumbleRescaleForDualSenses;
            set
            {
                if (SafeConfig != null) SafeConfig.useGenericRumbleRescaleForDualSenses = value;
            }
        }
        public byte[] DualSenseHapticPowerLevel
        {
            get => SafeConfig?.dualSenseHapticPowerLevel;
            set
            {
                if (SafeConfig != null) SafeConfig.dualSenseHapticPowerLevel = value;
            }
        }

        public LightbarSettingInfo GetLightbarSettingsInfo(int deviceIndex) => SafeConfig?.lightbarSettingInfo?[deviceIndex];

        public byte GetRumbleBoost(int deviceIndex)
        {
            if (Program.rootHub != null && Program.rootHub.DS4Controllers != null &&
                Program.rootHub.DS4Controllers[deviceIndex] is DualSenseDevice &&
                UseGenericRumbleStrRescaleForDualSenses != null &&
                !UseGenericRumbleStrRescaleForDualSenses[deviceIndex])
                return 100;

            if (SafeConfig?.rumbleSettings != null && deviceIndex >= 0 && deviceIndex < SafeConfig.rumbleSettings.Length)
                return SafeConfig.rumbleSettings[deviceIndex].RumbleBoost;

            return SafeConfig?.rumble != null ? SafeConfig.rumble[deviceIndex] : (byte)100;
        }

        public int GetRumbleAutostopTime(int deviceIndex)
        {
            if (SafeConfig?.rumbleSettings != null && deviceIndex >= 0 && deviceIndex < SafeConfig.rumbleSettings.Length)
                return SafeConfig.rumbleSettings[deviceIndex].RumbleAutostopTime;

            return SafeConfig?.rumbleAutostopTime != null ? SafeConfig.rumbleAutostopTime[deviceIndex] : 0;
        }

        public ref DS4Color GetMainColor(int deviceIndex) => ref SafeConfig.lightbarSettingInfo[deviceIndex].ds4winSettings.m_Led;
        public ref DS4Color GetLowColor(int deviceIndex) => ref SafeConfig.lightbarSettingInfo[deviceIndex].ds4winSettings.m_LowLed;
        public ref DS4Color GetChargingColor(int deviceIndex) => ref SafeConfig.lightbarSettingInfo[deviceIndex].ds4winSettings.m_ChargingLed;
        public ref DS4Color GetCustomColor(int deviceIndex) => ref SafeConfig.lightbarSettingInfo[deviceIndex].ds4winSettings.m_CustomLed;
        public bool GetUseCustomLed(int deviceIndex) => SafeConfig != null && SafeConfig.lightbarSettingInfo[deviceIndex].ds4winSettings.useCustomLed;
        public ref DS4Color GetFlashColor(int deviceIndex) => ref SafeConfig.lightbarSettingInfo[deviceIndex].ds4winSettings.m_FlashLed;

        public void SetRumbleAutostopTime(int index, int value)
        {
            if (SafeConfig?.rumbleSettings != null && index >= 0 && index < SafeConfig.rumbleSettings.Length)
            {
                SafeConfig.rumbleSettings[index].RumbleAutostopTime = value;
            }
            if (SafeConfig != null && SafeConfig.rumbleAutostopTime != null && index < SafeConfig.rumbleAutostopTime.Length)
            {
                SafeConfig.rumbleAutostopTime[index] = value;
                DS4Device tempDev = Program.rootHub?.DS4Controllers?[index];
                if (tempDev != null && tempDev.isSynced())
                    tempDev.RumbleAutostopTime = value;
            }
        }

        // ---- Step10-2-A-6: ボタン/マウス出力関連 (m_Config委譲) ----
        public ButtonMouseInfo[] ButtonMouseInfos => SafeConfig?.buttonMouseInfos;
        public ButtonAbsMouseInfo[] ButtonAbsMouseInfos => SafeConfig?.buttonAbsMouseInfos;
        public bool[] EnableTouchToggle => SafeConfig?.enableTouchToggle;
        public SteeringWheelSmoothingInfo[] WheelSmoothInfo => SafeConfig?.wheelSmoothInfo;
        public bool[] DoubleTap => SafeConfig?.doubleTap;
        public int[] ScrollSensitivity => SafeConfig?.scrollSensitivity;
        public bool[] TrackballMode => SafeConfig?.trackballMode;
        public double[] TrackballFriction => SafeConfig?.trackballFriction;
        public bool GetEnableTouchToggle(int deviceIndex) => SafeConfig != null && SafeConfig.enableTouchToggle != null && SafeConfig.enableTouchToggle[deviceIndex];
        public bool GetDoubleTap(int deviceIndex) => SafeConfig != null && SafeConfig.doubleTap != null && SafeConfig.doubleTap[deviceIndex];
        public int[] GetScrollSensitivity() => SafeConfig?.scrollSensitivity;
        public int GetScrollSensitivity(int deviceIndex) => SafeConfig != null && SafeConfig.scrollSensitivity != null ? SafeConfig.scrollSensitivity[deviceIndex] : 0;
        public bool GetTrackballMode(int deviceIndex) => SafeConfig != null && SafeConfig.trackballMode != null && SafeConfig.trackballMode[deviceIndex];
        public double GetTrackballFriction(int deviceIndex) => SafeConfig != null && SafeConfig.trackballFriction != null ? SafeConfig.trackballFriction[deviceIndex] : 0;

        // ---- Step10-2-A-7: SA/デッドゾーン関連 (m_Config委譲) ----
        public string[] SATriggers => SafeConfig?.sATriggers;
        public bool[] SATriggerCond => SafeConfig?.sATriggerCond;
        public string[] SAMousestickTriggers => SafeConfig?.sAMouseStickTriggers;
        public bool[] SAMouseStickTriggerCond => SafeConfig?.sAMouseStickTriggerCond;
        public SASteeringWheelEmulationAxisType[] SASteeringWheelEmulationAxis => SafeConfig?.sASteeringWheelEmulationAxis;
        public int[] SASteeringWheelEmulationRange => SafeConfig?.sASteeringWheelEmulationRange;
        public int[] SAWheelFuzzValues => SafeConfig?.saWheelFuzzValues;
        public double[] SXDeadzone => SafeConfig?.SXDeadzone;
        public double[] SZDeadzone => SafeConfig?.SZDeadzone;
        public double[] SXSens => SafeConfig?.SXSens;
        public double[] SZSens => SafeConfig?.SZSens;
        public double[] SXMaxzone => SafeConfig?.SXMaxzone;
        public double[] SZMaxzone => SafeConfig?.SZMaxzone;
        public double[] SXAntiDeadzone => SafeConfig?.SXAntiDeadzone;
        public double[] SZAntiDeadzone => SafeConfig?.SZAntiDeadzone;
        public BezierCurve[] SxOutBezierCurveObj => SafeConfig?.sxOutBezierCurveObj;
        public BezierCurve[] SzOutBezierCurveObj => SafeConfig?.szOutBezierCurveObj;
        public string GetSATriggers(int deviceIndex) => SafeConfig != null && SafeConfig.sATriggers != null ? SafeConfig.sATriggers[deviceIndex] : string.Empty;
        public bool GetSATriggerCond(int deviceIndex) => SafeConfig != null && SafeConfig.sATriggerCond != null && SafeConfig.sATriggerCond[deviceIndex];
        public string GetSAMouseStickTriggers(int deviceIndex) => SafeConfig != null && SafeConfig.sAMouseStickTriggers != null ? SafeConfig.sAMouseStickTriggers[deviceIndex] : string.Empty;
        public bool GetSAMouseStickTriggerCond(int deviceIndex) => SafeConfig != null && SafeConfig.sAMouseStickTriggerCond != null && SafeConfig.sAMouseStickTriggerCond[deviceIndex];
        public SASteeringWheelEmulationAxisType GetSASteeringWheelEmulationAxis(int deviceIndex) => SafeConfig != null && SafeConfig.sASteeringWheelEmulationAxis != null ? SafeConfig.sASteeringWheelEmulationAxis[deviceIndex] : SASteeringWheelEmulationAxisType.None;
        public int GetSASteeringWheelEmulationRange(int deviceIndex) => SafeConfig != null && SafeConfig.sASteeringWheelEmulationRange != null ? SafeConfig.sASteeringWheelEmulationRange[deviceIndex] : 0;
        public void SetSaTriggerCond(int index, string text) => SafeConfig?.SetSaTriggerCond(index, text);
        public void SetSaMouseStickTriggerCond(int index, string text) => SafeConfig?.SetSaMouseStickTriggerCond(index, text);
        public int GetSxOutCurveMode(int index) => SafeConfig != null ? SafeConfig.getSXOutCurveMode(index) : 0;
        public void SetSxOutCurveMode(int index, int value) => SafeConfig?.setSXOutCurveMode(index, value);
        public int GetSzOutCurveMode(int index) => SafeConfig != null ? SafeConfig.getSZOutCurveMode(index) : 0;
        public void SetSzOutCurveMode(int index, int value) => SafeConfig?.setSZOutCurveMode(index, value);

        // ---- Step10-2-A-8: 残余設定・デバイスオプション (m_Config委譲) ----
        public int[] BTPollRate => SafeConfig?.btPollRate;
        public bool DS4Mapping
        {
            get => SafeConfig != null && SafeConfig.ds4Mapping;
            set
            {
                if (SafeConfig != null) SafeConfig.ds4Mapping = value;
            }
        }
        public bool[] DinputOnly => SafeConfig?.dinputOnly;
        public int[] IdleDisconnectTimeout => SafeConfig?.idleDisconnectTimeout;
        public sbyte[] RightStickDriftXAxis => SafeConfig?.rightStickDriftXAxis;
        public sbyte[] RightStickDriftYAxis => SafeConfig?.rightStickDriftYAxis;
        public sbyte[] LeftStickDriftXAxis => SafeConfig?.leftStickDriftXAxis;
        public sbyte[] LeftStickDriftYAxis => SafeConfig?.leftStickDriftYAxis;
        public bool[] EnableOutputDataToDS4 => SafeConfig?.enableOutputDataToDS4;

        // Issue7是正: Global.OutContType と同一の _config.outputDevType への読み取り専用委譲。
        public OutContType[] OutContType => SafeConfig?.outputDevType;

        // 課題④: ProfileActions のインターフェース実装
        public List<string>[] ProfileActions
        {
            get => SafeConfig?.profileActions ?? Global.ProfileActions;
            set
            {
                if (SafeConfig != null)
                {
                    SafeConfig.profileActions = value;
                }
                if (value != null && Global.ProfileActions != null)
                {
                    for (int i = 0; i < Math.Min(value.Length, Global.ProfileActions.Length); i++)
                    {
                        Global.ProfileActions[i] = value[i];
                    }
                }
                OnProfileSettingChanged(-1, nameof(ProfileActions), null, value);
            }
        }

        public bool UseDs3PitchRollSim
        {
            get => SafeConfig != null && SafeConfig.useDs3PitchRollSim;
            set
            {
                if (SafeConfig != null) SafeConfig.useDs3PitchRollSim = value;
            }
        }
        public bool[] LowerRCOn => SafeConfig?.lowerRCOn;
        public string[] LaunchProgram => SafeConfig?.launchProgram;
        public int GetBTPollRate(int deviceIndex) => SafeConfig != null && SafeConfig.btPollRate != null ? SafeConfig.btPollRate[deviceIndex] : 0;
        public bool GetDInputOnly(int deviceIndex) => SafeConfig != null && SafeConfig.dinputOnly != null && SafeConfig.dinputOnly[deviceIndex];
        public int GetIdleDisconnectTimeout(int deviceIndex) => SafeConfig != null && SafeConfig.idleDisconnectTimeout != null ? SafeConfig.idleDisconnectTimeout[deviceIndex] : 0;
        public bool GetEnableOutputDataToDS4(int deviceIndex) => SafeConfig != null && SafeConfig.enableOutputDataToDS4 != null && SafeConfig.enableOutputDataToDS4[deviceIndex];
        public DS4ControlSettings GetDS4CSetting(int deviceIndex, string control) => SafeConfig?.GetDS4CSetting(deviceIndex, control);
        public DS4ControlSettings GetDS4CSetting(int deviceIndex, DS4Controls control) => SafeConfig?.GetDS4CSetting(deviceIndex, control);
        public List<DS4ControlSettings> GetDS4CSettings(int deviceIndex) => SafeConfig?.ds4settings != null ? SafeConfig.ds4settings[deviceIndex] : null;

        // ---- Step10-2-A-9: Mapping.cs専用 ----
        public bool ProfileChangedNotification
        {
            get => SafeConfig != null && SafeConfig.profileChangedNotification;
            set
            {
                if (SafeConfig != null) SafeConfig.profileChangedNotification = value;
            }
        }
        public int[] DebouncingMs => SafeConfig?.debouncingMs;
        public VirtualKBMMapping OutputKBMMapping { get; set; }
        public event EventHandler DebouncingMsChanged;
        public void NotifyDebouncingMsChanged() => DebouncingMsChanged?.Invoke(this, EventArgs.Empty);

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
                    if (SafeConfig?.rumbleSettings != null && deviceIndex < SafeConfig.rumbleSettings.Length)
                    {
                        SafeConfig.rumbleSettings[deviceIndex].Reset();
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

        // =========================================================================
        // 宣言的配線（Declarative SubSetting Descriptor）
        // =========================================================================
        public sealed class SubSettingDescriptor
        {
            public string Prefix { get; }
            public Func<ProfileSettingsService, int, ProfileSubSettingBase> Accessor { get; }

            public SubSettingDescriptor(string prefix, Func<ProfileSettingsService, int, ProfileSubSettingBase> accessor)
            {
                Prefix = prefix;
                Accessor = accessor;
            }
        }

        /// <summary>
        /// 全サブ設定の配線記述子リスト（完全性検証テストと実行時配線の双方で共通使用）
        /// </summary>
        public static readonly IReadOnlyList<SubSettingDescriptor> SubSettingDescriptors = new List<SubSettingDescriptor>
        {
            new SubSettingDescriptor("LS_X", (s, dev) => s.LSModInfo != null && s.LSModInfo.Length > dev ? s.LSModInfo[dev]?.xAxisDeadInfo : null),
            new SubSettingDescriptor("LS_Y", (s, dev) => s.LSModInfo != null && s.LSModInfo.Length > dev ? s.LSModInfo[dev]?.yAxisDeadInfo : null),
            new SubSettingDescriptor("RS_X", (s, dev) => s.RSModInfo != null && s.RSModInfo.Length > dev ? s.RSModInfo[dev]?.xAxisDeadInfo : null),
            new SubSettingDescriptor("RS_Y", (s, dev) => s.RSModInfo != null && s.RSModInfo.Length > dev ? s.RSModInfo[dev]?.yAxisDeadInfo : null),
            new SubSettingDescriptor("L2", (s, dev) => s.L2ModInfo != null && s.L2ModInfo.Length > dev ? s.L2ModInfo[dev] : null),
            new SubSettingDescriptor("R2", (s, dev) => s.R2ModInfo != null && s.R2ModInfo.Length > dev ? s.R2ModInfo[dev] : null),
            new SubSettingDescriptor("GyroControls", (s, dev) => s.GyroControlsInf != null && s.GyroControlsInf.Length > dev ? s.GyroControlsInf[dev] : null),
            new SubSettingDescriptor("TouchAbs", (s, dev) => s.TouchAbsMouse != null && s.TouchAbsMouse.Length > dev ? s.TouchAbsMouse[dev] : null),
            new SubSettingDescriptor("GyroMouse", (s, dev) => s.GyroMouseInfo != null && s.GyroMouseInfo.Length > dev ? s.GyroMouseInfo[dev] : null),
            new SubSettingDescriptor("GyroMouseStick", (s, dev) => s.GyroMouseStickInf != null && s.GyroMouseStickInf.Length > dev ? s.GyroMouseStickInf[dev] : null),
            new SubSettingDescriptor("TouchMouseStick", (s, dev) => s.TouchMouseStickInf != null && s.TouchMouseStickInf.Length > dev ? s.TouchMouseStickInf[dev] : null),
            new SubSettingDescriptor("LS_DeltaAccel", (s, dev) => s.LSOutputSettings != null && s.LSOutputSettings.Length > dev ? s.LSOutputSettings[dev]?.outputSettings?.controlSettings?.deltaAccelSettings : null),
            new SubSettingDescriptor("LS_FlickStick", (s, dev) => s.LSOutputSettings != null && s.LSOutputSettings.Length > dev ? s.LSOutputSettings[dev]?.outputSettings?.flickSettings : null),
            new SubSettingDescriptor("RS_DeltaAccel", (s, dev) => s.RSOutputSettings != null && s.RSOutputSettings.Length > dev ? s.RSOutputSettings[dev]?.outputSettings?.controlSettings?.deltaAccelSettings : null),
            new SubSettingDescriptor("RS_FlickStick", (s, dev) => s.RSOutputSettings != null && s.RSOutputSettings.Length > dev ? s.RSOutputSettings[dev]?.outputSettings?.flickSettings : null),
            new SubSettingDescriptor("Rumble", (s, dev) => s.RumbleSettings != null && s.RumbleSettings.Length > dev ? s.RumbleSettings[dev] : null),
        };

        /// <summary>
        /// 課題①: 指定スロット（-1 の場合は全スロット）のネストされたサブ設定オブジェクト群のイベント購読を安全に全解除します。
        /// </summary>
        public void UnwireSubSettingsEvents(int deviceIndex = -1)
        {
            lock (_syncLock)
            {
                int start = deviceIndex >= 0 ? deviceIndex : 0;
                int end = deviceIndex >= 0 ? deviceIndex + 1 : _environmentService.ControllerSlotLimit;

                for (int dev = start; dev < end && dev < _environmentService.ControllerSlotLimit; dev++)
                {
                    if (_subSettingsUnwireMap.TryGetValue(dev, out var unwireList))
                    {
                        foreach (var unwire in unwireList)
                        {
                            try
                            {
                                unwire?.Invoke();
                            }
                            catch
                            {
                                // 購読解除時の例外は握りつぶし、後続の解除を安全に継続
                            }
                        }
                        unwireList.Clear();
                        _subSettingsUnwireMap.Remove(dev);
                    }
                }
            }
        }

        /// <summary>
        /// ネストされたサブ設定オブジェクト群（スティック、トリガー、ジャイロ、タッチパッド、ランブル等）の
        /// OnSubPropertyChanged イベントを購読し、ProfileSettingChanged を発火させるように宣言的リストに基づき配線します。
        /// 課題①: 冒頭で UnwireSubSettingsEvents を実行し、100% の冪等性を保証します。
        /// </summary>
        public void WireSubSettingsEvents(int deviceIndex = -1)
        {
            try
            {
                // 課題①: 既存の購読を全解除してから再登録（冪等化）
                UnwireSubSettingsEvents(deviceIndex);

                lock (_syncLock)
                {
                    int start = deviceIndex >= 0 ? deviceIndex : 0;
                    int end = deviceIndex >= 0 ? deviceIndex + 1 : _environmentService.ControllerSlotLimit;

                    for (int dev = start; dev < end && dev < _environmentService.ControllerSlotLimit; dev++)
                    {
                        int currentDev = dev;
                        var unwireList = new List<Action>();

                        foreach (var desc in SubSettingDescriptors)
                        {
                            ProfileSubSettingBase subSetting = null;
                            try
                            {
                                subSetting = desc.Accessor(this, currentDev);
                            }
                            catch
                            {
                                subSetting = null;
                            }

                            if (subSetting != null)
                            {
                                string prefix = desc.Prefix;
                                Action<string> handler = (prop) => OnSubSettingChanged(currentDev, $"{prefix}_{prop}");
                                subSetting.OnSubPropertyChanged += handler;
                                unwireList.Add(() => subSetting.OnSubPropertyChanged -= handler);
                            }
                        }

                        _subSettingsUnwireMap[currentDev] = unwireList;
                    }
                }
            }
            catch (Exception ex)
            {
                AppLogger.LogDebug($"[ProfileSettingsService] WireSubSettingsEvents safe skip: {ex.Message}");
            }
        }

        private void OnSubSettingChanged(int deviceIndex, string settingName)
        {
            ProfileSettingChanged?.Invoke(this, new ProfileSettingChangedEventArgs(deviceIndex, settingName, null, null));
        }

        // ---- Phase6-Step2-2 (PR-2): 接続時のエクストラボタン再構築 ----
        public void RefreshExtrasButtons(int deviceIndex, List<DS4Controls> buttons)
            => Global.RefreshExtrasButtons(deviceIndex, buttons);

        // ---- Phase6-Step2-5 (PR-5): カスタムアクション／エクストラの有無 ----
        // 毎レポート評価される経路から呼ばれるため、BackingStore の配列を直接参照する（従来の Global.containsCustomAction／Extras と同一）。
        public bool ContainsCustomAction(int deviceIndex) => SafeConfig.containsCustomAction[deviceIndex];

        public bool ContainsCustomExtras(int deviceIndex) => SafeConfig.containsCustomExtras[deviceIndex];

        // ---- Phase6-Step3-1: 入力→出力割り当てグループ ----
        // 毎レポート評価される経路から呼ばれるため、BackingStore の配列を直接参照する（従来の Global.GetControlSettingsGroup と同一）。
        public ControlSettingsGroup GetControlSettingsGroup(int deviceIndex) => SafeConfig?.ds4controlSettings[deviceIndex];
    }
}