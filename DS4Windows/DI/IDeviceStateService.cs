using System;
using DS4Windows;

namespace DS4Windows.DI
{

    public interface IDeviceStateService
    {
        // Phase7-Step2 W3: デバイスの一覧・接続状態・電池残量などの取得（Devices／GetDevice／IsDeviceConnected／ConnectedControllersCount／
        // GetDeviceMacAddress／GetConnectionType／GetBatteryLevel／SetDevice／DeviceStateChanged）は削除した。
        // 実装の内部配列は本体で一度も設定されず、常に「未接続」を返す孤立実装だった。接続中のデバイスの実体は
        // ControlService.DS4Controllers（IDeviceStateAccessor.GetController で参照する）。
        // 参照: docs-forDIMG/MadeByAgent/Phase7-Step2-Plan.md §2.2

        // ---- Phase6-Step2-2 (PR-2): 起動後の初回接続判定（Global への薄い委譲）----
        bool IsFirstConnection(int slotIndex);
        void MarkConnected(int slotIndex);

        // ---- Phase6-Step7-1: 起動時に全スロットの「起動後の初回接続」フラグを立て直す（Global への薄い委譲）----
        void ResetConnectionFlags();

        // ---- Phase6-Step2-4 (PR-4): デバイスのシリアル（MAC アドレス）変更通知（Global への薄い委譲）----
        void OnDeviceSerialChange(object sender, int slotIndex, string serial);
    }
}