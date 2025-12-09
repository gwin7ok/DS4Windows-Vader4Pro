using System;
using System.Xml.Serialization;
using System.Globalization;
using DS4Windows;
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

public class AppSettingsDTO
{
    // ここに既存の全てのプロパティ・メソッド・フィールド・コンストラクタを収める

        [XmlElement("formWidth")]
    public int FormWidth { get; set; } = BackingStore.DEFAULT_FORM_WIDTH;

        [XmlElement("formHeight")]
        public int FormHeight { get; set; } = BackingStore.DEFAULT_FORM_HEIGHT;

        private int _formLocationX;
        [XmlElement("formLocationX")]
        public int FormLocationX
        {
            get => _formLocationX;
            set => _formLocationX = value;
        }

        private int _formLocationY;
        [XmlElement("formLocationY")]
        public int FormLocationY
        {
            get => _formLocationY;
            set => _formLocationY = value;
        }

    // ウィンドウ状態管理プロパティ
    public bool UseExclusiveMode { get; set; }
    public bool StartMinimized { get; set; }
    public bool MinimizeToTaskbar { get; set; }

    // プロフィール編集画面 Splitter位置・列幅
    [XmlElement("profileEditorLeftWidth")]
    public int ProfileEditorLeftWidth { get; set; } = BackingStore.DEFAULT_PROFILE_EDITOR_LEFT_WIDTH;

    [XmlElement("profileEditorRightWidth")]
    public int ProfileEditorRightWidth { get; set; } = BackingStore.DEFAULT_PROFILE_EDITOR_RIGHT_WIDTH;

    [XmlElement("specialActionNameColWidth")]
    public int SpecialActionNameColWidth { get; set; } = BackingStore.DEFAULT_SPECIAL_ACTION_NAME_COL_WIDTH;

    [XmlElement("specialActionTriggerColWidth")]
    public int SpecialActionTriggerColWidth { get; set; } = BackingStore.DEFAULT_SPECIAL_ACTION_TRIGGER_COL_WIDTH;

    [XmlElement("specialActionDetailColWidth")]
    public int SpecialActionDetailColWidth { get; set; } = BackingStore.DEFAULT_SPECIAL_ACTION_DETAIL_COL_WIDTH;

    // Controller tab column widths
    [XmlElement("controllerIndexColWidth")]
    public int ControllerIndexColWidth { get; set; } = BackingStore.DEFAULT_CONTROLLER_INDEX_COL_WIDTH;

    [XmlElement("controllerIdColWidth")]
    public int ControllerIdColWidth { get; set; } = BackingStore.DEFAULT_CONTROLLER_ID_COL_WIDTH;

    [XmlElement("controllerStatusColWidth")]
    public int ControllerStatusColWidth { get; set; } = BackingStore.DEFAULT_CONTROLLER_STATUS_COL_WIDTH;

    [XmlElement("controllerExclusiveColWidth")]
    public int ControllerExclusiveColWidth { get; set; } = BackingStore.DEFAULT_CONTROLLER_EXCLUSIVE_COL_WIDTH;

    [XmlElement("controllerBatteryColWidth")]
    public int ControllerBatteryColWidth { get; set; } = BackingStore.DEFAULT_CONTROLLER_BATTERY_COL_WIDTH;

    [XmlElement("controllerLinkProfColWidth")]
    public int ControllerLinkProfColWidth { get; set; } = BackingStore.DEFAULT_CONTROLLER_LINKPROF_COL_WIDTH;

    [XmlElement("controllerSelectProfileColWidth")]
    public int ControllerSelectProfileColWidth { get; set; } = BackingStore.DEFAULT_CONTROLLER_SELECTPROFILE_COL_WIDTH;

    [XmlElement("controllerEditColWidth")]
    public int ControllerEditColWidth { get; set; } = BackingStore.DEFAULT_CONTROLLER_EDIT_COL_WIDTH;

    [XmlElement("controllerCustomColorColWidth")]
    public int ControllerCustomColorColWidth { get; set; } = BackingStore.DEFAULT_CONTROLLER_CUSTOMCOLOR_COL_WIDTH;

    // ルート属性として保存するアプリ/設定バージョン
    [XmlAttribute("app_version")]
    public string AppVersion { get; set; }

    [XmlAttribute("config_version")]
    public string ConfigVersion { get; set; }

        [XmlElement("ProcessPriority")]
        public int ProcessPriority { get; set; }

        [XmlElement("Controller1")]
        public string Controller1CurrentProfile
        {
            get; set;
        }
        public bool ShouldSerializeController1CurrentProfile()
        {
            return !string.IsNullOrEmpty(Controller1CurrentProfile);
        }

        [XmlElement("Controller2")]
        public string Controller2CurrentProfile
        {
            get; set;
        }
        public bool ShouldSerializeController2CurrentProfile()
        {
            return !string.IsNullOrEmpty(Controller2CurrentProfile);
        }

        [XmlElement("Controller3")]
        public string Controller3CurrentProfile
        {
            get; set;
        }
        public bool ShouldSerializeController3CurrentProfile()
        {
            return !string.IsNullOrEmpty(Controller3CurrentProfile);
        }

        [XmlElement("Controller4")]
        public string Controller4CurrentProfile
        {
            get; set;
        }
        public bool ShouldSerializeController4CurrentProfile()
        {
            return !string.IsNullOrEmpty(Controller4CurrentProfile);
        }

        [XmlElement("Controller5")]
        public string Controller5CurrentProfile
        {
            get; set;
        }
        public bool ShouldSerializeController5CurrentProfile()
        {
            return !string.IsNullOrEmpty(Controller5CurrentProfile) &&
                Global.MAX_DS4_CONTROLLER_COUNT >= 5;
        }

        [XmlElement("Controller6")]
        public string Controller6CurrentProfile
        {
            get; set;
        }
        public bool ShouldSerializeController6CurrentProfile()
        {
            return !string.IsNullOrEmpty(Controller6CurrentProfile) &&
                Global.MAX_DS4_CONTROLLER_COUNT >= 6;
        }

        [XmlElement("Controller7")]
        public string Controller7CurrentProfile
        {
            get; set;
        }
        public bool ShouldSerializeController7CurrentProfile()
        {
            return !string.IsNullOrEmpty(Controller7CurrentProfile) &&
                Global.MAX_DS4_CONTROLLER_COUNT >= 7;
        }

        [XmlElement("Controller8")]
        public string Controller8CurrentProfile
        {
            get; set;
        }
        public bool ShouldSerializeController8CurrentProfile()
        {
            return !string.IsNullOrEmpty(Controller8CurrentProfile) &&
                Global.MAX_DS4_CONTROLLER_COUNT >= 8;
        }

        // Selected Profile settings
        [XmlElement("SelectedProfile1")]
        public string SelectedProfile1 { get; set; }
        public bool ShouldSerializeSelectedProfile1() => !string.IsNullOrEmpty(SelectedProfile1);

        [XmlElement("SelectedProfile2")]
        public string SelectedProfile2 { get; set; }
        public bool ShouldSerializeSelectedProfile2() => !string.IsNullOrEmpty(SelectedProfile2);

        [XmlElement("SelectedProfile3")]
        public string SelectedProfile3 { get; set; }
        public bool ShouldSerializeSelectedProfile3() => !string.IsNullOrEmpty(SelectedProfile3);

        [XmlElement("SelectedProfile4")]
        public string SelectedProfile4 { get; set; }
        public bool ShouldSerializeSelectedProfile4() => !string.IsNullOrEmpty(SelectedProfile4);

        [XmlElement("SelectedProfile5")]
        public string SelectedProfile5 { get; set; }
        public bool ShouldSerializeSelectedProfile5() => !string.IsNullOrEmpty(SelectedProfile5) && Global.MAX_DS4_CONTROLLER_COUNT >= 5;

        [XmlElement("SelectedProfile6")]
        public string SelectedProfile6 { get; set; }
        public bool ShouldSerializeSelectedProfile6() => !string.IsNullOrEmpty(SelectedProfile6) && Global.MAX_DS4_CONTROLLER_COUNT >= 6;

        [XmlElement("SelectedProfile7")]
        public string SelectedProfile7 { get; set; }
        public bool ShouldSerializeSelectedProfile7() => !string.IsNullOrEmpty(SelectedProfile7) && Global.MAX_DS4_CONTROLLER_COUNT >= 7;

        [XmlElement("SelectedProfile8")]
        public string SelectedProfile8 { get; set; }
        public bool ShouldSerializeSelectedProfile8() => !string.IsNullOrEmpty(SelectedProfile8) && Global.MAX_DS4_CONTROLLER_COUNT >= 8;


        [XmlIgnore]
        public DateTime LastChecked
        {
            get; private set;
        }

        [XmlElement("LastChecked")]
        public string LastCheckString
        {
            get => LastChecked.ToString("MM/dd/yyyy HH:mm:ss");
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    // leave LastChecked as default (DateTime.MinValue) if empty
                    return;
                }

                DateTime temp;
                // Try several common formats using invariant culture first to avoid culture-specific parsing issues.
                string[] formats = new[] {
                    "MM/dd/yyyy HH:mm:ss",
                    "M/d/yyyy H:mm:ss",
                    "yyyy-MM-ddTHH:mm:ss",
                    "yyyy-MM-dd HH:mm:ss",
                    "yyyy/MM/dd HH:mm:ss",
                    "yyyy-MM-ddTHH:mm:ssZ",
                    "o", // round-trip ISO 8601
                    "s"  // sortable
                };

                if (DateTime.TryParseExact(value, formats, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeLocal, out temp))
                {
                    LastChecked = temp;
                    return;
                }

                // Fallback to invariant culture parse, then to current culture.
                if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeLocal, out temp) ||
                    DateTime.TryParse(value, CultureInfo.CurrentCulture, DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeLocal, out temp))
                {
                    LastChecked = temp;
                }
                // If parsing fails, keep existing LastChecked (no throw/log here to keep DTO lightweight).
            }
        }

        public int CheckWhen
        {
            get; set;
        } = BackingStore.DEFAULT_CHECK_WHEN;

        public string LastVersionChecked
        {
            get; set;
        } = string.Empty;
        public bool ShouldSerializeLastVersionChecked()
        {
            return !string.IsNullOrEmpty(LastVersionChecked);
        }

        [XmlIgnore]
        public int Notifications
        {
            get; private set;
        } = BackingStore.DEFAULT_NOTIFICATIONS;

        [XmlElement("Notifications")]
        public string NotificationsString
        {
            get => Notifications.ToString();
            set
            {
                if (int.TryParse(value, out int tempNum))
                {
                    Notifications = tempNum;
                }
                else
                {
                    if (bool.TryParse(value, out bool temp))
                    {
                        Notifications = temp ? 2 : 0;
                    }
                }
            }
        }


        [XmlIgnore]
        public bool ProfileChangedNotification
        {
            get; private set;
        }

        [XmlElement("ProfileChangedNotification")]
        public string ProfileChangedNotificationString
        {
            get => ProfileChangedNotification.ToString();
            set
            {
                if (bool.TryParse(value, out var input))
                {
                    ProfileChangedNotification = input;
                }
            }
        }

        [XmlIgnore]
        public bool DisconnectBTAtStop
        {
            get; private set;
        }

        [XmlElement("DisconnectBTAtStop")]
        public string DisconnectBTAtStopString
        {
            get => DisconnectBTAtStop.ToString();
            set
            {
                if (bool.TryParse(value, out bool temp))
                {
                    DisconnectBTAtStop = temp;
                }
            }
        }

        [XmlIgnore]
        public bool SwipeProfiles
        {
            get; private set;
        } = BackingStore.DEFAULT_SWIPE_PROFILES;

        [XmlElement("SwipeProfiles")]
        public string SwipeProfilesString
        {
            get => SwipeProfiles.ToString();
            set
            {
                if (bool.TryParse(value, out bool temp))
                {
                    SwipeProfiles = temp;
                }
            }
        }

        [XmlIgnore]
        public bool QuickCharge
        {
            get; private set;
        }

        [XmlElement("QuickCharge")]
        public string QuickChargeString
        {
            get => QuickCharge.ToString();
            set
            {
                if (bool.TryParse(value, out bool temp))
                {
                    QuickCharge = temp;
                }
            }
        }

        [XmlElement("UseMoonlight")]
        public bool UseMoonlight
        {
            get;
            set;
        }

        [XmlElement("UseAdvancedMoonlight")]
        public bool UseAdvancedMoonlight
        {
            get;
            set;
        }

        [XmlIgnore]
        public bool CloseMinimizes
        {
            get; private set;
        }

        [XmlElement("CloseMinimizes")]
        public string CloseMinimizesString
        {
            get => CloseMinimizes.ToString();
            set
            {
                if (bool.TryParse(value, out bool temp))
                {
                    CloseMinimizes = temp;
                }
            }
        }

        // Default UseLang to empty string. Not null
        public string UseLang
        {
            get; set;
        } = "";

        [XmlIgnore]
        public bool DownloadLang
        {
            get; private set;
        }

        [XmlElement("DownloadLang")]
        public string DownloadLangString
        {
            get => DownloadLang.ToString();
            set
            {
                if (bool.TryParse(value, out bool temp))
                {
                    DownloadLang = temp;
                }
            }
        }

        [XmlIgnore]
        public bool FlashWhenLate
        {
            get; private set;
        } = BackingStore.DEFAULT_FLASH_WHEN_LATE;

        [XmlElement("FlashWhenLate")]
        public string FlashWhenLateString
        {
            get => FlashWhenLate.ToString();
            set
            {
                if (bool.TryParse(value, out bool temp))
                {
                    FlashWhenLate = temp;
                }
            }
        }

        public int FlashWhenLateAt
        {
            get; set;
        } = BackingStore.DEFAULT_FLASH_WHEN_LATE_AT;

        [XmlIgnore]
        public TrayIconChoice AppIcon
        {
            get; private set;
        }

        [XmlElement("AppIcon")]
        public string AppIconString
        {
            get => AppIcon.ToString();
            set
            {
                if (Enum.TryParse(value, out TrayIconChoice temp))
                {
                    AppIcon = temp;
                }
            }
        }

        [XmlIgnore]
        public AppThemeChoice AppTheme
        {
            get; set;
        }

        [XmlElement("AppTheme")]
        public string AppThemeString
        {
            get => AppTheme.ToString();
            set
            {
                if (Enum.TryParse(value, out AppThemeChoice temp))
                {
                    AppTheme = temp;
                }
            }
        }

        [XmlIgnore]
        public bool UseOSCServer
        {
            get; private set;
        }

        [XmlElement("UseOSCServer")]
        public string UseOSCServerString
        {
            get => UseOSCServer.ToString();
            set
            {
                if (bool.TryParse(value, out bool temp))
                {
                    UseOSCServer = temp;
                }
            }
        }

        public int OSCServerPort
        {
            get; set;
        } = BackingStore.DEFAULT_OSC_SERV_PORT;

        [XmlIgnore]
        public bool UseOSCSender
        {
            get; private set;
        }

        [XmlIgnore]
        public bool InterpretingOscMonitoring
        {
            get; private set;
        }

        [XmlElement("InterpretingOscMonitoring")]
        public string InterpretingOscMonitoringString
        {
            get => InterpretingOscMonitoring.ToString();
            set
            {
                if (bool.TryParse(value, out bool temp))
                {
                    InterpretingOscMonitoring = temp;
                }
            }
        }

        [XmlElement("UseOSCSender")]
        public string UseOSCSenderString
        {
            get => UseOSCSender.ToString();
            set
            {
                if (bool.TryParse(value, out bool temp))
                {
                    UseOSCSender = temp;
                }
            }
        }

        public int OSCSenderPort
        {
            get; set;
        } = BackingStore.DEFAULT_OSC_SEND_PORT;

        public string OSCSenderAddress
        {
            get; set;
        } = BackingStore.DEFAULT_OSC_SEND_ADDRESS;

        [XmlIgnore]
        public bool UseUDPServer
        {
            get; private set;
        }

        [XmlElement("UseUDPServer")]
        public string UseUDPServerString
        {
            get => UseUDPServer.ToString();
            set
            {
                if (bool.TryParse(value, out bool temp))
                {
                    UseUDPServer = temp;
                }
            }
        }

        public int UDPServerPort
        {
            get; set;
        } = BackingStore.DEFAULT_UDP_SERV_PORT;

        public string UDPServerListenAddress
        {
            get; set;
        } = BackingStore.DEFAULT_UDP_SERV_LISTEN_ADDR;

        public UDPSrvSmoothingOptionsGroup UDPServerSmoothingOptions
        {
            get; set;
        }

        [XmlIgnore]
        public bool UseCustomSteamFolder
        {
            get; private set;
        }

        [XmlElement("UseCustomSteamFolder")]
        public string UseCustomSteamFolderString
        {
            get => UseCustomSteamFolder.ToString();
            set
            {
                if (bool.TryParse(value, out bool temp))
                {
                    UseCustomSteamFolder = temp;
                }
            }
        }

        public string CustomSteamFolder
        {
            get; set;
        } = string.Empty;

        [XmlIgnore]
        public bool AutoProfileRevertDefaultProfile
        {
            get; private set;
        } = BackingStore.DEFAULT_AUTO_PROFILE_REVERT_DEFAULT_PROFILE;

        [XmlElement("AutoProfileRevertDefaultProfile")]
        public string AutoProfileRevertDefaultProfileString
        {
            get => AutoProfileRevertDefaultProfile.ToString();
            set
            {
                if (bool.TryParse(value, out bool temp))
                {
                    AutoProfileRevertDefaultProfile = temp;
                }
            }
        }

        [XmlElement("AutoProfileSwitchNotifyChoice")]
        public AutoProfileDisplayProfileSwitchChoices AutoProfileSwitchNotifyChoice
        {
            get; set;
        } = AutoProfileDisplayProfileSwitchChoices.None;
        public bool ShouldSerializeAutoProfileSwitchNotifyChoice()
        {
            return AutoProfileSwitchNotifyChoice != AutoProfileDisplayProfileSwitchChoices.None;
        }

        [XmlElement("AbsRegionDisplay")]
        public string AbsRegionDisplay
        {
            get; set;
        } = string.Empty;

        public InputDeviceOptions DeviceOptions
        {
            get; set;
        }

        [XmlIgnore]
        public LightbarDS4WinInfo LightbarInfo1
        {
            get; private set;
        }

        [XmlElement("CustomLed1")]
        public string CustomLed1String
        {
            get => BackingStore.CompileCustomLedString(LightbarInfo1);
            set
            {
                BackingStore.ParseCustomLedString(value, LightbarInfo1);
            }
        }

        [XmlIgnore]
        public LightbarDS4WinInfo LightbarInfo2
        {
            get; private set;
        }

        [XmlElement("CustomLed2")]
        public string CustomLed2String
        {
            get => BackingStore.CompileCustomLedString(LightbarInfo2);
            set
            {
                BackingStore.ParseCustomLedString(value, LightbarInfo2);
            }
        }

        [XmlIgnore]
        public LightbarDS4WinInfo LightbarInfo3
        {
            get; private set;
        }

        [XmlElement("CustomLed3")]
        public string CustomLed3String
        {
            get => BackingStore.CompileCustomLedString(LightbarInfo3);
            set
            {
                BackingStore.ParseCustomLedString(value, LightbarInfo3);
            }
        }

        [XmlIgnore]
        public LightbarDS4WinInfo LightbarInfo4
        {
            get; private set;
        }

        [XmlElement("CustomLed4")]
        public string CustomLed4String
        {
            get => BackingStore.CompileCustomLedString(LightbarInfo4);
            set
            {
                BackingStore.ParseCustomLedString(value, LightbarInfo4);
            }
        }

        [XmlIgnore]
        public LightbarDS4WinInfo LightbarInfo5
        {
            get; private set;
        }

        [XmlElement("CustomLed5")]
        public string CustomLed5String
        {
            get => BackingStore.CompileCustomLedString(LightbarInfo5);
            set
            {
                BackingStore.ParseCustomLedString(value, LightbarInfo5);
            }
        }
        public bool ShouldSerializeCustomLed5String()
        {
            return Global.MAX_DS4_CONTROLLER_COUNT >= 5;
        }

        [XmlIgnore]
        public LightbarDS4WinInfo LightbarInfo6
        {
            get; private set;
        }

        [XmlElement("CustomLed6")]
        public string CustomLed6String
        {
            get => BackingStore.CompileCustomLedString(LightbarInfo6);
            set
            {
                BackingStore.ParseCustomLedString(value, LightbarInfo6);
            }
        }
        public bool ShouldSerializeCustomLed6String()
        {
            return Global.MAX_DS4_CONTROLLER_COUNT >= 6;
        }

        [XmlIgnore]
        public LightbarDS4WinInfo LightbarInfo7
        {
            get; private set;
        }

        [XmlElement("CustomLed7")]
        public string CustomLed7String
        {
            get => BackingStore.CompileCustomLedString(LightbarInfo7);
            set
            {
                BackingStore.ParseCustomLedString(value, LightbarInfo7);
            }
        }
        public bool ShouldSerializeCustomLed7String()
        {
            return Global.MAX_DS4_CONTROLLER_COUNT > 7;
        }

        [XmlIgnore]
        public LightbarDS4WinInfo LightbarInfo8
        {
            get; private set;
        }

        [XmlElement("CustomLed8")]
        public string CustomLed8String
        {
            get => BackingStore.CompileCustomLedString(LightbarInfo8);
            set
            {
                BackingStore.ParseCustomLedString(value, LightbarInfo8);
            }
        }
        public bool ShouldSerializeCustomLed8String()
        {
            return Global.MAX_DS4_CONTROLLER_COUNT >= 8;
        }

        public AppSettingsDTO()
        {
            UDPServerSmoothingOptions = new UDPSrvSmoothingOptionsGroup();

            //Controller1CurrentProfile = string.Empty;
            //Controller2CurrentProfile = string.Empty;
            //Controller3CurrentProfile = string.Empty;
            //Controller4CurrentProfile = string.Empty;
            //Controller5CurrentProfile = string.Empty;
            //Controller6CurrentProfile = string.Empty;
            //Controller7CurrentProfile = string.Empty;
            //Controller8CurrentProfile = string.Empty;

            DeviceOptions = new InputDeviceOptions();
            LightbarInfo1 = new LightbarDS4WinInfo();
            LightbarInfo2 = new LightbarDS4WinInfo();
            LightbarInfo3 = new LightbarDS4WinInfo();
            LightbarInfo4 = new LightbarDS4WinInfo();
            LightbarInfo5 = new LightbarDS4WinInfo();
            LightbarInfo6 = new LightbarDS4WinInfo();
            LightbarInfo7 = new LightbarDS4WinInfo();
            LightbarInfo8 = new LightbarDS4WinInfo();
        }

        public void MapFrom(BackingStore source)
        {
            // ProfileEditorレイアウト情報の同期
        ProfileEditorLeftWidth = source.profileEditorLeftWidth > 0 ? source.profileEditorLeftWidth : BackingStore.DEFAULT_PROFILE_EDITOR_LEFT_WIDTH;
        ProfileEditorRightWidth = source.profileEditorRightWidth > 0 ? source.profileEditorRightWidth : BackingStore.DEFAULT_PROFILE_EDITOR_RIGHT_WIDTH;
        SpecialActionNameColWidth = source.specialActionNameColWidth > 0 ? source.specialActionNameColWidth : BackingStore.DEFAULT_SPECIAL_ACTION_NAME_COL_WIDTH;
        SpecialActionTriggerColWidth = source.specialActionTriggerColWidth > 0 ? source.specialActionTriggerColWidth : BackingStore.DEFAULT_SPECIAL_ACTION_TRIGGER_COL_WIDTH;
        SpecialActionDetailColWidth = source.specialActionDetailColWidth > 0 ? source.specialActionDetailColWidth : BackingStore.DEFAULT_SPECIAL_ACTION_DETAIL_COL_WIDTH;
            
            // Controller tab column widths
            ControllerIndexColWidth = source.controllerIndexColWidth > 0 ? source.controllerIndexColWidth : BackingStore.DEFAULT_CONTROLLER_INDEX_COL_WIDTH;
            ControllerIdColWidth = source.controllerIdColWidth > 0 ? source.controllerIdColWidth : BackingStore.DEFAULT_CONTROLLER_ID_COL_WIDTH;
            ControllerStatusColWidth = source.controllerStatusColWidth > 0 ? source.controllerStatusColWidth : BackingStore.DEFAULT_CONTROLLER_STATUS_COL_WIDTH;
            ControllerExclusiveColWidth = source.controllerExclusiveColWidth > 0 ? source.controllerExclusiveColWidth : BackingStore.DEFAULT_CONTROLLER_EXCLUSIVE_COL_WIDTH;
            ControllerBatteryColWidth = source.controllerBatteryColWidth > 0 ? source.controllerBatteryColWidth : BackingStore.DEFAULT_CONTROLLER_BATTERY_COL_WIDTH;
            ControllerLinkProfColWidth = source.controllerLinkProfColWidth > 0 ? source.controllerLinkProfColWidth : BackingStore.DEFAULT_CONTROLLER_LINKPROF_COL_WIDTH;
            ControllerSelectProfileColWidth = source.controllerSelectProfileColWidth > 0 ? source.controllerSelectProfileColWidth : BackingStore.DEFAULT_CONTROLLER_SELECTPROFILE_COL_WIDTH;
            ControllerEditColWidth = source.controllerEditColWidth > 0 ? source.controllerEditColWidth : BackingStore.DEFAULT_CONTROLLER_EDIT_COL_WIDTH;
            ControllerCustomColorColWidth = source.controllerCustomColorColWidth > 0 ? source.controllerCustomColorColWidth : BackingStore.DEFAULT_CONTROLLER_CUSTOMCOLOR_COL_WIDTH;

            // ルート属性に現在のアプリ/設定バージョンをセット（シリアライザで属性として出力される）
            AppVersion = Global.exeversion;
            ConfigVersion = Global.APP_CONFIG_VERSION.ToString();
            UseExclusiveMode = source.useExclusiveMode;
            StartMinimized = source.startMinimized;
            MinimizeToTaskbar = source.minToTaskbar;
            ProcessPriority = source.processPriority;
            FormWidth = source.formWidth;
            FormHeight = source.formHeight;
            FormLocationX = source.formLocationX;
            FormLocationY = source.formLocationY;
            Controller1CurrentProfile = source.UsedSavedProfileString(0);
            Controller2CurrentProfile = source.UsedSavedProfileString(1);
            Controller3CurrentProfile = source.UsedSavedProfileString(2);
            Controller4CurrentProfile = source.UsedSavedProfileString(3);
            Controller5CurrentProfile = source.UsedSavedProfileString(4);
            Controller6CurrentProfile = source.UsedSavedProfileString(5);
            Controller7CurrentProfile = source.UsedSavedProfileString(6);
            Controller8CurrentProfile = source.UsedSavedProfileString(7);

            // Selected Profile settings
            SelectedProfile1 = !string.IsNullOrEmpty(source.selectedProfile[0]) ? source.selectedProfile[0] : string.Empty;
            SelectedProfile2 = !string.IsNullOrEmpty(source.selectedProfile[1]) ? source.selectedProfile[1] : string.Empty;
            SelectedProfile3 = !string.IsNullOrEmpty(source.selectedProfile[2]) ? source.selectedProfile[2] : string.Empty;
            SelectedProfile4 = !string.IsNullOrEmpty(source.selectedProfile[3]) ? source.selectedProfile[3] : string.Empty;
            SelectedProfile5 = !string.IsNullOrEmpty(source.selectedProfile[4]) ? source.selectedProfile[4] : string.Empty;
            SelectedProfile6 = !string.IsNullOrEmpty(source.selectedProfile[5]) ? source.selectedProfile[5] : string.Empty;
            SelectedProfile7 = !string.IsNullOrEmpty(source.selectedProfile[6]) ? source.selectedProfile[6] : string.Empty;
            SelectedProfile8 = !string.IsNullOrEmpty(source.selectedProfile[7]) ? source.selectedProfile[7] : string.Empty;

            LastChecked = source.lastChecked;
            CheckWhen = source.CheckWhen;
            LastVersionChecked = source.lastVersionChecked;
            Notifications = source.notifications;
            ProfileChangedNotification = source.profileChangedNotification;
            DisconnectBTAtStop = source.disconnectBTAtStop;
            SwipeProfiles = source.swipeProfiles;
            QuickCharge = source.quickCharge;
            UseMoonlight = source.useMoonlight;
            UseAdvancedMoonlight = source.useAdvancedMoonlight;
            CloseMinimizes = source.closeMini;
            UseLang = source.useLang;
            DownloadLang = source.downloadLang;
            FlashWhenLate = source.flashWhenLate;
            FlashWhenLateAt = source.flashWhenLateAt;
            AppIcon = source.useIconChoice;
            AppTheme = source.useCurrentTheme;
            UseOSCServer = source.useOSCServ;
            OSCServerPort = source.oscServPort;
            InterpretingOscMonitoring = source.interpretingOscMonitoring;
            UseOSCSender = source.useOSCSend;
            OSCSenderPort = source.oscSendPort;
            OSCSenderAddress = source.oscSendAddress;
            UseUDPServer = source.useUDPServ;
            UDPServerPort = source.udpServPort;
            UDPServerListenAddress = source.udpServListenAddress;
            UDPServerSmoothingOptions = new UDPSrvSmoothingOptionsGroup()
            {
                UseSmoothing = source.useUdpSmoothing,
                UdpSmoothMinCutoff = source.udpSmoothingMincutoff,
                UdpSmoothBeta = source.udpSmoothingBeta,
            };
            UseCustomSteamFolder = source.useCustomSteamFolder;
            CustomSteamFolder = source.customSteamFolder;
            AutoProfileRevertDefaultProfile = source.autoProfileRevertDefaultProfile;
            AutoProfileSwitchNotifyChoice = source.autoProfileSwitchNotifyChoice;
            AbsRegionDisplay = source.absDisplayEDID;

            DeviceOptions = new InputDeviceOptions()
            {
                DS4SupportSettings = new DS4SupportSettingsGroup()
                {
                    Enabled = source.deviceOptions.DS4DeviceOpts.Enabled,
                },
                DualSenseSupportSettings = new DualSenseSupportSettings()
                {
                    Enabled = source.deviceOptions.DualSenseOpts.Enabled,
                },
                SwitchProSupportSettings = new SwitchProSupportSettings()
                {
                    Enabled = source.deviceOptions.SwitchProDeviceOpts.Enabled,
                },
                JoyConSupportSettings = new JoyConSupportSettings()
                {
                    Enabled = source.deviceOptions.JoyConDeviceOpts.Enabled,
                    LinkMode = source.deviceOptions.JoyConDeviceOpts.LinkedMode,
                    JoinedGyroProvider = source.deviceOptions.JoyConDeviceOpts.JoinGyroProv,
                },
                DS3SupportSettings = new DS3SupportSettings()
                {
                    Enabled = source.deviceOptions.DS3DeviceOpts.Enabled,
                }
            };

            LightbarDS4WinInfo[] tempLightArray = new LightbarDS4WinInfo[]
            {
                LightbarInfo1, LightbarInfo2, LightbarInfo3, LightbarInfo4,
                LightbarInfo5, LightbarInfo6, LightbarInfo7, LightbarInfo8,
            };

            for (int i = 0; i < Global.MAX_DS4_CONTROLLER_COUNT; i++)
            {
                LightbarDS4WinInfo lightbarDS4Win = source.ObtainLightbarDS4WinInfo(i);
                LightbarDS4WinInfo tempInstance = tempLightArray[i];
                tempInstance.useCustomLed = lightbarDS4Win.useCustomLed;
                tempInstance.m_CustomLed = lightbarDS4Win.m_CustomLed;
            }
        }

        public void MapTo(BackingStore destination)
        {
            // ProfileEditorレイアウト情報の同期
        destination.profileEditorLeftWidth = ProfileEditorLeftWidth > 0 ? ProfileEditorLeftWidth : BackingStore.DEFAULT_PROFILE_EDITOR_LEFT_WIDTH;
        destination.profileEditorRightWidth = ProfileEditorRightWidth > 0 ? ProfileEditorRightWidth : BackingStore.DEFAULT_PROFILE_EDITOR_RIGHT_WIDTH;
        destination.specialActionNameColWidth = SpecialActionNameColWidth > 0 ? SpecialActionNameColWidth : BackingStore.DEFAULT_SPECIAL_ACTION_NAME_COL_WIDTH;
        destination.specialActionTriggerColWidth = SpecialActionTriggerColWidth > 0 ? SpecialActionTriggerColWidth : BackingStore.DEFAULT_SPECIAL_ACTION_TRIGGER_COL_WIDTH;
        destination.specialActionDetailColWidth = SpecialActionDetailColWidth > 0 ? SpecialActionDetailColWidth : BackingStore.DEFAULT_SPECIAL_ACTION_DETAIL_COL_WIDTH;
            
            // Controller tab column widths
            destination.controllerIndexColWidth = ControllerIndexColWidth > 0 ? ControllerIndexColWidth : BackingStore.DEFAULT_CONTROLLER_INDEX_COL_WIDTH;
            destination.controllerIdColWidth = ControllerIdColWidth > 0 ? ControllerIdColWidth : BackingStore.DEFAULT_CONTROLLER_ID_COL_WIDTH;
            destination.controllerStatusColWidth = ControllerStatusColWidth > 0 ? ControllerStatusColWidth : BackingStore.DEFAULT_CONTROLLER_STATUS_COL_WIDTH;
            destination.controllerExclusiveColWidth = ControllerExclusiveColWidth > 0 ? ControllerExclusiveColWidth : BackingStore.DEFAULT_CONTROLLER_EXCLUSIVE_COL_WIDTH;
            destination.controllerBatteryColWidth = ControllerBatteryColWidth > 0 ? ControllerBatteryColWidth : BackingStore.DEFAULT_CONTROLLER_BATTERY_COL_WIDTH;
            destination.controllerLinkProfColWidth = ControllerLinkProfColWidth > 0 ? ControllerLinkProfColWidth : BackingStore.DEFAULT_CONTROLLER_LINKPROF_COL_WIDTH;
            destination.controllerSelectProfileColWidth = ControllerSelectProfileColWidth > 0 ? ControllerSelectProfileColWidth : BackingStore.DEFAULT_CONTROLLER_SELECTPROFILE_COL_WIDTH;
            destination.controllerEditColWidth = ControllerEditColWidth > 0 ? ControllerEditColWidth : BackingStore.DEFAULT_CONTROLLER_EDIT_COL_WIDTH;
            destination.controllerCustomColorColWidth = ControllerCustomColorColWidth > 0 ? ControllerCustomColorColWidth : BackingStore.DEFAULT_CONTROLLER_CUSTOMCOLOR_COL_WIDTH;

            destination.useExclusiveMode = UseExclusiveMode;
            destination.startMinimized = StartMinimized;
            destination.minToTaskbar = MinimizeToTaskbar;
            destination.processPriority = ProcessPriority;
            destination.formWidth = FormWidth;
            destination.formHeight = FormHeight;
            destination.formLocationX = FormLocationX;
            destination.formLocationY = FormLocationY;
            destination.profilePath[0] = destination.olderProfilePath[0] = !string.IsNullOrEmpty(Controller1CurrentProfile) ? Controller1CurrentProfile : string.Empty;
            destination.profilePath[1] = destination.olderProfilePath[1] = !string.IsNullOrEmpty(Controller2CurrentProfile) ? Controller2CurrentProfile : string.Empty;
            destination.profilePath[2] = destination.olderProfilePath[2] = !string.IsNullOrEmpty(Controller3CurrentProfile) ? Controller3CurrentProfile : string.Empty;
            destination.profilePath[3] = destination.olderProfilePath[3] = !string.IsNullOrEmpty(Controller4CurrentProfile) ? Controller4CurrentProfile : string.Empty;
            destination.profilePath[4] = destination.olderProfilePath[4] = !string.IsNullOrEmpty(Controller5CurrentProfile) ? Controller5CurrentProfile : string.Empty;
            destination.profilePath[5] = destination.olderProfilePath[5] = !string.IsNullOrEmpty(Controller6CurrentProfile) ? Controller6CurrentProfile : string.Empty;
            destination.profilePath[6] = destination.olderProfilePath[6] = !string.IsNullOrEmpty(Controller7CurrentProfile) ? Controller7CurrentProfile : string.Empty;
            destination.profilePath[7] = destination.olderProfilePath[7] = !string.IsNullOrEmpty(Controller8CurrentProfile) ? Controller8CurrentProfile : string.Empty;

            // Selected Profile settings
            destination.selectedProfile[0] = !string.IsNullOrEmpty(SelectedProfile1) ? SelectedProfile1 : string.Empty;
            destination.selectedProfile[1] = !string.IsNullOrEmpty(SelectedProfile2) ? SelectedProfile2 : string.Empty;
            destination.selectedProfile[2] = !string.IsNullOrEmpty(SelectedProfile3) ? SelectedProfile3 : string.Empty;
            destination.selectedProfile[3] = !string.IsNullOrEmpty(SelectedProfile4) ? SelectedProfile4 : string.Empty;
            destination.selectedProfile[4] = !string.IsNullOrEmpty(SelectedProfile5) ? SelectedProfile5 : string.Empty;
            destination.selectedProfile[5] = !string.IsNullOrEmpty(SelectedProfile6) ? SelectedProfile6 : string.Empty;
            destination.selectedProfile[6] = !string.IsNullOrEmpty(SelectedProfile7) ? SelectedProfile7 : string.Empty;
            destination.selectedProfile[7] = !string.IsNullOrEmpty(SelectedProfile8) ? SelectedProfile8 : string.Empty;

            destination.lastChecked = LastChecked;
            destination.CheckWhen = CheckWhen;
            destination.lastVersionChecked = LastVersionChecked;
            destination.notifications = Notifications;
            destination.profileChangedNotification = ProfileChangedNotification;
            destination.disconnectBTAtStop = DisconnectBTAtStop;
            destination.swipeProfiles = SwipeProfiles;
            destination.quickCharge = QuickCharge;
            destination.useMoonlight = UseMoonlight;
            destination.useAdvancedMoonlight = UseAdvancedMoonlight;
            destination.closeMini = CloseMinimizes;
            destination.useLang = UseLang;
            destination.downloadLang = DownloadLang;
            destination.flashWhenLate = FlashWhenLate;
            destination.flashWhenLateAt = FlashWhenLateAt;
            destination.useIconChoice = AppIcon;
            destination.useCurrentTheme = AppTheme;
            destination.useOSCServ = UseOSCServer;
            destination.oscServPort = OSCServerPort;
            destination.interpretingOscMonitoring = InterpretingOscMonitoring;
            destination.useOSCSend = UseOSCSender;
            destination.oscSendPort = OSCSenderPort;
            if (!string.IsNullOrEmpty(OSCSenderAddress))
            {
                destination.oscSendAddress = OSCSenderAddress;
            }

            destination.useUDPServ = UseUDPServer;
            destination.udpServPort = UDPServerPort;

            if (!string.IsNullOrEmpty(UDPServerListenAddress))
            {
                destination.udpServListenAddress = UDPServerListenAddress;
            }
            destination.useUdpSmoothing = UDPServerSmoothingOptions.UseSmoothing;
            destination.udpSmoothingMincutoff = UDPServerSmoothingOptions.UdpSmoothMinCutoff;
            destination.udpSmoothingBeta = UDPServerSmoothingOptions.UdpSmoothBeta;

            destination.useCustomSteamFolder = UseCustomSteamFolder;
            destination.customSteamFolder = CustomSteamFolder;
            destination.autoProfileRevertDefaultProfile = AutoProfileRevertDefaultProfile;
            destination.autoProfileSwitchNotifyChoice = AutoProfileSwitchNotifyChoice;
            if (!string.IsNullOrEmpty(AbsRegionDisplay))
            {
                destination.absDisplayEDID = AbsRegionDisplay;
            }

            destination.deviceOptions.DS4DeviceOpts.Enabled = DeviceOptions.DS4SupportSettings.Enabled;
            destination.deviceOptions.DualSenseOpts.Enabled = DeviceOptions.DualSenseSupportSettings.Enabled;
            destination.deviceOptions.SwitchProDeviceOpts.Enabled = DeviceOptions.SwitchProSupportSettings.Enabled;
            destination.deviceOptions.JoyConDeviceOpts.Enabled = DeviceOptions.JoyConSupportSettings.Enabled;
            destination.deviceOptions.JoyConDeviceOpts.LinkedMode = DeviceOptions.JoyConSupportSettings.LinkMode;
            destination.deviceOptions.JoyConDeviceOpts.JoinGyroProv = DeviceOptions.JoyConSupportSettings.JoinedGyroProvider;
            destination.deviceOptions.DS3DeviceOpts.Enabled = DeviceOptions.DS3SupportSettings.Enabled;

            LightbarDS4WinInfo[] tempLightArray = new LightbarDS4WinInfo[]
            {
                LightbarInfo1, LightbarInfo2, LightbarInfo3, LightbarInfo4,
                LightbarInfo5, LightbarInfo6, LightbarInfo7, LightbarInfo8,
            };

            for (int i = 0; i < Global.MAX_DS4_CONTROLLER_COUNT; i++)
            {
                LightbarDS4WinInfo tempInstance = tempLightArray[i];
                destination.PopulateLightbarDS4WinInfo(i, tempInstance);
            }
        }
    }

    public class UDPSrvSmoothingOptionsGroup
    {
        [XmlIgnore]
        public bool UseSmoothing
        {
            get; set;
        }

        [XmlElement("UseSmoothing")]
        public string UseSmoothingString
        {
            get => UseSmoothing.ToString();
            set
            {
                if (bool.TryParse(value, out bool temp))
                {
                    UseSmoothing = temp;
                }
            }
        }

        public double UdpSmoothMinCutoff
        {
            get; set;
        } = BackingStore.DEFAULT_UDP_SMOOTH_MINCUTOFF;

        public double UdpSmoothBeta
        {
            get; set;
        } = BackingStore.DEFAULT_UDP_SMOOTH_BETA;
    }

    public class InputDeviceOptions
    {
        public DS4SupportSettingsGroup DS4SupportSettings
        {
            get; set;
        } = new DS4SupportSettingsGroup();

        public DualSenseSupportSettings DualSenseSupportSettings
        {
            get; set;
        } = new DualSenseSupportSettings();

        public SwitchProSupportSettings SwitchProSupportSettings
        {
            get; set;
        } = new SwitchProSupportSettings();

        public JoyConSupportSettings JoyConSupportSettings
        {
            get; set;
        } = new JoyConSupportSettings();

        public DS3SupportSettings DS3SupportSettings
        {
            get; set;
        } = new DS3SupportSettings();
    }

    public abstract class BaseInputDeviceSettingsGroup
    {
        [XmlIgnore]
        public bool Enabled
        {
            get; set;
        }

        [XmlElement("Enabled")]
        public string EnabledString
        {
            get => Enabled.ToString();
            set
            {
                if (bool.TryParse(value, out bool temp))
                {
                    Enabled = temp;
                }
            }
        }
    }

    public class DS4SupportSettingsGroup : BaseInputDeviceSettingsGroup
    {
        public DS4SupportSettingsGroup(): base()
        {
            Enabled = DS4DeviceOptions.DEFAULT_ENABLE;
        }
    }

    public class DS3SupportSettings : BaseInputDeviceSettingsGroup
    {
        public DS3SupportSettings() : base()
        {
            Enabled = DS3DeviceOptions.DEFAULT_ENABLE;
        }
    }

    public class DualSenseSupportSettings : BaseInputDeviceSettingsGroup
    {
        public DualSenseSupportSettings() : base()
        {
            Enabled = DualSenseDeviceOptions.DEFAULT_ENABLE;
        }
    }

    public class SwitchProSupportSettings : BaseInputDeviceSettingsGroup
    {
        public SwitchProSupportSettings() : base()
        {
            Enabled = SwitchProDeviceOptions.DEFAULT_ENABLE;
        }
    }

    public class JoyConSupportSettings : BaseInputDeviceSettingsGroup
    {
        public JoyConDeviceOptions.LinkMode LinkMode
        {
            get; set;
        } = JoyConDeviceOptions.LINK_MODE_DEFAULT;

        public JoyConDeviceOptions.JoinedGyroProvider JoinedGyroProvider
        {
            get; set;
        }

        public JoyConSupportSettings() : base()
        {
            Enabled = JoyConDeviceOptions.DEFAULT_ENABLE;
        }
    }
