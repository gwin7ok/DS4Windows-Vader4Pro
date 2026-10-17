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
        public const int TEST_PROFILE_ITEM_COUNT = ControllerLimits.TestProfileItemCount;
        public const int MAX_DS4_CONTROLLER_COUNT = ControllerLimits.MaxControllerCount;

        // Step10-2-A: m_Config(BackingStore)委譲用。Global.storeと同一インスタンスを参照する
        // (データの二重管理を避けるため、専用バックアップ配列は持たない)
        private readonly BackingStore _config;
        private BackingStore SafeConfig => _config;

        // Phase6-Step2-1c (決定O2=C/O3=A): スロット上限（現在接続台数ではない）を IEnvironmentService から取得する。
        // 既存の生成箇所（テスト・Global のフォールバック）を変更しないよう省略可能とし、
        // 省略時は状態を持たない既定実装を用いる（AppHost.Services は使用しない）。
        private readonly IEnvironmentService _environmentService;

        // Phase6-Step10-4-2c: 全ての依存を必須化（Global.store／new EnvironmentService／Program.rootHub の参照を廃止）。
        // ControlService は ControlService → IProfileSettingsService の依存循環を避けるため、遅延解決（Func）で受け取る（決定 D4-3）。
        private readonly Func<ControlService> _controlProvider;

        public ProfileSettingsService(BackingStore backingStore,
            IEnvironmentService environmentService,
            Func<ControlService> controlProvider)
        {
            _config = backingStore ?? throw new ArgumentNullException(nameof(backingStore));
            _environmentService = environmentService ?? throw new ArgumentNullException(nameof(environmentService));
            _controlProvider = controlProvider ?? throw new ArgumentNullException(nameof(controlProvider));
        }

        private bool[] _touchpadActive = new bool[TEST_PROFILE_ITEM_COUNT] { true, true, true, true, true, true, true, true, true };
        private bool[] _useTempProfile = new bool[TEST_PROFILE_ITEM_COUNT] { false, false, false, false, false, false, false, false, false };
        private string[] _tempProfileName = new string[TEST_PROFILE_ITEM_COUNT] { string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty };
        private bool[] _tempProfileDistance = new bool[TEST_PROFILE_ITEM_COUNT] { false, false, false, false, false, false, false, false, false };
        private bool[] _useDInputOnly = new bool[TEST_PROFILE_ITEM_COUNT] { true, true, true, true, true, true, true, true, true };
        private bool[] _linkedProfileCheck = new bool[MAX_DS4_CONTROLLER_COUNT] { false, false, false, false, false, false, false, false };

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

        public bool GetTouchpadActive(int deviceIndex)
        {
            if (deviceIndex >= 0 && deviceIndex < _touchpadActive.Length)
                return _touchpadActive[deviceIndex];
            return true;
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
                    }
                }
            }
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
            if (_controlProvider()?.GetController(deviceIndex) is DualSenseDevice &&
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

        public void SetRumbleAutostopTime(int index, int value)
        {
            if (SafeConfig?.rumbleSettings != null && index >= 0 && index < SafeConfig.rumbleSettings.Length)
            {
                SafeConfig.rumbleSettings[index].RumbleAutostopTime = value;
            }
            if (SafeConfig != null && SafeConfig.rumbleAutostopTime != null && index < SafeConfig.rumbleAutostopTime.Length)
            {
                SafeConfig.rumbleAutostopTime[index] = value;
                DS4Device tempDev = _controlProvider()?.GetController(index);
                if (tempDev != null && tempDev.isSynced())
                    tempDev.RumbleAutostopTime = value;
            }
        }

        // ---- Step10-2-A-6: ボタン/マウス出力関連 (m_Config委譲) ----
        public ButtonMouseInfo[] ButtonMouseInfos => SafeConfig?.buttonMouseInfos;
        public bool[] EnableTouchToggle => SafeConfig?.enableTouchToggle;
        public SteeringWheelSmoothingInfo[] WheelSmoothInfo => SafeConfig?.wheelSmoothInfo;
        public bool[] DoubleTap => SafeConfig?.doubleTap;
        public int[] ScrollSensitivity => SafeConfig?.scrollSensitivity;
        public bool[] TrackballMode => SafeConfig?.trackballMode;
        public double[] TrackballFriction => SafeConfig?.trackballFriction;
        public bool GetEnableTouchToggle(int deviceIndex) => SafeConfig != null && SafeConfig.enableTouchToggle != null && SafeConfig.enableTouchToggle[deviceIndex];
        public bool GetDoubleTap(int deviceIndex) => SafeConfig != null && SafeConfig.doubleTap != null && SafeConfig.doubleTap[deviceIndex];
        public bool GetTrackballMode(int deviceIndex) => SafeConfig != null && SafeConfig.trackballMode != null && SafeConfig.trackballMode[deviceIndex];
        public double GetTrackballFriction(int deviceIndex) => SafeConfig != null && SafeConfig.trackballFriction != null ? SafeConfig.trackballFriction[deviceIndex] : 0;

        // ---- Step10-2-A-7: SA/デッドゾーン関連 (m_Config委譲) ----
        public string[] SATriggers => SafeConfig?.sATriggers;
        public bool[] SATriggerCond => SafeConfig?.sATriggerCond;
        public string[] SAMousestickTriggers => SafeConfig?.sAMouseStickTriggers;
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
            get => SafeConfig?.profileActions;
            set
            {
                if (SafeConfig != null)
                {
                    SafeConfig.profileActions = value;
                }
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

        public X360Controls[] GetDefaultButtonMapping()
        {
            return (X360Controls[])X360ControlTables.defaultButtonMapping.Clone();
        }

        // Phase6-Step9-1: Global.RefreshActionAlias への薄い委譲
        public void RefreshActionAlias(DS4ControlSettings setting, bool shift) =>
            Global.RefreshActionAlias(setting, shift);

        // コピーしない版（Phase6-Step3-6a、決定 R1）。Global.reverseX360ButtonMapping は起動時に作られて以後変化しない定数表。
        // 元の Global.reverseX360ButtonMapping（素の public static 配列）と同じく、参照をそのまま返す。
        public DS4Controls[] ReverseX360ButtonMapping => X360ControlTables.reverseX360ButtonMapping;

        // ---- Phase6-Step2-2 (PR-2): 接続時のエクストラボタン再構築 ----
        public void RefreshExtrasButtons(int deviceIndex, List<DS4Controls> buttons)
        {
            // Phase7-Step3 W5: 旧 Global.RefreshExtrasButtons の処理をここへ移した。
            _config.ds4controlSettings[deviceIndex].ResetExtraButtons();
            if (buttons != null)
            {
                _config.ds4controlSettings[deviceIndex].EstablishExtraButtons(buttons);
            }
        }

        // ---- Phase6-Step2-5 (PR-5): カスタムアクション／エクストラの有無 ----
        // 毎レポート評価される経路から呼ばれるため、BackingStore の配列を直接参照する（従来の Global.containsCustomAction／Extras と同一）。
        public bool ContainsCustomAction(int deviceIndex) => SafeConfig.containsCustomAction[deviceIndex];

        public bool ContainsCustomExtras(int deviceIndex) => SafeConfig.containsCustomExtras[deviceIndex];

        // ---- Phase6-Step3-1: 入力→出力割り当てグループ ----
        // 毎レポート評価される経路から呼ばれるため、BackingStore の配列を直接参照する（従来の Global.GetControlSettingsGroup と同一）。
        public ControlSettingsGroup GetControlSettingsGroup(int deviceIndex) => SafeConfig?.ds4controlSettings[deviceIndex];
    }
}