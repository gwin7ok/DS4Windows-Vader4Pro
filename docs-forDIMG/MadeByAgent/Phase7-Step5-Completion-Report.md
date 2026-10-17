# Phase7-Step5 完了報告書: `Program.rootHub` と Service Locator の削除

作成日: 2026-10-17  
対象ブランチ: `For-DI-migration-work`  
計画書: `Phase7-Step5-Plan.md`（Q1〜Q9 はすべて推奨どおり承認。DS3 の不具合の修正も承認）  
状態: 実装完了。W1〜W5 のビルド（警告 0）・テストビルド・テスト実行は成功（ユーザーが各 Wave で実行。W5 は 1 回の再ビルド）。W6 は文書のみ。実機確認 U-15 は未。

---

## 1. 結果の要約

- `Program.rootHub`（21 件）を **0** にし、`Program` クラスを削除した。`ControlService` は `App` の volatile なフィールドが持つ。
- Service Locator（`AppHost.GetService`）: 39 → **11**。残りは `Mapping`（Step7〜9）、互換ラッパー（Step9）、`AppHost` 自身、`Global.ProfileSettingsServiceInstance`、`App` の解決の窓口 1 件。`ServiceProviderHolder`: 23 → 22（残りは `Mapping` 5、`Actions/` 17）。
- `Global.*`: 452 → **446**。Phase7 開始時（733）からの累計は −287。
- 既存の不具合 1 件を是正した（DS3 のジャイロトリガー経路。DI 移行による回帰）。

## 2. Wave ごとの実績

- W1（Q3＝A）: DS3 のジャイロトリガー。`DS4SixAxis` の未使用のコンストラクターと null の `_controlService` を削除し、`GyroControlsTriggerActive`（`Func<int, bool>`）を受ける形にした。`ControlService` がデバイスをスロットへ接続するときに設定する。特性化テスト（`handleDS3Sixaxis`）を追加（テスト csproj に `AllowUnsafeBlocks`）。
- W2（Q1・Q2・Q7 の一部・Q8）: `Program.rootHub` の削除。`App` のフィールド化、`ServiceRegistration` の `rootHub ??` 削除、`Mapping`／`ProfileEditor`／`Global` のフォールバック削除、`Global.Save/LoadControllerConfigs` の削除（`ProfileXmlStore` が `BackingStore` を直接呼ぶ）。
- W3（Q9・Q7）: `IOutputSlotService.LoadOutputSlots`／`SaveOutputSlots` を接続し、`ControlService.LoadPermanentSlotsConfig` を削除。`Global.ProfileXmlStoreInstance` を削除、`NotificationServiceInstance` を `AppLogger.NotificationService` へ。
- W4（Q6）: `ControlService` に `IElevatedProcessLauncher` を注入。`BackingStore.ProcessInspector` のプロパティ注入。`App` の `LogSettingsApplierProxy`／`IEnvironmentService`（ブートストラップのインスタンス）／`ServiceProviderHolder`（`IControllerRegistry` を解決して保持）を置き換え。
- W5（Q4・Q5）: `IMainWindowFactory`／`MainWindowFactory` を新設し、`MainWindow` が 17 サービスをコンストラクター引数で受ける。`IEditorWindowFactory.CreateProfileEditor` を追加し、`ProfileEditor` が 4 サービスを引数で受ける。
- W6: ラチェットの確認（0／11／446）、台帳 §8、モデル図 03／04、U-15、本報告書。

## 3. 設計上の判断と、計画との差

- 計画（Q1〜Q9）どおりに実装した。計画との差は次のとおり。
  - `MainWindowFactory` は、メイン画面の生成の直前に解決する（`InitializePostHostServices` では解決しない）。窓口が受け取る多数のサービスが、設定の読込より前に生成されないようにするため。`ControlService` と `IAutoProfileService` は `Func` で遅延解決する。
  - `ProfileEditor` は `ControlService` を null 許容のまま受け取る（旧: `AppHost` で解決した値が null になり得た）。ほかの依存は必須（null なら `ArgumentNullException`）。
  - `AppHost.GetService` の残りの数は、計画の約 11 のとおり 11 件。
- **`MainWindow` のコンストラクター引数が 18 個**になった。Pure DI としては正しいが多い。Step10 で、画面ごとの責務の整理とあわせて見直す。
- **`MainWindowsViewModel` の `AppHost` 失敗時のフォールバック**（`new MainWindowsViewModel(...)`）は、必須引数になったため削除した（コンテナからは常に解決できる）。

## 4. 調査で判明した重要事項

- DS3 の不具合: `DS4SixAxis` に引数つきのコンストラクターを足したコミット `78ae8687`（2026-09-07）で、呼び出し側（`DS4Device`）が `new DS4SixAxis()`（引数なしが選ばれる）のままだったため、`_controlService` が常に null になっていた。DS3 の接続中にジャイロの処理が `NullReferenceException` になり得た。機材での再現は未確認。
- `GlobalPattern` の先読みにより、`DS4Windows.Global.X` と書く参照はラチェットで数えられない（Step4 から変わらない）。確認は各 Wave の回帰ガード（`Phase7Step5RootHubGuardTests`、`Phase7Step5ServiceLocatorGuardTests`、`Phase7Step5Ds3GyroTriggerTests`）で行った。
- 画面のソースを文字列で見るガード（`ProfileEditorGlobalReferenceGuardTests`、`ProfileEditorTargetDeviceGuardTests` ほか）は、生成の窓口を替えると期待する文字列も変わる。置換の前に、`new ProfileEditor(` のようなソース文字列を期待するテストを検索する（W5 で 2 件修正）。

## 5. 未完了・持ち越し

- **実機確認 U-15**（チェックリスト §11）。WPF の画面（`MainWindow`／`ProfileEditor`）の生成の窓口が変わったため、起動、プロファイル編集、トレイ、スロット管理、通知、終了の確認が中心。U-11〜U-14 も未。
- Step6 へ: 出力スロットの所有権（`outDevTypeTemp`／`activeOutDevType`）。
- Step7〜9 へ: `Mapping` の `AppHost` 6、`ServiceProviderHolder` 5、`Global.ProfileSettingsServiceInstance`、互換 `AppHost`、`CreationNote`。
- Phase8 へ: `Actions/` の `ServiceProviderHolder` 17。
- Step10 へ: `MainWindow` のコンストラクター引数の見直し、`Global` の窓口 4 つ、`BackingStore` のコンストラクター注入。
- ユーザー判断が残る事項: ログ設定の早期適用（`Phase7-Step4-Completion-Report.md` §4）を直すか。

## 6. 次の Step

- **Step6: 出力スロットの所有権の移動**（`Phase7-Plan.md` §2。`outDevTypeTemp`／`activeOutDevType` → `OutputSlotService`、約 45 参照）。調査と `Phase7-Step6-Plan.md` の作成から着手する。
