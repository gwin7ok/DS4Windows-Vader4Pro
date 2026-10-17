/*
DS4Windows
Copyright (C) 2023  Travis Nickles

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/

using DS4Windows.DS4Control;
using DS4Windows.InputDevices;
using DS4Windows.StickModifiers;
using DS4WinWPF.DS4Control.DTOXml;
using DS4WinWPF.DS4Forms.ViewModels;
using Sensorit.Base;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http.Json;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Xml;
using System.Xml.Serialization;
using DS4WinWPF;
using DS4WinWPF.ApiDTO;
using WpfScreenHelper;
using static DS4Windows.Mouse;
using static DS4Windows.Util;
using LightbarMacro = DS4WinWPF.DS4Forms.ViewModels.LightbarMacro;
namespace DS4Windows
{

    public class BackingStore
    {
        // ProfileEditor layout fields (DTOと統合)
        public const int DEFAULT_PROFILE_EDITOR_LEFT_WIDTH = WindowLayoutDefaults.PROFILE_EDITOR_LEFT_WIDTH;
        public const int DEFAULT_PROFILE_EDITOR_RIGHT_WIDTH = WindowLayoutDefaults.PROFILE_EDITOR_RIGHT_WIDTH;
        public const int DEFAULT_SPECIAL_ACTION_NAME_COL_WIDTH = WindowLayoutDefaults.SPECIAL_ACTION_NAME_COL_WIDTH;
        public const int DEFAULT_SPECIAL_ACTION_TRIGGER_COL_WIDTH = WindowLayoutDefaults.SPECIAL_ACTION_TRIGGER_COL_WIDTH;
        public const int DEFAULT_SPECIAL_ACTION_DETAIL_COL_WIDTH = WindowLayoutDefaults.SPECIAL_ACTION_DETAIL_COL_WIDTH;
        // Active 列はプロファイルに永続化しない列幅の初期値
        public const int DEFAULT_SPECIAL_ACTION_ACTIVE_COL_WIDTH = WindowLayoutDefaults.SPECIAL_ACTION_ACTIVE_COL_WIDTH;
        // SpecialActionDeleteColWidth 定数削除

        // Controller tab column widths
        public const int DEFAULT_CONTROLLER_INDEX_COL_WIDTH = WindowLayoutDefaults.CONTROLLER_INDEX_COL_WIDTH;
        public const int DEFAULT_CONTROLLER_ID_COL_WIDTH = WindowLayoutDefaults.CONTROLLER_ID_COL_WIDTH;
        public const int DEFAULT_CONTROLLER_STATUS_COL_WIDTH = WindowLayoutDefaults.CONTROLLER_STATUS_COL_WIDTH;
        public const int DEFAULT_CONTROLLER_EXCLUSIVE_COL_WIDTH = WindowLayoutDefaults.CONTROLLER_EXCLUSIVE_COL_WIDTH;
        public const int DEFAULT_CONTROLLER_BATTERY_COL_WIDTH = WindowLayoutDefaults.CONTROLLER_BATTERY_COL_WIDTH;
        public const int DEFAULT_CONTROLLER_SELECTPROFILE_COL_WIDTH = WindowLayoutDefaults.CONTROLLER_SELECTPROFILE_COL_WIDTH;
        public const int DEFAULT_CONTROLLER_EDIT_COL_WIDTH = WindowLayoutDefaults.CONTROLLER_EDIT_COL_WIDTH;
        public const int DEFAULT_CONTROLLER_LINKED_PROFILE_COL_WIDTH = WindowLayoutDefaults.CONTROLLER_LINKED_PROFILE_COL_WIDTH;
        public const int DEFAULT_CONTROLLER_LINK_PROF_ID_COL_WIDTH = WindowLayoutDefaults.CONTROLLER_LINK_PROF_ID_COL_WIDTH;
        public const int DEFAULT_CONTROLLER_CUSTOMCOLOR_COL_WIDTH = WindowLayoutDefaults.CONTROLLER_CUSTOMCOLOR_COL_WIDTH;

        // Log settings
        public const int DEFAULT_LOG_MAX_ARCHIVE_FILES = 50;
        public const string DEFAULT_LOG_MIN_LEVEL = "Info";

        public int profileEditorLeftWidth = DEFAULT_PROFILE_EDITOR_LEFT_WIDTH;
        public int profileEditorRightWidth = DEFAULT_PROFILE_EDITOR_RIGHT_WIDTH;
        public int specialActionNameColWidth = DEFAULT_SPECIAL_ACTION_NAME_COL_WIDTH;
        public int specialActionTriggerColWidth = DEFAULT_SPECIAL_ACTION_TRIGGER_COL_WIDTH;
        public int specialActionDetailColWidth = DEFAULT_SPECIAL_ACTION_DETAIL_COL_WIDTH;
        // SpecialActionDeleteColWidth フィールド削除

        public int controllerIndexColWidth = DEFAULT_CONTROLLER_INDEX_COL_WIDTH;
        public int controllerIdColWidth = DEFAULT_CONTROLLER_ID_COL_WIDTH;
        public int controllerStatusColWidth = DEFAULT_CONTROLLER_STATUS_COL_WIDTH;
        public int controllerExclusiveColWidth = DEFAULT_CONTROLLER_EXCLUSIVE_COL_WIDTH;
        public int controllerBatteryColWidth = DEFAULT_CONTROLLER_BATTERY_COL_WIDTH;
        public int controllerSelectProfileColWidth = DEFAULT_CONTROLLER_SELECTPROFILE_COL_WIDTH;
        public int controllerEditColWidth = DEFAULT_CONTROLLER_EDIT_COL_WIDTH;
        public int controllerLinkedProfileColWidth = DEFAULT_CONTROLLER_LINKED_PROFILE_COL_WIDTH;
        public int controllerLinkProfIdColWidth = DEFAULT_CONTROLLER_LINK_PROF_ID_COL_WIDTH;
        public int controllerCustomColorColWidth = DEFAULT_CONTROLLER_CUSTOMCOLOR_COL_WIDTH;

        public int logMaxArchiveFiles = DEFAULT_LOG_MAX_ARCHIVE_FILES;
        public string logMinLevel = DEFAULT_LOG_MIN_LEVEL;

        public const double DEFAULT_UDP_SMOOTH_MINCUTOFF = 0.4;
        public const double DEFAULT_UDP_SMOOTH_BETA = 0.2;
        // Use 15 minutes for default Idle Disconnect when initially enabling the option
        public const int DEFAULT_ENABLE_IDLE_DISCONN_MINS = 15;
        public const double DEFAULT_SX_TILT_DEADZONE = 0.25;
        public const double DEFAULT_SX_TILT_MAXZONE = 1.0;
        public const string DEFAULT_SA_TRIGGERS = "-1";
        public const string DEFAULT_GYRO_MSTICK_TRIGGERS = "-1";
        public const OutContType DEFAULT_OUT_CONT_TYPE = OutContType.X360;
        public const bool DEFAULT_OUTPUT_TO_DS4 = true;
        public const bool DEFAULT_TOUCH_TOGGLE = true;
        public const bool DEFAULT_TOUCHPAD_JITTER_COMP = true;
        public const int DEFAULT_TOUCHPAD_SENS = 100;
        public const int DEFAULT_DS4_BT_POLL_RATE = 4;
        public const int DEFAULT_RUMBLE = 100;
        public const double DEFAULT_ANALOG_SENS = 1.0;
        public const bool DEFAULT_DINPUT_ONLY = false;
        public const TouchpadOutMode DEFAULT_TOUCH_OUT_MODE = TouchpadOutMode.Mouse;
        public const GyroOutMode DEFAULT_GYRO_OUT_MODE = GyroOutMode.Controls;
        public const bool DEFAULT_SA_TRIGGER_COND = true;
        public const bool DEFAULT_SA_MSTICK_TRIGGER_COND = true;
        public const bool DEFAULT_GYRO_TRIGGER_TURNS = true;
        public const bool DEFAULT_GYRO_MSTICK_TRIGGER_TURNS = true;
        public const int DEFAULT_SA_WHEEL_EMULATION_RANGE = 360;
        public const int DEFAULT_GYRO_SENS = 100;
        public const int DEFAULT_GYRO_SENS_VERTICAL_SCALE = 100;
        public const int DEFAULT_TOUCH_DIS_INVERT_TRIGGER = -1;
        public const bool DEFAULT_TRACKBALL_MODE = false;
        public const double DEFAULT_TRACKBALL_FRICTION = 10.0;

        // Stick output curve consts in place more as a precaution
        // Phase7-Step10b W4: DEFAULT_STICK_OUTPUT_CURVE／_ID、DEFAULT_SA_OUTPUT_CURVE／_ID は削除した（廃止済みの ProfileDTO の既定値の残骸。参照 0 件）。

        public String m_Profile = Directory.GetParent(Assembly.GetExecutingAssembly().Location).FullName + "\\Profiles.xml";
        public String m_Actions = Global.appdatapath + "\\Actions.xml";
        public string m_linkedProfiles = Global.appdatapath + "\\LinkedProfiles.xml";
        public string m_controllerConfigs = Global.appdatapath + "\\ControllerConfigs.xml";

        // Phase7-Step10c W5: 作業用の XmlDocument（m_Xdoc）は、読み書きを担う各シリアライザーが自分のフィールドとして持つ。BackingStore からは削除した。
        // ninth (fifth in old builds) value used for options, not last controller
        public ButtonMouseInfo[] buttonMouseInfos = new ButtonMouseInfo[ControllerLimits.TestProfileItemCount]
        {
            new ButtonMouseInfo(), new ButtonMouseInfo(), new ButtonMouseInfo(),
            new ButtonMouseInfo(), new ButtonMouseInfo(), new ButtonMouseInfo(),
            new ButtonMouseInfo(), new ButtonMouseInfo(), new ButtonMouseInfo(),
        };

        public bool[] enableTouchToggle = new bool[ControllerLimits.TestProfileItemCount]
        { DEFAULT_TOUCH_TOGGLE, DEFAULT_TOUCH_TOGGLE, DEFAULT_TOUCH_TOGGLE,
          DEFAULT_TOUCH_TOGGLE, DEFAULT_TOUCH_TOGGLE, DEFAULT_TOUCH_TOGGLE,
          DEFAULT_TOUCH_TOGGLE, DEFAULT_TOUCH_TOGGLE, DEFAULT_TOUCH_TOGGLE };
        public int[] idleDisconnectTimeout = new int[ControllerLimits.TestProfileItemCount] { 0, 0, 0, 0, 0, 0, 0, 0, 0 };
        public bool[] enableOutputDataToDS4 = new bool[ControllerLimits.TestProfileItemCount]
        { DEFAULT_OUTPUT_TO_DS4, DEFAULT_OUTPUT_TO_DS4, DEFAULT_OUTPUT_TO_DS4,
          DEFAULT_OUTPUT_TO_DS4, DEFAULT_OUTPUT_TO_DS4, DEFAULT_OUTPUT_TO_DS4,
          DEFAULT_OUTPUT_TO_DS4, DEFAULT_OUTPUT_TO_DS4, DEFAULT_OUTPUT_TO_DS4 };
        public bool[] touchpadJitterCompensation = new bool[ControllerLimits.TestProfileItemCount]
        { DEFAULT_TOUCHPAD_JITTER_COMP, DEFAULT_TOUCHPAD_JITTER_COMP, DEFAULT_TOUCHPAD_JITTER_COMP,
          DEFAULT_TOUCHPAD_JITTER_COMP, DEFAULT_TOUCHPAD_JITTER_COMP, DEFAULT_TOUCHPAD_JITTER_COMP,
          DEFAULT_TOUCHPAD_JITTER_COMP, DEFAULT_TOUCHPAD_JITTER_COMP, DEFAULT_TOUCHPAD_JITTER_COMP };
        public bool[] lowerRCOn = new bool[ControllerLimits.TestProfileItemCount] { false, false, false, false, false, false, false, false, false };
        public bool[] touchClickPassthru = new bool[ControllerLimits.TestProfileItemCount] { false, false, false, false, false, false, false, false, false };
        public string[] profilePath = new string[ControllerLimits.TestProfileItemCount] { string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty };
        public string[] olderProfilePath = new string[ControllerLimits.TestProfileItemCount] { string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty };
        public string[] selectedProfile = new string[ControllerLimits.TestProfileItemCount] { string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty };
        public string[] linkedProfileUI = new string[ControllerLimits.TestProfileItemCount] { string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty };
        // プロファイル切り替え時の無効アクション名エラーログ抑制用（Phase7-Step3c W3: Global の静的フィールドから移した）
        public HashSet<string> loggedInvalidActions = new HashSet<string>();
        public bool[] firstConnectionAfterStartup = new bool[ControllerLimits.TestProfileItemCount] { true, true, true, true, true, true, true, true, true };
        public Dictionary<string, string> linkedProfiles = new Dictionary<string, string>();
        // Cache properties instead of performing a string comparison every frame
        public bool[] distanceProfiles = new bool[ControllerLimits.TestProfileItemCount] { false, false, false, false, false, false, false, false, false };
        public Byte[] rumble = new Byte[ControllerLimits.TestProfileItemCount]
        { DEFAULT_RUMBLE, DEFAULT_RUMBLE, DEFAULT_RUMBLE,
          DEFAULT_RUMBLE, DEFAULT_RUMBLE, DEFAULT_RUMBLE,
          DEFAULT_RUMBLE, DEFAULT_RUMBLE, DEFAULT_RUMBLE };
        public int[] rumbleAutostopTime = new int[ControllerLimits.TestProfileItemCount] { 0, 0, 0, 0, 0, 0, 0, 0, 0 }; // Value in milliseconds (0=autustop timer disabled)
        public Byte[] touchSensitivity = new Byte[ControllerLimits.TestProfileItemCount]
        { DEFAULT_TOUCHPAD_SENS, DEFAULT_TOUCHPAD_SENS, DEFAULT_TOUCHPAD_SENS,
          DEFAULT_TOUCHPAD_SENS, DEFAULT_TOUCHPAD_SENS, DEFAULT_TOUCHPAD_SENS,
          DEFAULT_TOUCHPAD_SENS, DEFAULT_TOUCHPAD_SENS, DEFAULT_TOUCHPAD_SENS };
        public StickDeadZoneInfo[] lsModInfo = new StickDeadZoneInfo[ControllerLimits.TestProfileItemCount]
        {
            new StickDeadZoneInfo(), new StickDeadZoneInfo(),
            new StickDeadZoneInfo(), new StickDeadZoneInfo(),
            new StickDeadZoneInfo(), new StickDeadZoneInfo(),
            new StickDeadZoneInfo(), new StickDeadZoneInfo(),
            new StickDeadZoneInfo(),
        };
        public StickDeadZoneInfo[] rsModInfo = new StickDeadZoneInfo[ControllerLimits.TestProfileItemCount]
        {
            new StickDeadZoneInfo(), new StickDeadZoneInfo(),
            new StickDeadZoneInfo(), new StickDeadZoneInfo(),
            new StickDeadZoneInfo(), new StickDeadZoneInfo(),
            new StickDeadZoneInfo(), new StickDeadZoneInfo(),
            new StickDeadZoneInfo(),
        };
        public TriggerDeadZoneZInfo[] l2ModInfo = new TriggerDeadZoneZInfo[ControllerLimits.TestProfileItemCount]
        {
            new TriggerDeadZoneZInfo(), new TriggerDeadZoneZInfo(),
            new TriggerDeadZoneZInfo(), new TriggerDeadZoneZInfo(),
            new TriggerDeadZoneZInfo(), new TriggerDeadZoneZInfo(),
            new TriggerDeadZoneZInfo(), new TriggerDeadZoneZInfo(),
            new TriggerDeadZoneZInfo(),
        };
        public TriggerDeadZoneZInfo[] r2ModInfo = new TriggerDeadZoneZInfo[ControllerLimits.TestProfileItemCount]
        {
            new TriggerDeadZoneZInfo(), new TriggerDeadZoneZInfo(),
            new TriggerDeadZoneZInfo(), new TriggerDeadZoneZInfo(),
            new TriggerDeadZoneZInfo(), new TriggerDeadZoneZInfo(),
            new TriggerDeadZoneZInfo(), new TriggerDeadZoneZInfo(),
            new TriggerDeadZoneZInfo(),
        };

        // Rotation angle expressed in radians
        public double[] LSRotation = new double[ControllerLimits.TestProfileItemCount] { 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0 };

        // Rotation angle expressed in radians
        public double[] RSRotation = new double[ControllerLimits.TestProfileItemCount] { 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0 };

        public double[] SXDeadzone = new double[ControllerLimits.TestProfileItemCount] { DEFAULT_SX_TILT_DEADZONE, DEFAULT_SX_TILT_DEADZONE, DEFAULT_SX_TILT_DEADZONE, DEFAULT_SX_TILT_DEADZONE, DEFAULT_SX_TILT_DEADZONE, DEFAULT_SX_TILT_DEADZONE, DEFAULT_SX_TILT_DEADZONE, DEFAULT_SX_TILT_DEADZONE, DEFAULT_SX_TILT_DEADZONE },
            SZDeadzone = new double[ControllerLimits.TestProfileItemCount] { DEFAULT_SX_TILT_DEADZONE, DEFAULT_SX_TILT_DEADZONE, DEFAULT_SX_TILT_DEADZONE, DEFAULT_SX_TILT_DEADZONE, DEFAULT_SX_TILT_DEADZONE, DEFAULT_SX_TILT_DEADZONE, DEFAULT_SX_TILT_DEADZONE, DEFAULT_SX_TILT_DEADZONE, DEFAULT_SX_TILT_DEADZONE };

        public double[] SXMaxzone = new double[ControllerLimits.TestProfileItemCount]
        { DEFAULT_SX_TILT_MAXZONE, DEFAULT_SX_TILT_MAXZONE, DEFAULT_SX_TILT_MAXZONE,
            DEFAULT_SX_TILT_MAXZONE, DEFAULT_SX_TILT_MAXZONE, DEFAULT_SX_TILT_MAXZONE,
            DEFAULT_SX_TILT_MAXZONE, DEFAULT_SX_TILT_MAXZONE, DEFAULT_SX_TILT_MAXZONE };
        public double[] SZMaxzone = new double[ControllerLimits.TestProfileItemCount]
        { DEFAULT_SX_TILT_MAXZONE, DEFAULT_SX_TILT_MAXZONE, DEFAULT_SX_TILT_MAXZONE,
          DEFAULT_SX_TILT_MAXZONE, DEFAULT_SX_TILT_MAXZONE, DEFAULT_SX_TILT_MAXZONE,
          DEFAULT_SX_TILT_MAXZONE, DEFAULT_SX_TILT_MAXZONE, DEFAULT_SX_TILT_MAXZONE };
        public double[] SXAntiDeadzone = new double[ControllerLimits.TestProfileItemCount] { 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0 },
            SZAntiDeadzone = new double[ControllerLimits.TestProfileItemCount] { 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0 };
        public double[] l2Sens = new double[ControllerLimits.TestProfileItemCount]
        { DEFAULT_ANALOG_SENS, DEFAULT_ANALOG_SENS, DEFAULT_ANALOG_SENS,
          DEFAULT_ANALOG_SENS, DEFAULT_ANALOG_SENS, DEFAULT_ANALOG_SENS,
          DEFAULT_ANALOG_SENS, DEFAULT_ANALOG_SENS, DEFAULT_ANALOG_SENS };
        public double[] r2Sens = new double[ControllerLimits.TestProfileItemCount]
        { DEFAULT_ANALOG_SENS, DEFAULT_ANALOG_SENS, DEFAULT_ANALOG_SENS,
          DEFAULT_ANALOG_SENS, DEFAULT_ANALOG_SENS, DEFAULT_ANALOG_SENS,
          DEFAULT_ANALOG_SENS, DEFAULT_ANALOG_SENS, DEFAULT_ANALOG_SENS };
        public double[] LSSens = new double[ControllerLimits.TestProfileItemCount]
        { DEFAULT_ANALOG_SENS, DEFAULT_ANALOG_SENS, DEFAULT_ANALOG_SENS,
          DEFAULT_ANALOG_SENS, DEFAULT_ANALOG_SENS, DEFAULT_ANALOG_SENS,
          DEFAULT_ANALOG_SENS, DEFAULT_ANALOG_SENS, DEFAULT_ANALOG_SENS};
        public double[] RSSens = new double[ControllerLimits.TestProfileItemCount]
        { DEFAULT_ANALOG_SENS, DEFAULT_ANALOG_SENS, DEFAULT_ANALOG_SENS,
          DEFAULT_ANALOG_SENS, DEFAULT_ANALOG_SENS, DEFAULT_ANALOG_SENS,
          DEFAULT_ANALOG_SENS, DEFAULT_ANALOG_SENS, DEFAULT_ANALOG_SENS };
        public double[] SXSens = new double[ControllerLimits.TestProfileItemCount]
        { DEFAULT_ANALOG_SENS, DEFAULT_ANALOG_SENS, DEFAULT_ANALOG_SENS,
          DEFAULT_ANALOG_SENS, DEFAULT_ANALOG_SENS, DEFAULT_ANALOG_SENS,
          DEFAULT_ANALOG_SENS, DEFAULT_ANALOG_SENS, DEFAULT_ANALOG_SENS };
        public double[] SZSens = new double[ControllerLimits.TestProfileItemCount]
        { DEFAULT_ANALOG_SENS, DEFAULT_ANALOG_SENS, DEFAULT_ANALOG_SENS,
          DEFAULT_ANALOG_SENS, DEFAULT_ANALOG_SENS, DEFAULT_ANALOG_SENS,
          DEFAULT_ANALOG_SENS, DEFAULT_ANALOG_SENS, DEFAULT_ANALOG_SENS };
        public Byte[] tapSensitivity = new Byte[ControllerLimits.TestProfileItemCount] { 0, 0, 0, 0, 0, 0, 0, 0, 0 };
        public bool[] doubleTap = new bool[ControllerLimits.TestProfileItemCount] { false, false, false, false, false, false, false, false, false };
        public int[] scrollSensitivity = new int[ControllerLimits.TestProfileItemCount] { 0, 0, 0, 0, 0, 0, 0, 0, 0 };
        public int[] touchpadInvert = new int[ControllerLimits.TestProfileItemCount] { 0, 0, 0, 0, 0, 0, 0, 0, 0 };
        public int[] btPollRate = new int[ControllerLimits.TestProfileItemCount]
        { DEFAULT_DS4_BT_POLL_RATE, DEFAULT_DS4_BT_POLL_RATE, DEFAULT_DS4_BT_POLL_RATE,
          DEFAULT_DS4_BT_POLL_RATE, DEFAULT_DS4_BT_POLL_RATE, DEFAULT_DS4_BT_POLL_RATE,
          DEFAULT_DS4_BT_POLL_RATE, DEFAULT_DS4_BT_POLL_RATE, DEFAULT_DS4_BT_POLL_RATE };
        public int[] gyroMouseDZ = new int[ControllerLimits.TestProfileItemCount] { MouseCursor.GYRO_MOUSE_DEADZONE, MouseCursor.GYRO_MOUSE_DEADZONE,
            MouseCursor.GYRO_MOUSE_DEADZONE, MouseCursor.GYRO_MOUSE_DEADZONE,
            MouseCursor.GYRO_MOUSE_DEADZONE, MouseCursor.GYRO_MOUSE_DEADZONE,
            MouseCursor.GYRO_MOUSE_DEADZONE,MouseCursor.GYRO_MOUSE_DEADZONE,
            MouseCursor.GYRO_MOUSE_DEADZONE,};
        public bool[] gyroMouseToggle = new bool[ControllerLimits.TestProfileItemCount] { false, false, false,
            false, false, false, false, false, false };

        public SquareStickInfo[] squStickInfo = new SquareStickInfo[ControllerLimits.TestProfileItemCount]
        {
            new SquareStickInfo(), new SquareStickInfo(),
            new SquareStickInfo(), new SquareStickInfo(),
            new SquareStickInfo(), new SquareStickInfo(),
            new SquareStickInfo(), new SquareStickInfo(),
            new SquareStickInfo(),
        };

        public StickAntiSnapbackInfo[] lsAntiSnapbackInfo = new StickAntiSnapbackInfo[ControllerLimits.TestProfileItemCount]
        {
            new StickAntiSnapbackInfo(), new StickAntiSnapbackInfo(),
            new StickAntiSnapbackInfo(), new StickAntiSnapbackInfo(),
            new StickAntiSnapbackInfo(), new StickAntiSnapbackInfo(),
            new StickAntiSnapbackInfo(), new StickAntiSnapbackInfo(),
            new StickAntiSnapbackInfo(),
        };

        public StickAntiSnapbackInfo[] rsAntiSnapbackInfo = new StickAntiSnapbackInfo[ControllerLimits.TestProfileItemCount]
        {
            new StickAntiSnapbackInfo(), new StickAntiSnapbackInfo(),
            new StickAntiSnapbackInfo(), new StickAntiSnapbackInfo(),
            new StickAntiSnapbackInfo(), new StickAntiSnapbackInfo(),
            new StickAntiSnapbackInfo(), new StickAntiSnapbackInfo(),
            new StickAntiSnapbackInfo(),
        };

        public StickOutputSetting[] lsOutputSettings = new StickOutputSetting[ControllerLimits.TestProfileItemCount]
        {
            new StickOutputSetting(), new StickOutputSetting(), new StickOutputSetting(),
            new StickOutputSetting(), new StickOutputSetting(), new StickOutputSetting(),
            new StickOutputSetting(), new StickOutputSetting(), new StickOutputSetting(),
        };

        public StickOutputSetting[] rsOutputSettings = new StickOutputSetting[ControllerLimits.TestProfileItemCount]
        {
            new StickOutputSetting(), new StickOutputSetting(), new StickOutputSetting(),
            new StickOutputSetting(), new StickOutputSetting(), new StickOutputSetting(),
            new StickOutputSetting(), new StickOutputSetting(), new StickOutputSetting(),
        };

        public TriggerOutputSettings[] l2OutputSettings = new TriggerOutputSettings[ControllerLimits.TestProfileItemCount]
        {
            new TriggerOutputSettings(), new TriggerOutputSettings(), new TriggerOutputSettings(),
            new TriggerOutputSettings(), new TriggerOutputSettings(), new TriggerOutputSettings(),
            new TriggerOutputSettings(), new TriggerOutputSettings(), new TriggerOutputSettings(),
        };

        public TriggerOutputSettings[] r2OutputSettings = new TriggerOutputSettings[ControllerLimits.TestProfileItemCount]
        {
            new TriggerOutputSettings(), new TriggerOutputSettings(), new TriggerOutputSettings(),
            new TriggerOutputSettings(), new TriggerOutputSettings(), new TriggerOutputSettings(),
            new TriggerOutputSettings(), new TriggerOutputSettings(), new TriggerOutputSettings(),
        };

        public SteeringWheelSmoothingInfo[] wheelSmoothInfo = new SteeringWheelSmoothingInfo[ControllerLimits.TestProfileItemCount]
        {
            new SteeringWheelSmoothingInfo(), new SteeringWheelSmoothingInfo(),
            new SteeringWheelSmoothingInfo(), new SteeringWheelSmoothingInfo(),
            new SteeringWheelSmoothingInfo(), new SteeringWheelSmoothingInfo(),
            new SteeringWheelSmoothingInfo(), new SteeringWheelSmoothingInfo(),
            new SteeringWheelSmoothingInfo(),
        };

        public int[] saWheelFuzzValues = new int[ControllerLimits.TestProfileItemCount];


        // Start of DualSense specific profile options
        //
        public DualSenseDevice.RumbleEmulationMode[] dualSenseRumbleEmulationMode = new DualSenseDevice.RumbleEmulationMode[ControllerLimits.TestProfileItemCount]
        {
            0,0,0,0,0,0,0,0,0
        };
        public bool[] useGenericRumbleRescaleForDualSenses = new bool[ControllerLimits.TestProfileItemCount] { false, false, false, false, false, false, false, false, false };
        public byte[] dualSenseHapticPowerLevel = new byte[ControllerLimits.TestProfileItemCount]
        {
            0,0,0,0,0,0,0,0,0
        };
        //
        // End of DualSense specific profile options

        private void setOutBezierCurveObjArrayItem(BezierCurve[] bezierCurveArray, int device, int curveOptionValue, BezierCurve.AxisType axisType)
        {
            // Set bezier curve obj of axis. 0=Linear (no curve mapping), 1-5=Pre-defined curves, 6=User supplied custom curve string value of a profile (comma separated list of 4 decimal numbers)
            switch (curveOptionValue)
            {
                // Commented out case 1..5 because Mapping.cs:SetCurveAndDeadzone function has the original IF-THEN-ELSE code logic for those original 1..5 output curve mappings (ie. no need to initialize the lookup result table).
                // Only the new bezier custom curve option 6 uses the lookup result table (initialized in BezierCurve class based on an input curve definition).
                //case 1: bezierCurveArray[device].InitBezierCurve(99.0, 91.0, 0.00, 0.00, axisType); break;  // Enhanced Precision (hard-coded curve) (almost the same curve as bezier 0.70, 0.28, 1.00, 1.00)
                //case 2: bezierCurveArray[device].InitBezierCurve(99.0, 92.0, 0.00, 0.00, axisType); break;  // Quadric
                //case 3: bezierCurveArray[device].InitBezierCurve(99.0, 93.0, 0.00, 0.00, axisType); break;  // Cubic
                //case 4: bezierCurveArray[device].InitBezierCurve(99.0, 94.0, 0.00, 0.00, axisType); break;  // Easeout Quad
                //case 5: bezierCurveArray[device].InitBezierCurve(99.0, 95.0, 0.00, 0.00, axisType); break;  // Easeout Cubic
                case 6: bezierCurveArray[device].InitBezierCurve(bezierCurveArray[device].CustomDefinition, axisType); break;  // Custom output curve
            }
        }

        public BezierCurve[] lsOutBezierCurveObj = new BezierCurve[ControllerLimits.TestProfileItemCount] { new BezierCurve(), new BezierCurve(), new BezierCurve(), new BezierCurve(), new BezierCurve(), new BezierCurve(), new BezierCurve(), new BezierCurve(), new BezierCurve() };
        public BezierCurve[] rsOutBezierCurveObj = new BezierCurve[ControllerLimits.TestProfileItemCount] { new BezierCurve(), new BezierCurve(), new BezierCurve(), new BezierCurve(), new BezierCurve(), new BezierCurve(), new BezierCurve(), new BezierCurve(), new BezierCurve() };
        public BezierCurve[] l2OutBezierCurveObj = new BezierCurve[ControllerLimits.TestProfileItemCount] { new BezierCurve(), new BezierCurve(), new BezierCurve(), new BezierCurve(), new BezierCurve(), new BezierCurve(), new BezierCurve(), new BezierCurve(), new BezierCurve() };
        public BezierCurve[] r2OutBezierCurveObj = new BezierCurve[ControllerLimits.TestProfileItemCount] { new BezierCurve(), new BezierCurve(), new BezierCurve(), new BezierCurve(), new BezierCurve(), new BezierCurve(), new BezierCurve(), new BezierCurve(), new BezierCurve() };
        public BezierCurve[] sxOutBezierCurveObj = new BezierCurve[ControllerLimits.TestProfileItemCount] { new BezierCurve(), new BezierCurve(), new BezierCurve(), new BezierCurve(), new BezierCurve(), new BezierCurve(), new BezierCurve(), new BezierCurve(), new BezierCurve() };
        public BezierCurve[] szOutBezierCurveObj = new BezierCurve[ControllerLimits.TestProfileItemCount] { new BezierCurve(), new BezierCurve(), new BezierCurve(), new BezierCurve(), new BezierCurve(), new BezierCurve(), new BezierCurve(), new BezierCurve(), new BezierCurve() };

        private int[] _lsOutCurveMode = new int[ControllerLimits.TestProfileItemCount] { 0, 0, 0, 0, 0, 0, 0, 0, 0 };
        public int getLsOutCurveMode(int index) { return _lsOutCurveMode[index]; }
        public void setLsOutCurveMode(int index, int value)
        {
            if (value >= 1) setOutBezierCurveObjArrayItem(lsOutBezierCurveObj, index, value, BezierCurve.AxisType.LSRS);
            _lsOutCurveMode[index] = value;
        }

        private int[] _rsOutCurveMode = new int[ControllerLimits.TestProfileItemCount] { 0, 0, 0, 0, 0, 0, 0, 0, 0 };
        public int getRsOutCurveMode(int index) { return _rsOutCurveMode[index]; }
        public void setRsOutCurveMode(int index, int value)
        {
            if (value >= 1) setOutBezierCurveObjArrayItem(rsOutBezierCurveObj, index, value, BezierCurve.AxisType.LSRS);
            _rsOutCurveMode[index] = value;
        }

        private int[] _l2OutCurveMode = new int[ControllerLimits.TestProfileItemCount] { 0, 0, 0, 0, 0, 0, 0, 0, 0 };
        public int getL2OutCurveMode(int index) { return _l2OutCurveMode[index]; }
        public void setL2OutCurveMode(int index, int value)
        {
            if (value >= 1) setOutBezierCurveObjArrayItem(l2OutBezierCurveObj, index, value, BezierCurve.AxisType.L2R2);
            _l2OutCurveMode[index] = value;
        }

        private int[] _r2OutCurveMode = new int[ControllerLimits.TestProfileItemCount] { 0, 0, 0, 0, 0, 0, 0, 0, 0 };
        public int getR2OutCurveMode(int index) { return _r2OutCurveMode[index]; }
        public void setR2OutCurveMode(int index, int value)
        {
            if (value >= 1) setOutBezierCurveObjArrayItem(r2OutBezierCurveObj, index, value, BezierCurve.AxisType.L2R2);
            _r2OutCurveMode[index] = value;
        }

        private int[] _sxOutCurveMode = new int[ControllerLimits.TestProfileItemCount] { 0, 0, 0, 0, 0, 0, 0, 0, 0 };
        public int getSXOutCurveMode(int index) { return _sxOutCurveMode[index]; }
        public void setSXOutCurveMode(int index, int value)
        {
            if (value >= 1) setOutBezierCurveObjArrayItem(sxOutBezierCurveObj, index, value, BezierCurve.AxisType.SA);
            _sxOutCurveMode[index] = value;
        }

        private int[] _szOutCurveMode = new int[ControllerLimits.TestProfileItemCount] { 0, 0, 0, 0, 0, 0, 0, 0, 0 };
        public int getSZOutCurveMode(int index) { return _szOutCurveMode[index]; }
        public void setSZOutCurveMode(int index, int value)
        {
            if (value >= 1) setOutBezierCurveObjArrayItem(szOutBezierCurveObj, index, value, BezierCurve.AxisType.SA);
            _szOutCurveMode[index] = value;
        }

        public LightbarSettingInfo[] lightbarSettingInfo = new LightbarSettingInfo[ControllerLimits.TestProfileItemCount]
        {
            new LightbarSettingInfo(), new LightbarSettingInfo(),
            new LightbarSettingInfo(), new LightbarSettingInfo(),
            new LightbarSettingInfo(), new LightbarSettingInfo(),
            new LightbarSettingInfo(), new LightbarSettingInfo(),
            new LightbarSettingInfo(),
        };

        public int processPriority;
        public string[] launchProgram = new string[ControllerLimits.TestProfileItemCount] { string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty };
        public bool[] dinputOnly = new bool[ControllerLimits.TestProfileItemCount]
        { DEFAULT_DINPUT_ONLY, DEFAULT_DINPUT_ONLY, DEFAULT_DINPUT_ONLY,
          DEFAULT_DINPUT_ONLY, DEFAULT_DINPUT_ONLY, DEFAULT_DINPUT_ONLY,
          DEFAULT_DINPUT_ONLY, DEFAULT_DINPUT_ONLY, DEFAULT_DINPUT_ONLY };
        public bool[] startTouchpadOff = new bool[ControllerLimits.TestProfileItemCount] { false, false, false, false, false, false, false, false, false };
        public TouchpadOutMode[] touchOutMode = new TouchpadOutMode[ControllerLimits.TestProfileItemCount]
        { DEFAULT_TOUCH_OUT_MODE, DEFAULT_TOUCH_OUT_MODE, DEFAULT_TOUCH_OUT_MODE,
          DEFAULT_TOUCH_OUT_MODE, DEFAULT_TOUCH_OUT_MODE, DEFAULT_TOUCH_OUT_MODE,
          DEFAULT_TOUCH_OUT_MODE, DEFAULT_TOUCH_OUT_MODE, DEFAULT_TOUCH_OUT_MODE };
        public GyroOutMode[] gyroOutMode = new GyroOutMode[ControllerLimits.TestProfileItemCount]
        { DEFAULT_GYRO_OUT_MODE, DEFAULT_GYRO_OUT_MODE, DEFAULT_GYRO_OUT_MODE,
          DEFAULT_GYRO_OUT_MODE, DEFAULT_GYRO_OUT_MODE, DEFAULT_GYRO_OUT_MODE,
          DEFAULT_GYRO_OUT_MODE, DEFAULT_GYRO_OUT_MODE, DEFAULT_GYRO_OUT_MODE };
        public GyroControlsInfo[] gyroControlsInf = new GyroControlsInfo[ControllerLimits.TestProfileItemCount]
        {
            new GyroControlsInfo(), new GyroControlsInfo(), new GyroControlsInfo(),
            new GyroControlsInfo(), new GyroControlsInfo(), new GyroControlsInfo(),
            new GyroControlsInfo(), new GyroControlsInfo(), new GyroControlsInfo(),
        };

        public RumbleSettings[] rumbleSettings = new RumbleSettings[ControllerLimits.TestProfileItemCount]
                {
            new RumbleSettings(), new RumbleSettings(),
            new RumbleSettings(), new RumbleSettings(),
            new RumbleSettings(), new RumbleSettings(),
            new RumbleSettings(), new RumbleSettings(),
            new RumbleSettings(),
                };

        public string[] sATriggers = new string[ControllerLimits.TestProfileItemCount]
        { BackingStore.DEFAULT_SA_TRIGGERS, BackingStore.DEFAULT_SA_TRIGGERS, BackingStore.DEFAULT_SA_TRIGGERS,
          BackingStore.DEFAULT_SA_TRIGGERS, BackingStore.DEFAULT_SA_TRIGGERS, BackingStore.DEFAULT_SA_TRIGGERS,
          BackingStore.DEFAULT_SA_TRIGGERS, BackingStore.DEFAULT_SA_TRIGGERS, BackingStore.DEFAULT_SA_TRIGGERS };
        public string[] sAMouseStickTriggers = new string[ControllerLimits.TestProfileItemCount]
        { BackingStore.DEFAULT_GYRO_MSTICK_TRIGGERS, BackingStore.DEFAULT_GYRO_MSTICK_TRIGGERS, BackingStore.DEFAULT_GYRO_MSTICK_TRIGGERS,
          BackingStore.DEFAULT_GYRO_MSTICK_TRIGGERS, BackingStore.DEFAULT_GYRO_MSTICK_TRIGGERS, BackingStore.DEFAULT_GYRO_MSTICK_TRIGGERS,
          BackingStore.DEFAULT_GYRO_MSTICK_TRIGGERS, BackingStore.DEFAULT_GYRO_MSTICK_TRIGGERS, BackingStore.DEFAULT_GYRO_MSTICK_TRIGGERS };
        public bool[] sATriggerCond = new bool[ControllerLimits.TestProfileItemCount]
        { DEFAULT_SA_TRIGGER_COND, DEFAULT_SA_TRIGGER_COND, DEFAULT_SA_TRIGGER_COND,
          DEFAULT_SA_TRIGGER_COND, DEFAULT_SA_TRIGGER_COND, DEFAULT_SA_TRIGGER_COND,
          DEFAULT_SA_TRIGGER_COND, DEFAULT_SA_TRIGGER_COND, DEFAULT_SA_TRIGGER_COND };
        public bool[] sAMouseStickTriggerCond = new bool[ControllerLimits.TestProfileItemCount]
        { DEFAULT_SA_MSTICK_TRIGGER_COND, DEFAULT_SA_MSTICK_TRIGGER_COND, DEFAULT_SA_MSTICK_TRIGGER_COND,
          DEFAULT_SA_MSTICK_TRIGGER_COND, DEFAULT_SA_MSTICK_TRIGGER_COND, DEFAULT_SA_MSTICK_TRIGGER_COND,
          DEFAULT_SA_MSTICK_TRIGGER_COND, DEFAULT_SA_MSTICK_TRIGGER_COND, DEFAULT_SA_MSTICK_TRIGGER_COND };
        public bool[] gyroMouseStickTriggerTurns = new bool[ControllerLimits.TestProfileItemCount]
        { DEFAULT_GYRO_MSTICK_TRIGGER_TURNS, DEFAULT_GYRO_MSTICK_TRIGGER_TURNS, DEFAULT_GYRO_MSTICK_TRIGGER_TURNS,
          DEFAULT_GYRO_MSTICK_TRIGGER_TURNS, DEFAULT_GYRO_MSTICK_TRIGGER_TURNS, DEFAULT_GYRO_MSTICK_TRIGGER_TURNS,
          DEFAULT_GYRO_MSTICK_TRIGGER_TURNS, DEFAULT_GYRO_MSTICK_TRIGGER_TURNS, DEFAULT_GYRO_MSTICK_TRIGGER_TURNS };
        public GyroMouseStickInfo[] gyroMStickInfo = new GyroMouseStickInfo[ControllerLimits.TestProfileItemCount]
        {
            new GyroMouseStickInfo(),
            new GyroMouseStickInfo(),
            new GyroMouseStickInfo(), new GyroMouseStickInfo(),
            new GyroMouseStickInfo(), new GyroMouseStickInfo(),
            new GyroMouseStickInfo(), new GyroMouseStickInfo(),
            new GyroMouseStickInfo(),
        };
        public GyroDirectionalSwipeInfo[] gyroSwipeInfo = new GyroDirectionalSwipeInfo[ControllerLimits.TestProfileItemCount]
        {
            new GyroDirectionalSwipeInfo(), new GyroDirectionalSwipeInfo(),
            new GyroDirectionalSwipeInfo(), new GyroDirectionalSwipeInfo(),
            new GyroDirectionalSwipeInfo(), new GyroDirectionalSwipeInfo(),
            new GyroDirectionalSwipeInfo(), new GyroDirectionalSwipeInfo(),
            new GyroDirectionalSwipeInfo(),
        };

        public bool[] gyroMouseStickToggle = new bool[ControllerLimits.TestProfileItemCount] { false, false, false,
            false, false, false, false, false, false };

        public SASteeringWheelEmulationAxisType[] sASteeringWheelEmulationAxis = new SASteeringWheelEmulationAxisType[ControllerLimits.TestProfileItemCount] { SASteeringWheelEmulationAxisType.None, SASteeringWheelEmulationAxisType.None, SASteeringWheelEmulationAxisType.None, SASteeringWheelEmulationAxisType.None, SASteeringWheelEmulationAxisType.None, SASteeringWheelEmulationAxisType.None, SASteeringWheelEmulationAxisType.None, SASteeringWheelEmulationAxisType.None, SASteeringWheelEmulationAxisType.None };
        public int[] sASteeringWheelEmulationRange = new int[ControllerLimits.TestProfileItemCount]
        { DEFAULT_SA_WHEEL_EMULATION_RANGE, DEFAULT_SA_WHEEL_EMULATION_RANGE, DEFAULT_SA_WHEEL_EMULATION_RANGE,
          DEFAULT_SA_WHEEL_EMULATION_RANGE, DEFAULT_SA_WHEEL_EMULATION_RANGE, DEFAULT_SA_WHEEL_EMULATION_RANGE,
          DEFAULT_SA_WHEEL_EMULATION_RANGE, DEFAULT_SA_WHEEL_EMULATION_RANGE, DEFAULT_SA_WHEEL_EMULATION_RANGE };
        public int[][] touchDisInvertTriggers = new int[ControllerLimits.TestProfileItemCount][]
        { new int[1] { DEFAULT_TOUCH_DIS_INVERT_TRIGGER}, new int[1] { DEFAULT_TOUCH_DIS_INVERT_TRIGGER}, new int[1] { DEFAULT_TOUCH_DIS_INVERT_TRIGGER },
          new int[1] { DEFAULT_TOUCH_DIS_INVERT_TRIGGER}, new int[1] { DEFAULT_TOUCH_DIS_INVERT_TRIGGER }, new int[1] { DEFAULT_TOUCH_DIS_INVERT_TRIGGER },
          new int[1] { DEFAULT_TOUCH_DIS_INVERT_TRIGGER }, new int[1] { DEFAULT_TOUCH_DIS_INVERT_TRIGGER}, new int[1] { DEFAULT_TOUCH_DIS_INVERT_TRIGGER } };
        public Boolean useExclusiveMode = false; // Re-enable Ex Mode

        public const int DEFAULT_FORM_WIDTH = WindowLayoutDefaults.MAIN_WINDOW_WIDTH;
        public int formWidth = DEFAULT_FORM_WIDTH;

        public const int DEFAULT_FORM_HEIGHT = WindowLayoutDefaults.MAIN_WINDOW_HEIGHT;
        public int formHeight = DEFAULT_FORM_HEIGHT;

        public const int DEFAULT_FORM_LOCATION_X = WindowLayoutDefaults.MAIN_WINDOW_LOCATION_X;
        public int formLocationX = DEFAULT_FORM_LOCATION_X;

        public const int DEFAULT_FORM_LOCATION_Y = WindowLayoutDefaults.MAIN_WINDOW_LOCATION_Y;
        public int formLocationY = DEFAULT_FORM_LOCATION_Y;

        public Boolean startMinimized = false;
        public Boolean minToTaskbar = false;
        public DateTime lastChecked;
        public string lastVersionChecked = string.Empty;
        public ulong lastVersionCheckedNum;

        // Legacy CheckWhen removed. Store explicit defaults for the new fields.
        public const int DEFAULT_CHECK_EVERY_VALUE = 1;
        public const int DEFAULT_CHECK_EVERY_UNIT = 1;
        // New explicit startup update-check persistence fields
        public bool checkUpdateStartup = true;
        // Phase7-Step10b W4: 既定値の出所を DEFAULT_CHECK_EVERY_VALUE／_UNIT に一本化した（値は従来と同じ）
        public int checkEveryValue = DEFAULT_CHECK_EVERY_VALUE; // default 1 day
        public int checkEveryUnit = DEFAULT_CHECK_EVERY_UNIT; // 0=hours,1=days

        public const int DEFAULT_NOTIFICATIONS = 2;
        public int notifications = DEFAULT_NOTIFICATIONS;
        public bool profileChangedNotification;
        public bool disconnectBTAtStop = false;

        public const bool DEFAULT_SWIPE_PROFILES = false;
        public bool swipeProfiles = DEFAULT_SWIPE_PROFILES;
        public bool ds4Mapping = false;
        public bool quickCharge = false;
        public bool useMoonlight = false;
        public bool useAdvancedMoonlight = false;
        public bool closeMini = false;
        public List<SpecialAction> actions = new List<SpecialAction>();
        public List<DS4ControlSettings>[] ds4settings = new List<DS4ControlSettings>[ControllerLimits.TestProfileItemCount]
            { new List<DS4ControlSettings>(), new List<DS4ControlSettings>(), new List<DS4ControlSettings>(),
              new List<DS4ControlSettings>(), new List<DS4ControlSettings>(), new List<DS4ControlSettings>(), new List<DS4ControlSettings>(), new List<DS4ControlSettings>(), new List<DS4ControlSettings>() };
        public ControlSettingsGroup[] ds4controlSettings;

        public List<string>[] profileActions = new List<string>[ControllerLimits.TestProfileItemCount] { null, null, null, null, null, null, null, null, null };
        public int[] profileActionCount = new int[ControllerLimits.TestProfileItemCount] { 0, 0, 0, 0, 0, 0, 0, 0, 0 };
        public Dictionary<string, SpecialAction>[] profileActionDict = new Dictionary<string, SpecialAction>[ControllerLimits.TestProfileItemCount]
            { new Dictionary<string, SpecialAction>(), new Dictionary<string, SpecialAction>(), new Dictionary<string, SpecialAction>(),
              new Dictionary<string, SpecialAction>(), new Dictionary<string, SpecialAction>(), new Dictionary<string, SpecialAction>(), new Dictionary<string, SpecialAction>(), new Dictionary<string, SpecialAction>(), new Dictionary<string, SpecialAction>() };

        public Dictionary<string, int>[] profileActionIndexDict = new Dictionary<string, int>[ControllerLimits.TestProfileItemCount]
            { new Dictionary<string, int>(), new Dictionary<string, int>(), new Dictionary<string, int>(),
              new Dictionary<string, int>(), new Dictionary<string, int>(), new Dictionary<string, int>(), new Dictionary<string, int>(), new Dictionary<string, int>(), new Dictionary<string, int>() };

        public string useLang = "";
        public bool downloadLang = true;
        public TrayIconChoice useIconChoice;
        public const bool DEFAULT_FLASH_WHEN_LATE = true;
        public bool flashWhenLate = DEFAULT_FLASH_WHEN_LATE;

        public const int DEFAULT_FLASH_WHEN_LATE_AT = 500;
        public int flashWhenLateAt = DEFAULT_FLASH_WHEN_LATE_AT;
        public bool useOSCServ = false;

        public const int DEFAULT_OSC_SERV_PORT = 9000;
        public int oscServPort = DEFAULT_OSC_SERV_PORT;

        public bool interpretingOscMonitoring = false;

        public bool useOSCSend = false;

        public const int DEFAULT_OSC_SEND_PORT = 9001;
        public int oscSendPort = DEFAULT_OSC_SEND_PORT;

        public const string DEFAULT_OSC_SEND_ADDRESS = "127.0.0.1";
        public string oscSendAddress = DEFAULT_OSC_SEND_ADDRESS;
        public bool useUDPServ = false;

        public const int DEFAULT_UDP_SERV_PORT = 26760;
        public int udpServPort = DEFAULT_UDP_SERV_PORT;

        // 127.0.0.1=IPAddress.Loopback (default), 0.0.0.0=IPAddress.Any as all interfaces, x.x.x.x = Specific ipv4 interface address or hostname
        public const string DEFAULT_UDP_SERV_LISTEN_ADDR = "127.0.0.1";
        public string udpServListenAddress = DEFAULT_UDP_SERV_LISTEN_ADDR;
        public bool useUdpSmoothing;

        public double udpSmoothingMincutoff = DEFAULT_UDP_SMOOTH_MINCUTOFF;
        public double udpSmoothingBeta = DEFAULT_UDP_SMOOTH_BETA;
        public bool useCustomSteamFolder;
        public string customSteamFolder;
        public AppThemeChoice useCurrentTheme;
        public string fakeExeFileName = string.Empty;

        public sbyte[] leftStickDriftXAxis = new sbyte[ControllerLimits.TestProfileItemCount]
        {
            0, 0, 0, 0, 0, 0, 0, 0, 0
        };
        public sbyte[] leftStickDriftYAxis = new sbyte[ControllerLimits.TestProfileItemCount]
        {
            0, 0, 0, 0, 0, 0, 0, 0, 0
        };
        public sbyte[] rightStickDriftXAxis = new sbyte[ControllerLimits.TestProfileItemCount]
        {
            0, 0, 0, 0, 0, 0, 0, 0, 0
        };
        public sbyte[] rightStickDriftYAxis = new sbyte[ControllerLimits.TestProfileItemCount]
        {
            0, 0, 0, 0, 0, 0, 0, 0, 0
        };

        public int[] debouncingMs = new int[ControllerLimits.TestProfileItemCount]
        {
            0, 0, 0, 0, 0, 0, 0, 0, 0
        };

        public bool[] inverseRumbleMotors = {
            false, false, false, false, false, false, false, false, false
        };

        public bool useDs3PitchRollSim = false;

        public ControlServiceDeviceOptions deviceOptions =
            new ControlServiceDeviceOptions();

        // Cache whether profile has custom action
        public bool[] containsCustomAction = new bool[ControllerLimits.TestProfileItemCount] { false, false, false, false, false, false, false, false, false };

        // Cache whether profile has custom extras
        public bool[] containsCustomExtras = new bool[ControllerLimits.TestProfileItemCount] { false, false, false, false, false, false, false, false, false };

        public int[] gyroSensitivity = new int[ControllerLimits.TestProfileItemCount]
        { DEFAULT_GYRO_SENS, DEFAULT_GYRO_SENS, DEFAULT_GYRO_SENS,
          DEFAULT_GYRO_SENS, DEFAULT_GYRO_SENS, DEFAULT_GYRO_SENS,
          DEFAULT_GYRO_SENS, DEFAULT_GYRO_SENS, DEFAULT_GYRO_SENS };
        public int[] gyroSensVerticalScale = new int[ControllerLimits.TestProfileItemCount]
        { DEFAULT_GYRO_SENS_VERTICAL_SCALE, DEFAULT_GYRO_SENS_VERTICAL_SCALE, DEFAULT_GYRO_SENS_VERTICAL_SCALE,
          DEFAULT_GYRO_SENS_VERTICAL_SCALE, DEFAULT_GYRO_SENS_VERTICAL_SCALE, DEFAULT_GYRO_SENS_VERTICAL_SCALE,
          DEFAULT_GYRO_SENS_VERTICAL_SCALE, DEFAULT_GYRO_SENS_VERTICAL_SCALE, DEFAULT_GYRO_SENS_VERTICAL_SCALE };
        public int[] gyroInvert = new int[ControllerLimits.TestProfileItemCount] { 0, 0, 0, 0, 0, 0, 0, 0, 0 };
        public bool[] gyroTriggerTurns = new bool[ControllerLimits.TestProfileItemCount]
        { DEFAULT_GYRO_TRIGGER_TURNS, DEFAULT_GYRO_TRIGGER_TURNS, DEFAULT_GYRO_TRIGGER_TURNS,
          DEFAULT_GYRO_TRIGGER_TURNS, DEFAULT_GYRO_TRIGGER_TURNS, DEFAULT_GYRO_TRIGGER_TURNS,
          DEFAULT_GYRO_TRIGGER_TURNS, DEFAULT_GYRO_TRIGGER_TURNS, DEFAULT_GYRO_TRIGGER_TURNS };

        public GyroMouseInfo[] gyroMouseInfo = new GyroMouseInfo[ControllerLimits.TestProfileItemCount]
        {
            new GyroMouseInfo(), new GyroMouseInfo(),
            new GyroMouseInfo(), new GyroMouseInfo(),
            new GyroMouseInfo(), new GyroMouseInfo(),
            new GyroMouseInfo(), new GyroMouseInfo(),
            new GyroMouseInfo(),
        };

        public int[] gyroMouseHorizontalAxis = new int[ControllerLimits.TestProfileItemCount] { 0, 0, 0, 0, 0, 0, 0, 0, 0 };

        public int[] gyroMouseStickHorizontalAxis = new int[ControllerLimits.TestProfileItemCount] { 0, 0, 0, 0, 0, 0, 0, 0, 0 };

        public bool[] trackballMode = new bool[ControllerLimits.TestProfileItemCount]
        { DEFAULT_TRACKBALL_MODE, DEFAULT_TRACKBALL_MODE, DEFAULT_TRACKBALL_MODE,
          DEFAULT_TRACKBALL_MODE, DEFAULT_TRACKBALL_MODE, DEFAULT_TRACKBALL_MODE,
          DEFAULT_TRACKBALL_MODE, DEFAULT_TRACKBALL_MODE, DEFAULT_TRACKBALL_MODE };
        public double[] trackballFriction = new double[ControllerLimits.TestProfileItemCount]
        { DEFAULT_TRACKBALL_FRICTION, DEFAULT_TRACKBALL_FRICTION, DEFAULT_TRACKBALL_FRICTION,
          DEFAULT_TRACKBALL_FRICTION, DEFAULT_TRACKBALL_FRICTION, DEFAULT_TRACKBALL_FRICTION,
          DEFAULT_TRACKBALL_FRICTION, DEFAULT_TRACKBALL_FRICTION, DEFAULT_TRACKBALL_FRICTION };
        //public bool[] touchStickTrackballMode = new bool[Global.TEST_PROFILE_ITEM_COUNT] { false, false, false, false, false, false, false, false, false };
        //public double[] touchStickTrackballFriction = new double[Global.TEST_PROFILE_ITEM_COUNT] { 10.0, 10.0, 10.0, 10.0, 10.0, 10.0, 10.0, 10.0, 10.0 };

        public TouchMouseStickInfo[] touchMStickInfo = new TouchMouseStickInfo[ControllerLimits.TestProfileItemCount]
        {
            new TouchMouseStickInfo(),
            new TouchMouseStickInfo(),
            new TouchMouseStickInfo(), new TouchMouseStickInfo(),
            new TouchMouseStickInfo(), new TouchMouseStickInfo(),
            new TouchMouseStickInfo(), new TouchMouseStickInfo(),
            new TouchMouseStickInfo(),
        };

        public TouchpadRelMouseSettings[] touchpadRelMouse = new TouchpadRelMouseSettings[ControllerLimits.TestProfileItemCount] { new TouchpadRelMouseSettings(), new TouchpadRelMouseSettings(), new TouchpadRelMouseSettings(), new TouchpadRelMouseSettings(),
            new TouchpadRelMouseSettings(), new TouchpadRelMouseSettings(), new TouchpadRelMouseSettings(), new TouchpadRelMouseSettings(), new TouchpadRelMouseSettings() };

        public TouchButtonActivationMode[] touchpadButtonMode = new TouchButtonActivationMode[ControllerLimits.TestProfileItemCount]
        {
            new TouchButtonActivationMode(), new TouchButtonActivationMode(), new TouchButtonActivationMode(), new TouchButtonActivationMode(),
            new TouchButtonActivationMode(), new TouchButtonActivationMode(), new TouchButtonActivationMode(), new TouchButtonActivationMode(), new TouchButtonActivationMode(),
        };

        // Used to hold the controller type desired in a profile
        public OutContType[] outputDevType = new OutContType[ControllerLimits.TestProfileItemCount]
        { DEFAULT_OUT_CONT_TYPE, DEFAULT_OUT_CONT_TYPE, DEFAULT_OUT_CONT_TYPE,
          DEFAULT_OUT_CONT_TYPE, DEFAULT_OUT_CONT_TYPE, DEFAULT_OUT_CONT_TYPE,
          DEFAULT_OUT_CONT_TYPE, DEFAULT_OUT_CONT_TYPE, DEFAULT_OUT_CONT_TYPE};

        public const bool DEFAULT_OUTPUT_VIRTUAL_TRIG_BUTTONS = true;
        public bool[] outputVirtualTriggerButtons = new bool[ControllerLimits.TestProfileItemCount]
        {
            DEFAULT_OUTPUT_VIRTUAL_TRIG_BUTTONS, DEFAULT_OUTPUT_VIRTUAL_TRIG_BUTTONS, DEFAULT_OUTPUT_VIRTUAL_TRIG_BUTTONS, DEFAULT_OUTPUT_VIRTUAL_TRIG_BUTTONS,
            DEFAULT_OUTPUT_VIRTUAL_TRIG_BUTTONS, DEFAULT_OUTPUT_VIRTUAL_TRIG_BUTTONS, DEFAULT_OUTPUT_VIRTUAL_TRIG_BUTTONS, DEFAULT_OUTPUT_VIRTUAL_TRIG_BUTTONS,
            DEFAULT_OUTPUT_VIRTUAL_TRIG_BUTTONS,
        };

        public const DS4TriggerOutputMode DEFAULT_DS4_TRIGGER_OUTPUT = DS4TriggerOutputMode.Default;
        public DS4TriggerOutputMode[] outputDS4TriggerMode = new DS4TriggerOutputMode[ControllerLimits.TestProfileItemCount]
        {
            DEFAULT_DS4_TRIGGER_OUTPUT,DEFAULT_DS4_TRIGGER_OUTPUT,DEFAULT_DS4_TRIGGER_OUTPUT,
            DEFAULT_DS4_TRIGGER_OUTPUT,DEFAULT_DS4_TRIGGER_OUTPUT,DEFAULT_DS4_TRIGGER_OUTPUT,
            DEFAULT_DS4_TRIGGER_OUTPUT,DEFAULT_DS4_TRIGGER_OUTPUT,DEFAULT_DS4_TRIGGER_OUTPUT,
        };


        // TRUE=AutoProfile reverts to default profile if current foreground process is unknown, FALSE=Leave existing profile active when a foreground proces is unknown (ie. no matching auto-profile rule)
        public const bool DEFAULT_AUTO_PROFILE_REVERT_DEFAULT_PROFILE = true;
        public bool autoProfileRevertDefaultProfile = DEFAULT_AUTO_PROFILE_REVERT_DEFAULT_PROFILE;
        public AutoProfileDisplayProfileSwitchChoices autoProfileSwitchNotifyChoice =
            AutoProfileDisplayProfileSwitchChoices.None;

        bool tempBool = false;

        // Phase7-Step5 W4（Q6＝A）: 旧 AppHost.GetService<IProcessInspector>()。ServiceRegistration の BackingStore の登録が設定する。
        // 未設定のとき（コンテナを通らないテストなど）は、LoadProfile が従来のフォールバック（プロセス一覧の走査）を使う。
        // TODO(Phase7-Step10): BackingStore の分割（永続化の切り出し）で、コンストラクター注入にする。
        public DS4Windows.Services.IProcessInspector ProcessInspector { get; set; }

        // Phase7-Step6 W1（Q2＝A）: 旧 Global.activeOutDevType。スロットごとの、現在接続中の出力種別（実体は OutputSlotService が持つ）。
        // ServiceRegistration の IOutputSlotService の登録が、OutputSlotService の生成時に ActiveOutDevType の配列を設定する。
        // 設定前（コンテナ構築前の起動処理、コンテナを通らないテスト）は、全スロット None の既定の配列を読む（接続前は元々 None）。
        // 書き込むのは ControlService だけで、既定の配列は書き換わらない。配列の参照は再代入されない前提で使うこと。
        // TODO(Phase7-Step10): BackingStore の分割で、コンストラクター注入にする。
        public OutContType[] ActiveOutDevType { get; set; } = new OutContType[ControllerLimits.TestProfileItemCount];

        public BackingStore()
        {
            ds4controlSettings = new ControlSettingsGroup[ControllerLimits.TestProfileItemCount];

            for (int i = 0; i < ControllerLimits.TestProfileItemCount; i++)
            {
                foreach (DS4Controls dc in Enum.GetValues(typeof(DS4Controls)))
                {
                    if (dc != DS4Controls.None)
                        ds4settings[i].Add(new DS4ControlSettings(dc));
                }

                ds4controlSettings[i] = new ControlSettingsGroup(ds4settings[i]);

                // RumbleSettings の変更を生配列 rumble / rumbleAutostopTime に連動させる
                int slot = i;
                rumbleSettings[slot].RumbleSettingsChanged += (sender, e) =>
                {
                    if (rumble != null && slot < rumble.Length)
                        rumble[slot] = rumbleSettings[slot].RumbleBoost;
                    if (rumbleAutostopTime != null && slot < rumbleAutostopTime.Length)
                        rumbleAutostopTime[slot] = rumbleSettings[slot].RumbleAutostopTime;
                };

                EstablishDefaultSpecialActions(i);
                CacheExtraProfileInfo(i);
            }

            SetupDefaultColors();
        }
        public void EstablishDefaultSpecialActions(int idx)
        {
            // Do not overwrite an existing profileActions list. Only seed a default
            // when the list is missing (null). This ensures we only add the default
            // Disconnect action for truly new/unspecified profiles and won't force-enable
            // it for existing profiles on startup.
            if (profileActions[idx] != null)
            {
                profileActionCount[idx] = profileActions[idx].Count;
                return;
            }

            // Initialize to an empty list for unspecified profiles but do NOT
            // automatically add any Disconnect action. That behavior caused
            // unwanted enabling of Disconnect for profiles that simply had no
            // explicit action configuration.
            profileActions[idx] = new List<string>();
        }

        public void CacheProfileCustomsFlags(int device)
        {
            bool customAct = false;
            containsCustomAction[device] = customAct = HasCustomActions(device);
            containsCustomExtras[device] = HasCustomExtras(device);

            {
                customAct = gyroOutMode[device] == GyroOutMode.MouseJoystick;
                customAct = customAct || lsOutputSettings[device].mode != StickMode.Controls;
                customAct = customAct || rsOutputSettings[device].mode != StickMode.Controls;
                containsCustomAction[device] = customAct;
            }
        }

        private void SetupDefaultColors()
        {
            lightbarSettingInfo[0].ds4winSettings.m_Led = new DS4Color(Color.Blue);
            lightbarSettingInfo[1].ds4winSettings.m_Led = new DS4Color(Color.Red);
            lightbarSettingInfo[2].ds4winSettings.m_Led = new DS4Color(Color.Green);
            lightbarSettingInfo[3].ds4winSettings.m_Led = new DS4Color(Color.Pink);
            lightbarSettingInfo[4].ds4winSettings.m_Led = new DS4Color(Color.Blue);
            lightbarSettingInfo[5].ds4winSettings.m_Led = new DS4Color(Color.Red);
            lightbarSettingInfo[6].ds4winSettings.m_Led = new DS4Color(Color.Green);
            lightbarSettingInfo[7].ds4winSettings.m_Led = new DS4Color(Color.Pink);
            lightbarSettingInfo[8].ds4winSettings.m_Led = new DS4Color(Color.White);
        }

        public void CacheExtraProfileInfo(int device)
        {
            CalculateProfileActionCount(device);
            CalculateProfileActionDicts(device);
            CacheProfileCustomsFlags(device);
        }

        public void CalculateProfileActionCount(int index)
        {
            profileActionCount[index] = profileActions[index].Count;
        }

        public void CalculateProfileActionDicts(int device)
        {
            profileActionDict[device].Clear();
            profileActionIndexDict[device].Clear();

            foreach (string actionname in profileActions[device])
            {
                var actionObj = GetAction(actionname);
                int index = GetActionIndexOf(actionname);
                profileActionDict[device][actionname] = actionObj;
                profileActionIndexDict[device][actionname] = index;
            }
        }

        /// <summary>
        /// Emit "Invalid action index" logs for missing actions in the profile for the given device.
        /// Respects loggedInvalidActions unless forceEmit is true.
        /// </summary>
        /// <param name="device">Device index</param>
        /// <param name="forceEmit">If true, ignore suppression and always emit logs</param>
        /// <param name="overrideProfileName">If provided, use this profile name instead of looking up profilePath[device]</param>
        public void EmitMissingActionLogsForDevice(int device, bool forceEmit = false, string overrideProfileName = null)
        {
            if (profileActions == null || device < 0 || device >= profileActions.Length) return;

            foreach (string actionname in profileActions[device])
            {
                var actionObj = GetAction(actionname);
                int index = GetActionIndexOf(actionname);
                if (actionObj == null || actionObj.name == "null" || index < 0)
                {
                    if (forceEmit || !loggedInvalidActions.Contains(actionname))
                    {
                        string profName;
                        if (overrideProfileName != null)
                        {
                            // Use explicitly provided profile name
                            profName = overrideProfileName;
                        }
                        else
                        {
                            // Determine the profile name actually in use for this device (temporary or regular)
                            profName = (Global.ProfileSettingsServiceInstance.GetUseTempProfile(device))
                                ? Global.ProfileSettingsServiceInstance.GetTempProfileName(device)
                                : (profilePath != null && device >= 0 && device < profilePath.Length ? profilePath[device] : string.Empty);
                        }

                        string displayProfile = string.IsNullOrEmpty(profName) ? "(unknown)" : profName;
                        AppLogger.LogToGui($"Profile '{displayProfile}' contains an invalid special action '{actionname}'.", false);
                        try
                        {
                            loggedInvalidActions.Add(actionname);
                        }
                        catch { }
                    }
                }
            }
        }

        public SpecialAction GetAction(string name)
        {
            //foreach (SpecialAction sA in actions)
            for (int i = 0, actionCount = actions.Count; i < actionCount; i++)
            {
                SpecialAction sA = actions[i];
                var normExisting = Util.NormalizeActionName(sA?.name);
                var normSearch = Util.NormalizeActionName(name);
                if (string.Equals(normExisting, normSearch, StringComparison.OrdinalIgnoreCase))
                    return sA;
            }

            return new SpecialAction("null", "null", "null", "null");
        }

        public int GetActionIndexOf(string name)
        {
            for (int i = 0, actionCount = actions.Count; i < actionCount; i++)
            {
                var normExisting = Util.NormalizeActionName(actions[i]?.name);
                var normSearch = Util.NormalizeActionName(name);
                if (string.Equals(normExisting, normSearch, StringComparison.OrdinalIgnoreCase))
                    return i;
            }

            return -1;
        }

        public string stickOutputCurveString(int id)
        {
            string result = "linear";
            switch (id)
            {
                case 0: break;
                case 1: result = "enhanced-precision"; break;
                case 2: result = "quadratic"; break;
                case 3: result = "cubic"; break;
                case 4: result = "easeout-quad"; break;
                case 5: result = "easeout-cubic"; break;
                case 6: result = "custom"; break;
                default: break;
            }

            return result;
        }

        public int stickOutputCurveId(string name)
        {
            int id = 0;
            switch (name)
            {
                case "linear": id = 0; break;
                case "enhanced-precision": id = 1; break;
                case "quadratic": id = 2; break;
                case "cubic": id = 3; break;
                case "easeout-quad": id = 4; break;
                case "easeout-cubic": id = 5; break;
                case "custom": id = 6; break;
                default: break;
            }

            return id;
        }


        public static bool SaTriggerCondValue(string text)
        {
            bool result = true;
            switch (text)
            {
                case "and": result = true; break;
                case "or": result = false; break;
                default: result = true; break;
            }

            return result;
        }

        public static string SaTriggerCondString(bool value)
        {
            string result = value ? "and" : "or";
            return result;
        }

        public void SetSaTriggerCond(int index, string text)
        {
            sATriggerCond[index] = SaTriggerCondValue(text);
        }

        public void SetSaMouseStickTriggerCond(int index, string text)
        {
            sAMouseStickTriggerCond[index] = SaTriggerCondValue(text);
        }

        public void SetGyroMouseDZ(int index, int value, ControlService control)
        {
            gyroMouseDZ[index] = value;
            if (index < ControlService.CURRENT_DS4_CONTROLLER_LIMIT && control.touchPad[index] != null)
                control.touchPad[index].CursorGyroDead = value;
        }

        public void SetGyroControlsToggle(int index, bool value, ControlService control)
        {
            gyroControlsInf[index].triggerToggle = value;
            if (index < ControlService.CURRENT_DS4_CONTROLLER_LIMIT && control.touchPad[index] != null)
                control.touchPad[index].ToggleGyroControls = value;
        }

        public void SetGyroMouseToggle(int index, bool value, ControlService control)
        {
            gyroMouseToggle[index] = value;
            if (index < ControlService.CURRENT_DS4_CONTROLLER_LIMIT && control.touchPad[index] != null)
                control.touchPad[index].ToggleGyroMouse = value;
        }

        public void SetGyroMouseStickToggle(int index, bool value, ControlService control)
        {
            gyroMouseStickToggle[index] = value;
            if (index < ControlService.CURRENT_DS4_CONTROLLER_LIMIT && control.touchPad[index] != null)
                control.touchPad[index].ToggleGyroStick = value;
        }


        public DS4Controls getDS4ControlsByName(string key)
        {
            if (!key.StartsWith("bn"))
                return (DS4Controls)Enum.Parse(typeof(DS4Controls), key, true);

            switch (key)
            {
                case "bnShare": return DS4Controls.Share;
                case "bnL3": return DS4Controls.L3;
                case "bnR3": return DS4Controls.R3;
                case "bnOptions": return DS4Controls.Options;
                case "bnUp": return DS4Controls.DpadUp;
                case "bnRight": return DS4Controls.DpadRight;
                case "bnDown": return DS4Controls.DpadDown;
                case "bnLeft": return DS4Controls.DpadLeft;

                case "bnL1": return DS4Controls.L1;
                case "bnR1": return DS4Controls.R1;
                case "bnTriangle": return DS4Controls.Triangle;
                case "bnCircle": return DS4Controls.Circle;
                case "bnCross": return DS4Controls.Cross;
                case "bnSquare": return DS4Controls.Square;

                case "bnPS": return DS4Controls.PS;
                case "bnLSLeft": return DS4Controls.LXNeg;
                case "bnLSUp": return DS4Controls.LYNeg;
                case "bnRSLeft": return DS4Controls.RXNeg;
                case "bnRSUp": return DS4Controls.RYNeg;

                case "bnLSRight": return DS4Controls.LXPos;
                case "bnLSDown": return DS4Controls.LYPos;
                case "bnRSRight": return DS4Controls.RXPos;
                case "bnRSDown": return DS4Controls.RYPos;
                case "bnL2": return DS4Controls.L2;
                case "bnR2": return DS4Controls.R2;

                case "bnTouchLeft": return DS4Controls.TouchLeft;
                case "bnTouchMulti": return DS4Controls.TouchMulti;
                case "bnTouchUpper": return DS4Controls.TouchUpper;
                case "bnTouchRight": return DS4Controls.TouchRight;
                case "bnGyroXP": return DS4Controls.GyroXPos;
                case "bnGyroXN": return DS4Controls.GyroXNeg;
                case "bnGyroZP": return DS4Controls.GyroZPos;
                case "bnGyroZN": return DS4Controls.GyroZNeg;

                case "bnSwipeUp": return DS4Controls.SwipeUp;
                case "bnSwipeDown": return DS4Controls.SwipeDown;
                case "bnSwipeLeft": return DS4Controls.SwipeLeft;
                case "bnSwipeRight": return DS4Controls.SwipeRight;
                case "bnTouchStarted": return DS4Controls.TouchStarted;
                case "bnTouchEnded": return DS4Controls.TouchEnded;

                #region OldShiftname
                case "sbnShare": return DS4Controls.Share;
                case "sbnL3": return DS4Controls.L3;
                case "sbnR3": return DS4Controls.R3;
                case "sbnOptions": return DS4Controls.Options;
                case "sbnUp": return DS4Controls.DpadUp;
                case "sbnRight": return DS4Controls.DpadRight;
                case "sbnDown": return DS4Controls.DpadDown;
                case "sbnLeft": return DS4Controls.DpadLeft;

                case "sbnL1": return DS4Controls.L1;
                case "sbnR1": return DS4Controls.R1;
                case "sbnTriangle": return DS4Controls.Triangle;
                case "sbnCircle": return DS4Controls.Circle;
                case "sbnCross": return DS4Controls.Cross;
                case "sbnSquare": return DS4Controls.Square;

                case "sbnPS": return DS4Controls.PS;
                case "sbnLSLeft": return DS4Controls.LXNeg;
                case "sbnLSUp": return DS4Controls.LYNeg;
                case "sbnRSLeft": return DS4Controls.RXNeg;
                case "sbnRSUp": return DS4Controls.RYNeg;

                case "sbnLSRight": return DS4Controls.LXPos;
                case "sbnLSDown": return DS4Controls.LYPos;
                case "sbnRSRight": return DS4Controls.RXPos;
                case "sbnRSDown": return DS4Controls.RYPos;
                case "sbnL2": return DS4Controls.L2;
                case "sbnR2": return DS4Controls.R2;

                case "sbnTouchLeft": return DS4Controls.TouchLeft;
                case "sbnTouchMulti": return DS4Controls.TouchMulti;
                case "sbnTouchUpper": return DS4Controls.TouchUpper;
                case "sbnTouchRight": return DS4Controls.TouchRight;
                case "sbnGsyroXP": return DS4Controls.GyroXPos;
                case "sbnGyroXN": return DS4Controls.GyroXNeg;
                case "sbnGyroZP": return DS4Controls.GyroZPos;
                case "sbnGyroZN": return DS4Controls.GyroZNeg;
                #endregion

                case "bnShiftShare": return DS4Controls.Share;
                case "bnShiftL3": return DS4Controls.L3;
                case "bnShiftR3": return DS4Controls.R3;
                case "bnShiftOptions": return DS4Controls.Options;
                case "bnShiftUp": return DS4Controls.DpadUp;
                case "bnShiftRight": return DS4Controls.DpadRight;
                case "bnShiftDown": return DS4Controls.DpadDown;
                case "bnShiftLeft": return DS4Controls.DpadLeft;

                case "bnShiftL1": return DS4Controls.L1;
                case "bnShiftR1": return DS4Controls.R1;
                case "bnShiftTriangle": return DS4Controls.Triangle;
                case "bnShiftCircle": return DS4Controls.Circle;
                case "bnShiftCross": return DS4Controls.Cross;
                case "bnShiftSquare": return DS4Controls.Square;

                case "bnShiftPS": return DS4Controls.PS;
                case "bnShiftLSLeft": return DS4Controls.LXNeg;
                case "bnShiftLSUp": return DS4Controls.LYNeg;
                case "bnShiftRSLeft": return DS4Controls.RXNeg;
                case "bnShiftRSUp": return DS4Controls.RYNeg;

                case "bnShiftLSRight": return DS4Controls.LXPos;
                case "bnShiftLSDown": return DS4Controls.LYPos;
                case "bnShiftRSRight": return DS4Controls.RXPos;
                case "bnShiftRSDown": return DS4Controls.RYPos;
                case "bnShiftL2": return DS4Controls.L2;
                case "bnShiftR2": return DS4Controls.R2;

                case "bnShiftTouchLeft": return DS4Controls.TouchLeft;
                case "bnShiftTouchMulti": return DS4Controls.TouchMulti;
                case "bnShiftTouchUpper": return DS4Controls.TouchUpper;
                case "bnShiftTouchRight": return DS4Controls.TouchRight;
                case "bnShiftGyroXP": return DS4Controls.GyroXPos;
                case "bnShiftGyroXN": return DS4Controls.GyroXNeg;
                case "bnShiftGyroZP": return DS4Controls.GyroZPos;
                case "bnShiftGyroZN": return DS4Controls.GyroZNeg;

                case "bnShiftSwipeUp": return DS4Controls.SwipeUp;
                case "bnShiftSwipeDown": return DS4Controls.SwipeDown;
                case "bnShiftSwipeLeft": return DS4Controls.SwipeLeft;
                case "bnShiftSwipeRight": return DS4Controls.SwipeRight;

                case "bnShiftTouchStarted": return DS4Controls.TouchStarted;
                case "bnShiftTouchEnded": return DS4Controls.TouchEnded;
            }

            return 0;
        }

        public X360Controls getX360ControlsByName(string key)
        {
            X360Controls x3c;
            if (Enum.TryParse(key, true, out x3c))
                return x3c;

            switch (key)
            {
                case "Back": return X360Controls.Back;
                case "Left Stick": return X360Controls.LS;
                case "Right Stick": return X360Controls.RS;
                case "Start": return X360Controls.Start;
                case "Up Button": return X360Controls.DpadUp;
                case "Right Button": return X360Controls.DpadRight;
                case "Down Button": return X360Controls.DpadDown;
                case "Left Button": return X360Controls.DpadLeft;

                case "Left Bumper": return X360Controls.LB;
                case "Right Bumper": return X360Controls.RB;
                case "Y Button": return X360Controls.Y;
                case "B Button": return X360Controls.B;
                case "A Button": return X360Controls.A;
                case "X Button": return X360Controls.X;

                case "Guide": return X360Controls.Guide;
                case "Left X-Axis-": return X360Controls.LXNeg;
                case "Left Y-Axis-": return X360Controls.LYNeg;
                case "Right X-Axis-": return X360Controls.RXNeg;
                case "Right Y-Axis-": return X360Controls.RYNeg;

                case "Left X-Axis+": return X360Controls.LXPos;
                case "Left Y-Axis+": return X360Controls.LYPos;
                case "Right X-Axis+": return X360Controls.RXPos;
                case "Right Y-Axis+": return X360Controls.RYPos;
                case "Left Trigger": return X360Controls.LT;
                case "Right Trigger": return X360Controls.RT;
                case "Touchpad Click": return X360Controls.TouchpadClick;

                case "Left Mouse Button": return X360Controls.LeftMouse;
                case "Right Mouse Button": return X360Controls.RightMouse;
                case "Middle Mouse Button": return X360Controls.MiddleMouse;
                case "4th Mouse Button": return X360Controls.FourthMouse;
                case "5th Mouse Button": return X360Controls.FifthMouse;
                case "Mouse Wheel Up": return X360Controls.WUP;
                case "Mouse Wheel Down": return X360Controls.WDOWN;
                case "Mouse Up": return X360Controls.MouseUp;
                case "Mouse Down": return X360Controls.MouseDown;
                case "Mouse Left": return X360Controls.MouseLeft;
                case "Mouse Right": return X360Controls.MouseRight;
                case "Unbound": return X360Controls.Unbound;
            }

            return X360Controls.Unbound;
        }

        // Phase7-Step10b W1: getX360ControlString（XML 保存用の名前）は X360ControlTables へ移した。


        // Legacy Load implementation (formerly LoadOld) will be the canonical Load method below.


        // Phase7-Step1 W3: Load() の末尾にあった偽の exe 名の読み込みを切り出した（旧形式の経路と DTO の経路の両方から呼ぶ。処理は同一）


        public void PostProcessLoad()
        {
            // Check if any set profile names should be considered Distance profiles
            for (int i = 0; i < ControllerLimits.MaxControllerCount; i++)
            {
                if (profilePath[i] != null && profilePath[i].ToLower().Contains("distance"))
                {
                    distanceProfiles[i] = true;
                }
            }

            // Compile shortcut version number if lastVersionChecked is populated
            if (!string.IsNullOrEmpty(lastVersionChecked))
            {
                lastVersionCheckedNum = VersionUtil.CompileVersionNumberFromString(lastVersionChecked);
                if (lastVersionCheckedNum == 0) lastVersionChecked = string.Empty;
            }

            oscServPort = Math.Clamp(oscServPort, 1024, 65535);
            oscSendPort = Math.Clamp(oscSendPort, 1024, 65535);
            udpServPort = Math.Clamp(udpServPort, 1024, 65535);

            udpSmoothingMincutoff = Math.Clamp(udpSmoothingMincutoff, 0.00001, 100.0);
            udpSmoothingBeta = Math.Clamp(udpSmoothingBeta, 0.0, 1.0);
        }

        public static void ParseCustomLedString(string source, LightbarDS4WinInfo destination)
        {
            try
            {
                string[] ss = source.Split(':');
                bool.TryParse(ss[0], out destination.useCustomLed);
                DS4Color.TryParse(ss[1], ref destination.m_CustomLed);
            }
            catch
            {
                destination.useCustomLed = false;
                destination.m_CustomLed = new DS4Color(Color.Blue);
            }
        }

        public static string CompileCustomLedString(LightbarDS4WinInfo ledInfo)
        {
            string result = $"{ledInfo.useCustomLed}:{ledInfo.m_CustomLed.red},{ledInfo.m_CustomLed.green},{ledInfo.m_CustomLed.blue}";
            return result;

        }

        public void PopulateLightbarDS4WinInfo(int index, LightbarDS4WinInfo source)
        {
            if (index < 0 && index > ControllerLimits.MaxControllerCount)
            {
                throw new ArgumentOutOfRangeException();
            }

            lightbarSettingInfo[index].ds4winSettings.useCustomLed = source.useCustomLed;
            lightbarSettingInfo[index].ds4winSettings.m_CustomLed = source.m_CustomLed;
        }

        public LightbarDS4WinInfo ObtainLightbarDS4WinInfo(int index)
        {
            if (index < 0 && index > ControllerLimits.MaxControllerCount)
            {
                throw new ArgumentOutOfRangeException();
            }

            return lightbarSettingInfo[index].ds4winSettings;
        }


        // Phase7-Step7 W2（Q4＝A）: Actions.xml の読み込み（または既定の作成）の後に発行する。BackingStore から Mapping への逆参照をなくすための通知。
        // コンテナ構築前の読み込みでは購読者がいないが、その時点では実行時状態は新品であり、結果は同じ。
        public event Action ActionsLoaded;

        // Phase7-Step10c W2: Actions.xml の読込を担う SpecialActionsXmlSerializer が呼ぶため、公開にした（ActionsLoaded イベントの発行元は BackingStore のまま）
        public void RaiseActionsLoaded()
        {
            ActionsLoaded?.Invoke();
        }


        public void UpdateDS4CSetting(int deviceNum, string buttonName, bool shift, object action, string exts, DS4KeyType kt, int trigger = 0)
        {
            DS4Controls dc;
            if (buttonName.StartsWith("bn"))
                dc = getDS4ControlsByName(buttonName);
            else
                dc = (DS4Controls)Enum.Parse(typeof(DS4Controls), buttonName, true);

            int temp = (int)dc;
            if (temp > 0)
            {
                int index = temp - 1;
                DS4ControlSettings dcs = ds4settings[deviceNum][index];
                dcs.UpdateSettings(shift, action, exts, kt, trigger);
                Global.RefreshActionAlias(dcs, shift);
            }
        }

        public void UpdateDS4CExtra(int deviceNum, string buttonName, bool shift, string exts)
        {
            DS4Controls dc;
            if (buttonName.StartsWith("bn"))
                dc = getDS4ControlsByName(buttonName);
            else
                dc = (DS4Controls)Enum.Parse(typeof(DS4Controls), buttonName, true);

            int temp = (int)dc;
            if (temp > 0)
            {
                int index = temp - 1;
                DS4ControlSettings dcs = ds4settings[deviceNum][index];
                if (shift)
                    dcs.shiftExtras = exts;
                else
                    dcs.extras = exts;
            }
        }

        public void UpdateDS4CKeyType(int deviceNum, string buttonName, bool shift, DS4KeyType keyType)
        {
            DS4Controls dc;
            if (buttonName.StartsWith("bn"))
                dc = getDS4ControlsByName(buttonName);
            else
                dc = (DS4Controls)Enum.Parse(typeof(DS4Controls), buttonName, true);

            int temp = (int)dc;
            if (temp > 0)
            {
                int index = temp - 1;
                DS4ControlSettings dcs = ds4settings[deviceNum][index];
                if (shift)
                    dcs.shiftKeyType = keyType;
                else
                    dcs.keyType = keyType;
            }
        }

        public DS4ControlSettings GetDS4CSetting(int deviceNum, string buttonName)
        {
            DS4Controls dc;
            if (buttonName.StartsWith("bn"))
                dc = getDS4ControlsByName(buttonName);
            else
                dc = (DS4Controls)Enum.Parse(typeof(DS4Controls), buttonName, true);

            int temp = (int)dc;
            if (temp > 0)
            {
                int index = temp - 1;
                DS4ControlSettings dcs = ds4settings[deviceNum][index];
                return dcs;
            }

            return null;
        }

        public DS4ControlSettings GetDS4CSetting(int deviceNum, DS4Controls dc)
        {
            int temp = (int)dc;
            if (temp > 0)
            {
                int index = temp - 1;
                DS4ControlSettings dcs = ds4settings[deviceNum][index];
                return dcs;
            }

            return null;
        }

        public bool HasCustomActions(int deviceNum)
        {
            List<DS4ControlSettings> ds4settingsList = ds4settings[deviceNum];
            for (int i = 0, settingsLen = ds4settingsList.Count; i < settingsLen; i++)
            {
                DS4ControlSettings dcs = ds4settingsList[i];
                if (dcs.actionType != DS4ControlSettings.ActionType.Default || dcs.shiftActionType != DS4ControlSettings.ActionType.Default)
                    return true;
            }

            return false;
        }

        public bool HasCustomExtras(int deviceNum)
        {
            List<DS4ControlSettings> ds4settingsList = ds4settings[deviceNum];
            for (int i = 0, settingsLen = ds4settingsList.Count; i < settingsLen; i++)
            {
                DS4ControlSettings dcs = ds4settingsList[i];
                if (dcs.extras != null || dcs.shiftExtras != null)
                    return true;
            }

            return false;
        }

        // Phase6-Step12: 旧形式の保存・読込に欠けていた設定（ProfileDTO にあり、旧形式に無かった 10 要素）の読み書き。


        // Phase7-Step10c W4: プロファイルの読込を担う LegacyProfileXmlSerializer が呼ぶため公開にした（プリセットの読込［Phase7-Step10c W6 まで BackingStore］も使う）
        public void ResetMouseProperties(int device, ControlService control)
        {
            if (device < ControllerLimits.MaxControllerCount &&
                    control.touchPad[device] != null)
            {
                control.touchPad[device]?.ResetToggleGyroModes();
            }
        }

        // Phase7-Step10c W4: プロファイルの読込を担う LegacyProfileXmlSerializer が呼ぶため公開にした（プリセットの読込［Phase7-Step10c W6 まで BackingStore］も使う）
        public void ResetProfile(int device)
        {
            buttonMouseInfos[device].Reset();
            gyroControlsInf[device].Reset();

            enableTouchToggle[device] = DEFAULT_TOUCH_TOGGLE;
            idleDisconnectTimeout[device] = 0;
            enableOutputDataToDS4[device] = DEFAULT_OUTPUT_TO_DS4;
            touchpadJitterCompensation[device] = DEFAULT_TOUCHPAD_JITTER_COMP;
            // Phase6-Step12: 旧形式の読込に追加した設定の既定値（要素の無いプロファイルで、前の値を引き継がないようにする）。
            // useDs3PitchRollSim は全体で 1 つの値のため、ここではリセットしない。
            debouncingMs[device] = 0;
            inverseRumbleMotors[device] = false;
            leftStickDriftXAxis[device] = 0;
            leftStickDriftYAxis[device] = 0;
            rightStickDriftXAxis[device] = 0;
            rightStickDriftYAxis[device] = 0;
            outputDS4TriggerMode[device] = DEFAULT_DS4_TRIGGER_OUTPUT;
            lowerRCOn[device] = false;
            touchClickPassthru[device] = false;

            rumble[device] = DEFAULT_RUMBLE;
            rumbleAutostopTime[device] = 0;
            touchSensitivity[device] = DEFAULT_TOUCHPAD_SENS;

            lsModInfo[device].Reset();
            rsModInfo[device].Reset();
            lsModInfo[device].deadZone = rsModInfo[device].deadZone = StickDeadZoneInfo.DEFAULT_DEADZONE;
            lsModInfo[device].antiDeadZone = rsModInfo[device].antiDeadZone = StickDeadZoneInfo.DEFAULT_ANTIDEADZONE;
            lsModInfo[device].maxZone = rsModInfo[device].maxZone = StickDeadZoneInfo.DEFAULT_MAXZONE;
            lsModInfo[device].maxOutput = rsModInfo[device].maxOutput = StickDeadZoneInfo.DEFAULT_MAXOUTPUT;
            lsModInfo[device].fuzz = rsModInfo[device].fuzz = StickDeadZoneInfo.DEFAULT_FUZZ;

            //l2ModInfo[device].deadZone = r2ModInfo[device].deadZone = 0;
            //l2ModInfo[device].antiDeadZone = r2ModInfo[device].antiDeadZone = 0;
            //l2ModInfo[device].maxZone = r2ModInfo[device].maxZone = 100;
            //l2ModInfo[device].maxOutput = r2ModInfo[device].maxOutput = 100.0;
            l2ModInfo[device].Reset();
            r2ModInfo[device].Reset();

            // Rotation angles expressed in radians
            LSRotation[device] = 0.0;
            RSRotation[device] = 0.0;

            SXDeadzone[device] = SZDeadzone[device] = DEFAULT_SX_TILT_DEADZONE;
            SXMaxzone[device] = SZMaxzone[device] = DEFAULT_RUMBLE;
            SXAntiDeadzone[device] = SZAntiDeadzone[device] = 0.0;
            l2Sens[device] = r2Sens[device] = DEFAULT_ANALOG_SENS;
            LSSens[device] = RSSens[device] = DEFAULT_ANALOG_SENS;
            SXSens[device] = SZSens[device] = DEFAULT_ANALOG_SENS;
            tapSensitivity[device] = 0;
            doubleTap[device] = false;
            scrollSensitivity[device] = 0;
            touchpadInvert[device] = 0;
            btPollRate[device] = DEFAULT_DS4_BT_POLL_RATE;

            lsOutputSettings[device].ResetSettings();
            rsOutputSettings[device].ResetSettings();
            l2OutputSettings[device].ResetSettings();
            r2OutputSettings[device].ResetSettings();

            LightbarSettingInfo lightbarSettings = lightbarSettingInfo[device];
            LightbarDS4WinInfo lightInfo = lightbarSettings.ds4winSettings;
            lightbarSettings.Mode = LightbarMode.DS4Win;
            lightInfo.m_LowLed = new DS4Color(Color.Black);
            //m_LowLeds[device] = new DS4Color(Color.Black);

            Color tempColor = Color.Blue;
            switch (device)
            {
                case 0: tempColor = Color.Blue; break;
                case 1: tempColor = Color.Red; break;
                case 2: tempColor = Color.Green; break;
                case 3: tempColor = Color.Pink; break;
                case 4: tempColor = Color.Blue; break;
                case 5: tempColor = Color.Red; break;
                case 6: tempColor = Color.Green; break;
                case 7: tempColor = Color.Pink; break;
                case 8: tempColor = Color.White; break;
                default: tempColor = Color.Blue; break;
            }

            lightInfo.m_Led = new DS4Color(tempColor);
            lightInfo.m_ChargingLed = new DS4Color(Color.Black);
            lightInfo.m_FlashLed = new DS4Color(Color.Black);
            lightInfo.flashAt = 0;
            lightInfo.flashType = 0;
            lightInfo.chargingType = 0;
            lightInfo.rainbow = 0;
            lightInfo.maxRainbowSat = LightbarDS4WinInfo.DEFAULT_MAX_RAINBOW_SAT;
            lightInfo.ledAsBattery = false;

            launchProgram[device] = string.Empty;
            dinputOnly[device] = DEFAULT_DINPUT_ONLY;
            startTouchpadOff[device] = false;
            touchOutMode[device] = DEFAULT_TOUCH_OUT_MODE;
            sATriggers[device] = BackingStore.DEFAULT_SA_TRIGGERS;
            sATriggerCond[device] = DEFAULT_SA_TRIGGER_COND;
            gyroOutMode[device] = DEFAULT_GYRO_OUT_MODE;
            sAMouseStickTriggers[device] = BackingStore.DEFAULT_GYRO_MSTICK_TRIGGERS;
            sAMouseStickTriggerCond[device] = true;

            gyroMStickInfo[device].Reset();
            gyroSwipeInfo[device].Reset();

            gyroMouseStickToggle[device] = false;
            gyroMouseStickTriggerTurns[device] = DEFAULT_GYRO_MSTICK_TRIGGER_TURNS;
            sASteeringWheelEmulationAxis[device] = SASteeringWheelEmulationAxisType.None;
            sASteeringWheelEmulationRange[device] = DEFAULT_SA_WHEEL_EMULATION_RANGE;
            saWheelFuzzValues[device] = 0;
            wheelSmoothInfo[device].Reset();
            touchDisInvertTriggers[device] = new int[1] { DEFAULT_TOUCH_DIS_INVERT_TRIGGER };
            gyroSensitivity[device] = DEFAULT_GYRO_SENS;
            gyroSensVerticalScale[device] = DEFAULT_GYRO_SENS_VERTICAL_SCALE;
            gyroInvert[device] = 0;
            gyroTriggerTurns[device] = DEFAULT_GYRO_TRIGGER_TURNS;
            gyroMouseInfo[device].Reset();

            gyroMouseHorizontalAxis[device] = 0;
            gyroMouseToggle[device] = false;
            //squStickInfo[device].lsMode = false;
            //squStickInfo[device].rsMode = false;
            //squStickInfo[device].lsRoundness = SquareStickInfo.DEFAULT_ROUNDNESS;
            //squStickInfo[device].rsRoundness = SquareStickInfo.DEFAULT_ROUNDNESS;
            squStickInfo[device].Reset();
            lsAntiSnapbackInfo[device].timeout = StickAntiSnapbackInfo.DEFAULT_TIMEOUT;
            lsAntiSnapbackInfo[device].delta = StickAntiSnapbackInfo.DEFAULT_DELTA;
            lsAntiSnapbackInfo[device].enabled = StickAntiSnapbackInfo.DEFAULT_ENABLED;
            setLsOutCurveMode(device, 0);
            setRsOutCurveMode(device, 0);
            setL2OutCurveMode(device, 0);
            setR2OutCurveMode(device, 0);
            setSXOutCurveMode(device, 0);
            setSZOutCurveMode(device, 0);
            trackballMode[device] = DEFAULT_TRACKBALL_MODE;
            trackballFriction[device] = DEFAULT_TRACKBALL_FRICTION;
            touchpadRelMouse[device].Reset();
            touchMStickInfo[device].Reset();
            touchpadButtonMode[device] = TouchButtonActivationMode.Click;
            outputDevType[device] = DEFAULT_OUT_CONT_TYPE;
            ds4Mapping = false;
        }


        // Phase7-Step10c W4: プロファイルの読込を担う LegacyProfileXmlSerializer が呼ぶため公開にした（プリセットの読込［Phase7-Step10c W6 まで BackingStore］も使う）
        public void CheckOldDevicestatus(int device, ControlService control,
            OutContType oldContType, out bool xinputPlug, out bool xinputStatus)
        {
            xinputPlug = false;
            xinputStatus = false;

            if (device < ControlService.CURRENT_DS4_CONTROLLER_LIMIT)
            {
                bool oldUseDInputOnly = Global.useDInputOnly[device];
                DS4Device tempDevice = control.DS4Controllers[device];
                bool exists = tempBool = (tempDevice != null);
                bool synced = tempBool = exists ? tempDevice.isSynced() : false;
                bool isAlive = tempBool = exists ? tempDevice.IsAlive() : false;
                if (dinputOnly[device] != oldUseDInputOnly)
                {
                    if (dinputOnly[device] == true)
                    {
                        xinputPlug = false;
                        xinputStatus = true;
                    }
                    else if (synced && isAlive)
                    {
                        xinputPlug = true;
                        xinputStatus = true;
                    }
                }
                else if (!dinputOnly[device] &&
                    oldContType != outputDevType[device])
                {
                    xinputPlug = true;
                    xinputStatus = true;
                }
            }
        }

        // Phase7-Step10c W4: プロファイルの読込を担う LegacyProfileXmlSerializer が呼ぶため公開にした（プリセットの読込［Phase7-Step10c W6 まで BackingStore］も使う）
        public void PostLoadSnippet(int device, ControlService control, bool xinputStatus, bool xinputPlug)
        {
            DS4Device tempDev = control.DS4Controllers[device];
            if (tempDev != null && tempDev.isSynced())
            {
                tempDev.queueEvent(() =>
                {
                    //tempDev.setIdleTimeout(idleDisconnectTimeout[device]);
                    //tempDev.setBTPollRate(btPollRate[device]);
                    if (xinputStatus && tempDev.PrimaryDevice)
                    {
                        if (xinputPlug)
                        {
                            OutputDevice tempOutDev = control.outputDevices[device];
                            if (tempOutDev != null)
                            {
                                tempOutDev = null;
                                //Global.activeOutDevType[device] = OutContType.None;
                                control.UnplugOutDev(device, tempDev);
                            }

                            OutContType tempContType = outputDevType[device];
                            control.PluginOutDev(device, tempDev);
                            //Global.useDInputOnly[device] = false;
                        }
                        else
                        {
                            //Global.activeOutDevType[device] = OutContType.None;
                            control.UnplugOutDev(device, tempDev);
                        }
                    }

                    //tempDev.RumbleAutostopTime = rumbleAutostopTime[device];
                    //tempDev.setRumble(0, 0);
                    //tempDev.LightBarColor = Global.getMainColor(device);
                    control.CheckProfileOptions(device, tempDev, true);
                });

                //Program.rootHub.touchPad[device]?.ResetTrackAccel(trackballFriction[device]);
            }
        }
    }
}

