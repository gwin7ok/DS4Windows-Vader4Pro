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

namespace DS4Windows.Services
{
    /// <summary>
    /// Phase7-Step10c W6（Q1・Q6＝A）: プロファイルのプリセット（空のプロファイルと、既定の割り当て 8 種）の読込。
    /// 旧: <c>BackingStore</c> の <c>LoadBlankDS4Profile</c>／<c>LoadBlankProfile</c>／<c>LoadDefault*Profile</c>（8 種）／<c>PrepareBlankingProfile</c>（移動のみ。
    /// ロジック・値・ログは変えない）。<c>BackingStore</c> の公開状態を <c>store.</c> で書き換える。呼び出し元は <see cref="ProfilePresetLoader"/> だけ。
    /// </summary>
    public class ProfilePresets
    {
        private readonly BackingStore store;

        public ProfilePresets(BackingStore store)
        {
            this.store = store ?? throw new ArgumentNullException(nameof(store));
        }

        private void PrepareBlankingProfile(int device, ControlService control, out bool xinputPlug, out bool xinputStatus, bool xinputChange = true)
        {
            xinputPlug = false;
            xinputStatus = false;

            OutContType oldContType = store.ActiveOutDevType[device];

            // Make sure to reset currently set profile values before parsing
            store.ResetProfile(device);
            store.ResetMouseProperties(device, control);

            // Only change xinput devices under certain conditions. Avoid
            // performing this upon program startup before loading devices.
            if (xinputChange)
            {
                store.CheckOldDevicestatus(device, control, oldContType,
                    out xinputPlug, out xinputStatus);
            }

            foreach (DS4ControlSettings dcs in store.ds4settings[device])
                dcs.Reset();

            store.profileActions[device].Clear();
            store.containsCustomAction[device] = false;
            store.containsCustomExtras[device] = false;
        }

        public void LoadBlankDS4Profile(int device, bool launchprogram, ControlService control,
            string propath = "", bool xinputChange = true, bool postLoad = true)
        {
            PrepareBlankingProfile(device, control, out bool xinputPlug, out bool xinputStatus, xinputChange);

            StickDeadZoneInfo lsInfo = store.lsModInfo[device];
            lsInfo.deadZone = (int)(0.00 * 127);
            lsInfo.antiDeadZone = 0;
            lsInfo.maxZone = 100;

            StickDeadZoneInfo rsInfo = store.rsModInfo[device];
            rsInfo.deadZone = (int)(0.00 * 127);
            rsInfo.antiDeadZone = 0;
            rsInfo.maxZone = 100;

            TriggerDeadZoneZInfo l2Info = store.l2ModInfo[device];
            l2Info.deadZone = (byte)(0.00 * 255);

            TriggerDeadZoneZInfo r2Info = store.r2ModInfo[device];
            r2Info.deadZone = (byte)(0.00 * 255);

            store.outputDevType[device] = OutContType.DS4;

            // If a device exists, make sure to transfer relevant profile device
            // options to device instance
            if (postLoad && device < ControllerLimits.MaxControllerCount)
            {
                store.PostLoadSnippet(device, control, xinputStatus, xinputPlug);
            }
        }

        public void LoadBlankProfile(int device, bool launchprogram, ControlService control,
            string propath = "", bool xinputChange = true, bool postLoad = true)
        {
            bool xinputPlug = false;
            bool xinputStatus = false;

            OutContType oldContType = store.ActiveOutDevType[device];

            // Make sure to reset currently set profile values before parsing
            store.ResetProfile(device);
            store.ResetMouseProperties(device, control);

            // Only change xinput devices under certain conditions. Avoid
            // performing this upon program startup before loading devices.
            if (xinputChange)
            {
                store.CheckOldDevicestatus(device, control, oldContType,
                    out xinputPlug, out xinputStatus);
            }

            foreach (DS4ControlSettings dcs in store.ds4settings[device])
                dcs.Reset();

            store.profileActions[device].Clear();
            store.containsCustomAction[device] = false;
            store.containsCustomExtras[device] = false;

            // If a device exists, make sure to transfer relevant profile device
            // options to device instance
            if (postLoad && device < ControllerLimits.MaxControllerCount)
            {
                store.PostLoadSnippet(device, control, xinputStatus, xinputPlug);
            }
        }

        public void LoadDefaultGamepadGyroProfile(int device, bool launchprogram, ControlService control,
            string propath = "", bool xinputChange = true, bool postLoad = true)
        {
            bool xinputPlug = false;
            bool xinputStatus = false;

            OutContType oldContType = store.ActiveOutDevType[device];

            // Make sure to reset currently set profile values before parsing
            store.ResetProfile(device);
            store.ResetMouseProperties(device, control);

            // Only change xinput devices under certain conditions. Avoid
            // performing this upon program startup before loading devices.
            if (xinputChange)
            {
                store.CheckOldDevicestatus(device, control, oldContType,
                    out xinputPlug, out xinputStatus);
            }

            foreach (DS4ControlSettings dcs in store.ds4settings[device])
                dcs.Reset();

            store.profileActions[device].Clear();
            store.containsCustomAction[device] = false;
            store.containsCustomExtras[device] = false;

            store.gyroOutMode[device] = GyroOutMode.MouseJoystick;
            store.sAMouseStickTriggers[device] = "4";
            store.sAMouseStickTriggerCond[device] = true;
            store.gyroMouseStickTriggerTurns[device] = false;
            store.gyroMStickInfo[device].useSmoothing = true;
            store.gyroMStickInfo[device].smoothingMethod = GyroMouseStickInfo.SmoothingMethod.OneEuro;

            // If a device exists, make sure to transfer relevant profile device
            // options to device instance
            if (postLoad && device < ControllerLimits.MaxControllerCount)
            {
                store.PostLoadSnippet(device, control, xinputStatus, xinputPlug);
            }
        }

        public void LoadDefaultDS4GamepadGyroProfile(int device, bool launchprogram, ControlService control,
            string propath = "", bool xinputChange = true, bool postLoad = true)
        {
            PrepareBlankingProfile(device, control, out bool xinputPlug, out bool xinputStatus, xinputChange);

            StickDeadZoneInfo lsInfo = store.lsModInfo[device];
            lsInfo.deadZone = (int)(0.00 * 127);
            lsInfo.antiDeadZone = 0;
            lsInfo.maxZone = 100;

            StickDeadZoneInfo rsInfo = store.rsModInfo[device];
            rsInfo.deadZone = (int)(0.00 * 127);
            rsInfo.antiDeadZone = 0;
            rsInfo.maxZone = 100;

            TriggerDeadZoneZInfo l2Info = store.l2ModInfo[device];
            l2Info.deadZone = (byte)(0.00 * 255);

            TriggerDeadZoneZInfo r2Info = store.r2ModInfo[device];
            r2Info.deadZone = (byte)(0.00 * 255);

            store.gyroOutMode[device] = GyroOutMode.MouseJoystick;
            store.sAMouseStickTriggers[device] = "4";
            store.sAMouseStickTriggerCond[device] = true;
            store.gyroMouseStickTriggerTurns[device] = false;
            store.gyroMStickInfo[device].useSmoothing = true;
            store.gyroMStickInfo[device].smoothingMethod = GyroMouseStickInfo.SmoothingMethod.OneEuro;

            store.outputDevType[device] = OutContType.DS4;

            // If a device exists, make sure to transfer relevant profile device
            // options to device instance
            if (postLoad && device < ControllerLimits.MaxControllerCount)
            {
                store.PostLoadSnippet(device, control, xinputStatus, xinputPlug);
            }
        }

        public void LoadDefaultMixedGyroMouseProfile(int device, bool launchprogram, ControlService control,
            string propath = "", bool xinputChange = true, bool postLoad = true)
        {
            bool xinputPlug = false;
            bool xinputStatus = false;

            OutContType oldContType = store.ActiveOutDevType[device];

            // Make sure to reset currently set profile values before parsing
            store.ResetProfile(device);
            store.ResetMouseProperties(device, control);

            // Only change xinput devices under certain conditions. Avoid
            // performing this upon program startup before loading devices.
            if (xinputChange)
            {
                store.CheckOldDevicestatus(device, control, oldContType,
                    out xinputPlug, out xinputStatus);
            }

            foreach (DS4ControlSettings dcs in store.ds4settings[device])
                dcs.Reset();

            store.profileActions[device].Clear();
            store.containsCustomAction[device] = false;
            store.containsCustomExtras[device] = false;

            store.gyroOutMode[device] = GyroOutMode.Mouse;
            store.sATriggers[device] = "4";
            store.sATriggerCond[device] = true;
            store.gyroTriggerTurns[device] = false;
            store.gyroMouseInfo[device].enableSmoothing = true;
            store.gyroMouseInfo[device].smoothingMethod = GyroMouseInfo.SmoothingMethod.OneEuro;

            StickDeadZoneInfo rsInfo = store.rsModInfo[device];
            rsInfo.deadZone = (int)(0.10 * 127);
            rsInfo.antiDeadZone = 0;
            rsInfo.maxZone = 90;

            // If a device exists, make sure to transfer relevant profile device
            // options to device instance
            if (postLoad && device < ControllerLimits.MaxControllerCount)
            {
                store.PostLoadSnippet(device, control, xinputStatus, xinputPlug);
            }
        }

        public void LoadDefaultDS4MixedGyroMouseProfile(int device, bool launchprogram, ControlService control,
            string propath = "", bool xinputChange = true, bool postLoad = true)
        {
            PrepareBlankingProfile(device, control, out bool xinputPlug, out bool xinputStatus, xinputChange);

            StickDeadZoneInfo lsInfo = store.lsModInfo[device];
            lsInfo.deadZone = (int)(0.00 * 127);
            lsInfo.antiDeadZone = 0;
            lsInfo.maxZone = 100;

            StickDeadZoneInfo rsInfo = store.rsModInfo[device];
            rsInfo.deadZone = (int)(0.10 * 127);
            rsInfo.antiDeadZone = 0;
            rsInfo.maxZone = 100;

            TriggerDeadZoneZInfo l2Info = store.l2ModInfo[device];
            l2Info.deadZone = (byte)(0.00 * 255);

            TriggerDeadZoneZInfo r2Info = store.r2ModInfo[device];
            r2Info.deadZone = (byte)(0.00 * 255);

            store.gyroOutMode[device] = GyroOutMode.Mouse;
            store.sATriggers[device] = "4";
            store.sATriggerCond[device] = true;
            store.gyroTriggerTurns[device] = false;
            store.gyroMouseInfo[device].enableSmoothing = true;
            store.gyroMouseInfo[device].smoothingMethod = GyroMouseInfo.SmoothingMethod.OneEuro;

            store.outputDevType[device] = OutContType.DS4;

            // If a device exists, make sure to transfer relevant profile device
            // options to device instance
            if (postLoad && device < ControllerLimits.MaxControllerCount)
            {
                store.PostLoadSnippet(device, control, xinputStatus, xinputPlug);
            }
        }

        public void LoadDefaultDS4MixedControlsProfile(int device, bool launchprogram, ControlService control,
            string propath = "", bool xinputChange = true, bool postLoad = true)
        {
            PrepareBlankingProfile(device, control, out bool xinputPlug, out bool xinputStatus, xinputChange);

            DS4ControlSettings setting = store.GetDS4CSetting(device, DS4Controls.RYNeg);
            setting.UpdateSettings(false, X360Controls.MouseUp, "", DS4KeyType.None);
            setting = store.GetDS4CSetting(device, DS4Controls.RYPos);
            setting.UpdateSettings(false, X360Controls.MouseDown, "", DS4KeyType.None);
            setting = store.GetDS4CSetting(device, DS4Controls.RXNeg);
            setting.UpdateSettings(false, X360Controls.MouseLeft, "", DS4KeyType.None);
            setting = store.GetDS4CSetting(device, DS4Controls.RXPos);
            setting.UpdateSettings(false, X360Controls.MouseRight, "", DS4KeyType.None);

            StickDeadZoneInfo rsInfo = store.rsModInfo[device];
            rsInfo.deadZone = (int)(0.035 * 127);
            rsInfo.antiDeadZone = 0;
            rsInfo.maxZone = 90;

            store.outputDevType[device] = OutContType.DS4;

            // If a device exists, make sure to transfer relevant profile device
            // options to device instance
            if (postLoad && device < ControllerLimits.MaxControllerCount)
            {
                store.PostLoadSnippet(device, control, xinputStatus, xinputPlug);
            }
        }

        public void LoadDefaultMixedControlsProfile(int device, bool launchprogram, ControlService control,
            string propath = "", bool xinputChange = true, bool postLoad = true)
        {
            bool xinputPlug = false;
            bool xinputStatus = false;

            OutContType oldContType = store.ActiveOutDevType[device];

            // Make sure to reset currently set profile values before parsing
            store.ResetProfile(device);
            store.ResetMouseProperties(device, control);

            // Only change xinput devices under certain conditions. Avoid
            // performing this upon program startup before loading devices.
            if (xinputChange)
            {
                store.CheckOldDevicestatus(device, control, oldContType,
                    out xinputPlug, out xinputStatus);
            }

            foreach (DS4ControlSettings dcs in store.ds4settings[device])
                dcs.Reset();

            store.profileActions[device].Clear();
            store.containsCustomAction[device] = false;
            store.containsCustomExtras[device] = false;

            DS4ControlSettings setting = store.GetDS4CSetting(device, DS4Controls.RYNeg);
            setting.UpdateSettings(false, X360Controls.MouseUp, "", DS4KeyType.None);
            setting = store.GetDS4CSetting(device, DS4Controls.RYPos);
            setting.UpdateSettings(false, X360Controls.MouseDown, "", DS4KeyType.None);
            setting = store.GetDS4CSetting(device, DS4Controls.RXNeg);
            setting.UpdateSettings(false, X360Controls.MouseLeft, "", DS4KeyType.None);
            setting = store.GetDS4CSetting(device, DS4Controls.RXPos);
            setting.UpdateSettings(false, X360Controls.MouseRight, "", DS4KeyType.None);

            StickDeadZoneInfo rsInfo = store.rsModInfo[device];
            rsInfo.deadZone = (int)(0.035 * 127);
            rsInfo.antiDeadZone = 0;
            rsInfo.maxZone = 90;

            // If a device exists, make sure to transfer relevant profile device
            // options to device instance
            if (postLoad && device < ControllerLimits.MaxControllerCount)
            {
                store.PostLoadSnippet(device, control, xinputStatus, xinputPlug);
            }
        }

        public void LoadDefaultKBMProfile(int device, bool launchprogram, ControlService control,
            string propath = "", bool xinputChange = true, bool postLoad = true)
        {
            bool xinputPlug = false;
            bool xinputStatus = false;

            OutContType oldContType = store.ActiveOutDevType[device];

            // Make sure to reset currently set profile values before parsing
            store.ResetProfile(device);
            store.ResetMouseProperties(device, control);

            // Only change xinput devices under certain conditions. Avoid
            // performing this upon program startup before loading devices.
            if (xinputChange)
            {
                store.CheckOldDevicestatus(device, control, oldContType,
                    out xinputPlug, out xinputStatus);
            }

            foreach (DS4ControlSettings dcs in store.ds4settings[device])
                dcs.Reset();

            store.profileActions[device].Clear();
            store.containsCustomAction[device] = false;
            store.containsCustomExtras[device] = false;

            StickDeadZoneInfo lsInfo = store.lsModInfo[device];
            lsInfo.antiDeadZone = 0;

            StickDeadZoneInfo rsInfo = store.rsModInfo[device];
            rsInfo.deadZone = (int)(0.035 * 127);
            rsInfo.antiDeadZone = 0;
            rsInfo.maxZone = 90;

            TriggerDeadZoneZInfo l2Info = store.l2ModInfo[device];
            l2Info.deadZone = (byte)(0.20 * 255);

            TriggerDeadZoneZInfo r2Info = store.r2ModInfo[device];
            r2Info.deadZone = (byte)(0.20 * 255);

            // Flag to unplug virtual controller
            store.dinputOnly[device] = true;

            DS4ControlSettings setting = store.GetDS4CSetting(device, DS4Controls.LYNeg);
            setting.UpdateSettings(false, KeyInterop.VirtualKeyFromKey(Key.W), "", DS4KeyType.None);
            setting = store.GetDS4CSetting(device, DS4Controls.LXNeg);
            setting.UpdateSettings(false, KeyInterop.VirtualKeyFromKey(Key.A), "", DS4KeyType.None);
            setting = store.GetDS4CSetting(device, DS4Controls.LYPos);
            setting.UpdateSettings(false, KeyInterop.VirtualKeyFromKey(Key.S), "", DS4KeyType.None);
            setting = store.GetDS4CSetting(device, DS4Controls.LXPos);
            setting.UpdateSettings(false, KeyInterop.VirtualKeyFromKey(Key.D), "", DS4KeyType.None);
            setting = store.GetDS4CSetting(device, DS4Controls.L3);
            setting.UpdateSettings(false, KeyInterop.VirtualKeyFromKey(Key.LeftShift), "", DS4KeyType.None);

            setting = store.GetDS4CSetting(device, DS4Controls.RYNeg);
            setting.UpdateSettings(false, X360Controls.MouseUp, "", DS4KeyType.None);
            setting = store.GetDS4CSetting(device, DS4Controls.RYPos);
            setting.UpdateSettings(false, X360Controls.MouseDown, "", DS4KeyType.None);
            setting = store.GetDS4CSetting(device, DS4Controls.RXNeg);
            setting.UpdateSettings(false, X360Controls.MouseLeft, "", DS4KeyType.None);
            setting = store.GetDS4CSetting(device, DS4Controls.RXPos);
            setting.UpdateSettings(false, X360Controls.MouseRight, "", DS4KeyType.None);
            setting = store.GetDS4CSetting(device, DS4Controls.R3);
            setting.UpdateSettings(false, KeyInterop.VirtualKeyFromKey(Key.LeftCtrl), "", DS4KeyType.None);

            setting = store.GetDS4CSetting(device, DS4Controls.DpadUp);
            setting.UpdateSettings(false, X360Controls.Unbound, "", DS4KeyType.None);
            setting = store.GetDS4CSetting(device, DS4Controls.DpadRight);
            setting.UpdateSettings(false, X360Controls.WDOWN, "", DS4KeyType.None);
            setting = store.GetDS4CSetting(device, DS4Controls.DpadDown);
            setting.UpdateSettings(false, X360Controls.Unbound, "", DS4KeyType.None);
            setting = store.GetDS4CSetting(device, DS4Controls.DpadLeft);
            setting.UpdateSettings(false, X360Controls.WUP, "", DS4KeyType.None);

            setting = store.GetDS4CSetting(device, DS4Controls.Cross);
            setting.UpdateSettings(false, KeyInterop.VirtualKeyFromKey(Key.Space), "", DS4KeyType.None);
            setting = store.GetDS4CSetting(device, DS4Controls.Square);
            setting.UpdateSettings(false, KeyInterop.VirtualKeyFromKey(Key.F), "", DS4KeyType.None);
            setting = store.GetDS4CSetting(device, DS4Controls.Triangle);
            setting.UpdateSettings(false, KeyInterop.VirtualKeyFromKey(Key.E), "", DS4KeyType.None);
            setting = store.GetDS4CSetting(device, DS4Controls.Circle);
            setting.UpdateSettings(false, KeyInterop.VirtualKeyFromKey(Key.C), "", DS4KeyType.None);

            setting = store.GetDS4CSetting(device, DS4Controls.L1);
            setting.UpdateSettings(false, KeyInterop.VirtualKeyFromKey(Key.Q), "", DS4KeyType.None);
            setting = store.GetDS4CSetting(device, DS4Controls.L2);
            setting.UpdateSettings(false, X360Controls.RightMouse, "", DS4KeyType.None);
            setting = store.GetDS4CSetting(device, DS4Controls.R1);
            setting.UpdateSettings(false, KeyInterop.VirtualKeyFromKey(Key.R), "", DS4KeyType.None);
            setting = store.GetDS4CSetting(device, DS4Controls.R2);
            setting.UpdateSettings(false, X360Controls.LeftMouse, "", DS4KeyType.None);

            setting = store.GetDS4CSetting(device, DS4Controls.Share);
            setting.UpdateSettings(false, KeyInterop.VirtualKeyFromKey(Key.Tab), "", DS4KeyType.None);
            setting = store.GetDS4CSetting(device, DS4Controls.Options);
            setting.UpdateSettings(false, KeyInterop.VirtualKeyFromKey(Key.Escape), "", DS4KeyType.None);

            // If a device exists, make sure to transfer relevant profile device
            // options to device instance
            if (postLoad && device < ControllerLimits.MaxControllerCount)
            {
                store.PostLoadSnippet(device, control, xinputStatus, xinputPlug);
            }
        }

        public void LoadDefaultKBMGyroMouseProfile(int device, bool launchprogram, ControlService control,
            string propath = "", bool xinputChange = true, bool postLoad = true)
        {
            bool xinputPlug = false;
            bool xinputStatus = false;

            OutContType oldContType = store.ActiveOutDevType[device];

            // Make sure to reset currently set profile values before parsing
            store.ResetProfile(device);
            store.ResetMouseProperties(device, control);

            // Only change xinput devices under certain conditions. Avoid
            // performing this upon program startup before loading devices.
            if (xinputChange)
            {
                store.CheckOldDevicestatus(device, control, oldContType,
                    out xinputPlug, out xinputStatus);
            }

            foreach (DS4ControlSettings dcs in store.ds4settings[device])
                dcs.Reset();

            store.profileActions[device].Clear();
            store.containsCustomAction[device] = false;
            store.containsCustomExtras[device] = false;

            StickDeadZoneInfo lsInfo = store.lsModInfo[device];
            lsInfo.antiDeadZone = 0;

            StickDeadZoneInfo rsInfo = store.rsModInfo[device];
            rsInfo.deadZone = (int)(0.105 * 127);
            rsInfo.antiDeadZone = 0;
            rsInfo.maxZone = 90;

            TriggerDeadZoneZInfo l2Info = store.l2ModInfo[device];
            l2Info.deadZone = (byte)(0.20 * 255);

            TriggerDeadZoneZInfo r2Info = store.r2ModInfo[device];
            r2Info.deadZone = (byte)(0.20 * 255);

            store.gyroOutMode[device] = GyroOutMode.Mouse;
            store.sATriggers[device] = "4";
            store.sATriggerCond[device] = true;
            store.gyroTriggerTurns[device] = false;
            store.gyroMouseInfo[device].enableSmoothing = true;
            store.gyroMouseInfo[device].smoothingMethod = GyroMouseInfo.SmoothingMethod.OneEuro;

            // Flag to unplug virtual controller
            store.dinputOnly[device] = true;

            DS4ControlSettings setting = store.GetDS4CSetting(device, DS4Controls.LYNeg);
            setting.UpdateSettings(false, KeyInterop.VirtualKeyFromKey(Key.W), "", DS4KeyType.None);
            setting = store.GetDS4CSetting(device, DS4Controls.LXNeg);
            setting.UpdateSettings(false, KeyInterop.VirtualKeyFromKey(Key.A), "", DS4KeyType.None);
            setting = store.GetDS4CSetting(device, DS4Controls.LYPos);
            setting.UpdateSettings(false, KeyInterop.VirtualKeyFromKey(Key.S), "", DS4KeyType.None);
            setting = store.GetDS4CSetting(device, DS4Controls.LXPos);
            setting.UpdateSettings(false, KeyInterop.VirtualKeyFromKey(Key.D), "", DS4KeyType.None);
            setting = store.GetDS4CSetting(device, DS4Controls.L3);
            setting.UpdateSettings(false, KeyInterop.VirtualKeyFromKey(Key.LeftShift), "", DS4KeyType.None);

            setting = store.GetDS4CSetting(device, DS4Controls.RYNeg);
            setting.UpdateSettings(false, X360Controls.MouseUp, "", DS4KeyType.None);
            setting = store.GetDS4CSetting(device, DS4Controls.RYPos);
            setting.UpdateSettings(false, X360Controls.MouseDown, "", DS4KeyType.None);
            setting = store.GetDS4CSetting(device, DS4Controls.RXNeg);
            setting.UpdateSettings(false, X360Controls.MouseLeft, "", DS4KeyType.None);
            setting = store.GetDS4CSetting(device, DS4Controls.RXPos);
            setting.UpdateSettings(false, X360Controls.MouseRight, "", DS4KeyType.None);
            setting = store.GetDS4CSetting(device, DS4Controls.R3);
            setting.UpdateSettings(false, KeyInterop.VirtualKeyFromKey(Key.LeftCtrl), "", DS4KeyType.None);

            setting = store.GetDS4CSetting(device, DS4Controls.DpadUp);
            setting.UpdateSettings(false, X360Controls.Unbound, "", DS4KeyType.None);
            setting = store.GetDS4CSetting(device, DS4Controls.DpadRight);
            setting.UpdateSettings(false, X360Controls.WDOWN, "", DS4KeyType.None);
            setting = store.GetDS4CSetting(device, DS4Controls.DpadDown);
            setting.UpdateSettings(false, X360Controls.Unbound, "", DS4KeyType.None);
            setting = store.GetDS4CSetting(device, DS4Controls.DpadLeft);
            setting.UpdateSettings(false, X360Controls.WUP, "", DS4KeyType.None);

            setting = store.GetDS4CSetting(device, DS4Controls.Cross);
            setting.UpdateSettings(false, KeyInterop.VirtualKeyFromKey(Key.Space), "", DS4KeyType.None);
            setting = store.GetDS4CSetting(device, DS4Controls.Square);
            setting.UpdateSettings(false, KeyInterop.VirtualKeyFromKey(Key.F), "", DS4KeyType.None);
            setting = store.GetDS4CSetting(device, DS4Controls.Triangle);
            setting.UpdateSettings(false, KeyInterop.VirtualKeyFromKey(Key.E), "", DS4KeyType.None);
            setting = store.GetDS4CSetting(device, DS4Controls.Circle);
            setting.UpdateSettings(false, KeyInterop.VirtualKeyFromKey(Key.C), "", DS4KeyType.None);

            setting = store.GetDS4CSetting(device, DS4Controls.L1);
            //setting.UpdateSettings(false, KeyInterop.VirtualKeyFromKey(Key.Q), "", DS4KeyType.None);
            setting.UpdateSettings(false, X360Controls.Unbound, "", DS4KeyType.None);
            setting = store.GetDS4CSetting(device, DS4Controls.L2);
            setting.UpdateSettings(false, X360Controls.RightMouse, "", DS4KeyType.None);
            setting = store.GetDS4CSetting(device, DS4Controls.R1);
            setting.UpdateSettings(false, KeyInterop.VirtualKeyFromKey(Key.R), "", DS4KeyType.None);
            setting = store.GetDS4CSetting(device, DS4Controls.R2);
            setting.UpdateSettings(false, X360Controls.LeftMouse, "", DS4KeyType.None);

            setting = store.GetDS4CSetting(device, DS4Controls.Share);
            setting.UpdateSettings(false, KeyInterop.VirtualKeyFromKey(Key.Tab), "", DS4KeyType.None);
            setting = store.GetDS4CSetting(device, DS4Controls.Options);
            setting.UpdateSettings(false, KeyInterop.VirtualKeyFromKey(Key.Escape), "", DS4KeyType.None);

            // If a device exists, make sure to transfer relevant profile device
            // options to device instance
            if (postLoad && device < ControllerLimits.MaxControllerCount)
            {
                store.PostLoadSnippet(device, control, xinputStatus, xinputPlug);
            }
        }
    }
}
