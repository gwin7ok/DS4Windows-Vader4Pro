# フェーズ4 計画書: Global 分割と ViewModel DI 化

作成日: 2026-08-31
最終更新日: 2026-08-31
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
- **§2.3 ログ出力の厳格な維持**:
  - `AppLogger.LogToGui` 等の既存ログ関数およびログレベルを厳格に維持する。
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

### 1.1.1 全体4層モデルとの責務境界
- **第1層（Core/Common）**: ユーティリティ計算、定数
- **第2層（Domain/Device）**: コントローラーデバイス・状態管理
- **第3層（Action/Execution）**: 信号変換・アクション実行
- **第4層（UI/Application）**: 設定サービス（`IProfileSettingsService`）、永続化リポジトリ（`IProfileRepository`, `ISpecialActionRepository`）、UI ViewModel

---

## 2. ステップ分割と実機検証マイルストーン

| ステップ | 対象 | 概要 | 成果物・検証 | 状態 |
|---|---|---|---|---|
| Step 0 | 現状棚卸し・基準テスト | Global メンバー442件、呼び出し元80ファイル、ViewModel 直接生成29件の棚卸し | 調査ドキュメント5件、基準テスト全件通過 | **完了** |
| Step 1 | IProfileSettingsService 実装化 | プロファイル設定値（メモリ上）、既定値、配列境界（9/8）、変更イベントの DI 化 | `IProfileSettingsService.cs`, `ProfileSettingsService.cs` | **完了** |
| Step 2 | IProfileRepository 分離 | プロファイル XML 読込・保存・パス解決・一覧・切替の分離 | `IProfileRepository.cs`, `ProfileRepository.cs` | **完了** |
| Step 3 | ISpecialActionRepository 分離 | Custom Action（マクロ、切替等）の XML 永続化・CRUD 分離 | `ISpecialActionRepository.cs`, `SpecialActionRepository.cs` | **完了** |
| **実機CP1** | **データ中核層 実機検証** | **設定・プロファイル・Action の実機接続・UI連動・物理XML永続化を検証** | **`Phase4-Step3-RealDevice-Verification-Checklist.md` (全12項目 ○ 合格)** | **完了** |
| Step 4 | 入力・出力・デバイス状態サービス | デバイス状態、仮想KBM、出力スロット管理の DI サービス分離 | 各種 Device/Output/Input サービス | **未着手 (次)** |
| Step 5 | 環境・UI・通知サービス | パス解決、環境情報、トースト通知、UI状態の DI サービス分離 | Path/Env/Notification/UI サービス | 未着手 |
| Step 6 | Composition Root 一本化 | DI コンテナ二重起動の解消・起動シーケンス一本化 | `AppHost.cs`, `App.xaml.cs` 一本化 | 未着手 |
| **実機CP2** | **全バックエンドDI＋Root一本化 実機検証** | **バックエンド完成段階での起動シーケンス・HID通信・仮想コントローラー出力・UAC昇格検証** | **実機検証チェックリスト CP2** | 未着手 (計画) |
| Step 7 | ViewModel DI 移行 (Pattern A) | 引数なし ViewModel（Settings, Log, About 等）の DI 移行 | View x:Static 注入 | 未着手 (計画) |
| Step 8 | ViewModel DI 移行 (Pattern B) | 共有依存 ViewModel（Controllers, Main 等）の DI 移行 | 共有サービス経由注入 | 未着手 (計画) |
| Step 9 | ViewModel DI 移行 (Pattern C) | 実行時引数付き ViewModel（ProfileEdit 等）の Factory 移行 | Factory パターン注入 | 未着手 (計画) |
| **実機CP3** | **全ViewModel DI移行完了 実機検証** | **全画面 UI 結合・ViewModel 直接 new 全廃後の UI バインディング・画面遷移検証** | **実機検証チェックリスト CP3** | 未着手 (計画) |
| Step 10 | Phase3 引継ぎ再確認・シム整理 | 残存シムの安全監査、Phase3引継ぎ事項の完全解消確認 | 監査レポート、不要シム整理 | 未着手 (計画) |
| **実機CP4** | **Phase4 最終総合 E2E 実機検証** | **シム整理後の Phase 4 最終総合結合テスト（長時間接続・負荷・安定性）** | **実機検証チェックリスト CP4** | 未着手 (計画) |

---

## 3. テスト計画

### 3.1 単体テスト（ユニットテスト）
- 各ステップで新設するサービスおよびリポジトリに対する専用テストを作成し、機能・境界・イベント通知を検証する。

### 3.2 回帰テスト
- `DS4Windows.Actions.Tests` (31件) および `StandaloneTests` (13件) を各ステップ完了毎に実行し、回帰ゼロ（全件パス）を維持する。

### 3.3 実機動作確認チェックポイント計画（実機検証 CP1〜CP4）

実機コントローラー（Vader 4 Pro / DS4 等）および実際の Windows / WPF 実行環境を用いて、以下の 4 大マイルストーンで実機動作検証を実施する：

1. **Checkpoint 1 (Step 3 完了時: データ中核層 DI 化確認) 【完了・全12項目合格】**:
   - **対象**: `IProfileSettingsService`, `IProfileRepository`, `ISpecialActionRepository`
   - **検証内容**: プロファイル設定の UI リアルタイム反映、`Profiles/*.xml` および `Actions.xml` の物理読み書き、スロット切替、コントローラー接続時の設定適用。
   - **成果物**: `Phase4-Step3-RealDevice-Verification-Checklist.md`
2. **Checkpoint 2 (Step 6 完了時: バックエンド完成＆Composition Root 一本化確認) 【計画】**:
   - **対象**: 全バックエンドサービス群（デバイス、出力スロット、仮想KBM、環境、通知）＋一本化された `AppHost`
   - **検証内容**: アプリ起動シーケンスの完全性、コントローラー実機の接続・切断検知、仮想コントローラー出力（ViGEmBus）、UAC 昇格実行、トースト通知、ログ出力。
3. **Checkpoint 3 (Step 9 完了時: 全 ViewModel DI 化＆UI 結合確認) 【計画】**:
   - **対象**: 全 29 箇所の ViewModel 直接 new の全廃、DI コンテナ / Factory 経由の UI バインディング
   - **検証内容**: 全画面（メイン、Controllers、Profiles、Special Actions、Settings、Log）のデータバインディング、プロファイル編集画面のダイアログ表示、画面遷移、直接 new の残存ゼロ確認。
4. **Checkpoint 4 (Step 10 完了時: Phase 4 最終総合 E2E 検証) 【計画】**:
   - **対象**: 過渡期シム整理後の DS4Windows 全体
   - **検証内容**: 総合 E2E テスト（長時間ゲームプレイ、プロファイル切替、マクロ再生、複数コントローラー接続、スリープ復帰、エラーログゼロ確認）。
