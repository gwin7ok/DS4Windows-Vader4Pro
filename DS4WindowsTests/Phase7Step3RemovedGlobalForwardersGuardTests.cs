using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase7-Step3: <c>Global</c> の、設定の転送だけを行っていたメンバー（<c>m_Config.xxx</c> への薄い転送）を削除した結果の回帰ガード。
    /// 設定を使うサービスは、注入された <c>BackingStore</c> を直接使う。削除済みのメンバーが <c>Global.cs</c> に再び宣言されないこと、
    /// および <c>AppSettingsService</c> が <c>Global</c> の削除済みメンバーを参照しないことを確かめる。
    /// 判断の根拠は <c>Phase7-Step3-Plan.md</c>。ソースが見つからない環境では、検査を行わず合格とする。
    /// </summary>
    public class Phase7Step3RemovedGlobalForwardersGuardTests
    {
        // Wave ごとに追記する（W2: 画面・ウィンドウの寸法と動作設定、W3: 通信と更新確認、W4: 処理を持つ転送、W5: ほかのサービス）
        private static readonly string[] Removed =
        {
            "StartMinimized", "MinToTaskbar", "CloseMini", "UseExclusiveMode", "AutoProfileRevertDefaultProfile",
            "FormWidth", "FormHeight", "FormLocationX", "FormLocationY",
            "ControllerIndexColWidth", "ControllerIdColWidth", "ControllerStatusColWidth", "ControllerExclusiveColWidth",
            "ControllerBatteryColWidth", "ControllerSelectProfileColWidth", "ControllerEditColWidth",
            "ControllerLinkedProfileColWidth", "ControllerLinkProfIdColWidth", "ControllerCustomColorColWidth",
            "SwipeProfiles", "ProfileEditorLeftWidth", "ProfileEditorRightWidth",
            "SpecialActionNameColWidth", "SpecialActionTriggerColWidth", "SpecialActionDetailColWidth",
            "QuickCharge", "DCBTatStop", "ProcessPriority", "FlashWhenLate", "FlashWhenLateAt",
            "UseUDPSeverSmoothing", "UseCustomSteamFolder", "CustomSteamFolder", "CheckUpdateStartupEnabled",
            // W3: 通信（UDP／OSC）と更新確認
            "isUsingUDPServer", "setUsingUDPServer", "getUDPServerPortNum", "setUDPServerPort",
            "getUDPServerListenAddress", "setUDPServerListenAddress", "isUsingOSCServer", "setUsingOSCServer",
            "isUsingOSCSender", "setUsingOSCSender", "isInterpretingOscMonitoring", "setInterpretingOscMonitoring",
            "setOSCServerPort", "getOSCSenderAddress", "setOSCSenderAddress", "getOSCSenderPortNum", "setOSCSenderPort",
            "CheckEveryValue", "CheckEveryUnit", "LastChecked", "LastVersionCheckedNum",
            // W4: 処理を持つ転送（偽の exe 名の検証、UDP 平滑化の変更通知）
            "FakeExeName", "UDPServerSmoothingMincutoff", "UDPServerSmoothingBeta",
            "UDPServerSmoothingMincutoffChanged", "UDPServerSmoothingBetaChanged",
            // W5: 各サービスへ移した転送（プロファイル、リンクプロファイル、外観、自動プロファイル、接続フラグ）
            "LinkedProfileUI", "ProfileActions", "SaveLinkedProfiles", "LoadLinkedProfiles",
            "CacheExtraProfileInfo", "CacheProfileCustomsFlags", "changeLinkedProfile", "removeLinkedProfile",
            "containsLinkedProfile", "getLinkedProfile", "UseIconChoice", "autoProfileSwitchNotifyChoice",
            "IsFirstConnection", "MarkConnected", "ResetConnectionFlags", "RefreshExtrasButtons",
            // Step3c W2: プリセット（ProfilePresetLoader へ移した）
            "LoadBlankDevProfile", "LoadBlankDS4Profile", "LoadDefaultGamepadGyroProfile", "LoadDefaultDS4GamepadGyroProfile",
            "LoadDefaultMixedControlsProfile", "LoadDefaultDS4MixedControlsProfile", "LoadDefaultMixedGyroMouseProfile",
            "LoadDefaultDS4MixedGyroMouseProfile", "LoadDefaultKBMProfile", "LoadDefaultKBMGyroMouseProfile",
            // Step3c W3: BackingStore のインスタンスのフィールドに移した
            "loggedInvalidActions",
            // Step3c W4: プロファイル適用（ProfileApplicationService／ProfileRepository／ProfileSlotApplier へ移した）
            "ApplyProfile", "ApplyProfileToSlot", "CompleteProfileApplication", "LoadProfile", "LoadTempProfile",
            "ProfilePath", "OlderProfilePath", "SelectedProfile", "SelectedProfileChanged", "RaiseSelectedProfileChanged",
            "getProfileActions", "GetProfileAction",
            // Step3c W5: AppSettingsService が BackingStore を直接使う
            "ProfileChangedNotification",
        };

        private readonly ITestOutputHelper _output;

        public Phase7Step3RemovedGlobalForwardersGuardTests(ITestOutputHelper output)
        {
            _output = output;
        }

        private static string StripComments(string text)
        {
            text = Regex.Replace(text, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);
            text = Regex.Replace(text, @"//[^\r\n]*", string.Empty);
            return text;
        }

        private string Read(string relativePath)
        {
            string path = SourceFileLocator.Find(relativePath);
            if (path == null)
            {
                _output.WriteLine("ソースが見つからないため検査を省略した: " + relativePath);
                return null;
            }

            return StripComments(File.ReadAllText(path));
        }

        [Fact]
        public void RemovedForwarders_DoNotReappearInGlobal()
        {
            string text = Read("DS4Windows/DS4Control/Global.cs");
            if (text == null) return;

            List<string> reappeared = Removed
                .Where(n => Regex.IsMatch(text, @"(?m)^\s*public\s+static\s+[\w<>\[\],.? ]+\s+" + Regex.Escape(n) + @"\s*(\(|\{|=>|;|=)"))
                .ToList();
            Assert.True(reappeared.Count == 0, "削除済みの転送メンバーが Global.cs に再び宣言されている: " + string.Join(", ", reappeared));
        }

        [Fact]
        public void AppSettingsService_DoesNotReferenceRemovedGlobalForwarders()
        {
            string text = Read("DS4Windows/DS4Control/Services/AppSettingsService.cs");
            if (text == null) return;

            List<string> used = Removed
                .Where(n => Regex.IsMatch(text, @"(?<![\w.])Global\." + Regex.Escape(n) + @"\b"))
                .ToList();
            Assert.True(used.Count == 0, "AppSettingsService が削除済みの Global のメンバーを参照している: " + string.Join(", ", used));
        }
    }
}
