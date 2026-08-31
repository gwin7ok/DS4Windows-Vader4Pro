# フェーズ4-Step6 計画書: Composition Root 一本化 & 実機検証 Checkpoint 2

作成日: 2026-08-31
対象ブランチ: `For-DI-migration-work`
前提ドキュメント:
- `docs-forDIMG/DI-App-Wide-Migration-Plan.md` §3.3, §4.1, §5, §6.6（全体計画書・全体4層モデル定義）
- `docs-forDIMG/MadeByAgent/Phase4-Plan.md` §1.1.1, §1.2, §2, §3 Step6（Phase4詳細計画書）
- `docs-forDIMG/MadeByAgent/Phase4-Status.md`（Phase4進捗管理）
- `docs-forDIMG/MadeByAgent/Phase4-Step5-Completion-Report.md`（Step5完了報告）
- `docs-forDIMG/MadeByAgent/Phase4-Step0-DI-Startup-Sequence.md`（DI起動シーケンス棚卸し）
- `docs-forDIMG/MadeByAgent/Phase4-Step3-RealDevice-Verification-Checklist.md`（実機CP1全件合格）
- `.github/copilot-instructions.md`（エージェント作業ルール）

---

## ルール確認（作業開始前に毎回読む）

- **§2.1 フォールバック実装・シム維持の原則**:
  - `Global` のシムアクセサは削除せず、一本化された DI コンテナから解決される仕組みを維持する。
- **§2.2 現在の機能の完全維持 (No Feature Drop)**:
  - WPF アプリケーションの起動引数、多重起動防止、UAC 昇格チェック、トレイ最小化起動、ロギング開始、終了処理の挙動を 100% 維持する。
- **§2.3 ログ出力の厳格な維持**:
  - `AppLogger.LogToGui` 等の既存ログ出力を厳格に維持する。
- **§3.1 DI (Dependency Injection) の実装**:
  - `AppHost.cs` をアプリケーション唯一の Composition Root として一本化する（**DI永続資産**）。
  - すべてのサービス登録を `DS4Windows/DI/ServiceRegistration.cs` に集約する（**DI永続資産**）。
- **§3.2 巨大ファイルの編集方針**:
  - `App.xaml.cs` は全体を再生成せず、起動（`OnStartup`）・終了（`OnExit`）シーケンスのみをピンポイントで置換・整理する。
- **資材のライフサイクル識別**:
  - DI永続資産（残るもの）と過渡期シム（Strangler Fig 移行用）を明確に区別して管理する。

---

## 0. Step6の位置づけと現状分析

### 0.1 Step0〜Step5の成果とStep6のスコープ
- **Step1〜Step5 で完了したこと**:
  - 第4層 4-c（設定／状態サービス）に必要な全バックエンドサービス（設定、プロファイル、Action、デバイス状態、出力スロット、パス、環境、通知）の DI 化が完了。
- **Step6 で行うこと**:
  - Step 0 の調査（`Phase4-Step0-DI-Startup-Sequence.md`）で判明していた `App.xaml.cs` と `AppHost.cs` における DI コンテナ二重起動・二重コンテナ構造を解消し、`AppHost.CreateHost()` を唯一の Composition Root として起動・停止シーケンスを完全一本化する。
  - Step 7〜9（ViewModel DI 移行）に進む前の **バックエンド全サービス完成マイルストーン** として、第2回実機検証（**Checkpoint 2**）を実施する。

### 0.2 全体4層モデルにおける責務境界と本Stepの位置づけ（全体計画書 §3.3 準拠）
全体計画書（`DI-App-Wide-Migration-Plan.md` §3.3）および Phase4 計画書（`Phase4-Plan.md` §1.1.1）で規定された **全体4層モデル（実行時3層 ＋ UI層）** に基づき、本Step（Step 6）の位置づけを以下のように整理する：

1. **第1層: 入力監視層**
   - コントローラーの機種差を吸収し、`DS4State` に正規化して上位へ渡す（`IDeviceStateService`, `IDs4DeviceRegistry` 経由）。
2. **第2層: 信号変換層（拡張版）**
   - 入力から「何を出力すべきか」を決定する（2-a 基本マッピング, 2-b SpecialAction判定, 2-c アクション選択, 2-d マクロ分解）。
3. **第3層: 信号出力層（拡張版）**
   - 決定された内容を実際に副作用として実行する（3-a 仮想コントローラー出力 `IOutputSlotService`, 3-b KBM出力 `IVirtualKBM`, 3-c アプリ内アクション実行）。
4. **第4層: UI層（制御面）**
   - ユーザーが設定・プロファイル・状態を操作し、サービス経由で実行時3層へ設定を反映する。
   - **4-a. View**: WPF の画面・UserControl。
   - **4-b. ViewModel**: 画面状態、入力値検証、画面イベントの調整。
   - **4-c. 設定／状態サービス & Composition Root 【★本Step対象】**:
     - `AppHost`: アプリケーション全体の DI コンテナ構築・ライフサイクル管理（Composition Root 一本化）。
     - `ServiceRegistration`: 全サービス（Step 1〜5 サービス ＋ 既存 Phase 2/3 サービス）の登録集約。

---

## 1. 設計方針とアーキテクチャ

### 1.1 Composition Root 一本化の設計方針
- **`AppHost.cs`（唯一のコンテナホスト）**:
  - `AppHost.CreateHost()` で `Microsoft.Extensions.Hosting.IHost` を生成。
  - `ServiceRegistration.RegisterServices(services)` ですべてのサービスを一括登録。
  - `AppHost.GetService<T>()` を通じた安全なサービス解決を提供。
  - `AppHost.Dispose()` でホストおよび全シングルトンサービスの適切な解放（Dispose）を担保。
- **`App.xaml.cs`（WPF ライフサイクル）**:
  - `OnStartup`: `AppHost.CreateHost()` を呼び出して DI コンテナを初期化後、メインウィンドウを解決して起動。
  - `OnExit`: `AppHost.Dispose()` を呼び出して安全にホストをシャットダウン。
  - 独自で二重に `ServiceCollection` を構築・保持する古い初期化コードを全廃する。

### 1.2 サービス登録の一覧（`ServiceRegistration.cs` 集約）
Step 6 時点で `AppHost` に登録・一本化される全サービス一覧：

| サービスインターフェース | 実装クラス | ライフタイム | 対応層 |
|---|---|---|---|
| `IProfileSettingsService` | `ProfileSettingsService` | Singleton | 第4層 4-c |
| `IProfileRepository` | `ProfileRepository` | Singleton | 第4層 4-c |
| `ISpecialActionRepository` | `SpecialActionRepository` | Singleton | 第4層 4-c |
| `IDeviceStateService` | `DeviceStateService` | Singleton | 第1層 / 第4層 4-c |
| `IOutputSlotService` | `OutputSlotService` | Singleton | 第3層 3-a / 第4層 4-c |
| `IPathService` | `PathService` | Singleton | 第4層 4-c |
| `IEnvironmentService` | `EnvironmentService` | Singleton | 第4層 4-c |
| `INotificationService` | `AppNotificationService` | Singleton | 第4層 4-c |
| `IVirtualKBM` | `VirtualKBMService` | Singleton | 第3層 3-b / 第4層 4-c |
| `IDs4DeviceRegistry` | `Ds4DeviceRegistry` | Singleton | 第1層 / 第4層 4-c |
| `IDeviceStateAccessor` | `DeviceStateAccessor` | Singleton | 第1層 / 第4層 4-c |
| `IElevatedProcessLauncher` | `ElevatedProcessLauncher` | Singleton | 第3層 3-c / 第4層 4-c |
| `IProcessInspector` | `ProcessInspectorService` | Singleton | 第3層 3-c / 第4層 4-c |

---

## 2. 成果物一覧

| ファイルパス | 種別 | ライフサイクル | 内容 |
|---|---|---|---|
| `DS4Windows/DI/AppHost.cs` | 更新 | **DI永続資産** | 唯一の Composition Root としてライフサイクル管理を一元化 |
| `DS4Windows/DI/ServiceRegistration.cs` | 更新 | **DI永続資産** | 全 13 バックエンドサービスの登録を集約 |
| `DS4Windows/App.xaml.cs` | 更新 | **DI永続資産** | `AppHost.CreateHost()` を呼び出す起動・終了シーケンスの一本化 |
| `DS4WindowsTests/CompositionRootTests.cs` | 新規 | **テスト資産** | コンテナ構築・全サービス解決・循環依存ゼロを検証する単体テスト |
| `docs-forDIMG/MadeByAgent/Phase4-Step6-RealDevice-Verification-Checklist.md` | 新規 | ドキュメント | 実機動作確認チェックリスト CP2 |
| `docs-forDIMG/MadeByAgent/Phase4-Step6-Plan.md` | 新規 | ドキュメント | 本計画書 |
| `docs-forDIMG/MadeByAgent/Phase4-Step6-Completion-Report.md` | 新規 | ドキュメント | Step6完了報告書 |
| `docs-forDIMG/MadeByAgent/Phase4-Status.md` | 更新 | ドキュメント | Step6進捗ステータス更新 |

---

## 3. 作業手順（マイクロタスク分割）

### タスク Step6-1: `AppHost.cs` & `ServiceRegistration.cs` の一本化実装
- `AppHost.cs` のライフサイクルメソッド（`CreateHost`, `GetService`, `Dispose`）を整備。
- `ServiceRegistration.cs` にて全 13 サービスを確実に登録。

### タスク Step6-2: `App.xaml.cs` の起動・終了シーケンス一本化
- `App.xaml.cs` 内の二重コンテナ初期化を全廃し、`AppHost.CreateHost()` への単一エントリポイントへ整理。

### タスク Step6-3: 単体テスト作成とコンテナ解決検証
- `DS4WindowsTests/CompositionRootTests.cs` を作成し、全サービスが `AppHost` から例外なく解決できることを検証。
- 回帰テスト（`Actions.Tests` 31件, `StandaloneTests` 13件, 全新設テスト）の通過を確認。

### タスク Step6-4: 実機動作確認 Checkpoint 2 の実施
- `Phase4-Step6-RealDevice-Verification-Checklist.md` を作成し、実機環境（HID通信、仮想コントローラー出力、UAC昇格、ログ出力）での動作確認を実施。

### タスク Step6-5: ビルド検証、進捗更新、完了報告書の作成
- `dotnet build DS4WindowsWPF.sln --nologo` を実行し警告0・エラー0を確認。
- `Phase4-Status.md` を更新し、`Phase4-Step6-Completion-Report.md` を作成。

---

## 4. リスクと回避策

| リスク | 該当タスク | 回避策 |
|---|---|---|
| コンテナ解決時の循環依存・例外 | Step6-1, Step6-3 | 単体テスト（`CompositionRootTests`）で全 13 サービスの一括解決を自動検証する。 |
| アプリ起動失敗・WPF初期化タイミング | Step6-2 | `OnStartup` の先頭で `AppHost.CreateHost()` を完了させてから WPF ウィンドウを初期化する。 |
| プロセス終了時のリソースリーク | Step6-2 | `OnExit` で `AppHost.Dispose()` を確実に実行し、シングルトンサービスをクリーンに解放する。 |

---

## 5. 完了判定基準

- [ ] `AppHost.cs` が唯一の Composition Root として一本化されている（DI永続資産）。
- [ ] `ServiceRegistration.cs` に全 13 バックエンドサービスが登録されている（DI永続資産）。
- [ ] `App.xaml.cs` の二重コンテナ初期化が全廃され、起動・終了シーケンスが一本化されている。
- [ ] 新設した `CompositionRootTests` で全サービスが例外なく解決される。
- [ ] 既存の全回帰テストが成功する。
- [ ] 実機検証 Checkpoint 2（`Phase4-Step6-RealDevice-Verification-Checklist.md`）で全項目が合格する。
- [ ] ソリューションビルドが警告0・エラー0で成功する。
- [ ] `Phase4-Status.md` が更新され、`Phase4-Step6-Completion-Report.md` が作成されている。

