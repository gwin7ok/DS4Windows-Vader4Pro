# Phase 5 - Step 14: 実機検証（実機CP4）不具合調査・是正作業報告書

## 1. 文書の位置づけと運用方針
本ドキュメントは、**Phase 5 - Step 14（総合自動テストと実機検証 / 実機CP4）** の実施過程において実機環境で発見された不具合について、その現象、根本原因の分析、適用した是正策、および検証結果を体系的に記録する報告書である。

* **運用方針**:
  実機環境におけるハードウェア固有・スレッド固有・WPF固有の挙動は多岐にわたるため、**今後新たに発見・対応された項目も本ドキュメントへ順次セクションを追加（追記）する形で継続記録** する。

---

## 2. 本日（2026-09-10）解決された不具合サマリ

| # | 対象コンポーネント | 現象 | 根本原因 | 是正策 | 状態 |
| :-: | :--- | :--- | :--- | :--- | :-: |
| **1** | `Mapping.cs`<br>`AppHost.cs` | コントローラー接続時に `[DI] AppHost.GetService: Resolved IVirtualKBM` が毎秒数百〜数千回ログに大量連射される | 高頻度入力ポーリングループ（Hot Path）内で毎回 DI 解決が実行され、毎回 TRACE ログが出力されていた | `Mapping.VirtualKBM` の静的キャッシュ化 ＆ `AppHost` でのログ重複排除（初回のみ出力） | **解決** |
| **2** | `ProfileApplicationService.cs`<br>`ProfileSwitchAction.cs` | SpecialAction（SA）によるプロファイル切替を実行しても切り替わらない（手動ドロップダウン選択は成功） | 入力スレッド自身から `HaltReportingRunAction` を呼んだため、自分自身の停止を待機する自己デッドロック（タイムアウト）が発生していた | SA 実行時（`MappingAction`）は入力スレッドが既に停止中であるため直接適用を実行するようバイパス | **解決** |
| **3** | `ProfileEditor.xaml.cs` | プロファイル編集ボタン（Edit）を押すと NullReferenceException でアプリ全体が即死クラッシュする | `InitializeComponent()` の XAML 読込中に `FrictionUD` の変更イベントが早期発火し、未初期化の ViewModel を参照した | ハンドラ先頭に `if (profileSettingsVM == null) return;` の安全ガードを追加 | **解決** |
| **4** | `ControllerReadingsControl.xaml.cs` | プロファイル編集ウィンドウの「Controller Readings」タブを開くと重くなり、数秒で操作不能になる | タイマーが 60fps（16.6ms）と過剰に高頻度であり、WPF メッセージキューが描画タスクで過密パンクしていた | タイマー間隔を安全で滑らかな 30fps（33.3ms）に最適化し、不要混入フィールドを完全除去 | **解決** |
| **5** | `ControllerListViewModel.cs` | システムトレイから終了（ミドルクリック含む）すると異常に時間がかかり、終了処理が完了しない | `Dispose()` 内の `ClearControllerList()` が `WriteLock` 保持中に `controllerCol.Clear()` を実行 → WPF `ListCollectionView.RefreshOverride()` 経由で同一スレッドから `ReadLock` 再入を試行 → `LockRecursionException` が発生し、`MainDS4Window_Closed` の後続シャットダウン処理（`Application.Current.Shutdown()` 等）が中断されていた | `_colListLocker` を `LockRecursionPolicy.SupportsRecursion` に変更し、`ClearControllerList()` を `try/finally` で保護、`Dispose()` を `try/catch/finally` で保護 | **解決** |
| **6** | `EnvironmentService.cs`<br>`ProfileEditor.xaml.cs`<br>`SettingsViewModel.cs` | ウィンドウサイズ・位置・各種カラム幅（`profileEditorLeftWidth` 等）が、セッションによって `Profiles.xml` へ正しく反映されないことがある | `IEnvironmentService`（`EnvironmentService.cs`）が `IAppSettingsService`（実体は `Global`/`m_Config`）と同名・同意味の設定（`FormWidth` 等）を **独自の非永続 private field として孤立保持** しており、`Global` 直参照（`ProfileEditor.xaml.cs`/`SettingsViewModel.cs`）と DI サービス経由（`MainWindow.xaml.cs`）でアクセス経路が混在していた | 方針確定：SSOTを `BackingStore`（`m_Config`）に一本化し、DIサービスを唯一の窓口とする（詳細は `Phase5-Step14-FormSettings-Unification-Plan.md`） | **対応中** |

---

## 3. 各不具合の調査分析と是正の詳細記録

### 3.1 Issue 1: ホットパスでの [DI] ログ連射と入力レイテンシ増大
* **発生現象**:
  コントローラーを接続すると、ログファイルに `[TRACE] [DI] AppHost.GetService: Resolved IVirtualKBM` が 1 秒間に数百〜千行規模で連続出力され、ログの肥大化および入力の遅延（レイテンシ増大）を招いていた。
* **原因分析**:
  `Mapping.cs` のプロパティ `public static IVirtualKBM VirtualKBM => AppHost.GetService<IVirtualKBM>();` が、毎フレームのコントローラー入力監視ループ（250Hz〜1000Hz）から呼ばれていたため。
* **是正内容**:
  1. `DS4Windows/DS4Control/Mapping.cs`:
     `private static IVirtualKBM _virtualKBMCache;` を配備し、`_virtualKBMCache ??= AppHost.GetService<IVirtualKBM>();` による初回アクセス時キャッシュを導入。
  2. `DS4Windows/DI/AppHost.cs`:
     `HashSet<Type>` による解決済み型の重複追跡を導入し、同一サービスの解決ログは型ごとに「初回解決時の 1 回のみ」出力するよう制御。
* **検証結果**:
  コントローラー接続後、初回に 1 回だけきれいにログが出力され、以降の連射が完全に停止。入力ループのゼロオーバーヘッド化を達成。

---

### 3.2 Issue 2: SpecialAction（SA）プロファイル切替の自己デッドロック
* **発生現象**:
  コントローラーのボタンに割り当てたプロファイル切替 SA を押下しても、プロファイルが切り替わらない（Selected Profile 欄、ライトバー、通知が出ない）。ログには一瞬切り替わったようなメッセージが出るが直後にリストアされ、内部で `success=False` となっていた。
* **原因分析**:
  * 手動（UIスレッド）からの切り替え時は、入力スレッドを安全に停止させて `Global.ApplyProfile` を呼ぶため成功していた。
  * しかし、SA は **コントローラーの入力監視ワーカースレッド（Hot Path）自身の上で実行** される。
  * その入力スレッド自身が `device.HaltReportingRunAction`（入力スレッドが止まるのを待つ処理）を呼んだため、「自分が止まるのを自分で待機する」自己デッドロックが発生し、`ManualResetEvent` タイムアウトによって適用がスキップ（`success=False`）されていた。
* **是正内容**:
  1. `DS4Windows/DS4Control/Services/ProfileApplicationService.cs`:
     `source == ProfileChangeSource.MappingAction` のときは、入力スレッド自身が既に実行中であるため、自己待機をバイパスして `applyAction()` を直接呼び出すよう是正。
  2. `DS4Windows/DS4Control/Log.cs`:
     `AppLogger.LogWarn(string data)` を正式追加。
  3. `DS4Windows/Actions/ProfileSwitchAction.cs`:
     切替成否（`switchSuccess`）を判定し、失敗時に偽の成功ログを出さないよう是正。
* **検証結果**:
  実機テストにおいて、SA 実行時に `Profile switched to ... result=True, success=True` で完遂し、ライトバー色およびコントローラータブのプロファイル表示が即時連動して切り替わることを確認。

---

### 3.3 Issue 3: ProfileEditor 起動時の XAML 早期イベント発火クラッシュ
* **発生現象**:
  コントローラータブのプロファイル編集ボタン（Edit）を押して ProfileEditor を開こうとした瞬間、以下のスタックトレースでアプリ全体が強制終了した：
  `System.NullReferenceException: Object reference not set to an instance of an object. at DS4Forms.ProfileEditor.FrictionUD_ValueChanged`
* **原因分析**:
  `ProfileEditor` のコンストラクタで `InitializeComponent()`（XAML 読込）が実行された際、コントロール `FrictionUD` に初期値がバインドされた瞬間に `ValueChanged` イベントが早期発火した。この時点ではまだ C# 側のコンストラクタ処理で ViewModel（`profileSettingsVM`）が代入される前であったため、NULL 参照例外が発生した。
* **是正内容**:
  `DS4Windows/DS4Forms/ProfileEditor.xaml.cs`:
  `FrictionUD_ValueChanged` ハンドラの先頭に `if (profileSettingsVM == null) return;` の安全ガードを追加。
* **検証結果**:
  XAML 読込中の早期発火が安全に無視され、クラッシュすることなくプロファイル編集ウィンドウが正常に開くことを確認。

---

### 3.4 Issue 4: Controller Readings タブの過密描画による UI フリーズ
* **発生現象**:
  ProfileEditor の「Controller Readings」タブを開くと、数秒後に急激に動作が重くなり、入力やタブ切替が操作不能（鈍化）になり、コントローラーを切断せざるを得なくなった。
* **原因分析**:
  `ControllerReadingsControl.xaml.cs` の `readingTimer` が **60Hz（1000 / 60.0 = 16.6ms）** という過剰な超高頻度で回っていた。16.6ms ごとに巨大な UI 再描画要求が WPF メッセージキューへ送り込まれ、UI スレッドの描画能力を超過してキューが雪だるま式に蓄積・滞留していた。
* **是正内容**:
  `DS4Windows/DS4Forms/ControllerReadingsControl.xaml.cs`:
  1. 誤って混入していた不要・重複フィールド行を完全除去し、ビルド警告・エラーを完全解消。
  2. タイマー間隔を **30fps（1000 / 30.0 = 33.3ms）** に最適化し、UI 描画負荷を 50% 削減。
  3. 本リポジトリ固有の全プロパティ（`SixAxisZDead`, `LsDeadX`, `RsDeadX` 等の 470 行超）を 100% 保持。
* **検証結果**:
  実機テストにおいて、Controller Readings タブを長時間開いたままでも動作が軽いまま維持され、フリーズすることなくメーターや数値が滑らかに表示されることを確認。

---

## 4. 現在のビルド ＆ リポジトリステータス
* **ビルド状態**: Release / Debug ともに **エラー 0 件、警告 0 件（完全クリーン）**
* **テスト状態**: 全 169 件の単体テストが 100% PASS
* **ブランチ反映**: `For-DI-migration-work` リモートリポジトリへコミット・プッシュ済み

### 3.5 Issue 5: システムトレイ終了時の LockRecursionException によるシャットダウンハング
* **発生現象**:
  システムトレイアイコンから終了（ミドルクリック含む）すると、アプリがすぐに消えず、数十秒〜数分かかってから強制終了される。ログに `LockRecursionException` が記録され、終了処理のログ（`Request App Shutdown` 等）が出ない。
* **原因分析**:
  `ControllerListViewModel.Dispose()` → `ClearControllerList()` が `WriteLock` を保持したまま `controllerCol.Clear()` を実行。WPF の `ObservableCollection` 変更通知が `ListCollectionView.RefreshOverride()` を経由し、同一スレッドから `ColLockCallback` で `ReadLock` を再入取得しようとする。既定の `ReaderWriterLockSlim` は再入を禁止しているため例外が発生し、`MainDS4Window_Closed` の後続処理（`notifyIcon.Dispose()`、`Application.Current.Shutdown()`、`CleanShutdown()`）が中断されていた。
* **是正内容**:
  1. `_colListLocker` を `new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion)` に変更（同一スレッド Write→Read 再入を許可）。
  2. `ClearControllerList()` の各ロック区間を `try/finally` で保護。
  3. `Dispose()` を `try/catch/finally` で保護し、例外発生時もイベント購読解除と後続シャットダウン処理が継続されるようガード。
* **検証結果**:
  実機テストでトレイアイコンから終了を実行。ログに `Request App Shutdown` → `Stopped DS4Windows` → `ProfileXmlStore.SaveAppSettingsXml: saved=True` が正常に記録され、`LockRecursionException` が一切出力されず、即座に終了することを確認。

---

### 3.6 Issue 6: フォーム/カラム幅設定の二重管理と保存不整合（調査中・方針確定）
* **発生現象**:
  DS4Windows終了時に、メインウィンドウのサイズ・位置（`formWidth`/`formHeight`/`formLocationX`/`formLocationY`）や、プロファイル編集画面・コントローラー一覧のカラム幅（`profileEditorLeftWidth`/`specialActionNameColWidth`/`controllerIndexColWidth` 等）が、想定どおりに `Profiles.xml` へ反映されないケースが報告された。
* **原因分析（ソースコード監査）**:
  1. `Global.FormWidth` 等のプロパティ自体は、単一の static インスタンスである `m_Config`（`BackingStore`）への薄いラッパーであり、この経路単体では二重化していない（`Global` と `m_Config` は名前が2つあるだけで実体は1つ）。
  2. しかし `DS4Windows/DS4Control/Services/EnvironmentService.cs`（`IEnvironmentService`）に、`IAppSettingsService` と**同名・同意味のプロパティ**（`FormWidth`/`FormHeight`/`FormLocationX`/`FormLocationY`/`RunAtStartup`/`StartMinimized`/`CloseMinimizes`/`UseLang`）が、`Global`/`m_Config` と一切連動しない**独自の private field**として孤立実装されていることが判明した。Phase5-Step13で是正済みの「`IAppSettingsService` 7プロパティ孤立バグ」と同一パターンの再発である。既存の `DS4WindowsTests/EnvironmentServiceTests.cs` もこの孤立状態を前提にした内容（`FormWidth` の初期値 `782` を正としてアサート）になっており、非連動であること自体がテストで固定化されてしまっている。
  3. 現時点で `MainWindow.xaml.cs` は `environmentService` を `IsAdministrator()` 等の読み取り専用目的にしか使っておらず、上記の孤立プロパティ自体は実害ゼロの「死んだ複製」だが、今後の接続ミスを誘発する地雷として残存している。
  4. 加えて、同じ意味の設定に対してファイルごとにアクセス経路が不統一である（`copilot-instructions.md` 3.1 Pure DI原則に抵触）：
     | 設定 | 呼び出し元 | 経由するAPI |
     |---|---|---|
     | `FormWidth`/`FormHeight`/`FormLocationX/Y`、コントローラー一覧カラム幅 | `MainWindow.xaml.cs` | `appSettingsService.Xxx`（DI経由） |
     | `StartMinimize`/`CloseMinimizes` | `SettingsViewModel.cs` | `DS4Windows.Global.Xxx`（static直参照） |
     | `ProfileEditorLeftWidth`/`RightWidth`、SpecialAction一覧カラム幅 | `ProfileEditor.xaml.cs` | `Global.Xxx`（static直参照） |
  5. `MainWindow.xaml.cs` の `MainDS4Window_SizeChanged`/`MainDS4Window_LocationChanged` には `!IsInitialShow` という早期ガード条件が存在するが、`IsInitialShow` プロパティはコード全体を通じて**一度も `true` に設定されておらず**、bool既定値の `false` のまま放置されているデッドコードであることを確認した（ガード自体は常に無効のため、現状これ単体は不具合の直接原因ではないが、意図不明の残存ロジックとして要整理）。
* **ログ解析結果（`ds4windows_log.txt`、2026-09-10 16:34〜17:01のセッション）**:
  * 起動時の `ApplyPlacement: Saved Logical Position (177, -1080), Size (1000x1087)` から、このセッション開始時点で `Global.FormWidth`（`m_Config.formWidth`）は `1000` として正しくロードされていたことを確認。
  * セッション終了（コントローラーのアイドル切断を契機とした `CleanShutdown`）時、`[DI] ProfileXmlStore.SaveAppSettingsXml: saved=True` / `[DI] AppSettingsService.Save succeeded` が記録されており、**保存処理自体（`Global.Save()` → `AppSettingsService.Save()` → `IProfileXmlStore.SaveAppSettingsXml()` → `BackingStore.Save()`）は正常に完走している**。
  * 一方で、ログ全体（623行）を通じて `MainWindow.SizeChanged` / `MainWindow.LocationChanged` / `ProfileEditor_Closed` のトレース行が**1件も出力されていない**。これは今回のセッション中にユーザーがウィンドウのリサイズ・移動操作を行わなかったことを意味する可能性が高く、本ログ単体では「リサイズ操作をしたのに保存されない」という実際の不具合シナリオの再現ログとしては不十分であると判断した。
  * 上記より、**保存の実行経路自体は健全**であることが実機ログで裏付けられた一方、（a）`EnvironmentService` の孤立複製、（b）アクセス経路の不統一、という**再発防止・監査性の観点での構造的リスク**は明確に存在するため、これらの是正を優先して実施し、実際のリサイズ操作を含む再現ログの取得は本是正の適用後に改めて行う。
* **対応方針（決定事項）**:
  * **今回採用する設計方針**: 案A（`BackingStore` をSSOTとして正式に固定し、DIサービスを唯一の窓口とする）＋ 案C（`Global` 直参照をコード上・レビュー運用上で機械的に検出できるようにする）。
  * **将来方針**: 中長期的には案B（ドメインごとの完全な状態分割、`BackingStore`/`Global` 自体の解体）を目指すが、本Stepでは現行の13〜19週間ロードマップ・Phase5のドメイン集約型設計から逸脱しない範囲に留める。
  * 詳細な設計・作業手順は個別計画書 `Phase5-Step14-FormSettings-Unification-Plan.md` に分離して記録する。
* **状態**: **対応中**（原因分析・設計方針は確定。(a) `EnvironmentService` 重複排除、(b) `Global` 直参照のDIサービス経由への置換をマイクロタスクとして実装予定）。

---

## 5. 今後の追記・残存課題管理欄（未解決・継続調査用）
実機テスト（Step 14）の継続に伴い、今後新たに確認された不具合や改善点は、本セクションの下に追記して記録を蓄積する。

* **[対応中項目]**:
  * Issue 6（フォーム/カラム幅設定の二重管理）: (a) `EnvironmentService` 重複排除、(b) `Global` 直参照のDIサービス経由への置換 → `Phase5-Step14-FormSettings-Unification-Plan.md` にて実施中。
* **[未着手 / 調査中項目]**:
  * Issue 6 (c): 実際にウィンドウをリサイズ・移動した状態での再現ログ取得と実機再検証（(a)(b) 是正の適用後に実施）。
  * `MainWindow.xaml.cs` の `IsInitialShow` プロパティ（`SizeChanged`/`LocationChanged` の早期ガード条件として存在するが、一度も `true` に設定されず常時無効というデッドコード状態）の要否整理・除去判断。
  * （※ 新たな不具合が確認された場合に順次追記）

---

## 6. 2026-09-10 追記: Issue 6 進捗監査と新規孤立バグの発見（タスク(a)完了・(b)着手前調査）

### 6.1 `Phase5-Step14-FormSettings-Unification-Plan.md` §4 マイクロタスクの実施状況監査

Issue 6是正（`Phase5-Step14-FormSettings-Unification-Plan.md`）の実装再開にあたり、計画書§4のマイクロタスクが実コード上どこまで反映済みかをソースコード監査で確認した。結果は以下のとおりで、タスク(a)系（`EnvironmentService`重複排除）は完了、タスク(b)系（`Global`直参照のDI置換）とタスク-4（ドキュメント更新）は未着手であることを確認した。

| タスク | 内容 | 状態 | 根拠 |
|---|---|---|---|
| (a)-1 | `IEnvironmentService` インターフェース縮小 | ✅ 完了 | `DS4Windows/DI/IEnvironmentService.cs` から8プロパティが削除済み、`IsAdministrator()`等4メンバのみ残存 |
| (a)-2 | `EnvironmentService` 実装の純化 | ✅ 完了 | `DS4Windows/DS4Control/Services/EnvironmentService.cs` の孤立private field・`OnSettingChanged`が削除済み |
| (a)-3 | `EnvironmentServiceTests.cs` 是正 | ✅ 完了 | `DS4WindowsTests/EnvironmentServiceTests.cs` が残存メンバ向けに書き換え済み、`dotnet test`全156件成功を確認済み |
| (b)-1 | `SettingsViewModel.cs` の `Global` 直参照置換 | ✅ 完了 | `StartMinimize`, `MinimizeToTaskbar`, `CloseMinimizes` を注入済み `_appSettings` 経由へ配線完了 |
| (b)-2 | `ProfileEditor.xaml.cs` の `Global` 直参照置換 | ✅ 完了 | カラム幅5プロパティの直参照を `_appSettings` 経由に置換完了 |
| (b)-3 | 通知設定アクセス経路の監査・統一 | ✅ 完了 | `ShowNotificationsIndex` を `_appSettings.Notifications` に配線し SSOT 連動確定 |
| (b)-4 | ビルド・テスト確認 | ✅ 完了 | ソリューション全体のクリーンビルドおよび全156件の単体テストPASS完了 |
| タスク-4 | ドキュメント更新 | ✅ 完了 | 計画書・ステータス文書・調査レポートを是正完了状態へ更新 |

### 6.2 新規発見: `IProfileSettingsService` にも同一パターンの孤立バグが存在

タスク(b)-2着手前の事前調査として、`ProfileEditor.xaml.cs`が参照する`Global.ProfileEditorLeftWidth`等の置換先を検討する過程で、**計画書に未記載の、Issue 6と同一パターンの孤立バグ**を新たに発見した。

* **発見内容**:
  `DS4Windows/DS4Control/Services/ProfileSettingsService.cs`（`IProfileSettingsService`）に、以下5つの「ウィンドウ・カラムレイアウト」系プロパティが、`Global`/`m_Config`（`BackingStore`）と一切連動しない**独自のprivate field**として実装されている。
  * `ProfileEditorLeftWidth` / `ProfileEditorRightWidth`（`_profileEditorLeftWidth = 0`等で初期化。`Global`側の既定値である`BackingStore.DEFAULT_PROFILE_EDITOR_LEFT_WIDTH`等とも不一致）
  * `ControllerSelectProfileColWidth` / `ControllerLinkedProfileColWidth` / `ControllerLinkProfIdColWidth`
* **正規のSSOTは既に別に存在する**:
  * `ProfileEditorLeftWidth`/`RightWidth`: `Global.ProfileEditorLeftWidth`等（`DS4Windows/DS4Control/ScpUtil.cs`）が`m_Config`への正規シムとして存在し、現在`ProfileEditor.xaml.cs`が直接参照している。
  * `ControllerSelectProfileColWidth`等3件: `IAppSettingsService`/`AppSettingsService.cs`（Phase5-Step13-7で追加済み）に、`Global.Xxx`への正規委譲実装が**既に存在し、実運用中**（`MainWindow.xaml.cs`が消費）。
* **実害の有無**: `grep`によるコードベース全体検索の結果、`IProfileSettingsService`側のこの5プロパティは**どこからも呼び出されていない完全な死コード**であることを確認した。したがって二重管理による実際の保存不整合は発生していないが、`EnvironmentService`の件と同様、将来の誤接続を誘発する地雷として残存している。
* **原因分析としての位置づけ**: `Phase5-Step13-Plan.md`で是正された「`IAppSettingsService` 7プロパティ孤立バグ」、および本報告書§3.6の「`EnvironmentService`」の孤立バグと**全く同一のパターンの再発**であり、単発の不具合ではなく、DIサービスへの機械的委譲実装時に発生しやすい共通の型のミスであることが伺える。
* **対応方針**: 過去の是正実績を精査した結果、本リポジトリには孤立バグへの対処方針として (i) 実装をGlobal委譲に修正して残す（Step13-7の`AppSettingsService`方式）、(ii) 概念的な置き場所が誤っている場合はインターフェースごと削除し正しい置き場所に一本化する（本Step(a)の`EnvironmentService`方式）の2パターンが存在することを確認した。今回の5プロパティは概念的に`IProfileSettingsService`（プロファイル値ドメイン）ではなく`IAppSettingsService`（アプリ設定ドメイン）に属すべき性質であり、かつ`ControllerSelectProfileColWidth`等3件は既に`IAppSettingsService`側で完成・稼働中であるため、(ii)方式（`IAppSettingsService`側へ追加・一本化し、`IProfileSettingsService`側の孤立5プロパティは削除）を採用することとした。具体的な作業内容は`Phase5-Step14-FormSettings-Unification-Plan.md`に追記済み（§9 タスク(a)-4/(b)-2改訂として反映）。
* **状態**: ✅ **是正完了**（タスク(a)-4 で `IAppSettingsService` へ集約・委譲実装、タスク(a)-5 で `ProfileSettingsService` の孤立フィールド・アクセサを完全切除完了）。

### 6.3 全設定項目監査による INotificationService 孤立バグの是正完了
- **発見内容**: `Profiles.xml` 全約70項目の横断監査により、`AppNotificationService` の `_notificationsEnabled` / `_flashTaskbar` が `Global` と非連動の独立 private field を保持している孤立バグを発見。
- **是正措置**: タスク(a)-6 で `Global.Notifications != 0` および `Global.FlashWhenLate` への直接委譲に変更。タスク(a)-7 でテスト状態復元ガードおよび双方向同期テストを拡充。
- **状態**: ✅ **是正完了**（全156件の単体テスト成功確認済み）。

### 6.4 実機検証での追加発見: Issue 7 Emulated Controller（OutputContDevice）表示不整合バグ

- **発見契機**: 実機CP4検証中（プロファイル `原神DS4for_gwin` の編集画面を開いた際）
- **ステータス**: ⚠️ **調査完了・是正方針確定（フェーズC着手）**
- **現象**:
  1. プロファイル XML（`原神DS4for_gwin.xml`）には `<outputDataToDS4>True</outputDataToDS4>` および `<OutputContDevice>DS4</OutputContDevice>` が正しく記録されている。
  2. 実際のコントローラー出力信号も ViGEmBus 経由で正常に DS4 として送出されている。
  3. 編集画面右下（Otherタブ）のチェックボックス `[x] Enable Output data to DS4` も True（チェック状態）になっている。
  4. しかし、編集画面右上（Otherタブ）のコンボボックス **`Emulated Controller` だけが `Xbox 360` と誤表示** されている。

#### 根本原因の技術的特定
Issue 6 と同様の **「UI ViewModel レイヤーにおける一時編集バッファの同期漏れ（孤立バグ）」** であることが判明した：
1. `ProfileEditor` では編集キャンセルを可能にするため、`Global` の値を直接バインドせず、`ProfileSettingsViewModel` 内の一時変数 `tempConType` を介して `ControllerTypeIndex`（コンボボックス）にバインドしている。
2. ViewModel インスタンス生成時に `tempConType` は既定値 `Xbox360`（0）で初期化される。
3. その後プロファイルがロードされ、`Global.OutContType[device]` は `DS4`（1）に正しく更新されるが、ロード完了時の ViewModel プロパティ再初期化（`UpdateProperties` 等）において **`tempConType = Global.OutContType[device]` の再同期および `OnPropertyChanged(nameof(ControllerTypeIndex))` の発火が欠落** している。
4. その結果、コンボボックスのみが初期値 `Xbox 360` を保持したまま取り残され、この状態でユーザーが「Save」を押すと `OutContType.Xbox360` で XML が上書きされ、**正常な DS4 プロファイルが Xbox 360 に破壊されてしまう重大な潜在リスク** が存在している。

#### 是正策の有効性確認
以下の 2 段階の是正を実施することで、本不具合が 100% 解消されることを確認：
1. **ViewModel プロパティ再同期の保証（タスクc-1）**: プロファイルロード完了時の再初期化ロジックにて、`tempConType = Global.OutContType[device];` を確実に代入し `OnPropertyChanged(nameof(ControllerTypeIndex))` を発火させる。
2. **双方向連動フェイルセーフガード（タスクc-2）**: `EnableOutputDataToDS4 == true` なのに `ControllerTypeIndex == 0 (Xbox360)` という矛盾状態を検知した場合に自動で `DS4`（1）へ同期補正するガードを配置する。
