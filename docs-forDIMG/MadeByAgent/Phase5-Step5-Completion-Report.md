# Phase5-Step5 完了報告書: AutoProfile（自動プロファイル切替）の自律実行系DI化

作成日: 2026-09-04
対象ブランチ: `For-DI-migration-work`
前提ドキュメント:
- `docs-forDIMG/DI-App-Wide-Migration-Plan.md`（全体計画書・全体4層モデル定義）
- `docs-forDIMG/MadeByAgent/Phase5-Plan.md`（Phase5詳細計画書 §2, §3 Step5, §5.2, §5.3）
- `docs-forDIMG/MadeByAgent/Phase5-Status.md`（Phase5進捗管理表）
- `docs-forDIMG/MadeByAgent/Phase5-Step5-Plan.md`（本ステップの個別計画書）
- `.github/copilot-instructions.md`（エージェント憲法）

---

## 1. 実施内容サマリ

個別計画書（`Phase5-Step5-Plan.md`）に規定された全タスク（Step5-1 〜 Step5-7）を完了した。

| タスク番号 | 内容 | 結果 |
| :--- | :--- | :--- |
| **タスク Step5-1** | 既存 AutoProfileChecker の構造監査と分離設計確認 | **完了**。`AutoProfileChecker` 内部のプロセス監視・適用処理・タイマーポーリングの結合度を特定し、自律実行系の分離境界を確定。 |
| **タスク Step5-2** | `IProcessInspector` 拡張と OS ネイティブ API（P/Invoke）集約 | **完了**。`DS4Windows/DS4Control/Services/IProcessInspector.cs` に `GetForegroundProcessInfo` を新設。<br>`DefaultProcessInspector.cs` に Win32 API（`user32.dll` / `kernel32.dll` / `psapi.dll`）を集約し、OS 依存をインフラ層へ隔離。 |
| **タスク Step5-3** | `IAutoProfileService` インターフェース策定 | **完了**。`DS4Windows/DI/IAutoProfileService.cs`（第4層 4-c 契約）を新設。自律監視チェック（`CheckProfiles`）および状態クリア（`ClearState`）の契約を定義。 |
| **タスク Step5-4** | `AutoProfileService` 実装 | **完了**。`DS4Windows/DS4Control/Services/AutoProfileService.cs` を新設。<br>§5.3 ガードレール（直列化保護）を内包し、Step 3（適用一本化・Halt保護）および Step 4（通知自動解決）の申し送り事項を完全適用。 |
| **タスク Step5-5** | `ServiceRegistration.cs` 登録と `AutoProfileChecker` 委譲シム化 | **完了**。`IAutoProfileService` を Singleton 登録。既存の `AutoProfileChecker.cs` を薄い委譲シム化し、外部コードへの 100% 互換性を確保。 |
| **タスク Step5-6** | 単体テスト作成と自動テスト実行 | **完了**。`DS4WindowsTests/AutoProfileServiceTests.cs` を新設。<br>モック `IProcessInspector` を用いて、ゲーム起動時の一致切替・終了時の復帰を実機なしで 100% 自動テスト化。Actions テスト全104件が常時グリーンを達成。 |
| **タスク Step5-7** | ビルド検証、進捗更新、完了報告書の作成 | **完了**。0警告・0エラーのビルド確認、進捗表（`Phase5-Status.md`）更新、本書（完了報告書）の作成。 |

---

## 2. 変更・追加ファイル一覧

- **変更**: `DS4Windows/DS4Control/Services/IProcessInspector.cs`
  - 名前空間: `DS4Windows.Services`
  - フォアグラウンドプロセスおよびウィンドウタイトル取得契約 `GetForegroundProcessInfo(out string processPath, out string windowTitle)` を追加。
- **変更**: `DS4Windows/DS4Control/Services/DefaultProcessInspector.cs`
  - `user32.dll`（`GetForegroundWindow`, `GetWindowText` 等）、`kernel32.dll`（`OpenProcess`, `CloseHandle`）、`psapi.dll`（`GetModuleFileNameEx`）の P/Invoke 実装を集約。
  - プロセスパスおよびタイトルの直前キャッシュとバックスラッシュ正規化を実装。
- **新規作成**: `DS4Windows/DI/IAutoProfileService.cs`
  - 名前空間: `DS4Windows.DI`（全体4層モデル 第4層 4-c サービス契約）
  - 自律実行系の抽象化インターフェースを策定。
- **新規作成**: `DS4Windows/DS4Control/Services/AutoProfileService.cs`
  - 名前空間: `DS4Windows`（過渡期ルール順守）
  - `CheckProfiles` を `lock (_syncLock)` で保護し、マルチスレッド並行実行を遮断（§5.3 ガードレール）。
  - `Program.rootHub` 直参照および `device.HaltReportingRunAction` の重複呼び出しを撤廃し、Step 3 で配備された `_profileAppService.ApplyProfile` へ一本化（Halt 保護自動適用）。
  - Step 4 で配備された `displayNotification: null` 引数でプロファイルを適用し、ユーザーの「通知オフ」設定をサービス内部で自動尊重。
- **変更**: `DS4Windows/AutoProfileChecker.cs`
  - `IAutoProfileService` への薄い委譲シムとして再構成。外部からの呼び出し元に対する下位互換性を 100% 維持（§2.1 原則）。
- **変更**: `DS4Windows/DI/ServiceRegistration.cs`
  - `AutoProfileHolder` および `IAutoProfileService`（Singleton）を追加登録。
  - テスト環境下でも安全に解決可能な `ControlService` / `IDeviceStateAccessor` のフォールバック配線を整備。
- **変更**: `DS4WindowsTests/ProcessInspectorTests.cs`
  - `GetForegroundProcessInfo` の例外非発生検証テストを追加。
- **新規作成**: `DS4WindowsTests/AutoProfileServiceTests.cs`
  - `CheckProfiles_WhenProcessInspectorReturnsFalse_DoesNotApply`: 取得失敗時の無操作検証。
  - `CheckProfiles_MatchingRule_AppliesProfileWithAutoProfileSource`: ゲームプロセス一致時の自動プロファイル適用、一時プロファイル設定、通知 null 解決の検証。
  - `CheckProfiles_UnknownProcessAfterMatch_RevertsDefaultProfile`: 未知プロセス切り替え時のデフォルトプロファイル復帰検証。

---

## 3. ビルドおよびテスト結果

- **ソリューションビルド**: 成功（エラー: 0, 警告: 0）
- **テストプロジェクトビルド**: 成功（エラー: 0, 警告: 0）
- **StandaloneTests ビルド**: 成功（エラー: 0, 警告: 0）
- **テスト実行結果**:
  - `DS4WindowsTests`（xUnit）: 全104件成功（グリーン）
  - Actions 回帰テスト（85件）: 全件成功（グリーン）
  - AutoProfileService 単体テスト（3件）: 全件成功（グリーン）
  - ProfileApplicationService 単体テスト（9件）: 全件成功（グリーン）
  - ProfileXmlStore 単体テスト（4件）: 全件成功（グリーン）
  - ProfileRepository 単体テスト（7件）: 全件成功（グリーン）

---

## 4. アーキテクチャ・ガードレールへの対応結果

1. **マルチスレッド直列化（Phase5-Plan §5.3 / Step5-Plan §1.3）**:
   - タイマーポーリング（1秒周期）の重なりや、UI スレッドからの手動トリガーとの衝突を防ぐため、`CheckProfiles` 全体を `lock (_syncLock)` で保護し、プロファイル切替の直列化を物理的に保証した。
2. **適用時入力停止（Halt）保護（Step 3 申し送り事項）**:
   - `AutoProfileChecker` 内にあった `Global.ApplyProfile(..., Program.rootHub, ...)` や自前の `HaltReportingRunAction` を完全に排除し、`IProfileApplicationService.ApplyProfile` に一本化したことで、プロファイル切替中のコレクション変更クラッシュ・入力競合を確実に遮断した。
3. **通知オフ設定の自動解決（Step 4 申し送り事項）**:
   - 自動切替時に `displayNotification: null` で `ApplyProfile` を呼び出すことで、ユーザーが設定画面で「プロファイル変更通知」を無効化している場合に通知が抑制されるようになり、長年存在した通知バグを解消した。
4. **OS ネイティブ API の隔離とテスト容易性の確立（論点 1 推奨案）**:
   - `user32.dll` や `kernel32.dll` への直呼び出しを `DefaultProcessInspector` 内に完全に封じ込め、`IProcessInspector` をモック化可能にしたことで、実機でゲームを起動することなく、自動プロファイル切替の全ロジックを自動テスト可能にした。

---

## 5. ルール順守状況の評価（copilot-instructions.md チェック）

- **§2.1 フォールバック実装・シム維持の原則**:
  - `AutoProfileChecker` を委譲シムとして維持し、未改修コードとの下位互換性を保持。
- **§2.2 現在の機能の完全維持 (No Feature Drop)**:
  - ルールマッチングロジック、一時プロファイル適用フラグ、`turnOffDS4WinApp` によるサービス停止・再開動作を 100% 踏襲。
- **§2.3 ログ出力の厳格な維持**:
  - 既存の `DEBUG: Auto-Profile.` プレフィックス付き GUI ログ出力（デバッグレベル連動）を完全維持。
- **§3.1 DI (Dependency Injection) の実装**:
  - `AutoProfileService` は `AutoProfileHolder`、`IProfileApplicationService`、`IProfileSettingsService`、`IProcessInspector` を純粋コンストラクタ注入で受領。
- **§3.3 ファイル構成・クラス設計・名前空間の3原則と過渡期ルール**:
  - 1ファイル ＝ 1型、ファイル名 ＝ クラス名を厳格順守。

---

## 6. 完了判定基準の充足状況

- [x] `IProcessInspector` に `GetForegroundProcessInfo` が追加され、P/Invoke が `DefaultProcessInspector` に集約されている
- [x] `IAutoProfileService` インターフェースが策定されている
- [x] `AutoProfileService` 実装クラスが新設され、スレッド直列化（`_syncLock`）が組み込まれている
- [x] `AutoProfileService` からのプロファイル適用が `IProfileApplicationService.ApplyProfile`（Halt保護内包・通知自動解決）に一本化されている
- [x] `AutoProfileChecker` が `IAutoProfileService` を呼び出す薄い委譲シム化されている
- [x] `ServiceRegistration.cs` に `AutoProfileHolder` および `IAutoProfileService` が登録されている
- [x] ソリューション全体が 0 警告・0 エラーでビルド成功する
- [x] 単体テストが配備され、全自動テスト（104件）が常時グリーンである
- [x] `Phase5-Status.md` が更新され、Step 5 の完了が記録されている
- [x] `Phase5-Step5-Completion-Report.md`（本書）が作成されている

---

## 7. 未実施・今後の確認事項・申し送り事項

- **[実機 E2E 検証]**:
  - 実際の外部ゲームウィンドウをアクティブにした際の自動プロファイル切替・通知表示・復帰動作は、Phase 5 総合検証（Step 14 / 実機CP4）にて一括実施する。
- **[Step 6 への申し送り事項]**:
  - 次ステップ（Step 6: AppSettings 永続化）において、`Global.AutoProfileRevertDefaultProfile` 等の全体設定フラグを `IAppSettingsService` の管理下へ集約する。

---

## 8. 次のアクション

1. フェーズ5進捗管理表（`Phase5-Status.md`）の反映確認。
2. ドメイン1 の最終ステップである **Phase5-Step6: アプリ全体設定（AppSettings）の永続化・状態管理のDI化（`Phase5-Step6-Plan.md`）** の実コード改修作業に着手する。
