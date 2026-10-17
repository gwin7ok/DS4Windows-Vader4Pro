using System;
using System.IO;
using System.Text.RegularExpressions;
using Xunit;
using DS4Windows;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase6-Step10-3d: ボタン種別のスペシャルアクションの、Press／Toggle の出力判定（<see cref="ButtonSpecialActionOutput"/>）。
    /// </summary>
    // ActionManager（静的）と ServiceProviderHolder を使うため、同じコレクションで直列に実行する
    [Collection("ServiceProviderHolder")]
    public class ButtonSpecialActionOutputTests
    {
        private static SpecialAction CreateButtonAction(string name, string switchMode)
        {
            // details は X360Controls の番号。extras の 1 行目が SwitchMode
            return new SpecialAction(name, "Cross", "Button", "1", 0, switchMode + "\n");
        }

        private static void ArmFreshPress(SpecialAction sa, int device, bool requiresFreshPress)
        {
            var state = ActionManager.GetStateFor(sa, device);
            Assert.NotNull(state);
            state.IsToggledOn = false;
            state.RequiresFreshPressAfterReset = requiresFreshPress;
        }

        [Fact]
        public void Press_FollowsTrigger()
        {
            DS4WinWPF.AppHost.CreateHost();
            var sa = CreateButtonAction("Step10_3d_press_" + Guid.NewGuid().ToString("N"), "Press");
            Assert.Equal(SpecialAction.KeyButtonSwitchModeEnum.Press, sa.KeyButtonSwitchMode);

            Assert.False(ButtonSpecialActionOutput.ResolveOutputActive(sa, 0, false, false));
            Assert.True(ButtonSpecialActionOutput.ResolveOutputActive(sa, 0, true, false));
            Assert.True(ButtonSpecialActionOutput.ResolveOutputActive(sa, 0, true, true));
            Assert.False(ButtonSpecialActionOutput.ResolveOutputActive(sa, 0, false, true));
        }

        [Fact]
        public void Toggle_FlipsOnRisingEdge_AndHoldsUntilNextPress()
        {
            DS4WinWPF.AppHost.CreateHost();
            var sa = CreateButtonAction("Step10_3d_toggle_" + Guid.NewGuid().ToString("N"), "Toggle");
            Assert.Equal(SpecialAction.KeyButtonSwitchModeEnum.Toggle, sa.KeyButtonSwitchMode);
            ArmFreshPress(sa, 0, requiresFreshPress: false);

            // 未操作: OFF
            Assert.False(ButtonSpecialActionOutput.ResolveOutputActive(sa, 0, false, false));
            // 1 回目の押下（立ち上がり）: ON
            Assert.True(ButtonSpecialActionOutput.ResolveOutputActive(sa, 0, true, false));
            // 押しっぱなし: ON のまま（反転しない）
            Assert.True(ButtonSpecialActionOutput.ResolveOutputActive(sa, 0, true, true));
            // 離す: ON を保持（Press と違い、離しても解除されない）
            Assert.True(ButtonSpecialActionOutput.ResolveOutputActive(sa, 0, false, true));
            Assert.True(ButtonSpecialActionOutput.ResolveOutputActive(sa, 0, false, false));
            // 2 回目の押下（立ち上がり）: OFF
            Assert.False(ButtonSpecialActionOutput.ResolveOutputActive(sa, 0, true, false));
            Assert.False(ButtonSpecialActionOutput.ResolveOutputActive(sa, 0, false, true));
            // 3 回目の押下: 再び ON
            Assert.True(ButtonSpecialActionOutput.ResolveOutputActive(sa, 0, true, false));
        }

        [Fact]
        public void Toggle_DoesNotFlip_WhileFreshPressGuardIsActive()
        {
            DS4WinWPF.AppHost.CreateHost();
            var sa = CreateButtonAction("Step10_3d_guard_" + Guid.NewGuid().ToString("N"), "Toggle");
            ArmFreshPress(sa, 0, requiresFreshPress: true);

            // プロファイル適用・接続の直後の押しっぱなし（立ち上がりに見える）では、ON にならない
            Assert.False(ButtonSpecialActionOutput.ResolveOutputActive(sa, 0, true, false));
            Assert.False(ButtonSpecialActionOutput.ResolveOutputActive(sa, 0, true, true));

            // ガードの解除後（離して 80ms 経過した後）の押下では、ON になる
            ActionManager.GetStateFor(sa, 0).RequiresFreshPressAfterReset = false;
            Assert.True(ButtonSpecialActionOutput.ResolveOutputActive(sa, 0, true, false));
        }

        [Fact]
        public void Toggle_StateIsPerDevice()
        {
            DS4WinWPF.AppHost.CreateHost();
            var sa = CreateButtonAction("Step10_3d_device_" + Guid.NewGuid().ToString("N"), "Toggle");
            ArmFreshPress(sa, 0, false);
            ArmFreshPress(sa, 1, false);

            Assert.True(ButtonSpecialActionOutput.ResolveOutputActive(sa, 0, true, false));
            Assert.False(ButtonSpecialActionOutput.ResolveOutputActive(sa, 1, false, false));
        }

        [Fact]
        public void Mapping_ButtonBranch_DoesNotOutputOrDispatch_AndStage3UsesResolver()
        {
            string path = SourceFileLocator.Find("DS4Windows/DS4Control/Mapping.cs");
            if (path == null) return;
            string text = File.ReadAllText(path);
            text = Regex.Replace(text, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);
            text = Regex.Replace(text, @"//[^\r\n]*", string.Empty);

            // MapCustomAction の Button の分岐は、出力もディスパッチもしない（Stage3 に一本化）
            const string buttonBranch = "else if (action.typeID == SpecialAction.ActionTypeId.Button)";
            const string keyBranch = "else if (action.typeID == SpecialAction.ActionTypeId.Key)";
            int start = text.IndexOf(buttonBranch, StringComparison.Ordinal);
            Assert.True(start >= 0, "Button の分岐が見つからない");
            int end = text.IndexOf(keyBranch, start, StringComparison.Ordinal);
            Assert.True(end > start, "Key の分岐が見つからない");
            string branch = text.Substring(start, end - start);
            Assert.DoesNotContain("TryDispatchSATriggerEstablished", branch);
            Assert.DoesNotContain("outputfieldMapping", branch);

            // Stage3 は、判定を ButtonSpecialActionOutput に委ねる
            Assert.Contains("ButtonSpecialActionOutput.ResolveOutputActive(", text);
        }
    }
}
