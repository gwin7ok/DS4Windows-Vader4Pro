# Phase 5 - Step 14 個別計画書: フォーム/カラム幅設定の SSOT 統一（EnvironmentService重複排除 ＆ Global直参照のDI化）

作成日: 2026-09-10
対象ブランチ: `For-DI-migration-work`
関連ドキュメント:
- `docs-forDIMG/MadeByAgent/Phase5-Step14-RealDevice-Investigation-and-Fix-Report.md`（§3.6 Issue 6）
- `docs-forDIMG/MadeByAgent/Phase5-Plan.md`（Step13/Step14の位置づけ）
- `docs-forDIMG/DI-App-Wide-Migration-Plan.md`（全体ロードマップ）
- `.github/copilot-instructions.md`（3.1 Pure DI原則）

---

## 1. 位置づけ

Phase5-Step14の実機検証中に、「DS4Windows終了時にウィンドウサイズ・位置・各種カラム幅が `Profiles.xml` へ正しく反映されないことがある」という報告を受け調査した。ソースコード監査の結果、以下の構造的問題が判明した（詳細は投稿レポート `Phase5-Step14-RealDevice-Investigation-and-Fix-Report.md` §3.6 Issue 6を参照）。

1. `Global.FormWidth` 等は単一の `BackingStore`（`m_Config`）への薄いラッパーであり、それ自体は二重化していない。
2. しかし `EnvironmentService`（`IEnvironmentService`）に、`AppSettingsService`（`IAppSettingsService`）と同名・同意味の設定（`FormWidth`/`FormHeight`/`FormLocationX`/`FormLocationY`/`RunAtStartup`/`StartMinimized`/`CloseMinimizes`/`UseLang`）が、`Global`/`m_Config` と一切連動しない独自の private field として孤立実装されている。
3. 同じ意味の設定に対し、呼び出し元ファイルによってアクセス経路（DIサービス経由 / `Global`静的直参照）が不統一である。

実機ログ（`ds4windows_log.txt`）解析の結果、保存処理の実行経路自体（`Global.Save()` → `AppSettingsService.Save()` → `IProfileXmlStore.SaveAppSettingsXml()` → `BackingStore.Save()`）は正常に完走していることを確認済みである。したがって本Stepは「保存が失敗している不具合の直接修正」ではなく、**再発防止・監査性確保のための構造是正**と位置づける。実際のリサイズ操作を伴う不具合再現テストは、本Step完了後に別途実施する（§6参照）。

---

## 2. 採用する設計方針

### 2.1 今回（Phase5）採用する方針: 案A ＋ 案C

- **案A（SSOTの固定）**: 永続化対象データの実体は `BackingStore`（＝現 `m_Config`）に置き続ける。既存のXML I/O、ファイル排他ロック（Phase5-Step2/Step6のガードレール）をそのまま活用する。DIサービス（`IAppSettingsService` 等）は、内部に永続対象データ用の独自 private field を **一切持たず**、必ず `BackingStore`（`Global` シム経由）への委譲のみで実装する。
- **案C（機械的な逸脱検出）**: `Global` の対象プロパティ（UI層から見えるべきでないもの）に段階的に `[Obsolete]` を付与し、UI層（View/ViewModel）からの新規 `Global` 直参照をコンパイル警告で検出できるようにする（Phase5-Step15で予定されている `[Obsolete]` 運用の対象を本件にも適用する）。

### 2.2 将来方針: 案B（ドメインごとの完全分割）

中長期的には、`IAppSettingsService` のうち「ウィンドウ幾何情報・カラム幅」を専用の小さいドメインサービス（例: `IWindowLayoutService`）へ分割し、`BackingStore`/`Global` 自体を段階的に解体することを最終形とする。ただし `Profiles.xml` のXML構造（`<AppSettings>` セクション同居、Step6のファイルI/O排他ロック）を含む大規模な再設計を伴うため、**本Stepおよび現行Phase5のロードマップのスコープには含めない**。Phase6以降の独立したテーマとして引き継ぐ。

### 2.3 本計画書のスコープ（今回実施する範囲）

| # | 内容 | 対応する既存指摘 |
|---|---|---|
| (a) | `EnvironmentService`／`IEnvironmentService` の重複プロパティ排除。永続設定は `IAppSettingsService` に一本化し、`IEnvironmentService` は実行時環境プローブ（`IsAdministrator`, `ApplicationVersion`, `RefreshHidHideInfo`, `RefreshFakerInputInfo` 等）専用インターフェースへ純化する | Issue 6 原因分析 2., 3. |
| (b) | UI層（`SettingsViewModel.cs`, `ProfileEditor.xaml.cs`）に残存する `Global.Xxx` 直参照を、`MainWindow.xaml.cs` と同様のDIサービス経由（`appSettingsService.Xxx`）に置換 | Issue 6 原因分析 4. |

以下は今回のスコープ外とし、`Phase5-Step14-RealDevice-Investigation-and-Fix-Report.md` §5 に残存課題として記録済みである。

- (c) 実際のリサイズ・移動操作を伴う実機再現テスト（(a)(b) 適用後に実施）。
- `IsInitialShow` デッドコードの要否整理・除去判断。

---

## 3. 作業対象ファイルの棚卸し

| ファイル | 現状 | 変更内容 |
|---|---|---|
| `DS4Windows/DI/IEnvironmentService.cs` | `FormWidth`/`FormHeight`/`FormLocationX`/`FormLocationY`/`RunAtStartup`/`StartMinimized`/`CloseMinimizes`/`UseLang` を含む | 上記8プロパティの宣言を削除。`IsAdministrator()`/`ApplicationVersion`/`RefreshHidHideInfo()`/`RefreshFakerInputInfo()` のみを残す |
| `DS4Windows/DS4Control/Services/EnvironmentService.cs` | 上記8プロパティを独自 private field で実装 | 対応するフィールド・プロパティ・`OnSettingChanged`発火コードを削除。読み取り専用メソッド群のみ残す |
| `DS4Windows/DI/ServiceRegistration.cs` | `AddSingleton<IEnvironmentService, EnvironmentService>()` | 変更なし（インターフェース縮小のみで登録自体は継続） |
| `DS4Windows/DS4Control/ScpUtil.cs`（`Global.EnvironmentServiceInstance`） | 現状の static シムをそのまま維持 | 変更なし（型 `IEnvironmentService` のメンバが縮小されるのみ） |
| `DS4WindowsTests/EnvironmentServiceTests.cs` | 孤立フィールドの既定値・非連動状態をテストで固定化している（`FormWidth==782`等の直接アサート、`GlobalShim_ShouldSynchronizeWithService`で`StartMinimized`のみ検証） | 削除対象プロパティに関するテストケースを削除し、`IsAdministrator`/`ApplicationVersion`等の残存メンバに対するテストへ絞り込む |
| `DS4Windows/DS4Forms/ViewModels/SettingsViewModel.cs` | `StartMinimize`/`CloseMinimizes` が `DS4Windows.Global.StartMinimized`/`Global.CloseMini` を直接参照 | コンストラクタで `IAppSettingsService` を受け取り（未受領の場合は `MainWindow.xaml.cs` の既存パターンに倣い `DS4WinWPF.AppHost.GetService<IAppSettingsService>()` フォールバック）、`appSettingsService.StartMinimized`/`appSettingsService.CloseMinimizes` に置換 |
| `DS4Windows/DS4Forms/ProfileEditor.xaml.cs` | `Global.ProfileEditorLeftWidth`/`Global.ProfileEditorRightWidth`/`Global.SpecialActionNameColWidth`/`Global.SpecialActionTriggerColWidth`/`Global.SpecialActionDetailColWidth` を直接参照（338-402行付近） | `IAppSettingsService` をDI解決（既存パターンに倣う）し、`appSettingsService.Xxx` に置換 |

> **重要（No Feature Drop）**: いずれの置換も「アクセス経路の変更」のみであり、`BackingStore`（`m_Config`）という実データの格納先・シリアライズ方法・ロック機構は一切変更しない。挙動（保存タイミング・既定値・XML構造）に変更が生じないことを各マイクロタスクで確認する。

---

## 4. マイクロタスク breakdown

### タスク(a)-1: `IEnvironmentService` インターフェースの縮小
- `FormWidth`/`FormHeight`/`FormLocationX`/`FormLocationY`/`RunAtStartup`/`StartMinimized`/`CloseMinimizes`/`UseLang` の宣言を削除。
- ビルドし、コンパイルエラーとなった参照箇所を全て洗い出す（現状の調査では実参照ゼロだが、ビルドエラーとして機械的に再確認する）。

### タスク(a)-2: `EnvironmentService` 実装の純化
- 上記8プロパティに対応する private field・getter/setter・`OnSettingChanged()`呼び出しを削除。
- クラスに残るのは `IsAdministrator()`/`ApplicationVersion`/`RefreshHidHideInfo()`/`RefreshFakerInputInfo()`（いずれも`Global`への薄い委譲）のみとする。

### タスク(a)-3: `EnvironmentServiceTests.cs` の是正
- `Defaults_ShouldMatchExpected`: 削除対象プロパティのアサートを削除し、残存メンバの検証に絞る（または本テストクラス自体を残存メンバ向けに再設計）。
- `GlobalShim_ShouldSynchronizeWithService`: `StartMinimized` は削除対象のため、`IsAdministrator`等の残存メンバを用いた同義のシム検証に置き換える。
- `dotnet test` で全件成功することを確認する。

### タスク(b)-1: `SettingsViewModel.cs` の `Global` 直参照置換
- `StartMinimize`/`CloseMinimizes` プロパティの実装を `appSettingsService.StartMinimized`/`appSettingsService.CloseMinimizes` に置換。
- 依存取得は既存ViewModel群のコンストラクタパターン（Phase4/5で確立済みのDI解決＋フォールバック）に厳密に合わせる。
- UI（設定画面のチェックボックス等）の見た目・保存挙動に差異がないことを確認する。

### タスク(b)-2: `ProfileEditor.xaml.cs` の `Global` 直参照置換
- `Global.ProfileEditorLeftWidth`/`Global.ProfileEditorRightWidth`/`Global.SpecialActionNameColWidth`/`Global.SpecialActionTriggerColWidth`/`Global.SpecialActionDetailColWidth` の読み書き箇所（338, 339, 377, 378, 385, 401, 402行付近）を `appSettingsService.Xxx` に置換。
- `SaveSplitterAndColumnWidths`相当の処理・ログ出力（`[SaveSplitterAndColumnWidths] Saved left=...`）の内容は変更しない（文言・タイミングを維持）。

### タスク(b)-3: 単体テスト・ビルド確認
- `dotnet build` でエラー・警告増加がないことを確認。
- `dotnet test`（`DS4WindowsTests`/`StandaloneTests`）が全件成功することを確認。

### タスク-4: ドキュメント更新
- 本計画書の完了条件チェックを更新。
- `Phase5-Step14-RealDevice-Investigation-and-Fix-Report.md` の Issue 6 ステータスを「対応中」から「解決」に更新（(a)(b)完了時点。(c)は別途）。

---

## 5. リスクと回避策

| リスク | 回避策 |
|---|---|
| `IEnvironmentService` のプロパティ削除により、未発見の参照箇所でビルドエラー以外の実行時不整合が発生する | タスク(a)-1でビルドエラーとして機械的に検出できるため、リフレクション経由等の動的参照がない限り検出漏れは発生しない。念のため `grep` によるテキスト検索でも二重確認する |
| `SettingsViewModel`/`ProfileEditor` のDI解決失敗時（`appSettingsService`がnull）にNullReferenceExceptionが発生する | `MainWindow.xaml.cs` で確立済みの `DS4WinWPF.AppHost.GetService<T>() ?? Global.Xxx` フォールバックパターンを踏襲する |
| 既存テスト（`EnvironmentServiceTests.cs`）の大幅な書き換えにより、意図せず別の検証観点が失われる | 削除するのは「孤立していたこと自体を固定化していた」アサートのみとし、`IsAdministrator`等の正当な検証は維持・必要なら追加する |
| UI層のプロパティアクセス経路変更によりカラム幅・ウィンドウサイズの保存タイミングが変わる | `BackingStore`実体・呼び出しタイミング（イベントハンドラの発火位置）は変更せず、読み書き先のAPIのみを置換するため、挙動的な差異は生じない設計とする |

---

## 6. 本計画書のスコープ外（別タスクとして継続管理）

- **Issue 6 (c) 網羅的な実機再現テスト**: 実際のセッション全体を通じたログ取得（`MainWindow.SizeChanged`/`MainWindow.LocationChanged`/`ProfileEditor_Closed`の完全なトレース採取・複数セッションにまたがる回帰確認等）による、より広範な実機再検証は引き続き別タスクとして継続管理する（`Phase5-Step14-RealDevice-Investigation-and-Fix-Report.md` §5に追跡事項として記録済み）。ただし、本計画書(a)(b)(a)-4(a)-5で実際に変更する機能そのものが実機で正常動作することの確認は、**本計画書のスコープ内**の完了条件として §10 に定義する（旧版では未記載だったため今回追加）。
- **`IsInitialShow` デッドコード整理**: `MainDS4Window_SizeChanged`/`MainDS4Window_LocationChanged`内の`!IsInitialShow`ガードが常時無効である点の要否判断（意図した初期表示保護ロジックとして復活させるか、単純に除去するか）は、上記(c)の広範な実機検証結果を踏まえて別途判断する。
- **案B（ドメイン完全分割・`BackingStore`/`Global`解体）**: Phase6以降の将来テーマとして引き継ぐ。

---

## 7. 完了条件

- [ ] `IEnvironmentService`/`EnvironmentService` から永続設定プロパティ（8件）が削除され、実行時環境プローブ専用インターフェースへ純化されている。
- [ ] `SettingsViewModel.cs`/`ProfileEditor.xaml.cs` の `Global.Xxx` 直参照が `appSettingsService.Xxx`（DIサービス経由）に置換されている。
- [ ] `BackingStore`（`m_Config`）の実データ・シリアライズ・保存タイミングに変更がない（No Feature Drop）。
- [ ] `EnvironmentServiceTests.cs` を含む既存の全自動テストが成功する。
- [ ] `dotnet build` でエラー・警告の増加がない。
- [ ] `Phase5-Step14-RealDevice-Investigation-and-Fix-Report.md` のIssue 6ステータスが更新されている。
- [ ] §10「実機検証計画」の各テスト項目（T1〜T10）が実施され、いずれも合格している（本計画書変更対象機能そのものの実機動作確認）。
- [ ] §6に残る、より網羅的なセッション全体のログ再取得によるIssue 6 (c)再現テストは、残存課題として引き続き明記され、次の作業に引き継がれている。

---

## 8. 進行ルール（`.github/copilot-instructions.md`準拠）

- 本計画書の承認後、マイクロタスク単位（§4のタスク単位）で実装・コミットを行う。
- 各マイクロタスク完了ごとに `dotnet test` を実行しグリーンを確認する。
- `Global`静的経路は本Stepでは削除せず、`EnvironmentService`側の重複解消および呼び出し元のDI化のみを行う（Strangler Figパターン継続、Legacy shim即時削除の禁止）。

---

## 9. 2026-09-10 追記: 実装再開に伴う進捗監査と追加スコープ（選択肢A採用）

### 9.1 §4マイクロタスクの実施状況（再開時点）

タスク(a)-1〜(a)-3は完了済み、タスク(b)-1〜(b)-3およびタスク-4は未着手であることをソースコード監査で確認した（詳細は `Phase5-Step14-RealDevice-Investigation-and-Fix-Report.md` §6.1）。

| タスク | 状態 |
|---|---|
| (a)-1 `IEnvironmentService` 縮小 | ✅ 完了 |
| (a)-2 `EnvironmentService` 純化 | ✅ 完了 |
| (a)-3 `EnvironmentServiceTests.cs` 是正 | ✅ 完了 |
| (b)-1 `SettingsViewModel.cs` 置換 | ❌ 未着手 |
| (b)-2 `ProfileEditor.xaml.cs` 置換 | ❌ 未着手 |
| (b)-3 テスト・ビルド確認 | ❌ 未着手 |
| タスク-4 ドキュメント更新 | ❌ 未着手 |

### 9.2 新規発見: `IProfileSettingsService` の孤立5プロパティ

タスク(b)-2の着手前調査として、`ProfileEditor.xaml.cs` が参照する `Global.ProfileEditorLeftWidth` 等の置換先を検討した結果、`DS4Windows/DS4Control/Services/ProfileSettingsService.cs`（`IProfileSettingsService`）に、`EnvironmentService`と同一パターンの孤立プロパティが5件存在することを新たに発見した（詳細は `Phase5-Step14-RealDevice-Investigation-and-Fix-Report.md` §6.2）。

- `ProfileEditorLeftWidth` / `ProfileEditorRightWidth`（独自 private field。`Global.ProfileEditorLeftWidth`等という正規SSOTが既に別に存在）
- `ControllerSelectProfileColWidth` / `ControllerLinkedProfileColWidth` / `ControllerLinkProfIdColWidth`（独自 private field。`IAppSettingsService`側に同名プロパティが既にPhase5-Step13-7で実装・実運用済み）

これら5件はコードベース全体を検索しても呼び出し箇所ゼロの死コードであり、実害はないが将来の誤接続を誘発する地雷である点も`EnvironmentService`のケースと同一である。

### 9.3 採用する対応方針（選択肢A）

過去の孤立バグ是正実績を精査した結果、本リポジトリには (i) 実装を`Global`委譲に修正して残す方式（Step13-7の`AppSettingsService`）と、(ii) 概念的な置き場所が誤っている場合はインターフェースごと削除し正しい置き場所に一本化する方式（本Step(a)の`EnvironmentService`）の2パターンが存在する。今回の5プロパティは意味的に「ウィンドウ・カラムレイアウト」であり`IProfileSettingsService`（プロファイル値ドメイン）には属さないこと、かつ`ControllerSelectProfileColWidth`等3件は既に`IAppSettingsService`側で完成・稼働中であることから、**方式(ii)を選択肢A**として採用する。

- `IAppSettingsService`/`AppSettingsService`に、未実装の`ProfileEditorLeftWidth`/`ProfileEditorRightWidth`/`SpecialActionNameColWidth`/`SpecialActionTriggerColWidth`/`SpecialActionDetailColWidth`を追加する（既存の`FormWidth`等と同じ「`Global.Xxx`への薄い委譲＋`NotifyChanged`」パターンを踏襲）。
- `IProfileSettingsService`/`ProfileSettingsService`から、死コードである5プロパティ（`ProfileEditorLeftWidth`/`ProfileEditorRightWidth`/`ControllerSelectProfileColWidth`/`ControllerLinkedProfileColWidth`/`ControllerLinkProfIdColWidth`）を削除する。

不採用とした選択肢:
- **選択肢B**（`IProfileSettingsService`側を方式(i)で「修正して残す」）: 同一設定に`IAppSettingsService`と`IProfileSettingsService`という2つの正当な経路が並立し、Issue 6が問題視する「アクセス経路の不統一」を新規に作り出すため不採用。
- **選択肢C**（`IProfileSettingsService`側の削除を見送り、`ProfileEditor.xaml.cs`の置換のみ実施）: 死コードとはいえ孤立プロパティを地雷として残置することになり、`EnvironmentService`是正で採用した「実害ゼロでも将来の誤接続防止のため撤去する」方針から逸脱するため不採用。

### 9.4 §3 作業対象ファイル棚卸しへの追加

| ファイル | 現状 | 変更内容 |
|---|---|---|
| `DS4Windows/DI/IAppSettingsService.cs` / `AppSettingsService.cs` | `ProfileEditorLeftWidth`等5プロパティ未定義 | `Global.Xxx`への薄い委譲として5プロパティを新設（既存`FormWidth`等と同一パターン） |
| `DS4Windows/DS4Control/Services/ProfileSettingsService.cs` / `DS4Windows/DI/IProfileSettingsService.cs` | `ProfileEditorLeftWidth`/`ProfileEditorRightWidth`/`ControllerSelectProfileColWidth`/`ControllerLinkedProfileColWidth`/`ControllerLinkProfIdColWidth`を独自private fieldとして孤立実装（呼び出し箇所ゼロの死コード） | 上記5プロパティの宣言・実装・privateフィールドを削除 |

### 9.5 マイクロタスク breakdown（追加分）

#### タスク(a)-4: `IAppSettingsService`/`AppSettingsService` への5プロパティ追加
- `IAppSettingsService`に`ProfileEditorLeftWidth`/`ProfileEditorRightWidth`/`SpecialActionNameColWidth`/`SpecialActionTriggerColWidth`/`SpecialActionDetailColWidth`を追加。
- `AppSettingsService`に、既存の`FormWidth`等と同一パターン（`get => Global.Xxx; set { if (Global.Xxx != value) { Global.Xxx = value; NotifyChanged(nameof(Xxx)); } }`）で実装。

#### タスク(a)-5: `IProfileSettingsService`/`ProfileSettingsService` の孤立5プロパティ削除
- `IProfileSettingsService`から`ProfileEditorLeftWidth`/`ProfileEditorRightWidth`/`ControllerSelectProfileColWidth`/`ControllerLinkedProfileColWidth`/`ControllerLinkProfIdColWidth`の宣言を削除。
- `ProfileSettingsService`から対応する private field・getter/setter・`OnProfileSettingChanged`呼び出しを削除。
- ビルドし、参照箇所がゼロであることを機械的に再確認する（事前grep調査ではゼロ件を確認済み）。

#### タスク(b)-1: `SettingsViewModel.cs` の `Global` 直参照置換（既存タスクを継続、変更なし）
- `StartMinimize`/`CloseMinimizes` プロパティの実装を `appSettingsService.StartMinimized`/`appSettingsService.CloseMinimizes` に置換。
- 依存取得は既存ViewModel群のコンストラクタパターンに厳密に合わせる（本ファイルは`_appSettings`フィールドが既にコンストラクタで注入済みのため、これを使用する）。
- UI（設定画面のチェックボックス等）の見た目・保存挙動に差異がないことを確認する。

#### タスク(b)-2改訂: `ProfileEditor.xaml.cs` の `Global` 直参照置換
- タスク(a)-4完了後、`Global.ProfileEditorLeftWidth`/`Global.ProfileEditorRightWidth`/`Global.SpecialActionNameColWidth`/`Global.SpecialActionTriggerColWidth`/`Global.SpecialActionDetailColWidth`の読み書き箇所（338, 339, 377, 378, 385, 401, 402行付近）を、新設した`appSettingsService.Xxx`（`IAppSettingsService`経由）に置換する。
- `SaveSplitterAndColumnWidths`相当の処理・ログ出力（`[SaveSplitterAndColumnWidths] Saved left=...`）の内容は変更しない（文言・タイミングを維持）。

#### タスク(b)-3: 単体テスト・ビルド確認（既存タスクを継続、変更なし）
- `dotnet build` でエラー・警告増加がないことを確認。
- `dotnet test`（`DS4WindowsTests`/`StandaloneTests`）が全件成功することを確認。

### 9.6 §7 完了条件への追加項目

- [ ] `IAppSettingsService`/`AppSettingsService`に`ProfileEditorLeftWidth`等5プロパティが`Global`への薄い委譲として追加されている。
- [ ] `IProfileSettingsService`/`ProfileSettingsService`から孤立していた5プロパティ（`ProfileEditorLeftWidth`/`ProfileEditorRightWidth`/`ControllerSelectProfileColWidth`/`ControllerLinkedProfileColWidth`/`ControllerLinkProfIdColWidth`）が削除されている。
- [ ] `ProfileEditor.xaml.cs`が新設された`appSettingsService.Xxx`経由でこれらの設定にアクセスしている。
- [ ] §10「実機検証計画」に定義した全テスト項目が実機で実施され、いずれも合格している。

---

## 10. 実機検証計画（本Stepの完了条件に含める）

`dotnet test`によるユニットテストは、DIサービスの委譲実装が正しいことを検証できるが、「実際のWPF UIイベント発火順序」「XMLファイルへの実書き込み・再起動後の読み込み」「管理者権限昇格の実環境判定」等はユニットテストの対象外である。旧版の計画書では実機確認が(c)として本計画のスコープ外に切り出されていたが、**本計画書(a)〜(a)-5・(b)-1〜(b)-3で変更する機能そのものについては、以下の実機検証を本Stepの完了条件とする**。

### 10.1 テスト項目

| # | 対象 | 対応タスク | 手順 | 期待結果 |
|---|---|---|---|---|
| T1 | `IEnvironmentService.IsAdministrator()` | (a)-1/(a)-2 | DS4Windowsを通常権限・管理者権限それぞれで起動し、設定画面の「管理者権限」関連表示（プロセス優先度RealTime選択可否等）を確認する | 純化前と同じ判定結果が得られ、`RealTime`優先度選択時に非管理者では警告・拒否動作が変わらず維持されている |
| T2 | `IEnvironmentService.ApplicationVersion` | (a)-1/(a)-2 | メイン画面のタイトルバー／バージョン表示欄を確認する | 純化前と同じバージョン文字列が表示される |
| T3 | `IEnvironmentService.RefreshHidHideInfo`/`RefreshFakerInputInfo` | (a)-1/(a)-2 | 設定画面でHidHide/FakerInputのインストール状態表示箇所を開く、またはドライバ状態を変更した後に再取得操作を行う | 例外が発生せず、最新のインストール状態が正しく反映される |
| T4 | `SettingsViewModel.StartMinimize` | (b)-1 | 設定画面で「起動時に最小化」をON/OFFに変更→アプリ終了→再起動 | 設定した状態が維持され、ON時は最小化した状態で起動する |
| T5 | `SettingsViewModel.CloseMinimizes` | (b)-1 | 設定画面で「閉じるボタンでトレイに最小化」をON/OFFに変更→メインウィンドウを閉じる操作を行う | 設定どおりに「トレイに最小化」または「終了」が実行される |
| T6 | `ProfileEditor` スプリッター位置（`ProfileEditorLeftWidth`/`RightWidth`） | (a)-4/(b)-2 | プロファイル編集ウィンドウを開き、左右ペインの境界（スプリッター）をドラッグして幅を変更→ウィンドウを閉じる→再度開く | 変更した幅で復元される |
| T7 | `ProfileEditor` の SpecialAction 一覧カラム幅（Name/Trigger/Detail） | (a)-4/(b)-2 | プロファイル編集ウィンドウのSpecialAction一覧で各カラム境界をドラッグして幅を変更→ウィンドウを閉じる→再度開く | 変更したカラム幅で復元される |
| T8 | `Profiles.xml`への永続化ラウンドトリップ | (a)-4/(a)-5/(b)-2 | T6/T7の操作後、DS4Windowsを完全終了→`Profiles.xml`を開き`profileEditorLeftWidth`等の値が更新されていることを確認→再起動して復元されることを確認 | XML上の値が変更後の値に更新されており、再起動後も同じ値で復元される（保存タイミング・XML構造に変化がないこと＝No Feature Drop確認） |
| T9 | 回帰確認: コントローラー一覧カラム幅（`ControllerSelectProfileColWidth`等、既存Step13-7実装分） | (a)-5（削除の副作用確認） | `IProfileSettingsService`側の同名孤立プロパティ削除後、コントローラー一覧のカラム幅変更・保存・再起動復元が従来どおり動作することを確認する | Step13-7実装の`IAppSettingsService`経由の動作に影響がなく、正常に保存・復元される |
| T10 | ビルド後の起動確認 | 全タスク | Release/Debug両方でビルドし、アプリを起動、上記T1〜T9の一連の操作をひととおり実施 | クラッシュ・例外ダイアログが発生しない |

### 10.2 実施タイミングと記録

- 実機検証は、本計画書のタスク(a)-4〜(a)-5・(b)-1〜(b)-3のコード実装完了後、かつ`dotnet test`グリーン確認後に実施する。
- 検証結果（合格・不合格、発見した不具合があればその詳細）は `Phase5-Step14-RealDevice-Investigation-and-Fix-Report.md` に追記し、Issue 6のステータス更新（「対応中」→「解決」）はT1〜T10全件合格後に行う。
- T1〜T10のいずれかが不合格の場合、Issue 6のステータスは「対応中」のまま維持し、原因調査・追加是正を行った上で再実施する。