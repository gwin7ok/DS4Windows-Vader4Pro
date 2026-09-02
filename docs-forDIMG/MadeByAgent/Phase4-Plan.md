# フェーズ4 計画書: Global 分割と ViewModel DI 化

作成日: 2026-08-31
最終更新日: 2026-09-01
対象ブランチ: `For-DI-migration-work`
全体計画書: `docs-forDIMG/DI-App-Wide-Migration-Plan.md`
前提ドキュメント:
- `docs-forDIMG/MadeByAgent/Phase4-Step0-Plan.md`
- `docs-forDIMG/MadeByAgent/Phase4-Step0-Completion-Report.md`
- `docs-forDIMG/MadeByAgent/Phase4-Step0-Global-Member-Inventory.md`
- `docs-forDIMG/MadeByAgent/Phase4-Step0-ViewModel-Inventory.md`
- `docs-forDIMG/MadeByAgent/Phase4-Step0-DI-Startup-Sequence.md`
- `docs-forDIMG/MadeByAgent/Phase4-Step0-Baseline-Test-Report.md`
- `.github/copilot-instructions.md`

---

## ルール確認（作業開始前に毎回読む）

- **§2.1 フォールバック実装の原則（修正版）**:
  - `Global` の静的メンバは削除せず、新設する各サービスへの薄いデリゲートシムとして残す。
  - 1つの設定アクセスに対して複数の新実装経路を同時に作らない（単一責任・単一路線）。
- **§2.2 現在の機能の完全維持 (No Feature Drop)**:
  - プロファイル設定の既定値、配列境界（`TEST_PROFILE_ITEM_COUNT` = 9, `MAX_DS4_CONTROLLER_COUNT` = 8）、カルチャ（`configFileDecimalCulture` = en-US）、特殊状態フラグの挙動を 100% 維持する。
- **§2.3 ログ出力の厳格な維持 ＆【最重要申し送り事項】[DI] / [Legacy] Trace ログ出力規約**:
  - `AppLogger.LogToGui` 等の既存ログ関数およびログレベルを厳格に維持する。
  - **今後のすべての変更・新規追加処理において**:
    1. **新方式 DI 実行経路**: DI サービス、Factory、ViewModel、AppHost 等の処理には、必ず **`[DI] <クラス名>.<メソッド名>: <詳細情報>`** 形式で Trace レベルログ（`AppLogger.LogToGui(..., false, true)`）を出力すること。
    2. **従来レガシーシム経路**: `Global` の静的プロパティ・メソッド、フォールバックインスタンス等の過渡期シム処理には、必ず **`[Legacy] Global.<メンバー名>: <詳細情報>`** 形式で Trace レベルログを出力すること。
    3. **目的**: アプリ実行時に新旧どちらの経路を通って処理が実行されたかをログ上で 100% 判別・可視化可能にし、将来のシム完全撤去およびリグレッション検証のエビデンスとする。
- **§3.1 DI (Dependency Injection) の実装**:
  - インターフェース契約は `DS4Windows/DI/`（名前空間 `DS4Windows.DI`）に配置（**DI永続資産**）。
  - 実装クラスは `DS4Windows/DS4Control/Services/`（名前空間 `DS4Windows`）に配置（**DI永続資産**）。
  - コンテナ登録は `DS4Windows/DI/ServiceRegistration.cs` に行う。
- **§3.2 巨大ファイル (`ScpUtil.cs` / `Global` / `Mapping.cs`) の編集方針**:
  - ファイル全体を再生成せず、対象メンバーのみをピンポイントで置換する。
- **§4 / §5 マイクロステップ進行 & 実機検証チェックポイント**:
  - 各ステップ（Step 0〜10）単位で区切り、要所（Step 3, 6, 9, 10）で実機動作確認（CP1〜CP4）を実施する。

---

## 1. フェーズ4の目的・スコープ

### 1.1 目的
- `Global` クラス（`ScpUtil.cs` 内、棚卸し実測値 442 件）に集中している設定・状態・I/O 責務を機能別 DI サービス群へ分割・移行する。
- View / UserControl による ViewModel 直接生成（棚卸し実測値 29 件）を DI コンテナ注入 / ファクトリ方式へ切り替える。
- `AppHost`（DIコンテナ）起動シーケンスを一本化し、UI およびバックエンド全体で安全に DI サービスを利用可能にする。
- 新方式 DI 実行経路（`[DI]`）および従来シム経路（`[Legacy]`）の稼働状況を客観的に可視化・比較監査するための Trace ログを整備する。

### 1.1.1 全体4層モデルとの責務境界（全体計画書 §3.3 準拠）
全体計画書（`DI-App-Wide-Migration-Plan.md` §3.3）で規定された **全体4層モデル（実行時3層 ＋ UI層）** に基づき、フェーズ4における各サービスの責務境界を統一管理する：

1. **第1層: 入力監視層**
   - コントローラーの機種差を吸収し、`DS4State` に正規化して上位へ渡す。
2. **第2層: 信号変換層（拡張版）**
   - 入力から「何を出力すべきか」を決定する（副作用の実行は行わない）。
   - **2-a. 基本マッピング決定**: 1入力→1出力（コントローラー信号／KBM信号）の対応表引き。
   - **2-b. SpecialActionトリガー判定**: 複数入力の組み合わせで成立/解除を判定し、元入力の出力を抑制するか決定。
   - **2-c. アクション選択・パラメータ決定**: 成立したSpecialActionが「マクロ／プロファイル切替／プロセス起動／KBM出力」のどれかを判定し、実行に必要なパラメータ（マクロ内容、プロファイル名、起動パス等）を確定。
   - **2-d. マクロの分解**: トリガーされたマクロを、時系列のKBM出力信号列（何をいつ押す/離すか）に分解。
3. **第3層: 信号出力層（拡張版）**
   - 決定された内容を実際に副作用として実行する。
   - **3-a. 仮想コントローラー出力**: 2-aの結果をDS4/Xbox360規格で実出力（`outputDevices[ind]` / `IOutputSlotService`）。
   - **3-b. KBM出力**: 2-aの結果、および2-dで分解されたマクロの信号列を、実際に時系列で送出（`outputKBMHandler` / `IVirtualKBM`、タイマー駆動の逐次実行を含む）。
   - **3-c. アプリ内アクション実行**: 2-cで決定されたプロファイル切替・プロセス起動を実際に実行（ファイルロード／Global状態更新／`Process.Start` 呼び出し。権限昇格・多重起動チェックも含む）。
4. **第4層: UI層（制御面）**
   - ユーザーが設定・プロファイル・状態を操作し、サービス経由で実行時3層へ設定を反映する。
   - **4-a. View**: WPF の画面・UserControl。
   - **4-b. ViewModel**: 画面状態、入力値検証、画面イベントの調整。
   - **4-c. 設定／状態サービス & Factory**: プロファイル（`IProfileSettingsService`, `IProfileRepository`）、SpecialAction（`ISpecialActionRepository`）、入力・出力設定、デバイス状態（`IDeviceStateService`, `IOutputSlotService`）、環境・通知（`IPathService`, `IEnvironmentService`, `INotificationService`）、ViewModel Factory（`IViewModelFactory`）をDI管理。

---

## 2. ステップ分割と実機検証マイルストーン

| ステップ | 対象 | 概要 | 成果物・検証 | 状態 |
|---|---|---|---|---|
| Step 0 | 現状棚卸し・基準テスト | Global メンバー442件、呼び出し元80ファイル、ViewModel 直接生成29件の棚卸し | 調査ドキュメント5件、基準テスト全件通過 | **完了** |
| Step 1 | IProfileSettingsService 実装化 | プロファイル設定値（第4層 4-c）、既定値、配列境界（9/8）、変更イベントの DI 化 | `IProfileSettingsService.cs`, `ProfileSettingsService.cs` | **完了** |
| Step 2 | IProfileRepository 分離 | プロファイル XML 読込・保存・パス解決・一覧・切替（第4層 4-c）の分離 | `IProfileRepository.cs`, `ProfileRepository.cs` | **完了** |
| Step 3 | ISpecialActionRepository 分離 | Custom Action（マクロ、切替等: 第4層 4-c）の XML 永続化・CRUD 分離 | `ISpecialActionRepository.cs`, `SpecialActionRepository.cs` | **完了** |
| **実機CP1** | **データ中核層 実機検証** | **設定・プロファイル・Action の実機接続・UI連動・物理XML永続化を検証** | **`Phase4-Step3-RealDevice-Verification-Checklist.md` (全12項目 ○ 合格)** | **完了** |
| Step 4 | 入力・出力・デバイス状態サービス | デバイス状態（第1層/第4層 4-c: `IDeviceStateService`）、出力スロット管理（第3層 3-a/第4層 4-c: `IOutputSlotService`）の DI 分離 | `IDeviceStateService`, `IOutputSlotService` | **完了** |
| Step 5 | 環境・UI・通知サービス | パス解決、環境情報、トースト通知、UI状態（第4層 4-c）の DI 分離 | Path/Env/Notification/UI サービス | **完了** |
| Step 6 | Composition Root 一本化 | DI コンテナ二重起動の解消・起動シーケンス一本化（第4層 4-c） | `AppHost.cs`, `ServiceRegistration.cs` 一本化 | **完了** |
| **実機CP2** | **全バックエンドDI＋Root一本化 実機検証** | **バックエンド完成段階での起動シーケンス・HID通信・仮想コントローラー出力・UAC昇格検証** | **`Phase4-Step6-RealDevice-Verification-Checklist.md` (実施完了)** | **完了** |
| Step 7 | ViewModel DI 移行 (Pattern A) | 引数なし ViewModel（Settings, Log, About: 第4層 4-b）の DI 移行 | View x:Static / GetService 注入 | **完了** |
| Step 8 | ViewModel DI 移行 (Pattern B) | 共有依存 ViewModel（Controllers, Main 等: 第4層 4-b）の DI 移行 | 共有サービス経由注入 | **完了** |
| Step 9 | ViewModel DI 移行 (Pattern C) | 実行時引数付き ViewModel（ProfileSettings, **RecordBox**, SpecialActEditor, AutoProfiles 等: 第4層 4-b）の Factory 移行 | Factory パターン注入, **Step9-4-α監査合格** | **完了** |
| **実機CP3** | **全ViewModel DI移行完了 実機検証** | **全画面 UI 結合・ViewModel 直接 new 全廃後の UI バインディング・画面遷移検証** | **`Phase4-Step9-RealDevice-Verification-Checklist.md` (全12項目 ○ 合格)** | **完了** |
| Step 10-1 | `[DI]`／`[Legacy]` Trace ログ整備 | DI 実行経路と従来シム経路を識別できる Trace ログを整備し、高頻度ログを抑制する | 各サービス・シムへのログ導入、ログ監査記録 | **進行中** |
| Step 10-2-A | `Global` シム接続拡張 | `IProfileSettingsService` 等へ `Global` の設定 API を接続する | `Phase4-Step10-2-A` 成果物、A-1〜A-9報告書 | **完了** |
| Step 10-2-B | 呼び出し元の DI 直接参照化 | `ProfileSettingsViewModel`、`ProfileEditor`、`ControlService`、`Mapping` の対象経路を DI へ移行する | `Phase4-Step10-2-B-Plan.md`、カテゴリ別完了報告書 | **完了** |
| Step 10-2-C | Legacy 経路残存の整理と段階移行 | Phase4 対象の Legacy 経路を分類し、Composition Root、`rootHub`、ViewModel フォールバック、設定シム呼び出し元等を段階整理する | `Phase4-Step10-2-C-Plan.md`、Legacy調査・分類報告書 | **C-0〜C-5-1完了、C-5-2計画済み** |
| Step 10-2-C-0 | 現状基準の固定 | Legacy 残存量、対象判定ルール、判断項目を固定する | Legacy残存調査報告書 | **完了** |
| Step 10-2-C-1 | Composition Root 一本化 | 旧 `ServiceCollection` を削除し、AppHost／ServiceRegistration を唯一の構築経路にする | C-1/C-2実装前確認記録 | **実装完了・検証待ち** |
| Step 10-2-C-2 | `ControlService` DI 登録と互換代入 | `ControlService` を Singleton 登録し、AppHost から解決する。`rootHub` 代入は維持する | C-1/C-2実装記録 | **実装完了・検証待ち** |
| Step 10-2-C-3 | `rootHub` 呼び出し元の分類と個別移行 | `Mapping` のプロファイル適用・復帰と Action 連鎖を専用サービスへ移す | `Phase4-Step10-2-C-3-Plan.md`、分類報告書 | **C-3-1完了、C-3-2〜C-3-4実装完了・検証待ち** |
| Step 10-2-C-4 | ViewModel フォールバックの可視化 | CP4 までフォールバックを維持し、使用時に `[Legacy]` ログを出力する | フォールバックログ、テスト | **未着手** |
| Step 10-2-C-5 | Legacy シムのログ網羅性監査 | 高頻度ログを抑制しながら、シム入口・変更・失敗を監査し、設定シム呼び出し元をDI APIへ移行する | シムログ監査報告書、C-5-1／C-5-2移行基準書 | **C-5-1完了、C-5-2計画済み** |
| Step 10-2-C-6 | CP4 前自動テスト化判定・実装・実行 | CP4 項目を自動テスト／実機／両方に分類し、自動化可能な項目を実装する | 自動テスト、CP4項目分類表 | **未着手** |
| Step 10-2-C-7 | CP4 実機検証 | C-6 で代替できない HID、WPF、ドライバ、長時間安定性等を確認する | CP4 実機チェックリスト | **未着手（計画）** |
| Step 10-2-C-8 | CP4 後のフォールバック削除判断 | CP4 結果を基に互換フォールバック削除の可否を別変更として判断する | フォールバック削除判断報告書 | **未着手（計画）** |
| **実機CP4** | **Phase4 最終総合 E2E 実機検証** | **`[DI]` および `[Legacy]` ログを活用したシム整理後・Phase 4 完了総合実機検証（長時間接続・負荷・安定性）** | **実機検証チェックリスト CP4** | 未着手 (計画) |

---

## 3. 各ステップの詳細

### Step 10-1: [DI] および [Legacy] Trace ログ整備
- **対象**:
  - **新方式 DI 経路**: `AppHost.GetService`、全 DI サービス、`ViewModelFactory` に `[DI] <クラス名>.<メソッド名>: <詳細>` ログを出力。
  - **従来レガシー経路**: `Global`（`ScpUtil.cs`）の各静的シム（`touchpadActive`, `LoadProfile`, `SaveProfile`, `actions`, `devices` 等）に `[Legacy] Global.<メンバー名>: <詳細>` ログを出力。
  - アプリ実行時に「どの処理が DI 新経路を通り、どの処理がまだ古いシムを経由しているか」をログ上で可視化・比較可能にする。
  - 今後の変更・機能追加でも、新経路には `[DI]`、従来シム経路には `[Legacy]` の Trace ログを付与する。

### Step 10-2-A: `Global` シム接続拡張

- `IProfileSettingsService` 等の契約へ `Global` の設定 API を接続する。
- A-1〜A-9 の各カテゴリについて、後方互換シムと既存挙動を確認する。

### Step 10-2-B: 呼び出し元の DI 直接参照化

- `ProfileSettingsViewModel`、`ProfileEditor`、`ControlService`、`Mapping` の対象設定参照を DI 経由へ移行する。
- 実行時引数付き ViewModel は Factory 経由で生成し、互換フォールバックは検証完了まで維持する。

### Step 10-2-C: Legacy 経路残存の整理と段階移行

- Phase3 引継ぎ事項と残存シムを C-0〜C-8 で整理する。
- Composition Root、`ControlService`、`rootHub`、ViewModel フォールバック、Legacy ログを段階的に整理する。
- C-6 で自動テスト化できる CP4 項目を抽出し、C-7 では実機必須項目に集中する。
- 第2層（信号変換層）と第3層（信号出力層）の責務境界を再確認する。

---

## 4. テスト計画

### 4.1 単体テスト（ユニットテスト）
- 各ステップで新設するサービス、リポジトリ、および Factory に対する専用テストを作成し、機能・境界・イベント通知・引数結合を検証する。

### 4.2 回帰テスト
- `DS4Windows.Actions.Tests` (83件) および `StandaloneTests` (13件) を各ステップ完了毎に実行し、回帰ゼロ（全件パス）を維持する。

### 4.3 実機動作確認チェックポイント計画（実機検証 CP1〜CP4）

実機コントローラー（Vader 4 Pro / DS4 等）および実際の Windows / WPF 実行環境を用いて、以下の 4 大マイルストーンで実機動作検証を実施する：

1. **Checkpoint 1 (Step 3 完了時: データ中核層 DI 化確認) 【完了・全12項目合格】**:
   - **対象**: `IProfileSettingsService`, `IProfileRepository`, `ISpecialActionRepository` (第4層 4-c)
   - **検証内容**: プロファイル設定の UI リアルタイム反映、`Profiles/*.xml` および `Actions.xml` の物理読み書き、スロット切替、コントローラー接続時の設定適用。
   - **成果物**: `Phase4-Step3-RealDevice-Verification-Checklist.md`
2. **Checkpoint 2 (Step 6 完了時: バックエンド完成＆Composition Root 一本化確認) 【完了・実施済み】**:
   - **対象**: 第1層〜第3層を制御する全バックエンドサービス群（デバイス、出力スロット、仮想KBM、環境、通知）＋一本化された `AppHost`
   - **検証内容**: アプリ起動シーケンスの完全性、コントローラー実機の接続・切断検知、仮想コントローラー出力（ViGEmBus: 3-a）、仮想KBM送出（3-b）、UAC 昇格実行（3-c）、トースト通知、ログ出力。
   - **成果物**: `Phase4-Step6-RealDevice-Verification-Checklist.md`
3. **Checkpoint 3 (Step 9 完了時: 全 ViewModel DI 化＆UI 結合確認) 【完了・全12項目合格】**:
   - **対象**: 第4層 4-a (View) / 4-b (ViewModel) における全 29 箇所の直接 new の全廃（Pattern A, B, C 全網羅）、DI コンテナ / Factory 経由の UI バインディング
   - **検証内容**: 全画面（メイン、Controllers、Profiles、Special Actions、Settings、Log、RecordBox、AutoProfiles）のデータバインディング、ダイアログ表示、画面遷移、直接 new の残存ゼロ確認。
   - **成果物**: `Phase4-Step9-RealDevice-Verification-Checklist.md`
4. **Checkpoint 4 (Step10-2-C-7 完了時: Phase 4 最終総合 E2E 検証) 【計画】**:
   - **対象**: 過渡期シム整理後・`[DI]` / `[Legacy]` ログ導入後の DS4Windows 全体（第1層〜第4層の完全統合）
   - **検証内容**: `[DI]` および `[Legacy]` Trace ログの出力を確認しながらの総合 E2E テスト（長時間ゲームプレイ、プロファイル切替、マクロ再生、複数コントローラー接続、スリープ復帰、エラーログゼロ確認）。
   - **成果物**: `Phase4-Step10-RealDevice-Verification-Checklist.md`
