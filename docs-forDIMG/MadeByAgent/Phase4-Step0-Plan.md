# Step0 計画書: 現状棚卸し・基準テスト

作成日: 2026-08-31
対象ブランチ: For-DI-migration-work
前提ドキュメント:
- `docs-forDIMG/DI-App-Wide-Migration-Plan.md` §3, §4, §6.6（4層モデル、DI化対象棚卸し、フェーズ4の位置づけ）
- `docs-forDIMG/MadeByAgent/Phase4-Plan.md`（Step0の定義、§0 着手前調査で判明した事実）
- `docs-forDIMG/MadeByAgent/Phase4-Status.md`（Step別進捗管理表）
- `docs-forDIMG/MadeByAgent/Phase3-Status.md`（Phase3完了記録・引継ぎ事項）
- `docs-forDIMG/MadeByAgent/Phase3-Step3-4.Followup-StepF0-Member-Audit-Report.md`（棚卸し作業の先行フォーマット）
- `.github/copilot-instructions.md`

## ルール確認（作業開始前に毎回読む）

- §2.1 修正版: 古い方式を残して移行OK。新方式の動作確認後に削除。複数候補同時実装はNG。本Stepはコード変更を行わないため直接該当しないが、後続Stepの前提として維持する。
- §2.2 機能100%維持、§2.3 ログ維持。本Stepの棚卸しはこれらを守るための基礎資料作りである。
- §3.1 コンテナ登録は `AppHost.cs`（またはそこから呼ばれる拡張）に行う。
- §3.2 巨大ファイル（`ScpUtil.cs` 内の `Global` クラス、`Mapping.cs`）はピンポイント置換のみ。全体再生成しない。本Stepでは読み取り調査のみでコード変更は行わない。
- §4: マイクロステップで進行。1ステップ完了ごとに確認を挟む。本Stepも内部でタスク0-1〜0-6に分割する。

---

## 0. Step0の位置づけ

Step0は実装作業ではなく、`Phase4-Plan.md` §2 で定義された「現状棚卸し・基準テスト」である。目的は次の3点。

1. `Global`（`ScpUtil.cs`）469メンバーの責務別分類表を作成し、`Phase4-Plan.md` §1.2 の対象サービス（`IProfileSettingsService` 等）に対応付ける。
2. ViewModel直接生成箇所を全数調査し、パターンA/B/C分類を確定する（`Phase4-Plan.md` §0.3 の「少なくとも16ファイル」は暫定値であり、正式な確定値は本Stepで作る）。
3. 移行前の `dotnet build`／`dotnet test`／主要画面起動の基準結果を記録し、以降のStepでの回帰判定の基準値とする。

**本Stepはコード変更を一切行わない。** 成果物はすべて調査記録の `.md` ファイルであり、`ScpUtil.cs`・`Mapping.cs`・DIコンテナ関連ファイルへの変更は含まない。

---

## 1. 調査範囲と方法

### 1.1 `Global` メンバーの分類（`Phase4-Plan.md` §3 Step0手順1・2に対応）

- **対象**: `DS4Windows/DS4Control/ScpUtil.cs` 内 `public class Global` の全 `public static` メンバー。
- **方法**:
  - GitHub `get_file_contents`／`search_code` で `ScpUtil.cs` を取得し、Pythonで `public static` 宣言行を正規表現抽出する。
  - 各メンバーを全体計画書 §4.1 のカテゴリ（プロファイル設定値get/set、プロファイル管理、SpecialAction管理、デバイス/コントローラ状態管理、システム環境判定、OSC/UDPサーバ設定、出力ハンドラ初期化・KBM/マウス設定、パス/ファイル位置、言語/UI/テーマ、ユーティリティ計算(純粋関数)、モニタ/座標変換、その他フラグ/設定値）へ分類する。
  - 全体計画書 §4.1 の件数目安（約100/約25/約20/約15/約10/約15/約27/約8/約6/約10/約4/約100+）と実測値を突合し、差分があれば理由とともに記録する。
- **呼び出し元抽出**: `Global.` を参照する全ファイルを `search_code` で洗い出し、全体計画書の推定値「75ファイル」を実測で検証する。ファイル単位で「主に使用するカテゴリ」を記録し、Step1以降のサービス移行時にどのファイルへ影響が及ぶかを事前に分かるようにする。

### 1.2 ViewModel直接生成の一覧化（`Phase4-Plan.md` §3 Step0手順3に対応）

- **対象**: `DS4Windows/DS4Forms` 配下のコードビハインド（`.xaml.cs`）。
- **方法**: `new XxxViewModel(` のパターンを全ファイルから検索し、以下を記録する。
  - ViewModel名、生成箇所ファイル、コンストラクタ引数
  - パターン分類（A: 引数なし／B: 共有依存（`ControlService` 等）／C: 実行時引数（`deviceNum`、`device`、`SpecialAction` 等））
  - `Phase4-Plan.md` §0.3 の暫定値、全体計画書 §4.3 の推定値（A:11／B:5／C:17）を実測で確定させる。

### 1.3 起動・解決順序の図示（`Phase4-Plan.md` §3 Step0手順4に対応）

- **対象**: `App.xaml.cs`、`AppHost.cs`、`ServiceRegistration.cs`、`ServiceProviderHolder.cs`。
- **方法**: 各ファイルの起動時呼び出し順序を時系列で書き出し、`ServiceProviderHolder`（旧・Actions系専用）と `AppHost`（新・Phase3以降）のどちらが何を解決しているかを表形式で整理する。`Phase3-Step3-6-Plan.md` §0.3 で判明済みの二重コンテナ構造を踏まえ、Step6（Composition Root一本化）で解消すべき境界を明確化する。

### 1.4 基準テスト・基準ビルドの記録（`Phase4-Plan.md` §3 Step0手順5に対応）

- `dotnet build` の結果（警告数を含む）。
- `dotnet test`（`DS4WindowsTests`、`StandaloneTests`）の結果。
- 主要画面（MainWindow、ProfileEditor、Controller関連タブ等）の起動確認。実機・GUI操作を伴う確認は、PowerShellスクリプトで実行手順を提示し、gwin7ok側で実施してもらう。
- 既存ログ出力のサンプル記録（`AppLogger.LogToGui` 等の出力例1〜2件）。

---

## 2. 成果物

| ファイル名 | 内容 |
|---|---|
| `Phase4-Step0-Global-Member-Inventory.md` | `Global` メンバー全件の分類表、呼び出し元ファイル一覧 |
| `Phase4-Step0-ViewModel-Inventory.md` | ViewModel直接生成箇所の一覧とパターンA/B/C分類 |
| `Phase4-Step0-DI-Startup-Sequence.md` | 起動・DI解決順序の図示、二重コンテナ構造の整理 |
| `Phase4-Step0-Baseline-Test-Report.md` | 基準ビルド・テスト結果、主要画面起動確認結果 |
| `Phase4-Step0-Completion-Report.md` | 本Stepの完了報告（差分サマリ、次Stepへの引継ぎ） |
| `Phase4-Status.md`（更新） | Step0を完了状態に更新 |

調査量が多いため、1ファイルに集約せず4種類の調査ドキュメント＋完了報告書に分割する。後続Stepから該当ドキュメントのみを参照しやすくするためである。

---

## 3. 作業手順（タスク分割）

| タスク | 内容 | 成果物 |
|---|---|---|
| **Step0-1** | `Global` メンバーの抽出・分類 | `Global-Member-Inventory.md`（前半） |
| **Step0-2** | `Global` 呼び出し元ファイルの一覧化（75ファイルの実測） | `Global-Member-Inventory.md`（後半に追記） |
| **Step0-3** | ViewModel直接生成箇所の抽出・パターン分類 | `ViewModel-Inventory.md` |
| **Step0-4** | 起動順序・DIコンテナ整理図の作成 | `DI-Startup-Sequence.md` |
| **Step0-5** | 基準ビルド・テストの実施、結果記録 | `Baseline-Test-Report.md` |
| **Step0-6** | `Phase4-Status.md` 更新、Step0完了報告書の作成 | `Phase4-Status.md`、`Completion-Report.md` |

各タスクはコード変更を伴わないため、通常の実装Stepで用いるCRLF/LF正規化ロジック付きピンポイント置換スクリプトは不要であり、新規 `.md` ファイルの追加のみとなる。

---

## 4. リスクと回避策

| リスク | 該当タスク | 回避策 |
|---|---|---|
| `Global` 469メンバーの手作業分類に漏れ・誤分類が生じる | Step0-1 | 全体計画書 §4.1 のカテゴリ定義を基準にし、分類できないメンバーは「要確認」欄を設けて個別記録する |
| ViewModel生成パターンの判定基準が曖昧（B/C境界の見誤り） | Step0-3 | 全体計画書 §4.3 の定義（Bは共有依存のみを受け取る／Cは画面表示時に決まる実行時パラメータを含む）に厳密に従う |
| 基準テストの実施環境がgwin7ok側のローカル環境に限定される | Step0-5 | `dotnet build`／`dotnet test` の実行はPowerShellスクリプトで提示し、gwin7ok側で実行後に結果を共有してもらう運用とする |
| 調査量が多くメッセージ切れ・コンテキスト切れで中断する | 全タスク | `copilot-instructions.md` §5-1の方針通り、タスク単位（Step0-1〜Step0-6）で区切り、途中終了しても次タスクから再開できるようにする |
| 実測値が既存の全体計画書・Phase4-Plan.mdの推定値と大きく乖離する | Step0-1〜Step0-4 | 乖離が判明した場合はコードを変更せず、差分と影響範囲だけを記録し、Phase4-Plan.mdの更新要否をgwin7okへ確認してから次Stepへ進む |

---

## 5. 完了判定基準

- [ ] `Global` 全メンバーがカテゴリ分類され、全体計画書 §4.1 の分類表との差分（件数・新カテゴリの要否）が記録されている
- [ ] `Global` 呼び出し元ファイル数が実測され、既存推定値（75ファイル）との差分が記録されている
- [ ] `new XxxViewModel(` の全箇所がパターンA/B/Cに分類され、既存推定値（A:11／B:5／C:17）との差分が記録されている
- [ ] `App.xaml.cs`／`AppHost.cs`／`ServiceRegistration.cs`／`ServiceProviderHolder.cs` の起動・解決順序が図示され、二重コンテナの範囲が明確化されている
- [ ] 移行前の `dotnet build`／`dotnet test` の結果が記録されている
- [ ] `Phase4-Status.md` のStep0行が「完了」に更新されている
- [ ] `Phase4-Step0-Completion-Report.md` が作成されている

---

## 6. 次のアクション

1. 本計画書の確認をgwin7okから得る。
2. 承認後、タスクStep0-1（`Global` メンバー抽出・分類）から着手する。
3. 各タスク完了ごとに区切り、途中経過を `Phase4-Status.md` へ随時反映する（メッセージ切れによる中断への対応）。
4. タスクStep0-6完了後、Step1（`IProfileSettingsService` 実装化）への着手可否をgwin7okと確認する。
