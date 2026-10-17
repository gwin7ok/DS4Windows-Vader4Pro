# フェーズ7 進捗管理文書: `Global`／`BackingStore` の解体、起動の近代化、`Mapping` の instance 化

最終更新日: 2026-10-17  
状態: **Step1・Step2・Step3・Step3c・Step4・Step5・Step6・Step7・Step8・Step9 完了。Step10 は 10a・10b・10c 完了（次は 10d の計画書の作成）。**  
計画書: `Phase7-Plan.md`。調査: `Phase7-0-Survey.md`。残存台帳: `Phase7-Residual-Ledger.md`

---

## 1. 全体進捗サマリ

| Step | 内容 | 状態 | 記録 |
|---|---|---|---|
| Phase7-0 | 調査と Step 分割案 | 完了（D1〜D6 承認、2026-10-17） | `Phase7-0-Survey.md` |
| Step1 | 整理と台帳の確定 | **完了**（2026-10-17。W1〜W4 のビルド・テスト成功、U-10 確認済み） | `Phase7-Step1-Plan.md`、`Phase7-Step1-Completion-Report.md` |
| Step2 | DI インターフェースの 0 件メンバー（49）の個別判断 | **完了**（2026-10-17。W1〜W7 のビルド・テスト成功。U-11 の実機確認は未。完了報告書あり。判断: 削除 45／接続 3／温存 1。Phase8 へ 12） | `Phase7-Step2-Plan.md` |
| Step3 | `BackingStore` の DI 化と `Global` の転送の削除 | **完了**（2026-10-17。W1〜W5 のビルド・テスト成功、W6 は文書。U-12 の実機確認は未。完了報告書あり） | `Phase7-Step3-Plan.md` |
| Step3c | プロファイル適用・プリセット・イベントの `Global` からの移管（Step3 から持ち越し） | **完了**（2026-10-17。W1〜W5 のビルド・テスト成功、W6 は文書。U-13 の実機確認は先送り。完了報告書あり） | `Phase7-Step3c-Plan.md`、`Phase7-Step3c-Completion-Report.md` |
| Step4 | 起動・終了シーケンスの近代化 | **完了**（2026-10-17。Q1〜Q7 推奨どおり承認。W1〜W4 のビルド・テスト成功、W5 は文書。U-14 の実機確認は未。完了報告書あり） | `Phase7-Step4-Plan.md`、`Phase7-Step4-Completion-Report.md` |
| Step5 | `Program.rootHub` と Service Locator の削除 | **完了**（2026-10-17。Q1〜Q9 推奨どおり承認、DS3 の不具合も修正。W1〜W5 のビルド・テスト成功、W6 は文書。U-15 の実機確認は未。完了報告書あり） | `Phase7-Step5-Plan.md`、`Phase7-Step5-Completion-Report.md` |
| Step6 | 出力デバイス種別の所有権の移動 | **完了**（2026-10-17。Q1〜Q5 推奨どおり承認。W1・W2 のビルド・テスト成功、W3 は文書。U-16 の実機確認は未。完了報告書あり） | |
| Step7 | `Mapping` の instance 化（1）特性化テストと状態の切り出し | **完了**（2026-10-17。Q1〜Q6 推奨どおり承認。W1〜W4 のビルド・テスト成功、W5 は文書。U-17 の実機確認は未。完了報告書あり） | |
| Step8 | `Mapping` の instance 化（2）instance 化と呼び出し元の切り替え | **完了**（2026-10-17。Q1〜Q9 推奨どおり承認。W1〜W4 のビルド・テスト成功、W5 は文書。U-18 の実機確認は未。完了報告書あり） | `Phase7-Step8-Plan.md`、`Phase7-Step8-Completion-Report.md` |
| Step9 | `Mapping` の instance 化（3）互換 `AppHost` の削除ほか | **完了**（2026-10-18。Q1〜Q5 推奨どおり承認。W1〜W3 のビルド・テスト成功、W4 は文書。U-19 の実機確認は未。完了報告書あり） | `Phase7-Step9-Plan.md`、`Phase7-Step9-Completion-Report.md` |
| Step10 | 仕上げ（`Global` の残り、永続化の切り出し、総合検証） | **実施中**（2026-10-18。Q1〜Q9 推奨どおり承認。10a・10b・10c 完了、10d〜10e は未着手） | `Phase7-Step10-Plan.md`、`Phase7-Step10a-Plan.md`、`Phase7-Step10a-Completion-Report.md` |

## 2. ラチェットの基準値（全域）

- 推移（`Global.*`／`AppHost`／`Program.rootHub`／`ServiceProviderHolder`）:
  - Phase6 完了時（2026-10-17）: 747／40／22／（未追跡）
  - Phase7-Step1 W1 後: 733／40／22／（未追跡）
  - Phase7-Step1 W4 後: 733／40／22／23
  - Phase7-Step2 W7 後: 723／40／22／23
  - Phase7-Step3 W5 後: 516／40／22／23
  - Phase7-Step3c W5 後: 488／40／22／23
  - Phase7-Step4 W5 後: 452／39／21／23（`rootHub` は基準値の過大分を直した）
  - Phase7-Step5 W6 後: 446／11／0／22
  - Phase7-Step6 W3 後: 438／11／0／22
  - Phase7-Step7 W5 後: 432／11／0／22
  - Phase7-Step8 W5 後: 399／5／0／17
  - Phase7-Step9 W4 後: 355／4／0／17
  - Phase7-Step10a W5 後: 107／4／0／17
  - Phase7-Step10b W5 後: 93／4／0／17
  - Phase7-Step10c W7 後（現在）: **69／4／0／17**
- ガード: `Step10FourGlobalRatchetGuardTests`（`Global.*`／`AppHost`／`rootHub`）、`Phase7Step1ServiceLocatorRatchetGuardTests`（`ServiceProviderHolder`）、`Phase7Step1DeadMethodGuardTests`（`BackingStore` の削除済みメソッド）。
- 目標値は、各 Step の計画書で確定し、`Phase7-Residual-Ledger.md` に反映する。

## 3. 各 Step の詳細進捗

### Phase7-Step1: 整理と台帳の確定【完了（2026-10-17）】
- W1: `BackingStore` の孤立メソッド 10 名（396 行）を削除。`Global.*` 747 → 733。
- W2: K2（`GetProfileActionIndexOf` が未検出で -1）と未使用変数の削除。
- W3: 不具合 B（アプリ設定の DTO 読込の後処理の欠落）の是正。
- W4: `ServiceProviderHolder` の追跡（23 件）。
- 実機確認 U-10: 偽の exe 名の保持は○。スキップしたバージョンは値の保持まで○（通知の抑制は U-7 とあわせて確認）。

### Phase7-Step2: DI インターフェースの 0 件メンバーの個別判断【完了（2026-10-17）】
- 走査の結果、本体の呼び出しが 0 件のメンバーは 61。うち `Actions/` の未配線の抽象 11 は Phase8 へ移す。残り約 50 が対象。
- 調査中に、`IDeviceStateService` の `SetDevice` に本体の呼び出しが無く、サービス内の配列が空のまま、`ConnectedControllersCount` などが常に 0 を返す構造であることを確認した（使うのは `ControllersViewModel.HasControllers` だけで、そのプロパティ自体の使用箇所が無い。実害は出ていない）。判断の詳細は `Phase7-Step2-Plan.md`。

### Phase7-Step3: `BackingStore` の DI 化と `Global` の転送の削除【完了（2026-10-17）】
- W1: `BackingStore` の登録、`ProfileXmlStore` の必須化、`ProfileRepository` への注入。
- W2〜W3: `AppSettingsService` の寸法・動作設定・通信・更新確認（`Global` の 55 名を削除）。
- W4: 処理を持つ転送（`FakeExeName`、UDP 平滑化と変更通知、`LastVersionChecked`）をサービスへ移管。
- W5: `ProfileRepository`、外観、自動プロファイル、接続フラグ、通知。`Global` の 16 名を削除。
- W6: 台帳、モデル図、U-12、完了報告書。`Global.*` 723 → 516。削除 76 名（計画の約 95 名のうち、他の呼び出し元があるものとプロファイル適用に絡むものは持ち越し）。
- 持ち越し: Step3c（プロファイル適用・プリセット・イベントの移管。別計画書）。詳細は `Phase7-Step3-Completion-Report.md` §5。

### Phase7-Step3c: プロファイル適用・プリセット・イベントの移管【完了（2026-10-17）】
- W1: 特性化テスト 9 件（コードの変更なし）。
- W2: プリセット 10 名を `ProfilePresetLoader` へ。重複する `IProfileRepository.LoadBlankProfile` を削除。
- W3: 適用の核の準備（`ProfileApplicationService.ApplyCore`、Halt なしの入口、`loggedInvalidActions` を `BackingStore` へ）。
- W4: 呼び出し元の切り替え（`ProfileSlotApplier`、`Mapping.cs` の 1 呼び出し）。`SelectedProfileChanged` をインスタンスイベントに。`Global` の 12 名を削除。
- W5: `ProfileChangedNotification`。W6: 台帳、モデル図、完了報告書。
- `Global.*` 516 → 488。削除 24 名。持ち越し: `tempprofileDistance` ほかの窓口（Step7〜9）、`ActionManager` の静的呼び出し（Phase8）。詳細は `Phase7-Step3c-Completion-Report.md` §5。

### Phase7-Step4: 起動・終了シーケンスの近代化【完了（2026-10-17）】
- W1: ViGEmBus／HidHide／FakerInput／OS の調べ物と状態を `EnvironmentService` へ（`Global` の 27 名を削除）。
- W2: `AppHost` の整理（`CreateHost(string[])` などを削除）、`-driverinstall` の明示的なホスト構築（`Global.Load` を削除）、`CleanShutdown` で `AppHost.Dispose()`。
- W3: 実行ファイル情報・保存場所・`FindConfigLocation` などを `PathService` へ（static な状態。`Global` に 4 つの窓口を残す）。`runHotPlug` は `AppSettingsService` の状態へ。
- W4: ブートストラップの `PathService`／`EnvironmentService` を起動の最初に作り、`CreateHost` へ渡して同じインスタンスを登録。`-reenableDevice` を `IDs4DeviceRegistry` 経由に。
- W5: 台帳 §7、モデル図、U-14、完了報告書。`Global.*` 488 → 452。計画との差（状態は static）と持ち越しは `Phase7-Step4-Completion-Report.md` §3・§5。

### Phase7-Step5: `Program.rootHub` と Service Locator の削除【完了（2026-10-17）】
- W1: DS3 のジャイロトリガーの不具合の修正（DI 移行による回帰。`DS4SixAxis.GyroControlsTriggerActive`）。
- W2: `Program.rootHub` の削除（`App` の volatile フィールド `_controlService`）、`Global.Save/LoadControllerConfigs` の削除。
- W3: `IOutputSlotService.LoadOutputSlots`／`SaveOutputSlots` の接続、`Global.ProfileXmlStoreInstance`／`NotificationServiceInstance` の削除（`AppLogger.NotificationService`）。
- W4: `ControlService` の `IElevatedProcessLauncher` 注入、`BackingStore.ProcessInspector` のプロパティ注入、`App` の Locator の置き換え。
- W5: `IMainWindowFactory`／`MainWindowFactory` の新設、`IEditorWindowFactory.CreateProfileEditor`。`MainWindow`（17）と `ProfileEditor`（4）の Locator を解消。
- W6: 台帳 §8、モデル図、U-15、完了報告書。`rootHub` 21 → 0、`AppHost.GetService` 39 → 11、`Global.*` 452 → 446。詳細は `Phase7-Step5-Completion-Report.md`。

### Phase7-Step6: 出力デバイス種別の所有権の移動【完了（2026-10-17）】
- W1: `OutputSlotService` が配列を持つ、`BackingStore.ActiveOutDevType`、`BackingStore` の読み取り 8 件の置き換え。W1 で、`BackingStore` の登録から `IOutputSlotService` を解決して循環（スタックオーバーフロー）したため、受け渡しを `IOutputSlotService` の登録側へ移した。
- W2: `Global` の 2 配列を削除、テスト更新、`Phase7Step6OutputTypeOwnershipGuardTests`、ラチェット（`BackingStore` 166 → 159、`OutputSlotService` 2 → 1）。
- W3: 台帳 §9、モデル図、U-16、完了報告書。`Global.*` 446 → 438。詳細は `Phase7-Step6-Completion-Report.md`。

### Phase7-Step7: `Mapping` の特性化テストと状態の切り出し【完了（2026-10-17）】
- W1: 特性化テスト（`MapCustom`／`Commit`／`MapClick`／`getBool*Mapping`／`HandleDeviceDisconnect`）と共通の土台（`MappingCharacterizationScope`、`RecordingVirtualKbmBase`）。本体は変更なし。
- W2: `SyntheticState` のトップレベル化、`SyntheticInputState`、`BackingStore` の逆参照を `ActionsLoaded` イベントへ（`ActionsLoadedRuntimeReset`）。
- W3: `MacroRuntimeState`。W2 のガードテストの正規表現の誤り（`\b`）も修正。
- W4: `StickMotionState`、入れ子の型 6 つのトップレベル化（`Click` を含む）。
- W5: 台帳 §10、モデル図、U-17、完了報告書。`Global.*` 438 → 432。詳細は `Phase7-Step7-Completion-Report.md`。

### Phase7-Step8: `Mapping` の instance 化と、呼び出し元の切り替え【完了（2026-10-17）】
- W1: `Mouse`／`ControlService` が保持クラスをコンストラクターで受ける。`MapClick`／`ResetRuntimeStateForKey` を `SyntheticInputState` へ。呼び出しが 0 件だった `Mapping` の内部メンバー 4 つを削除。
- W2: `IMapping`／`Mapping`（Singleton）、`KeyButtonControllerRegistry`。`static` の除去、`AppHost` 6・`ServiceProviderHolder` 5 の解消、呼び出し元の切り替え、テストの書き換え。
- W3: 出力 KBM ハンドラの実体を `OutputKBMHandlerAdapter` へ（`Global.outputKBMHandler`／`outputKBMMapping` の削除）。`SyntheticDispatcherService` と、静的な橋渡し。`ActionsLoadedRuntimeReset` を instance に。
- W4: 保持クラス 3 つの `Shared` を削除。
- W5: 台帳 §11、モデル図、U-18、完了報告書。`Global.*` 432 → 399、`AppHost` 11 → 5、`ServiceProviderHolder` 22 → 17。詳細は `Phase7-Step8-Completion-Report.md`。

### Phase7-Step9: 互換 `AppHost` の削除、`Mapping.cs` の `Global` 参照の再配置、K3【完了（2026-10-18）】
- W1: 互換 `AppHost`（`DI/Compat/AppHost.cs`）の削除。
- W2: `ControllerLimits`／`NumericUtil` の新設。`Mapping.cs` と保持クラス 3 つの `Global` 参照を切り替え（`Global` は別名・シムとして残す）。
- W3: K3 の二重実行防止を `MacroRuntimeState.TryBeginDispatch`／`EndDispatch` に包む（挙動は同一。恒久対応は Phase8）。
- W4: 台帳 §12、モデル図、U-19、完了報告書。`Global.*` 399 → 355、`AppHost` 5 → 4。詳細は `Phase7-Step9-Completion-Report.md`。実機確認 U-11〜U-19 はユーザー判断で先送り。

### Phase7-Step10: 仕上げ【実施中（2026-10-18）】
- `Phase7-Step10-Plan.md`（サブ Step 10a〜10e の構成と Q1〜Q9。推奨どおり承認）。各サブ Step は個別の計画書を作成してから進める。
- **10a: 完了**。W1 `BackingStore.cs` の定数（132）、W2 制御層ほか、W3 資源パス（`ResourcePaths`）、W4 `Clamp` の残り・テスト・`Global` の別名の削除、W5 文書。`Global.*` 355 → 107。詳細は `Phase7-Step10a-Completion-Report.md`。
- **10b: 完了**。W1 表と変換（`X360ControlTables`／`DS4ControlNames`）、W2 アイコンの表・色の関数（`TrayIconResources`／`ColorUtil`）、W3 契約の整理（`OnSubPropertyChanged` ほか）、W4 `UsingMaxControllers` の接続と孤立定数、W5 文書。`Global.*` 107 → 93。詳細は `Phase7-Step10b-Completion-Report.md`。
- **10c: 完了**。W1 ゴールデンテスト（11 ファイル）、W2 `Actions`／リンク／コントローラー設定、W3 アプリ設定（`AppSettingsXmlSerializer`）、W4 プロファイルの読込、W5 同 保存（`LegacyProfileXmlSerializer`）、W6 プリセット（`ProfilePresets`）、W7 文書。`BackingStore.cs` 6,522 → 1,660 行、`Global.*` 93 → 69。詳細は `Phase7-Step10c-Completion-Report.md`。
- **10d: 計画書作成済み・承認待ち（2026-10-18）**。`Phase7-Step10d-Plan.md`（`BackingStore` のコンストラクター注入と `Global` の削除。Q1〜Q9、W1〜W7）。
- 10e: 未着手（10d＝`BackingStore` の注入と `Global` の廃止、10e＝仕上げと総合検証）。

## 4. 実機確認の台帳（`Phase6-Step11-RealDevice-Verification-Checklist.md` §11）

- U-1〜U-7: 機材・環境が揃うまで未確認（リスク受容）。
- U-8、U-9: 確認済み（Phase6）。
- U-11（Step2 後）、U-12（Step3 後）、U-13（Step3c 後）、U-14（Step4 後）、U-15（Step5 後）、U-16（Step6 後）、U-17（Step7 後）、U-18（Step8 後）、U-19（Step9 後）、U-20（Step10c 後）: 未（U-13 はユーザー判断で先送り）。
- U-10: 確認済み（偽の exe 名の保持。スキップしたバージョンの値の保持）。通知の抑制は、新バージョンの公開時に確認（U-7 とあわせる）。
- 今後の追加候補: Step6 の後（スロットの付け外し）。

## 5. 持ち越し・保留事項

- K3（マクロの二重ガード）: Step9 で `MacroRuntimeState.TryBeginDispatch`／`EndDispatch` に包んだ（仕組みは変えていない）。恒久の再設計は Phase8（実行層の一本化）。
- K7-1: 解消済み（通常の起動は Phase6、`-driverinstall` 分岐は Phase7-Step4 W2）。
- `AppHost.CreationNote` と `[DI] Host creation path` のログ: ユーザー決定により残す。Step5 で Service Locator とあわせて、残すか判断する。
- DS3 のジャイロトリガー経路の不具合（コミット `78ae8687` からの回帰）: Phase7-Step5 W1 で修正済み（機材での実機確認は難しい可能性。U-15 の注記）。
- 起動直後のログローテーションとログの最小レベルが `BackingStore` の既定値になる課題（DI 移行前からの動作）: 記録のみ。直す場合は承認が必要（`Phase7-Step4-Completion-Report.md` §4）。
- 孤立定数 8 件（`BackingStore`）: Step10 の定数の再配置。
- Phase8 への申し送り: `Actions/` の `ServiceProviderHolder` 17 件、`Actions/` の未配線の抽象 11 メンバー（`IActionBinding`、`IKeyController`、`IActionRegistry`、`IControllerRegistry`、`IInputAction`、`IMacroPlayer`、`ITrigger`）。

## 6. 直近の次アクション

1. Step10d（`Phase7-Step10d-Plan.md`）の承認と実装。その後 10e。
2. 実機確認 U-11、U-12、U-13、U-14、U-15、U-16、U-17、U-18、U-19、U-20（任意のタイミング。U-14 は終了時のホスト破棄と初回起動の確認が中心）。
