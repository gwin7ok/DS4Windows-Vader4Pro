# Phase6-Step12 完了報告書: 呼び出し元 0 件の旧静的シムの物理削除

作成日: 2026-10-17  
対象ブランチ: `For-DI-migration-work`  
計画書: `Phase6-Step12-Plan.md`（台帳・決定・各 Wave の記録は §0A）  
状態: 実装完了。最終のビルド・テスト（Wave 5）と、先送りの実機確認（U-8、U-9）の結果待ち

---

## 1. 結果の要約

- Step12-0 で現行コードから台帳を作り直し（`Global` 487 名のうち、不要 253 名・宣言 258 件）、5 つの Wave に分けて削除した。
- 削除した `Global` のメンバー: 251 名（Wave 1: 24、Wave 2: 150、Wave 3: 19、Wave 4: 58。宣言は 255 件）。`Global.cs` は 3,472 行 → 2,414 行（約 30% 減）。
- 削除したファイル: `BatteryReportArgs.cs`、`ControllerRemovedArgs.cs`、`DeviceStatusChangeEventArgs.cs`（3 型）。
- Wave 6: プロファイル用 DTO（`ProfileDTO` と専用の型、19 ファイル 4,298 行）と、`ProfileRepository` の DTO 側の読み書きを削除。プロファイルの保存形式は旧形式に統一した（方針は Plan §0A.18）。安全網として、旧形式の保存結果に 162 要素が含まれることを確認するテストを追加した。
- 削除した周辺: 互換ラッパー `DS4Windows.AppHost` の未使用 5 件、`IPathService.AppDataPath`／`PathService.AppDataPath` のセッター。
- ラチェット基準値（全域）: `Global.*` 780 → **747**、`AppHost` 48 → **40**、`Program.rootHub` 25 → **22**。`Global.cs` は (9, 11, 47) → **(6, 4, 26)**。
- 削除の過程で、**移行前後の回帰 1 件と、上流由来の不具合 1 件**を見つけて是正した（§3）。

## 2. Wave ごとの実績

- Wave 1（孤立コード 24 件＋引数型 3 ファイル）: 旧フィールド、上流由来の未使用イベント 4 種、重複実装の `NormalizeActionName` ほか。`Global` 47→45。
- Wave 2（ProfileSettings 系の互換アクセサ 150 名）: `Global` 45→38。テスト 2 件を削除、3 件を書き換え。
- Wave 3（`*Instance` のフォールバック群 19 件）: `Global` 38→26、`AppHost` 11→5、`rootHub` 9→6。テスト 5 件を削除、3 ファイルを書き換え。
- Wave 4（`BackingStore` 直転送ラッパー 58 名）: `AppHost` 5→4。テスト 7 ファイルを書き換え（削除なし）。
- Wave 5（互換 `AppHost` の未使用 5 件、`AppDataPath` のセッター）: テスト 1 件を削除。
- 各 Wave でビルド・テストが成功した（Wave 1〜4。ユーザーが実行）。Wave 5 は結果待ち。

## 3. 削除の過程で見つけた不具合と是正

- **回帰（移行前→移行後）: デバウンス時間が接続中に反映されない**。2026-09-14 の `9da60b85` で、プロファイル編集画面の保存が `Global.DebouncingMsHasChanged()` の呼び出しから `ApplyProfile` に置き換わり、デバウンス時間の更新が失われた。`ControlService.CheckProfileOptions` に `Debouncer.SetDuration` を追加して是正（案 A）。
- **上流由来の不具合: `Debouncer` が 0ms で生成されると、`SetDuration` が効かない**。`ProcessInput` の先頭の判定が、コンストラクタ引数（生成時の値）を見ていた。現在値のフィールド `_duration` を追加して是正。
- 新規テスト: `DebouncerSetDurationTests`（3 件）、`Step12DeadShimRemovalGuardTests`（4 件）。
- 実機確認は先送り（チェックリスト §11 の U-8）。

## 4. 保護した領域（削除しなかったもの）

- `BackingStore` の全フィールド・配列・プロパティ（防壁 1）。メソッド 10 件（旧保存系を含む）は要判断のまま Phase7 へ登録。
- Phase7 持ち越し（`Mapping` の静的 `profileSettings` 束縛、温存 3 件、`SyntheticDispatcher`、第 1 層）。
- Pre-Host 12 箇所、定数・純粋計算（`DS4_STICK_AXIS_MIDPOINT` を含む）、ViGEm クライアント依存 4 件。
- サービス実装の薄いシム（分類 D の `Global` の実体 144 名）。呼び出し元が残るため 0 件にならない。Phase7 でサービス側へ移管して消す。
- 再混入の防止: `Step12DeadShimRemovalGuardTests` が、削除した名前の再宣言と、防壁にあたる 18 名の欠落を検出する。

## 5. 削除したテスト

- `ProfileSettingsServiceTests`: `GlobalShim_ShouldSynchronizeWithService`、`TriggerSettings_ShouldShareBackingStoreWithGlobalShim`。
- `DeviceStateServiceTests`、`EnvironmentServiceTests`、`OutputSlotServiceTests`、`PathServiceTests`、`SpecialActionRepositoryTests`: 各 `GlobalShim_…` 1 件。
- `Phase6Step10GroupOneContractTests`: `PathService_AppDataPathSetter_DoesNotChangeGlobalAppDataPath`。
- いずれも、旧シムと新サービスの同期・一致だけを確認するテストで、旧シムの削除により目的が消えた。書き換えた分は、サービス側の確認として維持した。

## 6. Phase7 への引き継ぎ

`Phase6-Step12-Plan.md` §4 のとおり。主な項目: `BackingStore` の正本とメソッド 10 件、`Global` の実体 144 名、`Mapping` の instance 化、Pre-Host、`outDevTypeTemp`／`activeOutDevType`、K7-1、`Program.rootHub` 22 件、互換 `AppHost`（`Mapping.cs:86`）、`DS4WinWPF.AppHost.Dispose()`、DI インターフェースの 0 件メンバー約 52 件、K2、デバウンスの通知経路（`DebouncingMsHasChanged` ほか。U-8 の確認後に削除を判断）。

## 7. 最終確認

- Wave 1〜6 のビルド（警告 0）・テストビルド・テスト実行: すべて成功（ユーザーが各 Wave で実行。テスト件数の増減は、Plan §0A.11〜§0A.20 の各記録のとおり）。
- 実機確認 U-9（起動・接続・プロファイル切替・保存先の読み書き）: ○（2026-10-17）。起動ログの `Host initialized with runtime parser` が出ない件は、K7-1 の既存の状態で、Wave 3 の影響ではない（Plan §0A.16）。
- 実機確認 U-8（デバウンス）: 500ms で効くことを確認（○）。連打が押しっぱなしになるのは `Debouncer` の仕様（Plan §0A.19）。再起動後の値の保持は、未確認（チェックリスト U-8 に残す）。
- 完了後の追加対応（Plan §0A.21）: `OutputStick` の二重書き出しの修正（旧ファイルの救済つき）、デバウンスの通知経路の削除、K7-1 の暗黙構築の記録（原因特定用のログ）。再起動後の 500ms の保持は、確認済み。
- 判定: Step12 は完了。
