using System;
using System.IO;
using System.Linq;
using System.Xml;
using DS4Windows;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase6-Step12（方針: 旧形式を正とし、プロファイル用 DTO［ProfileDTO］を削除して旧形式に統一する。Plan §0A.18）の安全網。
    /// 旧形式の <c>BackingStore.SaveProfile</c> が書くプロファイル XML に、ProfileDTO が持っていた設定の要素（162 要素）が
    /// すべて含まれることを固定する。ProfileDTO が持っていて旧形式が書いていなかった 10 要素（U-8 の原因）の再発も検出する。
    /// 要素名の一覧は、ProfileDTO の削除前に、そのソースから抜き出して固定したもの（読み取り専用の移行用の 13 要素 —
    /// Red／Green／Blue、LowRed ほか、ChargingRed ほか、ShiftModifier、UseSAforMouse、UseTPforControls、LastChecked — は除く）。
    /// 変更した Global 状態は必ず元に戻す。
    /// </summary>
    public class ProfileLegacyXmlCompletenessTests
    {
        private static readonly string[] ExpectedRootElements =
        {
            "BTPollRate", "ButtonMouseOffset", "ButtonMouseSensitivity", "ButtonMouseVerticalScale", "ChargingColor", "ChargingType",
            "Color", "Control", "DS4OutputTriggerMode", "DebouncingMs", "DinputOnly", "DualSenseControllerSettings", "FlashColor",
            "FlashType", "GyroControlsSettings", "GyroInvert", "GyroMouseDeadZone", "GyroMouseHAxis", "GyroMouseJitterCompensation",
            "GyroMouseMinThreshold", "GyroMouseSmoothingSettings", "GyroMouseStickAntiDeadX", "GyroMouseStickAntiDeadY",
            "GyroMouseStickDeadZone", "GyroMouseStickHAxis", "GyroMouseStickInvert", "GyroMouseStickJitterCompensation",
            "GyroMouseStickMaxOutput", "GyroMouseStickMaxOutputEnabled", "GyroMouseStickMaxZone", "GyroMouseStickOutputStick",
            "GyroMouseStickOutputStickAxes", "GyroMouseStickSmoothingSettings", "GyroMouseStickToggle", "GyroMouseStickTriggerCond",
            "GyroMouseStickTriggerTurns", "GyroMouseStickTriggers", "GyroMouseStickVerticalScale", "GyroMouseToggle",
            "GyroOutputMode", "GyroSensVerticalScale", "GyroSensitivity", "GyroSwipeSettings", "GyroTriggerTurns",
            "InverseRumbleMotors", "L2AntiDeadZone", "L2HipFireTime", "L2MaxOutput", "L2MaxZone", "L2OutputCurveCustom",
            "L2OutputCurveMode", "L2TriggerEffect", "L2TwoStageMode", "LSAntiDeadZone", "LSAntiSnapback", "LSAntiSnapbackDelta",
            "LSAntiSnapbackTimeout", "LSAxialDeadOptions", "LSDeadZone", "LSDeadZoneType", "LSDeltaAccelSettings", "LSFuzz",
            "LSMaxOutput", "LSMaxOutputForce", "LSMaxZone", "LSOuterBindDead", "LSOuterBindInvert", "LSOutputCurveCustom",
            "LSOutputCurveMode", "LSOutputMode", "LSOutputSettings", "LSRotation", "LSSquareStick", "LSVerticalScale",
            "LaunchProgram", "LeftStickDriftXAxis", "LeftStickDriftYAxis", "LeftTriggerMiddle", "LightbarMode", "LowColor",
            "MaxSatRainbow", "MouseAcceleration", "OutputContDevice", "ProfileActions", "R2AntiDeadZone", "R2HipFireTime",
            "R2MaxOutput", "R2MaxZone", "R2OutputCurveCustom", "R2OutputCurveMode", "R2TriggerEffect", "R2TwoStageMode",
            "RSAntiDeadZone", "RSAntiSnapback", "RSAntiSnapbackDelta", "RSAntiSnapbackTimeout", "RSAxialDeadOptions", "RSDeadZone",
            "RSDeadZoneType", "RSDeltaAccelSettings", "RSFuzz", "RSMaxOutput", "RSMaxOutputForce", "RSMaxZone", "RSOuterBindDead",
            "RSOuterBindInvert", "RSOutputCurveCustom", "RSOutputCurveMode", "RSOutputMode", "RSOutputSettings", "RSRotation",
            "RSSquareStick", "RSVerticalScale", "Rainbow", "RightStickDriftXAxis", "RightStickDriftYAxis", "RightTriggerMiddle",
            "RumbleAutostopTime", "RumbleBoost", "SASteeringWheelEmulationAxis", "SASteeringWheelEmulationRange",
            "SASteeringWheelFuzz", "SASteeringWheelSmoothingOptions", "SATriggerCond", "SATriggers", "SXAntiDeadZone", "SXDeadZone",
            "SXMaxZone", "SXOutputCurveCustom", "SXOutputCurveMode", "SZAntiDeadZone", "SZDeadZone", "SZMaxZone",
            "SZOutputCurveCustom", "SZOutputCurveMode", "Sensitivity", "ShiftControl", "SquareRStickRoundness",
            "SquareStickRoundness", "StartTouchpadOff", "TouchDisInvTriggers", "TouchRelMouseMinThreshold", "TouchRelMouseRotation",
            "TouchpadButtonMode", "TouchpadClickPassthru", "TouchpadInvert", "TouchpadMouseStick", "TouchpadOutputMode",
            "TrackballFriction", "TrackballMode", "UseDs3PitchRollSim", "doubleTap", "flashBatteryAt", "idleDisconnectTimeout",
            "ledAsBatteryIndicator", "lowerRCOn", "outputDataToDS4", "scrollSensitivity", "tapSensitivity", "touchSensitivity",
            "touchToggle", "touchpadJitterCompensation",
        };

        [Fact]
        public void SaveProfile_WritesEveryExpectedElement()
        {
            string originalPath = Global.appdatapath;
            string tempDir = Path.Combine(Path.GetTempPath(), "Phase6Step12_Completeness_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.Combine(tempDir, "Profiles"));
            try
            {
                Global.appdatapath = tempDir;
                var store = new BackingStore();

                Assert.True(TestRepositoryFactory.CreateLegacyProfileXml(store).SaveProfile(0, "Completeness"));

                var doc = new XmlDocument();
                doc.Load(Path.Combine(tempDir, "Profiles", "Completeness.xml"));
                string[] written = doc.DocumentElement.ChildNodes.Cast<XmlNode>()
                    .Where(n => n.NodeType == XmlNodeType.Element).Select(n => n.Name).Distinct().ToArray();

                string[] missing = ExpectedRootElements.Where(n => !written.Contains(n)).ToArray();
                Assert.True(missing.Length == 0,
                    "旧形式の SaveProfile が書いていない要素: " + string.Join(", ", missing));
            }
            finally
            {
                Global.appdatapath = originalPath;
                try { Directory.Delete(tempDir, true); } catch { }
            }
        }

        [Fact]
        public void SaveProfile_TouchpadMouseStick_WritesOutputStickAndAxesOnce()
        {
            // 上流（2022-10-20）以来、軸が <OutputStick> として 2 回書かれ、<OutputStickAxes> が無かった不具合の再発防止
            string originalPath = Global.appdatapath;
            string tempDir = Path.Combine(Path.GetTempPath(), "Phase6Step12_OutputStick_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.Combine(tempDir, "Profiles"));
            try
            {
                Global.appdatapath = tempDir;
                var store = new BackingStore();

                Assert.True(TestRepositoryFactory.CreateLegacyProfileXml(store).SaveProfile(0, "OutputStick"));

                var doc = new XmlDocument();
                doc.Load(Path.Combine(tempDir, "Profiles", "OutputStick.xml"));
                XmlNode group = doc.SelectSingleNode("/DS4Windows/TouchpadMouseStick");
                Assert.NotNull(group);
                Assert.Equal(1, group.SelectNodes("OutputStick").Count);
                Assert.Equal(1, group.SelectNodes("OutputStickAxes").Count);
            }
            finally
            {
                Global.appdatapath = originalPath;
                try { Directory.Delete(tempDir, true); } catch { }
            }
        }
    }
}
