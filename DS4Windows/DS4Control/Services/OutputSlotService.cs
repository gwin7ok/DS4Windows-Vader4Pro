using System;
using System.Collections.Generic;
using System.Linq;
using DS4Windows.DI;
using DS4Windows.Services;
using DS4WinWPF.DS4Control;

namespace DS4Windows
{
    public class OutputSlotService : IOutputSlotService
    {
        private readonly OutputSlotManager _slotManager;
        private readonly IOutputSlotStore _store;
        private readonly ControlService _control;
        private readonly object _syncLock = new object();
        public const int MAX_SLOTS = 8;

        public event EventHandler<OutputSlotChangedEventArgs> OutputSlotChanged;

        // Phase6-Step5-2: 孤立配列 _deviceTypes（Global/BackingStore と非連動で常に None を返していた）と、
        // その読み書き API（GetOutputDeviceType／SetOutputDeviceType）は削除した。
        // 出力デバイス種別の三態は、永続設定＝IProfileSettingsService.OutContType、
        // 実行時接続状態＝ActiveOutDevType、UI一時状態＝OutDevTypeTemp を参照すること。
        private readonly OutputDevice[] _outputDevices = new OutputDevice[MAX_SLOTS];

        public OutputSlotService(OutputSlotManager slotManager = null, IOutputSlotStore store = null, ControlService control = null)
        {
            _control = control ?? Program.rootHub;
            _slotManager = slotManager ?? _control?.OutputslotMan ?? new OutputSlotManager();
            _store = store ?? DS4WinWPF.AppHost.GetService<IOutputSlotStore>() ?? new OutputSlotStore();
        }

        public OutputDevice[] OutputDevices
        {
            get
            {
                lock (_syncLock)
                {
                    return _outputDevices;
                }
            }
        }

        public OutputDevice GetOutputDevice(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= MAX_SLOTS)
                return null;

            lock (_syncLock)
            {
                return _outputDevices[slotIndex];
            }
        }

        public bool IsSlotPlugin(int slotIndex)
        {
            var device = GetOutputDevice(slotIndex);
            return device != null;
        }

        public void SetOutputDevice(int slotIndex, OutputDevice outputDevice)
        {
            if (slotIndex < 0 || slotIndex >= MAX_SLOTS)
                return;

            lock (_syncLock)
            {
                _outputDevices[slotIndex] = outputDevice;
                if (AppLogger.IsTraceEnabled)
                    AppLogger.LogTrace($"[DI] OutputSlotService.SetOutputDevice: Slot {slotIndex} output device updated");
            }

            // Phase6-Step5-2: 種別は孤立配列からではなく None を通知する（従来も孤立配列の初期値 None が通知されていた）
            RaiseOutputSlotChanged(slotIndex, OutContType.None, outputDevice);
        }

        /// <summary>
        /// OutputSlotChanged イベントの発行（Phase6-Step5-2 で SetOutputDeviceType から分離）。
        /// deviceType は、プラグイン／アンプラグ操作で指定された種別であり、永続設定（OutContType）には書き込まない。
        /// </summary>
        private void RaiseOutputSlotChanged(int slotIndex, OutContType deviceType, OutputDevice outputDevice)
        {
            if (AppLogger.IsTraceEnabled)
                AppLogger.LogTrace($"[DI] OutputSlotService.OutputSlotChanged: Slot {slotIndex} = {deviceType}");
            OutputSlotChanged?.Invoke(this, new OutputSlotChangedEventArgs(slotIndex, deviceType, outputDevice));
        }

        // === Step 12 拡充: 実体 OutputSlotManager 連動操作 ===
        public IReadOnlyList<OutSlotDevice> OutputSlots
        {
            get
            {
                if (_slotManager?.OutputSlots != null)
                {
                    return _slotManager.OutputSlots.ToList().AsReadOnly();
                }
                return Array.Empty<OutSlotDevice>();
            }
        }

        public OutSlotDevice GetOutSlotDevice(int slotNumber)
        {
            if (slotNumber < 0 || slotNumber >= MAX_SLOTS) return null;
            return _slotManager?.GetOutSlotDevice(slotNumber);
        }

        // TODO(技術的負債・詳細は IOutputSlotService.PluginSlot のコメント参照):
        // 呼出元0件（2026-09-11確認）。実際のホットスワップは別経路(ScpUtil.cs PostLoadSnippet)で実現。
        public bool PluginSlot(int slotNumber, OutContType devType)
        {
            if (slotNumber < 0 || slotNumber >= MAX_SLOTS) return false;

            try
            {
                var slotDevice = GetOutSlotDevice(slotNumber);
                if (slotDevice == null) return false;

                // §5.5 最重要ガードレール: ViGEm ネイティブドライバ保護
                // ControlService の正規公開 API（AttachUnboundOutDev）を経由して安全にプラグイン
                if (_control != null)
                {
                    _control.AttachUnboundOutDev(slotDevice, devType);
                }

                RaiseOutputSlotChanged(slotNumber, devType, GetOutputDevice(slotNumber));
                return true;
            }
            catch (Exception ex)
            {
                if (AppLogger.IsTraceEnabled)
                    AppLogger.LogTrace($"[DI] OutputSlotService.PluginSlot failed: {ex}");
                return false;
            }
        }

        // TODO(技術的負債・詳細は IOutputSlotService.UnplugSlot のコメント参照):
        // 呼出元0件（2026-09-11確認）。実際のホットスワップは別経路(ScpUtil.cs PostLoadSnippet)で実現。
        public bool UnplugSlot(int slotNumber)
        {
            if (slotNumber < 0 || slotNumber >= MAX_SLOTS) return false;

            try
            {
                var slotDevice = GetOutSlotDevice(slotNumber);
                if (slotDevice == null) return false;

                // §5.5 最重要ガードレール: ViGEm ネイティブドライバ保護
                // ControlService の正規公開 API（DetachUnboundOutDev）を経由して安全にアンプラグ
                if (_control != null)
                {
                    _control.DetachUnboundOutDev(slotDevice);
                }

                SetOutputDevice(slotNumber, null);
                return true;
            }
            catch (Exception ex)
            {
                if (AppLogger.IsTraceEnabled)
                    AppLogger.LogTrace($"[DI] OutputSlotService.UnplugSlot failed: {ex}");
                return false;
            }
        }

        public bool LoadOutputSlots()
        {
            if (_slotManager == null || _store == null) return false;
            return _store.Load(_slotManager);
        }

        public bool SaveOutputSlots()
        {
            if (_slotManager == null || _store == null) return false;
            return _store.Save(_slotManager);
        }

        // TODO(技術的負債・Phase7 で解消予定、Phase6-Step5 決定2＝案X で温存、2026-09-24):
        // 以下 2 つの配列の実体は Global の静的配列（Global.outDevTypeTemp／Global.activeOutDevType）であり、
        // 本サービスは裏づけとして同じ配列参照を返しているだけである（状態の複製はしない）。
        // 配列は ScpUtil.cs の 8 箇所、ProfileEditor.xaml.cs／BindingWindowViewModel.cs／PressKeyViewModel.cs から直接
        // 読み書きされ、ControlService は配列参照を 1 回だけキャッシュする前提で使っている（ControlService.cs の ActiveOutDevType）。
        // Phase7 で、配列の所有を本サービスへ移し、Global 側を転送プロパティ（または削除）にする（案Y）。
        // 前提条件と詳細: docs-forDIMG/MadeByAgent/Phase6-Step5-Plan.md §9、Phase6-Step12-Plan.md §4。
        // 配列の参照は再代入しないこと（ControlService のキャッシュが無効になるため）。

        // ---- Phase5-Step13-6: Profile Editor で選択中の未確定(Temp)出力デバイスタイプ ----
        // 三態の「UI一時状態」。永続設定（IProfileSettingsService.OutContType）とは別物。
        public OutContType[] OutDevTypeTemp => Global.outDevTypeTemp;

        // ---- Phase5-Step13-7: スロット別・現在稼働中の出力デバイスタイプ ----
        // 三態の「実行時接続状態」。未接続時は None。永続設定（IProfileSettingsService.OutContType）とは別物。
        public OutContType[] ActiveOutDevType => Global.activeOutDevType;
    }
}