# Phase6-Step9b 完了報告書: PR #1 の Copilot レビュー指摘 14 件の検証と是正

完了日: 2026-10-14  
対象ブランチ: `For-DI-migration-work`  
計画書: `Phase6-Step9b-Plan.md`（各指摘の検証結果は §1、決定は §2A、実施記録は §2B）  
参照: PR #1（`gwin7ok/DS4Windows-Vader4Pro`、`For-DI-migration-work` → `master`）への Copilot の自動レビュー（2026-10-13 22:54 JST、指摘 14 件）

---

## 1. 結果の要約

- 指摘 14 件を、現行コードと置換前の実装（`git log -S`、`git show`）を比べて検証し、分類した。自動レビューの指摘をそのまま採用せず、稼働経路か・実害があるかで判断した。
- 分類A（稼働経路で実害。修正）: 4 件。F02、F04、F05、F06。
- 分類B（未配線・呼び出し元なし。TODO または是正のみ）: 2 件。F03、F07。
- 分類C（誤検出）: 1 件。F01。
- テスト品質の是正: 3 件。F10、F11、F14。
- 別 Step へ配分: 4 件。F08・F09（Step10）、F12・F13（Step10b。台帳に既出）。

## 2. 各指摘の結論

### 分類A（修正した）
- **F02 `IManagedActionManager` の未登録**: 2026-09-04（`31dddc43`、自動プロファイル設定コレクションの追加）が、無関係な差分として `IManagedActionManager`・`IControllerRegistry`・`IActionRegistry` の登録を巻き込み削除していた。以降、`ActionManager.SetToggledOn` が例外になり（呼び出し元が握りつぶす）、Toggle 型キーの `IsToggledOn` が true にならなかった。`IManagedActionManager`→`DefaultActionManager`、`IControllerRegistry`→`DefaultControllerRegistry` を再登録した（`IActionRegistry` は実装が静的クラスで呼び出し元もないため対象外）。
  - 再登録で挙動が変わらないよう、`DefaultActionManager.ClearDeviceState` を静的ファサード版に揃えた（`Mapping.HandleDeviceDisconnect`。プロファイル適用時の押しっぱなし合成キーの解放を維持）。
  - 実機ログで、Controls タブ・SpecialActions タブの両方で、トグルのオン／オフ、リピートの開始／停止、トグルオン中のプロファイル切替でのキー解放を確認した。
- **F04 ログ設定の `NullReferenceException`**: `NLog.config` の `async="false"`（2025-12 から）で `logfile` が直接の `FileTarget` になり、ラッパー前提の `UpdateLogSettings`／`RestoreArchiveSetting` が例外になっていた。例外が `Save()` より前に起きるため、設定が保存されず、NLog.config の更新も行われなかった。`FindLogFileTarget`（ラッパー内と直接の両方に対応）に統一した。DI 移行の回帰ではない既存不具合。
- **F05 `DefaultMacroPlayer` の 4 スロット固定**: 配列長と範囲判定を `Global.MAX_DS4_CONTROLLER_COUNT`（8）にした。`Mapping.PlayMacroDirect`／`EndMacroDirect` の `>= 4` も揃えた。既定の 4 スロットでは変化なし。
- **F06 `LaunchProcessAction`**: 置換前（`Mapping.cs` の `specActionLaunchProc`）の仕様へ戻した。`extra` の `$hidden`（大文字小文字不問）のときだけ、その語を取り除き非表示で起動（`.bat`／`.cmd` は `%COMSPEC% /C`）。それ以外は `UseShellExecute=true`、非表示にせず `extra` をそのまま引数に渡す。実機で、引数なし・引数付き・`$hidden`・ショートカットを確認した。

### 分類B（動作は変えない／是正のみ）
- **F03 `MouseOutputAction`**: 未登録・未完成。`Mapping.cs` の `GetService(typeof(MouseOutputAction))` は常に null で、従来の `PerformMouseButtonEvent` が実際の出力経路。登録すると左クリックなどが出なくなるため、クラスと `Mapping.cs` の 2 箇所に「登録してはならない」旨の TODO を付与した（Phase8 で完成させる）。
- **F07 `ServiceProviderHolder.GetRequiredService`**: 呼び出し元 0 件。§2.4 に従い、全体計画書のシム例が使用を想定した契約のため削除せず、未設定・未登録なら `InvalidOperationException` を投げる実装にした。

### 分類C（誤検出）
- **F01 プロファイル切替の 250ms デバウンス**: 250ms のデバウンスは 2026-09-16（`94066f3c`）に消えたが、機能上の安全弁は Fresh-press ガード（`RequiresFreshPressAfterReset`、80ms の未成立継続で解除）・`IsExecuting`・`ProfileActionChainService` の Profile 型チェーンのスキップに置き換わっており、稼働経路では `Mapping.cs` の上流で必ず働く（Phase5-Step14 で実機確認済み）。コード変更なし。`copilot-instructions.md` §2.2 の記述が現状と食い違っていたため、現行の機構へ更新した。残る弱点（`IProfileSwitcher` を直接呼ぶ新しい経路では効かない）は Phase8 計画書へ申し送った。

### テスト品質
- **F10**: `ProfileSettingsServiceSubSettingsTests` の 6 テストで、前提が満たされないと `return` で成功扱いになる条件分岐を `Assert.NotNull` に置き換えた。Copilot の指摘位置は `ProfileUnifiedPersistenceTests.cs`（2 テスト、同じパターン）で別ファイルだったため、PR 返信の準備中に追加で修正した（コミット・テスト実行前。§7）。
- **F11**: `SpecialActionsListViewModelTests` の購読の監査を、発行側（サービス）のデリゲートの購読者を調べる動作による検証に置き換えた。当該 ViewModel は `IDisposable` ではなく、サービスのイベントを購読していない（計画書の「購読と解除の検証」は「購読していないことの検証」に置き換えた）。
- **F14**: `StandaloneTests` の正規化テストを、`Util` と `Global` の独立した 2 実装の比較と期待値の固定に書き直した。

## 3. 付随の修正（実機確認・テスト実行で見つかったもの）
- `ProfileSettingsViewModelGlobalReferenceGuardTests` の `UriFormatException`（`pack://` のスキームが `Application` の生成で登録されるが、このクラスに準備がなく実行順に依存していた既存テストの不具合）: 他の ViewModel テストと同じ準備を静的コンストラクタに追加した。
- `ServiceProviderHolder`（静的）を差し替えるテストが並列に走る問題: `ManagedActionManagerRegistrationTests` と `ToggleAndToggledOnTests` を同じ xUnit コレクション `"ServiceProviderHolder"` で直列化した。
- 「Launch Program」タブのプログラム名ラベルの固定幅 150 を解消した（`SpecialActionEditor.xaml`。親の幅いっぱいを使い、アイコンは右端、長い名前は末尾を省略）。

## 4. 決定（2026-10-14）
決定1＝A（F03 は TODO のみ）、決定2＝A（F02 は再登録）、決定3＝A（F07 は例外を投げる）、決定4＝A（F06 は置換前の仕様）、決定5＝B（PR への返信は内容を提示し、許可を得たものだけ書き込む。Step9b 完了後にまとめて）、決定6＝A（`copilot-instructions.md` §2.2 の更新）。

## 5. 変更したファイル（主なもの）
- 本番コード: `DS4Windows/Core/Logging/LoggerHolder.cs`、`DS4Windows/DI/ServiceRegistration.cs`、`DS4Windows/DI/ServiceProviderHolder.cs`、`DS4Windows/Actions/DefaultActionManager.cs`、`DS4Windows/Actions/DefaultMacroPlayer.cs`、`DS4Windows/Actions/LaunchProcessAction.cs`、`DS4Windows/Actions/MouseOutputAction.cs`（TODO のみ）、`DS4Windows/DS4Control/Mapping.cs`（F05 の範囲判定、F03 の TODO）、`DS4Windows/DS4Forms/SpecialActionEditor.xaml`。
- テスト: `LoggerHolderFindLogFileTargetTests`（新規）、`ManagedActionManagerRegistrationTests`（新規）、`LaunchProcessActionTests`（書き直し）、`DefaultActionManagerTests`（F05 追加）、`ProfileSettingsServiceSubSettingsTests`（F10）、`SpecialActionsListViewModelTests`（F11）、`StandaloneTests/TestSpecialActionUtils`（F14）、`ProfileSettingsViewModelGlobalReferenceGuardTests`、`ToggleAndPressedOnceTests`（コレクション属性）。
- 文書: `Phase6-Step9b-Plan.md`、`Phase6-Status.md`、`Phase6-Plan.md`、`Phase6-Step10-Plan.md`（付随の整理項目）、`Phase6-Step11-Plan.md`（先送り台帳）、`Phase8-Unified-Trigger-Dispatch-Plan.md`（申し送り）、`Phase2-Bugfix-ProfileSwitch-CascadeLoop.md`（その後の経緯）、`DI-App-Wide-Migration-Plan.md`、`.github/copilot-instructions.md` §2.2、`Model-Diagram/04-Service-Lifecycle-Spec.md`。

## 6. 後続への申し送り
- **Step10**: F08（サービス実装のコンストラクタのフォールバック。`DefaultActionManager` のコンストラクタも含む）、F09（`ControllersViewModel`）、`SpecialActionsListViewModel` の未使用フィールド `actionManager`、`ProfileApplicationService` の `deviceIndex >= 4`（K1）。`Phase6-Step10-Plan.md` 冒頭に追記済み。
- **Step10b**: F12（`INotificationService.cs`）、F13（`Trigger.cs`）。台帳に既出。
- **Step11 の先送り台帳**: 8 スロット拡張時のデバイス 4〜7 のマクロ（`DefaultMacroPlayer`。単体テストで固定済み）。
- **Phase8**: `MouseOutputAction` の完成、プロファイル切替ガードの統一経路での維持、`IManagedActionManager`／`IControllerRegistry` の再登録の前提、プログラム起動仕様の維持。`Phase8-Unified-Trigger-Dispatch-Plan.md` 末尾に追記済み。
- **持ち越し（変更なし）**: K7b-1（異常終了。先送り）、K9-3（プリセットメニューのサブメニュー）、K7-1。

## 7. PR #1 のレビューコメントへの返信文面（確定版）
- **投稿状況（2026-10-14）**: 14 スレッドすべてに、下記の返信を投稿し、すべて解決済み（resolved）にした（`gh`、fine-grained PAT。スレッドの解決には、Pull requests に加えて Contents の書き込み権限が必要だった）。
- スレッドの URL とコメント ID は、`https://github.com/gwin7ok/DS4Windows-Vader4Pro/pull/1#discussion_r<ID>`。
- **F10 の指摘の実体**: Copilot の指摘位置は `DS4WindowsTests/ProfileUnifiedPersistenceTests.cs`（13〜14 行）で、計画書で想定していた `ProfileSettingsServiceSubSettingsTests.cs` とは別のファイルだった。Step9b-4 では後者のみを直しており、前者は PR 返信の準備中に追加で修正した（2 テストの `return` を `Assert.NotNull` に置換。dc8e16f7、ビルド・テスト成功、反映済み）。

いずれも「返信を投稿し、スレッドを解決済みにする」（ユーザー指示。F08・F09・F12・F13 は後続 Step での対応を明記した返信で解決する）。

#### F01（コメント ID 4259525776、`DefaultProfileSwitcher.cs`）
検証の結果、誤検出（コード変更なし）と判断しました。

ご指摘のとおり、250ms のデバウンスは 2026-09-16（94066f3c、通知レガシー経路の削除）で `DefaultProfileSwitcher` から撤去されています。ただし、連鎖発火（カスケード）の防止は別の機構に置き換わっており、稼働経路では `Mapping.cs` の上流で必ず働きます。

- `ActionInstanceState.RequiresFreshPressAfterReset`: プロファイル適用で `ClearDeviceState` が状態を新品にし、トリガーが 80ms（`FreshPressReleaseHoldMs`）連続して未成立になるまで、新規成立とみなさない
- `ActionInstanceState.IsExecuting`: 同一アクションの実行中の再実行を防止
- `ProfileActionChainService`: Profile 型のチェーンをスキップ

`IProfileSwitcher.SwitchProfile` の呼び出し元は `ProfileSwitchAction.Execute` のみで、`Mapping.cs` のゲートを通ります。実機でカスケードしないことを確認済みです（Phase5-Step14）。

`.github/copilot-instructions.md` §2.2 の記述を現行の機構に更新しました。また、統一ディスパッチ（Phase8）で `IProfileSwitcher` を直接呼ぶ経路を作る場合は同等のガードが必要なため、Phase8 計画書へ申し送り済みです（Phase6-Step9b、a3f62132）。

#### F02（コメント ID 4259525859、`IManagedActionManager.cs`）
ご指摘のとおりでした。2026-09-04 の別コミット（31dddc43）が、`IManagedActionManager`・`IControllerRegistry`・`IActionRegistry` の登録を巻き込み削除していました。

`ServiceRegistration` に `IManagedActionManager`→`DefaultActionManager` と `IControllerRegistry`→`DefaultControllerRegistry` を再登録しました（`IActionRegistry` は実装が静的クラスで呼び出し元もないため対象外）。あわせて、再登録で挙動が変わらないよう、`DefaultActionManager.ClearDeviceState` を静的ファサード版と同じく `Mapping.HandleDeviceDisconnect` を呼ぶ形に揃えました。回帰テスト（登録、`SetToggledOn`、Controls タブ経由の Toggle 遷移）を追加し、実機ログでトグルのオン／オフとプロファイル切替時のキー解放を確認しています（Phase6-Step9b-2、3c4bc504）。

#### F03（コメント ID 4259525929、`MouseOutputAction.cs`）
ご指摘のとおり、`MouseOutputAction` は未完成です。ただし、現時点では実害はありません。

このクラスは `ServiceRegistration`・`DefaultActionFactory`・`ActionFactory` のいずれにも登録されておらず、`Mapping.cs` の `sp.GetService(typeof(MouseOutputAction))` は常に null になります。そのため、従来の `VirtualKBM.PerformMouseButtonEvent` の経路が実際の出力を行っています。一方、登録すると `mouseHandled=true` となり、左クリックなどが出なくなる危険があります。

動作は変えず、クラスと `Mapping.cs` の 2 箇所に「登録してはならない」旨と完成時期（Phase8: Controls／SpecialActions 統一トリガーディスパッチ）を明記した TODO を付けました。Phase8 計画書にも申し送り済みです（Phase6-Step9b-3、5d756028）。

#### F04（コメント ID 4259525997、`NLog.config`）
ご指摘のとおりでした（`async="false"` は 2025-12 からの既存の不具合で、DI 移行の回帰ではありません）。

`UpdateLogSettings`／`RestoreArchiveSetting` の例外は、`SettingsViewModel` のセッターで `_appSettings.Save()` より前に起きるため、ログレベルとアーカイブ数の設定が保存されず、NLog.config の更新も行われない副作用がありました。

非同期ラッパー内と直接の `FileTarget` の両方に対応する `LoggerHolder.FindLogFileTarget` を追加し、コンストラクタ・`UpdateLogSettings`・`RestoreArchiveSetting` で共用しました。回帰テスト 4 件を追加し、実機で設定の保存・反映と `Log settings updated:` のログを確認しています（Phase6-Step9b-1、056b5ebc）。

#### F05（コメント ID 4259526075、`DefaultMacroPlayer.cs`）
ご指摘のとおりでした。`DefaultMacroPlayer` の配列長とガードを `Global.MAX_DS4_CONTROLLER_COUNT`（8）に揃えました。呼び出し先の `Mapping.PlayMacroDirect`／`EndMacroDirect` の `>= 4` も同様に修正しています。既定の 4 スロット構成では挙動は変わりません。

単体テスト（`DefaultMacroPlayer_HandlesAllControllerSlots_NotJustFour`）で固定しました。8 スロット構成のデバイス 4〜7 での実機確認は、機材の都合で Step11 の先送り台帳に登録しています（Phase6-Step9b-3、5d756028）。

#### F06（コメント ID 4259526137、`LaunchProcessAction.cs`）
ご指摘のとおりでした。DI 移行前の仕様（`Mapping.cs` の `specActionLaunchProc`）と比較したところ、現行の `LaunchProcessAction` が仕様から外れていました。置換前の仕様に戻しています。

- `extra`（Arguments）に `$hidden`（大文字小文字不問）があるときだけ、その語を 1 回取り除き、非表示で起動（`.bat`／`.cmd` は `%COMSPEC% /C "…" 引数`）
- それ以外は `UseShellExecute=true` の OS の既定動作で起動し、`extra` をそのまま引数に渡す（非表示にしない。ショートカットや書類の起動も維持）

`LaunchProcessActionTests` を新仕様に書き直し、実機で、引数なし・引数付き・`$hidden`・ショートカットの起動を確認しています（Phase6-Step9b-3、5d756028）。

#### F07（コメント ID 4259526216、`ServiceProviderHolder.cs`）
ご指摘のとおりです。呼び出し元は 0 件で実害はありませんでしたが、名前の契約に合わせて是正しました。プロバイダー未設定、またはサービス未登録のときは、null を返さず `InvalidOperationException` を投げる実装にしています。削除せず契約を保持したのは、全体計画書のシム例が使用を想定している契約のためです。単体テスト 3 件（登録済み／未登録／プロバイダー未設定）を追加しました（Phase6-Step9b-2、3c4bc504）。

#### F08（コメント ID 4259526286、`ProfileActionChainService.cs`）
ご指摘のとおり、Pure DI 違反です。同じパターンがほかのサービス実装のコンストラクタ（`ProfileApplicationService`、`DefaultActionManager` など）にもあるため、個別には直さず、Phase6-Step10 で洗い出してまとめて是正します（`Phase6-Step10-Plan.md` の台帳に登録済み）。コード変更はこの PR の範囲外としています。

#### F09（コメント ID 4259526566、`ControllersViewModel.cs`）
ご指摘のとおり、Pure DI 違反です。主要 4 ViewModel と同じ方針（依存を必須引数にし null を拒否する）で、Phase6-Step10 で是正します（`Phase6-Step10-Plan.md` の台帳に登録済み）。コード変更はこの PR の範囲外としています。

#### F10（コメント ID 4259526717、`ProfileUnifiedPersistenceTests.cs`）
ご指摘のとおりでした。サブ設定が null のときに `return` して成功扱いになる条件分岐を `Assert.NotNull` に置き換え、前提が満たされないときは失敗するようにしました。同じパターンの `ProfileSettingsServiceSubSettingsTests`（6 テスト）も同様に修正しています（Phase6-Step9b-4、6c16b5cb）。

#### F11（コメント ID 4259526850、`SpecialActionsListViewModelTests.cs`）
ご指摘のとおり、旧テストは購読を検出できていませんでした。発行側（サービス）のデリゲートの購読者（`Delegate.Target`）を調べる `CountSubscriptions` に書き換え、(1) 偽の発行側・購読者で検出器自身を検証、(2) 実際の Singleton（`ISpecialActionRepository`、`IProfileRepository`、`IOutputSlotService`、`IProfileSettingsService`、`IManagedActionManager`）と `ActionManager`・`Global` の静的イベントに、ViewModel が購読していないことを検証する形にしました。

なお、この ViewModel は `IDisposable` ではなく、サービスのイベントを購読していないため（購読するのは自身の `actionCol.CollectionChanged` と各項目の `PropertyChanged` のみ）、「購読と解除」ではなく「購読していないこと」を検証しています（Phase6-Step9b-4、6c16b5cb）。

#### F12（コメント ID 4259527009、`INotificationService.cs`）
ご指摘のとおり、1ファイル1型の違反です。リポジトリ全体の複数型ファイル 55 件（本ファイルを含む）を一括して分割する Phase6-Step10b の台帳に登録済みです。過渡期ルール（`copilot-instructions.md` §3.3 原則1）に従い、機能変更を含む PR では分割せず、Step10b でまとめて対応します。

#### F13（コメント ID 4259527123、`Trigger.cs`）
ご指摘のとおり、1ファイル1型の違反（ファイル名がどの型名とも不一致）です。リポジトリ全体の複数型ファイル 55 件（本ファイルを含む）を一括して分割する Phase6-Step10b の台帳に登録済みです。機能変更を含む PR では分割しない過渡期ルールに従い、Step10b でまとめて対応します。

#### F14（コメント ID 4259527192、`TestSpecialActionUtils.cs`）
ご指摘のとおりでした。`Util.NormalizeActionName` と `Global.NormalizeActionName`（独立した 2 つの実装）を、複数の入力（前後の空白・タブ・改行・空文字・null）で比較し、あわせて期待値も固定するテストに書き直しました（Phase6-Step9b-4、6c16b5cb）。
