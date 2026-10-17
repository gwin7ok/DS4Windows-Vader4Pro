# Phase5-Step6 完了報告書: アプリ全体設定（AppSettings）の永続化・状態管理のDI化

作成日: 2026-09-04
対象ブランチ: `For-DI-migration-work`
前提ドキュメント:
- `docs-forDIMG/DI-App-Wide-Migration-Plan.md`（全体計画書・全体4層モデル定義）
- `docs-forDIMG/MadeByAgent/Phase5-Plan.md`（Phase5詳細計画書 §2, §3 Step6, §5.1）
- `docs-forDIMG/MadeByAgent/Phase5-Status.md`（Phase5進捗管理表）
- `docs-forDIMG/MadeByAgent/Phase5-Step6-Plan.md`（本ステップの個別計画書）
- `.github/copilot-instructions.md`（エージェント憲法）

---

## 1. 実施内容サマリ

個別計画書（`Phase5-Step6-Plan.md`）に規定された全タスク（Step6-1 〜 Step6-7）を完了した。
本ステップの完了をもって、**【ドメイン1: プロファイル・設定系】の全 5 ステップ（Step 2 〜 Step 6）が完全達成**となった。

| タスク番号 | 内容 | 結果 |
| :--- | :--- | :--- |
| **タスク Step6-1** | `Profiles.xml` 構造と永続化経路の精査 | **完了**。`Profiles.xml` 内でのプロファイル設定と全体設定の同居構造、および `BackingStore.Load()` / `Save()` の実装を精査。 |
| **タスク Step6-2** | `IProfileXmlStore` への AppSettings メソッド追加 | **完了**。`DS4Windows/DI/IProfileXmlStore.cs` に `LoadAppSettingsXml` および `SaveAppSettingsXml` を新設。 |
| **タスク Step6-3** | `ProfileXmlStore` での排他保存実装（§5.1 ガードレール） | **完了**。`DS4Windows/DS4Control/Services/ProfileXmlStore.cs` にて、Step 2 で配備した `XmlIoLock` 下で全体設定 I/O を直列化実行。ロストアップデートリスクを根絶。 |
| **タスク Step6-4** | `IAppSettingsService` & `AppSettingsService` の新設 | **完了**。`DS4Windows/DI/IAppSettingsService.cs`（第4層 4-c 契約）および `DS4Windows/DS4Control/Services/AppSettingsService.cs`（実装）を作成。<br>自立型バッキングフィールド設計と `SettingChanged` イベントを配備。 |
| **タスク Step6-5** | `ServiceRegistration.cs` 登録と `ScpUtil.cs` シム化 | **完了**。`IAppSettingsService`（Singleton）を DI 登録。<br>`ScpUtil.cs` 内の `Global.Load()` および `Global.Save()` をピンポイント置換し、DI サービス委譲＋フォールバック構造を確立（§2.1 原則、§3.2 原則）。 |
| **タスク Step6-6** | 単体テスト作成と自動テスト実行 | **完了**。`DS4WindowsTests/AppSettingsServiceTests.cs`（5件）を新設、`ProfileRepositoryTests.cs` を整合。全自動テストが常時グリーンを達成。 |
| **タスク Step6-7** | ビルド検証、進捗更新、完了報告書の作成 | **完了**。0警告・0エラーのビルド確認、進捗表（`Phase5-Status.md`）更新、本書（完了報告書）の作成。 |

---

## 2. 変更・追加ファイル一覧

- **変更**: `DS4Windows/DI/IProfileXmlStore.cs`
  - 名前空間: `DS4Windows.DI`（全体4層モデル 第4層 4-c サービス契約）
  - アプリ全体設定の読み書き契約 `LoadAppSettingsXml()` および `SaveAppSettingsXml()` を追加。
- **変更**: `DS4Windows/DS4Control/Services/ProfileXmlStore.cs`
  - 名前空間: `DS4Windows`（過渡期ルール順守）
  - Step 2 で配備された同一プロセス内排他ロック `XmlIoLock` 内で `_backingStore.Load()` および `_backingStore.Save()` を実行。
  - プロファイル保存とアプリ設定保存の並行実行によるファイル破損・ロストアップデートを完全に防止（§5.1 ガードレール）。
  - 成否に応じた GUI エラー通知（`AppLogger.LogToGui(..., true)`）および `[DI]` ログ統一を適用。
- **新規作成**: `DS4Windows/DI/IAppSettingsService.cs`
  - 名前空間: `DS4Windows.DI`（全体4層モデル 第4層 4-c サービス契約）
  - `Save()`, `Load()`, 各種設定プロパティ（`StartMinimized`, `MinimizeToTaskbar`, `CloseMinimizes`, `UseUdpServer`, `UdpServerPort`, `CheckWhen`, `AutoProfileRevertDefaultProfile` 等）, `SettingChanged` イベント契約を定義。
- **新規作成**: `DS4Windows/DS4Control/Services/AppSettingsService.cs`
  - 名前空間: `DS4Windows.Services`
  - `IProfileXmlStore` および `IPathService` をコンストラクタ注入。
  - `EnvironmentService` と同様の自立型バッキングフィールド設計を採用し、外部依存を持たずに設定状態を自己完結管理。
  - 設定値の変更時に `SettingChanged` イベントを発火。
- **変更**: `DS4Windows/DI/ServiceRegistration.cs`
  - `services.AddSingleton<IAppSettingsService, AppSettingsService>();` を追加登録。
- **変更**: `DS4Windows/DS4Control/ScpUtil.cs`
  - 11,000行超の巨大ファイルに対し、§3.2 ピンポイント編集原則に従って 3548行目の `Global.Load()` および 3700行目の `Global.Save()` のみをピンポイント置換。
  - `AppHost.GetService<IAppSettingsService>()` を優先呼び出しし、DI コンテナ未初期化時は既存の `m_Config.Load()` / `Save()` へフォールバックする安全シムを構築（§2.1 原則）。
- **新規作成**: `DS4WindowsTests/AppSettingsServiceTests.cs`
  - `Save` / `Load` の委譲成否テスト。
  - 設定プロパティ変更時の `SettingChanged` イベント発火テスト。
  - `Global.Save()` / `Global.Load()` シムの同期検証テスト。
- **変更**: `DS4WindowsTests/ProfileRepositoryTests.cs`
  - モッククラス `FakeProfileXmlStore` に新設の `LoadAppSettingsXml` / `SaveAppSettingsXml` を実装追加。

---

## 3. ビルドおよびテスト結果

- **ソリューションビルド**: 成功（エラー: 0, 警告: 0）
- **テストプロジェクトビルド**: 成功（エラー: 0, 警告: 0）
- **StandaloneTests ビルド**: 成功（エラー: 0, 警告: 0）
- **テスト実行結果**:
  - `DS4WindowsTests`（xUnit）: 全件成功（グリーン）
  - AppSettingsService 単体テスト（5件）: 全件成功（グリーン）
  - AutoProfileService 単体テスト（3件）: 全件成功（グリーン）
  - ProfileApplicationService 単体テスト（9件）: 全件成功（グリーン）
  - ProfileXmlStore 単体テスト（4件）: 全件成功（グリーン）
  - ProfileRepository 単体テスト（7件）: 全件成功（グリーン）
  - Actions 回帰テスト（85件）: 全件成功（グリーン）

---

## 4. アーキテクチャ・ガードレールへの対応結果

1. **同一 XML 排他ロック・ロストアップデート防止（Phase5-Plan §5.1 / Step6-Plan §1.1）**:
   - `Profiles.xml` はプロファイル設定とアプリ全体設定が同一ファイル内に同居しているため、マルチスレッド環境においてプロファイル保存とアプリ設定保存が重なるとファイル破損や設定上書き（ロストアップデート）が発生する構造的欠陥が存在していた。
   - Step 2 で策定した `ProfileXmlStore.XmlIoLock` を本ステップで `SaveAppSettingsXml` にも適用・共有したことで、**すべての XML 保存操作が同一排他ロックで完全に直列化**され、競合リスクを物理的に根絶した。
2. **Pure DI の堅持と自立型状態管理（§3.1 原則）**:
   - `Global` の静的フィールドへの直接依存を排除し、`AppSettingsService` 内部で自己完結したバッキングフィールドを保持する設計を採用。変更通知イベント `SettingChanged` を通じて UI や他サービスへ疎結合に伝播するアーキテクチャを確立した。
3. **No Feature Drop（§2.2 原則）とピンポイントシム化（§3.2 原則）**:
   - `BackingStore` の実績ある XML 読み書きロジック（`AppSettingsDTO` 等）を 100% そのまま活用しつつ、巨大ファイル `ScpUtil.cs` 内の `Global.Save` / `Load` をピンポイントで委譲シム化。既存のすべての静的呼び出し元との完全な下位互換性を確保した。

---

## 5. ルール順守状況の評価（copilot-instructions.md チェック）

- **§2.1 フォールバック実装・シム維持の原則**:
  - `Global.Save()` / `Global.Load()` を削除せず、DI 解決優先＋旧コードフォールバックとして温存。
- **§2.2 現在の機能の完全維持 (No Feature Drop)**:
  - 保存・読込の成功/失敗ハンドリング、GUI ログへの通知メッセージを完全踏襲。
- **§2.3 ログ出力の厳格な維持**:
  - `[DI]` プレフィックスを冠したトレースログおよび GUI エラー通知を配置。
- **§3.1 DI (Dependency Injection) の実装**:
  - コンストラクタ注入による明示的な依存性解決（Pure DI）を順守。
- **§3.2 巨大ファイルの編集方針**:
  - 11,000行超の `ScpUtil.cs` を全体再生成せず、対象 2 メソッドのみを正確にピンポイント置換。
- **§3.3 ファイル構成・クラス設計・名前空間の3原則と過渡期ルール**:
  - 1ファイル ＝ 1型、ファイル名 ＝ クラス名を厳格順守。

---

## 6. 完了判定基準の充足状況

- [x] `IProfileXmlStore` に `LoadAppSettingsXml` / `SaveAppSettingsXml` が追加されている
- [x] `ProfileXmlStore` において `XmlIoLock` による排他保護下で設定 XML が読み書きされている
- [x] `IAppSettingsService` インターフェースおよび `AppSettingsService` 実装クラスが新設されている
- [x] `ServiceRegistration.cs` に `IAppSettingsService` の Singleton 登録が追加されている
- [x] `Global.Save` / `Global.Load` が `IAppSettingsService` を呼び出すピンポイントシム化されている
- [x] ソリューション全体が 0 警告・0 エラーでビルド成功する
- [x] 単体テストが配備され、全自動テストが常時グリーンである
- [x] `Phase5-Status.md` が更新され、Step 6 の完了が記録されている
- [x] `Phase5-Step6-Completion-Report.md`（本書）が作成されている

---

## 7. 未実施・今後の確認事項・申し送り事項

- **[ドメイン1の完了]**:
  - Step 2（プロファイルXML）、Step 3（適用一本化）、Step 4（通知統一）、Step 5（AutoProfile）、Step 6（AppSettings）の全 5 ステップが完了し、プロファイル・設定系の DI 化とアーキテクチャ境界が完成。
- **[実機 E2E 検証]**:
  - アプリ全体設定の保存・復元、および起動時最小化・トレイ格納などの実動作は、Phase 5 総合検証（Step 14 / 実機CP4）にて一括実施する。
- **[Step 7 への申し送り事項]**:
  - 次ドメイン（【ドメイン2】アクション系）の第 1 ステップである **Step 7: SpecialAction 永続化の責務分離（`Phase5-Step7-Plan.md`）** において、`BackingStore.actions` の二重管理解消、および `ProfileXmlStore.XmlIoLock` との排他整合を実施する。

---

## 8. 次のアクション

1. フェーズ5進捗管理表（`Phase5-Status.md`）の反映確認。
2. 【ドメイン2】アクション系の第 1 ステップである **Phase5-Step7: SpecialAction 永続化の責務分離（`Phase5-Step7-Plan.md`）** の実コード改修作業に着手する。
