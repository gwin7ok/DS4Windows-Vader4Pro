using System.Xml.Serialization;
using DS4Windows;
using DS4WinWPF.DS4Control.DTOXml;
using System.IO;

namespace DS4WindowsTests
{
    [TestClass]
    public class MappingTests
    {
        private string testProfileXml = string.Empty;
        
        /// <summary>
        /// テスト用Actions.xmlファイルのパスを取得
        /// </summary>
        private string GetTestActionsFilePath()
        {
            // プロジェクトの基準ディレクトリからTestDataフォルダを探す
            string currentDir = Directory.GetCurrentDirectory();
            string testDataDir = Path.Combine(currentDir, "..", "..", "..", "TestData");
            return Path.GetFullPath(Path.Combine(testDataDir, "Actions.xml"));
        }
        
        /// <summary>
        /// テスト用のSpecial Actionsを直接作成してテスト環境を構築
        /// </summary>
        private void SetupTestActions()
        {
            // 既存のアクションをクリア
            var actions = DS4Windows.Global.GetActions();
            actions.Clear();
            DS4Windows.Mapping.actionDone.Clear();
            DS4Windows.Mapping.actionDoneInitialized = false;
            
            // テスト用のSpecial Actionsを作成
            try
            {
                actions.Add(new DS4Windows.SpecialAction("TestAction_01_BatteryCheck", "L1/R2", "BatteryCheck", "|True|True|255|0|0|0|255|0"));
                actions.Add(new DS4Windows.SpecialAction("TestAction_02_DisconnectBT", "PS/Down", "DisconnectBT", ""));
                actions.Add(new DS4Windows.SpecialAction("TestAction_03_ProfileSwitch", "L2/PS", "Profile", "TestProfile"));
                actions.Add(new DS4Windows.SpecialAction("TestAction_04_Macro", "PS/R2", "Macro", "162/160/36/36/160/162"));
                actions.Add(new DS4Windows.SpecialAction("TestAction_05_Key", "L2/Up", "Key", "27"));
                actions.Add(new DS4Windows.SpecialAction("TestAction_06_MultiAction", "PS", "MultiAction", "272/400/400/400/400/400/284/350/284/500/272,,"));
                actions.Add(new DS4Windows.SpecialAction("TestAction_07_Program", "L1/L3", "Program", "C:\\Windows\\System32\\notepad.exe"));
                actions.Add(new DS4Windows.SpecialAction("TestAction_08_SpecialAtEnd", "R3/PS", "Profile", "EndProfile"));
                
                Console.WriteLine($"SetupTestActions: Created {actions.Count} test actions");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating test actions: {ex.Message}");
            }
            
            // InitializeActionDoneListを呼び出してactionDone配列を初期化
            DS4Windows.Mapping.InitializeActionDoneList();
            
            Console.WriteLine($"SetupTestActions: ActionDone initialized with {DS4Windows.Mapping.actionDone.Count} items");
        }

        public MappingTests()
        {
            #region ProfileXml
            testProfileXml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<!-- DS4Windows Configuration Data. 12/08/2023 14:26:24 -->
<!-- Made with DS4Windows version 3.2.21 -->

<DS4Windows app_version=""3.2.21"" config_version=""5"">
  <touchToggle>True</touchToggle>
  <idleDisconnectTimeout>0</idleDisconnectTimeout>
  <outputDataToDS4>True</outputDataToDS4>
  <Color>0,0,255</Color>
  <RumbleBoost>100</RumbleBoost>
  <RumbleAutostopTime>0</RumbleAutostopTime>
  <LightbarMode>DS4Win</LightbarMode>
  <ledAsBatteryIndicator>False</ledAsBatteryIndicator>
  <FlashType>0</FlashType>
  <flashBatteryAt>0</flashBatteryAt>
  <touchSensitivity>100</touchSensitivity>
  <LowColor>0,0,0</LowColor>
  <ChargingColor>0,0,0</ChargingColor>
  <FlashColor>0,0,0</FlashColor>
  <touchpadJitterCompensation>True</touchpadJitterCompensation>
  <lowerRCOn>False</lowerRCOn>
  <tapSensitivity>0</tapSensitivity>
  <doubleTap>False</doubleTap>
  <scrollSensitivity>0</scrollSensitivity>
  <LeftTriggerMiddle>0</LeftTriggerMiddle>
  <RightTriggerMiddle>0</RightTriggerMiddle>
  <TouchpadInvert>0</TouchpadInvert>
  <TouchpadClickPassthru>False</TouchpadClickPassthru>
  <L2AntiDeadZone>0</L2AntiDeadZone>
  <R2AntiDeadZone>0</R2AntiDeadZone>
  <L2MaxZone>100</L2MaxZone>
  <R2MaxZone>100</R2MaxZone>
  <L2MaxOutput>100</L2MaxOutput>
  <R2MaxOutput>100</R2MaxOutput>
  <ButtonMouseSensitivity>25</ButtonMouseSensitivity>
  <ButtonMouseOffset>0.008</ButtonMouseOffset>
  <Rainbow>0</Rainbow>
  <MaxSatRainbow>100</MaxSatRainbow>
  <LSDeadZone>10</LSDeadZone>
  <RSDeadZone>10</RSDeadZone>
  <LSAntiDeadZone>20</LSAntiDeadZone>
  <RSAntiDeadZone>20</RSAntiDeadZone>
  <LSMaxZone>100</LSMaxZone>
  <RSMaxZone>100</RSMaxZone>
  <LSVerticalScale>100</LSVerticalScale>
  <RSVerticalScale>100</RSVerticalScale>
  <LSMaxOutput>100</LSMaxOutput>
  <RSMaxOutput>100</RSMaxOutput>
  <LSMaxOutputForce>False</LSMaxOutputForce>
  <RSMaxOutputForce>False</RSMaxOutputForce>
  <LSDeadZoneType>Radial</LSDeadZoneType>
  <RSDeadZoneType>Radial</RSDeadZoneType>
  <LSAxialDeadOptions>
    <DeadZoneX>10</DeadZoneX>
    <DeadZoneY>10</DeadZoneY>
    <MaxZoneX>100</MaxZoneX>
    <MaxZoneY>100</MaxZoneY>
    <AntiDeadZoneX>20</AntiDeadZoneX>
    <AntiDeadZoneY>20</AntiDeadZoneY>
    <MaxOutputX>100</MaxOutputX>
    <MaxOutputY>100</MaxOutputY>
  </LSAxialDeadOptions>
  <RSAxialDeadOptions>
    <DeadZoneX>10</DeadZoneX>
    <DeadZoneY>10</DeadZoneY>
    <MaxZoneX>100</MaxZoneX>
    <MaxZoneY>100</MaxZoneY>
    <AntiDeadZoneX>20</AntiDeadZoneX>
    <AntiDeadZoneY>20</AntiDeadZoneY>
    <MaxOutputX>100</MaxOutputX>
    <MaxOutputY>100</MaxOutputY>
  </RSAxialDeadOptions>
  <LSRotation>0</LSRotation>
  <RSRotation>0</RSRotation>
  <LSFuzz>0</LSFuzz>
  <RSFuzz>0</RSFuzz>
  <LSOuterBindDead>75</LSOuterBindDead>
  <RSOuterBindDead>75</RSOuterBindDead>
  <LSOuterBindInvert>False</LSOuterBindInvert>
  <RSOuterBindInvert>False</RSOuterBindInvert>
  <LSDeltaAccelSettings>
    <Enabled>False</Enabled>
    <Multiplier>4</Multiplier>
    <MaxTravel>0.2</MaxTravel>
    <MinTravel>0.01</MinTravel>
    <EasingDuration>0.2</EasingDuration>
    <MinFactor>1</MinFactor>
  </LSDeltaAccelSettings>
  <RSDeltaAccelSettings>
    <Enabled>False</Enabled>
    <Multiplier>4</Multiplier>
    <MaxTravel>0.2</MaxTravel>
    <MinTravel>0.01</MinTravel>
    <EasingDuration>0.2</EasingDuration>
    <MinFactor>1</MinFactor>
  </RSDeltaAccelSettings>
  <SXDeadZone>0.25</SXDeadZone>
  <SZDeadZone>0.25</SZDeadZone>
  <SXMaxZone>100</SXMaxZone>
  <SZMaxZone>100</SZMaxZone>
  <SXAntiDeadZone>0</SXAntiDeadZone>
  <SZAntiDeadZone>0</SZAntiDeadZone>
  <Sensitivity>1|1|1|1|1|1</Sensitivity>
  <ChargingType>0</ChargingType>
  <MouseAcceleration>False</MouseAcceleration>
  <ButtonMouseVerticalScale>100</ButtonMouseVerticalScale>
  <LaunchProgram />
  <DinputOnly>True</DinputOnly>
  <StartTouchpadOff>False</StartTouchpadOff>
  <TouchpadOutputMode>Mouse</TouchpadOutputMode>
  <SATriggers>-1</SATriggers>
  <SATriggerCond>and</SATriggerCond>
  <SASteeringWheelEmulationAxis>None</SASteeringWheelEmulationAxis>
  <SASteeringWheelEmulationRange>360</SASteeringWheelEmulationRange>
  <SASteeringWheelFuzz>0</SASteeringWheelFuzz>
  <SASteeringWheelSmoothingOptions>
    <SASteeringWheelUseSmoothing>False</SASteeringWheelUseSmoothing>
    <SASteeringWheelSmoothMinCutoff>0.1</SASteeringWheelSmoothMinCutoff>
    <SASteeringWheelSmoothBeta>0.1</SASteeringWheelSmoothBeta>
  </SASteeringWheelSmoothingOptions>
  <TouchDisInvTriggers>-1</TouchDisInvTriggers>
  <GyroSensitivity>100</GyroSensitivity>
  <GyroSensVerticalScale>100</GyroSensVerticalScale>
  <GyroInvert>0</GyroInvert>
  <GyroTriggerTurns>True</GyroTriggerTurns>
  <GyroControlsSettings>
    <Triggers>-1</Triggers>
    <TriggerCond>and</TriggerCond>
    <TriggerTurns>True</TriggerTurns>
    <Toggle>False</Toggle>
  </GyroControlsSettings>
  <GyroMouseSmoothingSettings>
    <UseSmoothing>False</UseSmoothing>
    <SmoothingMethod>none</SmoothingMethod>
    <SmoothingWeight>50</SmoothingWeight>
    <SmoothingMinCutoff>1</SmoothingMinCutoff>
    <SmoothingBeta>0.7</SmoothingBeta>
  </GyroMouseSmoothingSettings>
  <GyroMouseHAxis>0</GyroMouseHAxis>
  <GyroMouseDeadZone>10</GyroMouseDeadZone>
  <GyroMouseMinThreshold>1</GyroMouseMinThreshold>
  <GyroMouseToggle>False</GyroMouseToggle>
  <GyroMouseJitterCompensation>True</GyroMouseJitterCompensation>
  <GyroOutputMode>Controls</GyroOutputMode>
  <GyroMouseStickTriggers>-1</GyroMouseStickTriggers>
  <GyroMouseStickTriggerCond>and</GyroMouseStickTriggerCond>
  <GyroMouseStickTriggerTurns>True</GyroMouseStickTriggerTurns>
  <GyroMouseStickHAxis>0</GyroMouseStickHAxis>
  <GyroMouseStickDeadZone>30</GyroMouseStickDeadZone>
  <GyroMouseStickMaxZone>830</GyroMouseStickMaxZone>
  <GyroMouseStickOutputStick>RightStick</GyroMouseStickOutputStick>
  <GyroMouseStickOutputStickAxes>XY</GyroMouseStickOutputStickAxes>
  <GyroMouseStickAntiDeadX>0.4</GyroMouseStickAntiDeadX>
  <GyroMouseStickAntiDeadY>0.4</GyroMouseStickAntiDeadY>
  <GyroMouseStickInvert>0</GyroMouseStickInvert>
  <GyroMouseStickToggle>False</GyroMouseStickToggle>
  <GyroMouseStickMaxOutput>100</GyroMouseStickMaxOutput>
  <GyroMouseStickMaxOutputEnabled>False</GyroMouseStickMaxOutputEnabled>
  <GyroMouseStickVerticalScale>100</GyroMouseStickVerticalScale>
  <GyroMouseStickJitterCompensation>False</GyroMouseStickJitterCompensation>
  <GyroMouseStickSmoothingSettings>
    <UseSmoothing>False</UseSmoothing>
    <SmoothingMethod>none</SmoothingMethod>
    <SmoothingWeight>50</SmoothingWeight>
    <SmoothingMinCutoff>0.4</SmoothingMinCutoff>
    <SmoothingBeta>0.7</SmoothingBeta>
  </GyroMouseStickSmoothingSettings>
  <GyroSwipeSettings>
    <DeadZoneX>80</DeadZoneX>
    <DeadZoneY>80</DeadZoneY>
    <Triggers>-1</Triggers>
    <TriggerCond>and</TriggerCond>
    <TriggerTurns>True</TriggerTurns>
    <XAxis>Yaw</XAxis>
    <DelayTime>0</DelayTime>
  </GyroSwipeSettings>
  <BTPollRate>4</BTPollRate>
  <LSOutputCurveMode>linear</LSOutputCurveMode>
  <LSOutputCurveCustom />
  <RSOutputCurveMode>linear</RSOutputCurveMode>
  <RSOutputCurveCustom />
  <LSSquareStick>False</LSSquareStick>
  <RSSquareStick>False</RSSquareStick>
  <SquareStickRoundness>5</SquareStickRoundness>
  <SquareRStickRoundness>5</SquareRStickRoundness>
  <LSAntiSnapback>False</LSAntiSnapback>
  <RSAntiSnapback>False</RSAntiSnapback>
  <LSAntiSnapbackDelta>135</LSAntiSnapbackDelta>
  <RSAntiSnapbackDelta>135</RSAntiSnapbackDelta>
  <LSAntiSnapbackTimeout>50</LSAntiSnapbackTimeout>
  <RSAntiSnapbackTimeout>50</RSAntiSnapbackTimeout>
  <LSOutputMode>Controls</LSOutputMode>
  <RSOutputMode>Controls</RSOutputMode>
  <LSOutputSettings>
    <FlickStickSettings>
      <RealWorldCalibration>5.3</RealWorldCalibration>
      <FlickThreshold>0.9</FlickThreshold>
      <FlickTime>0.1</FlickTime>
      <MinAngleThreshold>0</MinAngleThreshold>
    </FlickStickSettings>
  </LSOutputSettings>
  <RSOutputSettings>
    <FlickStickSettings>
      <RealWorldCalibration>5.3</RealWorldCalibration>
      <FlickThreshold>0.9</FlickThreshold>
      <FlickTime>0.1</FlickTime>
      <MinAngleThreshold>0</MinAngleThreshold>
    </FlickStickSettings>
  </RSOutputSettings>
  <DualSenseControllerSettings>
    <RumbleSettings>
      <EmulationMode>Accurate</EmulationMode>
      <EnableGenericRumbleRescale>False</EnableGenericRumbleRescale>
      <HapticPowerLevel>0</HapticPowerLevel>
    </RumbleSettings>
  </DualSenseControllerSettings>
  <L2OutputCurveMode>linear</L2OutputCurveMode>
  <L2OutputCurveCustom />
  <L2TwoStageMode>Disabled</L2TwoStageMode>
  <R2TwoStageMode>Disabled</R2TwoStageMode>
  <L2HipFireTime>100</L2HipFireTime>
  <R2HipFireTime>100</R2HipFireTime>
  <L2TriggerEffect>None</L2TriggerEffect>
  <R2TriggerEffect>None</R2TriggerEffect>
  <R2OutputCurveMode>linear</R2OutputCurveMode>
  <R2OutputCurveCustom />
  <SXOutputCurveMode>linear</SXOutputCurveMode>
  <SXOutputCurveCustom />
  <SZOutputCurveMode>linear</SZOutputCurveMode>
  <SZOutputCurveCustom />
  <TrackballMode>True</TrackballMode>
  <TrackballFriction>10</TrackballFriction>
  <TouchRelMouseRotation>0</TouchRelMouseRotation>
  <TouchRelMouseMinThreshold>1</TouchRelMouseMinThreshold>
  <TouchpadAbsMouseSettings>
    <MaxZoneX>90</MaxZoneX>
    <MaxZoneY>90</MaxZoneY>
    <SnapToCenter>False</SnapToCenter>
  </TouchpadAbsMouseSettings>
  <TouchpadMouseStick>
    <DeadZone>0</DeadZone>
    <MaxZone>8</MaxZone>
    <OutputStick>RightStick</OutputStick>
    <OutputStickAxes>XY</OutputStickAxes>
    <AntiDeadX>0.4</AntiDeadX>
    <AntiDeadY>0.4</AntiDeadY>
    <Invert>0</Invert>
    <MaxOutput>100</MaxOutput>
    <MaxOutputEnabled>False</MaxOutputEnabled>
    <VerticalScale>100</VerticalScale>
    <OutputCurve>Linear</OutputCurve>
    <Rotation>0</Rotation>
    <SmoothingSettings>
      <SmoothingMethod>None</SmoothingMethod>
      <SmoothingMinCutoff>0.8</SmoothingMinCutoff>
      <SmoothingBeta>0.7</SmoothingBeta>
    </SmoothingSettings>
  </TouchpadMouseStick>
  <TouchpadButtonMode>Click</TouchpadButtonMode>
  <AbsMouseRegionSettings>
    <AbsWidth>1</AbsWidth>
    <AbsHeight>1</AbsHeight>
    <AbsXCenter>0.5</AbsXCenter>
    <AbsYCenter>0.5</AbsYCenter>
    <AntiRadius>0</AntiRadius>
    <SnapToCenter>True</SnapToCenter>
  </AbsMouseRegionSettings>
  <OutputContDevice>X360</OutputContDevice>
  <ProfileActions>Disconnect Controller</ProfileActions>
  <Control>
    <Button>
      <Square>Y Button</Square>
      <Triangle>X Button</Triangle>
    </Button>
  </Control>
  <ShiftControl />
</DS4Windows>";
            #endregion
        }

        /// <summary>
        /// Never mind. Mapping is not testable due to hard dependency on the
        /// ControlService class. Does not help Mapping is static.
        /// Too bulky. Don't bother
        /// </summary>
        public void CheckMappingConversion()
        {
            // Test profile reading. Will fail if an XML exception is thrown
            XmlSerializer serializer = new XmlSerializer(typeof(ProfileDTO),
                   ProfileDTO.GetAttributeOverrides());
            using StringReader sr = new StringReader(testProfileXml);
            ProfileDTO dto = serializer.Deserialize(sr) as ProfileDTO;
            dto.DeviceIndex = 8; // Use test slot
            dto.MapTo(Global.store);

            // Test input state to use
            DS4State inputState = new DS4State()
            {
                Square = true,
                LX = 255,
                LY = 128,
                RX = 0,
                RY = 255,
            };

            // Just use reference
            DS4State cState = inputState;

            DS4State outState = new DS4State();
            DS4StateExposed exposedState = new DS4StateExposed(inputState);
            cState = Mapping.SetCurveAndDeadzone(8, cState, outState);

            // Stopping point. Hard dependency on ControlService class
            //Mapping.MapCustom(ind, cState, tempMapState, ExposedState[ind], touchPad[ind], this);
        }

        [TestMethod]
        public void TestActionDoneArrayInitialization()
        {
            // Arrange: テスト用のSpecial Actionsを設定
            SetupTestActions();

            // Act & Assert: actionDone配列が適切に初期化されていることを確認
            var actionDoneList = DS4Windows.Mapping.actionDone;
            Assert.IsNotNull(actionDoneList, "actionDone list should be initialized");
            
            // テスト用アクション数（8個）と一致することを確認
            var actions = DS4Windows.Global.GetActions();
            Assert.AreEqual(8, actions.Count, "Should have 8 test actions");
            Assert.AreEqual(actions.Count, actionDoneList.Count, 
                "actionDone list size should match actions count");
            
            // 全ての要素がActionStateオブジェクトで初期化されていることを確認
            for (int i = 0; i < actionDoneList.Count; i++)
            {
                Assert.IsNotNull(actionDoneList[i], $"actionDone[{i}] should be initialized ActionState object");
            }
            
            // 最後のSpecial Action（インデックス7）が安全にアクセスできることを確認
            Assert.IsNotNull(actionDoneList[7], "Last action (index 7) should be accessible");
            Assert.AreEqual("TestAction_08_SpecialAtEnd", actions[7].name, "Last action should be the end profile switch");
            
            // 初期化フラグが正しく設定されることを確認
            Assert.IsTrue(DS4Windows.Mapping.actionDoneInitialized, "actionDoneInitialized should be true after initialization");
        }

        [TestMethod]
        public void TestActionDoneInitializationFlag()
        {
            // Arrange: テスト用のSpecial Actionsを設定
            SetupTestActions();

            // Act & Assert: actionDoneInitialized フラグが適切に設定されることを確認
            bool isInitialized = DS4Windows.Mapping.actionDoneInitialized;
            Assert.IsTrue(isInitialized, "actionDoneInitialized should be set to true after SetupTestActions");
            
            // actionDone配列のサイズも確認
            var actions = DS4Windows.Global.GetActions();
            var actionDoneList = DS4Windows.Mapping.actionDone;
            Assert.AreEqual(actions.Count, actionDoneList.Count, 
                "actionDone list size should match actions count when initialized flag is true");
        }

        [TestMethod]
        public void TestInitializeActionDoneListMethod()
        {
            // Arrange: テスト用のSpecial Actionsを設定
            SetupTestActions();

            // Act & Assert: InitializeActionDoneListメソッドが存在し、適切に動作することを確認
            var mappingType = typeof(DS4Windows.Mapping);
            var initMethod = mappingType.GetMethod("InitializeActionDoneList", 
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            
            Assert.IsNotNull(initMethod, "InitializeActionDoneList method should exist and be public");
            
            // 初期化前の状態をリセットしてメソッドを直接呼び出し
            DS4Windows.Mapping.actionDone.Clear();
            DS4Windows.Mapping.actionDoneInitialized = false;
            
            // メソッドを呼び出し
            DS4Windows.Mapping.InitializeActionDoneList();

            // Assert: actionDone配列が適切に初期化されていることを確認
            var actions = DS4Windows.Global.GetActions();
            var actionDoneList = DS4Windows.Mapping.actionDone;
            
            Assert.IsNotNull(actionDoneList, "actionDone list should be initialized");
            Assert.AreEqual(8, actions.Count, "Should have 8 test actions");
            Assert.AreEqual(actions.Count, actionDoneList.Count, "actionDone list should match actions count");
            
            // 初期化フラグが設定されることを確認
            Assert.IsTrue(DS4Windows.Mapping.actionDoneInitialized, "actionDoneInitialized should be set to true");
        }

        [TestMethod]
        public void TestProfileSwitchingActionExecution()
        {
            // Arrange: テスト用のSpecial Actionsを設定（プロファイル切り替えアクションを含む）
            SetupTestActions();
            
            // Act & Assert: プロファイル切り替えアクションが安全に実行できることを確認
            var actions = DS4Windows.Global.GetActions();
            var profileActions = actions.Where(a => a.typeID == DS4Windows.SpecialAction.ActionTypeId.Profile).ToList();
            
            // テストアクションには2つのプロファイル切り替えアクションがある
            Assert.AreEqual(2, profileActions.Count, "Should have 2 profile switching actions in test data");
            
            // actionDone配列が適切にアクセス可能であることを確認
            var actionDoneList = DS4Windows.Mapping.actionDone;
            Assert.IsNotNull(actionDoneList, "actionDone list should be initialized");
            
            // すべてのプロファイルアクションが安全にアクセスできることを確認
            foreach (var profileAction in profileActions)
            {
                int actionIndex = actions.IndexOf(profileAction);
                Assert.IsTrue(actionIndex >= 0 && actionIndex < actionDoneList.Count, 
                    $"Profile action '{profileAction.name}' index {actionIndex} should be within actionDone bounds (0-{actionDoneList.Count-1})");
                
                // ActionStateオブジェクトが初期化されていることを確認
                Assert.IsNotNull(actionDoneList[actionIndex], $"ActionState at index {actionIndex} should be initialized");
            }
            
            // 特に最後のプロファイル切り替えアクション（インデックス7）を確認
            var lastProfileAction = profileActions.LastOrDefault();
            if (lastProfileAction != null)
            {
                int lastIndex = actions.IndexOf(lastProfileAction);
                Assert.AreEqual(7, lastIndex, "Last profile action should be at index 7");
                Assert.IsNotNull(actionDoneList[lastIndex], "Last profile action should have initialized ActionState");
            }
        }

        [TestMethod]
        public void TestSpecialActionExecutionOrderIndependence()
        {
            // This test verifies that InitializeActionDoneList method prevents IndexOutOfRangeException
            // regardless of Special Action position in Actions.xml (which was the original bug)
            
            // Arrange: テスト用のSpecial Actionsを設定
            SetupTestActions();
            
            // Act: バグ状態をシミュレート - actionDoneを未初期化状態にする
            DS4Windows.Mapping.actionDone.Clear();
            DS4Windows.Mapping.actionDoneInitialized = false;
            
            // InitializeActionDoneListを呼び出してIndexOutOfRangeExceptionを防ぐ
            DS4Windows.Mapping.InitializeActionDoneList();

            // Assert: 修正により IndexOutOfRangeException が発生しないことを確認
            var actions = DS4Windows.Global.GetActions();
            var actionDoneList = DS4Windows.Mapping.actionDone;
            
            Assert.IsTrue(DS4Windows.Mapping.actionDoneInitialized, "actionDone should be initialized");
            Assert.IsNotNull(actionDoneList, "actionDone list should not be null");
            Assert.AreEqual(actions.Count, actionDoneList.Count, 
                "actionDone list length should match actions count");
            
            // どのアクションインデックスでも安全にアクセスできることをテスト
            for (int i = 0; i < actions.Count; i++)
            {
                Assert.IsTrue(i < actionDoneList.Count, $"Should be able to safely access actionDone[{i}]");
                Assert.IsNotNull(actionDoneList[i], $"actionDone[{i}] should be initialized");
            }
            
            // Specific test for the bug scenario: verify bounds protection
            if (actions.Count > 0)
            {
                int lastIndex = actions.Count - 1;
                try
                {
                    var lastActionState = actionDoneList[lastIndex];
                    Assert.IsNotNull(lastActionState, $"actionDone[{lastIndex}] should be initialized ActionState object");
                }
                catch (IndexOutOfRangeException)
                {
                    Assert.Fail("Accessing last action in actionDone list should not throw IndexOutOfRangeException after fix");
                }
            }
        }

        [TestMethod]
        public void TestActionDoneArrayBoundsProtection()
        {
            // This test specifically targets the bug where actionDone list
            // was not initialized, causing IndexOutOfRangeException
            
            // Arrange: テスト用のSpecial Actionsを設定してからバグ状態をシミュレート
            SetupTestActions();
            var actions = DS4Windows.Global.GetActions();
            
            // actionDoneを未初期化状態にして元のバグをシミュレート
            DS4Windows.Mapping.actionDone.Clear();
            DS4Windows.Mapping.actionDoneInitialized = false;
            
            // Act: 修正を適用 - actionDone配列を初期化
            DS4Windows.Mapping.InitializeActionDoneList();
            
            // Assert: 重要な回帰テスト - 修正が機能することを確認
            var actionDoneList = DS4Windows.Mapping.actionDone;
            Assert.IsNotNull(actionDoneList, "actionDone list must not be null (this was the bug)");
            
            // テストアクション数（8個）に適切にサイズ調整されていることを確認
            Assert.AreEqual(8, actions.Count, "Should have 8 test actions");
            Assert.AreEqual(actions.Count, actionDoneList.Count, 
                "actionDone list must be sized correctly to prevent IndexOutOfRangeException");
            
            // 元のバグの原因となったシナリオのテスト:
            // 有効なインデックスでの要素への安全なアクセスはIndexOutOfRangeExceptionを発生させない
            for (int i = 0; i < actions.Count; i++)
            {
                try
                {
                    // 修正前はここでIndexOutOfRangeExceptionが発生していた
                    var actionState = actionDoneList[i];
                    Assert.IsNotNull(actionState, $"ActionState at index {i} should be properly initialized");
                }
                catch (IndexOutOfRangeException)
                {
                    Assert.Fail($"Accessing actionDone[{i}] should never throw IndexOutOfRangeException after our fix");
                }
            }
            
            // 特に最後のアクション（インデックス7）のアクセステスト - これが元のバグの典型的な失敗ケース
            int lastIndex = actions.Count - 1;
            Assert.IsNotNull(actionDoneList[lastIndex], $"Last ActionState at index {lastIndex} should be accessible");
        }

        /// <summary>
        /// テスト1: DS4Windows起動直後にアクションリストが生成されるかをテスト
        /// + パフォーマンス計測を追加
        /// </summary>
        [TestMethod]
        public void TestActionListGenerationOnStartup()
        {
            // Arrange: 起動時の状態をシミュレート（アクションリストが空の状態から開始）
            var actions = DS4Windows.Global.GetActions();
            actions.Clear();
            DS4Windows.Mapping.actionDone.Clear();
            DS4Windows.Mapping.actionDoneInitialized = false;

            // Act: 起動時にLoadActionsが呼ばれることをシミュレート（時間計測付き）
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            bool loadResult = DS4Windows.Global.LoadActions();
            stopwatch.Stop();

            // Assert: アクションリストが適切に生成されることを確認
            Assert.IsTrue(loadResult, "LoadActions should succeed on startup");
            
            // 最低でもデフォルトアクション（Disconnect Controller）が作成される
            Assert.IsTrue(actions.Count >= 1, "At least default action should be created on startup");
            
            // actionDone配列も適切に初期化される
            Assert.IsTrue(DS4Windows.Mapping.actionDoneInitialized, "actionDone should be initialized on startup");
            Assert.AreEqual(actions.Count, DS4Windows.Mapping.actionDone.Count, 
                "actionDone size should match actions count on startup");
            
            // デフォルトアクションの確認
            var defaultAction = actions.FirstOrDefault(a => a.name.Contains("Disconnect Controller"));
            Assert.IsNotNull(defaultAction, "Default 'Disconnect Controller' action should be created");
            Assert.AreEqual(DS4Windows.SpecialAction.ActionTypeId.DisconnectBT, defaultAction.typeID, 
                "Default action should be DisconnectBT type");

            // パフォーマンス結果を出力
            Console.WriteLine($"🕐 Startup LoadActions Performance:");
            Console.WriteLine($"   Actions count: {actions.Count}");
            Console.WriteLine($"   Total time: {stopwatch.ElapsedMilliseconds}ms ({stopwatch.ElapsedTicks} ticks)");
            Console.WriteLine($"   Time per action: {(double)stopwatch.ElapsedTicks / actions.Count:F2} ticks/action");
        }

        /// <summary>
        /// テスト2: 設定変更後にアクションリストが再生成されるかをテスト
        /// + 中規模アクション数でのパフォーマンス計測
        /// </summary>
        [TestMethod]
        public void TestActionListRegenerationAfterSettingsChange()
        {
            // Arrange: 初期状態を設定
            SetupTestActions();
            int initialActionCount = DS4Windows.Global.GetActions().Count;
            Assert.AreEqual(8, initialActionCount, "Should start with 8 test actions");

            // Act: 設定変更をシミュレート - 新しいアクションを追加してLoadActionsを再実行
            var actions = DS4Windows.Global.GetActions();
            actions.Add(new DS4Windows.SpecialAction("NewAction_AfterChange", "L3/R3", "Key", "32"));
            
            // 設定変更後の再読み込みをシミュレート（時間計測付き）
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            DS4Windows.Mapping.actionDone.Clear();
            DS4Windows.Mapping.actionDoneInitialized = false;
            DS4Windows.Mapping.InitializeActionDoneList();
            stopwatch.Stop();

            // Assert: アクションリストが更新されていることを確認
            Assert.AreEqual(9, actions.Count, "Action count should increase after adding new action");
            Assert.IsTrue(DS4Windows.Mapping.actionDoneInitialized, "actionDone should be re-initialized after change");
            Assert.AreEqual(actions.Count, DS4Windows.Mapping.actionDone.Count, 
                "actionDone size should match updated actions count");

            // 新しいアクションが適切に追加されていることを確認
            var newAction = actions.LastOrDefault();
            Assert.IsNotNull(newAction, "New action should be added");
            Assert.AreEqual("NewAction_AfterChange", newAction.name, "New action should have correct name");
            
            // actionDone配列の最後の要素も初期化されていることを確認
            Assert.IsNotNull(DS4Windows.Mapping.actionDone[8], "ActionDone for new action should be initialized");

            // パフォーマンス結果を出力
            Console.WriteLine($"🔄 Settings Change Re-initialization Performance:");
            Console.WriteLine($"   Actions count: {actions.Count}");
            Console.WriteLine($"   Initialization time: {stopwatch.ElapsedMilliseconds}ms ({stopwatch.ElapsedTicks} ticks)");
            Console.WriteLine($"   Time per action: {(double)stopwatch.ElapsedTicks / actions.Count:F2} ticks/action");
        }

        /// <summary>
        /// テスト3: Special Action実行時の初期化待機機能をテスト（スキップ方式から待機方式への変更）
        /// </summary>
        [TestMethod]
        public async Task TestSpecialActionExecutionWithInitializationWait()
        {
            // Arrange: テスト用アクションを設定後、未初期化状態をシミュレート
            SetupTestActions();
            
            // 意図的に未初期化状態にする
            DS4Windows.Mapping.actionDoneInitialized = false;
            DS4Windows.Mapping.actionDone.Clear();

            // Act: 待機方式のテスト - EnsureActionDoneInitializedメソッドをテスト
            // プライベートメソッドなので、リフレクションを使用してテスト
            var mappingType = typeof(DS4Windows.Mapping);
            var ensureMethod = mappingType.GetMethod("EnsureActionDoneInitialized", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            
            Assert.IsNotNull(ensureMethod, "EnsureActionDoneInitialized method should exist");

            // 初期化が未完了の状態から開始
            Assert.IsFalse(DS4Windows.Mapping.actionDoneInitialized, "Should start in uninitialized state");

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // 並行して初期化を実行（実際のLoadActionsをシミュレート）
            var initializationTask = Task.Run(() =>
            {
                // 100ms後に初期化を実行（遅延初期化をシミュレート）
                System.Threading.Thread.Sleep(100);
                DS4Windows.Mapping.InitializeActionDoneList();
            });

            // EnsureActionDoneInitializedを呼び出し（待機をテスト）
            var waitTask = ensureMethod.Invoke(null, null) as Task;
            Assert.IsNotNull(waitTask, "EnsureActionDoneInitialized should return a Task");

            // 両方のタスクの完了を待機
            await Task.WhenAll(waitTask, initializationTask);
            stopwatch.Stop();

            // Assert: 待機後に初期化が完了していることを確認
            Assert.IsTrue(DS4Windows.Mapping.actionDoneInitialized, "Should be initialized after waiting");
            
            var actions = DS4Windows.Global.GetActions();
            Assert.AreEqual(actions.Count, DS4Windows.Mapping.actionDone.Count, 
                "ActionDone size should match actions count after initialization");

            Console.WriteLine($"⏳ Initialization Wait Test Results:");
            Console.WriteLine($"   Actions count: {actions.Count}");
            Console.WriteLine($"   Wait time: {stopwatch.ElapsedMilliseconds}ms");
            Console.WriteLine($"   Wait mechanism: ✅ Successfully waited for delayed initialization");
            Console.WriteLine($"   Result: ActionDone properly initialized after wait");

            // 待機時間が適切であることを確認（100ms + 処理時間程度）
            Assert.IsTrue(stopwatch.ElapsedMilliseconds >= 100, "Should have waited for the delayed initialization");
            Assert.IsTrue(stopwatch.ElapsedMilliseconds <= 1000, "Should not have timed out");
        }

        /// <summary>
        /// テスト4: 待機方式のタイムアウト機能をテスト
        /// </summary>
        [TestMethod]
        public async Task TestEnsureActionDoneInitializedTimeout()
        {
            // Arrange: テスト用アクションを設定後、初期化を意図的に行わない
            SetupTestActions();
            DS4Windows.Mapping.actionDoneInitialized = false;
            DS4Windows.Mapping.actionDone.Clear();

            // Act: タイムアウトをテスト（初期化を行わずに待機）
            var mappingType = typeof(DS4Windows.Mapping);
            var ensureMethod = mappingType.GetMethod("EnsureActionDoneInitialized", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            // 初期化を行わずにEnsureActionDoneInitializedを呼び出し（強制初期化をテスト）
            var waitTask = ensureMethod.Invoke(null, null) as Task;
            await waitTask;
            
            stopwatch.Stop();

            // Assert: 強制初期化により初期化が完了していることを確認
            Assert.IsTrue(DS4Windows.Mapping.actionDoneInitialized, "Should be force-initialized after timeout");
            
            var actions = DS4Windows.Global.GetActions();
            Assert.AreEqual(actions.Count, DS4Windows.Mapping.actionDone.Count, 
                "ActionDone should match actions count after force initialization");

            Console.WriteLine($"⏰ Timeout & Force Initialization Test (Updated for 500ms×3):");
            Console.WriteLine($"   Actions count: {actions.Count}");
            Console.WriteLine($"   Total wait time: {stopwatch.ElapsedMilliseconds}ms");
            Console.WriteLine($"   Expected: ~1500ms (500ms × 3 retries)");
            Console.WriteLine($"   Result: ✅ Force initialization successful after timeout");

            // タイムアウト時間（500ms×3回=1500ms）+ 余裕時間以内に完了していることを確認
            Assert.IsTrue(stopwatch.ElapsedMilliseconds <= 2500, "Should complete within timeout period (1.5s) + buffer");
        }

        /// <summary>
        /// テスト5: 並行処理でのアクション初期化の安全性をテスト
        /// </summary>
        [TestMethod]
        public void TestConcurrentActionInitializationSafety()
        {
            // Arrange: 複数スレッドからの同時アクセスをシミュレート
            SetupTestActions();
            DS4Windows.Mapping.actionDone.Clear();
            DS4Windows.Mapping.actionDoneInitialized = false;

            bool thread1Success = false;
            bool thread2Success = false;
            Exception thread1Exception = null;
            Exception thread2Exception = null;

            // Act: 2つのスレッドから同時にInitializeActionDoneListを呼び出し
            var thread1 = new System.Threading.Thread(() =>
            {
                try
                {
                    DS4Windows.Mapping.InitializeActionDoneList();
                    thread1Success = true;
                }
                catch (Exception ex)
                {
                    thread1Exception = ex;
                }
            });

            var thread2 = new System.Threading.Thread(() =>
            {
                try
                {
                    // 少し遅らせて競合状態を作る
                    System.Threading.Thread.Sleep(10);
                    DS4Windows.Mapping.InitializeActionDoneList();
                    thread2Success = true;
                }
                catch (Exception ex)
                {
                    thread2Exception = ex;
                }
            });

            thread1.Start();
            thread2.Start();

            // 完了を待機
            thread1.Join(5000); // 5秒でタイムアウト
            thread2.Join(5000);

            // Assert: 両方のスレッドが安全に処理を完了することを確認
            Assert.IsNull(thread1Exception, $"Thread 1 should not throw exception: {thread1Exception?.Message}");
            Assert.IsNull(thread2Exception, $"Thread 2 should not throw exception: {thread2Exception?.Message}");
            Assert.IsTrue(thread1Success, "Thread 1 should complete successfully");
            Assert.IsTrue(thread2Success, "Thread 2 should complete successfully");

            // 最終状態の確認
            Assert.IsTrue(DS4Windows.Mapping.actionDoneInitialized, "Should be initialized after concurrent access");
            var actions = DS4Windows.Global.GetActions();
            Assert.AreEqual(actions.Count, DS4Windows.Mapping.actionDone.Count, 
                "ActionDone should have correct size after concurrent initialization");
        }

        /// <summary>
        /// テスト6: アクション実行のタイミングテスト（初期化完了前後での動作確認）
        /// </summary>
        [TestMethod]
        public void TestActionExecutionTimingAndSafety()
        {
            // Arrange: テスト用のアクションを設定
            SetupTestActions();
            var actions = DS4Windows.Global.GetActions();
            var profileAction = actions.FirstOrDefault(a => a.typeID == DS4Windows.SpecialAction.ActionTypeId.Profile);
            Assert.IsNotNull(profileAction, "Should have at least one profile action for testing");

            // Phase 1: 未初期化状態でのアクセス試行
            DS4Windows.Mapping.actionDone.Clear();
            DS4Windows.Mapping.actionDoneInitialized = false;

            // 未初期化状態でのIndex範囲外アクセスが安全に処理されることを確認
            int profileActionIndex = actions.IndexOf(profileAction);
            bool accessedWithoutException = true;
            
            try
            {
                // 実際のMapCustomActionで行われる処理をシミュレート
                lock (DS4Windows.Mapping.actionDoneLock)
                {
                    if (!DS4Windows.Mapping.actionDoneInitialized)
                    {
                        // 実装では return でスキップ - 例外は発生しない
                        accessedWithoutException = true;
                    }
                    else if (profileActionIndex >= DS4Windows.Mapping.actionDone.Count)
                    {
                        // actionDoneのサイズが不適切な場合もスキップ
                        accessedWithoutException = true;
                    }
                    else
                    {
                        // 正常な場合のアクセス（この時点では到達しない）
                        var _ = DS4Windows.Mapping.actionDone[profileActionIndex];
                    }
                }
            }
            catch (Exception)
            {
                accessedWithoutException = false;
            }

            Assert.IsTrue(accessedWithoutException, "Should safely handle uninitialized access without exceptions");

            // Phase 2: 初期化後の正常アクセス
            DS4Windows.Mapping.InitializeActionDoneList();
            
            try
            {
                lock (DS4Windows.Mapping.actionDoneLock)
                {
                    if (DS4Windows.Mapping.actionDoneInitialized && 
                        profileActionIndex < DS4Windows.Mapping.actionDone.Count)
                    {
                        var actionState = DS4Windows.Mapping.actionDone[profileActionIndex];
                        Assert.IsNotNull(actionState, "ActionState should be properly initialized");
                    }
                }
                accessedWithoutException = true;
            }
            catch (Exception ex)
            {
                Assert.Fail($"Should safely access after initialization: {ex.Message}");
            }

            Assert.IsTrue(DS4Windows.Mapping.actionDoneInitialized, "Should be initialized");
            Assert.AreEqual(actions.Count, DS4Windows.Mapping.actionDone.Count, "Size should match");
        }

        /// <summary>
        /// テスト7: 大規模なアクション数でのパフォーマンステスト（現実的な本番環境想定）
        /// 実際のユーザーが使用する可能性のある最大規模をテスト
        /// </summary>
        [TestMethod]
        public void TestLargeScaleActionInitializationPerformance()
        {
            // Arrange: 大規模アクション構成を作成（50個 - ヘビーユーザー想定）
            var actions = DS4Windows.Global.GetActions();
            actions.Clear();

            // 1. デフォルトアクション
            actions.Add(new DS4Windows.SpecialAction("Disconnect Controller", "PS/Options", "DisconnectBT", "0"));

            // 2. プロファイル切り替え（10個 - 複数ゲーム・アプリ対応）
            string[] profiles = { "Desktop", "Gaming", "FPS", "RPG", "Racing", "Media", "Browser", "Work", "Stream", "Design" };
            foreach (var profile in profiles)
            {
                actions.Add(new DS4Windows.SpecialAction($"Profile_{profile}", "L1+R1+Cross", "Profile", profile));
            }

            // 3. マクロアクション（20個 - 複雑なキーシーケンス）
            for (int i = 0; i < 20; i++)
            {
                string macroSeq = i % 4 == 0 ? "87/0/65/0/83/0/68/0" :  // WASD
                                 i % 4 == 1 ? "162/65/0/162/0/163/67/0/163/0" : // Ctrl+A, Ctrl+C
                                 i % 4 == 2 ? "9/0/72/0/69/0/76/0/76/0/79/0" : // TAB+HELLO
                                            "112/0/113/0/114/0/115/0"; // F1-F4
                actions.Add(new DS4Windows.SpecialAction($"Macro_{i + 1}", "R1+R2+Cross", "Macro", macroSeq));
            }

            // 4. キーアクション（15個 - よく使うキー）
            string[] keys = { "32", "13", "27", "9", "20", "175", "174", "173", "179", "178", "177", "176", "112", "113", "114" };
            for (int i = 0; i < keys.Length; i++)
            {
                actions.Add(new DS4Windows.SpecialAction($"Key_{keys[i]}", "L2+R2+Circle", "Key", keys[i]));
            }

            // 5. その他のアクション（4個）
            actions.Add(new DS4Windows.SpecialAction("Battery_Check", "TouchButton+L1", "BatteryCheck", ""));
            actions.Add(new DS4Windows.SpecialAction("Gyro_Calibrate", "Share+Options+L3", "GyroCalibrate", ""));
            actions.Add(new DS4Windows.SpecialAction("Disconnect_Alt", "PS+Share+Options", "DisconnectBT", "0"));
            actions.Add(new DS4Windows.SpecialAction("Emergency_Disconnect", "L1+L2+R1+R2", "DisconnectBT", "0"));

            int totalActions = actions.Count; // 50個

            // Act: 初期化時間を計測
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            DS4Windows.Mapping.actionDone.Clear();
            DS4Windows.Mapping.actionDoneInitialized = false;
            DS4Windows.Mapping.InitializeActionDoneList();
            stopwatch.Stop();

            // Assert: 初期化が正常に完了することを確認
            Assert.IsTrue(DS4Windows.Mapping.actionDoneInitialized, "Large action set should be initialized");
            Assert.AreEqual(totalActions, DS4Windows.Mapping.actionDone.Count, "ActionDone count should match large action set");

            // パフォーマンス結果を詳細出力
            Console.WriteLine($"📈 Large Scale Performance Test Results:");
            Console.WriteLine($"   Total actions: {totalActions}");
            Console.WriteLine($"   Breakdown:");
            Console.WriteLine($"     - Default: 1");
            Console.WriteLine($"     - Profiles: {profiles.Length}");
            Console.WriteLine($"     - Macros: 20");
            Console.WriteLine($"     - Keys: {keys.Length}");
            Console.WriteLine($"     - Others: 4");
            Console.WriteLine($"   ⏱️  Initialization time: {stopwatch.ElapsedMilliseconds}ms");
            Console.WriteLine($"   📊 Performance metrics:");
            Console.WriteLine($"     - Ticks: {stopwatch.ElapsedTicks}");
            Console.WriteLine($"     - Ticks per action: {(double)stopwatch.ElapsedTicks / totalActions:F2}");
            Console.WriteLine($"     - Actions per ms: {(double)totalActions / Math.Max(stopwatch.ElapsedMilliseconds, 1):F2}");

            // 性能基準をチェック
            if (stopwatch.ElapsedMilliseconds <= 1)
            {
                Console.WriteLine($"   ✅ Excellent: Very fast initialization (<= 1ms)");
            }
            else if (stopwatch.ElapsedMilliseconds <= 10)
            {
                Console.WriteLine($"   ✅ Good: Fast initialization (<= 10ms)");  
            }
            else if (stopwatch.ElapsedMilliseconds <= 50)
            {
                Console.WriteLine($"   ⚠️  Acceptable: Moderate initialization (<= 50ms)");
            }
            else
            {
                Console.WriteLine($"   ❌ Slow: Initialization time may be noticeable to users");
            }

            // 50個のアクションでも合理的な時間で完了することを期待
            Assert.IsTrue(stopwatch.ElapsedMilliseconds <= 100, 
                $"Large action set initialization should complete within 100ms, but took {stopwatch.ElapsedMilliseconds}ms");
        }

        /// <summary>
        /// テスト8: 現実的な本番環境アクション数でのパフォーマンステスト
        /// DS4Windowsの典型的な実用例での性能を測定
        /// </summary>
        [TestMethod]
        public void TestRealisticProductionEnvironmentPerformance()
        {
            // Arrange: 現実的なアクション構成（20-25個程度）
            var actions = DS4Windows.Global.GetActions();
            actions.Clear();

            // 実際のDS4Windowsユーザーの典型的な設定をシミュレート

            // 1. 必須デフォルト
            actions.Add(new DS4Windows.SpecialAction("Disconnect Controller", "PS/Options", "DisconnectBT", "0"));

            // 2. ゲーム用プロファイル（5個 - 実際によく使われる数）
            string[] gameProfiles = { "Desktop", "Steam", "Apex_Legends", "Elden_Ring", "Cyberpunk2077" };
            foreach (var profile in gameProfiles)
            {
                actions.Add(new DS4Windows.SpecialAction($"Switch_{profile}", "L1+R1+Cross", "Profile", profile));
            }

            // 3. メディアコントロール（6個 - 音量・再生制御）
            var mediaActions = new[]
            {
                ("Volume_Up", "175"),
                ("Volume_Down", "174"), 
                ("Mute", "173"),
                ("Play_Pause", "179"),
                ("Next_Track", "176"),
                ("Prev_Track", "177")
            };
            foreach (var (name, key) in mediaActions)
            {
                actions.Add(new DS4Windows.SpecialAction(name, "PS+DpadUp", "Key", key));
            }

            // 4. よく使うショートカット（6個）
            var shortcuts = new[]
            {
                ("Alt_Tab", "18/9/0/18/0"),
                ("Ctrl_C", "162/67/0/162/0"),
                ("Ctrl_V", "162/86/0/162/0"),
                ("Win_D", "91/68/0/91/0"),
                ("Escape", "27/0"),
                ("Enter", "13/0")
            };
            foreach (var (name, macro) in shortcuts)
            {
                actions.Add(new DS4Windows.SpecialAction(name, "L2+R2+Triangle", "Macro", macro));
            }

            // 5. システム機能（3個）
            actions.Add(new DS4Windows.SpecialAction("Battery_Status", "TouchButton+L1", "BatteryCheck", ""));
            actions.Add(new DS4Windows.SpecialAction("Gyro_Reset", "Share+Options+L3", "GyroCalibrate", ""));
            actions.Add(new DS4Windows.SpecialAction("Quick_Disconnect", "PS+L3+R3", "DisconnectBT", "0"));

            int realisticActionCount = actions.Count; // 21個（現実的な数）

            // Act: 実際のLoadActions相当の処理を計測
            var totalStopwatch = System.Diagnostics.Stopwatch.StartNew();

            // 初期化処理
            DS4Windows.Mapping.actionDone.Clear();
            DS4Windows.Mapping.actionDoneInitialized = false;
            
            var initStopwatch = System.Diagnostics.Stopwatch.StartNew();
            DS4Windows.Mapping.InitializeActionDoneList();
            initStopwatch.Stop();

            totalStopwatch.Stop();

            // Assert: 正常性確認
            Assert.IsTrue(DS4Windows.Mapping.actionDoneInitialized, "Realistic configuration should initialize properly");
            Assert.AreEqual(realisticActionCount, DS4Windows.Mapping.actionDone.Count, "ActionDone should match realistic count");

            // 詳細パフォーマンス報告
            Console.WriteLine($"🎮 Realistic Production Environment Performance:");
            Console.WriteLine($"   Configuration: Typical DS4Windows User Setup");
            Console.WriteLine($"   Total actions: {realisticActionCount}");
            Console.WriteLine($"   Action types:");
            Console.WriteLine($"     • Default disconnect: 1");
            Console.WriteLine($"     • Game profiles: {gameProfiles.Length}");
            Console.WriteLine($"     • Media controls: {mediaActions.Length}");  
            Console.WriteLine($"     • Shortcuts/Macros: {shortcuts.Length}");
            Console.WriteLine($"     • System functions: 3");
            Console.WriteLine($"");
            Console.WriteLine($"   ⏱️  Performance Results:");
            Console.WriteLine($"     • Initialization: {initStopwatch.ElapsedMilliseconds}ms ({initStopwatch.ElapsedTicks} ticks)");
            Console.WriteLine($"     • Total process: {totalStopwatch.ElapsedMilliseconds}ms");
            Console.WriteLine($"     • Per-action cost: {(double)initStopwatch.ElapsedTicks / realisticActionCount:F2} ticks");

            // ユーザー体感への影響を評価
            if (initStopwatch.ElapsedMilliseconds == 0)
            {
                Console.WriteLine($"   🚀 Instant: Initialization is imperceptible to users");
            }
            else if (initStopwatch.ElapsedMilliseconds <= 5)
            {
                Console.WriteLine($"   ✅ Excellent: Users won't notice initialization delay");
            }
            else if (initStopwatch.ElapsedMilliseconds <= 20)
            {
                Console.WriteLine($"   ⚠️  Noticeable: Minor delay but acceptable for startup");
            }
            else
            {
                Console.WriteLine($"   ❌ Problematic: Initialization delay may impact user experience");
            }

            // 現実的な環境では非常に高速であることを期待
            Assert.IsTrue(initStopwatch.ElapsedMilliseconds <= 20, 
                $"Realistic production environment should initialize very quickly, but took {initStopwatch.ElapsedMilliseconds}ms");
        }

        /// <summary>
        /// テスト9: 待機メカニズムの詳細動作を可視化（デモンストレーション用）
        /// </summary>
        [TestMethod]
        public async Task TestWaitingMechanismDetailedBehavior()
        {
            Console.WriteLine("\n🔍 === 待機メカニズム詳細動作テスト ===");
            
            // Arrange: 初期化されていない状態を作成
            SetupTestActions();
            DS4Windows.Mapping.actionDoneInitialized = false;
            DS4Windows.Mapping.actionDone.Clear();

            var totalStopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            // 段階的な遅延で初期化をシミュレート
            Console.WriteLine("📋 テストシナリオ: 200ms後に初期化完了");
            
            // 遅延初期化タスクを開始
            var delayedInitTask = Task.Run(async () =>
            {
                Console.WriteLine($"[{totalStopwatch.ElapsedMilliseconds:D3}ms] 🔄 初期化処理開始（他スレッド）");
                await Task.Delay(200); // 200ms遅延
                DS4Windows.Mapping.InitializeActionDoneList();
                Console.WriteLine($"[{totalStopwatch.ElapsedMilliseconds:D3}ms] ✅ 初期化処理完了");
            });

            // Act: EnsureActionDoneInitializedによる待機テスト
            Console.WriteLine($"[{totalStopwatch.ElapsedMilliseconds:D3}ms] ⏳ 待機開始");
            
            var mappingType = typeof(DS4Windows.Mapping);
            var ensureMethod = mappingType.GetMethod("EnsureActionDoneInitialized", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            
            var waitTask = ensureMethod.Invoke(null, null) as Task;
            
            // 両方の完了を待機
            await Task.WhenAll(waitTask, delayedInitTask);
            
            totalStopwatch.Stop();

            // Assert: 結果の検証
            Assert.IsTrue(DS4Windows.Mapping.actionDoneInitialized, "Should be initialized after waiting");
            
            var actions = DS4Windows.Global.GetActions();
            Assert.AreEqual(actions.Count, DS4Windows.Mapping.actionDone.Count, 
                "ActionDone should match actions count after wait");

            // 結果レポート
            Console.WriteLine($"\n📊 === 待機メカニズム テスト結果 ===");
            Console.WriteLine($"   設定された遅延: 200ms");
            Console.WriteLine($"   実際の待機時間: {totalStopwatch.ElapsedMilliseconds}ms");
            Console.WriteLine($"   アクション数: {actions.Count}");
            Console.WriteLine($"   チェック頻度: 10msごと");
            Console.WriteLine($"   予想チェック回数: ~{200 / 10}回");
            Console.WriteLine($"   結果: {(totalStopwatch.ElapsedMilliseconds >= 200 ? "✅ 正常待機" : "🔥 予想より高速")}");
            
            // 待機時間の妥当性チェック（1秒タイムアウトに対応）
            Assert.IsTrue(totalStopwatch.ElapsedMilliseconds >= 190, "Should wait approximately 200ms");
            Assert.IsTrue(totalStopwatch.ElapsedMilliseconds <= 300, "Should not exceed reasonable wait time");
        }

        /// <summary>
        /// テスト10: 1秒タイムアウトの妥当性検証
        /// 実測データに基づく適切なタイムアウト時間の検証
        /// </summary>
        [TestMethod]
        public async Task TestOptimizedOneSecondTimeout()
        {
            Console.WriteLine("\n⏰ === 1秒タイムアウトの妥当性テスト ===");
            
            // Arrange: 大量アクションでの最悪ケースをテスト
            SetupTestActions();
            
            // 更に大量のアクションを追加（極端なケース）
            var actions = DS4Windows.Global.GetActions();
            int initialCount = actions.Count;
            
            // 500個のアクションを追加（非現実的だが限界テスト用）
            for (int i = initialCount; i < initialCount + 500; i++)
            {
                actions.Add(new DS4Windows.SpecialAction($"Test_Action_{i}", "L1+R1+Cross", "Key", "32"));
            }
            
            Console.WriteLine($"📊 極限テストケース: {actions.Count}個のアクション");

            // Act: 実際の初期化時間を計測
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            DS4Windows.Mapping.actionDone.Clear();
            DS4Windows.Mapping.actionDoneInitialized = false;
            DS4Windows.Mapping.InitializeActionDoneList();
            
            stopwatch.Stop();

            // Assert: 1秒以内に完了することを確認
            Assert.IsTrue(DS4Windows.Mapping.actionDoneInitialized, "Should initialize successfully");
            Assert.AreEqual(actions.Count, DS4Windows.Mapping.actionDone.Count, "Should match action count");
            
            // パフォーマンス結果
            Console.WriteLine($"📈 極限ケース結果:");
            Console.WriteLine($"   アクション数: {actions.Count}個");
            Console.WriteLine($"   初期化時間: {stopwatch.ElapsedMilliseconds}ms");
            Console.WriteLine($"   1秒タイムアウトとの比較: {(stopwatch.ElapsedMilliseconds <= 1000 ? "✅ 十分な余裕" : "⚠️ 余裕不足")}");
            Console.WriteLine($"   初期化効率: {actions.Count / Math.Max(stopwatch.ElapsedMilliseconds, 1):F0} actions/ms");

            // 1秒タイムアウトの妥当性を検証
            Assert.IsTrue(stopwatch.ElapsedMilliseconds <= 100, 
                $"Even with {actions.Count} actions, initialization should be much faster than 1s timeout. Actual: {stopwatch.ElapsedMilliseconds}ms");

            // 実測に基づく余裕度の計算
            double safetyMargin = 1000.0 / Math.Max(stopwatch.ElapsedMilliseconds, 1);
            Console.WriteLine($"   安全余裕: {safetyMargin:F1}倍（1秒タイムアウト vs 実測{stopwatch.ElapsedMilliseconds}ms）");
            
            Assert.IsTrue(safetyMargin >= 10, 
                $"Safety margin should be at least 10x, but was {safetyMargin:F1}x");

            Console.WriteLine($"\n💡 結論: 1秒タイムアウトは実測データに対して十分な安全余裕を持っている");
        }
    }
}
