# Phase6-Step5 完了報告書: OutputSlotService の孤立配列撤廃と UDP 診断コマンドの是正

作成日: 2026-09-24  
状態: **完了確定**（Step5-1・5-2 のビルド・テスト成功。Step5-3 の追加テストはユーザー確認待ち。実機確認は Step11 へ先送り）  
対象ブランチ: `For-DI-migration-work`  
計画書: `docs-forDIMG/MadeByAgent/Phase6-Step5-Plan.md`

---

## 1. 結果の要約
- UDP 診断コマンド `query.<device>.outconttype` が、常に `None` を返していた孤立 API ではなく、正本の永続設定（`IProfileSettingsService.OutContType`）を返すようになった。
- `OutputSlotService` の孤立配列 `_deviceTypes` と、その読み書き API（`GetOutputDeviceType`／`SetOutputDeviceType`）を、実装・インターフェースから削除した。出力デバイス種別を持つ場所は、三態（永続設定／実行時接続状態／UI 一時状態）だけになった。
- `OutDevTypeTemp`／`ActiveOutDevType` の裏づけの `Global` 静的配列は温存し（決定2＝案X）、理由と Phase7 での解消予定（案Y）を TODO コメントで明記した。
- コンストラクタ、`ServiceRegistration.cs`、`ScpUtil.cs` の静的初期化、モデル図の変更はなし。

## 2. 計画書からの主な変更点（Step5-0 の台帳再作成）
- 行番号・コードの実態が旧計画と異なっていた（UDP 診断コマンドは 1432 行、`_deviceTypes` の要素数は 8、コンストラクタの引数も異なる）。
- 旧計画の「`Get`／`Set` を永続設定へ委譲して `[Obsolete]`」は、`UnplugSlot` 経由で永続設定へ `None` を書き込む危険があるため不採用とし、両 API を削除した（決定1＝案B）。
- `IProfileSettingsService` の依存追加は不要になり、`ScpUtil.cs:863` の静的初期化とテスト 10 箇所の修正も不要になった。

## 3. 実施した変更
- **Step5-1**: `MainWindow.xaml.cs:1432` を `profileSettingsService.OutContType[tdevice].ToString()` に変更（`profileSettingsService` は既存のフィールド）。
- **Step5-2**:
  - `OutputSlotService.cs`: `_deviceTypes`、`GetOutputDeviceType`、`SetOutputDeviceType` を削除。`OutputSlotChanged` の発行を private ヘルパー `RaiseOutputSlotChanged` に分離し、`SetOutputDevice`（種別 `None`）、`PluginSlot`（引数の種別）、`UnplugSlot`（`SetOutputDevice(null)` 経由で `None`）から発行する。`OutDevTypeTemp`／`ActiveOutDevType` に TODO と三態の説明を付与。
  - `IOutputSlotService.cs`: 2 宣言と古い TODO を削除し、削除の経緯と三態の参照先を示すコメントを追加。
  - `ProfileSettingsViewModel.cs`、`SpecialActionsListViewModel.cs`: 説明コメントに「削除済み」を追記（コメントのみ）。
  - `OutputSlotServiceTests.cs`: 削除した API のテスト 5 件を、初期状態・`SetOutputDevice` のイベント発行・範囲外の安全性・グローバルシムのテストへ置き換え。
- **Step5-3**: テスト 2 ファイルの新設、文書の更新、本報告書の作成。

## 4. 挙動の変化
- UDP 診断コマンド `outconttype` の応答が、`None` からプロファイルの出力種別（`X360`／`DS4`）に変わる（バグの修正）。
- `UnplugSlot` が発行する `OutputSlotChanged` イベントが 2 回（旧種別と `None`）から 1 回（`None`）になった。イベントの購読者はテストのみで、`UnplugSlot` のアプリ本体からの呼出元は 0 件のため、実際の動作は変わらない。
- 接続時の出力種別の切り替え（ホットスワップ）経路（`ScpUtil.cs` の `PostLoadSnippet`、`ControlService.PluginOutDev`）には触れていない。

## 5. 追加・更新したテスト（`DS4WindowsTests`）
- `OutputSlotServiceTests`（更新）: 初期状態で出力デバイスなし、`SetOutputDevice` でイベントが種別 `None` で発行されること、範囲外でイベントが発行されないこと、`PluginSlot`／`UnplugSlot`／`GetOutputDevice` の範囲外の安全性、`Global.OutputSlotServiceInstance` の同一性（静的状態は元に戻す）。
- `OutputSlotServiceSsotTests`（新設、4 件）: 三態が別々の配列であること、永続設定を変えても実行時・一時状態が変わらないこと、実行時状態を変えても永続設定が変わらないこと、`UnplugSlot` が永続設定へ `None` を書き込まないこと。
- `MainWindowUdpOutContTypeGuardTests`（新設、2 件、ソース走査ガード）: `outconttype` の分岐が `profileSettingsService.OutContType[tdevice].ToString()` を返すこと、`MainWindow.xaml.cs` が `GetOutputDeviceType` を使わないこと。

## 6. 検証結果
- **ビルド・テスト（ユーザー確認、2026-09-24）**: Step5-1、Step5-2 の時点で、ビルド・テストビルド・テスト実行がすべて成功。Step5-3 の追加テストは確認待ち。
- **実機確認**: UDP クライアントの環境がないため、UDP 診断コマンドと、念のための接続時の出力種別切り替えを、`Phase6-Step11-Plan.md` §3.3 の先送り台帳へ登録した。

## 7. 持ち越し事項
- **K5-1（Phase7）**: `Global.outDevTypeTemp`／`Global.activeOutDevType` の所有権を `OutputSlotService` へ移す（案Y）。`Phase6-Step12-Plan.md` §4、`DI-App-Wide-Migration-Plan.md` §6.9.3 に登録済み。前提条件は `Phase6-Step5-Plan.md` §9。
- **K5-2（Step6）**: `PluginSlot`／`UnplugSlot`（呼出元 0 件）の扱い。`Phase6-Step6-Plan.md` の記述（「常に `false` を返すダミー」）は現行コードと一致しないため、Step6 着手時に台帳を再作成する（同計画書の冒頭に注記済み）。
- `Global.outDevTypeTemp` の直接参照（`ProfileEditor.xaml.cs:1263` は Step7b／Step8、`BindingWindowViewModel.cs:57`・`PressKeyViewModel.cs:221` は Step10）と、`ScpUtil.cs` の `Global.activeOutDevType` 8 箇所（Step12／Phase7）は、それぞれの Step で扱う。

## 8. 完了判定チェックリストの結果
- [x] UDP 診断コマンド `outconttype` が永続設定を参照している
- [x] 孤立配列と `GetOutputDeviceType`／`SetOutputDeviceType` を削除し、`OutputSlotChanged` イベントは維持
- [x] `OutDevTypeTemp`／`ActiveOutDevType` の裏づけと解消予定フェーズを TODO で明記
- [x] 三態の SSOT 境界をコメントとテストで固定
- [ ] Step5-3 の追加テストを含むビルド・テスト全件成功（ユーザー確認待ち）
- [x] 実機確認を Step11 の先送り台帳へ登録
- [x] `Phase6-Status.md`／`Phase6-Plan.md`／`Phase6-Step6-Plan.md`／`Phase6-Step11-Plan.md` を更新
