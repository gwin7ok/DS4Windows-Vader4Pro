# Phase6-Step10-5 計画書: 出力スロットの抜き差しを `IOutputSlotService` 経由にする

作成日: 2026-10-15
親計画: `Phase6-Step10-Plan.md` §8（決定5＝B。Step10-4 の後）、`Phase6-Step6-Plan.md` §3.1（差分の詳細）
前提: Step10-4 完了（`OutputSlotService` の依存は `Func<ControlService>` などで必須化済み。`Phase6-Step10-4-Plan.md` §25）

---

## 1. 目的
- `IOutputSlotService.PluginSlot`／`UnplugSlot` は、Phase5-Step12 で「UI から仮想スロットを抜き差しする正式な入口」として作られたが、呼出元が 0 件のまま残っている（規約 §2.4 の「使うべきもの」。Phase6-Step6 決定1＝案R）。
- 実際の呼び出し側は、`ControlService.AttachUnboundOutDev`／`DetachUnboundOutDev` を直接呼んでいる。これらを、サービス経由に切り替える。

## 2. 現行コードの確認（2026-10-15）
- **画面（出力スロット管理）**
  - `CurrentOutDeviceViewModel`: `OutSlot_PluginRequest`／`OutSlot_UnplugRequest` が、事前条件（接続状態と入力の割り当て）を確認し、`controlService.EventDispatcher.BeginInvoke` で `AttachUnboundOutDev`／`DetachUnboundOutDev` を呼ぶ。種別は `OutSlotDevice.CurrentType`。
  - `OutputSlotManagerControl.SetupDataContext(ControlService, OutputSlotManager)` が、生の `ControlService` を渡す（`MainWindow` から）。
- **UDP コマンド**（`MainWindow.xaml.cs`、`outputslot.<n>.unplug／plugds4／plugx360`）: 事前条件の確認もスレッドの切り替えもなく、`controlService.DetachUnboundOutDev`／`AttachUnboundOutDev` を直接呼ぶ。
- **既存の不具合の見込み（UDP）**: `DetachUnboundOutDev` は `slotDevice.OutputDevice` が null のとき（未接続のスロット）に、`dev.GetDeviceType()` で `NullReferenceException` になる。画面は「接続中」のときだけ呼ぶため問題が出ない。UDP の `unplug` は、未接続のスロットへ送ると例外になる可能性がある（上流から存在。回帰ではない）。
- **サービス（`OutputSlotService`）**: `PluginSlot`／`UnplugSlot` は、呼び出し元のスレッドで `Control.AttachUnboundOutDev`／`DetachUnboundOutDev` を呼び、`OutputSlotChanged` を発行する。事前条件の確認とスレッドの切り替えは無い。`Control` が null の場合も `true` を返す。

## 3. 方針（案）
- **V1（サービスを、画面と同等の挙動にする）**
  - 事前条件を確認する: `PluginSlot`＝未接続かつ入力未割り当て、`UnplugSlot`＝接続中かつ入力未割り当て。満たさなければ `false`。
  - `Control.EventDispatcher.BeginInvoke` で、`ControlService` の正規 API を呼ぶ（スレッドの切り替え）。`true` は「要求を受け付けた」の意味。
  - `OutputSlotChanged` は、ディスパッチされた処理が完了した時点で発行する（実際に抜き差しされた後）。
  - `Control` が null（ホストが無い場合）は、何もできないため `false` を返す（従来は `true`）。
  - ViGEm のネイティブドライバ保護（Phase5-Plan §5.5）: `OutputSlotManager` の非同期キュー（`DeferredPlugin`／`DeferredRemoval`）と破棄順序には触れない。
- **V2（画面）**: `CurrentOutDeviceViewModel` と `OutputSlotManagerControl` が、`ControlService` の代わりに `IOutputSlotService` を受け取り、`PluginSlot`／`UnplugSlot` を呼ぶ。スロット番号は、`SlotDeviceEntry` に `Index` を公開して得る。画面の更新（`OutputSlotManager.SlotAssigned`／`SlotUnassigned` の購読）は、変更しない。
- **V3（UDP）**: `MainWindow` の 3 つのコマンドを、`outputSlotService.PluginSlot`／`UnplugSlot` に切り替える（事前条件の確認とスレッドの切り替えが加わる。未接続のスロットへの `unplug` の例外が起きなくなる）。
- **テスト**
  - `OutputSlotService`: 範囲外、事前条件を満たさない場合に `false`、`Control` が無い場合に `false`、`OutputSlotChanged` が発行されないこと。
  - ソース走査: `CurrentOutDeviceViewModel`／`OutputSlotManagerControl`／`MainWindow` の UDP に、`AttachUnboundOutDev`／`DetachUnboundOutDev` の直接呼び出しが無いこと。
  - 退行ガード（`Step10FourGlobalRatchetGuardTests`）の基準値は、減る方向のため変更不要。
- **実機確認項目**: 出力スロット管理画面での Plug／Unplug（X360／DS4）、Permanent の保存と復元、UDP コマンド（`outputslot.1.plugx360` など。環境があれば）。

## 4. 決定事項（要承認）
- **V1**: サービスの挙動を、画面と同等にする（A）／温存する（B）。
- **V2**: 画面を、サービス経由へ切り替える（A）／温存する（B）。
- **V3**: UDP を、サービス経由へ切り替える（A）／温存する（B）。

## 5. 進捗
- 計画書の作成（2026-10-15）。
- **決定（2026-10-15）**: V1＝A、V2＝A、V3＝A（すべて推奨案）。
- **実装（2026-10-15）**
  - `OutputSlotService.PluginSlot`/`UnplugSlot`: 事前条件の確認、`ControlService.EventDispatcher` への切り替え、実行後の `OutputSlotChanged` の発行。`ControlService` が無い場合は `false`。`IOutputSlotService` のコメントを更新（接続済み）。
  - `CurrentOutDeviceViewModel`: `ControlService` の代わりに `IOutputSlotService` を受け取り、`PluginSlot`/`UnplugSlot` を呼ぶ。`SlotDeviceEntry.Index` を公開。
  - `OutputSlotManagerControl.SetupDataContext`: `IOutputSlotService` を受け取る。
  - `MainWindow` の UDP コマンド（`outputslot.<n>.unplug`/`plugds4`/`plugx360`）: `IOutputSlotService` 経由。
  - テスト: `OutputSlotService_PluginAndUnplug_AreRejectedWithoutControlService_AndRaiseNoEvent`、`OutputSlotOperations_GoThroughOutputSlotService_NotControlServiceDirectly`。
- **実機確認待ち**: 出力スロット管理画面での Plug/Unplug（X360/DS4）、Permanent の保存と復元、UDP コマンド（環境があれば）。
- **実機確認（2026-10-15）**: ビルド・テストビルド・テスト実行成功。画面の Plug／Unplug、Permanent の保存と復元は問題なし。UDP コマンドは環境が無く先送り（`Phase6-Step11-Plan.md` §3.3 に登録）。Step10-5 は完了。
