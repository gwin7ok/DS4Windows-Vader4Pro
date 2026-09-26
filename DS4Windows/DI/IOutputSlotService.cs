using System;
using System.Collections.Generic;
using DS4Windows;
using DS4WinWPF.DS4Control;

namespace DS4Windows.DI
{
    public class OutputSlotChangedEventArgs : EventArgs
    {
        public int Slot { get; }
        public int SlotIndex => Slot;
        public OutContType DeviceType { get; }
        public OutputDevice OutputDevice { get; }

        public OutputSlotChangedEventArgs(int slot, OutContType deviceType, OutputDevice outputDevice = null)
        {
            Slot = slot;
            DeviceType = deviceType;
            OutputDevice = outputDevice;
        }

        public OutputSlotChangedEventArgs(int slotIndex, OutputDevice outputDevice)
            : this(slotIndex, OutContType.None, outputDevice)
        {
        }
    }

    public interface IOutputSlotService
    {
        // === 既存互換メンバー ===
        OutputDevice[] OutputDevices { get; }
        OutputDevice GetOutputDevice(int slotIndex);
        bool IsSlotPlugin(int slotIndex);

        // Phase6-Step5-2（2026-09-24）: GetOutputDeviceType／SetOutputDeviceType を削除した。
        // 両メソッドは Global/BackingStore と非連動の孤立配列 _deviceTypes を読み書きしており、
        // GetOutputDeviceType は常に None を返していた（Phase5-Step14-Issue7 で判明）。
        // 出力デバイス種別は、次の三態のいずれかを参照すること:
        //   永続設定（プロファイルに保存される値）: IProfileSettingsService.OutContType
        //   実行時接続状態（仮想バスに接続中の種別）: ActiveOutDevType（本インターフェース）
        //   UI一時状態（プロファイル編集画面の未確定値）: OutDevTypeTemp（本インターフェース）
        // 参照: docs-forDIMG/MadeByAgent/Phase6-Step5-Plan.md §2

        // DeviceType は、PluginSlot では指定された種別、SetOutputDevice／UnplugSlot では None。
        event EventHandler<OutputSlotChangedEventArgs> OutputSlotChanged;

        // === Step 12 拡充: 実体 OutputSlotManager 連動操作 ===
        IReadOnlyList<OutSlotDevice> OutputSlots { get; }
        OutSlotDevice GetOutSlotDevice(int slotNumber);

        // TODO(技術的負債・未接続の DI 契約・Phase6-Step10 §8 で接続予定。Phase6-Step6 決定1＝案R、2026-09-24):
        // PluginSlot／UnplugSlot は、UI（出力スロット管理画面）から仮想スロットを抜き差しする正式な入口として
        // Phase5-Step12（2026-09-05）で追加された。当時の計画（Phase5-Step12-Plan.md §1.5）は、UI 側を本 API 経由へ
        // 切り替える予定だったが、その切り替えが未実施のため、アプリ本体からの呼出元は現在 0 件である（テストのみ）。
        // 現状の呼び出し側（CurrentOutDeviceViewModel、MainWindow の UDP コマンド）は ControlService.AttachUnboundOutDev／
        // DetachUnboundOutDev を直接呼んでいる。全体4層モデル・Pure DI の方針では本 API 経由が正しいため、
        // 削除も [Obsolete] 化もしない。新規コードは ControlService の API を直接呼ばず、本 API を使うこと。
        // なお、プロファイル適用時の出力種別の切り替え（ホットスワップ）は別経路（ScpUtil.cs の PostLoadSnippet →
        // ControlService.UnplugOutDev／PluginOutDev）であり、本 API の対象外。
        // 接続時に是正する差分（詳細: docs-forDIMG/MadeByAgent/Phase6-Step6-Plan.md §3.1）:
        //   1. スレッド: 画面側は ControlService.EventDispatcher へディスパッチしているが、本 API は呼び出し元のスレッドで実行する
        //   2. 事前条件と種別: 画面側は接続状態・入力割り当てを確認し、種別に OutSlotDevice.CurrentType を使う
        //   3. 依存: 実装が ControlService を Program.rootHub から取得している（循環依存を避けた Pure DI 化が必要）
        //   4. イベント: 画面は OutputSlotManager.SlotAssigned／SlotUnassigned を購読しており、OutputSlotChanged の購読者はテストのみ
        bool PluginSlot(int slotNumber, OutContType devType);

        // TODO(技術的負債・未接続の DI 契約・Phase6-Step10 §8 で接続予定): 上記 PluginSlot と同じ経緯・方針。
        // 実装は ControlService.DetachUnboundOutDev を中継する。
        bool UnplugSlot(int slotNumber);
        bool LoadOutputSlots();
        bool SaveOutputSlots();

        // ---- Phase5-Step13-6: Profile Editor で選択中の未確定(Temp)出力デバイスタイプ ----
        OutContType[] OutDevTypeTemp { get; }

        // ---- Phase5-Step13-7: スロット別・現在稼働中の出力デバイスタイプ ----
        OutContType[] ActiveOutDevType { get; }
    }
}