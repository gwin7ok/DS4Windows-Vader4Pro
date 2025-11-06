using System;
using System.Threading.Tasks;
using System.Diagnostics;

namespace DS4WindowsTests
{
    /// <summary>
    /// 待機メカニズムの動作を詳しく可視化するデモテスト
    /// </summary>
    public class WaitingMechanismDemo
    {
        /// <summary>
        /// ポーリング待機の具体的な動作を示すデモ
        /// </summary>
        public static async Task DemonstrateWaitingMechanism()
        {
            Console.WriteLine("=== 待機メカニズム動作デモ ===");
            
            // デモ用の初期化フラグ
            bool isInitialized = false;
            
            // 待機開始
            var stopwatch = Stopwatch.StartNew();
            Console.WriteLine($"[{stopwatch.ElapsedMilliseconds:D4}ms] 待機開始");
            
            // 別スレッドで遅延初期化をシミュレート
            var initTask = Task.Run(async () =>
            {
                await Task.Delay(234); // 234ms後に初期化完了
                isInitialized = true;
                Console.WriteLine($"[{stopwatch.ElapsedMilliseconds:D4}ms] 🎯 初期化完了！");
            });
            
            // ポーリング待機のシミュレート
            const int maxWaitMs = 5000;
            const int checkIntervalMs = 10;
            int elapsedMs = 0;
            
            while (elapsedMs < maxWaitMs)
            {
                if (isInitialized)
                {
                    Console.WriteLine($"[{stopwatch.ElapsedMilliseconds:D4}ms] ✅ 待機終了 - 初期化確認");
                    break;
                }
                
                // 10msごとのチェックをログ出力（最初の数回のみ）
                if (elapsedMs < 50 || elapsedMs % 50 == 0)
                {
                    Console.WriteLine($"[{stopwatch.ElapsedMilliseconds:D4}ms] ⏳ チェック中... (経過: {elapsedMs}ms)");
                }
                
                await Task.Delay(checkIntervalMs);
                elapsedMs += checkIntervalMs;
            }
            
            // タイムアウトチェック
            if (!isInitialized && elapsedMs >= maxWaitMs)
            {
                Console.WriteLine($"[{stopwatch.ElapsedMilliseconds:D4}ms] ⏰ タイムアウト - 強制初期化実行");
                isInitialized = true; // 強制初期化
            }
            
            await initTask; // 初期化タスク完了を待機
            stopwatch.Stop();
            
            Console.WriteLine($"[{stopwatch.ElapsedMilliseconds:D4}ms] 🏁 処理完了");
            Console.WriteLine($"総待機時間: {stopwatch.ElapsedMilliseconds}ms");
            Console.WriteLine();
        }
        
        /// <summary>
        /// 異なる初期化時間でのパフォーマンステスト
        /// </summary>
        public static async Task TestVariousInitializationTimes()
        {
            Console.WriteLine("=== 様々な初期化時間でのテスト ===");
            
            int[] initTimes = { 0, 5, 50, 150, 500, 1000, 6000 }; // 最後は意図的にタイムアウト
            
            foreach (int initTime in initTimes)
            {
                Console.WriteLine($"\n--- 初期化時間: {initTime}ms ---");
                
                bool isInitialized = false;
                var stopwatch = Stopwatch.StartNew();
                
                // 指定時間後に初期化するタスク
                var initTask = Task.Run(async () =>
                {
                    if (initTime > 0)
                        await Task.Delay(initTime);
                    isInitialized = true;
                });
                
                // 待機ロジック
                const int maxWaitMs = 5000;
                const int checkIntervalMs = 10;
                int elapsedMs = 0;
                int checkCount = 0;
                
                while (elapsedMs < maxWaitMs && !isInitialized)
                {
                    await Task.Delay(checkIntervalMs);
                    elapsedMs += checkIntervalMs;
                    checkCount++;
                }
                
                // タイムアウト処理
                if (!isInitialized)
                {
                    Console.WriteLine("⏰ タイムアウト発生 - 強制初期化");
                    isInitialized = true;
                }
                
                stopwatch.Stop();
                
                // 結果出力
                string status = stopwatch.ElapsedMilliseconds <= initTime + 50 ? "✅ 正常" : 
                               stopwatch.ElapsedMilliseconds >= 5000 ? "⏰ タイムアウト" : "🔥 高速";
                
                Console.WriteLine($"  実際の待機時間: {stopwatch.ElapsedMilliseconds}ms");
                Console.WriteLine($"  チェック回数: {checkCount}回");
                Console.WriteLine($"  結果: {status}");
                
                await initTask; // クリーンアップ
            }
        }
    }
}