# フェーズ4-Step10-2-C 計画書: Legacy 経路残存の整理と段階移行

作成日: 2026-09-02
対象ブランチ: `For-DI-migration-work`
関連文書:

- `docs-forDIMG/DI-App-Wide-Migration-Plan.md`
- `docs-forDIMG/MadeByAgent/Phase4-Plan.md`
- `docs-forDIMG/MadeByAgent/Phase4-Step10-2-B-Plan.md`
- `docs-forDIMG/MadeByAgent/Phase4-Step10-2-C-Legacy-Inventory-Report.md`

## 0. Step10 の作業階層

Phase4 の Step10 は、目的の異なる作業を次の単位に分けて管理する。

| 区分 | 内容 | 本書との関係 |
|---|---|---|
| Step10-1 | `[DI]`／`[Legacy]` Trace ログ整備 | Legacy 経路を判別する共通基盤。本書の C-5 と連携する |
| Step10-2-A | `Global` シム接続拡張 | `IProfileSettingsService` 等へのシム接続。完了済み |
| Step10-2-B | 呼び出し元の DI 直接参照化 | `ProfileSettingsViewModel`、`ProfileEditor`、`ControlService`、`Mapping` を対象。完了済み |
| **Step10-2-C** | **Legacy 経路残存の整理と段階移行** | **本書の対象。C-0〜C-8 で実施する** |

本書では、Step10-2-C の実装・検証段階を `C-0` から `C-8` まで分けて追跡する。C-6 は CP4 前の自動テスト化、C-7 は CP4 実機検証、C-8 は CP4 後のフォールバック削除判断を担当する。

### 現在の実施状況

| 段階 | 状態 | 備考 |
|---|---|---|
| C-0 | **完了** | Legacy 残存量、対象判定ルール、判断事項を調査・文書化済み |
| C-1 | **実装完了・検証待ち** | 旧 Provider 構築削除、Action 登録統合、Provider 一本化を実装済み。ユーザー側検証待ち |
| C-2 | **実装完了・検証待ち** | `ControlService` の Singleton 登録、AppHost 解決、parser 注入、`rootHub` 互換代入を実装済み。ユーザー側検証待ち |
| C-3 | **完了（自動ビルド・主要実機確認済み）** | 専用プロファイル適用・復帰経路を実装し、主要実機確認を完了。詳細は `Phase4-Step10-2-C-3-Completion-Report.md` |
| C-4 | **完了（実機ログ確認済み）** | ViewModelフォールバック5箇所に画面名・ViewModel名付きの`[Legacy]`ログを追加し、通常利用時の出力を確認 |
| C-5-1 | **完了（実機確認済み）** | `linkedProfileCheck`の呼び出し元をDI APIへ移行し、対象Legacy getterログが出ないことを確認 |
| C-5-2 | **計画作成済み・未着手** | `tempprofilename`／`useTempProfile`の呼び出し元をDI APIへ移行する |
| C-5-3〜C-8 | **未着手** | C-5-3以降でGlobalシムのログ監査、CP4前自動テスト化、CP4実機検証を実施する |

本書に記載された採用方針や分類は、実装を開始するための計画上の決定であり、実装完了を意味しない。各段階はコード変更、検証、コミット・リモート反映が完了した時点で個別に完了と判定する。

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

### 3.3 今回確定した分類

判断保留としていた4項目について、次の方針を採用する。実装時はこの分類を初期方針とし、既存動作を維持できない具体的な問題が見つかった場合だけ、該当項目単位で再確認する。

| 項目 | 採用方針 | 補足 |
|---|---|---|
| `AutoProfileChecker.cs` | **C-1** | `IDeviceStateAccessor`、`IProfileSwitcher`、`IProfileRepository` 等へ責務別に分割して注入する。接続監視・判定・切替実行を一つの `ControlService` 依存にまとめない |
| `PresetOption.cs` | **C-2 から開始** | 既存のプリセット適用とデバイス反映の挙動を維持するため、初期移行は `ControlService` 注入とする。安定後に `IProfilePresetService` 等の C-1 へ再評価する |
| `MainWindow.xaml.cs` のプロファイル適用 | **C-1** | 短期は専用 `IProfileApplicationService` へ適用手順を移す。将来は `IManualProfileApplicationService` 等へ整理する |
| `App.rootHub` と `Program.rootHub` の併存 | **C-1/C-2 の分類対象外** | 呼び出し元ごとに上記分類を適用し、互換代入自体は CP4 完了まで維持する。DI 解決インスタンスとの同一性を検証する |

### 3.4 判断保留として残すもの

次の項目は、今回の4項目とは別に、実装箇所と責務を確認してから決定する。

- `SpecialActionEditor` 等の固定引数 ViewModel 群を個別 Factory にするか、用途別の統合 Factory にするか
- Legacy Trace ログを全シムへ追加する際の高頻度アクセス抑制方法

## 3.5 短期方針と将来の移行先

今回の判断では、まず既存機能を壊さずに移行を進める短期方式を採用する。同時に、短期方式が将来の最終形にならない項目については、次の移行先を明示して後続作業へ引き継ぐ。

| 対象 | 短期の実装方式 | 短期方式の理由 | 将来の推奨移行先 |
|---|---|---|---|
| C-1 契約範囲 | `IDeviceStateAccessor.GetController(int)` を基本契約とする | 実行時経路の依存を最小化し、入力ループへの影響を抑える | 状態取得、TouchPad 操作などを用途別の小さなアクセサへ分離 |
| UI の `ControlService` 依存 | まず `ControlService` を注入する | 既存の UI 操作とデバイス反映の順序を保ちやすい | `IControllerInteractionService` 等の画面用インターフェースへ分離 |
| `AutoProfileChecker` | `IDeviceStateAccessor`、`IProfileRepository`、`IProfileSwitcher` へ分割注入 | 判定と実行を分離し、ControlService 全体への依存を避ける | 自動プロファイル専用サービスが必要になった時点で `IAutoProfileService` を検討 |
| `PresetOption` | `ControlService` 注入から開始 | Blank／Default 適用時の既存デバイス反映を維持する | `IProfilePresetService` 等へ移し、UI から ControlService を隠す |
| MainWindow のプロファイル適用 | 専用 `IProfileApplicationService` を新設する | `Global.ApplyProfile` の副作用を専用サービス内で短期的に保持する | 手動適用手順を `IManualProfileApplicationService` に集約 |
| `rootHub` 互換代入 | CP4 完了まで `App.rootHub`／`Program.rootHub` を維持 | 既存呼び出し元の段階移行とロールバックを可能にする | 全呼び出し元移行後に互換代入を削除するか別途判断 |
| `[DI]`／`[Legacy]` ログ | 通常操作は入口単位、フォールバック・失敗・重要変更は必須 | 高頻度ログを避けつつ経路を判別できる | CP4 後にログ実績を確認し、不要な Legacy ログを整理 |

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
- 短期方式で `ControlService` を注入した UI は、将来 `IControllerInteractionService` 等へ分離する候補として記録する

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

### C-0: 現状基準の固定（Step10-2-C-0）

- 本書と Legacy 残存調査報告書を基準版として保存する
- 生の検索件数と対象判定済み件数を分けて記録する
- `Global`、`rootHub`、ViewModel 直接生成、DI 構築の検索コマンドを固定する

完了条件:

- 調査対象と分類ルールが文書化されている
- 判断保留項目が明示されている

### C-1: Composition Root 一本化（Step10-2-C-1）

- `App.xaml.cs` の旧 Action 登録を `ServiceRegistration.cs` へ統合する
- 旧 `ServiceCollection` と旧 Provider の生成を削除する
- `AppHost` を唯一の Composition Root とする
- 起動順序、Action 登録、Singleton 共有を確認する

完了条件:

- `App.xaml.cs` の `new ServiceCollection()` が 0 件
- `AppHost.CreateHost()` が唯一の Host 構築経路
- Action／Profile／Settings サービスが同一 Provider から解決される

### C-2: `ControlService` DI 登録と互換代入（Step10-2-C-2）

- `ControlService` を Singleton 登録する
- AppHost から解決する
- `App.rootHub`／`Program.rootHub` への代入を維持する
- 起動・停止・終了時のスレッドとイベントを検証する

完了条件:

- App 側の通常経路に `new ControlService(...)` がない
- DI 解決したインスタンスと `rootHub` が同一である
- 終了時にバックグラウンド処理が残らない

### C-3: `rootHub` 呼び出し元の分類と個別移行（Step10-2-C-3）

プロファイル適用・復帰については、既存 `IProfileSwitcher` の拡張ではなく、専用 `IProfileApplicationService` を新設する。詳細な作業単位は `Phase4-Step10-2-C-3-Plan.md` を正本とする。現在は計画完了・実装前である。

各呼び出し元を、次の台帳で管理する。

| 呼び出し元 | 使用メンバー | 呼出頻度 | 分類候補 | 方針 | 状態 |
|---|---|---:|---|---|---|
| `Mapping.cs` | コントローラー取得・状態参照 | 高 | C-1 | `IDeviceStateAccessor` | 要実装 |
| `ProfileEditor.xaml.cs` | 入力停止、状態取得、出力操作 | UI操作 | C-2 | 短期は `ControlService` 注入。将来は画面用操作サービスへ分離 | 方針確定 |
| `MainWindow.xaml.cs` | 開始停止、状態、出力スロット | UI操作 | C-2候補 | 画面用依存を整理 | 要判断 |
| `MainWindow.xaml.cs` のプロファイル適用箇所 | プロファイル適用、通知、デバイス停止 | UI操作 | C-1 | `IProfileSwitcher`／`IProfileRepository` 等へ責務分割 | 方針確定 |
| `AutoProfileChecker.cs` | 自動切替、接続状態 | 常駐処理 | C-1 | `IDeviceStateAccessor`、`IProfileSwitcher`、`IProfileRepository` 等へ責務分割 | 方針確定 |
| `PresetOption.cs` | Blank／Default プロファイル生成とデバイス操作 | UI操作 | C-2開始 | 短期は `ControlService` 注入。将来は `IProfilePresetService` へ移行 | 方針確定 |
| `DS4Sixaxis.cs` | TouchPad／ControlService状態 | 高 | C-1候補 | 最小アクセサを確認 | 要判断 |
| `App.rootHub` と `Program.rootHub` の併存 | 互換代入と正規参照の管理 | 横断 | 分類対象外 | 呼び出し元ごとの C-1/C-2 を適用し、CP4 まで互換代入を維持 | 方針確定 |

完了条件:

- 各呼び出し元に C-1／C-2／分類対象外の判定理由がある
- 今回確定した4項目は承認済み方針に沿って実装する
- `Mapping` の高頻度経路で毎回 DI 解決しない

### C-4: ViewModel フォールバックの可視化（Step10-2-C-4）

- Factory 解決失敗時に `[Legacy]` Trace ログを追加する
- 全画面を起動し、通常時に DI 経路が選ばれることを確認する
- フォールバック使用件数を CP4 の確認項目へ追加する

完了条件:

- フォールバック使用時の画面名・ViewModel 名がログで判別できる
- 通常起動で予期しないフォールバックがない

### C-5: Legacy シムのログ網羅性監査（Step10-2-C-5）

- `Global` の設定シム、Repository シム、Factory フォールバックを一覧化する
- 高頻度 getter にはログを追加せず、必要な入口・変更操作だけを記録する
- `[DI]` と `[Legacy]` の表記を統一する

#### C-5-1: `linkedProfileCheck` 呼び出し元のDI直接参照化

`linkedProfileCheck` は、保持データ自体は `ProfileSettingsService` に移行済みだが、呼び出し元の一部が `Global.linkedProfileCheck` 配列を参照する過渡期シム状態である。配列プロパティのgetterは要素参照だけでも呼び出されるため、接続・切断・プロファイル再構築時に同じLegacyログが出力される。

移行対象は次の呼び出し元とする。

| 呼び出し元 | 現在の参照 | 移行先 | 方針 |
|---|---|---|---|
| `ControlService` | `Global.linkedProfileCheck[index]` | 注入した`IProfileSettingsService` | 接続時の取得・設定を`GetLinkedProfileCheck`／`SetLinkedProfileCheck`へ置換 |
| `ControllerListViewModel` | `Global.linkedProfileCheck[devIndex]` | 注入した`IProfileSettingsService` | UI getter/setterとリンク状態判定をサービスAPIへ置換 |
| `ScpUtil`内部の保存・参照処理 | `Global.linkedProfileCheck[i]` | `ProfileSettingsServiceInstance`または専用API | 保存処理の責務を維持し、Global配列getterを直接通さない |

実施順序:

1. C-5-1開始前に参照箇所と既存の接続・切断・保存動作を固定する。
2. `IProfileSettingsService` の既存 `GetLinkedProfileCheck`／`SetLinkedProfileCheck` 契約を使用する。
3. `ControllerListViewModel` と`ControlService`へ必要なサービスをコンストラクタ注入する。
4. `ScpUtil`の内部参照は、既存のサービスインスタンス境界を利用して直接参照へ置換する。
5. `Global.linkedProfileCheck` getterは互換用に残すが、高頻度getterの`[Legacy]`ログは出力しない。setter、変更操作、DI解決失敗は必要な監査ログを維持する。
6. 接続、切断、再接続、リンク設定変更、プロファイル保存で値と通知が変わらないことを検証する。

実装方針の確定事項:

- `ControlService` は既存の`IProfileSettingsService _profileSettings`を使用し、追加の依存注入は行わない。
- `ControllerListViewModel` は`IProfileSettingsService`をコンストラクターから受け取り、`MainWindow`の生成時にAppHostから渡す。既存の直接生成・テスト互換性のため、移行期間中はnull時のシムフォールバックを許可する。
- `ScpUtil`は既存の`ProfileSettingsServiceInstance.GetLinkedProfileCheck`を使用し、配列プロパティgetterを通さない。
- `Global.linkedProfileCheck`のgetterは互換シムとして残すが、getter内部の高頻度TraceログとGUIログは削除する。setterの変更監査ログは維持する。

移行時の制約:

- 配列の直接公開を新しいDI APIとして拡張しない。
- getterログを単に削除するだけで呼び出し元移行済みとは扱わない。
- `ProfileSettingsService`の既存変更通知、LinkedProfiles.xmlの保存、UIバインディングの挙動を維持する。
- `Global`シムの削除は、全呼び出し元の移行と実機確認が完了した後に別判断とする。

完了条件:

- 監査対象シムごとにログ有無と理由が記録される
- 実機接続時に高頻度ログが連発しない
- `linkedProfileCheck`の対象呼び出し元がDIサービスAPIを直接使用している
- 接続・切断・再接続・リンク設定変更・プロファイル保存の既存動作が維持される
- `Global.linkedProfileCheck`は互換シムとして残し、削除判断をC-8へ引き継ぐ

#### C-5-2: `tempprofilename`／`useTempProfile` 呼び出し元のDI直接参照化

`tempprofilename` と `useTempProfile` は、DI契約（`GetTempProfileName`／`SetTempProfileName`、`GetUseTempProfile`／`SetUseTempProfile`）とサービス実装が存在する一方、プロファイル適用、復帰、自動プロファイル、再接続、UI表示の一部がGlobal配列シムを参照している。C-5-2では、プロファイル状態遷移を維持したまま、すべての実行コードをデバイス単位DI APIへ置換する。

詳細な参照基準と実施手順は `Phase4-Step10-2-C-5-2-TempProfile-State-Reference-Baseline.md` を正本とする。

移行対象:

- `ScpUtil.cs`: `LoadProfile`／`LoadTempProfile` およびBlank／Defaultロード後の状態更新
- `Mapping.cs`: Profile Actionの抑制判定、ログ、`prevProfileName`／`prevProfileWasTemporary`保存
- `ControlService.cs`: 接続・再接続時の自動プロファイル判定
- `AutoProfileChecker.cs`: 自動プロファイルの比較、解除、状態ログ
- `MainWindow.xaml.cs`: 現在の一時プロファイル名表示・状態取得

実施順序:

1. `ScpUtil`の状態更新を`Set...` APIへ置換する。
2. `Mapping`、`ControlService`、`AutoProfileChecker`、`MainWindow`の読み取りを`Get...` APIへ置換する。
3. Global getterの高頻度Legacyログを抑制し、setterは互換監査用に残す。
4. 通常／一時適用、復帰、自動プロファイル、再接続、切断の状態遷移を確認する。

完了条件:

- 実行コードから`Global.tempprofilename`と`Global.useTempProfile`の参照がなくなる。
- 両Globalシムは互換定義としてのみ残る。
- 一時プロファイル適用時、復帰時、再接続時の状態が従来どおり維持される。
- Actions／Standaloneのビルド・テストと実機確認が完了する。

### C-6: CP4 前自動テスト化判定・実装・実行（Step10-2-C-6）

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

### C-7: CP4 実機検証（Step10-2-C-7）

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

### C-8: CP4 後のフォールバック削除判断（Step10-2-C-8）

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

## 7. 実装前に確認する事項

4項目の分類方針は確定したため、次は実装単位ごとに使用メンバーと変更範囲を確認する。以下は分類を再決定するためではなく、具体的な契約範囲と副作用を確認するための項目である。

### 判断が必要な箇所での進行ルール

実装方法が複数あり、既存動作・責務境界・テスト容易性に影響する箇所では、コード変更の前に次の内容を提示し、ユーザーの判断を得る。

1. 対象箇所と現在の処理内容
2. 選択可能な実装方法
3. 各方法のメリットとデメリット
4. 既存機能、ログ、性能、テスト、将来の移行先への影響
5. 推奨案と、その推奨理由
6. 採用後の検証方法と、必要な場合の戻し方

判断をいただくまでは、該当箇所の実装変更を行わない。単純な機械的置換や、すでに承認された方針をそのまま適用する作業は、対象範囲と検証方法を説明したうえで進める。

- `Mapping.cs` の `Program.rootHub` から `IDeviceStateAccessor` へ切り出す具体的メンバー
- `AutoProfileChecker` の `IDeviceStateAccessor`／`IProfileSwitcher`／`IProfileRepository` の責務分担
- `PresetOption` に注入する `ControlService` の機能範囲と、将来の専用サービス移行条件
- `MainWindow` のプロファイル適用で使用する `IProfileSwitcher`／`IProfileRepository` の契約範囲
- Legacy ログを入口だけにする対象と、変更操作ログを必須にする対象

## 8. 完了条件

- Phase4 対象の Legacy 経路が C-1〜C-5 のいずれかで整理され、自動テスト化対象は C-6 で検証されている
- `linkedProfileCheck`の呼び出し元がC-5-1でDIサービスAPIへ移行されている
- 旧 Composition Root が削除され、AppHost が唯一の構築経路になっている
- `ControlService` が DI Singleton として解決され、互換 `rootHub` 代入が維持されている
- `Mapping` の高頻度経路で毎回 DI 解決していない
- ViewModel フォールバックが CP4 まで可視化された状態で維持されている
- C-6 の自動テスト化判定、テスト実装、テスト実行が完了し、CP4 から除外した項目が明示されている
- CP4 で主要機能、長時間動作、ログ経路を確認している
- CP4 後にフォールバック削除の可否を別判断できる

本計画で分類を確定した項目は実装へ進める。新たに複数方式が発生した場合は、実装前に対象・影響範囲・代替案を提示して確認する。
