# Phase7-Step3c 計画書: プロファイル適用・プリセット・イベントの `Global` からの移管

作成日: 2026-10-17  
対象ブランチ: `For-DI-migration-work`  
前提: `Phase7-Step3-Completion-Report.md` §5（Step3c への持ち越し）、`Phase7-Plan.md` §2（Step3 の 3c）  
状態: **実装完了（2026-10-17）。実機確認 U-13 は先送り。完了報告書あり。**  
ルール: ビルド・テストビルド・テスト実行・コミットはユーザーが行う。エージェントはローカルファイルの編集のみ。

---

## 0. 結論（先に）

- 目的: `Global` に残っている「プロファイルを適用する処理」を、`ProfileApplicationService` などの DI サービスへ移し、`Global` の該当メンバーを削除する。
- 対象は約 26 名（§1.2）。いずれも、呼び出し元は少ない（本体のサービス 4 つと `Mapping.cs` の 1 箇所、`BackingStore` の 1 箇所）。
- 難しい点は、メンバーの数ではなく **処理の中身**（適用の手順、Halt の入れ子、静的な `ActionManager`／`Mapping` の呼び出し、イベントの発火元の変更）。したがって **先に特性化テストを書いてから移す**（Q6）。
- 効果の見込み: `Global.*` 516 → 約 485（約 30 件減）。確定値は各 Wave の走査で出す。
- 今回の対象外（理由は §3 の Q1）: `DS4LightBar`／`Mapping` が静的に読む状態の窓口（`tempprofileDistance`、`useTempProfile`、`DistanceProfiles`）、永続化（`CreateAutoProfiles`、`SaveAction` ほか）。

## 1. 調査結果

### 1.1 現状の構造
- `Global.ApplyProfile`（本体）は、次の流れで動く。
  - 一時プロファイルなら `LoadTempProfile`、通常なら `ProfilePath[device]` を更新して `LoadProfile`。
  - 成功したら `CompleteProfileApplication`（約 95 行）を実行する: `SelectedProfile`／`OlderProfilePath` の更新、ログ、`LogProfileChanged`、`SelectedProfileChanged` イベントの発火、`ActionManager` の全エントリー破棄・デバイス状態破棄・事前確保、`Mapping.ClearKeyButtonControllersForDevice`、プロファイルのアクションの走査（トグル／押下の判定）。
- 入口は 3 系統ある。
  - `ProfileApplicationService.ApplyProfile`（Halt あり。`source == MappingAction` のときだけ Halt なし）と `ApplyFromAction`（Halt の内側で `Global.ApplyProfile` を直接呼ぶ）。呼び出しは `Global.ApplyProfile` の 3 箇所。
  - `ProfileSlotApplier.ApplyToSlot` → `Global.ApplyProfileToSlot`（Halt なし。`Program.rootHub` を使う）。呼び出し元: `ControlService`（接続時）と `MainWindow` の 2 箇所（手動）。
  - `Mapping.cs` の特殊アクション処理（`!handled` の経路）。`Task.Run` と `HaltReportingRunAction` の内側で `Global.ApplyProfile` を直接呼ぶ。
- 既存の `ProfileRepository.LoadProfile` は、`Global.LoadProfile` とは別物（ファイルの存在確認があり、読み込み結果を無視して `true` を返す）。そのため、置き換えには使えない。
- 2026-10-17 時点の Phase6 の TODO（K1: スロット 5〜8 の拒否など）は是正済み。残っているのは「接続処理中の Halt の入れ子」だけ。

### 1.2 対象メンバーと呼び出し元（`Global.*`、コメントを除く）
- **プロファイル適用**: `ApplyProfile`（`ProfileApplicationService` 3、`Mapping` 1、テスト 1）、`ApplyProfileToSlot`（`ProfileSlotApplier` 1、テスト 2）、`CompleteProfileApplication`（内部）、`LoadProfile`（`ApplyProfile` からのみ）、`LoadTempProfile`（`ProfileRepository` 1）。
- **プリセット**: `LoadBlankDevProfile`／`LoadBlankDS4Profile`／`LoadDefault…Profile` の 10 名（`ProfilePresetLoader` が各 1。`LoadBlankDevProfile` は `ProfileRepository` も 1）。
- **イベント**: `SelectedProfileChanged`（静的イベント）と `RaiseSelectedProfileChanged`。購読者は `ControllerListViewModel` だけ（`IProfileRepository` 経由）。
- **状態の窓口**: `ProfilePath`／`OlderProfilePath`／`SelectedProfile`（`Global` の内部、つまり `CompleteProfileApplication` 以外に使用者なし。テスト 4）、`loggedInvalidActions`（`BackingStore` 2、`ProfileRepository` 1、`Global` 2）、`ProfileChangedNotification`（`AppSettingsService` 3。実体は `ProfileSettingsServiceInstance` 経由）、`getProfileActions`（テスト 1）、`GetProfileAction`（`CompleteProfileApplication` のみ）。
- **対象外にするもの**（§3 Q1）: `tempprofileDistance`（`DS4LightBar` が `using static` で参照）、`useTempProfile`（`Mapping` が同様に参照）、`DistanceProfiles`（`DS4LightBar`）。これらは `ProfileSettingsServiceInstance`（Service Locator）への窓口で、静的な `DS4LightBar`／`Mapping` を instance 化する Step7〜9 で解消する。

### 1.3 移管先に必要な依存と、残る静的呼び出し
- `ProfileApplicationService` の新しい依存: `IProfileXmlStore`（読み込み）、`IProfileActionProvider`（プロファイルのアクションの取得。`Global.GetProfileAction` の置き換え）。パスは `IPathService`、配列とイベントは `IProfileRepository` が持つ（`ProfilePath`／`OlderProfilePath`／`SelectedProfile`／`RaiseSelectedProfileChanged`）。循環はない（`ControlService` は従来どおり `Func` で遅延解決）。
- 移した後も静的に残る呼び出し: `ActionManager.ClearAllEntries`／`ClearDeviceState`／`PreallocateForProfileApply`（Phase8 の範囲）、`Mapping.ClearKeyButtonControllersForDevice`（Step7〜9）、`AppLogger`。これらは TODO を付けて残す。
- `ProfilePresetLoader` の新しい依存: `BackingStore`、`IProfileSettingsService`。10 名の本体は同じ形（読み込み、既定の特殊アクション、エクストラ情報のキャッシュ、一時プロファイル状態のリセット）なので、1 つの私的ヘルパーにまとめられる。

## 2. 設計

- **適用の核を `ProfileApplicationService` に移す**（`Global.ApplyProfile` の TODO に書かれていた本来の姿）。
  - 私的な核 `ApplyCore`（読み込み → 完了処理）を置き、公開の `ApplyProfile`（Halt あり）と、新設の Halt なしの入口の両方がこれを呼ぶ。
  - 一時プロファイルの読み込みは `IProfileRepository.LoadTempProfile` を使い、`ProfileRepository.LoadTempProfile` の本体は `BackingStore.LoadProfile` ＋ 一時状態の更新（旧 `Global.LoadTempProfile` の処理）に置き換える。通常の読み込みは `IProfileXmlStore.LoadProfileXml` ＋ 一時状態のリセット（旧 `Global.LoadProfile` の処理）。
- **`ProfileSlotApplier`** は、`IProfileApplicationService` の Halt なしの入口へ委譲する（Q2）。
- **`SelectedProfileChanged`** は、`ProfileRepository` のインスタンスイベントにする（発火の呼び出しは `IProfileRepository.RaiseSelectedProfileChanged`）。`Global` の静的イベントは削除。`sender` が `null` から `ProfileRepository` に変わるが、購読者は `e` だけを使う。
- **プリセット**は `ProfilePresetLoader` に移す。`IProfileRepository.LoadBlankProfile` は、`IProfilePresetLoader.LoadBlank` と重複するため削除し、呼び出し元の `ProfileSettingsViewModel` を `IProfilePresetLoader` に切り替える（Q4）。
- **`loggedInvalidActions`** は `BackingStore` のインスタンスの `HashSet` にする（使用者が `BackingStore` と、クリアする側の読み込み処理だけのため）。
- **`ProfileChangedNotification`** は `AppSettingsService` が `_config.profileChangedNotification` を直接使う（`ProfileSettingsService` はすでに同じフィールドを使っている）。

## 3. ご判断をお願いしたい事項（選択肢と推奨）

- **Q1: スコープ** — (A・推奨) §1.2 の対象（約 26 名）のみ。`tempprofileDistance`／`useTempProfile`／`DistanceProfiles`（`DS4LightBar`／`Mapping` の静的参照）と、永続化（`CreateAutoProfiles`、`SaveAction` ほか）は対象外とし、それぞれ Step7〜9／Step10 に残す。(B) `DS4LightBar`／`Mapping` の窓口も含める（静的クラスの instance 化とぶつかるため非推奨）。
- **Q2: Halt の扱い** — (A・推奨) `IProfileApplicationService` に Halt なしの入口（仮称 `ApplyProfileWithoutHalt`）を追加し、`ProfileSlotApplier` が委譲する。接続処理中の Halt の入れ子を避ける現在の動作を維持できる。(B) 既存の `ApplyProfile` の Halt を、`source == ControlService` でも省略する条件を足す（暗黙の条件が増える）。
- **Q3: `Mapping.cs` の呼び出し** — (A・推奨) `Global.ApplyProfile` の呼び出しを、すでに `Mapping` が保持している `profileApplication.ApplyProfile(…, ProfileChangeSource.MappingAction, prolog)` に置き換える。サービスは `MappingAction` のとき Halt を省略するため、`Mapping` 側の `HaltReportingRunAction` の内側で同じ順序で動く。変わる点は、スロット範囲・名前の検査と、例外を握って記録することだけ。(B) 今回は `Mapping.cs` を触らず、`Global.ApplyProfile` を薄い転送として残す（Step7〜9 まで `Global` に残るメンバーが 1 つ増える）。
- **Q4: `IProfileRepository.LoadBlankProfile`** — (A・推奨) `IProfilePresetLoader.LoadBlank` と重複するため契約から削除し、`ProfileSettingsViewModel` を `IProfilePresetLoader` に切り替える（生成箇所 8 件の更新）。(B) 契約を残し、`ProfileRepository` が `IProfilePresetLoader` へ委譲する（`ProfileRepository` の生成箇所が増える）。(C) 重複する数行を `ProfileRepository` に書き写す。
- **Q5: `SelectedProfileChanged`** — (A・推奨) `ProfileRepository` のインスタンスイベントにし、`Global` の静的イベントを削除。(B) 静的イベントを残す。
- **Q6: 進め方** — (A・推奨) 先に特性化テストを書く（W1）。適用後の状態（`ProfilePath`、`SelectedProfile`、`OlderProfilePath`（一時プロファイルでは更新されない）、一時プロファイルの名前・使用・距離プロファイルの状態、イベントが 1 回だけ引数付きで発火、読み込み失敗時に何も変えない）を、現行の `Global.ApplyProfile` に対して固定してから移す。(B) 既存テストのみで進める（適用の核は既存テストが薄いため非推奨）。

## 4. Wave 計画（各 Wave ＝ 1 コミット。ビルド・テストはユーザーが実行）

- **W1: 特性化テスト**（コードの変更なし）
  - 現行の `Global.ApplyProfile`／`ApplyProfileToSlot`／`LoadTempProfile`／プリセット 1 つ（`LoadBlankDevProfile`）の結果を固定するテストを追加（一時ディレクトリにプロファイルを作成）。Global の静的状態（`appdatapath`）を保存・復元する。
- **W2: プリセットの移管**（Q4）
  - 10 名を `ProfilePresetLoader` に移し、`Global` から削除。`IProfileRepository.LoadBlankProfile` を削除し、`ProfileSettingsViewModel` を切り替える。ラチェット: `ProfilePresetLoader` 10→0、`ProfileRepository` −1。
- **W3: 適用の核の準備**（呼び出し元はまだ切り替えない）
  - `loggedInvalidActions` を `BackingStore` へ。`ProfileRepository.LoadTempProfile` の実体化。`SelectedProfileChanged` を `ProfileRepository` のインスタンスイベントに（Q5）。`ProfileApplicationService` に `ApplyCore` と Halt なしの入口を追加（`IProfileXmlStore`／`IProfileActionProvider` の依存を追加。生成箇所 7 件の更新）。この時点では `Global.ApplyProfile` も残し、W1 のテストを新旧の両方に流す。
- **W4: 呼び出し元の切り替えと `Global` の削除**
  - `ProfileApplicationService` の 3 箇所、`ProfileSlotApplier`（Q2）、`Mapping.cs` の 1 箇所（Q3）を切り替え。`Global` から `ApplyProfile`／`ApplyProfileToSlot`／`CompleteProfileApplication`／`LoadProfile`／`LoadTempProfile`／`ProfilePath` 系 3／静的イベントと `Raise`／`getProfileActions`／`GetProfileAction` を削除。
- **W5: `ProfileChangedNotification`**
  - `AppSettingsService` を `_config.profileChangedNotification` に切り替え、`Global` から削除。
- **W6: ガード・台帳・モデル図・完了報告書**
  - `Phase7Step3RemovedGlobalForwardersGuardTests` に名前を追記（または新しいガード）。ラチェットの基準値、台帳、モデル図（03、04）、チェックリスト U-13、`Phase7-Step3c-Completion-Report.md`。
- 各 Wave の共通作業: 削除の直前に、`Global.<名前>`、`DS4Windows.Global.<名前>`、`using static` の無修飾の使用（`Mapping.cs`、`DS4LightBar.cs`）、テストの参照、ソース文字列を正規表現で確認するテスト（`Assert.Matches(@"…Global\…")` 型）を再走査する。既存のコメントを消さない。

## 5. リスクと検証

- **適用の順序**: `CompleteProfileApplication` の手順（状態の更新 → ログ → イベント → `ActionManager`／`Mapping` の破棄 → アクションの走査）を一字一句保つ。W1 の特性化テストで、状態とイベントを固定する（`ActionManager`／`Mapping` の静的状態は、既存の方式で呼び出しが例外を出さないことだけを確認）。
- **Halt の入れ子**（Q2）: 接続処理中の `ApplyToSlot` が Halt を使わない現在の動作を保つ。
- **イベントの `sender` の変更**（Q5）: 購読者は `e` のみ使う。ログ（`Global_SelectedProfileChanged` ほか）に影響しない。
- **`Mapping.cs`**（Q3）: 入力処理の経路に近いため、変更は 1 呼び出しに限り、前後の `HaltReportingRunAction`、`Task.Run`、後続の `DispatchOrSetBeingTriggered` は触らない。
- **実機確認 U-13**（チェックリスト §11 に追記予定）: 特殊アクションでのプロファイル切替（通常・一時・復帰）、自動プロファイル、コントローラー接続時の適用、メイン画面での手動切替（`SelectedProfileChanged` による一覧の更新）、プロファイルエディターのプリセット読み込み。
- 持ち越し: 窓口の `tempprofileDistance` ほか（Step7〜9）、`ActionManager` の静的呼び出し（Phase8）。

## 6. 完了判定チェックリスト

- [x] W1〜W5 のビルド（警告 0）・テストビルド・テスト実行が成功している（W6 は文書のみ）。
- [x] §1.2 の対象（24 名。見積りの約 26 名のうち `ProfileChangedNotification` ほかを含む）が `Global` から削除され、各サービスが DI で受けた依存を使っている。
- [x] 特性化テストが、移管の前後で同じ結果を示している（W3 で新旧の両方に流し、W4 で移管先だけにした。期待値は W1 から不変）。
- [x] ラチェットの基準値が更新されている（`Global.*` 516 → 488）。
- [x] 台帳、モデル図、`Phase7-Status.md` が更新されている。
- [ ] 実機確認 U-13（ユーザー判断で先送り。チェックリスト §11 に記録済み）。
- [x] `Phase7-Step3c-Completion-Report.md` が作成されている。

---

## 7. 承認と実行記録

- 承認: 2026-10-17、Q1〜Q6 すべて推奨（A）で承認。
- **W1 実装済み（ビルド・テスト待ち）**: 新規 `Phase7Step3cApplyProfileCharacterizationTests`（9 件）。
  - 固定した内容: 通常プロファイルの適用（`ProfilePath`／`SelectedProfile`／`OlderProfilePath` の更新、一時状態のリセット、`SelectedProfileChanged` が 1 回・引数付きで発火）、一時プロファイルの適用（`OlderProfilePath` と `ProfilePath` は更新されない、一時状態の設定、名前に `distance` を含むと距離フラグ）、存在しないプロファイル（失敗、`SelectedProfile`／`OlderProfilePath` は不変、イベントなし。通常は `ProfilePath` だけ先に更新される従来の動作）、`LoadTempProfile` 単体、`ApplyProfileToSlot` のスロット・名前の検査と成功、`LoadBlankDevProfile` による一時状態のリセット。
  - 方法: 一時ディレクトリを `Global.appdatapath` に設定し、`Global.store.SaveProfile` で有効なプロファイルを作成。Host を構築して実物の `ControlService` を使い、`Program.rootHub` も一時的に差し替える。終了時にすべて復元する。
  - 副作用の対策: `Global.appdatapath` を書き換える `PathServiceTests` を、同じ `ServiceProviderHolder` コレクションに入れて直列化した。
  - 注意: このテストは W1 の時点で **現行コードに対して成功する** ことが前提（コードは変更していない）。失敗した場合は、テストの前提の誤りであり、移管の前に修正する。
- **W2 実装済み（ビルド・テスト待ち）**
  - `ProfilePresetLoader`: `BackingStore` と `IProfileSettingsService` を必須で受け取る。10 名の読み込みを、`BackingStore` の同名の読み込み＋共通の後処理 `CompleteLoad`（既定の特殊アクションの作成、エクストラ情報のキャッシュ、一時プロファイルの名前・使用・距離のクリア）に置き換えた（処理は旧 `Global` と同一。距離のクリアは `tempprofileDistance[...] = false` から `SetTempProfileDistance(..., false)` に変えたが、同じ配列への同じ書き込み）。
  - `Global`: プリセット 10 名を削除。`IProfileRepository.LoadBlankProfile` とその実装を削除（Q4＝A）。`ProfileSettingsViewModel` と `ViewModelFactory` に `IProfilePresetLoader` を追加し、`LoadBlankProfile(ControlService)` は `presetLoader.LoadBlank` を呼ぶ。
  - テスト: `TestRepositoryFactory.CreateProfilePresetLoader` を追加し、`ProfileSettingsViewModel` の生成 6 件と `ProfilePresetLoader` の生成 2 件を更新。`Phase6Step10GroupTwoContractTests` の委譲確認は新しい実装の形（`_config.<読み込み>` ＋ `CompleteLoad`）に、`Phase6Step8ContractExtensionTests` の `LoadBlankProfile` の確認は契約ごと削除。特性化テストのプリセットは `ProfilePresetLoader` 経由に変更（期待値は同じ）。ガードテストの名前一覧に 10 名を追記。
  - ラチェット基準値: `ProfilePresetLoader.cs` 10→0、`ProfileRepository.cs` 6→5（`Global.*` 合計 516→505）。
- **W3 実装済み（ビルド・テスト待ち）**: 適用の核の準備。呼び出し元はまだ切り替えていない（`Global.ApplyProfile` ほかは残っている）。
  - `loggedInvalidActions` を `BackingStore` のインスタンスのフィールドに移した（`Global` の静的フィールドを削除。`Global` の読み込み 2 箇所、`BackingStore` の 2 箇所、`ProfileRepository` の 1 箇所を切り替え）。
  - `ProfileRepository.LoadTempProfile` に、旧 `Global.LoadTempProfile` の処理を移した（`Global.LoadTempProfile` は W4 まで残す）。
  - `ProfileApplicationService` に `IProfileXmlStore`／`IProfileActionProvider`／`BackingStore` の依存を追加し、`ApplyCore`／`LoadNormalProfile`／`CompleteProfileApplication`（旧 `Global` から手順・ログ・順序を変えずに移した）と、Halt なしの入口 `ApplyProfileWithoutHalt` を追加。`IProfileApplicationService` に同メソッドを追加（テストの模擬 2 件にも実装）。
  - **計画との差**: `SelectedProfileChanged` のインスタンスイベント化は W4 に移した。W3 の時点では `Global.CompleteProfileApplication` がまだ静的イベントを発火するため、`ProfileRepository` の転送を残さないと購読者に届かなくなる。新しい `CompleteProfileApplication` は `_profileRepo.RaiseSelectedProfileChanged` を呼ぶ（W3 の時点では、これが `Global` の静的イベントを発火する）。
  - テスト: 特性化テストの主要 6 件を `[Theory]`（旧 `Global` と移管先の両方で同じ結果になること）に変更し、`ApplyProfileWithoutHalt` の検査テストを追加。`ProfileApplicationService` の生成 7 件を `TestRepositoryFactory.CreateProfileApplicationService` に置換。ガードテストに `loggedInvalidActions` を追記。
  - ラチェット基準値: `BackingStore` 168→166、`Global.cs` 26→24、`ProfileRepository` 5→3（`Global.*` 合計 505→499）。
- **W4 実装済み（ビルド・テスト待ち）**: 呼び出し元の切り替えと `Global` の旧メンバーの削除。
  - `ProfileApplicationService`: 3 箇所の `Global.ApplyProfile` を `ApplyCore` に切り替え（Halt の有無は従来どおり）。
  - `ProfileSlotApplier`: `IProfileApplicationService` を受け取り、スロット範囲・名前の検査のあと `ApplyProfileWithoutHalt` に委譲（Q2＝A。Halt なしの動作を維持）。
  - `Mapping.cs`: 特殊アクションの `!handled` 経路の `Global.ApplyProfile` を、すでに保持している `profileApplication.ApplyProfile(…, MappingAction, prolog)` に置換（Q3＝A。呼び出し 1 箇所だけ。前後の `HaltReportingRunAction`／`Task.Run`／後続の処理は変更なし）。
  - `ProfileRepository.SelectedProfileChanged` を、`Global` の静的イベントの転送から、リポジトリ自身のインスタンスイベントに変更（Q5＝A。W3 で見送った分）。
  - `Global`: `ApplyProfile`／`ApplyProfileToSlot`／`CompleteProfileApplication`／`LoadProfile`／`LoadTempProfile`／`ProfilePath`／`OlderProfilePath`／`SelectedProfile`／`SelectedProfileChanged`／`RaiseSelectedProfileChanged`／`getProfileActions`／`GetProfileAction` の 12 名と、領域マーカーを削除。
  - テスト: 特性化テストを移管先だけ（`ApplyProfileWithoutHalt`／`ApplyProfile` の両入口、`ProfileSlotApplier`、`ProfileRepository.LoadTempProfile`）に書き換え（期待値は W1 から変更なし）。`ControlServicePr2ShimEquivalenceTests` の `ProfileSlotApplier` の確認から旧 `Global` との比較を除去。`MappingStep3FinalGlobalReferenceGuardTests` は `Global.ApplyProfile(` の出現が 0 件に変更。ほか、`Global.ProfilePath` 系の参照を `Global.store.<field>` に書き換え。ガードテストの名前一覧に 12 名を追記。
  - ラチェット基準値: `Mapping.cs` 53→52、`ProfileApplicationService.cs` 3→0、`ProfileRepository.cs` 3→0、`ProfileSlotApplier.cs` 1→0（`Global.*` 合計 499→491）。
- **W5 実装済み（ビルド・テスト待ち）**: `AppSettingsService.ProfileChangedNotification` を `_config.profileChangedNotification` の直接参照に変更し、`Global.ProfileChangedNotification`（`ProfileSettingsServiceInstance` 経由の窓口）を削除。`ProfileSettingsService` が同じフィールドを使うため、値の実体は変わらない。ガードテストに名前を追記。ラチェット基準値: `AppSettingsService.cs` 11→8（`Global.*` 合計 491→488）。
- **U-13（実機確認）はユーザー判断で先送り**（2026-10-17）。チェックリスト §11 に「未（先送り）」として手順を記録した（U-14 は空き枠）。
- **W6 実施（文書）**: 台帳 §6、モデル図 03／04、チェックリスト U-13（W5 の時点で記入済み）、完了報告書、本チェックリスト。
