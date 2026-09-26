# Phase5-Step4 完了報告書: Save／Apply の操作結果と通知の統一

作成日: 2026-09-04
対象ブランチ: `For-DI-migration-work`
前提ドキュメント:
- `docs-forDIMG/DI-App-Wide-Migration-Plan.md`（全体計画書・全体4層モデル定義）
- `docs-forDIMG/MadeByAgent/Phase5-Plan.md`（Phase5詳細計画書 §2, §3 Step4）
- `docs-forDIMG/MadeByAgent/Phase5-Status.md`（Phase5進捗管理表）
- `docs-forDIMG/MadeByAgent/Phase5-Step4-Plan.md`（本ステップの個別計画書）
- `.github/copilot-instructions.md`（エージェント憲法）

---

## 1. 実施内容サマリ

個別計画書（`Phase5-Step4-Plan.md`）に規定された全タスク（Step4-1 〜 Step4-5）を完了した。

| タスク番号 | 内容 | 結果 |
| :--- | :--- | :--- |
| **タスク Step4-1** | Step2／Step3 の前提状況確認 | **完了**。Step 2（`SaveProfileXml` の `bool` 成否返却）および Step 3（`ApplyProfile` の新設・Halt保護）の接続点を確認。 |
| **タスク Step4-2** | 保存系成否伝播とログの実装 | **完了**。`DS4Windows/DS4Control/Services/ProfileRepository.cs` を改修。<br>`SaveProfileXml` の結果に応じた GUI エラー通知（`AppLogger.LogToGui(..., true)`）および `[DI]` ログ統一を実装。 |
| **タスク Step4-3** | 通知抑制自動解決の実装 | **完了**。`DS4Windows/DI/IProfileApplicationService.cs` および `DS4Windows/DS4Control/Services/ProfileApplicationService.cs` を改修。<br>`bool? displayNotification = null`（Nullable）を導入し、呼び出し元の過剰な結合を増やさずにユーザー通知設定をサービス内部で自動解決（通知オフ設定無視バグを解消）。 |
| **タスク Step4-4** | 単体テスト作成と自動テスト実行 | **完了**。`DS4WindowsTests/ProfileApplicationServiceTests.cs` に自動解決および明示指定の検証テストを追加。Actions テスト全100件が常時グリーンを達成。 |
| **タスク Step4-5** | ビルド検証、進捗更新、完了報告書の作成 | **完了**。0警告・0エラーのビルド確認、進捗表（`Phase5-Status.md`）更新、本書（完了報告書）の作成。 |

---

## 2. 変更・追加ファイル一覧

- **変更**: `DS4Windows/DS4Control/Services/ProfileRepository.cs`
  - `SaveProfile`: `_profileXmlStore.SaveProfileXml` の戻り値 `bool` を厳密に評価。
  - パス不正時および保存失敗時に `AppLogger.LogToGui($"Failed to save profile ...", true)` で警告フラグ付き GUI ログを出力。
  - 成功・失敗・例外の各分岐において `[DI]` プレフィックスを冠したトレースログを統一。
- **変更**: `DS4Windows/DI/IProfileApplicationService.cs`
  - 名前空間: `DS4Windows.DI`（全体4層モデル 第4層 4-c サービス契約）
  - `ApplyProfile` の第7引数を `bool displayNotification = true` から **`bool? displayNotification = null`**（Nullable）へと拡張。
- **変更**: `DS4Windows/DS4Control/Services/ProfileApplicationService.cs`
  - `ApplyProfile`: `bool shouldDisplay = displayNotification ?? (_profileSettings?.ProfileChangedNotification ?? true);` による自動解決ロジックを実装。
  - 呼び出し元が通知引数を省略（既定値 `null`）した場合、DI 注入された `_profileSettings` からユーザーの通知オン/オフ設定を自動適用。
  - 明示的に `true` または `false` を渡した場合は指定通りの挙動を保証。
  - `[DI]` トレースログに `displayNotification` の解決結果を出力。
- **変更**: `DS4WindowsTests/ProfileApplicationServiceTests.cs`
  - `ApplyProfile_NullDisplayNotification_ResolvesFromSettings`: 引数省略時の自動解決が正常動作することを検証。
  - `ApplyProfile_ExplicitDisplayNotification_AcceptsExplicitValue`: 明示指定時の上書き動作を検証。
  - `AppHost.CreateHost()` および `PathService` による完全自己充足的なテスト環境初期化を適用。

---

## 3. ビルドおよびテスト結果

- **ソリューションビルド**: 成功（エラー: 0, 警告: 0）
- **テストプロジェクトビルド**: 成功（エラー: 0, 警告: 0）
- **StandaloneTests ビルド**: 成功（エラー: 0, 警告: 0）
- **テスト実行結果**:
  - `DS4WindowsTests`（xUnit）: 全100件成功（グリーン）
  - Actions 回帰テスト（85件）: 全件成功（グリーン）
  - ProfileApplicationService 単体テスト（9件）: 全件成功（グリーン）
  - ProfileXmlStore 単体テスト（4件）: 全件成功（グリーン）
  - ProfileRepository 単体テスト（7件）: 全件成功（グリーン）

---

## 4. アーキテクチャ・ガードレールへの対応結果

1. **保存成否の完全な可視化と伝播（Step4-Plan §1.1）**:
   - 従来は `Global.SaveProfile` が例外や I/O 失敗を内部で握りつぶして呼び出し元へ通知しない不透明さがあった。
   - Step 2 で導入された `bool` 戻り値を Step 4 で GUI ログおよびトレースログに完全に接続し、ユーザーおよび呼び出し元へ確実に成否を伝播する構造を確立した。
2. **通知抑制自動解決によるバグ解消と Pure DI 順守（Step4-Plan §1.2）**:
   - `DefaultProfileSwitcher` など複数のプロファイル切り替え経路において、ユーザーの「ProfileChangedNotification（通知オフ）」設定が反映されず常にトースト通知が出てしまう既存バグが存在した。
   - `IProfileApplicationService.ApplyProfile` を Nullable 引数とし、サービス内部で一元解決する設計を採用したことで、呼び出し元に不要な依存性（`IProfileSettingsService`）を持ち込ませることなく（Pure DI 順守）、バグを完全に解消した。
3. **`[DI]` ログプレフィックスの統一（Step4-Plan §1.3）**:
   - `ProfileRepository.SaveProfile` および `ProfileApplicationService.ApplyProfile` のログ出力を標準の `[DI]` プレフィックスに統一し、保守性と診断性を向上させた。

---

## 5. ルール順守状況の評価（copilot-instructions.md チェック）

- **§2.1 フォールバック実装・シム維持の原則**:
  - 既存の引数呼び出し（省略時）との完全な下位互換性を保持。
- **§2.2 現在の機能の完全維持 (No Feature Drop)**:
  - 成功時の動作、例外発生時の安全なリカバリ、GUI ログへの出力メッセージ形式を完全踏襲。
- **§2.3 ログ出力の厳格な維持**:
  - GUI 向けのエラー表示（`warning: true`）とトレース向け詳細ログの両面を維持・強化。
- **§3.1 DI (Dependency Injection) の実装**:
  - インターフェースの引数設計を工夫し、不要な依存関係の拡散を防止。

---

## 6. 完了判定基準の充足状況

- [x] `ProfileRepository.SaveProfile` が成否に応じた GUI エラー通知および `[DI]` トレースログを出力している
- [x] `IProfileApplicationService.ApplyProfile` の引数が `bool? displayNotification = null` に拡張されている
- [x] `ProfileApplicationService.ApplyProfile` 内部で `_profileSettings.ProfileChangedNotification` が自動解決されている
- [x] ソリューション全体が 0 警告・0 エラーでビルド成功する
- [x] 単体テストが配備され、全自動テスト（100件）が常時グリーンである
- [x] `Phase5-Status.md` が更新され、Step 4 の完了が記録されている
- [x] `Phase5-Step4-Completion-Report.md`（本書）が作成されている

---

## 7. 未実施・今後の確認事項・申し送り事項

- **[実機 E2E 検証]**:
  - 実際の通知ポップアップ（WPFトースト通知）およびステータスバー表示の連動確認は、Phase 5 総合検証（Step 14 / 実機CP4）にて一括実施する。
- **[Step 5 への申し送り事項]**:
  - 次ステップ（Step 5: AutoProfile）において、自動プロファイル切替処理からプロファイルを適用する際、`displayNotification: null` で `ApplyProfile` を呼ぶだけで、ユーザーの通知設定が自動適用される。
- **[Step 6 への申し送り事項]**:
  - Step 6（AppSettingsService 永続化）においても、本ステップで確立した保存成否伝播・エラー通知パターン（`AppLogger.LogToGui(..., true)`）を踏襲する。

---

## 8. 次のアクション

1. フェーズ5進捗管理表（`Phase5-Status.md`）の反映確認。
2. ドメイン1 の第4ステップである **Phase5-Step5: AutoProfile（自動プロファイル切替）の自律実行系DI化（`Phase5-Step5-Plan.md`）** の実コード改修作業に着手する。
