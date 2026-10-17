# Phase6-Step9b 計画書: Copilot レビュー（PR #1）指摘 14 件の検証と是正

作成日: 2026-10-14  
状態: **完了（2026-10-14）。Step9b-0〜9b-5。完了報告書 `Phase6-Step9b-Completion-Report.md`。PR #1 への返信 14 件の投稿と、スレッドの解決は完了（決定5＝B、2026-10-14）**  
対象ブランチ: `For-DI-migration-work`  
位置づけ: Step9 の後・Step10 の前（Step7b と同じく、実機確認・外部レビューで見つかった事項を扱う追加 Step）  
上位計画書: `docs-forDIMG/MadeByAgent/Phase6-Plan.md`  
準拠指針: `.github/copilot-instructions.md`（特に §2.1〜2.4、§3.1、§3.3、§4）  
参照: PR #1（`gwin7ok/DS4Windows-Vader4Pro`、`For-DI-migration-work` → `master`）への Copilot の自動レビュー（2026-10-13 22:54 JST、COMMENTED、指摘 14 件。依頼は 22:46 JST に gwin7ok アカウントから）

---

## 0. 背景と方針

### 0.1 経緯
- 2026-10-13、PR #1（作業ブランチ全体の PR。コミット 1013 件）に Copilot の自動コードレビューが付いた。判定は「Changes recommended」、指摘は 14 件（High 4、Medium 7、Low 3）。
- 2026-10-14 に、指摘を現在のブランチ（Step9 完了時点、HEAD `83845149`）で簡易確認した。指摘の該当箇所はほぼすべて残っている。ただし、自動レビューのため、実害の有無（稼働経路かどうか）は未検証。
- 指摘の一部は、`copilot-instructions.md` §2.2（現在の機能の完全維持。250ms デバウンスなどの安全弁は 100% 維持）に反する可能性がある。Step10 以降の移行でさらに積み上げる前に、検証と是正を行う。

### 0.2 方針
1. **まず検証する**（Step9b-0）。自動レビューの指摘を鵜呑みにせず、現行コードと、置換前の実装（`git log -S`／`git show <commit>^:<path>`）と比べて、次の 3 つに分類する。
   - 分類A: 稼働経路で実害がある（修正する）
   - 分類B: 未配線のコード（系統A など）で、現時点の実害はない（TODO コメントを付けて Phase8 へ。§3.3 原則4）
   - 分類C: 誤検出（何もしない。理由を記録する）
2. **配分**: 機能・安全に関わるもの（F01〜F07）と、テストの品質（F10、F11、F14）は本 Step。Pure DI 違反（F08、F09）は Step10。1ファイル1型違反（F12、F13）は Step10b（台帳に既に載っている）。
3. 修正は小さなマイクロステップに分け、ユーザーのビルド・テスト・実機確認を待つ（Step9 と同じ）。

---

## 1. 指摘の台帳（Copilot の指摘 14 件と配分）

重大度は Copilot の判定。「簡易確認」は 2026-10-14 時点の現行コードでの確認。

### F01（High）プロファイル切替の 250ms デバウンスが落ちている
- 指摘: `Actions/DefaultProfileSwitcher.SwitchProfile` が、デバイスごとの 250ms デバウンスなしで `ApplyFromAction` を呼ぶ。押しっぱなしのトリガーが、切替先のプロファイルでも成立し、連鎖（カスケード）する。
- 簡易確認: `DefaultProfileSwitcher.cs:21〜23` にガードが無い。`Phase2-Bugfix-ProfileSwitch-CascadeLoop.md` には、同ファイルにデバウンスを導入したと記録されている。Phase5-Step14 の「`SwitchProfile` 一本化」で落ちた可能性がある。一方、`Mapping.cs` 側の稼働経路（`ApplyProfileDirect`）や `ProfileApplicationService.ApplyFromAction` に別のガードがあるかは未確認。
- 配分: Step9b。検証後、分類A なら修正。
- 実機確認（案）: 同じトリガーを各プロファイルに割り当てた構成（5-3 テスト）で、1 回の短押しにつき 1 段階だけ切り替わること。ログ: `ApplyProfile CALLED: device=…, profile=…`（Debug）の回数。
- **【Step9b-0 検証結果】分類C（誤検出。デバウンスは別の機構に置き換わっている）**:
  - 事実: 250ms デバウンスは `134aae60`（2026-08-28）で `DefaultProfileSwitcher` に導入され、`94066f3c`（2026-09-16、通知レガシー経路の削除）で `SwitchProfile` を `Global.Instance` へ委譲する形にした際に消え、その後 `profileAppService?.ApplyFromAction` への薄い委譲になった。指摘の「ガードが無い」は事実。
  - ただし機能上の安全弁は置き換わっている。稼働経路は `Mapping.MapCustomAction`（Profile 型）→ `DispatchInputEdge` → `ProfileSwitchAction.Execute` → `DefaultProfileSwitcher.SwitchProfile`（呼び出し元はこの 1 経路のみ）。`Mapping.cs` の上流に、(1) `RequiresFreshPressAfterReset`（プロファイル適用で `ClearDeviceState` が状態を新品にし、トリガーが 80ms［`FreshPressReleaseHoldMs`］連続して未成立になるまで新規成立とみなさない）、(2) `IsExecuting`（実行中の再実行防止）、(3) `ProfileActionChainService` の Profile 種別チェーンのスキップ、がある。これらは Phase5-Step14 の Issue8-1 とカスケードループ根本解決（`Phase5-Status.md` §2.1。実機確認済み）で導入された。
  - 結論: 稼働経路でカスケードしない。コードの変更は不要。ただし `copilot-instructions.md` §2.2 の「カスケードループ防止ガード（250ms デバウンス）」と `Phase2-Bugfix-ProfileSwitch-CascadeLoop.md` の記述が現状と食い違うため、文書の更新を提案する（`copilot-instructions.md` はユーザーの規約のため、承認を得てから）。
  - 残る弱点（実害は未確認）: 安全弁が `Mapping.cs` の上流だけにあり、`IProfileSwitcher` を直接呼ぶ新しい経路（Phase8 の統一ディスパッチ）では効かない。Phase8 の計画書へ申し送る。

### F02（High）`IManagedActionManager` が DI に登録されていない
- 指摘: `ServiceRegistration` に登録が無いのに、`ActionManager` のファサードと `AppHost.PreallocateActionEntries` が解決しようとする。`DefaultActionManager` が使われず、`ActionManager.SetToggledOn` が失敗し（コントローラーが握りつぶす）、トグル状態の通知が失われる。
- 簡易確認: `ServiceRegistration` に `IManagedActionManager` の登録が無い。`ActionManager.cs` に `sp.GetService(typeof(IManagedActionManager))` が 8 箇所以上ある。`DefaultActionManager.cs` は存在する。
- 配分: Step9b。登録するか、ファサード側の利用をやめるかは決定事項（Step9b-0）。
- **【Step9b-0 検証結果】分類A（稼働経路で実害。DI 移行に伴う回帰）**:
  - 経緯: 登録は `d9ea1cf7`（2025-12-18、App.xaml.cs）で追加され、`cc7a556b` で `ServiceRegistration` へ移った。`31dddc43`（2026-09-04、「自動プロファイル設定コレクション・実行サービスを追加」）の差分で、同じコミットが `IActionRegistry`・`IControllerRegistry`・`IManagedActionManager` の 3 件の登録を削除した（コミットの趣旨と無関係な巻き込み削除に見える）。以降、`DefaultActionManager` は一度も使われていない。
  - 実害（コードで確認）: `ActionManager.SetToggledOn`（静的ファサード）はマネージャーが無いと例外を投げる（フォールバックなし）。呼び出し元の `KeyButtonActionController`（Toggle 型。179・208 行）は `catch {}` で握りつぶす。結果、`ActionInstanceState.IsToggledOn` が true にならず、`OnDown` の `wasPressed` が常に false になるため、Toggle 型の「2 回目の押下でトグルオフ」が状態に基づいて行われない。`ToggledOnChanged` イベントも発火しない。ほかの `ActionManager` のメソッドは静的実装へフォールバックするため動く（状態も静的辞書に持つ）。
  - 実機での症状は未確認（Toggle 型キー割り当てのトグルオン／オフ、リピーターの停止を確認する必要がある）。
  - 関連（同じコミットの巻き込み）: `IControllerRegistry` 未登録のため、`DefaultKeyButtonActionControllerFactory` が登録をスキップし、`App.xaml.cs` 終了処理（1079〜1085 行）のコントローラー破棄が空振りする。`IActionRegistry`（系統A）の登録は、呼び出し元が無く実害なし（分類B）。
  - 補足: `SpecialActionsListViewModel` の `actionManager` フィールドは代入のみで未使用のため、null でも影響なし（F09 と同様、Step10 で整理）。
  - 提案: `ServiceRegistration` に `IManagedActionManager`→`DefaultActionManager`（と `IControllerRegistry`→`DefaultControllerRegistry`）を再登録する。`AppHost.PreallocateActionEntries` が登録済みなら動く設計のため、`ActionManager` ファサード側の変更は不要。再登録により、静的な状態から `DefaultActionManager` の状態へ切り替わるため、`ClearDeviceState` やトグルの挙動を回帰テストと実機で確認する。

### F03（High）`MouseOutputAction` がマウスイベントを出さない
- 指摘: `Execute`／`Stop` が `IVirtualKBM` を取得するだけで呼ばない。テストは「例外が出ない」ことしか見ていない。
- 簡易確認: `Actions/MouseOutputAction.cs` に `PerformMouse…`／`MoveRelativeMouse` の呼び出しが無い。`Mapping.cs:1266〜1360` が、`MouseOutputAction` を DI から取得する経路を持つ（「new DI route + fallback preserved」）。DI 登録の有無と、この経路が稼働するかは未確認。
- 配分: Step9b。経路が稼働していれば、マウスボタン割り当てが効かなくなる重大な不具合のため、最優先で検証する。
- **【Step9b-0 検証結果】分類B（未配線。現時点の実害なし。ただし将来の地雷）**:
  - `MouseOutputAction` は `ServiceRegistration` にも `DefaultActionFactory`／`ActionFactory` にも登録されていない。`Mapping.cs:1283`・`1360` の `sp.GetService(typeof(MouseOutputAction))` は、Microsoft DI が未登録の具象型を自動生成しないため常に null になり、`mouseHandled=false` で従来どおり `VirtualKBM.PerformMouseButtonEvent` が呼ばれる（マウスボタン出力は稼働）。
  - 登録すると、`Execute` が何も出力しないのに `mouseHandled=true` となり、左クリックが出なくなる。
  - 提案: `MouseOutputAction` に、未完成であること・登録してはならないこと・Phase8 で完成させることを書いた TODO（§3.3 原則4）を付け、`Mapping.cs` の 2 箇所にも注記する。コードの動作は変えない。

### F04（High）`NLog.config` の `async="false"` で、ログ設定の変更が `NullReferenceException`
- 指摘: 非同期ラッパーを無効にすると、`logfile` が直接の `FileTarget` になる。`LoggerHolder.UpdateLogSettings`／`RestoreArchiveSetting` は `FindTargetByName<WrapperTargetBase>("logfile").WrappedTarget` を前提にしているため、設定変更で例外になる。
- 簡易確認: `NLog.config:6` は `async="false"`。`LoggerHolder.cs` の `UpdateLogSettings`（180 行付近）・`RestoreArchiveSetting` が同じ前提。Step9-3 の実機確認でログ設定を変えた際のログにも `Log settings updated:` の行が見当たらず、整合する。例外は WPF のバインディングが握りつぶすため、画面では気づきにくい。
- 配分: Step9b。直接の `FileTarget` とラッパーの両方に対応させる。
- 実機確認（案）: 設定タブでアーカイブ数と最小ログレベルを変えた直後に、`Log settings updated: MaxArchiveFiles=…, MinLevel=…`（Debug）が出ること、レベルがその場で反映されること。
- **【Step9b-0 検証結果】分類A（稼働経路で実害。既存不具合で、DI 移行の回帰ではない）**:
  - `NLog.config` の `async="false"` は `70dbf3bc`（2025-12-15）から。`UpdateLogSettings`／`RestoreArchiveSetting` の `wrapTarget.WrappedTarget` 前提（183・204 行）はそれ以前から。非同期ラッパーが無いと `FindTargetByName<WrapperTargetBase>` は null を返し、`NullReferenceException` になる。
  - 影響は例外だけでなく、`SettingsViewModel.LogMaxArchiveFiles`／`LogMinLevel` のセッター（676〜706 行）で、例外が `_appSettings.Save()` より前に起きるため、**設定が保存されず（ログレベルは設定タブの値が保存されない）、NLog.config の更新（アーカイブ数）も行われず、変更通知も飛ばない**。例外は WPF のバインディングが握りつぶす。
  - なお起動時の初期化（47〜55 行付近）は、直接の `FileTarget` にも対応済み。同じ解決方法をこの 2 メソッドへ適用すれば足りる。
  - 実機確認前に、現状の症状を確認するとよい（設定タブでレベルを変えて再起動すると元に戻る、など）。

### F05（Medium）`DefaultMacroPlayer` が 4 スロット固定
- 指摘: `bool[4]` で、デバイス 4〜7 のマクロが何もしない（8 スロット対応のはず）。
- 簡易確認: `DefaultMacroPlayer.cs:16` が `new bool[4]`。使用者は `MacroAction`／`MacroActionAdapter`（系統A の経路）。稼働しているかは未確認。
- 配分: Step9b。スロット数は `IEnvironmentService.ControllerSlotLimit`（または `Global.MAX_DS4_CONTROLLER_COUNT`）に合わせる。
- **【Step9b-0 検証結果】分類A（稼働経路。ただし 8 スロット拡張時のみ実害）**:
  - 稼働経路: `Mapping`（Macro 型）→ `DispatchInputEdge` → `DefaultActionFactory` → `MacroActionAdapter` → `MacroAction` → `DefaultMacroPlayer`。`Play`／`Stop`／`IsPlaying` は `deviceIndex >= 4` で即 return するため、拡張スロット数が 8 のとき、デバイス 4〜7 のスペシャルアクションのマクロは再生されない（`handled` がどう返るかによっては、フォールバックの `PlayMacro` も呼ばれない可能性がある。要確認）。既定の 4 スロットでは影響なし。
  - 同じ「4」前提が `ProfileApplicationService`（`deviceIndex >= 4` を拒否）にもあり、これは既知の K1（`Phase6-Status.md` §6.5）。同じ性質のため、まとめて直す案を提示する。
  - 提案: 配列長と範囲判定を `Global.MAX_DS4_CONTROLLER_COUNT`（const、ホットパスではない）にする。

### F06（Medium）`LaunchProcessAction` がすべての起動を非表示にする
- 指摘: 通常の GUI 実行ファイルも `UseShellExecute=false`、`hidden=true` で起動される。非表示は `.bat`／`.cmd` のラッパーだけにすべき。
- 簡易確認: `LaunchProcessAction.cs:38` が `_launcher.Launch(targetPath, arguments, false, true)`。使用者は `LaunchProcessActionAdapter`（`ActionFactory`／`DefaultActionFactory`）と `Mapping.cs:5047` のフォールバック。
- 配分: Step9b。置換前の `Mapping.cs` の起動方法（`git show` で確認）と比べて、同じ動作に戻す。
- **【Step9b-0 検証結果】分類A（稼働経路で実害。置換前との比較で回帰を確認）**:
  - 置換前（`79170f73` の直前の `Mapping.cs`）の仕様: 通常は `UseShellExecute = true` で `FileName = details`、`Arguments = extra`。`extra` に `$hidden`（大文字小文字不問）が含まれるときだけ、その語を 1 回取り除き、`WindowStyle = Hidden`、`CreateNoWindow = true`、`UseShellExecute = true` で起動。`.bat`／`.cmd` は `$hidden` のときだけ `%COMSPEC% /C "details" 引数` で起動。
  - 現行の `LaunchProcessAction`: `$hidden` を見ない（`extra` が引数にそのまま渡り、`$hidden` という文字列が引数に混じる）。常に `UseShellExecute=false`、`hidden=true`。`.bat`／`.cmd` は常に `cmd.exe /c`。
  - 実害: (1) `$hidden` を付けない通常起動も非表示扱いになる（`CreateNoWindow` はコンソールのみに効く）、(2) `UseShellExecute=false` のため、ショートカット（.lnk）・ドキュメント・URL・関連付けによる起動ができない、(3) `$hidden` の語が引数に残る、(4) .bat の `/C` の大文字小文字の違い。
  - 提案: 置換前の仕様に戻す（`$hidden` の解釈と除去、`UseShellExecute=true`、非表示は `$hidden` のときだけ）。`MockProcessLauncher`（`LastHidden` など）を使う既存の `LaunchProcessActionTests` も、現行仕様に固定している可能性があるため、修正が必要。

### F07（Medium）`ServiceProviderHolder.GetRequiredService` が null を返しうる
- 指摘: 名前に反し、`GetService` を呼んで null を `T` として返す。失敗が無関係な呼び出し先へ移る。
- 簡易確認: `ServiceProviderHolder.cs:17`。`ActionFactory`／`ActionManager` が `ServiceProviderHolder.Provider` を直接使う（多数）。
- 配分: Step9b。例外を投げるようにすると、null を許容していた呼び出し側が影響を受けるため、呼び出し側の確認が前提。
- **【Step9b-0 検証結果】分類B（呼び出し元 0 件。実害なし）**:
  - `ServiceProviderHolder.GetRequiredService<T>` はソリューション全体（テストを含む）で呼び出し元が 0 件（`f7547140` の初期実装から）。`ActionFactory`／`ActionManager` は `Provider` を直接使い、null を許容している（22 箇所）。したがって、例外を投げる実装に変えても影響する呼び出し側はない。
  - §2.4 の判断: 「使うべきもの」（全体計画書 §5.4 のシム例が使用を想定した契約）のため、削除はしない。名前どおりの動作（未登録なら `InvalidOperationException`）に直すのが安全（決定3）。

### F08（Medium）`ProfileActionChainService` が自分で依存を解決し、フォールバックで `new`
- 指摘: `_actionDispatcher = actionDispatcher ?? AppHost.GetService<IMappingActionDispatcher>() ?? new MappingActionDispatcher()`。Pure DI 違反。
- 簡易確認: `ProfileActionChainService.cs:15`。同じパターンが `ProfileApplicationService`（`_deviceState ?? AppHost.GetService…` など）にもある。
- 配分: **Step10**（サービス実装のコンストラクタのフォールバック解消）。Step10-0 の台帳に、同じパターンのサービス実装を洗い出して載せる。

### F09（Medium）`ControllersViewModel` のコンストラクタの依存が任意引数＋ Service Locator
- 簡易確認: `ControllersViewModel.cs:18〜23`。
- 配分: **Step10**（ViewModel の Pure DI 化。決定1＝S9-A と同じ方針）。

### F10（Medium）サブ設定が null だとテストが `return` で通ってしまう
- 簡易確認: `ProfileSettingsServiceSubSettingsTests.cs` の複数箇所（41、66、94 行など）に `if (service.LSModInfo == null …) return;`。
- 配分: Step9b。null なら `Assert.NotNull` で失敗させる。ただし、環境未初期化で null になりうるテストがあるため、初期化を明示してから検査する。
- **【Step9b-0 検証結果】指摘は事実（テスト品質の問題。本番コードへの影響なし）**: `ProfileSettingsServiceSubSettingsTests.cs` の 17・42・67・94・118〜120・144 行の 6 テストが、`SafeConfig` が null（`LSModInfo` などが `SafeConfig?.…` のため環境未初期化で null）なら `return` で成功扱いになる。テストの前提（`BackingStore` の初期化）を明示してから `Assert.NotNull` にする。修正時に、現状で実際に null になっているテストが無いかを確認する（null なら、今は検証されていない）。

### F11（Medium）シングルトンのイベント購読のテストが、リフレクションでは検証になっていない
- 簡易確認: `SpecialActionsListViewModelTests.cs:28`（`GetFields` でデリゲート型のフィールドを探す）。購読は発行側（サービス）に保持されるため、検出できない。
- 配分: Step9b。偽のシングルトンのイベントで、購読と解除（`Dispose`）を動作で検証する。
- **【Step9b-0 検証結果】指摘は事実（テスト品質の問題）**: `SpecialActionsListViewModel_Audit_NoSingletonExternalEventSubscriptions` は、ViewModel 自身のフィールドの型を調べるだけで、サービス側のイベントに購読されているかを検出できない。コンストラクタに偽のサービス（イベントを発火できるもの）を渡し、購読後に発火して ViewModel が反応すること、`Dispose` 後は反応しないことを動作で検証する。コンストラクタが `ISpecialActionRepository`／`IProfileRepository`／`IOutputSlotService` を受け取るため、テスト用の実装を用意できるかを実装時に確認する。

### F12（Low）`NotificationEventArgs` と `INotificationService` が 1 ファイルに同居
- 簡易確認: `DI/INotificationService.cs`。
- 配分: **Step10b**（台帳に載っている。`Phase6-Step10b-Plan.md` 付録 B 付近の `INotificationService.cs`）。

### F13（Low）`Trigger.cs` に 4 つの型が同居（ファイル名がどの型名とも不一致）
- 簡易確認: `DS4Control/Trigger.cs`（`TriggerContext`、`TriggerType`、`ITrigger`、`SingleButtonTrigger`）。
- 配分: **Step10b**（台帳に載っている）。

### F14（Low）`StandaloneTests` の正規化テストが、同じ関数同士を比較していて常に通る
- 簡易確認: `StandaloneTests/TestSpecialActionUtils.cs:30〜35` が `Util.NormalizeActionName` 同士を比較。
- 配分: Step9b。比較先を `Global.NormalizeActionName`（`ScpUtil.cs` のシム）にするか、テストを削除・改名する。
- **【Step9b-0 検証結果】指摘は事実（テスト品質の問題）**: `ScpUtilAndUtil_NormalizeConsistent` は `Util.NormalizeActionName` 同士を比較している（常に通る）。`Global.NormalizeActionName`（`ScpUtil.cs:3492`）は存在するため、比較先を `Global` にすれば意図どおりのテストになる。`StandaloneTests` が `Global` を参照できるか（ビルド構成）を実装時に確認する。

---

## 2A. 決定の記録（2026-10-14、ユーザー回答）
- **決定1＝A**: 分類B の `MouseOutputAction`（F03）は TODO コメントのみ（Phase8 へ）。動作は変えない。
- **決定2＝A**: F02 は `IManagedActionManager`→`DefaultActionManager`（と `IControllerRegistry`→`DefaultControllerRegistry`）を再登録する。ファサードは変更しない。回帰テストと実機（Toggle 型キー）で確認する。
- **決定3＝A**: F07 は `GetRequiredService` を、未登録なら `InvalidOperationException` を投げる実装にする（削除しない。§2.4）。
- **決定4＝A**: F06 は置換前の仕様（`$hidden` の解釈と除去、`UseShellExecute=true`、非表示は `$hidden` のときだけ）へ戻す。既存の `LaunchProcessActionTests` も合わせて修正する。
- **決定5＝B**: PR #1 のレビューコメントへの返信・解決済みは、内容をユーザーへ提示し、許可を得たものだけ私が書き込む。実施は **Step9b がすべて終わったときにまとめて**行う。
- **決定6＝A**: `copilot-instructions.md` §2.2 の「250ms デバウンス」の記述を現行の機構（80ms の未成立継続で解除する Fresh-press ガード＋実行中の再実行防止）へ更新する。Step9b-5 で実施。
- **順序（確定）**: 9b-1 F04 → 9b-2 F02＋F07 → 9b-3 F06、F05、F03 の TODO → 9b-4 F10、F11、F14 → 9b-5 文書・完了報告・PR 返信。F01 は分類C のためコード変更なし。

## 2. 決定が必要な事項（Step9b-0 で、検証結果とともにユーザーへ提示する。**回答済み。§2A 参照**）
- **決定1**: 分類B（未配線で実害なし）の扱い。案: 稼働経路でなければ TODO コメント（§3.3 原則4）を付けて Phase8 へ送る（推奨）／本 Step で直す。
- **決定2**: F02 の方針。案: `DefaultActionManager` を登録する／`ActionManager` ファサード側の利用をやめる。
- **決定3**: F07 の方針。案: 例外を投げる実装に変え、呼び出し側を確認・修正する／名前を `GetService` 相当に変える。
- **決定4**: PR #1 のレビューコメントへの返信・解決済みマーク（GitHub への書き込み）は、ユーザーの明示的な許可を得て行うか、ユーザー自身が行うか。

## 2B. 実施記録
### Step9b-1（F04）【完了、2026-10-14】
- `LoggerHolder.FindLogFileTarget`（ラッパー内と直接の `FileTarget` の両方に対応）を追加し、`UpdateLogSettings`／`RestoreArchiveSetting`／コンストラクタが共用。未検出時は例外ではなく警告ログ。
- 回帰テスト `LoggerHolderFindLogFileTargetTests`（4 件）。ビルド・テストビルド・テスト実行成功、コミット・反映済み。実機確認（ログ設定の保存・反映・`Log settings updated:` のログ）問題なし。

### Step9b-2（F02・F07）【ビルド・テスト成功・コミット反映済み。実機確認: Toggle の A・B は問題なし（2026-10-14、ログ確認済み）。項目 2・3 は未確認】
- **実機ログの確認結果（2026-10-14 15:43〜15:44、Trace）**: (A) Controls タブ: `Synthetic_Key_0_18`（合成アクション）が `DispatchTriggerEdge` → `DefaultActionManager.DispatchTriggerEstablished` → `KeyAction.OnTrigger` → `ToggleImpl.OnDown` → `ActionManager.SetToggledOn` と進み、`ToggledOnChanged old=False new=True` → 解放で維持 → 再押下で `old=True new=False`（`RepeatHelper.Stop`、`KeyRelease` 送出）。2 サイクルとも正常。(B) SpecialActions タブ: `000000name1`（Key 型、Toggle）も同じ遷移を 2 サイクル確認。例外・ERROR・WARN なし。プロファイル適用時は `DefaultActionManager.ClearDeviceState` と `KeyButtonActionController destroyed` が出ており、`HandleDeviceDisconnect` 経由の破棄が動作。
- **観察（問題なし・対応不要）**: (1) A でトグルオフ時に `KeyRelease` が 2 回送出される（`ToggledOnChanged` の購読側と `ToggleImpl.OnToggleOff` 自身の送出）。押していないキーの解放の重複で、無害。設計上の構造。(2) B（SpecialActions の Key 型）の最初のエッジは `logical=0`（`kvpKey=0`）で、出力は `nativeKey=65` の正しいキー。`DispatchOrSetBeingTriggered` が `logicalValue` なしで先に呼ばれるため。今回の変更とは無関係の既存の挙動。
- **項目 2・3 の実機確認（2026-10-14 15:49〜15:50、ログ確認済み）: 問題なし**。(2) トグルオン中（`Synthetic_Key_0_18`、15:49:52 に `old=False new=True`、約 33ms 間隔のリピート）に、スペシャルアクション（Profile 型）でプロファイルを切り替えると、`ApplyProfile` の直後（15:49:53.4861）に `KeyRelease key=74` が送出され、`RepeatHelper.Dispose` と `KeyButtonActionController destroyed`（`Synthetic_Key_0_18` を含む）が続き、以後 `KeyPress key=74` は出ていない。リピートの停止とキー解放を確認。(3) 起動・プロファイル適用・コントローラーのスペシャルアクションによる切断（`WARN … disconnected due to read failure: 1167`＝デバイス未接続。意図した切断で想定どおり）・仮想コントローラーの取り外し・全コントローラーの破棄・アプリの終了（`Request App Shutdown` → 停止 → 設定の保存成功）まで、例外・ERROR・ハングなし。
- **F02**: `ServiceRegistration` に `IManagedActionManager`→`DefaultActionManager`、`IControllerRegistry`→`DefaultControllerRegistry` を再登録（`IActionRegistry` は実装が静的クラスで呼び出し元もないため対象外）。
- **再登録で挙動が変わる点の是正（実装前の突き合わせで判明）**: 2026-09-04 以降は静的な `ActionManager` のフォールバックで実機確認が行われていた。その `ClearDeviceState` は `Mapping.HandleDeviceDisconnect`（保持中の合成キーの解放、カスタムマッピング／マクロキューのクリア、コントローラー破棄）を呼ぶが、`DefaultActionManager.ClearDeviceState` は `ClearKeyButtonControllersForDevice` のみだった。再登録でプロファイル適用時に押しっぱなしの合成キーが解放されなくなる回帰を避けるため、`DefaultActionManager.ClearDeviceState` も `HandleDeviceDisconnect` を呼ぶよう揃えた（`ClearKeyButtonControllersForDevice` は `HandleDeviceDisconnect` に内包）。
- **F07**: `ServiceProviderHolder.GetRequiredService<T>` を、プロバイダー未設定・未登録のとき `InvalidOperationException` を投げる実装に変更（呼び出し元 0 件）。
- **テスト**: `ManagedActionManagerRegistrationTests`（6 件。登録とシングルトン、`SetToggledOn` が状態を変える、Controls タブ経由［合成 SpecialAction］の Toggle が `ActionManager.DispatchTriggerEdge` で「押下でオン、解放で維持、再押下でオフ」と遷移する、`GetRequiredService` の 3 ケース）。モデル図 04 の §2 に 2 行を追記。
- **Controls タブと SpecialActions タブの経路**: Controls タブの Key 割り当ては合成 SpecialAction（`Synthetic_Key_…`）に変換され、SpecialActions タブと同じ `ActionManager.DispatchTriggerEdge` → `KeyAction` → `KeyButtonActionController`（Toggle）→ `ToggleImpl` を通るため、どちらも本修正の対象。Controls タブでマウスボタンに割り当てた Toggle（`currentClicks.toggle`）と Macro の Toggle は別の状態管理で、対象外（変更なし）。
- **テスト実行での失敗（既存テストの実行順依存）**: `ProfileSettingsViewModelGlobalReferenceGuardTests.ViewModelFactory_ResolvedFromContainer_CreatesViewModelWithEnvironmentService` が `UriFormatException: Invalid port specified`（`ProfileSettingsViewModel` コンストラクタの `pack://application:,,,/…` の `new Uri`）で失敗。`pack` スキームは `System.Windows.Application` の生成で登録されるが、このクラスは `Application` を生成する準備を持たず、ほかのテストクラスが先に生成したかどうか（実行順）に依存していた。今回の変更（再登録・`GetRequiredService`）とは無関係で、新しいテストクラスの追加で実行順が変わって顕在化したと推定される。他の ViewModel テストと同じ準備（`Application` の生成と `ResourceAssembly`）をこのクラスの静的コンストラクタに追加して修正。
- **実機確認（案）**: (1) Toggle 型のキー割り当て（コントロールタブの Toggle、またはスペシャルアクションの Key 型トグル）で、1 回目の押下でオン（リピート）、2 回目の押下でオフになること。Trace ログ（設定タブのログ設定で Trace に変更 → 再起動、`ds4windows_log.txt`）で `ToggledOnChanged` の `old=False new=True`／`old=True new=False` が出ること。確認後は元のログレベルへ戻す。(2) プロファイル切替（スペシャルアクション）で、押しっぱなしのキー出力が切替時に解放されること。(3) 起動・終了（コントローラー接続／切断を含む）で例外やハングがないこと、終了時の破棄で問題がないこと。

### Step9b-3（F06・F05・F03）【実装済み・ユーザーのビルド・テスト・実機確認待ち】
- **F06**: `LaunchProcessAction` を置換前の仕様へ戻した。`extra` に `$hidden`（大文字小文字不問）があるときだけ、その語を 1 回取り除き `hidden=true` で起動（`.bat`／`.cmd` は `%COMSPEC% /C "<details>" <引数>`）。それ以外は `UseShellExecute=true`、非表示にせず `extra` をそのまま引数に渡す。`LaunchProcessActionTests` を、新仕様に合わせて書き直した（旧 T4 は `$hidden` なしの `.bat` が `cmd.exe` になる現行仕様を固定していた。5 ケース＋T6）。
- **F05**: `DefaultMacroPlayer` の配列長と範囲判定を `Global.MAX_DS4_CONTROLLER_COUNT`（const、8）にし、`Mapping.PlayMacroDirect`／`EndMacroDirect` の `>= 4` も揃えた（`macrocontrol` 引数は内部で静的 `macroControl` を使うため、`new bool[4]` はダミーで影響なし）。テスト `DefaultMacroPlayer_HandlesAllControllerSlots_NotJustFour`。既定の 4 スロットでは挙動に変化なし。
- **F03**: 分類B のため動作は変えず、`MouseOutputAction` クラスと `Mapping.cs` の 2 箇所に TODO（登録してはならない理由、Phase8 で完成）を付与。`Phase8-Unified-Trigger-Dispatch-Plan.md` 末尾へ、F01〜F03・F06 の申し送りを追記。
- **実機確認（案）**: (1) スペシャルアクション「プログラム起動」で、(a) 引数なしの exe、(b) `$hidden` なしの引数付き exe（引数が渡る）、(c) `$hidden` 付きの `.bat`（ウィンドウが出ない）、(d) ショートカット（.lnk）やドキュメントの起動、を確認する。Trace ログに `LaunchProcessAction.Execute: … hidden=…` が出る。(2) 8 スロット拡張（設定タブの 8 台対応）にしている場合のみ、デバイス 5 以降（インデックス 4〜7）でスペシャルアクションのマクロが再生されること（実機が 5 台目を持てなければ、Step11 の先送り台帳へ登録）。

### Step9b-3 の実機確認結果（2026-10-14）
- プログラム起動（引数なし・引数付き・`$hidden`・ショートカット等）は問題なし。8 スロット拡張時のデバイス 4〜7 のマクロは、実機（5 台目以降）がないため先送り（`Phase6-Step11-Plan.md` §3.3 に登録、単体テストで固定済み）。あわせて、実機確認中の指示で「Launch Program」タブのプログラム名ラベルの固定幅 150 を解消した（`SpecialActionEditor.xaml`。親の幅いっぱいを使い、アイコンは右端、長い名前は末尾を省略）。

### Step9b-4（F10・F11・F14）【実装済み・ユーザーのビルド・テスト待ち。本番コードの変更なし】
- **F10**: `ProfileSettingsServiceSubSettingsTests` の 6 テストで、前提が満たされないと `return` で成功扱いになっていた条件分岐を `Assert.NotNull` に置き換えた。`BackingStore` の該当配列（`lsModInfo` など）はフィールド初期化子で常に生成されるため、通常は通る（null なら検証されていなかったことが、失敗として表に出る）。
- **F11**: `SpecialActionsListViewModelTests` の監査テストを、動作による検証に置き換えた。購読は発行側（サービス）のデリゲートに保持されるため、購読者のフィールドの型を見る旧テストでは検出できなかった。発行側のバッキングフィールドの購読者（`Delegate.Target`）を調べる `CountSubscriptions` を作り、(1) 偽の発行側・購読者で検出器自身を検証、(2) 実際の Singleton（`ISpecialActionRepository`、`IProfileRepository`、`IOutputSlotService`、`IProfileSettingsService`、`IManagedActionManager`、`ActionManager`・`Global` の静的イベント）に ViewModel が購読していないことを確認する。なお当該 ViewModel は `IDisposable` ではなく、サービスのイベントを購読していない（自身の `actionCol.CollectionChanged` と各項目の `PropertyChanged` のみ）。そのため、計画書 §1 F11 の「購読と解除（`Dispose`）を動作で検証する」は、「購読していないことの検証」に置き換えた。
- **F14**: `StandaloneTests.ScpUtilAndUtil_NormalizeConsistent` を、`Util.NormalizeActionName` と `Global.NormalizeActionName`（独立した 2 つの実装）の比較（複数の入力）と、期待値の固定に書き直した。

## 3. マイクロステップ（案。Step9b-0 の結果と決定で確定）
- **Step9b-0（実装なし）**: F01〜F07、F10、F11、F14 の検証。稼働経路か（分類A／B／C）、置換前の実装との比較（`git log -S`、`git show`）、決定の提示。結果を本書 §1 に追記する。
- **Step9b-1**: F04（ログ設定の `NullReferenceException`）。テスト＋実機確認。独立していて確実なため先に行う。
- **Step9b-2**: F01（デバウンス）と F07（`GetRequiredService`）。F01 は §2.2 の安全弁のため、回帰テスト（連続発火の遮断）を必須とする。
- **Step9b-3**: F02、F03、F05、F06（Actions 系。分類A のもの）。分類B のものは TODO コメントのみ。
- **Step9b-4**: テストの品質（F10、F11、F14）。
- **Step9b-5**: 文書更新（Status、Plan、Phase8 計画書、Step10／Step10b への申し送り）、完了報告書 `Phase6-Step9b-Completion-Report.md`。

## 4. リスクと対策
- **自動レビューの誤検出**: 検証で分類C にする。理由を記録し、不要な変更をしない。
- **未配線のコードを直して、稼働経路に影響する**: 分類B は変更せず、TODO と Phase8 への申し送りのみ。
- **`GetRequiredService` の変更による新たな例外**: 呼び出し側（`ActionFactory`、`ActionManager`）を網羅して確認してから変更する。
- **ログ設定の変更**: 実機確認が必須（§2.3。ログ出力とレベルの維持）。

## 5. 完了判定チェックリスト（案）
- [x] F01〜F07、F10、F11、F14 の各件が、検証の結果（A／B／C）と対応が記録されている
- [x] 分類A の指摘が修正され、回帰テストで固定されている
- [x] 分類B の指摘に TODO コメントがあり、Phase8 計画書へ申し送られている
- [x] F08、F09 が Step10 の台帳に、F12、F13 が Step10b の台帳に載っている
- [x] `dotnet build`／`dotnet test` が全件成功。実機確認（F01、F04 ほか）で問題なし
- [x] Status／Plan／完了報告書を更新
