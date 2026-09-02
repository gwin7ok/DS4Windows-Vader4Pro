# フェーズ4-Step10-2-C 計画書: Legacy 経路残存の整理と段階移行

作成日: 2026-09-02
対象ブランチ: `For-DI-migration-work`
関連文書:

- `docs-forDIMG/DI-App-Wide-Migration-Plan.md`
- `docs-forDIMG/MadeByAgent/Phase4-Plan.md`
- `docs-forDIMG/MadeByAgent/Phase4-Step10-2-B-Plan.md`
- `docs-forDIMG/MadeByAgent/Phase4-Step10-2-C-Legacy-Inventory-Report.md`

## 1. 目的

Stage2-B までの実装と検証を踏まえ、Phase4 の計画上は DI 化されているべきでありながら、現在も Legacy 経路に残っている箇所を整理する。

この作業の目的は、`Global` や `rootHub` の文字列件数を機械的にゼロにすることではない。各参照を次のいずれかに分類し、移行すべき Legacy 経路と、現時点で残してよい互換・対象外経路を明確にすることである。

- **Phase4 で移行するもの**: 設定、プロファイル、ViewModel、Composition Root など、計画書が DI 化を明示しているもの
- **別フェーズで扱うもの**: KBM 出力、プロセス起動、デバイス状態など、全体計画上の別サービス境界に属するもの
- **対象外として維持するもの**: 定数、純粋関数、仕様上直接呼び出す単純処理など
- **判断保留**: 複数の移行方法があり、責務境界または既存動作への影響を確認してから決定するもの

## 2. 現時点の残存量

### 2.1 生の検索件数

現在のソースを横断検索した結果は次のとおりである。

| 検索対象 | 生の確認件数 | ファイル数 | この数字の意味 |
|---|---:|---:|---|
| `Global.`、`rootHub`、ViewModel直接生成、DI構築等 | 約1,001件 | 76ファイル | コメント、定数、互換シム、対象外処理を含む全体量 |
| `App.rootHub`／`Program.rootHub` | 約140件 | 17ファイル | App／Program の静的シングルトン依存の全体量 |
| `new XxxViewModel(...)` | 31件 | 16ファイル | Factory 内の正規生成と互換フォールバックを含む |
| 旧 `ServiceCollection` 構築 | 1系統 | `App.xaml.cs` | 正式 `AppHost` と併存する旧 Composition Root |

### 2.2 生の件数をそのまま Phase4 残存件数にしない理由

たとえば `Global.MAX_DS4_CONTROLLER_COUNT` は単なる配列サイズ定数であり、設定サービスへ移す対象ではない。また `Global.Clamp` は純粋な計算関数であり、DI 化しても設計上の利益がない。

同様に、Factory 内の `new ProfileSettingsViewModel(...)` は、実行時引数と DI サービスを合成するための正規生成処理である。これは View が Legacy 経路で直接生成しているケースとは意味が異なる。

したがって本計画では、上記の生の件数とは別に、**Phase4 対象として残っている経路を分類単位で管理する**。

## 3. Phase4 対象判定ルール

### 3.1 Phase4 対象

次の条件に該当する場合は Phase4 対象として扱う。

- `IProfileSettingsService`、`IProfileRepository` 等の既存 DI 契約があり、呼び出し元が `Global` を直接参照している
- ViewModel が View や App 起動処理から直接生成され、既存の Factory／DI 登録で置き換えられる
- `App.xaml.cs` が旧 `ServiceCollection` を構築し、正式な `AppHost` と別の Provider を作っている
- `ControlService` が DI 登録されず、App 側で直接生成されている
- Phase4 計画が `IDeviceStateAccessor` 等の最小インターフェース化を明示している
- DI 経路が存在するにもかかわらず、呼び出し元が常に Legacy 経路を使用している

### 3.2 現時点では対象外または別フェーズ

次の参照は、文字列上は Legacy に見えても、Step10-2-C で機械的に移行しない。

- `Global.MAX_DS4_CONTROLLER_COUNT` 等の定数
- `Global.Clamp` 等の純粋関数
- KBM 出力ハンドラ、プロセス起動、座標変換など別サービス境界の処理
- `Mapping` の static 状態そのもの
- Factory 内で実行時引数付き ViewModel を生成する `new`
- CP4 まで維持する互換フォールバック

ただし、対象外とした理由と、将来の担当フェーズは監査報告に残す。

### 3.3 判断保留

次の項目は複数の実装方法があり、承認なしに一方へ決め打ちしない。

- `rootHub` を C-1 最小アクセサ方式で移すか、C-2 `ControlService` 注入方式で移すか
- `SpecialActionEditor` 等の固定引数 ViewModel 群を個別 Factory にするか、用途別の統合 Factory にするか
- `AutoProfileChecker` のプロファイル切替、デバイス状態、アプリ状態をどのサービス境界へ分けるか
- Legacy Trace ログを全シムへ追加する際の高頻度アクセス抑制方法

## 4. 採用方針

### 4.1 Composition Root

旧 `ServiceCollection` の構築を削除し、Action 系を含む登録を `AppHost`／`ServiceRegistration` に統合する。

- DI の Provider は 1 つにする
- Singleton サービスが UI とバックエンドで共有される状態を保証する
- `AppHost.CreateHost()` の起動順序を維持する
- 旧 Provider 依存箇所は、統合後の Provider へ段階的に切り替える

### 4.2 `ControlService`

`ControlService` を Singleton として DI 登録し、AppHost から解決する。

- App 側の `new ControlService(...)` を DI 解決へ置き換える
- 現在の専用スレッド生成タイミングを維持する
- 解決後も `App.rootHub`／`Program.rootHub` へ互換代入する
- 終了時の `Stop`、イベント解除、`Dispose` の有無を確認してから実装する
- 直接生成フォールバックは、移行期間中の起動障害対策として必要性を確認する

### 4.3 `rootHub` の分類方針

#### C-1: 最小アクセサ方式

実行時経路、特に `Mapping`、`DS4Library`、低レイヤ処理には C-1 を優先する。

例:

```csharp
public interface IDeviceStateAccessor
{
    DS4Device GetController(int deviceIndex);
}
```

判断基準:

- 呼び出し元が必要とする機能が 1〜数個に限定される
- 高頻度ループである
- `ControlService` 全体へ依存させると循環依存が強まる
- テスト時に小さなモックで代替したい

#### C-2: `ControlService` 注入方式

UI や、複数の ControlService 機能を同時に必要とする画面には C-2 を候補とする。

判断基準:

- コントローラー状態、出力スロット、操作イベントなど複数機能を使用する
- 最小アクセサを多数作るより、画面用の明確な依存としてまとめた方が理解しやすい
- UI のライフサイクルと `ControlService` の生成順序を保証できる

#### C-1／C-2 の適用ルール

- 同一呼び出し元へ C-1 と C-2 を同時導入しない
- まず呼び出し元ごとに必要メンバーを列挙する
- 実行時経路は C-1 を第一候補、UI は C-2 を第一候補とする
- 境界が不明確な場合は、対象ファイル、使用メンバー、呼出頻度、循環依存の有無を記載して確認する

### 4.4 ViewModel フォールバック

CP4 の実機検証が完了するまでは、ViewModel の互換フォールバックを残す。

```csharp
var factory = AppHost.GetService<IViewModelFactory>();
viewModel = factory != null
    ? factory.Create(...)
    : new SomeViewModel(...);
```

ただし、フォールバックは黙って使用しない。

- フォールバック使用時に `[Legacy]` Trace ログを出す
- DI 解決失敗の画面名と ViewModel 名を記録する
- CP4 では全画面で DI 経路が選択されたことを確認する
- CP4 完了後、フォールバック削除専用の変更として再評価する

## 5. 実施フェーズ

### C-0: 現状基準の固定

- 本書と Legacy 残存調査報告書を基準版として保存する
- 生の検索件数と対象判定済み件数を分けて記録する
- `Global`、`rootHub`、ViewModel 直接生成、DI 構築の検索コマンドを固定する

完了条件:

- 調査対象と分類ルールが文書化されている
- 判断保留項目が明示されている

### C-1: Composition Root 一本化

- `App.xaml.cs` の旧 Action 登録を `ServiceRegistration.cs` へ統合する
- 旧 `ServiceCollection` と旧 Provider の生成を削除する
- `AppHost` を唯一の Composition Root とする
- 起動順序、Action 登録、Singleton 共有を確認する

完了条件:

- `App.xaml.cs` の `new ServiceCollection()` が 0 件
- `AppHost.CreateHost()` が唯一の Host 構築経路
- Action／Profile／Settings サービスが同一 Provider から解決される

### C-2: `ControlService` DI 登録と互換代入

- `ControlService` を Singleton 登録する
- AppHost から解決する
- `App.rootHub`／`Program.rootHub` への代入を維持する
- 起動・停止・終了時のスレッドとイベントを検証する

完了条件:

- App 側の通常経路に `new ControlService(...)` がない
- DI 解決したインスタンスと `rootHub` が同一である
- 終了時にバックグラウンド処理が残らない

### C-3: `rootHub` 呼び出し元の分類と個別移行

各呼び出し元を、次の台帳で管理する。

| 呼び出し元 | 使用メンバー | 呼出頻度 | 分類候補 | 方針 | 状態 |
|---|---|---:|---|---|---|
| `Mapping.cs` | コントローラー取得・状態参照 | 高 | C-1 | `IDeviceStateAccessor` | 要実装 |
| `ProfileEditor.xaml.cs` | 入力停止、状態取得、出力操作 | UI操作 | C-2候補 | 必要機能を確認 | 要判断 |
| `MainWindow.xaml.cs` | 開始停止、状態、出力スロット | UI操作 | C-2候補 | 画面用依存を整理 | 要判断 |
| `AutoProfileChecker.cs` | 自動切替、接続状態 | 常駐処理 | 判断保留 | サービス責務を分解 | 要相談 |
| `DS4Sixaxis.cs` | TouchPad／ControlService状態 | 高 | C-1候補 | 最小アクセサを確認 | 要判断 |

完了条件:

- 各呼び出し元に C-1／C-2／保留の判定理由がある
- 判断保留項目は承認後にのみ実装する
- `Mapping` の高頻度経路で毎回 DI 解決しない

### C-4: ViewModel フォールバックの可視化

- Factory 解決失敗時に `[Legacy]` Trace ログを追加する
- 全画面を起動し、通常時に DI 経路が選ばれることを確認する
- フォールバック使用件数を CP4 の確認項目へ追加する

完了条件:

- フォールバック使用時の画面名・ViewModel 名がログで判別できる
- 通常起動で予期しないフォールバックがない

### C-5: Legacy シムのログ網羅性監査

- `Global` の設定シム、Repository シム、Factory フォールバックを一覧化する
- 高頻度 getter にはログを追加せず、必要な入口・変更操作だけを記録する
- `[DI]` と `[Legacy]` の表記を統一する

完了条件:

- 監査対象シムごとにログ有無と理由が記録される
- 実機接続時に高頻度ログが連発しない

### C-6: CP4 前自動テスト化判定・実装・実行

CP4 の直前に、実機で確認予定の項目を「自動テストで代替できるか」「実機確認が必要か」に分類する段階を設ける。目的は実機確認を無条件に削減することではなく、同じ論理を何度も実機で確認する部分を自動テストへ移し、実機ではハードウェア依存部分に集中することである。

#### 自動テストへ移す候補

| CP4確認内容 | 自動テストで確認する内容 | 実機で残す確認 |
|---|---|---|
| DI サービス解決 | Singleton 同一性、必要サービスの解決、`ControlService` と設定サービスの依存関係 | 実際の起動・終了順序 |
| Composition Root | 旧 Provider が構築されないこと、Action／Profile／Settings が同一 Provider であること | 実アプリ起動 |
| Profile 操作 | プロファイル名の正規化、読込・保存・適用の引数、重複呼出し防止 | 実ファイルと UI 操作の結合 |
| ViewModel Factory | 全画面の DI 経路選択、Factory 戻り値、フォールバック使用時のログ | WPF 画面表示・画面遷移 |
| 設定変更 | `IProfileSettingsService` の get/set、配列境界、変更通知 | コントローラーへの実反映 |
| Mapping | DI キャッシュが 1 回だけ解決されること、設定値が変換処理へ渡ること | 実入力の連打・同時押し・遅延 |
| Actions／KBM | 既存モックでアクション選択、出力イベント、プロファイル切替引数を確認 | OS／ドライバへの実送出 |
| ログ | `[DI]`／`[Legacy]` の形式、フォールバック識別情報、過剰出力抑制 | 実機接続時のログ量と実経路 |

#### 自動テストにしない項目

- HID コントローラーの接続、切断、再接続
- ViGEm、キーボード／マウスドライバへの実出力
- WPF の実画面レンダリング、フォーカス、画面遷移
- スリープ復帰、長時間接続、複数実機の同時利用
- 実機固有のジャイロ、タッチパッド、ランブル、ライトバー挙動

#### 実施手順

1. CP4 チェックリストの各項目に `自動テスト`／`実機`／`両方` のタグを付ける。
2. 既存テストで代替できる項目と、新規テストが必要な項目を分ける。
3. 新規テストは `DS4Windows.Actions.Tests` または既存のテストプロジェクトへ追加する。
4. DI 経路、Legacy フォールバック、引数、イベント、重複実行をモックで検証する。
5. Actions／Standalone のテストビルドとテスト実行を行う。
6. 合格した自動テスト項目を CP4 の実機確認対象から外し、実機必須項目だけを残す。
7. テストで代替できなかった項目は、理由を CP4 チェックリストへ記録する。

#### 完了条件

- CP4 各項目に自動テスト／実機の分類がある
- 自動化対象のテストが実装され、Actions／Standalone で全件成功する
- 自動テストで代替した CP4 項目が明示されている
- 実機に残す項目が HID、WPF、ドライバ、長時間安定性などに限定されている

### C-7: CP4 実機検証

C-1〜C-6 の実装・検証後、自動テストで代替できなかった項目を中心に Phase4 最終総合 E2E 実機検証を実施する。

CP4 の対象:

- アプリ起動・終了
- コントローラー接続、切断、再接続
- 複数コントローラー
- プロファイル読込、保存、切替
- 設定変更と実機反映
- ボタン、スティック、トリガー、タッチパッド、ジャイロ
- マクロ、KBM、仮想コントローラー出力
- スリープ復帰、長時間動作
- `[DI]`／`[Legacy]` ログの判別
- ViewModel フォールバックが予期せず使用されていないこと

### C-8: CP4 後のフォールバック削除判断

CP4 完了後、フォールバック削除専用の変更として次を再評価する。

- 全画面で DI 経路が安定しているか
- 起動順序の例外がないか
- 直接 `new ViewModel` の残存が互換経路だけか
- フォールバックを削除した場合の復旧方法があるか

フォールバック削除は本計画の実装と同時には行わない。

## 6. 検証方針

各実装フェーズで次を行う。

1. エージェント: Debug x64 ビルド
2. ユーザー: Actions／Standalone テストビルド
3. ユーザー: Actions／Standalone テスト実行
4. ユーザー: 必要な実機確認
5. ユーザー: 問題がなければコミット・push
6. エージェント: 報告書と進捗表更新

各段階で、既存機能、ログ、スレッド、Singleton の同一性、起動順序を確認する。

C-6 では、テストで確認できる論理を実機検証から除外したことを、CP4 チェックリスト上で追跡可能にする。

## 7. 判断確認が必要な事項

実装開始前に、次の項目は対象ファイルと使用メンバーを提示して確認する。

- `Mapping.cs` の `Program.rootHub` を `IDeviceStateAccessor` に切り出す具体的メンバー
- UI ごとの C-2 注入範囲と、C-1 ではなく C-2 とする理由
- `AutoProfileChecker` のサービス責務境界
- 旧 Provider 削除時に Action 系登録をどこまで `ServiceRegistration` へ移すか
- Legacy ログを入口だけにする対象と、変更操作ログを必須にする対象

## 8. 完了条件

- Phase4 対象の Legacy 経路が C-1〜C-5 のいずれかで整理されている
- 旧 Composition Root が削除され、AppHost が唯一の構築経路になっている
- `ControlService` が DI Singleton として解決され、互換 `rootHub` 代入が維持されている
- `Mapping` の高頻度経路で毎回 DI 解決していない
- ViewModel フォールバックが CP4 まで可視化された状態で維持されている
- C-6 の自動テスト化判定、テスト実装、テスト実行が完了し、CP4 から除外した項目が明示されている
- CP4 で主要機能、長時間動作、ログ経路を確認している
- CP4 後にフォールバック削除の可否を別判断できる

本計画の判断保留項目は、ユーザー確認なしに実装を開始しない。
