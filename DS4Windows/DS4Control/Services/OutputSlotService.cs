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
        // Phase6-Step10-4-3: 全ての依存を必須化（Program.rootHub／AppHost／new のフォールバックを廃止。Phase6-Step10 §8 の TODO を解消）。
        // ControlService は Func<IOutputSlotService> で本サービスへ依存している（Phase6-Step2 決定D1）ため、直接のコンストラクタ注入は
        // 循環する。Func による遅延解決で受け取る（決定 D4-3）。スロット管理（OutputSlotManager）の実体は ControlService が所有する。
        private readonly Func<OutputSlotManager> _slotManagerProvider;
        private readonly IOutputSlotStore _store;
        private readonly Func<ControlService> _controlProvider;
        private OutputSlotManager SlotManager => _slotManagerProvider();
        private ControlService Control => _controlProvider();
        private readonly object _syncLock = new object();
        public const int MAX_SLOTS = 8;

        public event EventHandler<OutputSlotChangedEventArgs> OutputSlotChanged;

        // Phase6-Step5-2: 孤立配列 _deviceTypes（Global/BackingStore と非連動で常に None を返していた）と、
        // その読み書き API（GetOutputDeviceType／SetOutputDeviceType）は削除した。
        // 出力デバイス種別の三態は、永続設定＝IProfileSettingsService.OutContType、
        // 実行時接続状態＝ActiveOutDevType、UI一時状態＝OutDevTypeTemp を参照すること。

        public OutputSlotService(Func<OutputSlotManager> slotManagerProvider, IOutputSlotStore store,
            Func<ControlService> controlProvider)
        {
            _slotManagerProvider = slotManagerProvider ?? throw new ArgumentNullException(nameof(slotManagerProvider));
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _controlProvider = controlProvider ?? throw new ArgumentNullException(nameof(controlProvider));
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
                if (SlotManager?.OutputSlots != null)
                {
                    return SlotManager.OutputSlots.ToList().AsReadOnly();
                }
                return Array.Empty<OutSlotDevice>();
            }
        }

        public OutSlotDevice GetOutSlotDevice(int slotNumber)
        {
            if (slotNumber < 0 || slotNumber >= MAX_SLOTS) return null;
            return SlotManager?.GetOutSlotDevice(slotNumber);
        }

        // Phase6-Step10-5: UI（出力スロット管理画面）・UDP コマンドから仮想スロットを抜き差しする正式な入口。
        // 画面の従来の挙動と同等にする: 事前条件（未接続かつ入力未割り当て）を確認し、ControlService.EventDispatcher
        // （UI スレッド）へ切り替えて ControlService の正規 API を呼ぶ。戻り値は「要求を受け付けたか」。
        // OutputSlotChanged は、抜き差しの要求を実行した後に発行する。
        // §5.5 最重要ガードレール: ViGEm ネイティブドライバ保護（OutputSlotManager の非同期キューと破棄順序には触れない）。
        public bool PluginSlot(int slotNumber, OutContType devType)
        {
            if (slotNumber < 0 || slotNumber >= MAX_SLOTS) return false;

            try
            {
                var slotDevice = GetOutSlotDevice(slotNumber);
                var control = Control;
                if (slotDevice == null || control == null) return false;

                if (slotDevice.CurrentAttachedStatus != OutSlotDevice.AttachedStatus.UnAttached ||
                    slotDevice.CurrentInputBound != OutSlotDevice.InputBound.Unbound)
                {
                    return false;
                }

                control.EventDispatcher.BeginInvoke((System.Action)(() =>
                {
                    try
                    {
                        control.AttachUnboundOutDev(slotDevice, devType);
                        RaiseOutputSlotChanged(slotNumber, devType, null);
                    }
                    catch (Exception ex)
                    {
                        if (AppLogger.IsTraceEnabled)
                            AppLogger.LogTrace($"[DI] OutputSlotService.PluginSlot dispatch failed: {ex}");
                    }
                }));
                return true;
            }
            catch (Exception ex)
            {
                if (AppLogger.IsTraceEnabled)
                    AppLogger.LogTrace($"[DI] OutputSlotService.PluginSlot failed: {ex}");
                return false;
            }
        }

        // Phase6-Step10-5: PluginSlot と同じ方針。事前条件は「接続中かつ入力未割り当て」。
        public bool UnplugSlot(int slotNumber)
        {
            if (slotNumber < 0 || slotNumber >= MAX_SLOTS) return false;

            try
            {
                var slotDevice = GetOutSlotDevice(slotNumber);
                var control = Control;
                if (slotDevice == null || control == null) return false;

                if (slotDevice.CurrentAttachedStatus != OutSlotDevice.AttachedStatus.Attached ||
                    slotDevice.CurrentInputBound != OutSlotDevice.InputBound.Unbound)
                {
                    return false;
                }

                control.EventDispatcher.BeginInvoke((System.Action)(() =>
                {
                    try
                    {
                        control.DetachUnboundOutDev(slotDevice);
                        RaiseOutputSlotChanged(slotNumber, OutContType.None, null);
                    }
                    catch (Exception ex)
                    {
                        if (AppLogger.IsTraceEnabled)
                            AppLogger.LogTrace($"[DI] OutputSlotService.UnplugSlot dispatch failed: {ex}");
                    }
                }));
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
            if (SlotManager == null || _store == null) return false;
            return _store.Load(SlotManager);
        }

        public bool SaveOutputSlots()
        {
            if (SlotManager == null || _store == null) return false;
            return _store.Save(SlotManager);
        }

        // Phase7-Step6 W1（Q1＝A）: 2 つの配列の実体を本サービスが持つ（旧 Global.outDevTypeTemp／Global.activeOutDevType）。
        // 本サービスは Singleton なので、プロセス内で 1 つの配列になる（従来の静的配列と同じ）。要素の読み書きにロックはない（従来どおり）。
        // 配列の参照は再代入しないこと（ControlService.ActiveOutDevType のキャッシュが無効になるため）。
        // BackingStore は、ServiceRegistration の登録を通じて ActiveOutDevType の同じ配列を受け取る。
        private readonly OutContType[] _outDevTypeTemp = CreateFilled(OutContType.X360);
        private readonly OutContType[] _activeOutDevType = CreateFilled(OutContType.None);

        private static OutContType[] CreateFilled(OutContType value)
        {
            var array = new OutContType[ControllerLimits.TestProfileItemCount];
            for (int i = 0; i < array.Length; i++) array[i] = value;
            return array;
        }

        // ---- Phase5-Step13-6: Profile Editor で選択中の未確定(Temp)出力デバイスタイプ ----
        // 三態の「UI一時状態」。永続設定（IProfileSettingsService.OutContType）とは別物。初期値は全スロット X360。
        public OutContType[] OutDevTypeTemp => _outDevTypeTemp;

        // ---- Phase5-Step13-7: スロット別・現在稼働中の出力デバイスタイプ ----
        // 三態の「実行時接続状態」。未接続時は None。永続設定（IProfileSettingsService.OutContType）とは別物。
        public OutContType[] ActiveOutDevType => _activeOutDevType;
    }
}