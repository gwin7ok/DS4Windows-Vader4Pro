# Phase7-Step5 計画書: `Program.rootHub` と Service Locator の削除

作成日: 2026-10-17  
対象ブランチ: `For-DI-migration-work`  
前提: `Phase7-Plan.md` §2（Step5）、`Phase7-0-Survey.md` §1.5・F-A、`Phase7-Step4-Completion-Report.md` §5、`Phase7-Residual-Ledger.md` §2・§7  
状態: **Q1〜Q9 を推奨どおり承認（2026-10-17。DS3 の修正も承認）。W1〜W6 完了。結果は `Phase7-Step5-Completion-Report.md`。**  
ルール: ビルド・テストビルド・テスト実行・コミットはユーザーが行う。エージェントはローカルファイルの編集のみ。

---

## 0. 結論（先に）

- 目的: `Program.rootHub`（21 件）を 0 にし、Service Locator（`AppHost.GetService` 39 件）のうち、`Mapping`（Step7〜9）と `Actions/`（Phase8）以外を、コンストラクター注入または `App` の保持へ置き換える。
- 目標値（Step5 完了時）:
  - `Program.rootHub`: 21 → **0**（`Program` クラスごと削除）。
  - `AppHost.GetService|Services`: 39 → **約 11**（`Mapping` 6、互換ラッパー 1、`AppHost` 自身 2、`Global.ProfileSettingsServiceInstance` 1、`App` の解決の窓口 1）。
  - `ServiceProviderHolder`: 23 → 22（`App` の 1 件を解消。`Mapping` 5 は Step7〜9、`Actions/` 17 は Phase8）。
  - `Global.*`: 452 → 約 449。
- 調査中に、**既存の不具合 1 件**（DS3 コントローラーのジャイロトリガー経路）を発見した（§1.6）。DI 移行前は動いていた。修正は承認が必要（Q3）。
- 実機確認 U-15 が必要（起動、プロファイル編集画面、トレイ、スロット管理、DS3 は機材があれば）。

## 1. 調査結果

### 1.1 `Program.rootHub`（21 件）の使用箇所（コメントを除く）
- `App.xaml.cs` 12: `LoggerHolder` の生成（423）、起動後の `LogDebug`／`CheckHidHidePresence`／`LoadPermanentSlotsConfig`（547・551・554）、`PrepareAbort`（596・609。未処理例外の処理）、`ControlService` の解決の結果の代入と null 検査（835・836）、`CleanShutdown` の停止（1096〜1103）。
- `Global.cs` 5: `fallbackProfileSettingsService` のラムダ（101）、`SaveControllerConfigs`／`LoadControllerConfigs` の引数なしの経路（1028・1029・1040・1041）。
- `ServiceRegistration.cs` 1（123）: `Program.rootHub ?? new ControlService(...)`。`App` が解決の結果を `rootHub` へ代入するため、循環に近い構造。
- `Mapping.cs` 1（6477）: `IDeviceStateAccessor`（`ControlService` 自身を返す登録）で取れなかったときのフォールバック。
- `ProfileEditor.xaml.cs` 1（314）: `AppHost` で解決した値の `??` フォールバック。
- `DS4SixAxis.cs` 1（51）: §1.6 の不具合に関係する。
- `Program.cs` は `rootHub` のフィールド 1 つだけのクラス。

### 1.2 `rootHub` を介した暗黙の共有状態
- `ControlService` は別スレッド（`controlThread`）で生成され、そのスレッドが `rootHub` へ代入する（呼び出し元は `IsAlive` を待つ）。この生成の仕方（別スレッドで解決）は変えない。保持先を `App` のフィールドに替えるだけで、可視性は同じ（スレッド終了を待ってから読む）。
- `Global.ProfileSettingsServiceInstance` のフォールバック（`new ProfileSettingsService(..., () => Program.rootHub)`）は、コンテナが無いテスト用。本体ではコンテナがあり、使われない。`ProfileSettingsService` は `_controlProvider()?.` と null 安全に使う。
- テストの側: `TestRepositoryFactory`（4 箇所）が `Program.rootHub` を既定値に使う。`Phase7Step3cApplyProfileCharacterizationTests` と `ServiceRegistrationArgumentParserTests` が `rootHub` を代入・退避する。

### 1.3 `AppHost.GetService`（39 件）の内訳
- `MainWindow.xaml.cs` 17: コンストラクターが 15 サービス（`IProfileSettingsService`、`IAppSettingsService`、`IProfileXmlStore`、`IEditorWindowFactory`、`IPathService`、`IEnvironmentService`、`IAppearanceSettingsService`、`IProfileApplicationService`、`IOutputSlotService`、`IProfileRepository`、`INotificationService`、`ControlService`、`IProfileSlotApplier`、`MainWindowsViewModel`、`ILogSettingsApplier`）に加え、`IAutoProfileService`・`IViewModelFactory` を解決する。
- `Mapping.cs` 6: 静的なフィールド初期化子（`profileSettings`、`profileApplication`）、`VirtualKBM`、`IProcessLauncher`、`IProfileSwitcher`、`IDeviceStateAccessor`。→ Step7〜9。
- `App.xaml.cs` 4: `LogSettingsApplierProxy`（425）、`ResolvePostHostService`（579。Post-Host の 7 サービスの解決の窓口）、`IEnvironmentService`（726。`-driverinstall` の `WelcomeDialog` へ渡す）、`ControlService`（835）。
- `ProfileEditor.xaml.cs` 4: `IProfileRepository`、`IProfileSwitcher`、`ControlService`、`IViewModelFactory`。生成は `MainWindow.xaml.cs:1945` の 1 箇所で、`IEditorWindowFactory` を既に渡している。
- `Global.cs` 3: `ProfileSettingsServiceInstance`、`ProfileXmlStoreInstance`、`NotificationServiceInstance`（いずれも「コンテナがあれば解決、なければフォールバック」）。
- `ControlService.cs` 1（718: `IElevatedProcessLauncher`。フォールバックの `Process.Start` あり）、`BackingStore.cs` 1（2823: `IProcessInspector`。フォールバックの `Process.GetProcesses` あり）。
- ほか: `DI/Compat/AppHost.cs` 1（互換ラッパー。Step9 で削除）、`DI/AppHost.cs` 2（自身のログ文）。

### 1.4 `Global` の `*Instance` 3 つ
- `ProfileXmlStoreInstance`: 本体の使用者が **0 件**（テストの `TestRepositoryFactory` だけ）。
- `NotificationServiceInstance`: 本体の使用者は `AppLogger.LogToTray` の 1 箇所。静的なロガーのため、注入できない。
- `ProfileSettingsServiceInstance`: 使用者は `Global` の静的メンバー（`useTempProfile`、`DebouncingMs` ほか約 10）、`BackingStore`（869）、`Mapping`（81）。`Mapping` の instance 化（Step7〜9）と `BackingStore` の分割（Step10）が済むまで残る。

### 1.5 `Global.SaveControllerConfigs`／`LoadControllerConfigs`
- 呼び出し元は `ProfileXmlStore`（接続・切断時、`device` を渡す）だけ。引数なし（全コントローラーの走査）の経路は、本体の呼び出しが 0 件で、`Program.rootHub` を使う。`device` ありの経路は `BackingStore.Load/SaveControllerConfigsForDevice` へ転送するだけ。

### 1.6 既存の不具合の発見（DS3 のジャイロトリガー。修正は承認後）
- **症状**: `DS4SixAxis.handleDS3Sixaxis` は `_controlService.touchPad[device].IsGyroTriggerActive(...)` を呼ぶ。`_controlService` は、コンストラクター `DS4SixAxis(ControlService controlService = null)` の `controlService ?? Program.rootHub` で設定される想定だが、`DS4Device` は `new DS4SixAxis()` と書き、同名の引数なしコンストラクターが選ばれる。そのため `_controlService` は **常に null**。DS3 コントローラーが接続されていると、ジャイロの入力処理で `NullReferenceException` になり得る。
- **移行前との比較**: 移行前（master）は `App.rootHub.touchPad[device].IsGyroTriggerActive(GyroOutMode.Controls)` と静的に呼び、動いていた。コミット `78ae8687`（2026-09-07 の「ControlServiceを全サブクラスに注入し、App.rootHubからの依存を削減」。ビルドに失敗する中間コミット）で、引数つきのコンストラクターを足したが、呼び出し側（`DS4Device`）を切り替えなかったのが原因。**DI 移行による回帰**。
- **影響範囲**: DS3 コントローラー（`DS3Device`）の接続時のみ。DS4・DualSense・Switch Pro・JoyCon は `handleDS3Sixaxis` を通らない。機材は未確認（U-2〜U-6 と同様に、実機確認は難しい可能性）。

## 2. 設計

- **`ControlService` の保持**: `App` のフィールド `_controlService` が持つ（`CreateControlService` の別スレッドの中で代入する）。`LoggerHolder` の生成、`PrepareAbort`、`CleanShutdown` は、このフィールドを使う。`Program.rootHub` と `Program` クラスは削除する。
- **`ServiceRegistration`**: `ControlService` の登録は `new ControlService(...)` だけにする（`Program.rootHub ??` を削除）。
- **画面の生成**: `MainWindow` は、`IMainWindowFactory`（新設、`IEditorWindowFactory`・`IViewModelFactory` と同じ方式）が生成する。ファクトリーがコンストラクターで依存を受け、`MainWindow` のコンストラクターは引数で受け取る。`ProfileEditor` は、既存の `IEditorWindowFactory` に `CreateProfileEditor` を足す。
- **`App` の解決の窓口**: `App` は WPF の `Application` でコンストラクター注入ができないため、Composition Root として `InitializePostHostServices`（`ResolvePostHostService`）で解決する形を残す。この 1 箇所だけが `AppHost.GetService` の正規の使用者になる。
- **`BackingStore`**: コンテナ外で生成される（`Global`）ため、`IProcessInspector` はプロパティ注入（`BackingStore` の登録のファクトリーで設定）にする。Step10 で `BackingStore` を分割するときに、コンストラクター注入へ。

## 3. ご判断をお願いしたい事項（選択肢と推奨）

- **Q1: `ControlService` の保持先（`rootHub` の置き換え）** — (A・推奨) `App` のフィールド `_controlService` が持ち、`Program.rootHub` と `Program` クラスを削除する。(B) 小さなホルダークラス（`IControlServiceAccessor`）を新設して共有する（`rootHub` の別名になるだけで、静的な状態が残る）。
- **Q2: `ServiceRegistration` の `Program.rootHub ?? new ControlService(...)`** — (A・推奨) `?? ` を削除し、常に生成する（`Program.rootHub` を代入するテストは、`ControlService` を直接渡す形へ書き換える）。(B) 残す（`rootHub` を削除できない）。
- **Q3: DS3 のジャイロトリガーの不具合（§1.6）** — (A・推奨) 移行前と同じ結果になるよう修正する。`DS4SixAxis` が、引数なしコンストラクターのまま、`ControlService` を指す関数（`Func<int, bool> GyroTriggerActive`）を公開プロパティで受け取り、`ControlService` がデバイスを追加するときに設定する（`rootHub` を使わない）。未設定のときは false（NRE にしない）。特性化テストを追加する。(B) 今回は直さず、台帳に記録して Step8（`Mouse`／`Mapping` の instance 化）で直す。(C) 修正せず、DS3 のジャイロ経路を廃止する（機能削減。承認が必要）。
- **Q4: `MainWindow` の 17 件の Service Locator** — (A・推奨) `IMainWindowFactory` を新設して DI 登録する。`App` はファクトリーを 1 件解決し、`MainWindow` は必要なサービスをコンストラクター引数で受け取る。(B) `App` が 17 個を解決して、`MainWindow` のコンストラクターへ長い引数リストで渡す（`App` の `AppHost` 参照が増える）。(C) `MainWindow` は Composition Root の隣接物として、現状のまま残す。
- **Q5: `ProfileEditor` の 4 件** — (A・推奨) `IEditorWindowFactory` に `CreateProfileEditor(targetDevice)` を追加し、`MainWindow:1945` の `new ProfileEditor(...)` を置き換える。`ProfileEditor` は 4 サービスをコンストラクター引数で受ける。(B) `MainWindow` が持つ 4 サービスを、`new ProfileEditor(...)` の引数に足す。
- **Q6: `ControlService`（718）と `BackingStore`（2823）の Locator** — (A・推奨) `ControlService` は `IElevatedProcessLauncher` をコンストラクター引数に追加する（フォールバックの `Process.Start` は残す）。`BackingStore` は `IProcessInspector` のプロパティ注入（登録のファクトリーで設定。フォールバックは残す）。(B) `BackingStore` の分は Step10（分割）まで残す。
- **Q7: `Global` の `*Instance` 3 つ** — (A・推奨) `ProfileXmlStoreInstance` を削除する（本体の使用者が 0 件。テストは `new ProfileXmlStore(Global.store)` へ）。`NotificationServiceInstance` は、`AppLogger` の静的プロパティ（`AppLogger.NotificationService`。`App` が起動時に設定する）へ置き換え、`Global` から削除する。`ProfileSettingsServiceInstance` は Step7〜9／Step10 まで残すが、フォールバックの `rootHub` のラムダを `() => null` にする。(B) 3 つとも Step10 まで残す（`rootHub` のラムダだけ直す）。
- **Q8: `Global.SaveControllerConfigs`／`LoadControllerConfigs`** — (A・推奨) 両メソッドを削除し、`ProfileXmlStore` が `BackingStore.Load/SaveControllerConfigsForDevice` を直接呼ぶ（引数なしの経路は呼び出し 0 件。`rootHub` 4 件が消える）。(B) 残す。
- **Q9: `IOutputSlotService.LoadOutputSlots`／`SaveOutputSlots` の接続**（Step2 で「接続」と判断し、Step5 に予定） — (A・推奨) `App` の起動後の読込を `IOutputSlotService.LoadOutputSlots()` に、`ControlService` の終了時の保存（702）を `SaveOutputSlots()` に切り替え、重複する `ControlService.LoadPermanentSlotsConfig` を削除する（処理は同じ `OutputSlotPersist` を呼ぶ）。(B) 契約を削除し、`ControlService` の直接呼び出しを残す（Step2 の判断の取り消し）。

その他の決定済み事項: `AppHost.CreationNote` は残す（Step9 で互換 `AppHost` と同時に判断）。`ServiceProviderHolder` の `App` 1 件は、`InitializePostHostServices` で `IControllerRegistry` を解決して保持する形へ置き換える（Q の対象外）。

## 4. Wave 計画（各 Wave ＝ 1 コミット。ビルド・テストはユーザーが実行）

- **W1: DS3 のジャイロトリガーの修正**（Q3＝A のとき）
  - 特性化テスト（`handleDS3Sixaxis` が未設定でも例外にならないこと、設定した関数の結果を使うこと）を先に追加。`DS4SixAxis` の未使用のコンストラクター（`rootHub` 参照）を削除し、`ControlService` がデバイス追加時に関数を設定する。
- **W2: `rootHub` の削除**（Q1・Q2・Q7 の一部・Q8）
  - `App` のフィールド化、`ServiceRegistration`、`Mapping`（6477）、`ProfileEditor`（314）、`Global`（ラムダ、`Save/LoadControllerConfigs`）、`ProfileXmlStore`。`Program.rootHub` と `Program` クラスを削除。`TestRepositoryFactory` ほかのテストを書き換える（`control` を明示で渡す）。
- **W3: 出力スロットの契約の接続と `Global.*Instance`**（Q9・Q7）
  - `LoadOutputSlots`／`SaveOutputSlots` の接続。`ProfileXmlStoreInstance` の削除。`NotificationServiceInstance` を `AppLogger` の静的プロパティへ。
- **W4: 個別の Locator の解消**（Q6）
  - `ControlService` の `IElevatedProcessLauncher` 注入、`BackingStore` の `IProcessInspector` のプロパティ注入。`App` の `LogSettingsApplierProxy`（425）・`IEnvironmentService`（726）・`ServiceProviderHolder`（1133）の置き換え。
- **W5: 画面の生成**（Q4・Q5）
  - `IMainWindowFactory` の新設、`MainWindow` のコンストラクター引数化、`IEditorWindowFactory.CreateProfileEditor`、`ProfileEditor` のコンストラクター引数化。
- **W6: ガード・台帳・モデル図・完了報告書**
  - ラチェットの基準値（`rootHub` 全ファイル 0、`AppHost` ほか）、`Phase7Step5…GuardTests`、台帳 §8、モデル図（03・04）、チェックリスト U-15、`Phase7-Step5-Completion-Report.md`。
- 各 Wave の共通作業: 削除の直前に、`Global.<名前>`、`DS4Windows.Global.<名前>`、`using static` の無修飾の使用、テストの参照、ソース文字列を正規表現で見るテストを再走査する。コンストラクターに引数を足すときは、`ControlServiceTestFactory`・`TestRepositoryFactory` と DI の登録を同時に直す。

## 5. リスクと検証

- **起動順序**: `ControlService` の生成スレッドと、`InitializePostHostServices`・`LoggerHolder` の生成の順序を変えない（W2）。
- **`MainWindow` のコンストラクター**（W5）: WPF の画面で、単体テストが届かない。ファクトリー経由に切り替えても、解決するサービスの集合と順序は同じにする。実機確認 U-15 の中心。
- **`rootHub` を代入していたテスト**: `Phase7Step3cApplyProfileCharacterizationTests`、`ServiceRegistrationArgumentParserTests`、`TestRepositoryFactory`。書き換えで期待値は変えない。
- **`Mapping` の静的なフィールド初期化子**（`profileSettings`／`profileApplication`）は、Step7〜9 まで触らない。`rootHub` を削除しても影響しない（確認済み）。
- **実機確認 U-15**: 起動・終了、コントローラーの接続・切断、プロファイル編集画面（開く・保存・閉じる）、トレイ、出力スロット管理画面、通知（LogToTray）、再起動後のスロット復元。DS3 の機材があればジャイロトリガー。

## 6. 完了判定チェックリスト

- [ ] W1〜W5 のビルド（警告 0）・テストビルド・テスト実行が成功している。
- [ ] `Program.rootHub` が 0 件で、`Program` クラスが削除されている。
- [ ] `ServiceRegistration` が `rootHub` を参照しない。
- [ ] `MainWindow`／`ProfileEditor` が `AppHost` を参照しない。
- [ ] DS3 のジャイロトリガーの不具合が、承認した方針で処理されている。
- [ ] ラチェットの基準値、台帳、モデル図、`Phase7-Status.md` が更新されている。
- [ ] 実機確認 U-15。
- [ ] `Phase7-Step5-Completion-Report.md` が作成されている。
