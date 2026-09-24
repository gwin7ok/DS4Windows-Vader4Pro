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

        // TODO(技術的負債・Phase6-Step6で再評価予定、2026-09-11時点): アプリ本体コードからの
        // 呼出元が0件であることを確認済み（テストを除く、
        // Phase5-Step14-Issue7-RootCause-and-CrossSetting-Audit-Report.md §5.2）。実装自体は
        // ControlService.AttachUnboundOutDev を経由して実際にViGEm仮想デバイスをプラグインする
        // 機能を持つが、どこからも呼ばれていない。現状、出力デバイス種別変更時の実際の
        // アンプラグ／再プラグイン（ホットスワップ）は、Global.ApplyProfile →
        // ScpUtil.cs の PostLoadSnippet（ControlService.UnplugOutDev/PluginOutDev）という
        // 別経路のレガシーシムで実現されており、本メソッドは一度も採用されなかった並行実装
        // である可能性が高い。削除／実配線の判断はPhase6-Step6で行う。
        bool PluginSlot(int slotNumber, OutContType devType);

        // TODO(技術的負債・Phase6-Step6で再評価予定、2026-09-11時点): 上記 PluginSlot と同様、
        // 呼出元0件（テストを除く）。実装は ControlService.DetachUnboundOutDev を経由するが、
        // 実際のアンプラグは Global.ApplyProfile 経由の ControlService.UnplugOutDev
        // （別経路のレガシーシム）で行われている。削除／実配線の判断はPhase6-Step6で行う。
        bool UnplugSlot(int slotNumber);
        bool LoadOutputSlots();
        bool SaveOutputSlots();

        // ---- Phase5-Step13-6: Profile Editor で選択中の未確定(Temp)出力デバイスタイプ ----
        OutContType[] OutDevTypeTemp { get; }

        // ---- Phase5-Step13-7: スロット別・現在稼働中の出力デバイスタイプ ----
        OutContType[] ActiveOutDevType { get; }
    }
}