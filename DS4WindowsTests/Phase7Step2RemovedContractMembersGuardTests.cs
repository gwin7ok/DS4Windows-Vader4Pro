using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase7-Step2: DI インターフェースの、本体の呼び出しが 0 件だったメンバーを削除した結果の回帰ガード。
    /// (1) 削除済みのメンバーが、契約（<c>DS4Windows/DI/*.cs</c>）に再び現れないこと。
    /// (2) 削除済みのファイルが再び存在しないこと。
    /// (3) 「使うべきだが未接続」と判断して残した契約（TODO 付き）が、削除されていないこと。
    /// 判断の根拠は <c>Phase7-Step2-Plan.md</c> §2。ソースが見つからない環境では、検査を行わず合格とする。
    /// </summary>
    public class Phase7Step2RemovedContractMembersGuardTests
    {
        // インターフェースのファイル名（拡張子なし） → 削除したメンバー
        private static readonly Dictionary<string, string[]> Removed = new Dictionary<string, string[]>
        {
            ["IProfileSettingsService"] = new[]
            {
                "GetLowColor", "GetChargingColor", "GetCustomColor", "GetFlashColor", "GetUseCustomLed", "GetDS4CSettings",
                "GetGyroMouseDeadZone", "GetScrollSensitivity", "GetUseDInputOnly", "SetUseDInputOnly", "GetTempProfileDistance",
                "GetReverseX360ButtonMapping", "SAMouseStickTriggerCond", "SetTouchpadActive", "LinkedProfileCheckArray",
                "TempProfileNameArray", "ConfigDecimalCulture", "ResetAllToDefaults", "ResetToDefaults", "RumbleAutostopTime",
                "ProfileSettingChanged", "WireSubSettingsEvents", "UnwireSubSettingsEvents",
            },
            ["IDeviceStateService"] = new[]
            {
                "Devices", "GetDevice", "IsDeviceConnected", "ConnectedControllersCount", "GetDeviceMacAddress",
                "GetConnectionType", "GetBatteryLevel", "SetDevice", "DeviceStateChanged",
            },
            ["IOutputSlotService"] = new[] { "OutputDevices", "GetOutputDevice", "IsSlotPlugin" },
            ["IVirtualKBM"] = new[] { "ErrorMessage", "Version" },
            ["IUdpServerService"] = new[] { "ListenAddress" },
            ["INotificationService"] = new[] { "NotificationsEnabled", "FlashTaskbar" },
            ["IPathService"] = new[] { "GetAutoProfilesPath", "ActionsPath" },
            ["IProfileRepository"] = new[] { "GetProfileNames", "ProfileExists", "LoadProfileToSlot", "LoadDefaultProfile" },
            ["ISpecialActionRepository"] = new[] { "AddAction", "ReplaceAction", "RemoveAction", "GetActionIndex", "GetAction", "ActionExists", "SaveActions" },
            ["IDs4DeviceRegistry"] = new[] { "ConnectedDevices", "DeviceCount", "IsHidHideInstalled" },
        };

        // 削除したファイル
        private static readonly string[] RemovedFiles =
        {
            "DS4Windows/DI/DeviceStateChangedEventArgs.cs",
            "DS4Windows/DI/ProfileSettingChangedEventArgs.cs",
            "DS4Windows/DS4Forms/ViewModels/ControllersViewModel.cs",
        };

        // 残した契約（本来使うべきだが、呼び出し側の切り替えが未実施。TODO 付き）
        private static readonly Dictionary<string, string[]> Kept = new Dictionary<string, string[]>
        {
            ["IEnvironmentService"] = new[] { "UsingMaxControllers" },
            ["IDs4DeviceRegistry"] = new[] { "ReEnableDevice" },
            ["IOutputSlotService"] = new[] { "LoadOutputSlots", "SaveOutputSlots", "OutputSlotChanged" },
            ["IDeviceStateService"] = new[] { "IsFirstConnection", "MarkConnected", "ResetConnectionFlags", "OnDeviceSerialChange" },
            ["IUdpServerService"] = new[] { "IsRunning", "Port", "Start", "Stop" },
        };

        private readonly ITestOutputHelper _output;

        public Phase7Step2RemovedContractMembersGuardTests(ITestOutputHelper output)
        {
            _output = output;
        }

        private static string StripComments(string text)
        {
            text = Regex.Replace(text, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);
            text = Regex.Replace(text, @"//[^\r\n]*", string.Empty);
            return text;
        }

        private static bool Declares(string strippedSource, string member)
        {
            string name = Regex.Escape(member);
            return Regex.IsMatch(strippedSource, @"(?m)^\s*[\w<>\[\],.? ]+\s+" + name + @"\s*(\(|\{|;)")
                || Regex.IsMatch(strippedSource, @"(?m)^\s*event\s+[\w<>\[\],.? ]+\s+" + name + @"\s*;");
        }

        private string ReadContract(string interfaceName)
        {
            string path = SourceFileLocator.Find("DS4Windows/DI/" + interfaceName + ".cs");
            if (path == null)
            {
                _output.WriteLine("ソースが見つからないため検査を省略した: " + interfaceName);
                return null;
            }

            return StripComments(File.ReadAllText(path));
        }

        [Fact]
        public void RemovedMembers_DoNotReappearInContracts()
        {
            var reappeared = new List<string>();
            foreach (var pair in Removed)
            {
                string text = ReadContract(pair.Key);
                if (text == null) continue;
                reappeared.AddRange(pair.Value.Where(m => Declares(text, m)).Select(m => pair.Key + "." + m));
            }

            Assert.True(reappeared.Count == 0, "削除済みのメンバーが契約に再び宣言されている: " + string.Join(", ", reappeared));
        }

        [Fact]
        public void KeptMembers_StillExistInContracts()
        {
            var missing = new List<string>();
            foreach (var pair in Kept)
            {
                string text = ReadContract(pair.Key);
                if (text == null) continue;
                missing.AddRange(pair.Value.Where(m => !Declares(text, m)).Select(m => pair.Key + "." + m));
            }

            Assert.True(missing.Count == 0, "残すべき契約が失われている（TODO 付きの未接続の契約）: " + string.Join(", ", missing));
        }

        [Fact]
        public void RemovedFiles_DoNotExist()
        {
            string anchor = SourceFileLocator.Find("DS4Windows/DS4Control/Global.cs");
            if (anchor == null)
            {
                _output.WriteLine("ソースが見つからないため検査を省略した。");
                return;
            }

            string root = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(anchor), "..", ".."));
            List<string> existing = RemovedFiles.Where(f => File.Exists(Path.Combine(root, f))).ToList();
            Assert.True(existing.Count == 0, "削除済みのファイルが再び存在する: " + string.Join(", ", existing));
        }
    }
}
