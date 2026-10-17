using System;
using System.Collections.Generic;
using DS4Windows;
using DS4WinWPF.DS4Control;

namespace DS4Windows.DI
{

    public interface IOutputSlotService
    {
        // === 既存互換メンバー ===
        // Phase7-Step2 W4: OutputDevices／GetOutputDevice／IsSlotPlugin（と実装の SetOutputDevice）は削除した。
        // 実装の内部配列は、本体で null 以外が設定されず、常に null／false を返す孤立実装だった。
        // 実体の入口は OutputSlots／GetOutSlotDevice（OutputSlotManager 連動）。
        // 参照: docs-forDIMG/MadeByAgent/Phase7-Step2-Plan.md §2.3

        // Phase6-Step5-2（2026-09-24）: GetOutputDeviceType／SetOutputDeviceType を削除した。
        // 両メソッドは Global/BackingStore と非連動の孤立配列 _deviceTypes を読み書きしており、
        // GetOutputDeviceType は常に None を返していた（Phase5-Step14-Issue7 で判明）。
        // 出力デバイス種別は、次の三態のいずれかを参照すること:
        //   永続設定（プロファイルに保存される値）: IProfileSettingsService.OutContType
        //   実行時接続状態（仮想バスに接続中の種別）: ActiveOutDevType（本インターフェース）
        //   UI一時状態（プロファイル編集画面の未確定値）: OutDevTypeTemp（本インターフェース）
        // 参照: docs-forDIMG/MadeByAgent/Phase6-Step5-Plan.md §2

        // DeviceType は、PluginSlot では指定された種別、UnplugSlot では None。
        // TODO(Phase7-Step2 W6、温存。Phase7-Step6 Q5＝A・Phase7-Step10b Q5＝A で再判断し、Phase8 以降の UI 通知の拡張点として温存): 本体の購読者がいない。PluginSlot／UnplugSlot の実行後に発行される
        // （2026-08-31 の初期契約。出力スロット画面の更新が OutputSlotManager 側のイベントで足りているかを、未確認のまま温存した）。
        // 出力デバイス種別の配列の所有権は Step6 で OutputSlotService へ移した。画面の更新経路を実機で確かめられるまで削除しない。
        // 削除するとトレースログ「[DI] OutputSlotService.OutputSlotChanged: Slot … = …」も消えるため、購読者を付けるのは機能拡張（Phase8 以降）として扱う。
        event EventHandler<OutputSlotChangedEventArgs> OutputSlotChanged;

        // === Step 12 拡充: 実体 OutputSlotManager 連動操作 ===
        IReadOnlyList<OutSlotDevice> OutputSlots { get; }
        OutSlotDevice GetOutSlotDevice(int slotNumber);

        // Phase6-Step10-5（接続済み）: PluginSlot／UnplugSlot は、UI（出力スロット管理画面）と UDP コマンドから、仮想スロットを
        // 抜き差しする正式な入口である（Phase5-Step12 で追加。Phase6-Step6 決定1＝案R。呼び出し側の切り替えを Step10-5 で実施）。
        // 新規コードは ControlService.AttachUnboundOutDev／DetachUnboundOutDev を直接呼ばず、本 API を使うこと。
        // 挙動: 事前条件（Plug＝未接続かつ入力未割り当て、Unplug＝接続中かつ入力未割り当て）を確認し、満たさなければ false。
        // 満たす場合は ControlService.EventDispatcher（UI スレッド）へ切り替えて ControlService の正規 API を呼び、true を返す
        // （true は「要求を受け付けた」の意味）。ControlService が無い場合は false。OutputSlotChanged は、実行後に発行する。
        // なお、プロファイル適用時の出力種別の切り替え（ホットスワップ）は別経路（ScpUtil.cs の PostLoadSnippet →
        // ControlService.UnplugOutDev／PluginOutDev）であり、本 API の対象外。
        bool PluginSlot(int slotNumber, OutContType devType);

        // Phase6-Step10-5: 上記 PluginSlot と同じ方針（実装は ControlService.DetachUnboundOutDev を中継する）。
        bool UnplugSlot(int slotNumber);
        // Phase7-Step5 W3 で接続した（Phase7-Step2 W4 の調査で「接続」と判断していた契約）: OutputSlots.xml の永続化の窓口。
        // App の起動後の読込（LoadOutputSlots）と、ControlService.ShutDown の保存（SaveOutputSlots）が、この契約を通す。
        // 旧 ControlService.LoadPermanentSlotsConfig と、ShutDown の OutputSlotPersist.WriteConfig の直接呼び出しは削除した。
        bool LoadOutputSlots();
        bool SaveOutputSlots();

        // ---- Phase5-Step13-6: Profile Editor で選択中の未確定(Temp)出力デバイスタイプ ----
        OutContType[] OutDevTypeTemp { get; }

        // ---- Phase5-Step13-7: スロット別・現在稼働中の出力デバイスタイプ ----
        OutContType[] ActiveOutDevType { get; }
    }
}