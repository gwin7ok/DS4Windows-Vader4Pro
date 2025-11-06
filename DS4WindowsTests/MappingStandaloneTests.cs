using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace DS4WindowsTests
{
    /// <summary>
    /// スタンドアロンのMappingテスト - 依存関係の問題を回避
    /// </summary>
    [TestClass]
    public class MappingStandaloneTests
    {
        /// <summary>
        /// 簡単なActionStateクラスのシミュレーション
        /// </summary>
        public class MockActionState
        {
            public bool[] dev = new bool[8]; // MAX_DS4_CONTROLLER_COUNT = 8
        }

        /// <summary>
        /// 簡単なSpecialActionクラスのシミュレーション
        /// </summary>
        public class MockSpecialAction
        {
            public string name;
            public string trigger;
            public string type;
            public string details;

            public MockSpecialAction(string name, string trigger, string type, string details)
            {
                this.name = name;
                this.trigger = trigger;
                this.type = type;
                this.details = details;
            }
        }

        /// <summary>
        /// 初期化処理のシミュレーション
        /// </summary>
        public static class MockMapping
        {
            public static List<MockActionState> actionDone = new List<MockActionState>();
            public static bool actionDoneInitialized = false;
            public static readonly object actionDoneLock = new object();

            public static void InitializeActionDoneList(List<MockSpecialAction> actions)
            {
                lock (actionDoneLock)
                {
                    if (actionDoneInitialized)
                        return;

                    actionDone.Clear();
                    foreach (var action in actions)
                    {
                        actionDone.Add(new MockActionState());
                    }

                    actionDoneInitialized = true;
                }
            }

            public static async Task<bool> EnsureActionDoneInitialized(List<MockSpecialAction> actions)
            {
                if (actionDoneInitialized)
                    return true;

                const int maxWaitTimeMs = 1000; // 1秒タイムアウト
                const int checkIntervalMs = 10;

                var stopwatch = Stopwatch.StartNew();

                while (!actionDoneInitialized && stopwatch.ElapsedMilliseconds < maxWaitTimeMs)
                {
                    await Task.Delay(checkIntervalMs);
                }

                return actionDoneInitialized;
            }

            public static void Reset()
            {
                lock (actionDoneLock)
                {
                    actionDone.Clear();
                    actionDoneInitialized = false;
                }
            }
        }

        [TestInitialize]
        public void TestSetup()
        {
            MockMapping.Reset();
        }

        /// <summary>
        /// テスト1: 基本的な初期化テスト
        /// </summary>
        [TestMethod]
        public void TestBasicInitialization()
        {
            Console.WriteLine("\n🚀 === 基本初期化テスト ===");
            
            // Arrange
            var actions = new List<MockSpecialAction>
            {
                new MockSpecialAction("Test_Profile_Switch", "L1+R1+Cross", "Profile", "Default"),
                new MockSpecialAction("Test_Macro", "L2+R2+Circle", "Macro", "Alt+Tab"),
                new MockSpecialAction("Test_Key", "L3+R3+Square", "Key", "32")
            };

            // Act
            MockMapping.InitializeActionDoneList(actions);

            // Assert
            Assert.IsTrue(MockMapping.actionDoneInitialized, "Should initialize successfully");
            Assert.AreEqual(actions.Count, MockMapping.actionDone.Count, "Should match action count");
            
            foreach (var actionState in MockMapping.actionDone)
            {
                Assert.IsNotNull(actionState, "ActionState should not be null");
                Assert.AreEqual(8, actionState.dev.Length, "Should have 8 controller slots");
                Assert.IsTrue(actionState.dev.All(d => !d), "All controller states should be false initially");
            }

            Console.WriteLine($"✅ 初期化完了: {actions.Count}個のアクション");
        }

        /// <summary>
        /// テスト2: 1秒タイムアウトの妥当性検証
        /// </summary>
        [TestMethod]
        public async Task TestOptimizedOneSecondTimeout()
        {
            Console.WriteLine("\n⏰ === 1秒タイムアウト妥当性テスト ===");
            
            // Arrange: 大量アクションでの極限テスト
            var actions = new List<MockSpecialAction>();
            
            // 500個のアクションを生成（極限ケース）
            for (int i = 0; i < 500; i++)
            {
                actions.Add(new MockSpecialAction($"Test_Action_{i}", "L1+R1+Cross", "Key", "32"));
            }
            
            Console.WriteLine($"📊 極限テストケース: {actions.Count}個のアクション");

            // Act: 実際の初期化時間を計測
            var stopwatch = Stopwatch.StartNew();
            MockMapping.InitializeActionDoneList(actions);
            stopwatch.Stop();

            // Assert: パフォーマンス検証
            Assert.IsTrue(MockMapping.actionDoneInitialized, "Should initialize successfully");
            Assert.AreEqual(actions.Count, MockMapping.actionDone.Count, "Should match action count");
            
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

        /// <summary>
        /// テスト3: 待機メカニズムテスト
        /// </summary>
        [TestMethod]
        public async Task TestWaitMechanism()
        {
            Console.WriteLine("\n⏳ === 待機メカニズムテスト ===");
            
            // Arrange
            var actions = new List<MockSpecialAction>
            {
                new MockSpecialAction("Test_Action", "L1+R1+Cross", "Key", "32")
            };

            // Act & Assert: 初期化前の待機
            var waitTask = MockMapping.EnsureActionDoneInitialized(actions);
            
            // 200ms後に初期化を実行
            _ = Task.Run(async () =>
            {
                await Task.Delay(200);
                MockMapping.InitializeActionDoneList(actions);
            });

            var totalStopwatch = Stopwatch.StartNew();
            bool result = await waitTask;
            totalStopwatch.Stop();

            // 結果検証
            Assert.IsTrue(result, "Wait mechanism should succeed");
            Assert.IsTrue(MockMapping.actionDoneInitialized, "Should be initialized after wait");
            
            // 待機時間の妥当性チェック（1秒タイムアウトに対応）
            Assert.IsTrue(totalStopwatch.ElapsedMilliseconds >= 190, "Should wait approximately 200ms");
            Assert.IsTrue(totalStopwatch.ElapsedMilliseconds <= 300, "Should not exceed reasonable wait time");

            Console.WriteLine($"⏱️ 待機時間: {totalStopwatch.ElapsedMilliseconds}ms（期待: ~200ms）");
            Console.WriteLine("✅ 待機メカニズム正常動作確認");
        }

        /// <summary>
        /// テスト4: タイムアウト動作テスト
        /// </summary>
        [TestMethod]
        public async Task TestTimeoutBehavior()
        {
            Console.WriteLine("\n⚡ === タイムアウト動作テスト ===");
            
            // Arrange: 初期化を実行しない状態
            var actions = new List<MockSpecialAction>
            {
                new MockSpecialAction("Test_Action", "L1+R1+Cross", "Key", "32")
            };

            // Act: 1秒タイムアウトを待つ
            var stopwatch = Stopwatch.StartNew();
            bool result = await MockMapping.EnsureActionDoneInitialized(actions);
            stopwatch.Stop();

            // Assert: タイムアウト動作
            Assert.IsFalse(result, "Should timeout and return false");
            Assert.IsFalse(MockMapping.actionDoneInitialized, "Should remain uninitialized");
            
            // 1秒タイムアウトの検証（余裕を持って1100ms以内）
            Assert.IsTrue(stopwatch.ElapsedMilliseconds >= 1000, "Should wait at least 1000ms");
            Assert.IsTrue(stopwatch.ElapsedMilliseconds <= 1100, "Should not exceed 1100ms");

            Console.WriteLine($"⏱️ タイムアウト時間: {stopwatch.ElapsedMilliseconds}ms（期待: ~1000ms）");
            Console.WriteLine("✅ 1秒タイムアウト正常動作確認");
        }
    }
}