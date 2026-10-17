# Phase7-Step3 計画書: `BackingStore` の DI 化と `Global` の転送の削除

作成日: 2026-10-17  
対象ブランチ: `For-DI-migration-work`  
前提: `Phase7-Plan.md` §2（Step3）、`Phase7-0-Survey.md` §1.2、D2（`BackingStore` は「DI の単一インスタンス化＋永続化の切り出し」まで）  
状態: **実装完了（2026-10-17）。実機確認 U-12 は未。完了報告書あり。**  
ルール: ビルド・テストビルド・テスト実行・コミットはユーザーが行う。エージェントはローカルファイルの編集のみ。

---

## 0. 結論（先に）

- 目的: サービスが `Global` の静的な転送（`Global.X` → `m_Config.x`）を経由せず、注入された `BackingStore` を直接使う。転送の呼び出し元が 0 件になった `Global` のメンバーは、同じ Wave で削除する。
- 対象（走査の結果）:
  - サービスからだけ呼ばれる、純粋な転送の `Global` メンバー **80 名**と、少しの処理を持つ転送 **15 名**（計 95 名）。
  - 参照の大半は `AppSettingsService`（`Global.*` 185 件のうち 152 件が対象）。ほかは `ProfileRepository`（17）、`ProfilePresetLoader`（10）、小さなサービス数件。
- 効果の見込み: `Global.*` **723 → 約 520**（約 200 件減）。`Global` の実体を持つ約 50 名（環境・パスの状態、プロファイル適用、イベントなど）は、この Step の対象外（§3 Q1）。
- 影響の見込み: テスト参照 157 件（15 ファイル）。うち、旧シムと新サービスの一致だけを確認するテスト（`ControlService*ShimEquivalenceTests` ほか）は削除、残りは `Global.store` またはサービスへの書き換え。

## 1. 調査結果

### 1.1 `BackingStore` の現状
- 唯一の実体は `Global.m_Config`（`protected static BackingStore m_Config = new BackingStore()`、`Global.store` で公開）。**DI には未登録**。
- `ServiceRegistration` は、ファクトリーで `Global.store` を渡している（`ProfileSettingsService`、`SpecialActionRepository`、`ProfileActionProvider`）。
- 注入の方式が不統一:
  - 必須引数: `ProfileSettingsService`、`SpecialActionRepository`、`ProfileActionProvider`。
  - **任意引数で `Global.store` へフォールバック**: `ProfileXmlStore(BackingStore backingStore = null) { _backingStore = backingStore ?? Global.store; }`（Pure DI の違反。テストの 6 箇所が引数なしで生成）。
  - 注入なしで `Global.store` を直接参照: `ProfileRepository`（5 箇所）。
  - `Global` 経由: `AppSettingsService`、`ProfilePresetLoader`、`AppearanceSettingsService` ほか。

### 1.2 `Global` の転送の分類（走査）
- 純粋な転送（`m_Config` のフィールドを読み書きするだけ）で、サービスからだけ呼ばれるもの: **80 名**。内訳:
  - `AppSettingsService` 53 名: 画面・ウィンドウの寸法（`Form*`、`Controller*ColWidth`、`ProfileEditor*Width`、`SpecialAction*ColWidth`）、動作設定（`StartMinimized`、`MinToTaskbar`、`CloseMini`、`QuickCharge`、`SwipeProfiles`、`ProcessPriority`、`Notifications`、`FlashWhenLate*` ほか）、更新確認（`CheckEvery*`、`LastChecked`、`LastVersionCheckedNum`）、通信（OSC／UDP の 16 名）。
  - `ProfileRepository` 12 名、`ProfilePresetLoader` 10 名、そのほか 5 サービスに 1〜2 名ずつ。
- 少しの処理を持つ転送: **15 名**。
  - `FakeExeName`（設定時にファイル名として有効かを検証）、`UDPServerSmoothingBeta`／`Mincutoff`（変更時に静的イベント `…Changed` を発行）、`FormWidth` ほか 4 名（実際は純粋な転送）、`getLinkedProfile`／`removeLinkedProfile`（シリアルの正規化）、`SaveAction`、`LoadTempProfile`（`ProfileSettingsService` を更新）、`RefreshExtrasButtons`、`ResetConnectionFlags`、`LoadControllerConfigs`／`SaveControllerConfigs`。
- サービス以外からも呼ばれる転送（13 名）: `UseLang`、`LogMinLevel`、`LogMaxArchiveFiles`、`UseCurrentTheme`（`App.xaml.cs` の Pre-Host）、`LastVersionChecked`（`Changelog`）、`UseMoonlight`、`UseAdvancedMoonlight`（`DS4Devices`）、`DeviceOptions`（`HidDevices`）、`DistanceProfiles`（`DS4LightBar`）、`OutContType`（`Mapping`）、`GetAction`／`GetActionIndexOf`（`BackingStore`）、`store`。→ 呼び出し元の Step（Step4／Step7〜9／Step10）まで `Global` に残る。
- 実体を持つ名前（約 50 名）: 環境・パス（`exedirpath`、`appdatapath`、ViGEm／HidHide の情報ほか）、イベント（`BatteryChanged`、`SelectedProfileChanged`、`ProfileChangedNotification` ほか）、プロファイル適用（`ApplyProfile`、`ApplyProfileToSlot`、`defaultButtonMapping`、`CreateStdActions`、`CreateAutoProfiles` ほか）、出力（`outputKBMHandler`、`outDevTypeTemp`、`activeOutDevType`）。→ 環境・パスは Step4、出力は Step6、プロファイル適用は `Mapping` の instance 化（Step7〜9）と絡む。

### 1.3 サービスごとの規模（`Global.*` の参照件数、うち本 Step の対象）
- `AppSettingsService` 185（152）、`ProfileRepository` 26（17）、`ProfilePresetLoader` 10（10）、`AppearanceSettingsService` 8（2）、`ProfileXmlStore` 3（2）、`DeviceStateService` 4（3）、`SpecialActionRepository` 3（2）、`AutoProfileService` 3（2）、`PathService` 8（1）、`AppNotificationService` 1（1）、`ProfileSettingsService` 4（1）。
- 生成箇所の影響（引数追加に伴うテストの書き換え）: `new AppSettingsService(` 35、`new ProfileRepository(` 5、`new ProfilePresetLoader(` 2、`new AppearanceSettingsService(` 6、`new AutoProfileService(` 9、`new AppNotificationService(` 4、`new ProfileXmlStore(` 6。本体の `new` は、`MainWindow` などのフォールバック 3 箇所（`AppearanceSettingsService`、`AppNotificationService`、`ProfileXmlStore`。Pure DI の違反で、別途の整理対象）。

### 1.4 テストへの影響
- `Global` の対象メンバーを参照するテスト: 157 件、15 ファイル。多い順:
  - `AppSettingsServiceTests` 49、`ControlServiceShimEquivalenceTests` 28、`ControlServicePr2ShimEquivalenceTests` 15、`Phase6Step9ContractExtensionTests` 13、`NotificationServiceTests` 13、`ControlServicePr5ShimEquivalenceTests` 11、`SettingsViewModelGlobalReferenceGuardTests` 9。
- 方針（Step12 の前例）: 旧シムと新サービスの同期だけを確認するテストは削除。サービスの動作を確認するテストは、`Global.store.<フィールド>` またはサービスのプロパティへ書き換える。

## 2. 設計

- `BackingStore` を DI のシングルトンとして登録する。**実体は `Global.store` と同一**（`services.AddSingleton<BackingStore>(_ => Global.store);`）。`Global` の静的な生成（`new BackingStore()`）は、Step10 まで残す（Pre-Host が使うため）。
- サービスは、コンストラクターで `BackingStore` を**必須引数**で受け取る（Pure DI）。テストは `TestRepositoryFactory` に生成ヘルパーを追加して置き換える。
- 名前の対応（`Global.MinToTaskbar` → `_config.minToTaskbar` など）は、`Global` の本体から機械的に抽出して一覧にし、置換する（置換後に、走査で `Global.<名前>` の参照が 0 件であることを確認）。
- 処理を持つ転送は、処理ごとサービスへ移す（`FakeExeName` の検証、`UDPServer…Changed` の通知は `AppSettingsService` のインスタンスのイベントへ。購読側の `ControlService` は、すでに `_appSettings.…Changed` を購読している）。

## 3. ご判断をお願いしたい事項（選択肢と推奨）

- **Q1: スコープ** — (A・推奨) 本 Step は「3a（登録・注入）＋3b（転送の削除）」に限定する。3c（イベント・プロファイル適用・環境パスの実体の移管）は、`Phase7-Plan.md` の計画どおり、環境パスは Step4、出力は Step6、プロファイル適用は Step5〜9 と依存するため、別の計画（Step3c）に切り出す。(B) 3c の一部（イベント）も含める。
  - 理由: `ApplyProfile` は `Mapping`／`rootHub` と結びつき、イベントは購読関係の整理が要る。一つの Step に混ぜると検証が難しい。
- **Q2: コンストラクター** — (A・推奨) `BackingStore` を必須引数にする。`ProfileXmlStore` の `= null`＋`Global.store` のフォールバックも廃止。テストは生成ヘルパーを経由。(B) 任意引数のまま `Global.store` を既定にする（Pure DI の原則に反するため非推奨）。
- **Q3: 削除のタイミング** — (A・推奨) 転送の呼び出し元が 0 件になった `Global` のメンバーは、同じ Wave で削除する（Step12 と同じ方式）。旧シムの等価性テストは削除。(B) Step3 の最後にまとめて削除する（シムが長く残り、二重管理になる）。
- **Q4: 処理を持つ転送の移管** — (A・推奨) `FakeExeName` の検証、`UDPServerSmoothing…` の通知は `AppSettingsService` へ移し、`Global` 側を削除する。(B) 今回は転送だけを直し、処理を持つ 15 名は `Global` に残す。
- **Q5: サービス以外からも呼ばれる 13 名** — (A・推奨) 今回は `Global` に残し、サービスの側だけを `BackingStore` へ切り替える（`Global` 側のメンバーは、他の呼び出し元の Step で削除）。

## 4. Wave 計画（各 Wave ＝ 1 コミット。ビルド・テストはユーザーが実行）

- **W1（3a）: 登録と必須化**
  - `BackingStore` の登録、`ServiceRegistration` の `Global.store` 3 箇所を解決へ置換。
  - `ProfileXmlStore` の引数を必須化、`ProfileRepository` に `BackingStore` を注入（`Global.store` の 5 箇所を置換）。`TestRepositoryFactory` と、テストの生成箇所（`ProfileXmlStore` 6、`ProfileRepository` 5）を更新。
  - ラチェット: `ServiceRegistration.cs` 3→0、`ProfileRepository.cs` 26→21、`ProfileXmlStore.cs` 3→2。
- **W2（3b-1）: `AppSettingsService` の寸法・動作設定**
  - コンストラクターに `BackingStore` を追加（`TestRepositoryFactory.CreateAppSettingsService` を新設し、35 箇所を置換）。画面・ウィンドウの寸法（約 20 名）、動作設定（約 12 名）を `_config` へ切り替え、`Global` から削除。
- **W3（3b-2）: `AppSettingsService` の通信・更新確認**
  - OSC／UDP（16 名）、更新確認（`CheckEvery*`、`LastChecked`、`LastVersionCheckedNum` ほか）を切り替え、削除。
- **W4（3b-3）: 処理を持つ転送（Q4）**
  - `FakeExeName`（検証）、`UDPServerSmoothingBeta`／`Mincutoff`（通知）、`FormWidth` ほかの残りを `AppSettingsService` へ移し、`Global` から削除。通知の発行元が変わる（静的 → インスタンス）ため、`ControlService` の購読の動作を確認するテストを追加。
- **W5（3b-4）: ほかのサービス**
  - `ProfileRepository` の 12 名＋処理を持つ 3 名、`ProfilePresetLoader` の 10 名、`AppearanceSettingsService`、`AutoProfileService`、`AppNotificationService`、`SpecialActionRepository`、`PathService`、`DeviceStateService`、`ProfileXmlStore`、`ProfileSettingsService` の残り。
- **W6: ガード・台帳・モデル図・完了報告**
  - 新規 `Phase7Step3RemovedGlobalForwardersGuardTests`（削除した `Global` の名前の再宣言禁止）。ラチェットの基準値の更新、`Phase7-Residual-Ledger.md`、モデル図（`04-Service-Lifecycle-Spec.md` に `BackingStore` の登録を追記）、`Phase7-Step3-Completion-Report.md`。
- 各 Wave の共通作業: 削除の直前に、`Global.<名前>` と `using static`、リフレクション、XAML の Binding、テストの参照を再走査する。ソースの文字列でメンバーの範囲を切り出すテスト（Step2 の教訓）が隣接メンバーの削除で壊れないか、`public ... <隣接メンバー>` を含む正規表現を事前に検索する。既存のコメントを消さない。

## 5. リスクと検証

- 設定値の取りこぼし: `AppSettingsService` の 53 名は、Global の本体（`m_Config` のフィールド名）から機械的に対応表を作り、置換後に `Global.<名前>` が 0 件であることを走査で確認する。保存・読込（DTO）は `BackingStore` のフィールドを直接使うため、影響しない。
- 通知の変更（W4）: `UDPServerSmoothing…Changed` が静的イベント → インスタンスイベントに変わる。購読者は `ControlService` のみ（すでにインスタンス経由）。購読解除の漏れ（静的イベントのリーク）が解消される副次効果がある。
- テストの量: 157 件の書き換えと、30 件前後の削除（旧シムの等価性）。1 Wave あたりのテスト修正が多いため、Wave ごとにビルド・テストを確認する。
- 実機確認（チェックリスト §11 に U-12 として追記予定）: 起動、設定画面の値の保持（寸法、通知、OSC／UDP、更新確認）、再起動後の復元、偽の exe 名、UDP サーバーの起動。

## 6. 完了判定チェックリスト

- [x] W1〜W5 のビルド（警告 0）・テストビルド・テスト実行が成功している（W6 は文書のみ）。
- [x] 対象の `Global` メンバー 76 名が削除され、サービスが `BackingStore` を直接使っている（計画の約 95 名との差は完了報告書 §1・§5）。
- [x] `ProfileXmlStore`、`ProfileRepository` に `Global.store` の直接参照がない。`ServiceRegistration` は登録の 1 件だけ（Composition Root）。
- [x] ラチェットの基準値が更新され、`Global.*` が 516 になっている。
- [x] `Phase7Step3RemovedGlobalForwardersGuardTests` が成功している（W2〜W5 で毎回実行済み）。
- [x] 台帳、モデル図、`Phase7-Status.md` が更新されている。
- [ ] 実機確認 U-12（設定値の保持ほか）。
- [x] `Phase7-Step3-Completion-Report.md` が作成されている。

---

## 7. 承認と実行記録

- 承認: 2026-10-17、Q1〜Q5 すべて推奨（A）で承認。
- **W1 実装済み（ビルド・テスト待ち）**
  - `ServiceRegistration`: `services.AddSingleton<BackingStore>(_ => Global.store)` を追加。`ProfileSettingsService`／`SpecialActionRepository`／`ProfileActionProvider` のファクトリーは `sp.GetRequiredService<BackingStore>()` に変更。
  - `ProfileXmlStore`: 引数を必須化（null は `ArgumentNullException`）。`Global.store` のフォールバックを廃止。`Global.fallbackProfileXmlStore` は `new ProfileXmlStore(m_Config)`。
  - `ProfileRepository`: 第1引数に `BackingStore config` を追加。`Global.store` の 5 箇所を `_config` に置換。
  - テスト: `TestRepositoryFactory.CreateProfileRepository` に `store` 引数を追加。`ProfileXmlStoreTests`（フォールバック → 例外）、`Step10FourTwoGlobalReferenceGuardTests`（引数の並びと null 検査）、`Phase6Step10GroupThreeBContractTests`／`Step10GroupTwoGlobalReferenceGuardTests`（`new ProfileXmlStore(Global.store)`）を更新。
  - ラチェット基準値: `ServiceRegistration.cs` 3→1、`ProfileRepository.cs` 26→21、`ProfileXmlStore.cs` 3→2（`Global.*` 合計 723→715）。
- **W2 実装済み（ビルド・テスト待ち）**
  - `AppSettingsService`: コンストラクターの第1引数に `BackingStore config` を追加（必須）。寸法・動作設定・更新確認の開始時設定など 35 名の `Global.X` 参照 105 件を `_config.<field>` に置換。
  - `Global`: 純粋な転送だった 34 名を削除（`Notifications` は `AppNotificationService`／`MainWindow` が参照するため W5 まで残す）。残っていた見出しコメント（ProfileEditor／Controller tab）も削除。
  - テスト: 削除した名前の参照 66 件を `Global.store.<field>` に書き換え（等価性テストは削除せず、サービスと `BackingStore` の状態共有を確認する形で残した。計画書 §1.4 の「削除」から変更）。`new AppSettingsService(...)` 34 件を `TestRepositoryFactory.CreateAppSettingsService(...)` に置換。
  - 新規 `Phase7Step3RemovedGlobalForwardersGuardTests`（削除した名前の再宣言禁止と `AppSettingsService` からの参照禁止。以降の Wave で名前を追記する）。
  - ラチェット基準値: `AppSettingsService.cs` 185→80（`Global.*` 合計 715→610）。
- **W3 実装済み（ビルド・テスト待ち）**
  - `AppSettingsService`: UDP／OSC のメソッド型の転送 9 組と、更新確認（`CheckEveryValue`／`CheckEveryUnit`／`LastChecked`／`LastVersionCheckedNum`）を `_config.<field>` に置換。アドレス 2 件の `value.Trim()` は維持。他の呼び出し元がある `UseLang`／`LogMinLevel`／`LogMaxArchiveFiles`／`UseMoonlight`／`UseAdvancedMoonlight`／`DeviceOptions` も、サービス側だけ `_config` に切り替えた（`Global` 側は Q5 により残す）。
  - `Global`: 21 名を削除（メソッド 16、プロパティ 5）。`getOSCServerPortNum` は `ControlService` が参照するため残す（`setOSCServerPort` は削除）。
  - テスト: 15 件を `Global.store.<field>` に書き換え。`ControlServiceShimEquivalenceTests` の関数参照 5 件はラムダに変更。ガードテストの名前一覧に W3 を追記し、メソッド宣言も検出するよう正規表現を拡張。
  - `LastVersionChecked`（代入時に番号を算出する処理を持つ）、`SetCulture`、`firstRun`、`runHotPlug`、`ProfileChangedNotification`、UDP 平滑化、`FakeExeName` は W4 以降（処理を持つため）。
  - ラチェット基準値: `AppSettingsService.cs` 80→27。
- **W4 実装済み（ビルド・テスト待ち）**
  - `FakeExeName`: ファイル名として有効かの検証を `AppSettingsService` の setter へ移し、`Global.FakeExeName` を削除（`null` を代入すると例外になる点も従来どおり）。
  - UDP 平滑化: `UDPServerSmoothingMincutoff`／`Beta` の値と、`…Changed` イベントを `AppSettingsService` のインスタンスに移した（発火の条件と順序は従来と同一、発火元が `Global` の静的イベントから `this` に変わった）。購読者は `ControlService` のみ（すでに `_appSettings` 経由）。`Global` の 2 プロパティと 2 イベントを削除。静的イベントの購読が解除されないまま残る問題も解消した。
  - `LastVersionChecked`: 代入時に `LastVersionCheckedNum` を算出する処理をサービスへ移し、`Global.LastVersionChecked` は読み取り専用にした（`Changelog` が読むため残す。後続 Step で `Changelog` を切り替えたら削除）。
  - テスト: `Global.store.<field>` への書き換え、`ControlServiceShimEquivalenceTests` の 2 件は「サービスの setter が `Changed` を 1 回発火し、購読解除後は発火しない」確認に変更。ガードテストの名前一覧に W4 を追記。
  - ラチェット基準値: `AppSettingsService.cs` 27→11。
  - 持ち越し: `SetCulture`（`Global` に他の呼び出し元あり）、`firstRun`／`runHotPlug`（環境の状態。Step4）、`ProfileChangedNotification`（`ProfileSettingsServiceInstance` 経由。サービス間の依存の整理が要るため W5 で判断）。
- **W5 実装済み（ビルド・テスト待ち）**
  - `ProfileRepository`: `ProfilePath` 系、`LinkedProfileUI`、`ProfileActions`、リンクプロファイルの保存・読込、`CacheExtraProfileInfo`／`CacheProfileCustomsFlags` を `_config` に切り替え。リンクプロファイルの 4 操作（シリアルからコロンを除く正規化を含む）をサービスへ移した。
  - `AppearanceSettingsService`／`DeviceStateService`／`AppNotificationService`: コンストラクターに `BackingStore` を必須で追加。`AutoProfileService`: 末尾に `BackingStore config` を追加（必須）。`ProfileSettingsService.RefreshExtrasButtons`: 処理をサービスへ移した。
  - `Global`: 16 名を削除（`LinkedProfileUI`、`ProfileActions`、リンクプロファイル関連 6、`Cache…` 2、`UseIconChoice`、`autoProfileSwitchNotifyChoice`、接続フラグ 3、`RefreshExtrasButtons`）。`Global` のフォールバック `new AppNotificationService(m_Config)` に変更。
  - `MainWindow`: `IAppearanceSettingsService` の `?? new AppearanceSettingsService()`（Pure DI 違反のフォールバック）を、未登録なら例外にする形に変更。
  - テスト: 生成を `TestRepositoryFactory` の新ヘルパー（`CreateAppearanceSettingsService`／`CreateDeviceStateService`／`CreateAppNotificationService`）に置換、`Global.<名前>` を `Global.store.<field>` に書き換え。`AutoProfileService` の null 検査に `config` を追加。
  - **W5 で見送ったもの（判断）**: `ProfilePresetLoader` の 10 名と `ProfileRepository.LoadBlankProfile`（`Global.tempprofileDistance` など、プロファイル適用の状態と絡むため 3c）。`ProfilePath`／`OlderProfilePath`／`SelectedProfile`（`Global.ApplyProfile` が内部で使う。3c）。`SelectedProfileChanged` ほかのイベント、`LoadTempProfile`（`Global` の状態と絡む。3c）。`ProfileXmlStore.Load/SaveControllerConfigs`（`Program.rootHub` を走査する。Step5）。`SpecialActionRepository`／`PathService`／`EnvironmentService`／`OutputSlotService`／`OutputKBMHandlerAdapter`（実体を持つ状態。Step4／Step6）。`Notifications`（`MainWindow` が参照）は `Global` に残す。
  - ラチェット基準値: `AppearanceSettingsService` 8→4、`AppNotificationService` 1→0、`AutoProfileService` 3→1、`DeviceStateService` 4→2、`ProfileRepository` 21→6、`ProfileSettingsService` 4→3。`Global.*` 合計 516。
- **W6 実施（文書とガード）**: 台帳 §5、モデル図 03／04、チェックリスト U-12、完了報告書、本チェックリスト。`Phase7Step3RemovedGlobalForwardersGuardTests` は W2〜W5 で名前を追記済み。
