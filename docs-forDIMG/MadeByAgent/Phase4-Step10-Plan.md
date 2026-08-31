# フェーズ4-Step10 計画書: Phase3引継ぎ再確認・シム整理・[DI]/[Legacy]ログ整備 & 最終実機検証 CP4

作成日: 2026-09-01
対象ブランチ: `For-DI-migration-work`
前提ドキュメント:
- `docs-forDIMG/DI-App-Wide-Migration-Plan.md` §3.3, §4.1, §5, §6.6（全体計画書・全体4層モデル定義）
- `docs-forDIMG/MadeByAgent/Phase4-Plan.md` §1.1.1, §2, §3 Step10（Phase4詳細計画書）
- `docs-forDIMG/MadeByAgent/Phase4-Status.md`（Phase4進捗管理）
- `docs-forDIMG/MadeByAgent/Phase4-Step9-Completion-Report.md`（Step9完了報告）
- `docs-forDIMG/MadeByAgent/Phase4-Step9-Audit-Report.md`（全体監査報告書）
- `docs-forDIMG/MadeByAgent/Phase4-Step9-RealDevice-Verification-Checklist.md`（実機CP3全件合格）
- `.github/copilot-instructions.md`（エージェント作業ルール）

---

## ルール確認（作業開始前に毎回読む）

- **§2.1 フォールバック実装・シム維持の原則**:
  - `Global` の静的シムは外部呼び出し元の互換性を担保するために安全に維持・検証し、1つの機能に対して複数の新実装経路を作らない。
- **§2.2 現在の機能の完全維持 (No Feature Drop)**:
  - アプリケーションの全機能、プロファイル・アクション設定、仮想コントローラー出力、KBM出力の挙動を 100% 維持する。
- **§2.3 ログ出力の厳格な維持**:
  - `AppLogger.LogToGui` 等の既存ログ出力を厳格に維持しつつ、DI 実行経路に `[DI]`、従来シム経路に `[Legacy]` の Trace レベルログを付与する。
- **§3.1 DI (Dependency Injection) の実装**:
  - 全 13 バックエンドサービス、Composition Root、および全 ViewModel の DI 結合の完全性を維持する。
- **資材のライフサイクル識別**:
  - DI永続資産（残るもの）と過渡期シム（Strangler Fig 移行用）を明確に区別して管理する。

---

## 0. Step10の位置づけと現状分析

### 0.1 Step0〜Step9の成果とStep10のスコープ
- **Step1〜Step9 で完了したこと**:
  - 全 13 バックエンドサービス（第1層〜第3層 ＋ 第4層 4-c）の DI 化完了。
  - `AppHost` による Composition Root 一本化完了。
  - 全 ViewModel（Pattern A, B, C 全 29 箇所）の直接 new 全廃・DI / Factory 解決への移行完了。
  - 実機検証 Checkpoint 1, 2, 3 すべて合格。
- **Step10 で行うこと**:
  - **1. [DI] および [Legacy] Trace ログの整備**:
    - DI 新経路を通る処理に `[DI]` ログを出力。
    - `Global`（`ScpUtil.cs`）の静的シムを経由する処理に `[Legacy]` ログを出力。
    - これにより、アプリ実行中に「どこが新方式で動き、どこがまだレガシーシムを通っているか」を完全に可視化・判別可能にする。
  - **2. Phase 3 引継ぎ事項の完全解消確認**: 第2層（信号変換層）と第3層（信号出力層）の責務境界が正しく維持されていることを再確認。
  - **3. 残存シムの安全監査**: `Global` シムの呼び出し状況を監査し、全体の健全性を点検。
  - **4. 最終総合実機検証（Checkpoint 4）**: Phase 4 全体完了の最終 E2E 総合テストを実施。

### 0.2 全体4層モデルにおける責務境界（全体計画書 §3.3 準拠）
1. **第1層: 入力監視層**: 機種差吸収・`DS4State` 正規化（`IDeviceStateService`, `IDs4DeviceRegistry`）。
2. **第2層: 信号変換層（拡張版）**: 2-a 基本マッピング, 2-b SpecialAction判定, 2-c アクション選択, 2-d マクロ分解。
3. **第3層: 信号出力層（拡張版）**: 3-a 仮想コントローラー出力 (`IOutputSlotService`), 3-b KBM出力 (`IVirtualKBM`), 3-c アプリ内アクション実行 (`IElevatedProcessLauncher`, `IProcessInspector`)。
4. **第4層: UI層（制御面）**:
   - **4-a. View**: 全 WPF 画面。
   - **4-b. ViewModel**: 全 ViewModel（Pattern A / B / C）。
   - **4-c. 設定／状態サービス & Factory**: `IProfileSettingsService`, `IProfileRepository`, `ISpecialActionRepository`, `IPathService`, `IEnvironmentService`, `INotificationService`, `IViewModelFactory`。

---

## 1. 設計方針とアーキテクチャ

### 1.1 [DI] および [Legacy] Trace ログ出力の設計方針
各 DI サービスおよび `Global` 静的シムの主要エントリポイントにおいて、Trace レベルでプレフィックスを付与したログを出力する。

```csharp
// ログフォーマット統一ルール:
// [DI] <クラス名>.<メソッド名>: <詳細情報>
// [Legacy] Global.<メンバー名>: <詳細情報>

// 1. DI 新経路のログ例
AppLogger.LogToGui($"[DI] AppHost.GetService: Resolved {typeof(T).Name} (Singleton)", false, true);
AppLogger.LogToGui($"[DI] ProfileRepository.LoadProfile: Slot {deviceIndex}, Profile '{profileName}' loaded via DI", false, true);
AppLogger.LogToGui($"[DI] ViewModelFactory: Created {typeof(T).Name} for Device {device}", false, true);

// 2. 従来レガシーシム経路のログ例
AppLogger.LogToGui($"[Legacy] Global.touchpadActive: Slot {i} accessed via static shim", false, true);
AppLogger.LogToGui($"[Legacy] Global.LoadProfile: Slot {deviceIndex} called via static shim", false, true);
AppLogger.LogToGui($"[Legacy] Global.actions: List accessed via static shim", false, true);
```

### 1.2 Phase 3 引継ぎ再確認とシム整理
- `Mapping.cs` や `ControlService.cs` から `Global` シムを経由している箇所を精査し、DI サービス（`IDeviceStateService`, `IOutputSlotService`, `IProfileSettingsService`）への直接参照が安全に行われているかを確認。
- `Global` のシムプロパティ（8系統）がすべて正しいフォールバックインスタンスを保持し、例外なく動作することを確認。

---

## 2. 成果物一覧

| ファイルパス | 種別 | ライフサイクル | 内容 |
|---|---|---|---|
| `DS4Windows/DI/AppHost.cs` | 更新 | **DI永続資産** | `[DI]` Trace ログ出力の追加 |
| `DS4Windows/DS4Control/Services/*.cs` | 更新 | **DI永続資産** | 各 DI サービスへの `[DI]` Trace ログ出力追加 |
| `DS4Windows/DS4Control/ScpUtil.cs` | 更新 | **過渡期シム** | `Global` 静的シムへの `[Legacy]` Trace ログ出力追加 |
| `docs-forDIMG/MadeByAgent/Phase4-Step10-RealDevice-Verification-Checklist.md` | 更新 | ドキュメント | 最終実機動作確認チェックリスト CP4（`[DI]` / `[Legacy]` 検証反映） |
| `docs-forDIMG/MadeByAgent/Phase4-Step10-Plan.md` | 更新 | ドキュメント | 本計画書 |
| `docs-forDIMG/MadeByAgent/Phase4-Step10-Completion-Report.md` | 新規 | ドキュメント | Step10完了報告書（Phase 4 全体完了報告書） |
| `docs-forDIMG/MadeByAgent/Phase4-Status.md` | 更新 | ドキュメント | 進捗ステータス更新 |

---

## 3. 作業手順（マイクロタスク分割）

### タスク Step10-1: [DI] および [Legacy] Trace ログ出力の整備
- 各 DI サービス、`ViewModelFactory`、および `AppHost` に `[DI]` ログを追加。
- `ScpUtil.cs` の各 `Global` シムに `[Legacy]` ログを追加。

### タスク Step10-2: Phase 3 引継ぎ再確認・シム健全性監査
- 第2層・第3層の境界健全性および `Global` 残存シムのフォールバック動作を監査。

### タスク Step10-3: 単体テスト・回帰テスト全件実行
- `DS4Windows.Actions.Tests` および `StandaloneTests` の全自動テストを実行し、全件合格を確認。

### タスク Step10-4: 最終総合実機動作確認 Checkpoint 4 の実施
- `Phase4-Step10-RealDevice-Verification-Checklist.md` を作成し、`[DI]` / `[Legacy]` ログを確認しながら最終 E2E 総合テストを実施。

### タスク Step10-5: ビルド検証、進捗更新、Phase 4 全体完了報告書の作成
- `dotnet build DS4WindowsWPF.sln --nologo` を実行し警告0・エラー0を確認。
- `Phase4-Status.md` を更新し、`Phase4-Step10-Completion-Report.md` を作成。

---

## 4. リスクと回避策

| リスク | 該当タスク | 回避策 |
|---|---|---|
| ログ出力によるパフォーマンスオーバーヘッド | Step10-1 | ログレベルを `Trace`（GUI通常非表示・詳細ログ有効時のみ）とし、コントローラーの毎ミリ秒ポーリングループ内にはログを入れない。 |
| シム整理時の既存コード破壊 | Step10-2 | `Global` のシムプロパティは削除せず維持し、フォールバック安全性を確認する。 |

---

## 5. 完了判定基準

- [ ] 各 DI サービスおよび Factory に `[DI]` Trace ログが導入され、DI 経路の動作が可視化されている。
- [ ] `Global` 静的シムに `[Legacy]` Trace ログが導入され、従来経路の呼び出しが可視化されている。
- [ ] Phase 3 引継ぎ事項（第2層・第3層の境界）が健全に維持されている。
- [ ] 全 96 件の自動テストが成功する。
- [ ] 最終実機検証 Checkpoint 4（`Phase4-Step10-RealDevice-Verification-Checklist.md`）で全項目が合格する。
- [ ] ソリューションビルドが警告0・エラー0で成功する。
- [ ] `Phase4-Status.md` が更新され、`Phase4-Step10-Completion-Report.md`（Phase 4 全体完了報告書）が作成されている。

