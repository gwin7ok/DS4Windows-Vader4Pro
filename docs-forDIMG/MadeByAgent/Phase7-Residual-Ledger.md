# Phase7 残存台帳: Service Locator と静的アクセス

作成日: 2026-10-17（Phase7-Step1 W4）  
基準: 現行コードの走査（コメントを除く）。`Phase6-Step11-Global-Residual-Ledger.md` の続き。  
ガード: `Step10FourGlobalRatchetGuardTests`（`Global.*`／`AppHost`／`Program.rootHub`）、`Phase7Step1ServiceLocatorRatchetGuardTests`（`ServiceProviderHolder`）。減る変更だけ許可。基準値を更新して固定する。

## 1. 集計（2026-10-17、Phase7-Step1 W3 まで）

- `Global.*`: 733（Phase6 完了時 747。W1 の `BackingStore` 孤立メソッド削除で 14 減）。
- `AppHost.GetService|Services`: 40。
- `Program.rootHub`: 22。
- `ServiceProviderHolder.Provider|GetRequiredService`: 23（今回、追跡を開始）。

## 2. Service Locator の分類

### 2.1 AppHost.GetService|Services（40）
- `MainWindow.xaml.cs` 17: コンストラクターで 14 件のサービスを解決。Composition Root 隣接の画面。→ Phase7-Step5
- `Mapping.cs` 6: 静的束縛（`profileSettings`、`profileApplication`、`VirtualKBM`）と 3 件の遅延解決。→ Phase7-Step7〜9（`Mapping` の instance 化）
- `App.xaml.cs` 4（Composition Root。`CreateHost` 自身を除く解決）、`ProfileEditor.xaml.cs` 4、`Global.cs` 4（`*Instance` のフォールバックと `Load`）。→ Phase7-Step4／5
- `DI/AppHost.cs` 2、`DI/Compat/AppHost.cs` 1（`Mapping.cs:86` 用の互換ラッパー）、`BackingStore.cs` 1、`ControlService.cs` 1。→ Step4／Step9／Step3

### 2.2 Program.rootHub（22）
- `App.xaml.cs` 12（代入、`LoggerHolder` の生成、起動時の呼び出し、終了処理）、`Global.cs` 6、`ServiceRegistration.cs` 1、`Mapping.cs` 1、`ProfileEditor.xaml.cs` 1、`DS4SixAxis.cs` 1。→ Phase7-Step5

### 2.3 ServiceProviderHolder（23。新規に追跡）
- `Actions/ActionManager.cs` 13、`ActionFactory.cs` 1、`DefaultKeyButtonActionControllerFactory.cs` 2、`KeyActionAdapter.cs` 1: **Phase8 の持ち越し**（Actions サブシステムは Phase8 で統合ディスパッチへ作り直す）。計 17。
- `Mapping.cs` 5、`App.xaml.cs` 1: → Phase7-Step5（`App` の `CleanShutdown`）と Step7〜9（`Mapping`）。計 6。
- 設定側の `SetProvider`（`DI/AppHost.cs` 3）は Composition Root の正規の使い方で、数えない。

## 3. 参照
- 調査: `Phase7-0-Survey.md` §1.5（F-A）。計画: `Phase7-Step1-Plan.md` W4。

## 4. Phase7-Step2 で追加した台帳（2026-10-17）

- 削除した DI 契約のメンバー: 51（`Phase7-Step2-Plan.md` §2・§8。ガード: `Phase7Step2RemovedContractMembersGuardTests`）。
- 「使うべきだが未接続」として TODO 付きで残した契約: `IEnvironmentService.UsingMaxControllers`（接続予定 Step10）、`IDs4DeviceRegistry.ReEnableDevice`（Step4）、`IOutputSlotService.LoadOutputSlots`／`SaveOutputSlots`（Step5）。温存: `IOutputSlotService.OutputSlotChanged`（Step6 で再判断）。
- 承認待ちの追加候補（Step2 計画書 §8 W6）: `IUdpServerService` 契約全体（未接続。接続を推奨）、`ISpecialActionRepository.GetAction`／`ActionExists`／`SaveActions`、`IPathService.ActionsPath`、`IDs4DeviceRegistry.IsHidHideInstalled`、`IProfileSettingsService.RumbleAutostopTime`、`IVirtualKBM.Version`（削除を推奨）。
- Phase8 へ移管: `Actions/` の未配線の抽象 12 メンバー（`Phase8-Unified-Trigger-Dispatch-Plan.md` に追記済み）。
- Step10 で再判断: `ProfileSubSettingBase.OnSubPropertyChanged`（購読者がいなくなった）、`ProfileApplicationService` の未使用の依存 `IDeviceStateService`。

## 5. Phase7-Step3 で更新した台帳（2026-10-17）

- `Global.*`: 723 → **516**（W1 −8、W2 −105、W3〜W5 −94。基準値は `Step10FourGlobalRatchetGuardTests`）。`AppHost` 40、`Program.rootHub` 22、`ServiceProviderHolder` 23 は変わらない。
- `BackingStore` を DI のシングルトンとして登録（実体は `Global.store` と同一）。`ServiceRegistration`、`ProfileXmlStore`、`ProfileRepository` から `Global.store` の直接参照を除いた（`ServiceRegistration` の登録の 1 件だけが Composition Root として残る）。
- 削除した `Global` の転送メンバー: 76 名（W2 34、W3 21、W4 5、W5 16）。ガード: `Phase7Step3RemovedGlobalForwardersGuardTests`。
- サービス側だけ `BackingStore` に切り替え、`Global` のメンバーは他の呼び出し元のために残したもの（Q5）: `Notifications`（`MainWindow`）、`UseLang`、`LogMinLevel`、`LogMaxArchiveFiles`、`UseCurrentTheme`、`UseMoonlight`、`UseAdvancedMoonlight`、`DeviceOptions`、`getOSCServerPortNum`（`ControlService`）、`LastVersionChecked`（読み取り専用にした。`Changelog` が読む）。
- Step3c（別計画書）へ持ち越し: `ProfilePresetLoader` の 10 名と `ProfileRepository.LoadBlankProfile`（`tempprofileDistance` ほかの状態と絡む）、`ProfilePath`／`OlderProfilePath`／`SelectedProfile`（`Global.ApplyProfile` が内部で使う）、`SelectedProfileChanged` ほかのイベント、`LoadTempProfile`、`ProfileApplicationService`／`ProfileSlotApplier` の `ApplyProfile` 系、`ProfileChangedNotification`（`ProfileSettingsServiceInstance` 経由）。
- Step4 へ: `firstRun`／`runHotPlug`、`PathService`／`EnvironmentService` が参照する環境・パスの実体、`SetCulture`。
- Step5 へ: `ProfileXmlStore.Load/SaveControllerConfigs`（`Program.rootHub` を走査する）、`MainWindow` などの Service Locator。
- Step6 へ: `OutputSlotService` の `outDevTypeTemp`／`activeOutDevType`、`OutputKBMHandlerAdapter` の `outputKBMHandler`。
- 残る Pure DI 違反のフォールバック: `Global.cs` の `new ProfileXmlStore(m_Config)`／`new AppNotificationService(m_Config)`（`*Instance` 系の最後のフォールバック。Step4／Step5 で削除）。

## 6. Phase7-Step3c で更新した台帳（2026-10-17）

- `Global.*`: 516 → **488**（W2 −11、W3 −6、W4 −8、W5 −3。基準値は `Step10FourGlobalRatchetGuardTests`）。`AppHost` 40、`Program.rootHub` 22、`ServiceProviderHolder` 23 は変わらない。Phase7 開始時（733）からの累計は −245。
- 削除した `Global` のメンバー: 24 名（プリセット 10、`loggedInvalidActions`、プロファイル適用 12、`ProfileChangedNotification`）。契約からも、重複していた `IProfileRepository.LoadBlankProfile` を削除。ガード: `Phase7Step3RemovedGlobalForwardersGuardTests`（Step3 と合わせて 100 名）。
- 適用の本体は `ProfileApplicationService`（`ApplyCore`／`LoadNormalProfile`／`CompleteProfileApplication`、入口は `ApplyProfile`／`ApplyFromAction`／`ApplyProfileWithoutHalt`）に移った。`ProfileRepository` が `LoadTempProfile` と `SelectedProfileChanged`（インスタンスイベント）を持つ。
- 静的呼び出しのまま残るもの（適用の本体の中）: `ActionManager.ClearAllEntries`／`ClearDeviceState`／`PreallocateForProfileApply`（→ Phase8）、`Mapping.ClearKeyButtonControllersForDevice`（→ Phase7-Step7〜9）。
- `Global` に残る、プロファイルに関係する窓口: `tempprofileDistance`／`useTempProfile`／`DistanceProfiles`（`DS4LightBar`／`Mapping` が `using static` で参照。→ Step7〜9）、`ProfileSettingsServiceInstance`／`ProfileXmlStoreInstance`（フォールバックの Service Locator。→ Step4／Step5）。
- 実機確認: U-13（未。ユーザー判断で先送り）。


## 7. Phase7-Step4 で更新した台帳（2026-10-17）

- `Global.*`: 488 → **452**（W1 −17、W3 −19。基準値は `Step10FourGlobalRatchetGuardTests`）。`AppHost` 40 → **39**（W2: `Global.Load` の削除）、`Program.rootHub` 22 → **21**（基準値 `Global.cs` の 6 が過大で、実数 5 に直した。コードの変更ではない）、`ServiceProviderHolder` 23 は変わらない。Phase7 開始時（733）からの累計は −281。
- 削除した `Global` のメンバー: 環境の調べ物とその状態 27 名（W1。`EnvironmentService` が処理と状態を持つ）、`Load`（W2）、保存場所・実行ファイルの実体と処理 8 名（W3: `FindConfigLocation`／`SaveWhere`／`SaveDefault`／`firstRun`／`multisavespots`／`appDataPpath`／`runHotPlug`／`fileVersion`）。ガード: `Phase7Step4EnvironmentProbesGuardTests`、`Phase7Step4PathServiceGuardTests`、`Phase7Step4AppHostGuardTests`、`Phase7Step4StartupBootstrapGuardTests`。
- 設計上の逸脱（計画書 §2 との差）: `PathService`／`EnvironmentService` の状態は、インスタンスではなく **static** で持つ。理由は、下位層（`BackingStore`／`LoggerHolder`）、既存テスト（`Global.appdatapath` を書き換えるもの約 46 件）、起動前の `App` が、同じプロセス全体の値を共有するため。ブートストラップのインスタンスは作り、`CreateHost` へ渡して同じインスタンスをコンテナへ登録する（インスタンスはどれも同じ状態を見る）。
- `Global` に残した窓口（読み書き）: `appdatapath`／`exedirpath`／`exelocation`／`exeversion`（`PathService` の状態を指す。`BackingStore`／`LoggerHolder`／`Changelog` ほかが静的に読む。→ Step10 で `BackingStore` の永続化を切り出すときに、`IPathService` の注入に置き換えて廃止）。
- `Global` に残した環境系: `IsAdministrator`／`AdminNeeded`／`GetInstanceIdFromDevicePath`／`CheckIfVirtualDevice`（`BackingStore`／`Util`／`DS4Devices` が呼ぶ。→ Step10）。
- `AppHost`: `CreateHost(string[])` と `CreateHost()` の未使用引数を削除。`Dispose` を `CleanShutdown` で呼ぶ（Dispose の後は暗黙にホストを作り直さない）。`-driverinstall` 分岐は、設定の読込の前にホストを明示的に作る（K7-1 の残存経路の解消）。`CreationNote` は残す。
- `IDs4DeviceRegistry.ReEnableDevice`: 接続した（`-reenableDevice` 分岐が契約経由で呼ぶ）。
- `WelcomeDialog` の `loadConfig` 引数と、中の `FindConfigLocation`／`Load` の重複呼び出しを削除（呼び出し元が直前に同じ処理をしている）。
- 既存の課題（回帰ではない。修正は未承認）: 起動直後のログローテーションとログの最小レベルは、`Profiles.xml` を読む前に参照されるため、`BackingStore` の既定値になる（2025-12-14 の `25d60ecb`／`c1cb918b` の機能から。DI 移行前から）。計画書 Q7＝A により記録のみ。直す場合は、ログ設定だけを先に読む手順を足す（起動順序の変更になる）。
- 持ち越し: `Global.cs` の `*Instance` フォールバック（`new ProfileXmlStore(m_Config)`／`new AppNotificationService(m_Config)` ほか）は Step5 で Service Locator とあわせて削除する（Step4 では触っていない）。`AppHost.Dispose()` は呼ぶようになったが、`Program.rootHub` と Service Locator は Step5。
- 実機確認: U-14（未）。

## 8. Phase7-Step5 で更新した台帳（2026-10-17）

- `Program.rootHub`: 21 → **0**（`Program` クラスと `Program.cs` を削除）。`AppHost.GetService|Services`: 39 → **11**。`ServiceProviderHolder`: 23 → **22**。`Global.*`: 452 → **446**。Phase7 開始時（733）からの累計は −287。基準値は `Step10FourGlobalRatchetGuardTests`（合計 0／11／446）と `Phase7Step1ServiceLocatorRatchetGuardTests`。
- 残る `AppHost.GetService`（11）: `Mapping.cs` 6（静的なフィールド初期化子ほか。→ Step7〜9）、`DI/Compat/AppHost.cs` 1（互換ラッパー。→ Step9）、`DI/AppHost.cs` 2（自身のログ文）、`Global.cs` 1（`ProfileSettingsServiceInstance`。→ Step7〜9／Step10）、`App.xaml.cs` 1（`ResolvePostHostService`。Composition Root の解決の窓口）。
- 残る `ServiceProviderHolder`（22）: `Actions/` 17（→ Phase8）、`Mapping.cs` 5（→ Step7〜9）。
- 新設: `IMainWindowFactory`／`MainWindowFactory`（`MainWindow` の 17 件の Locator を引数化）、`IEditorWindowFactory.CreateProfileEditor`（`ProfileEditor` の 4 件を引数化）、`AppLogger.NotificationService`（静的ロガーへ通知サービスを設定）、`BackingStore.ProcessInspector`（プロパティ注入）、`ControlService` のコンストラクター引数 `IElevatedProcessLauncher`、`DS4SixAxis.GyroControlsTriggerActive`。
- 削除: `Program.rootHub`、`Global.SaveControllerConfigs`／`LoadControllerConfigs`、`Global.ProfileXmlStoreInstance`／`NotificationServiceInstance`、`ControlService.LoadPermanentSlotsConfig`。接続: `IOutputSlotService.LoadOutputSlots`／`SaveOutputSlots`（Step2 の「接続」の判断を完了）。
- 不具合の是正: DS3 コントローラーのジャイロトリガー経路（`DS4SixAxis._controlService` が常に null。コミット `78ae8687` からの DI 移行による回帰）。移行前と同じ結果を返す形に直した（`Phase7-Step5-Plan.md` §1.6、Q3＝A）。DS3 の機材での実機確認は難しい可能性がある（U-15 の注記）。
- 持ち越し: `Global.ProfileSettingsServiceInstance` とそのフォールバック（→ Step7〜9／Step10）。`AppHost.CreationNote` と暗黙構築の経路（→ Step9 で互換 `AppHost` と同時に判断）。`MainWindow` のコンストラクター引数は 18 個と多い（→ Step10 で、画面ごとの責務の整理とあわせて見直す）。
- 実機確認: U-15（未）。

## 9. Phase7-Step6 で更新した台帳（2026-10-17）

- `Global.*`: 446 → **438**（`BackingStore.cs` 166 → 159、`OutputSlotService.cs` 2 → 1）。`AppHost.GetService|Services` 11、`Program.rootHub` 0、`ServiceProviderHolder` 22 は変更なし。Phase7 開始時（733）からの累計は −295。基準値は `Step10FourGlobalRatchetGuardTests`（合計 0／11／438）。
- 移管: `Global.outDevTypeTemp`（編集画面の未確定の出力種別）と `Global.activeOutDevType`（スロットごとの接続中の種別）を削除し、実体を `OutputSlotService` の readonly フィールドへ移した（Q1＝A、Q3＝A）。`BackingStore.ActiveOutDevType`（プロパティ。既定は全スロット None）が、`IOutputSlotService` の登録時に同じ配列を受け取る（Q2＝A）。`BackingStore` の読み取り 8 件を置き換えた。
- 注意（循環）: `BackingStore` の登録の中で `IOutputSlotService` を解決してはならない（`IOutputSlotStore` → `IPathService` → `BackingStore` と循環し、実行時にスタックオーバーフローになる）。W1 で一度発生し、受け渡しを `IOutputSlotService` の登録側へ移して解消した。`Phase7Step6OutputTypeOwnershipGuardTests` が固定している。
- 持ち越し: `Global.outputKBMHandler`／`outputKBMMapping`（`OutputKBMHandlerAdapter`、`OutputKBMHandlerLifecycle`、`SyntheticDispatcher`、`Mapping` のフォールバック）は、Q4＝A により Step7〜9（`Mapping` の instance 化）で扱う。`IOutputSlotService.OutputSlotChanged`（購読者 0 件）は、Q5＝A により温存し、TODO の再判断先を Step10 へ更新した。`BackingStore.ActiveOutDevType` のコンストラクター注入は Step10。
- 実機確認: U-16（未）。

## 10. Phase7-Step7 で更新した台帳（2026-10-17）

- `Global.*`: 438 → **432**（`Mapping.cs` 52 → 43、新ファイル `SyntheticInputState.cs`／`MacroRuntimeState.cs`／`StickMotionState.cs` が各 1）。`AppHost.GetService|Services` 11、`Program.rootHub` 0、`ServiceProviderHolder` 22 は変更なし。Phase7 開始時（733）からの累計は −301。基準値は `Step10FourGlobalRatchetGuardTests`（合計 0／11／432）。
- 新設（状態の保持クラス。いずれも `Shared`＝暫定の静的な窓口＋DI の Singleton 登録。Step8 で `Shared` を削除）: `SyntheticInputState`（`globalState`／`deviceState`／`syncStateLock`）、`MacroRuntimeState`（`macrodone`／`macroDispatchInFlight`／`macroDispatchLock`／`macroControl`／`macroCount`／`macroTaskQueue`）、`StickMotionState`（`gyroStickX`／`gyroStickY`／`mapStickActionData`／`flickMappingData`／`l2/r2TwoStageMappingData`／`wheelFilters`／`deltaAccelProcessors`）。`Mapping` の同名メンバーは、保持クラスへの転送（状態は持たない。TODO は Step8）。
- トップレベルへ出した型（Q5＝A。中身は変えていない）: `SyntheticState`、`PostMapStickData`、`FlickStickMappingData`、`TwoStageTriggerMappingData`、`DeltaSettingsProcessor`、`DeltaSettingsProcessorGroup`、`Click`。
- 逆参照の解消（Q4＝A）: `BackingStore` から `Mapping` への参照を 0 にした。`BackingStore.ActionsLoaded` イベントを `LoadActions` の末尾で発行し、`ServiceRegistration` が購読者 `ActionsLoadedRuntimeReset.Reset`（順序とログは従来どおり）を登録する。
- 特性化テスト（Q1＝A）: `MapCustom`（素通し、ボタン→ボタン、軸の向き→ボタン、トリガー→ボタン、ボタン→マウスボタン）、`Commit`（左・右・中ボタンの押下／保持／解放、Sync）、`MapClick`、`getBool*Mapping`、`HandleDeviceDisconnect`。範囲外: ボタン→キー（`ActionManager` 経由。Phase8）、ホイール（実時間 150ms の連続送出）、マクロ、ジャイロ、フリックスティック、タッチ。
- 計画との差: `ControlService` と `Mouse` は、保持クラスを注入ではなく `Shared` から得る（`ControlService` のコンストラクター変更によるテストの改修を避けるため。Step8 で `Mapping` の instance 化と同時に注入へ）。`stickValueHistory`／`lastStickAxisValues`／`outSqrStk`／`lastWheelGyroValues` など `Mapping` 内部だけで使う状態は、Step8 まで `Mapping` に残した。
- 持ち越し: `Mapping` の `profileSettings`／`profileApplication`／`VirtualKBM` の束縛、`AppHost` 6・`ServiceProviderHolder` 5、`Global.outputKBMHandler`／`outputKBMMapping`（Q6＝A）、`Shared` の削除（Step8）。互換 `AppHost`、`Global` 定数参照 43、K3（`macroDispatchInFlight` の再設計）（Step9）。`Actions/` の `ServiceProviderHolder` 17（Phase8）。
- 実機確認: U-17（未）。

## 11. Phase7-Step8 で更新した台帳（2026-10-17）

- `Global.*`: 432 → **399**（`OutputKBMHandlerAdapter.cs` 19 → 0、`OutputKBMHandlerLifecycle.cs` 8 → 0、`SyntheticDispatcher.cs` 4 → 0、`Mapping.cs` 43 → 41）。`AppHost.GetService|Services`: 11 → **5**（`Mapping.cs` の 6 件。残りは `App.xaml.cs` 1、`DI/AppHost.cs` 2、`DI/Compat/AppHost.cs` 1、`Global.cs` 1）。`Program.rootHub` 0 のまま。`ServiceProviderHolder`（`Phase7Step1ServiceLocatorRatchetGuardTests` の数え方）: 22 → **17**（`Mapping.cs` の 5 件。残りは `Actions/` の 17 件で Phase8）。Phase7 開始時（733）からの累計は −334。基準値は `Step10FourGlobalRatchetGuardTests`（合計 0／5／399）。
- 新設: `IMapping`／`Mapping`（Singleton。窓口は 8 メンバー）、`IKeyButtonControllerRegistry`／`KeyButtonControllerRegistry`（旧 `Mapping` の静的な登録簿）、`ISyntheticDispatcher`／`SyntheticDispatcherService`（旧 静的クラスの処理）。`ActionsLoadedRuntimeReset` は静的クラスから instance へ。
- 削除: 保持クラス 3 つの `Shared`、`Mapping` の転送プロパティ（Step7）、`Global.outputKBMHandler`／`outputKBMMapping`／`InitOutputKBMHandler`／`InitOutputKBMMapping`、`Mapping` の `AppHost`／`ServiceProviderHolder`／`Global` のフォールバック、`MouseOutputAction` の無効な検索（2 箇所）、呼び出しが 0 件だった内部メンバー 4 つ（`DispatchProfileActionEdge`、`TakePendingRestoreProfileName`、`ApplyProfileDirect`、`RestoreProfileDirect`）。`MapClick`／`ResetRuntimeStateForKey` は `SyntheticInputState` へ移した。
- 付け替え: `Mouse`／`ControlService` は保持クラスをコンストラクターで受ける。`ControlService` は `IMapping` を受け、`MappingService` として公開する（`ControllerReadingsControl`、`RecordBoxViewModel` が使う）。`DefaultActionManager`／`DefaultMacroPlayer` は `Func<IMapping>`（遅延）を受ける。`ProfileApplicationService` は `IKeyButtonControllerRegistry` を受ける。`SpecialActionRepository` は `SyntheticInputState` を受け、キー型の保存後のリセットを `Global.SaveAction` から引き取った。
- 持ち越し（暫定の橋渡し）: 静的な `SyntheticDispatcher` は、Actions サブシステム（`KeyButtonActionController`、`KeyAction`、`KeyOutputAction`、`RepeatHelper`）が静的に呼ぶための橋渡しとして残した。状態もフォールバックも持たず、DI が作った instance を `Bind` で受ける。→ Phase8 で削除。`ActionManager.cs` の `Mapping` 呼び出し 3 件は、取得済みの `sp` から解決する形に替えた（`ServiceProviderHolder` の語は増えていない）。
- 持ち越し（Step9）: 互換 `AppHost`（`DI/Compat/AppHost.cs`。`Mapping.cs` が使わなくなったため、呼び出し元は 0 件のはず。削除前に確認）、`Mapping.cs` の `Global` 参照 41（定数・`Clamp`）、K3（`macroDispatchInFlight`）、static のまま残した純粋な関数・定数の最終的な置き場所。
- 持ち越し（Step10）: `BackingStore` のコンストラクター注入、`Mapping` のコンストラクターの依存（11 個）の整理。
- 実機確認: U-18（未）。

## 12. Phase7-Step9 で更新した台帳（2026-10-18）

- `Global.*`: 399 → **355**（`Mapping.cs` 41 → 0、`SyntheticInputState.cs`／`MacroRuntimeState.cs`／`StickMotionState.cs` 各 1 → 0）。`AppHost.GetService|Services`: 5 → **4**（`DI/Compat/AppHost.cs` 1 → 削除。残りは `App.xaml.cs` 1、`DI/AppHost.cs` 2、`Global.cs` 1）。`Program.rootHub` 0、`ServiceProviderHolder` 17（Actions/ で Phase8）は変更なし。Phase7 開始時（733）からの累計は −378。基準値は `Step10FourGlobalRatchetGuardTests`（合計 0／4／355）。なお、計画書 §7 の「354」は 41＋3 を 45 と数えた誤りで、正しくは 355。
- 削除: 互換 `DS4Windows.AppHost`（`DI/Compat/AppHost.cs`。呼び出し元 0 件。`DI/Compat/` フォルダーも消えた）。`Mapping` のキャッシュフィールド `macroDispatchInFlight`／`macroDispatchLock`、`MacroRuntimeState.DispatchLock` の公開（非公開にした）。
- 新設: `ControllerLimits`（コントローラー数などの定数 5 つ）、`NumericUtil`（`Clamp`。本体は旧 `Global.Clamp` と同一で、`AggressiveInlining`）。`Global` の同名の定数は `ControllerLimits` を指す別名、`Global.Clamp` は `NumericUtil.Clamp` へ委譲する薄いシムとして残した。`MacroRuntimeState.TryBeginDispatch`／`EndDispatch`（K3 の二重実行防止のロックと確保を包んだもの。挙動は同一）。
- K3 の判断（Q3＝A）: 暫定フラグの実体を `MacroRuntimeState` に包んだだけで、仕組みは変えていない。恒久の再設計（トリガー判定層とマクロ実行層の分離、実行層の一本化）は **Phase8** へ持ち越し（TODO を `MacroRuntimeState.cs` と `Mapping.cs` に明記）。
- 持ち越し（Step10）: `Global` の定数の利用箇所（約 250 件。`BackingStore` 135 が最大）と `Global.Clamp` の残り 10 件（`DS4State` 4、`BezierCurve` 2、`Mouse` 2、`MouseCursor` 2）を `ControllerLimits`／`NumericUtil` へ移し、別名とシムを削除する。静的な純粋関数（`getBool*Mapping`、`DS4ControltoInt`、`remainderCutoff` ほか）の置き場所（Q4＝`Mapping` に残す）。`AppHost` の暗黙構築と `CreationNote`（Q5＝触らない）。`BackingStore` のコンストラクター注入。`Mapping` の依存 11 個の整理。
- 持ち越し（Phase8）: K3 の恒久対応、静的な `SyntheticDispatcher`、`Actions/` の `ServiceProviderHolder` 17。
- 実機確認: U-19（未。先送り）。

## 13. Phase7-Step10a で更新した台帳（2026-10-18）

- `Global.*`: 355 → **107**（`BackingStore.cs` 159 → 27、`ProfileEditor.xaml.cs` 32 → 29 → 0 ほか。定数 246 件相当と `Clamp` 9 件を切り替え）。`AppHost.GetService|Services` 4、`Program.rootHub` 0、`ServiceProviderHolder` 17 は変更なし。Phase7 開始時（733）からの累計は −626。基準値は `Step10FourGlobalRatchetGuardTests`（合計 0／4／107）。なお、計画書の目標「100」は数え違いで、正しくは 107。
- 削除（`Global` から）: 定数 5 つ（`MAX_DS4_CONTROLLER_COUNT`、`TEST_PROFILE_ITEM_COUNT`、`TEST_PROFILE_INDEX`、`OLD_XINPUT_CONTROLLER_COUNT`、`DS4_STICK_AXIS_MIDPOINT`）、資源パスの定数 2 つ（`ASSEMBLY_RESOURCE_PREFIX`、`RESOURCES_PREFIX`）、`Clamp`。再導入は `Step12DeadShimRemovalGuardTests` が禁止する（削除禁止の名簿 `ProtectedInGlobal` から外し、`RemovedFromGlobal` へ移した）。
- 新設・移動: `ControllerLimits`（Step9 で新設）を全利用箇所が使う。`ResourcePaths.ResourcesPrefix`／`AssemblyResourcePrefix`（画面側。値は従来と同じ）。`NumericUtil.Clamp`（`DS4State`／`Mouse`／`MouseCursor`／`BezierCurve` も切り替え）。`ControlService.MAX_DS4_CONTROLLER_COUNT`（公開の名前は維持）と `ProfileSettingsService` の再宣言は `ControllerLimits` から値を取る。
- 持ち越し（Step10b）: `Global.iconChoiceResources`（トレイアイコンの表）は画面側の `ResourcePaths` を一時的に参照している（下位層から上位層への参照。TODO を付けた）。表と文字列変換（`getX360ControlString`、`macroDS4Values`、`defaultButtonMapping`、`reverseX360ButtonMapping`、名前表）の再配置、契約の再判断、孤立定数 8 件。
- 持ち越し（Step10d）: `using static DS4Windows.Global;`（`Mapping.cs`、`DS4LightBar.cs`）の削除と、`MappingStep3FinalGlobalReferenceGuardTests` の更新。
- 実機確認: 10a 単独の項目なし（10e の総合検証で、トレイアイコン・プロファイル編集の画像の表示を確認項目に加える）。

## 14. Phase7-Step10b で更新した台帳（2026-10-18）

- `Global.*`: 107 → **93**（W1 −13、W2 −1。`PressKeyViewModel` 7、`MappedControl` 3、`RecordBoxViewModel` 1、`ProfileSettingsService` 2、`AppearanceSettingsService` 1 が 0 または減）。`AppHost.GetService|Services` 4、`Program.rootHub` 0、`ServiceProviderHolder` 17 は変更なし。Phase7 開始時（733）からの累計は −640。基準値は `Step10FourGlobalRatchetGuardTests`（合計 0／4／93）。計画書の目標「約 80」は見積もりで、実測は 93（W3・W4 は `Global.` の参照に関係しない）。
- 移動（`Global` から削除。宣言は移動のみで内容は同一）:
  - `X360ControlTables`: `defaultButtonMapping`、`reverseX360ButtonMapping`、`xboxDefaultNames`、`ds4DefaultNames`、`getX360ControlString`（2 引数）、および `BackingStore` の 1 引数の `getX360ControlString`（XML 保存用の名前。状態を持たない switch）。
  - `DS4ControlNames`: `ds4inputNames`、`macroDS4Values`。
  - `TrayIconResources`（画面側）: `iconChoiceResources`。`AppearanceSettingsService` が参照する（サービスから画面側の型への参照が残る。TODO を記載）。
  - `ColorUtil`: `getTransitionedColor`、`applyRatio`×2、`HuetoRGB`。`DS4LightBar`／`Mapping` は `ColorUtil.` で修飾して呼ぶ。
  - 再導入は `Step12DeadShimRemovalGuardTests` が禁止する。
- 削除: `ProfileSubSettingBase.OnSubPropertyChanged`（購読者 0 件の孤立イベント。承認済み。`PropertyChanged` は残す）、`ProfileApplicationService` の未使用の依存 `IDeviceStateService`、`ControlService.USING_MAX_CONTROLLERS`（静的シム）、孤立定数 4 件（`DEFAULT_STICK_OUTPUT_CURVE`／`_ID`、`DEFAULT_SA_OUTPUT_CURVE`／`_ID`。廃止済みの `ProfileDTO` の残骸）。
- 接続: `IEnvironmentService.UsingMaxControllers`（Step2 の「接続」判断の実装）。`AutoProfileHolder`（保存時の 4〜8 番目の書き出し。`AutoProfilesDTO.MapFrom` が `source.UsingMaxControllers` を読む）、`AutoProfilesViewModel`（`IEnvironmentService` を受ける）、`ProgramItem`（生成元が値を渡す）。`DEFAULT_CHECK_EVERY_VALUE`／`_UNIT` は `checkEveryValue`／`checkEveryUnit` の初期化子に接続。
- 温存（判断済み）: `IOutputSlotService.OutputSlotChanged`（購読者 0 件。トレースログを維持するため。TODO を「Phase8 以降の UI 通知の拡張点」に更新）。`MainWindow` の引数 18 個（全部使用中。画面の責務の見直し［Phase8 以降］で扱う）。
- 未接続のまま残した孤立コード: `ColorUtil.applyRatio(Color, Color, uint)` と `HuetoRGB(float, float, float)`（移動前から呼び出し元 0 件。TODO を記載。削除は copilot-instructions §2.4 の手順［経緯の調査と承認］で判断する → Step10e で判断）。
- 新たな持ち越し: `ControlService.CURRENT_DS4_CONTROLLER_LIMIT`（静的シム。30 箇所以上、16 ファイル、テスト 5 件が使う）を `IEnvironmentService.ControllerSlotLimit` の注入に置き換える → `BackingStore` の分は 10d、画面・サービスの分は 10e。
- 持ち越し（10c）: `configFileDecimalCulture`、`CompileVersionNumber*`、設定の定数（`CONFIG_VERSION` ほか）、`LoadActions`／`SaveAction` などの薄いシム。持ち越し（10d）: `Global.store`、環境・パスの窓口、`ProfileSettingsServiceInstance` 経由の設定の窓口、`using static DS4Windows.Global;`。
- 実機確認: 10b 単独の項目なし（10e の総合検証に、トレイアイコンの切り替え、自動プロファイル画面［8 台表示］、ライトバーの色の遷移、自動プロファイルの保存を追加）。

## 15. Phase7-Step10c で更新した台帳（2026-10-18）

- `Global.*`: 93 → **69**（W2 −3［`SpecialActionRepository`。基準値の更新を W7 で実施］、W3 −10［`BackingStore` 9、`AppSettingsService` 1］、W4 −4、W5 −7）。`AppHost.GetService|Services` 4、`Program.rootHub` 0、`ServiceProviderHolder` 17 は変更なし。Phase7 開始時（733）からの累計は −664。基準値は `Step10FourGlobalRatchetGuardTests`（合計 0／4／69）。
- `BackingStore.cs`: **6,522 行 → 1,660 行**（設定の状態［公開のフィールド・プロパティ］と、名前引き・後処理の補助だけ）。切り出した型（いずれも `DS4Control/Services/`、具象クラスの Singleton。`BackingStore` を `store` として受ける）:
  - `SpecialActionsXmlSerializer`（465 行。`Actions.xml`）、`LinkedProfilesXmlSerializer`（281 行）、`ControllerConfigsXmlSerializer`（186 行）。
  - `AppSettingsXmlSerializer`（556 行。`Profiles.xml`。`IPathService`／`IEnvironmentService` を受ける）。
  - `LegacyProfileXmlSerializer`（3,093 行。プロファイルの読込・保存。`IPathService`／`IProfileSettingsService`／`IEnvironmentService` を受ける）。
  - `ProfilePresets`（604 行。プリセット 10 種と `PrepareBlankingProfile`）。
  - `VersionUtil`（バージョンの数値化。静的）。
- 削除（`Global` から）: `SaveAction`／`SaveActions`／`RemoveAction`／`ReloadActionsAndVerify`／`LoadActions`（→ `SpecialActionRepository` のプライベートメソッドと `SpecialActionsXmlSerializer`）、`CreateStdActions`（→ `SpecialActionRepository`）、`CompileVersionNumberFromString`／`CompileVersionNumber`（→ `VersionUtil`）。再導入は `Step12DeadShimRemovalGuardTests` が禁止する。
- 別名として残した定数（`Global`）: `CUSTOM_EXE_CONFIG_FILENAME`、`CONFIG_VERSION`、`XML_EXTENSION`（実体は各シリアライザーの定数。利用箇所をなくしたら削除 → 10d）。
- 削除（`BackingStore` から）: 作業用の `m_Xdoc`（各シリアライザーが自分のフィールドとして持つ）。永続化のメソッド 36 個とプリセット関連 11 個。
- `BackingStore` に残し、公開にした補助: `ResetProfile`／`ResetMouseProperties`／`PostLoadSnippet`／`CheckOldDevicestatus`（`LegacyProfileXmlSerializer` と `ProfilePresets` が呼ぶ）、`RaiseActionsLoaded`（`ActionsLoaded` イベントの発行元）、`PostProcessLoad`。
- 計画との差: (1) W4＝読込側、W5＝保存側に入れ替えた（`LoadProfile` が `SaveProfile` を呼ぶため。逆向きの依存を作らない）。(2) 移動先の `Global` の参照を、注入された窓口に替えた: `Global.appdatapath` → `IPathService.AppDataPath`（未設定のときの代替が違う。デバッグログとプロファイルの置き場所の組み立てのみで、本番では起きない）、`Global.exedirpath` → `IPathService.ProgramDirectory`、`Global.AdminNeeded`／`IsAdministrator`／`exeversion` → `IEnvironmentService`、`Global.useDInputOnly` → `IProfileSettingsService.UseDInputOnlyArray`、`Global.GetAction`／`GetActionIndexOf` → `store.GetAction`／`GetActionIndexOf`（本体では `Global.store` と同じ実体）。
- ゴールデンテスト（`DS4WindowsTests/Golden/Phase7Step10c/` の 11 ファイル）: プロファイル（既定／変化させた状態の保存 XML、読込後の状態）、アプリ設定（同）、`Actions.xml`、リンクプロファイル、プリセット 10 種の読込後の状態。**対象外**: コントローラー固有設定（実機のデバイス実体が要る）。
- 持ち越し（10d）: `BackingStore` に残る `Global` 参照 7（`appdatapath`×3［`m_Actions`／`m_linkedProfiles`／`m_controllerConfigs` の初期化子］、`ProfileSettingsServiceInstance`×2、`useDInputOnly`、`RefreshActionAlias`）、`Global.store`、環境・パスの窓口、`using static DS4Windows.Global;`、別名（上記）、`CURRENT_DS4_CONTROLLER_LIMIT`（`BackingStore` の分 7）。`configFileDecimalCulture`（9 件。DTO と画面）の置き場所。
- 実機確認: **U-20**（設定ファイルの互換。新設・未）。
