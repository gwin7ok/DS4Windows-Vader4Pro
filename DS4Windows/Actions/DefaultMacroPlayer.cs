using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DS4Windows;

namespace DS4Windows.Actions
{
    /// <summary>
    /// IMacroPlayer の標準実装
    /// Mapping.cs のマクロ再生・非同期タスク管理・キー解放ロジックを完全維持してカプセル化
    /// </summary>
    public class DefaultMacroPlayer : IMacroPlayer, IDisposable
    {
        // 4デバイス（コントローラー0〜3）分の実行管理
        private const int MaxDevices = 4;
        private readonly bool[] _macroPlaying = new bool[MaxDevices];
        private readonly CancellationTokenSource[] _macroSources = new CancellationTokenSource[MaxDevices];
        private readonly Task[] _macroTasks = new Task[MaxDevices];
        private readonly HashSet<int>[] _pressedKeys = new HashSet<int>[MaxDevices];
        private readonly object _lock = new object();

        public DefaultMacroPlayer()
        {
            for (int i = 0; i < MaxDevices; i++)
            {
                _pressedKeys[i] = new HashSet<int>();
            }
        }

        public bool IsPlaying(int deviceIndex)
        {
            if (deviceIndex < 0 || deviceIndex >= MaxDevices) return false;
            lock (_lock)
            {
                return _macroPlaying[deviceIndex];
            }
        }

        public void Play(int deviceIndex, SpecialAction action, CancellationToken cancellationToken = default)
        {
            if (deviceIndex < 0 || deviceIndex >= MaxDevices || action == null) return;

            lock (_lock)
            {
                // 既存マクロが再生中の場合は先に停止して安全解放
                if (_macroPlaying[deviceIndex])
                {
                    StopInternal(deviceIndex);
                }

                _macroSources[deviceIndex] = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                var token = _macroSources[deviceIndex].Token;
                _macroPlaying[deviceIndex] = true;

                _macroTasks[deviceIndex] = Task.Run(async () =>
                {
                    try
                    {
                        await RunMacroTaskAsync(deviceIndex, action, token).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        // キャンセルは正常動作
                    }
                    catch (Exception ex)
                    {
                        AppLogger.LogToGui($"Macro execution error on device {deviceIndex}: {ex.Message}", true);
                    }
                    finally
                    {
                        lock (_lock)
                        {
                            StopInternal(deviceIndex);
                        }
                    }
                }, token);
            }
        }

        public void Stop(int deviceIndex)
        {
            if (deviceIndex < 0 || deviceIndex >= MaxDevices) return;
            lock (_lock)
            {
                StopInternal(deviceIndex);
            }
        }

        private void StopInternal(int deviceIndex)
        {
            try
            {
                _macroSources[deviceIndex]?.Cancel();
            }
            catch (ObjectDisposedException) { }

            // 押下状態のまま残っているキー・マウスを全解放 (Safe Cleanup)
            ReleaseAllPressedKeys(deviceIndex);

            _macroPlaying[deviceIndex] = false;
        }

        private async Task RunMacroTaskAsync(int deviceIndex, SpecialAction action, CancellationToken token)
        {
            if (action.macro == null || action.macro.Length == 0) return;

            bool repeat = action.macroRepeat;
            bool hold = action.macroHold;

            do
            {
                token.ThrowIfCancellationRequested();

                int i = 0;
                while (i < action.macro.Length)
                {
                    token.ThrowIfCancellationRequested();

                    int value = action.macro[i];

                    // ディレイ（待機時間）の処理
                    if (value < 0)
                    {
                        int delayMs = -value;
                        if (delayMs > 0)
                        {
                            await Task.Delay(delayMs, token).ConfigureAwait(false);
                        }
                    }
                    else
                    {
                        // キー・マウス入力の処理
                        ProcessMacroCode(deviceIndex, value);
                    }

                    i++;
                }

                // リピートモード判定
                if (!repeat || token.IsCancellationRequested)
                {
                    break;
                }

                // ループ間の微小ディレイ
                await Task.Delay(1, token).ConfigureAwait(false);

            } while (repeat && !token.IsCancellationRequested);

            // hold 指定がなく通常終了した場合はキー解放
            if (!hold)
            {
                ReleaseAllPressedKeys(deviceIndex);
            }
        }

        private void ProcessMacroCode(int deviceIndex, int value)
        {
            // InputMethods を通じたキー・マウスシミュレーション
            // 既存 Mapping.cs の PlayMacroCodeValue のコード規約に準拠
            if (value >= 1000)
            {
                // キー解放コード (例: 1000 + scanCode)
                int scanCode = value - 1000;
                InputMethods.performSCKeyRelease((ushort)scanCode);
                lock (_lock)
                {
                    _pressedKeys[deviceIndex].Remove(scanCode);
                }
            }
            else if (value > 0)
            {
                // キー押下コード
                InputMethods.performSCKeyPress((ushort)value);
                lock (_lock)
                {
                    _pressedKeys[deviceIndex].Add(value);
                }
            }
        }

        private void ReleaseAllPressedKeys(int deviceIndex)
        {
            lock (_lock)
            {
                foreach (var scanCode in _pressedKeys[deviceIndex])
                {
                    try
                    {
                        InputMethods.performSCKeyRelease((ushort)scanCode);
                    }
                    catch { }
                }
                _pressedKeys[deviceIndex].Clear();
            }
        }

        public void Dispose()
        {
            for (int i = 0; i < MaxDevices; i++)
            {
                Stop(i);
                _macroSources[i]?.Dispose();
            }
        }
    }
}