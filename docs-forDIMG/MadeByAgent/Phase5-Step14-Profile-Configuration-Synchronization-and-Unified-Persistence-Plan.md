# Phase 5 - Step 14: Profile Configuration Synchronization and Unified Persistence Plan

## 1. 概要 (Overview)
本ドキュメントは、プロファイル設定画面（`ProfileEditor`）における一部の設定項目（`ProfileActions` やネストされたサブ設定オブジェクトなど）の変更値がプロファイル設定ファイル（XML）へ正しくシリアライズ・保存されない不具合に対する、根本型かつ統一的な解決に向けた全体設計・実装計画である。

従来の「各UIコントロールが個別に設定を更新し、シリアライズ対象モデルとの同期が漏れる」という構造的課題を解消し、DI化移行後のアーキテクチャ（レイヤーアーキテクチャ）に完全に合致した一貫性のあるデータフローを構築することを目的とする。

---

## 2. 課題の背景と原因 (Background & Problem Statement)
プロファイル編集画面の一部の設定変更（例: スペシャルアクションの有効/無効チェック、ネストされた詳細設定など）が保存されない原因は以下の通りである。

* **データ同期・バインディングの欠損**: UI上でリストやネストプロパティが変更された際、XMLシリアライズの対象となる結合文字列（例: `ProfileActions` のスラッシュ区切りなど）や親モデルへの変更通知（Dirtyフラグの伝搬）が自動で行われていない。
* **責務の分散**: 個別のUIビハインドコード（`ProfileEditor.xaml.cs`）やコントロール固有のイベントに処理が依存しており、永続化レイヤーとUI層の間で状態の乖離が発生している。

---

## 3. 対象となる設定項目一覧 (Target Settings Categories)
今回の不具合が顕現している項目および、同種の構造的リスク（同期漏れ・ネスト非連動）を抱えている全 9 カテゴリを統一管理の対象とする。

1. **`ProfileActions`** (Special Actions タブ: 有効/無効状態・結合文字列)
2. **`GyroControlsSettings`** (Gyro タブ: ネストされたトリガー条件等)
3. **`TouchpadAbsMouseSettings`** (Touchpad タブ: 絶対マウス領域・中央リセット等)
4. **`LSOutputSettings` / `RSOutputSettings`** (FlickStick 設定: サブオブジェクト群)
5. **`Sensitivity`** (Sensitivities タブ: パイプ区切りの感度配列)
6. **`LSAxialDeadOptions` / `RSAxialDeadOptions`** (Axial Deadzone タブ: 軸別デッドゾーン設定)
7. **`GyroMouseSmoothingSettings` / `GyroMouseStickSmoothingSettings`** (Gyro タブ: スムージング詳細設定)
8. **`LSDeltaAccelSettings` / `RSDeltaAccelSettings`** (Stick Acceleration タブ: 加速度詳細設定)
9. **`DualSenseControllerSettings.RumbleSettings`** (DualSense タブ: 固有のハプティック/ランブル設定)

---

## 4. 統一設計アーキテクチャ (Unified Architecture Design)
理想のDI化移行後（`DI-App-Wide-Migration-Plan.md`）のレイヤー構造に基づき、すべての設定項目が以下のフローで透過的に処理される仕組みを導入する。

* **UI Layer (`ProfileEditor` / ViewModel)**
  * ユーザーの入力を受け付け、バインドされたプロファイルデータコンテナ（`ProfilePropHandler` 等）の状態を更新する。
* **Service / Model Layer (同期・変更通知機構)**
  * **自動双方向マッピング**: 特殊なフォーマット（`ProfileActions` や `Sensitivity` の区切り文字文字列など）は、UI側のコレクション操作と連動してリアルタイムに自動再構築される仕組みを導入する。
  * **変更通知のバブリング (Bubble-up Notification)**: ネストされたサブオブジェクト（例: `AxialDeadOptions`, `GyroControlsSettings` 等）のプロパティ変更が起きた際、親のプロファイルコンテナへ自動的に変更通知が伝搬し、常に最新の「Dirty（保存要）」状態が維持されるようにする。
* **Persistence Layer (Repository / XML Serialization)**
  * 保存処理が実行された際、全項目が漏れなくシリアライズ対象としてXML要素へ書き出されることを保証する。

---

## 5. 段階的実装計画 (Step-by-Step Implementation Plan)

### Step 1: サブオブジェクトの変更検知バブリング基盤の整備
* ネストされた設定クラス群に共通の変更通知メカニズムを適用し、子要素の変更が親プロファイルモデルへ確実に伝達されるようにする。

### Step 2: 特殊コレクション・文字列の自動同期ハンドラーの導入
* `ProfileActions` や `Sensitivity` などの複合文字列データを管理するセッター／同期ヘルパーを定義し、UI操作からの保存漏れを構造的に根絶する。

### Step 3: `ProfileEditor` のバインディング処理の整理と集約
* UIイベントハンドラーに散らばっていた個別ロジックを整理し、統一されたモデル更新フローへ統合する。

### Step 4: ユニットテストによる品質担保 (Unit Testing)
* 各種設定（特に `ProfileActions` やネスト設定）を変更した上で保存処理をシミュレートし、生成される XML に期待値が正しく書き出されているかを検証する自動テストコードを `DS4Windows.Actions.Tests` に追加する。

---

## 6. 期待される効果 (Expected Benefits)
* **網羅性と堅牢性**: 今回問題となった項目だけでなく、将来追加されるすべての設定項目に対しても一貫した保存・読込の信頼性を担保できる。
* **保守性の向上**: DI移行計画の思想（責務の分離と明確なレイヤー依存関係）に合致し、コードの複雑性を大幅に軽減する。