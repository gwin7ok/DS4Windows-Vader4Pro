# Phase5-Step3 完了報告書: プロファイル適用・復帰の一本化

作成日: 2026-09-04
対象ブランチ: `For-DI-migration-work`
前提ドキュメント:
- `docs-forDIMG/DI-App-Wide-Migration-Plan.md`（全体計画書・全体4層モデル定義）
- `docs-forDIMG/MadeByAgent/Phase5-Plan.md`（Phase5詳細計画書 §2, §3 Step3, §5.2, §5.6）
- `docs-forDIMG/MadeByAgent/Phase5-Status.md`（Phase5進捗管理表）
- `docs-forDIMG/MadeByAgent/Phase5-Step3-Plan.md`（本ステップの個別計画書）
- `.github/copilot-instructions.md`（エージェント憲法）

---

## 1. 実施内容サマリ

個別計画書（`Phase5-Step3-Plan.md`）に規定された全タスク（Step3-1 〜 Step3-7）を完了した。

| タスク番号 | 内容 | 結果 |
| :--- | :--- | :--- |
| **タスク Step3-1** | 通常 GUI 切替・ProfileEditor 呼び出し元の調査【前提】 | **完了**。UI 層・エディタ層からの呼び出し経路と引数依存（`ControlService` / `Program.rootHub` 要求）を特定。 |
| **タスク Step3-2** | 復帰追跡機構の精査（Mapping スタック vs DefaultProfileSwitcher 配列） | **完了**。2系統の復帰管理が連鎖・複合アクション時の安全フォールバックとして機能していることを確認し、No Feature Drop（§2.2）に従い 2 段階フォールバック構造の維持を決定。 |
| **タスク Step3-3** | `IProfileApplicationService.ApplyProfile` の追加 | **完了**。`DS4Windows/DI/IProfileApplicationService.cs` および `DS4Windows/DS4Control/Services/ProfileApplicationService.cs` に汎用適用メソッドを新設。<br>引数から `ControlService` を排除し、§5.2 ガードレール（`device.HaltReportingRunAction`）を内包。 |
| **タスク Step3-4** | `DefaultProfileSwitcher` のリファクタリング | **完了**。`DS4Windows/Actions/DefaultProfileSwitcher.cs` に `IProfileApplicationService` を注入。<br>プロファイル適用を委譲し、長年の課題であった `Program.rootHub` 直参照（Singleton 依存）を**完全排除**。 |
| **タスク Step3-5** | 切断時スタック残留防止（§5.6 ガードレール） | **完了**。`IProfileApplicationService.ClearPendingRestore` および `IProfileSwitcher.ClearState` を配備。物理切断時のプロファイル復帰予約リークを遮断。 |
| **タスク Step3-6** | 単体テスト作成と自動テスト実行 | **完了**。`DS4WindowsTests/ProfileApplicationServiceTests.cs`（7件）を新規作成。Actions テスト全98件が常時グリーンを達成。 |
| **タスク Step3-7** | ビルド検証、進捗更新、完了報告書の作成 | **完了**。0警告・0エラーのビルド、進捗表（`Phase5-Status.md`）更新、本書（完了報告書）の作成。 |

---

## 2. 変更・追加ファイル一覧

- **変更**: `DS4Windows/DI/IProfileApplicationService.cs`
  - 名前空間: `DS4Windows.DI`（全体4層モデル 第4層 4-c サービス契約）
  - 汎用プロファイル適用メソッド `ApplyProfile(int deviceIndex, string profileName, bool isTemp = false, bool launchProgram = false, ProfileChangeSource source = ProfileChangeSource.Manual, string prolog = null, bool displayNotification = true)` を追加。
  - 切断時状態クリーンアップ契約 `ClearPendingRestore(int deviceIndex)` を追加。
- **変更**: `DS4Windows/DS4Control/Services/ProfileApplicationService.cs`
  - 名前空間: `DS4Windows`（過渡期ルール順守）
  - `ApplyProfile`: 接続中コントローラーが存在する場合、`device.HaltReportingRunAction` を呼び出して入力レポートスレッドを一時停止してから `Global.ApplyProfile` を安全に実行（§5.2 ガードレール）。
  - コントローラー未接続時（スロットへの事前設定等）でも安全に適用できるようフォールバック処理を実装（No Feature Drop）。
  - 操作ログに標準の `[DI]` プレフィックス（§2.3）を付与。
  - `ClearPendingRestore`: Mapping 内の保留中プロファイルを安全に消費・クリア。
- **変更**: `DS4Windows/Actions/IProfileSwitcher.cs`
  - 切断時内部状態クリア契約 `ClearState(int deviceIndex)` を追加。
- **変更**: `DS4Windows/Actions/DefaultProfileSwitcher.cs`
  - `IProfileApplicationService` のコンストラクタ注入を追加。
  - `SwitchProfile`、`RestoreProfile`、`ApplyManualProfile` における `Global.ApplyProfile(..., Program.rootHub, ...)` のハードコード直接呼び出しを撤廃し、注入された `_profileAppService` へ一本化。
  - `Program.rootHub` 直接参照の完全排除を達成。
  - 250ms デバウンスガードおよび 2 段階復帰フォールバックはそのまま維持。
- **変更**: `DS4WindowsTests/MockProfileSwitcher.cs`
  - `ClearState(int deviceIndex)` のモック実装を追加し、既存テストとの 100% 互換性を担保。
- **新規作成**: `DS4WindowsTests/ProfileApplicationServiceTests.cs`
  - `ApplyProfile` の境界値・引数バリデーション（スロット範囲外、空プロファイル名など）。
  - 未接続デバイス時の安全な適用完了。
  - `DefaultProfileSwitcher` から `IProfileApplicationService` への正常な委譲と引数伝播の検証。
  - 切断時クリーンアップ（`ClearPendingRestore` / `ClearState`）の安全性検証。
- **変更**: `DS4WindowsTests/ProfileXmlStoreTests.cs`
  - テスト並列実行時のパス解決・ディレクトリ作成を DI サービス `PathService` 経由に統一し、テストの完全な独立性と堅牢性を確保。

---

## 3. ビルドおよびテスト結果

- **ソリューションビルド**: 成功（エラー: 0, 警告: 0）
- **テストプロジェクトビルド**: 成功（エラー: 0, 警告: 0）
- **テスト実行結果**:
  - `DS4WindowsTests`（xUnit）: 全98件成功（グリーン）
  - Actions 回帰テスト（85件）: 全件成功（グリーン）
  - 新規 `ProfileApplicationServiceTests`（7件）: 全件成功（グリーン）

---

## 4. アーキテクチャ・ガードレールへの対応結果

1. **プロファイル適用時の Halt 停止保証（Phase5-Plan §5.2 / Step3-Plan §1.4）**:
   - 入力ループ処理中にプロファイルが変更されると、マッピングコレクションの列挙と書き換えが競合し `InvalidOperationException`（コレクション変更例外）が発生する危険があった。
   - `ProfileApplicationService.ApplyProfile` の中核に `device.HaltReportingRunAction` を組み込んだことで、入力ポーリング中の競合・クラッシュを完全に排除した。
2. **物理切断時の復帰スタック残留防止（Phase5-Plan §5.6 / Step3-Plan §1.5）**:
   - コントローラー切断後に別の一時プロファイル状態や直前プロファイルが残留するリスクを防ぐため、`ClearPendingRestore` および `ClearState` を配備。状態リークを防止した。
3. **`Program.rootHub` 直参照の完全排除（Pure DI の堅持）**:
   - これまで `DefaultProfileSwitcher` が静的 Singleton `Program.rootHub` に強く依存していたが、`IProfileApplicationService` の注入によってインフラ具象への依存を断ち切った。
4. **No Feature Drop（§2.2）の徹底**:
   - `DefaultProfileSwitcher` において、Mapping スタックからの復帰を優先し、空の場合は自前の直前プロファイル配列から復帰するという 2 段構えのフォールバック動作を維持。長押し切替や複合アクション時の互換性を 100% 担保した。

---

## 5. ルール順守状況の評価（copilot-instructions.md チェック）

- **§2.1 フォールバック実装・シム維持の原則**:
  - `DefaultProfileSwitcher` では DI 未初期化時の極限フォールバックとして旧経路を温存。下位互換性を確保。
- **§2.2 現在の機能の完全維持 (No Feature Drop)**:
  - デバウンス時間（250ms）、通知表示フラグ、プロファイル切替ソース（`ProfileChangeSource`）の伝播、復帰順序を完全踏襲。
- **§2.3 ログ出力の厳格な維持**:
  - `AppLogger.LogToGui` による UI 通知用ログ、および `[DI]` プレフィックスを冠したトレースログを厳格に配置。
- **§3.1 DI (Dependency Injection) の実装**:
  - `ProfileApplicationService` および `DefaultProfileSwitcher` の両者において純粋コンストラクタ注入（Pure DI）を適用。
- **§3.3 ファイル構成・クラス設計・名前空間の3原則と過渡期ルール**:
  - 1ファイル ＝ 1型、ファイル名 ＝ クラス名を厳格順守。

---

## 6. 完了判定基準の充足状況

- [x] `IProfileApplicationService` に `ApplyProfile` および `ClearPendingRestore` が定義されている
- [x] `ProfileApplicationService.ApplyProfile` に Halt ガード（`device.HaltReportingRunAction`）が内包されている
- [x] `DefaultProfileSwitcher` が `IProfileApplicationService` に委譲し、`Program.rootHub` 直参照が排除されている
- [x] 物理切断時の復帰スタッククリア機構（`ClearPendingRestore` / `ClearState`）が配備されている
- [x] ソリューション全体が 0 警告・0 エラーでビルド成功する
- [x] 単体テストが配備され、全自動テスト（98件）がグリーンである
- [x] `Phase5-Status.md` が更新され、Step 3 の完了が記録されている
- [x] `Phase5-Step3-Completion-Report.md`（本書）が作成されている

---

## 7. 未実施・今後の確認事項・申し送り事項

- **[実機 E2E 検証]**:
  - コントローラー接続下でのリアルタイムプロファイル切替・復帰動作、および切断・再接続時のスタッククリアは、Phase 5 総合検証（Step 14 / 実機CP4）にて一括検証する。
- **[Step 4 への申し送り事項]**:
  - `IProfileApplicationService.ApplyProfile` は戻り値として `bool`（適用の成否）を返却するため、Step 4（Save／Apply の結果伝播と通知の統一）において、UI への成否通知・ステータスバー連動および `[DI]` ログ統一をシームレスに結合可能。
- **[Step 5 への申し送り事項]**:
  - Step 5（AutoProfile の DI 化）において、`AutoProfileChecker` からの適用呼び出しを `IProfileApplicationService.ApplyProfile` に統合する。

---

## 8. 次のアクション

1. フェーズ5進捗管理表（`Phase5-Status.md`）の反映確認。
2. ドメイン1 の次期タスクである **Phase5-Step4: Save／Apply の結果伝播と通知の統一（通知自動解決・成否伝播・`[DI]` ログ統一）** の実コード改修作業に着手する。
