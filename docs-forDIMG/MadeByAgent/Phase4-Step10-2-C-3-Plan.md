# フェーズ4-Step10-2-C-3 計画書: 専用プロファイル実行サービスによる rootHub 依存整理

作成日: 2026-09-02
対象ブランチ: `For-DI-migration-work`
関連文書:

- `docs-forDIMG/DI-App-Wide-Migration-Plan.md`
- `docs-forDIMG/MadeByAgent/Phase4-Plan.md`
- `docs-forDIMG/MadeByAgent/Phase4-Step10-2-C-Plan.md`
- `docs-forDIMG/MadeByAgent/Phase4-Step10-2-C-3-RootHub-Classification-Report.md`

## 0. 現在の進捗

| 段階 | 状態 | 備考 |
|---|---|---|
| C-3-1 | **完了** | 対象メソッド、分岐、非同期、停止、復帰条件を固定 |
| C-3-2 | **実装完了・Actions検証済み** | `IProfileApplicationService`、`IProfileActionProvider`、`IProfileActionChainService` と DI 登録を追加 |
| C-3-3 | **実装完了・Actions検証済み** | `Mapping` の適用・復帰入口を専用サービスへ委譲 |
| C-3-4 | **実装完了・Actions検証済み** | 静的キャッシュ境界と互換状態アクセサを追加。既存 dispatch は互換境界として残存 |
| C-3-5 | **Actions検証済み・Standalone待ち** | Actions 85件成功。専用サービスの追加境界テストと Standalone 確認を継続 |
| C-3-6〜C-3-7 | **未着手** | 実機引継ぎ、完了報告 |

### 0.1 プロファイル Action の一時性修正

解除設定（`uTrigger` または `automaticUntrigger`）を持つ Profile Action は、適用時に `isTemp=true` とする。これにより `LoadTempProfile`、`useTempProfile=true`、解除時の元プロファイル復帰が同じ状態遷移になる。

解除設定のない通常プロファイル切替は `isTemp=false` のまま維持する。ActionManager 経路、Mapping のフォールバック、専用適用サービスは同じ `SpecialAction.IsTemporaryProfileAction` 判定を使用する。

一時 Profile Action の解除条件は Mapping 側で判定するが、復帰の正規入口は `ProfileSwitchAction.Stop()` とする。`Stop()` から `IProfileSwitcher`、`IProfileApplicationService` を経由して一度だけ復帰する。切替前の通常／一時状態は `UntriggerAction` に保持し、通常プロファイルの場合は `ProfilePath` を復元してからロードする。

## 1. 目的

`Mapping.cs` に残る `Program.rootHub` 直接依存と、そこから呼び出されるプロファイル適用・復帰の副作用を、専用の実行サービスへ移す。

今回は、既存 `IProfileSwitcher` に手動適用処理を追加する方式ではなく、プロファイル適用の実行責務を専用サービスへ分離する。`Mapping` は適用処理の詳細を実行せず、DI サービスへ実行を依頼する構造を目指す。

## 2. 対象範囲

### 対象

- `Mapping.ApplyProfileDirect`
- `Mapping.RestoreProfileDirect`
- 上記処理内の `Program.rootHub`、`DS4Device`、`HaltReportingRunAction` 依存
- `Global.ApplyProfile`、`LoadProfile`、`LoadTempProfile` への実行委譲境界
- プロファイル適用後の Untrigger／連鎖処理
- `[DI]`／`[Legacy]` ログと失敗時の扱い

### 対象外

- `Mapping` 全体の instance 化
- プロファイル XML の読み書き実装そのもの
- `Global` シムの即時削除
- 自動プロファイルの全体移行
- UI の `ControlService` 注入（別の C-2 作業単位）

## 3. 既存処理で維持すべき挙動

`ApplyProfileDirect` では、少なくとも次の順序と条件を維持する。

1. デバイス番号と Action の null／範囲を確認する
2. 対象コントローラーを取得する
3. コントローラーが存在しなければ何もしない
4. バッテリー情報を含む通知文を組み立てる
5. 通知設定を読み取る
6. `HaltReportingRunAction` 内でプロファイルを適用する
7. 必要な場合、同一コントロールに紐づく次の Action を再発火する
8. 既存の `Global.ApplyProfile` が担う状態更新・入力反映を維持する

`RestoreProfileDirect` では、Untrigger 状態から元プロファイル名を取り出し、通常プロファイルまたは一時プロファイルを既存ルールどおりロードする。

## 4. 採用する設計

### 4.1 新設契約

契約は `DS4Windows/DI/IProfileApplicationService.cs` に置く。短期契約では、実行時に必要な情報を引数として明示する。

```csharp
public interface IProfileApplicationService
{
    void ApplyFromAction(int deviceIndex, SpecialAction action);
    void RestoreFromAction(int deviceIndex);
}
```

この契約は `Mapping` から見た「プロファイル適用を実行する」という責務だけを表す。`Mapping` が `ControlService`、`DS4Device`、停止処理、通知処理を直接扱わないことを目的とする。

### 4.2 実装

実装は `DS4Windows/DS4Control/Services/ProfileApplicationService.cs` に置く。

注入候補:

- `IDeviceStateAccessor`: 対象コントローラー取得
- `IProfileRepository`: 通常／一時プロファイルの読み込み境界
- `IProfileSettingsService`: 通知設定、必要なプロファイル状態
- `IProfileSwitcher`: 将来の切替責務との接続候補

ただし、既存 `Global.ApplyProfile` が `ControlService` 引数を必要とするため、初回実装でその副作用を無理に分解しない。必要な場合は、短期の内部互換境界として `ControlService` を実装側で扱い、将来 `IProfileApplicationService` 内部からデバイス操作・通知・Repository をさらに分割する。

### 4.3 Mapping 側

`Mapping.ApplyProfileDirect` と `Mapping.RestoreProfileDirect` は、静的キャッシュ済みの `IProfileApplicationService` を呼ぶ薄い委譲メソッドにする。

- 入力ループ内で毎回 `AppHost.GetService` しない
- `Mapping` にプロファイル適用の副作用ロジックを残さない
- 既存の Action ディスパッチ入口とメソッド可視性を維持する
- DI サービスが解決できない場合の互換フォールバックは、CP4 までの方針に従い `[Legacy]` ログ付きで扱う

## 5. 段階的な作業計画

### C-3-1: 現行処理の基準固定

- `ApplyProfileDirect`／`RestoreProfileDirect` の分岐、非同期、停止、Untrigger、連鎖発火を一覧化する
- `Program.rootHub` から実際に使用しているメンバーを固定する
- 成功、対象デバイスなし、空の復帰プロファイル、例外の期待動作を記録する

完了条件:

- 既存挙動の比較基準が文書化されている
- 変更対象が上記2メソッドに限定されている

### C-3-2: 契約と DI 登録

- `IProfileApplicationService` を新設する
- `ProfileApplicationService` を実装する
- `ServiceRegistration.cs` に Singleton 登録する
- `AppHost` の Provider から解決できることを確認する

完了条件:

- 契約が Mapping の詳細実装を含まない
- DI 解決が可能である
- 既存の `IProfileSwitcher` の SpecialAction 経路を変更しない

### C-3-3: 適用処理の移設

- `ApplyProfileDirect` の処理を専用サービスへ移す
- `RestoreProfileDirect` の処理を専用サービスへ移す
- `HaltReportingRunAction`、通知文、`ProfileChangeSource`、一時プロファイル条件を維持する
- 切替前の通常プロファイル名と一時プロファイル状態を保存し、解除時に通常プロファイルなら `ProfilePath` を復元してからロードする
- 連鎖発火と Untrigger 状態を維持する

完了条件:

- Mapping はサービス呼出しだけになる
- 既存の処理順序と条件分岐が変わらない
- `Program.rootHub` の直接参照が対象メソッドからなくなる

### C-3-4: Mapping の委譲境界確認

- 静的サービス参照の初期化タイミングを確認する
- DI Provider 未初期化時の挙動を確認する
- 互換フォールバックを使う場合は `[Legacy]` ログを出す
- 高頻度入力経路での DI 解決・ログ連発がないことを確認する

完了条件:

- `Mapping` 内の毎回解決がない
- DI 経路と Legacy フォールバックをログで判別できる

### C-3-5: 自動テスト

`DS4Windows.Actions.Tests` に専用テストを追加する。

- 正常な Apply 委譲
- 無効なデバイス番号
- null Action
- 対象デバイスなし
- Restore の通常プロファイル復帰
- Restore の一時プロファイル復帰
- Action 連鎖発火条件
- DI サービス解決と Singleton 同一性
- DI／Legacy ログの識別

実デバイス、HID、WPF、ViGEm、実キーボード／マウス出力は自動テスト対象外とする。

完了条件:

- Actions／Standalone のテストビルド・テスト実行が全件成功する
- 既存の Action テストに回帰がない
- 自動化できない実機項目が CP4 リストに残る

### C-3-6: 実機確認への引継ぎ

自動テスト後、次を実機で確認する。

- 実際のプロファイル切替
- 一時プロファイルからの復帰
- 入力停止・再開のタイミング
- 通知表示
- 連鎖 Action の二重実行がないこと
- 接続・切断時の異常がないこと

### C-3-7: 完了報告と後続移行

- `C-3-Completion-Report.md` を作成する
- `Phase4-Status.md` を更新する
- 将来の `IManualProfileApplicationService` への移行候補と、短期実装で残した内部互換境界を記録する

## 6. 検証方針

各段階で次を実施する。

1. Debug x64 ビルド
2. Actions／Standalone テストビルド
3. Actions／Standalone テスト実行
4. 必要な実機確認
5. 問題がなければコミット・push
6. 完了報告書と進捗表更新

## 7. 実装前の判断ルール

本計画では専用実行サービス方式を採用済みであり、既存 `IProfileSwitcher` の拡張へ戻さない。実装中に次の判断が発生した場合だけ、コード変更前に確認する。

- `Global.ApplyProfile` の副作用を専用サービス内部でさらに分割するか
- `IProfileRepository` に一時プロファイル API を追加するか
- `IDeviceStateAccessor` に `DS4Device` 以外の状態を追加するか
- DI 解決失敗時に Legacy フォールバックを残す具体的方法

## 8. 完了条件

- `Mapping` の対象プロファイル適用・復帰処理が `IProfileApplicationService` 経由になっている
- 対象メソッドから `Program.rootHub` 直接参照がなくなっている
- 既存の停止、通知、Untrigger、連鎖発火、プロファイル状態更新が維持されている
- 自動テストで論理的な動作を確認している
- 実機必須項目が CP4 用に整理されている
- 短期方式と将来の `IManualProfileApplicationService` 移行先が文書化されている
