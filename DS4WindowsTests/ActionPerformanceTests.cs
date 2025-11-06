using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.IO;

namespace DS4WindowsTests
{
    /// <summary>
    /// Special Actionsのパフォーマンステスト - アクションリスト生成時間の計測
    /// </summary>
    [TestClass]
    public class ActionPerformanceTests
    {
        /// <summary>
        /// テスト用の大量Special Actionsを生成
        /// 実際のDS4Windowsユーザーが使用する可能性のある規模をシミュレート
        /// </summary>
        private void CreateLargeActionSet(int actionCount)
        {
            var actions = DS4Windows.Global.GetActions();
            actions.Clear();

            // 1. デフォルトアクション（必須）
            actions.Add(new DS4Windows.SpecialAction("Disconnect Controller", "PS/Options", "DisconnectBT", "0"));

            // 2. Profile切り替えアクション（多くのユーザーが使用）
            string[] profileNames = { "Desktop", "Gaming", "Media", "Browser", "Steam", "RPG", "FPS", "Racing" };
            for (int i = 0; i < Math.Min(actionCount / 4, profileNames.Length); i++)
            {
                actions.Add(new DS4Windows.SpecialAction(
                    $"Switch to {profileNames[i]}", 
                    $"L1+R1+Cross+{(i % 4 == 0 ? "Triangle" : i % 4 == 1 ? "Circle" : i % 4 == 2 ? "Square" : "L3")}", 
                    "Profile", 
                    profileNames[i]));
            }

            // 3. Macroアクション（複雑なキーシーケンス）
            string[] macroSequences = {
                "87/0/87/0/65/0/65/0/83/0/83/0/68/0/68/0", // WASD練習
                "9/0/72/0/69/0/76/0/76/0/79/0/79", // TAB+HELLO
                "162/65/0/162/0/163/67/0/163/0", // Ctrl+A, Ctrl+C
                "27/0/27/0/13/0", // ESC ESC Enter
                "112/0/113/0/114/0/115/0", // F1,F2,F3,F4
            };
            
            for (int i = 0; i < Math.Min(actionCount / 4, macroSequences.Length * 4); i++)
            {
                string macroSeq = macroSequences[i % macroSequences.Length];
                actions.Add(new DS4Windows.SpecialAction(
                    $"Macro_{i + 1}", 
                    $"R1+R2+{(i % 8 == 0 ? "Cross" : i % 8 == 1 ? "Circle" : i % 8 == 2 ? "Square" : i % 8 == 3 ? "Triangle" : i % 8 == 4 ? "L1" : i % 8 == 5 ? "L2" : i % 8 == 6 ? "L3" : "R3")}", 
                    "Macro", 
                    macroSeq));
            }

            // 4. キー送信アクション
            string[] keyActions = { "32", "13", "27", "9", "20", "144", "145", "112", "113", "114", "115", "116" }; // Space, Enter, Esc, Tab, CapsLock, NumLock, ScrollLock, F1-F5
            for (int i = 0; i < Math.Min(actionCount / 4, keyActions.Length * 2); i++)
            {
                string keyCode = keyActions[i % keyActions.Length];
                actions.Add(new DS4Windows.SpecialAction(
                    $"Send_Key_{keyCode}", 
                    $"L2+R2+{(i % 6 == 0 ? "Cross" : i % 6 == 1 ? "Circle" : i % 6 == 2 ? "Square" : i % 6 == 3 ? "Triangle" : i % 6 == 4 ? "DpadUp" : "DpadDown")}", 
                    "Key", 
                    keyCode));
            }

            // 5. プログラム起動アクション
            string[] programs = { 
                "notepad.exe", "calc.exe", "mspaint.exe", "explorer.exe", 
                "chrome.exe", "firefox.exe", "steam.exe", "discord.exe" 
            };
            for (int i = 0; i < Math.Min(actionCount / 8, programs.Length); i++)
            {
                actions.Add(new DS4Windows.SpecialAction(
                    $"Launch_{programs[i].Replace(".exe", "")}", 
                    $"PS+Share+{(i % 4 == 0 ? "Cross" : i % 4 == 1 ? "Circle" : i % 4 == 2 ? "Square" : "Triangle")}", 
                    "Program", 
                    programs[i]));
            }

            // 6. その他のアクション
            for (int i = actions.Count; i < actionCount; i++)
            {
                if (i % 3 == 0)
                {
                    // BatteryCheck
                    actions.Add(new DS4Windows.SpecialAction(
                        $"Battery_Check_{i}", 
                        $"TouchButton+{(i % 4 == 0 ? "L1" : i % 4 == 1 ? "R1" : i % 4 == 2 ? "L2" : "R2")}", 
                        "BatteryCheck", 
                        ""));
                }
                else if (i % 3 == 1)
                {
                    // GyroCalibrate
                    actions.Add(new DS4Windows.SpecialAction(
                        $"Gyro_Calibrate_{i}", 
                        $"Share+Options+{(i % 2 == 0 ? "L3" : "R3")}", 
                        "GyroCalibrate", 
                        ""));
                }
                else
                {
                    // DisconnectBT (追加)
                    actions.Add(new DS4Windows.SpecialAction(
                        $"Disconnect_{i}", 
                        $"PS+{(i % 8 == 0 ? "Cross+Circle" : i % 8 == 1 ? "Square+Triangle" : i % 8 == 2 ? "L1+R1" : i % 8 == 3 ? "L2+R2" : i % 8 == 4 ? "DpadUp+DpadDown" : i % 8 == 5 ? "DpadLeft+DpadRight" : i % 8 == 6 ? "L3+R3" : "Share+Options")}", 
                        "DisconnectBT", 
                        "0"));
                }
            }
        }

        /// <summary>
        /// 少数アクション（10個）での初期化時間計測
        /// </summary>
        [TestMethod]
        public void TestSmallActionSetInitializationTime()
        {
            // Arrange: 10個のアクションを作成
            CreateLargeActionSet(10);
            var actions = DS4Windows.Global.GetActions();
            Assert.AreEqual(10, actions.Count, "Should have exactly 10 actions for small test");

            // Measure initialization time
            var stopwatch = Stopwatch.StartNew();
            
            // Act: actionDone配列の初期化を実行
            DS4Windows.Mapping.actionDone.Clear();
            DS4Windows.Mapping.actionDoneInitialized = false;
            DS4Windows.Mapping.InitializeActionDoneList();
            
            stopwatch.Stop();

            // Assert: 結果の検証
            Assert.IsTrue(DS4Windows.Mapping.actionDoneInitialized, "Should be initialized");
            Assert.AreEqual(actions.Count, DS4Windows.Mapping.actionDone.Count, "ActionDone count should match actions count");
            
            Console.WriteLine($"Small Action Set (10 actions): {stopwatch.ElapsedMilliseconds}ms ({stopwatch.ElapsedTicks} ticks)");
            
            // 10個のアクションなら1ms以下であることを期待
            Assert.IsTrue(stopwatch.ElapsedMilliseconds <= 5, $"Small action set initialization should be very fast, but took {stopwatch.ElapsedMilliseconds}ms");
        }

        /// <summary>
        /// 中規模アクション（50個）での初期化時間計測
        /// 一般的なパワーユーザーの使用規模
        /// </summary>
        [TestMethod]
        public void TestMediumActionSetInitializationTime()
        {
            // Arrange: 50個のアクション（一般的なパワーユーザー）
            CreateLargeActionSet(50);
            var actions = DS4Windows.Global.GetActions();
            Assert.AreEqual(50, actions.Count, "Should have exactly 50 actions for medium test");

            var stopwatch = Stopwatch.StartNew();
            
            // Act: 初期化実行
            DS4Windows.Mapping.actionDone.Clear();
            DS4Windows.Mapping.actionDoneInitialized = false;
            DS4Windows.Mapping.InitializeActionDoneList();
            
            stopwatch.Stop();

            // Assert
            Assert.IsTrue(DS4Windows.Mapping.actionDoneInitialized, "Should be initialized");
            Assert.AreEqual(actions.Count, DS4Windows.Mapping.actionDone.Count, "ActionDone count should match actions count");
            
            Console.WriteLine($"Medium Action Set (50 actions): {stopwatch.ElapsedMilliseconds}ms ({stopwatch.ElapsedTicks} ticks)");
            
            // 50個でも数ms以下であることを期待
            Assert.IsTrue(stopwatch.ElapsedMilliseconds <= 20, $"Medium action set initialization should be fast, but took {stopwatch.ElapsedMilliseconds}ms");
        }

        /// <summary>
        /// 大規模アクション（200個）での初期化時間計測  
        /// ヘビーユーザー、マクロ多用者の使用規模
        /// </summary>
        [TestMethod]
        public void TestLargeActionSetInitializationTime()
        {
            // Arrange: 200個のアクション（ヘビーユーザー規模）
            CreateLargeActionSet(200);
            var actions = DS4Windows.Global.GetActions();
            Assert.AreEqual(200, actions.Count, "Should have exactly 200 actions for large test");

            var stopwatch = Stopwatch.StartNew();
            
            // Act: 初期化実行
            DS4Windows.Mapping.actionDone.Clear();
            DS4Windows.Mapping.actionDoneInitialized = false;
            DS4Windows.Mapping.InitializeActionDoneList();
            
            stopwatch.Stop();

            // Assert
            Assert.IsTrue(DS4Windows.Mapping.actionDoneInitialized, "Should be initialized");
            Assert.AreEqual(actions.Count, DS4Windows.Mapping.actionDone.Count, "ActionDone count should match actions count");
            
            Console.WriteLine($"Large Action Set (200 actions): {stopwatch.ElapsedMilliseconds}ms ({stopwatch.ElapsedTicks} ticks)");
            
            // 200個でも50ms以下であることを期待
            Assert.IsTrue(stopwatch.ElapsedMilliseconds <= 50, $"Large action set initialization should still be reasonable, but took {stopwatch.ElapsedMilliseconds}ms");
        }

        /// <summary>
        /// 極大規模アクション（1000個）での初期化時間計測
        /// 理論上の最大規模（非現実的だが性能限界テスト）
        /// </summary>
        [TestMethod]
        public void TestExtremeActionSetInitializationTime()
        {
            // Arrange: 1000個のアクション（非現実的な規模だが性能テスト用）
            CreateLargeActionSet(1000);
            var actions = DS4Windows.Global.GetActions();
            Assert.AreEqual(1000, actions.Count, "Should have exactly 1000 actions for extreme test");

            var stopwatch = Stopwatch.StartNew();
            
            // Act: 初期化実行
            DS4Windows.Mapping.actionDone.Clear();
            DS4Windows.Mapping.actionDoneInitialized = false;
            DS4Windows.Mapping.InitializeActionDoneList();
            
            stopwatch.Stop();

            // Assert
            Assert.IsTrue(DS4Windows.Mapping.actionDoneInitialized, "Should be initialized");
            Assert.AreEqual(actions.Count, DS4Windows.Mapping.actionDone.Count, "ActionDone count should match actions count");
            
            Console.WriteLine($"Extreme Action Set (1000 actions): {stopwatch.ElapsedMilliseconds}ms ({stopwatch.ElapsedTicks} ticks)");
            
            // 1000個でも500ms以下であることを期待（非現実的規模だが念のため）
            Assert.IsTrue(stopwatch.ElapsedMilliseconds <= 500, $"Even extreme action set initialization should complete reasonably, but took {stopwatch.ElapsedMilliseconds}ms");
        }

        /// <summary>
        /// LoadActions全体の処理時間計測（XML読み込み + 初期化）
        /// 実際のDS4Windows起動時の処理をシミュレート
        /// </summary>
        [TestMethod]
        public void TestFullLoadActionsPerformance()
        {
            // Arrange: 実際のActions.xml相当のテストデータ
            CreateLargeActionSet(100); // 中規模ユーザー想定
            
            // Act & Measure: LoadActions全体の時間を計測
            var stopwatch = Stopwatch.StartNew();
            
            // 実際のLoadActionsと同等の処理をシミュレート
            var actions = DS4Windows.Global.GetActions();
            int actionCount = actions.Count;
            
            DS4Windows.Mapping.actionDone.Clear();
            DS4Windows.Mapping.actionDoneInitialized = false;
            DS4Windows.Mapping.InitializeActionDoneList();
            
            stopwatch.Stop();

            // Assert
            Assert.IsTrue(DS4Windows.Mapping.actionDoneInitialized, "LoadActions should complete initialization");
            Assert.AreEqual(actionCount, DS4Windows.Mapping.actionDone.Count, "ActionDone should be properly sized");
            
            Console.WriteLine($"Full LoadActions simulation ({actionCount} actions): {stopwatch.ElapsedMilliseconds}ms");
            Console.WriteLine($"Initialization overhead per action: {(double)stopwatch.ElapsedTicks / actionCount:F2} ticks/action");
            
            // 100個のアクションで100ms以下であることを期待
            Assert.IsTrue(stopwatch.ElapsedMilliseconds <= 100, $"Full LoadActions should complete quickly, but took {stopwatch.ElapsedMilliseconds}ms");
        }

        /// <summary>
        /// 現実的なアクション構成での性能テスト
        /// 実際のDS4Windowsユーザーの典型的な使用パターン
        /// </summary>
        [TestMethod]
        public void TestRealisticActionConfigurationPerformance()
        {
            // Arrange: 現実的な構成
            var actions = DS4Windows.Global.GetActions();
            actions.Clear();

            // 典型的なユーザー構成:
            // - デフォルトアクション: 1個
            actions.Add(new DS4Windows.SpecialAction("Disconnect Controller", "PS/Options", "DisconnectBT", "0"));

            // - プロファイル切り替え: 5個（デスクトップ、ゲーム、メディア、ブラウザ、作業用）
            string[] realProfiles = { "Desktop", "Gaming", "Media", "Browser", "Work" };
            foreach (var profile in realProfiles)
            {
                actions.Add(new DS4Windows.SpecialAction($"Switch to {profile}", "L1+R1+Cross", "Profile", profile));
            }

            // - よく使うキー: 8個（音量、メディアコントロール等）
            string[] commonKeys = { "175", "174", "173", "179", "178", "177", "176", "27" }; // Volume+, Volume-, Mute, Play, Stop, Prev, Next, Esc
            for (int i = 0; i < commonKeys.Length; i++)
            {
                actions.Add(new DS4Windows.SpecialAction($"Media_Key_{i}", $"PS+DpadUp", "Key", commonKeys[i]));
            }

            // - 便利マクロ: 5個
            string[] utilityMacros = {
                "9/0/9/0/9/0/9/0", // Tab x4 (ウィンドウ切り替え)
                "91/68/0/91/0", // Win+D (デスクトップ表示)
                "18/9/0/18/0", // Alt+Tab
                "162/67/0/162/0", // Ctrl+C
                "162/86/0/162/0"  // Ctrl+V
            };
            for (int i = 0; i < utilityMacros.Length; i++)
            {
                actions.Add(new DS4Windows.SpecialAction($"Utility_Macro_{i + 1}", $"L2+R2+Cross", "Macro", utilityMacros[i]));
            }

            // - バッテリーチェック: 1個
            actions.Add(new DS4Windows.SpecialAction("Battery Check", "TouchButton+L1", "BatteryCheck", ""));

            // - ジャイロキャリブレーション: 1個  
            actions.Add(new DS4Windows.SpecialAction("Gyro Calibrate", "Share+Options+L3", "GyroCalibrate", ""));

            int totalActions = actions.Count; // 21個の現実的な構成

            // Act & Measure
            var stopwatch = Stopwatch.StartNew();
            
            DS4Windows.Mapping.actionDone.Clear();
            DS4Windows.Mapping.actionDoneInitialized = false;
            DS4Windows.Mapping.InitializeActionDoneList();
            
            stopwatch.Stop();

            // Assert
            Assert.IsTrue(DS4Windows.Mapping.actionDoneInitialized, "Should complete initialization");
            Assert.AreEqual(totalActions, DS4Windows.Mapping.actionDone.Count, "Should match realistic action count");
            
            Console.WriteLine($"Realistic Configuration ({totalActions} actions): {stopwatch.ElapsedMilliseconds}ms ({stopwatch.ElapsedTicks} ticks)");
            Console.WriteLine($"Action breakdown:");
            Console.WriteLine($"  - Default: 1");
            Console.WriteLine($"  - Profile switches: {realProfiles.Length}");
            Console.WriteLine($"  - Media keys: {commonKeys.Length}");
            Console.WriteLine($"  - Utility macros: {utilityMacros.Length}");
            Console.WriteLine($"  - Other actions: 2");
            Console.WriteLine($"  - Total: {totalActions}");

            // 現実的な構成では数ms以下であることを期待
            Assert.IsTrue(stopwatch.ElapsedMilliseconds <= 10, $"Realistic configuration should initialize very quickly, but took {stopwatch.ElapsedMilliseconds}ms");
        }
    }
}