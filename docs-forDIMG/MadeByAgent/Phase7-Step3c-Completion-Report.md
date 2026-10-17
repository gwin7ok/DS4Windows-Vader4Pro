# Phase7-Step3c 完了報告書: プロファイル適用・プリセット・イベントの `Global` からの移管

作成日: 2026-10-17  
対象ブランチ: `For-DI-migration-work`  
計画書: `Phase7-Step3c-Plan.md`（各 Wave の記録は §7）  
状態: 実装完了。W1〜W5 のビルド（警告 0）・テストビルド・テスト実行は成功（ユーザーが各 Wave で実行）。W6 は文書のみ。実機確認 U-13 は、ユーザー判断で先送り。

---

## 1. 結果の要約

- `Global` に残っていた「プロファイルを適用する処理」を、DI サービスへ移した。
  - 適用の本体（読み込み、状態の更新、ログ、イベント、アクション関連の破棄）→ `ProfileApplicationService`。
  - プリセット 10 種 → `ProfilePresetLoader`（`BackingStore`／`IProfileSettingsService` を注入で受ける）。
  - `LoadTempProfile`、`SelectedProfileChanged`（インスタンスイベント）→ `ProfileRepository`。
  - `loggedInvalidActions` → `BackingStore` のフィールド。
  - `ProfileChangedNotification` → `AppSettingsService` が `BackingStore` を直接使う。
- `Global` から **24 名** を削除した（プリセット 10、`loggedInvalidActions`、プロファイル適用 12、`ProfileChangedNotification`）。契約からは、重複していた `IProfileRepository.LoadBlankProfile` を削除。
- ラチェット基準値（全域）: `Global.*` 516 → **488**。Phase7 開始時（733）からは −245。`AppHost` 40、`Program.rootHub` 22、`ServiceProviderHolder` 23 は変わらない。
- 先に特性化テストを書いてから移した（Q6）。期待値は移管の前後で変更していない。

## 2. Wave ごとの実績

- W1: 特性化テスト 9 件を追加（コードの変更なし）。通常・一時・距離プロファイル、存在しないプロファイル、スロット・名前の検査、プリセットによる一時状態のリセット、`SelectedProfileChanged` の回数と引数を固定。
- W2: プリセット 10 名を `ProfilePresetLoader` へ移し、`IProfileRepository.LoadBlankProfile` を削除（`ProfileSettingsViewModel` と `ViewModelFactory` が `IProfilePresetLoader` を受け取る）。`Global.*` 516 → 505。
- W3: 適用の核の準備。`loggedInvalidActions` を `BackingStore` へ、`ProfileRepository.LoadTempProfile` の実体化、`ProfileApplicationService` に核（`ApplyCore`）と Halt なしの入口を追加。特性化テストを新旧の両方に流した。505 → 499。
- W4: 呼び出し元の切り替え。`ProfileApplicationService`（3 箇所）、`ProfileSlotApplier`（Halt なしの入口へ委譲）、`Mapping.cs`（1 呼び出し）。`SelectedProfileChanged` をインスタンスイベントにし、`Global` の 12 名を削除。499 → 491。
- W5: `ProfileChangedNotification`。491 → 488。
- W6: 台帳 §6、モデル図 03／04、チェックリスト U-13、本報告書。

## 3. 設計上の判断と、その結果

- **Halt の扱い**（Q2＝A）: `IProfileApplicationService` に Halt なしの入口 `ApplyProfileWithoutHalt` を追加した。接続処理中の Halt の入れ子を避ける動作は、旧 `Global.ApplyProfileToSlot` と同じ（例外も握らない）。
- **`Mapping.cs`**（Q3＝A）: 呼び出し 1 箇所だけを `profileApplication.ApplyProfile(…, MappingAction, prolog)` に置き換えた。サービスは `MappingAction` のとき Halt しないため、呼び出し側の `HaltReportingRunAction` の内側で同じ順序で動く。従来との差は、スロット範囲・名前の検査が加わったことと、例外を握ってログに残すことだけ。
- **重複していた契約**（Q4＝A）: `IProfileRepository.LoadBlankProfile` は `IProfilePresetLoader.LoadBlank` と同じ処理だったため削除した。
- **イベント**（Q5＝A）: 発火元が `Global` の静的イベントから `ProfileRepository` のインスタンスに変わった。`sender` は `null` から `ProfileRepository` に変わったが、購読者（`ControllerListViewModel`）は引数だけを使う。
- **計画との差**: `SelectedProfileChanged` のインスタンスイベント化は、計画では W3 だったが W4 に移した。W3 の時点では、旧 `Global` の適用がまだ静的イベントを発火しており、転送を残さないと購読者に届かなくなるため。

## 4. 調査で判明した重要事項

- **`using static DS4Windows.Global` の無修飾の使用**: `Mapping.cs` と `DS4LightBar.cs` は、`Global` のメンバーを無修飾で呼ぶ。`Global.<名前>` を検索する走査には現れない（`tempprofileDistance`、`useTempProfile`、`DistanceProfiles`、`getTransitionedColor` ほか）。削除の前に、`using static` を持つファイルの無修飾の使用も確認した。これらの窓口は Step7〜9 まで `Global` に残る。
- **ソース文字列の正規表現のテスト**: 置換のたびに、期待する式の変更が必要になった（W2 で 2 件、W5 までの各 Wave で確認）。移管の前に `Assert.Matches(@"…")` 型のテストを検索するのが有効。
- **静的状態を使うテストの直列化**: `Global.appdatapath` を書き換える特性化テストと `PathServiceTests` を、同じ `ServiceProviderHolder` コレクションにして直列に実行する。
- **スクリプトの連鎖**: 置換スクリプトの修正に失敗したとき、後続のコマンドが未修正のスクリプトを実行した（`;` や改行での連結）。W2 では、バックアップから該当ファイルを戻して再実行した。以降は、修正が成功してから実行する。
- **回帰ではない**: 各 Wave の特性化テストと既存テストが成功している。機能・ログ・安全弁（Halt の使い分け、Fresh-press ガード）に変更はない。

## 5. 未完了・持ち越し

- **実機確認 U-13**（ユーザー判断で先送り。チェックリスト §11 に手順を記録済み）。`Mapping.cs` の呼び出しを変更したため、特殊アクションでのプロファイル切替（通常・一時・復帰）の確認が最も重要。U-11、U-12 も未。
- 静的呼び出しのまま残る: `ActionManager.*`（→ Phase8）、`Mapping.ClearKeyButtonControllersForDevice`（→ Step7〜9）。
- `Global` に残る窓口: `tempprofileDistance`／`useTempProfile`／`DistanceProfiles`（→ Step7〜9）、`ProfileSettingsServiceInstance`／`ProfileXmlStoreInstance`（→ Step4／Step5）。
- Step4 へ: `firstRun`／`runHotPlug`、`SetCulture`、環境・パスの実体、`Global.cs` に残るフォールバックの `new`。
- Step5 へ: `ProfileXmlStore.Load/SaveControllerConfigs`（`Program.rootHub` を走査）、Service Locator。
- Step6 へ: `outDevTypeTemp`／`activeOutDevType`、`outputKBMHandler`。

## 6. 次の Step

- **Step4: 起動・終了シーケンスの近代化**（`Phase7-Plan.md` §2）。ブートストラップ、Pre-Host、`AppHost` のオーバーロード、ホストの `Dispose`、`CreationNote`、`ReEnableDevice` の接続、環境・パスの実体の移管。調査と `Phase7-Step4-Plan.md` の作成から着手する。
