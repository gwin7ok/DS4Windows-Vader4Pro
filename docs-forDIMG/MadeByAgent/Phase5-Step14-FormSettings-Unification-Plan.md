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

- **Issue 6 (c) 実機再現テスト**: 本計画書の(a)(b)適用後、実際にウィンドウのリサイズ・移動・カラム幅変更を行った状態でログを取得し、`MainWindow.SizeChanged`/`MainWindow.LocationChanged`/`ProfileEditor_Closed`のトレースが正しく出力され、`Profiles.xml`に反映されることを実機で確認する。`Phase5-Step14-RealDevice-Investigation-and-Fix-Report.md` §5に追跡事項として記録済み。
- **`IsInitialShow` デッドコード整理**: `MainDS4Window_SizeChanged`/`MainDS4Window_LocationChanged`内の`!IsInitialShow`ガードが常時無効である点の要否判断（意図した初期表示保護ロジックとして復活させるか、単純に除去するか）は、(c)の実機検証結果を踏まえて別途判断する。
- **案B（ドメイン完全分割・`BackingStore`/`Global`解体）**: Phase6以降の将来テーマとして引き継ぐ。

---

## 7. 完了条件

- [ ] `IEnvironmentService`/`EnvironmentService` から永続設定プロパティ（8件）が削除され、実行時環境プローブ専用インターフェースへ純化されている。
- [ ] `SettingsViewModel.cs`/`ProfileEditor.xaml.cs` の `Global.Xxx` 直参照が `appSettingsService.Xxx`（DIサービス経由）に置換されている。
- [ ] `BackingStore`（`m_Config`）の実データ・シリアライズ・保存タイミングに変更がない（No Feature Drop）。
- [ ] `EnvironmentServiceTests.cs` を含む既存の全自動テストが成功する。
- [ ] `dotnet build` でエラー・警告の増加がない。
- [ ] `Phase5-Step14-RealDevice-Investigation-and-Fix-Report.md` のIssue 6ステータスが更新されている。
- [ ] Issue 6 (c)（実機再現テスト）が残存課題として明記され、次の作業に引き継がれている。

---

## 8. 進行ルール（`.github/copilot-instructions.md`準拠）

- 本計画書の承認後、マイクロタスク単位（§4のタスク単位）で実装・コミットを行う。
- 各マイクロタスク完了ごとに `dotnet test` を実行しグリーンを確認する。
- `Global`静的経路は本Stepでは削除せず、`EnvironmentService`側の重複解消および呼び出し元のDI化のみを行う（Strangler Figパターン継続、Legacy shim即時削除の禁止）。