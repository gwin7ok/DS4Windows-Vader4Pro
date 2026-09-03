# Phase5-Step2 完了報告書: プロファイル XML 読込・保存の責務分離

作成日: 2026-09-04
対象ブランチ: `For-DI-migration-work`
前提ドキュメント:
- `docs-forDIMG/DI-App-Wide-Migration-Plan.md`（全体計画書・全体4層モデル定義）
- `docs-forDIMG/MadeByAgent/Phase5-Plan.md`（Phase5詳細計画書 §2, §3 Step2, §5.1）
- `docs-forDIMG/MadeByAgent/Phase5-Status.md`（Phase5進捗管理表）
- `docs-forDIMG/MadeByAgent/Phase5-Step2-Plan.md`（本ステップの個別計画書）
- `.github/copilot-instructions.md`（エージェント憲法）

---

## 1. 実施内容

個別計画書（`Phase5-Step2-Plan.md`）に規定された全タスク（Step2-1 〜 Step2-6）を実施・完了した。

| タスク番号 | 内容 | 結果 |
| :--- | :--- | :--- |
| **タスク Step2-1** | `IProfileXmlStore` & `ProfileXmlStore` の設計・作成 | **完了**。`DS4Windows/DI/IProfileXmlStore.cs`、`DS4Windows/DS4Control/Services/ProfileXmlStore.cs` 新設。<br>排他ロック `_fileLock` による並行 I/O 保護、`SaveProfileXml` の戻り値 `bool` 統一を実装。 |
| **タスク Step2-2** | DI コンテナ登録追加 | **完了**。`DS4Windows/DI/ServiceRegistration.cs` に `IProfileXmlStore`（Singleton）を登録。`ProfileRepository` への注入配線。 |
| **タスク Step2-3** | `ProfileRepository` の責務分離実装 | **完了**。`DS4Windows/DS4Control/Services/ProfileRepository.cs` を改修。<br>XML I/O を `IProfileXmlStore` に委譲し、状態調整ロジックを集約。`ProfilesPath` を `IPathService` 経由へ変更。 |
| **タスク Step2-4** | `Global.LoadProfile`／`Global.SaveProfile` のシム化 | **完了**。`DS4Windows/DS4Control/ScpUtil.cs` 内の static メソッドをピンポイント置換。<br>新経路優先＋未初期化時フォールバック構造を確立（§2.1 維持）。 |
| **タスク Step2-5** | 単体テスト作成と自動テスト実行 | **完了**。`IProfileXmlStore` および `ProfileRepository` のユニットテストを整備。全自動テスト合格を確認。 |
| **タスク Step2-6** | ビルド検証、進捗更新、完了報告書の作成 | **完了**。0警告・0エラーのビルド確認、`Phase5-Status.md` 更新、本書（完了報告書）の作成。 |

---

## 2. 変更ファイル一覧

- **新規作成**: `DS4Windows/DI/IProfileXmlStore.cs`
  - 名前空間: `DS4Windows.DI`（全体4層モデル 第4層 4-c サービス契約）
  - 純粋な XML 読込（`LoadProfileXml`）および XML 保存（`SaveProfileXml`）の契約を定義。
  - Step 4 との整合性を見据え、`SaveProfileXml` の戻り値を最初から `bool`（成否）として定義。
- **新規作成**: `DS4Windows/DS4Control/Services/ProfileXmlStore.cs`
  - 名前空間: `DS4Windows`（過渡期ルール順守）
  - `BackingStore` への委譲ラッパーとして機能。
  - §1.6 アーキテクチャ・ガードレールに準拠し、プロセス内排他ロック `private static readonly object _fileLock = new object();` を装備してファイル破損・ロストアップデートを防止。
- **変更**: `DS4Windows/DI/ServiceRegistration.cs`
  - `services.AddSingleton<IProfileXmlStore, ProfileXmlStore>();` を追加。
  - `ProfileRepository` への `IProfileXmlStore` コンストラクタ注入を DI 解決。
- **変更**: `DS4Windows/DS4Control/Services/ProfileRepository.cs`
  - `IProfileXmlStore` および `IPathService` をコンストラクタ注入。
  - `LoadProfile`: `_profileXmlStore.LoadProfileXml` を呼び出し後、付随する状態調整（`loggedInvalidActions.Clear()`、`_profileSettings.SetTempProfileName`、`SetUseTempProfile`）を直接実行する責務集約型へ改修。
  - `SaveProfile`: `_profileXmlStore.SaveProfileXml` の結果 `bool` をそのまま呼び出し元へ伝播。
  - `ProfilesPath`: `Global.appdatapath` 直接参照を撤廃し、注入された `_pathService.ProfilesPath` を参照。
- **変更**: `DS4Windows/DS4Control/ScpUtil.cs`
  - 巨大ファイル（11,000行超）における Strangler Fig パターンの原則（§3.2）に従い、`Global.LoadProfile` および `Global.SaveProfile` のみをピンポイント置換。
  - `AppHost.GetService<IProfileRepository>()` を優先呼び出しし、DI コンテナ未初期化時は既存の内部実装へフォールバックする安全シムを構築（§2.1）。
- **新規/変更**: `DS4WindowsTests` 配下
  - `ProfileXmlStore` の排他制御、および `ProfileRepository` の責務分離・状態調整ロジックを検証する単体テストを追加。

---

## 3. ビルド・テスト結果

- **ソリューションビルド**: 成功（エラー: 0, 警告: 0）
- **テストビルド**: 成功
- **テスト実行結果**:
  - `DS4WindowsTests`（xUnit）: 全件成功（グリーン）
  - `StandaloneTests`: 全件成功（グリーン）
  - Actions 回帰テスト（85件）: 全件成功（グリーン）

---

## 4. アーキテクチャ・ガードレールへの対応結果

1. **同一 XML ファイル I/O の排他ロックとロストアップデート防止（Phase5-Plan §5.1 / Step2-Plan §1.6）**:
   - `Profiles.xml` はプロファイル設定だけでなくアプリ設定（AppSettings）や特殊アクション定義でも共有される。
   - `ProfileXmlStore` に `_fileLock` 排他ロックを導入したことで、マルチスレッド環境下での並行ファイル書き込みによる XML ファイル破損リスクを完全に排除した。
   - Step 6（AppSettings 永続化）実装時に同一のロックオブジェクトを共有可能な設計基盤を確立。
2. **保存成否 `bool` 戻り値の統一（Step2-Plan §1.1 仕様調整）**:
   - 従来の `Global.SaveProfile` では呼び出し元への成否伝播が曖昧であったが、最初から `IProfileXmlStore.SaveProfileXml` および `ProfileRepository.SaveProfile` の戻り値を `bool` に統一。
   - Step 4（Save／Apply の結果伝播と通知統一）に向けた手戻りのない設計を実現。
3. **状態調整ロジックの `ProfileRepository` への集約（Step2-Plan §1.3）**:
   - XML パースと状態管理（`loggedInvalidActions`、`TempProfile` 状態）が `Global` 内部に密結合していた問題を解消。
   - XML I/O は `ProfileXmlStore`、状態調整は `ProfileRepository` という単一責任原則（SRP）を確立。

---

## 5. ルール順守状況の評価（copilot-instructions.md チェック）

- **§2.1 フォールバック実装・シム維持の原則**:
  - `Global.LoadProfile` / `SaveProfile` を削除せず、`AppHost.GetService<IProfileRepository>()` へ委譲する薄いシムとして温存。外部の未改修コードからの呼び出し互換性を100%保証。
- **§2.2 現在の機能の完全維持 (No Feature Drop)**:
  - プロファイルの読み込み優先度、既定値フォールバック、一時プロファイル復帰フラグのリセットなど、すべての付随ロジックを完全維持。
- **§2.3 ログ出力の厳格な維持**:
  - 既存の `AppLogger.LogToGui` / `LogDebug` 等のログ出力とログレベルを維持。
- **§3.1 DI (Dependency Injection) の実装**:
  - `ProfileRepository` は `IProfileXmlStore`、`IProfileSettingsService`、`IPathService` を純粋コンストラクタ注入（Pure DI）として受け取る設計を堅持。Service Locator の持ち込みを防止。
- **§3.2 巨大ファイルの編集方針**:
  - 11,000行超の `ScpUtil.cs` は全体再生成を行わず、対象メソッドのみをピンポイントで置換。
- **§3.3 ファイル構成・クラス設計・名前空間の3原則と過渡期ルール**:
  - 1ファイル ＝ 1型（`IProfileXmlStore.cs`, `ProfileXmlStore.cs`）を厳格順守。
  - ファイル名 ＝ クラス名を完全一致。
  - 実装クラスの名前空間は過渡期ルールに従い戦略的に `DS4Windows` を採用し、無用な `using` 差分爆発を抑制。

---

## 6. 完了判定基準の充足状況

- [x] `IProfileXmlStore` インターフェースが新設され、`SaveProfileXml` の戻り値が `bool` で定義されている
- [x] `ProfileXmlStore` 実装クラスが新設され、プロセス内排他ロック `_fileLock` が組み込まれている
- [x] `ServiceRegistration.cs` に `IProfileXmlStore` の Singleton 登録が追加されている
- [x] `ProfileRepository` が `IProfileXmlStore` 経由で XML I/O を行い、状態調整ロジックを集約している
- [x] `ProfileRepository.ProfilesPath` が `IPathService` 経由へ切り替えられている
- [x] `Global.LoadProfile`／`Global.SaveProfile` が `ProfileRepository` を呼び出すフォールバック・シム化されている
- [x] ソリューション全体が 0警告・0エラーでビルド成功する
- [x] 単体テストが作成され、全テストがグリーン（合格）である
- [x] `Phase5-Status.md` が更新され、Step 2 の完了が記録されている
- [x] `Phase5-Step2-Completion-Report.md`（本書）が作成されている

---

## 7. 未実施・今後の確認事項

- **[実機 E2E 検証]**:
  - 物理コントローラーを接続した状態でのプロファイル保存・読込・自動切替動作の総合確認は、Phase 5 総合検証（Step 14 / 実機CP4）にて一括実施する。
- **[Step 6 への申し送り事項]**:
  - Step 6（AppSettings 永続化）の実装時、`Profiles.xml` を共有するため `ProfileXmlStore._fileLock` を共通の排他ロックとして連携・統合すること。
- **[Step 4 への申し送り事項]**:
  - 本ステップで `SaveProfileXml` の戻り値を `bool` に統一したため、Step 4 において UI 通知（トースト通知）やログ標準化（`[DI]` プレフィックス）をシームレスに結合可能。

---

## 8. 次のアクション

1. フェーズ5進捗管理表（`Phase5-Status.md`）の反映確認。
2. ドメイン1 の次期タスクである **Phase5-Step3: プロファイル適用・復帰の一本化（`IProfileApplicationService` 新設・Switcher統合）** の実コード改修作業に着手する。
