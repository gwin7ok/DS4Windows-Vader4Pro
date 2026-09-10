# Phase 5 - Step 14 個別計画書: フォーム/カラム幅設定の SSOT 統一（EnvironmentService重複排除 ＆ Global直参照のDI化）

作成日: 2026-09-10
改訂日: 2026-09-11（タスク一本化統合 ＆ 全設定項目監査に伴う INotificationService 是正追記・IUdpServerService 見積もり反映）
対象ブランチ: `For-DI-migration-work`
関連ドキュメント:
- `docs-forDIMG/MadeByAgent/Phase5-Step14-RealDevice-Investigation-and-Fix-Report.md`（§3.6 Issue 6）
- `docs-forDIMG/MadeByAgent/Phase5-Plan.md`（Step13/Step14の位置づけ）
- `docs-forDIMG/DI-App-Wide-Migration-Plan.md`（全体ロードマップ）
- `.github/copilot-instructions.md`（3.1 Pure DI原則）

---

## 1. 位置づけ

Phase5-Step14の実機検証中に、「DS4Windows終了時にウィンドウサイズ・位置・各種カラム幅が `Profiles.xml` へ正しく反映されないことがある」という報告を受け調査した。ソースコード監査の結果、以下の構造的問題が判明した（詳細は調査レポート `Phase5-Step14-RealDevice-Investigation-and-Fix-Report.md` §3.6 Issue 6を参照）。

1. `Global.FormWidth` 等は単一の `BackingStore`（`m_Config`）への薄いラッパーであり、それ自体は二重化していない。
2. しかし `EnvironmentService`（`IEnvironmentService`）に、`AppSettingsService`（`IAppSettingsService`）と同名・同意味の設定（`FormWidth`/`FormHeight`/`FormLocationX`/`FormLocationY`/`RunAtStartup`/`StartMinimized`/`CloseMinimizes`/`UseLang`）が、`Global`/`m_Config` と一切連動しない独自の private field として孤立実装されている。
3. 同じ意味の設定に対し、呼び出し元ファイルによってアクセス経路（DIサービス経由 / `Global`静的直参照）が不統一である。
4. 全設定項目（約70項目）の横断監査により、`IProfileSettingsService`（カラム幅5プロパティ）および `INotificationService`（`_notificationsEnabled`/`_flashTaskbar`）にも同様の孤立バグ（実体 `BackingStore` と非連動の独立 private field を保持）が存在することが判明した。

実機ログ（`ds4windows_log.txt`）解析の結果、保存処理の実行経路自体（`Global.Save()` → `AppSettingsService.Save()` → `IProfileXmlStore.SaveAppSettingsXml()` → `BackingStore.Save()`）は正常に完走していることを確認済みである。したがって本Stepは「保存が失敗している不具合の直接修正」にとどまらず、**再発防止・監査性確保のための設定 SSOT 構造是正** と位置づける。

---

## 2. 採用する設計方針

### 2.1 今回（Phase5）採用する方針: 案A ＋ 案C

* **案A（SSOTの固定 ＆ 委譲化）**: 永続化対象データの実体は `BackingStore`（＝現 `m_Config`）に置き続ける。既存のXML I/O、ファイル排他ロック（Phase5-Step2/Step6のガードレール）をそのまま活用する。DIサービス（`IAppSettingsService`, `INotificationService` 等）は、内部に永続対象データ用の独自 private field を **一切持たず**、必ず `BackingStore`（`Global` シム経由）への委譲のみで実装する。
* **案C（機械的な逸脱検出）**: `Global` の対象プロパティ（UI層から見えるべきでないもの）に段階的に `[Obsolete]` を付与し、UI層（View/ViewModel）からの新規 `Global` 直参照をコンパイル警告で検出できるようにする（Phase5-Step15で予定されている `[Obsolete]` 運用の対象を本件にも適用する）。

### 2.2 将来方針: 案B（ドメインごとの完全分割）

中長期的には、`IAppSettingsService` のうち「ウィンドウ幾何情報・カラム幅」を専用の小さいドメインサービス（例: `IWindowLayoutService`）へ分割し、`BackingStore`/`Global` 自体を段階的に解体することを最終形とする。ただし `Profiles.xml` のXML構造（`<AppSettings>` セクション同居、Step6のファイルI/O排他ロック）を含む大規模な再設計を伴うため、**本Stepおよび現行Phase5のロードマップのスコープには含めない**。Phase6以降の独立したテーマとして引き継ぐ。

### 2.3 本計画書のスコープ（今回実施する範囲）

| # | 内容 | 対応する是正対象 |
|---|---|---|
| (a) | DI サービス側の孤立 private field 排除と SSOT 委譲化（`IEnvironmentService`, `IProfileSettingsService`, `INotificationService`） | 孤立バグの根絶（Issue 6原因 2, 3 ＋ 監査新規発見） |
| (b) | UI層（`SettingsViewModel.cs`, `ProfileEditor.xaml.cs`）に残存する `Global.Xxx` 直参照を、DIサービス経由（`appSettingsService.Xxx` 等）に置換 | アクセス経路統一（Issue 6原因 4） |

---

## 3. 作業対象ファイルの棚卸し（完全統合版）

| ファイル | 現状 | 変更内容 | 進捗状況 |
|---|---|---|---|
| `DS4Windows/DI/IEnvironmentService.cs` | 8設定プロパティを宣言 | 8プロパティの宣言を削除、環境プローブのみ残す | **完了** (タスクa-1) |
| `DS4Windows/DS4Control/Services/EnvironmentService.cs` | 独自 private field 8個を保持 | 独自フィールド・アクセサ削除、読み取り専用純化 | **完了** (タスクa-2) |
| `DS4Windows/DI/ServiceRegistration.cs` | `AddSingleton<IEnvironmentService, ...>()` | 変更なし（登録維持） | **完了** |
| `DS4Windows/DS4Control/ScpUtil.cs` | static シム | 変更なし（型メンバ縮小のみ） | **完了** |
| `DS4WindowsTests/EnvironmentServiceTests.cs` | 孤立フィールド前提のテスト | 削除対象プロパティのテストを撤去 | **完了** (タスクa-3) |
| `DS4Windows/DI/IAppSettingsService.cs` | カラム幅5プロパティ未定義 | カラム幅5プロパティの定義を追加 | **完了** (タスクa-4) |
| `DS4Windows/DS4Control/Services/AppSettingsService.cs` | カラム幅5プロパティ未実装 | `Global` 委譲として5プロパティを実装 | **完了** (タスクa-4) |
| `DS4Windows/DI/IProfileSettingsService.cs` | カラム幅5プロパティを宣言 | カラム幅5プロパティの宣言を削除 | **完了** (タスクa-5) |
| `DS4Windows/DS4Control/Services/ProfileSettingsService.cs` | 独自 private field 5個を保持 | 独自フィールド・アクセサを完全削除 | **完了** (タスクa-5) |
| `DS4Windows/DS4Control/Services/AppNotificationService.cs` | `_notificationsEnabled`, `_flashTaskbar` を孤立保持 | 独自フィールド撤去、`Global`（`m_Config`）委譲へ変更 | **未着手** (タスクa-6) |
| `DS4WindowsTests/NotificationServiceTests.cs` | 孤立フィールド前提のテストコード | `Global` 連動を検証するテストへ是正・拡充 | **未着手** (タスクa-7) |
| `DS4Windows/DS4Forms/ViewModels/SettingsViewModel.cs` | `Global.StartMinimized` 等を直参照 | コンストラクタ注入の `IAppSettingsService` 経由へ置換 | **未着手** (タスクb-1) |
| `DS4Windows/DS4Forms/ProfileEditor.xaml.cs` | `Global.ProfileEditorLeftWidth` 等を直参照 | `IAppSettingsService` を解決し `appSettingsService.Xxx` に置換 | **未着手** (タスクb-2) |

> **重要（No Feature Drop）**: いずれの置換も「アクセス経路の変更」のみであり、`BackingStore`（`m_Config`）という実データの格納先・シリアライズ方法・ロック機構は一切変更しない。挙動（保存タイミング・既定値・XML構造）に変更が生じないことを各マイクロタスクで確認する。

---

## 4. マイクロタスク breakdown（完全一本化）

### フェーズA: DI サービス側の孤立フィールド排除と SSOT 委譲化

- [x] **タスク(a)-1: `IEnvironmentService` インターフェースの縮小**
  - `FormWidth`, `FormHeight`, `FormLocationX`, `FormLocationY`, `RunAtStartup`, `StartMinimized`, `CloseMinimizes`, `UseLang` の8プロパティ宣言を削除。
  - 環境プローブ（`IsAdministrator()`, `ApplicationVersion`, `RefreshHidHideInfo()`, `RefreshFakerInputInfo()` 等）のみを残す。

- [x] **タスク(a)-2: `EnvironmentService` 実装の純化**
  - 上記8プロパティの独自 private field およびアクセサ、`OnSettingChanged` 発火コードを完全削除。

- [x] **タスク(a)-3: `EnvironmentServiceTests.cs` の是正**
  - 削除した8プロパティに関する単体テストケースを撤去し、環境プローブ機能のテストのみに絞り込む。

- [x] **タスク(a)-4: `IAppSettingsService` / `AppSettingsService` への5プロパティ追加**
  - `IAppSettingsService` にカラム幅5プロパティを追加宣言：
    - `int ProfileEditorLeftWidth { get; set; }`
    - `int ProfileEditorRightWidth { get; set; }`
    - `int SpecialActionNameColWidth { get; set; }`
    - `int SpecialActionTriggerColWidth { get; set; }`
    - `int SpecialActionDetailColWidth { get; set; }`
  - `AppSettingsService.cs` に `Global`（`BackingStore`）委譲として上記5プロパティを実装。

- [x] **タスク(a)-5: `IProfileSettingsService` / `ProfileSettingsService` の孤立5プロパティ削除**
  - `IProfileSettingsService` から上記5プロパティの宣言を削除。
  - `ProfileSettingsService.cs` から孤立 private field およびアクセサを完全削除。

- [ ] **タスク(a)-6: `AppNotificationService.cs` の孤立フィールド排除と Global 委譲化**
  - `_notificationsEnabled`（独自フィールド）を削除し、`Global.Instance.Notifications != 0` へ委譲。
  - `_flashTaskbar`（独自フィールド）を削除し、`Global.Instance.FlashWhenLate` へ委譲。
  - `SendNotification` メソッド等における通知有効判定が `Global.Instance.Notifications != 0` と同期して動作することを確認。

- [ ] **タスク(a)-7: `NotificationServiceTests.cs` の是正**
  - 孤立フィールド前提のテストを改修し、`Global` との双方向同期・委譲が正しく機能することを検証する単体テストを追加。

---

### フェーズB: UI 層の Global 直参照置換とアクセス経路統一

- [ ] **タスク(b)-1: `SettingsViewModel.cs` の Global 直参照置換**
  - コンストラクタ引数で `IAppSettingsService` を受領（Pure DI 原則）。
  - 引数なしコンストラクタは `DS4WinWPF.AppHost.GetService<IAppSettingsService>()` によるフォールバックを維持（§2.1 フォールバック・シム維持原則）。
  - `StartMinimized`, `CloseMinimizes` 等のプロパティの getter/setter を `appSettingsService.StartMinimized` / `appSettingsService.CloseMinimizes` に置換。

- [ ] **タスク(b)-2: `ProfileEditor.xaml.cs` の Global 直参照置換**
  - `ProfileEditor.xaml.cs`（338〜402行付近）における以下の直参照を置換：
    - `Global.ProfileEditorLeftWidth` → `appSettingsService.ProfileEditorLeftWidth`
    - `Global.ProfileEditorRightWidth` → `appSettingsService.ProfileEditorRightWidth`
    - `Global.SpecialActionNameColWidth` → `appSettingsService.SpecialActionNameColWidth`
    - `Global.SpecialActionTriggerColWidth` → `appSettingsService.SpecialActionTriggerColWidth`
    - `Global.SpecialActionDetailColWidth` → `appSettingsService.SpecialActionDetailColWidth`
  - インスタンスはコンストラクタ注入または `AppHost.GetService<IAppSettingsService>()` 経由で取得。

- [ ] **タスク(b)-3: 通知設定アクセス経路の監査・統一**
  - UI層（`SettingsViewModel.cs` 等）における通知関連設定のアクセス経路を確認し、`IAppSettingsService`（設定永続化）と `INotificationService`（通知実行）の間で SSOT（`BackingStore`）が整合していることを確認。

- [ ] **タスク(b)-4: 単体テスト・クリーンビルド確認**
  - 変更した全プロジェクトのビルド確認。
  - 全単体テスト（169件超）を実行し、全件 PASS を確認。

---

### フェーズC: ドキュメント更新・実機検証

- [ ] **タスク-4: ドキュメント・進捗ステータス更新**
  - `Phase5-Status.md`, `Phase5-Plan.md` を更新。
  - `Phase5-Step14-RealDevice-Investigation-and-Fix-Report.md` §3.6 Issue 6 を完了（是正済み）に更新。

---

## 5. リスクと回避策

1. **No Feature Drop の死守**:
   * いずれの置換も「アクセス経路の統一」のみであり、`BackingStore`（`m_Config`）という実データの格納先・XMLシリアライズ方法・ファイルロック機構は一切変更しない。
2. **UI 初期化タイミング（WPF XAML 早期発火）への配慮**:
   * Issue 3（`FrictionUD` クラッシュ）の教訓を踏まえ、ViewModel への代入前にイベントが走っても安全な null ガードを維持する。

---

## 6. 本計画書のスコープ外（別タスクとして継続管理）

1. **実際のリサイズ・移動操作を伴う実機再現テスト**:
   * 本計画のコード改修完了後、実機検証（実機CP4）の最終確認として実施する。
2. **`IsInitialShow` デッドコードの要否整理・除去判断**:
   * 動作に実害がないため、UIリファクタリング時に別途判断する。
3. **🟡 IUdpServerService の ControlService への実接続（技術的負債の解消）**:
   - **現状分析**: `ControlService.cs` は DI コンテナ登録済みの `IUdpServerService`（`UdpServerService`）を使わず、独自の生 `UdpServer` フィールド（`_udpServer`）を直接生成・起動・パケット送出している。設定値（Port/ListenAddress）は `Global` から正しく読んでおり実害（データ破壊等）はないが、DIサービスが未接続のまま死コード化している。
   - **作業量・リスク見積もり**:
     - 改修範囲: `ControlService.cs`（3000行超コア巨大ファイル）、`IUdpServerService.cs`、`UdpServerService.cs`、テスト
     - 改修内容: コントローラー入力パケット送出・ライフサイクル委譲、Cemuhookプロトコル対応クライアント（Dolphin, Cemu等）によるジャイロ・遅延の実機検証
     - 改修規模: 約 150〜250 行 / 推定工数: 約 2〜3 人日
     - リスク: 通信 Hot Path 改変によるジャイロ入力レイテンシ・パケットドロップの回帰リスク
   - **判定**: **本Step14のスコープとしては「過大」**
   - **方針**: UI フォーム設定・カラム幅の SSOT 統一（Issue 6）と責務が大きく異なるため、本 Step 14 には含めず、独立した通信系リファクタリングタスクとして継続管理する。

---

## 7. 完了条件（完全統合版）

1. `IEnvironmentService`, `IProfileSettingsService`, `AppNotificationService` から孤立 private field が完全に排除されていること。
2. 設定アクセスの窓口が `IAppSettingsService` および委譲型 `INotificationService` に一本化され、すべて単一の `BackingStore`（`m_Config`）と連動すること。
3. `SettingsViewModel.cs` および `ProfileEditor.xaml.cs` から対象設定の `Global.Xxx` 直参照が排除され、DI サービス経由に統一されていること。
4. 単体テストがすべてクリーンに PASS すること。
5. 実機起動・終了テストにおいて、ウィンドウサイズ・位置・カラム幅・通知設定が `Profiles.xml` に正常に保存・復元されること。
6. `NotificationServiceTests.cs` がクリーンに PASS し、設定変更が `Global` へ正しく波及することがテストで証明されていること。

---

## 8. 進行ルール（.github/copilot-instructions.md準拠）

* 本個別計画書に記載されたマイクロタスク単位で順次進める。
* 巨大ファイル編集時はピンポイント編集ルール（§3.2）を徹底し、不要な空白・改行差分を一切混入させない。
* 成果物スクリプトは `.github/PowerShell-script-generation-rules-for-deliverables.md` に従い、PowerShell 形式で安全に提供する。

---

## 9. 監査経緯と設計判断の記録

### 9.1 2026-09-10 監査: IProfileSettingsService の孤立5プロパティ発見と対応
- `IProfileSettingsService` にカラム幅5プロパティ（`ProfileEditorLeftWidth` 等）が `Global` 非連動の独自フィールドとして存在することを発見。
- 選択肢A（`IAppSettingsService` への集約・SSOT統一）を採用し、タスク(a)-4, (a)-5 として実施・完了した。

### 9.2 2026-09-11 監査: 全設定項目監査による孤立バグ是正と IUdpServerService の見積もり
- `Profiles.xml` 全約70項目の横断監査を実施。
- 🔴 `INotificationService`（`AppNotificationService`）の孤立バグを発見 → 案A準拠で是正スコープに追加（タスク(a)-6, (a)-7）。
- 🟡 `IUdpServerService` の未接続を発見 → 作業量見積もり（約2〜3人日）の結果、本Step14としては過大と判定し、§6 スコープ外課題に登録。
- ※ 全タスクの詳細・進捗管理は **§4 マイクロタスク breakdown** に完全統合済み。

---

## 10. 実機検証計画（本Stepの完了条件に含める）

### 10.1 テスト項目
1. **ウィンドウ幾何情報の永続化確認**:
   - メインウィンドウをリサイズ・移動後にアプリを終了し、`Profiles.xml` の `FormWidth`, `FormHeight`, `FormLocationX`, `FormLocationY` に値が反映されていることを確認。
   - 再起動時にそのサイズ・位置で復元されることを確認。
2. **プロファイル編集画面のカラム幅永続化確認**:
   - プロファイル編集画面の左右スプリッターおよび Special Action の各カラム幅を変更して終了し、`Profiles.xml` への保存を確認。
3. **通知設定の永続化・動作確認**:
   - 設定画面で通知トグルを切り替えて終了し、`Profiles.xml`（`Notifications`）への保存を確認。
   - 通知が設定通りに発火/抑制されることを確認。

### 10.2 実施タイミングと記録
- 全マイクロタスク（フェーズA・B）完了後、実機テスト（実機CP4）の最終確認として実施。
- 結果は `Phase5-Step14-RealDevice-Investigation-and-Fix-Report.md` に記録する。
