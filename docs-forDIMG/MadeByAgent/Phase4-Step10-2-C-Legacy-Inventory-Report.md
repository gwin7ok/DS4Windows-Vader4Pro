# フェーズ4-Step10-2-C Legacy 経路残存調査報告書

作成日: 2026-09-02
対象ブランチ: `For-DI-migration-work`
基準文書:

- `docs-forDIMG/DI-App-Wide-Migration-Plan.md`
- `docs-forDIMG/MadeByAgent/Phase4-Plan.md`
- `docs-forDIMG/MadeByAgent/Phase4-Step10-2-B-Plan.md`

## 1. 調査目的

Stage2 後の実機検証へ進む前に、Phase4 の計画上 DI 化されているべきでありながら、現在も Legacy 経路に残っている箇所を棚卸しする。

本調査では、単純な文字列件数を完了条件とはせず、次の分類で評価する。

- **Phase4 対象**: 計画書が DI／Factory／Composition Root への移行を明示するもの
- **対象外**: 定数、純粋関数、デバイス状態・出力ハンドラ等、現行計画で別フェーズまたは対象外とするもの
- **判断保留**: 複数の実装経路があり、責務境界または互換性を確認してから決めるもの

## 2. 機械的棚卸し結果

| 検索対象 | 生の確認件数 | 主な範囲 | 備考 |
|---|---:|---|---|
| `Global.`、`rootHub`、ViewModel直接生成、DI構築等 | 約1,001件 | 76ファイル | コメント、定数、Legacyシムを含む横断検索 |
| `App.rootHub`／`Program.rootHub` | 約140件 | 17ファイル | 起動時代入、UI、AutoProfile、デバイス処理を含む |
| `new XxxViewModel(...)` | 31件 | 16ファイル | Factory 内の正規生成、DI取得後の互換フォールバックを含む |
| `new ServiceCollection()`／`AppHost.CreateHost()` | 各1系統 | `App.xaml.cs` | 旧インライン構築と正式 Host 構築の併存を確認 |
| `AppHost.GetService<IProfileSettingsService>` in `Mapping` | 1件 | `Mapping.cs` | 静的キャッシュ。入力ループ内の毎回解決ではない |

件数は検索時点のソースに対する生の件数であり、Phase4 対象件数そのものではない。

## 3. Phase4 対象と評価

### 3.1 DI／Composition Root

| 残存項目 | 評価 | 根拠・対応先 |
|---|---|---|
| `App.xaml.cs` の旧 `ServiceCollection` 構築 | **Phase4 対象・残存** | 全体計画 §5.2、Phase4 計画 Step6 は Composition Root 一本化を完了条件に含む。現在は `AppHost.CreateHost()` と併存しているため、二重構築の整理が必要 |
| `ControlService` の App 側直接生成 | **Phase4 対象・部分残存** | `ControlService` は App で直接生成され、DI登録されていない。設定サービスの注入は進んだが、Composition Root からの完全解決は未完了 |
| `IProfileSwitcher` の DI 登録 | **確認済み** | `ServiceRegistration.cs` に登録済み |
| `IProfileSettingsService`／`IProfileRepository` 等 | **確認済み** | `ServiceRegistration.cs` に登録済み |
| `IVirtualKBM`、`IDeviceStateAccessor` 等 | **判断保留／後続対象** | 全体計画では別フェーズまたは循環依存解消の対象。現時点で登録・解決経路の完全性を別途確認する必要がある |

### 3.2 ViewModel／UI

| 残存項目 | 評価 | 根拠・対応先 |
|---|---|---|
| Factory 内の `new ProfileSettingsViewModel` 等 | **対象外（正規実装）** | Factory が実行時引数と DI サービスを合成するための生成であり、計画 §5.6 の想定どおり |
| View の `vmFactory ?? new XxxViewModel(...)` | **Phase4 対象・残存** | Factory 解決失敗時の互換フォールバック。新方式の動作確認後に削除判断が必要 |
| `MainWindow.xaml.cs` の Settings／Log／ControllerList／TrayIcon 直接生成 | **Phase4 対象・残存または判断保留** | Pattern A/B の DI 化方針に該当。ただし既存クラスのコンストラクタ依存と Factory 契約を確認してから一括変更しない |
| `ProfileEditor` の MappingList／SpecialActions／TouchButton 直接生成 | **Phase4 対象・残存** | Pattern C／Factory 化の対象範囲と整合確認が必要 |
| `SpecialActionEditor` 内の各 Action ViewModel 直接生成 | **Phase4 対象・残存** | 固定値パラメータを持つ ViewModel 群。個別 Factory 化か一括 Factory 化か判断が必要 |
| `AxialStick`、`BindingWindow`、各種小画面の直接生成 | **判断保留** | Phase4 の29件棚卸しに含まれるか、既存の Pattern A/B/C 分類台帳と突合が必要 |

### 3.3 `Global` 参照

| 残存分類 | 評価 | 内容 |
|---|---|---|
| `ProfileSettingsViewModel` の設定系 `Global` | **Phase4-B 対象範囲は概ね移行済み** | B-1-1〜B-1-9 で設定サービス対象を移行。残る `LaunchProgram`、`OutContType`、リソース、キャッシュ、デバイス操作等は別サービスまたは対象外が混在 |
| `ControlService` の設定系 `Global` | **B-3対象は移行済み、残余あり** | L2/R2、ランブル、通知、出力データ等は移行。環境、出力ハンドラ、デバイス状態、プロファイル適用等は別境界 |
| `Mapping` の設定系 `Global` | **B-4対象は移行済み、残余あり** | デバウンス、通知、LS/RS、L2/R2、ジャイロ、ランブル、ライトバー、SA設定は移行。`Clamp`、定数、KBMハンドラ、プロファイル適用、座標変換等は対象外または後続フェーズ |
| `Global` の互換シム | **Phase4対象・存置可** | 全体計画と Phase4 計画は新経路確認までシム存置を許可。削除ではなく `[Legacy]` 可視化と呼出元監査が必要 |
| `Global` の設定シムにログがない箇所 | **Phase4-Step10対象** | 計画は新経路 `[DI]`、Legacy 経路 `[Legacy]` の Trace 可視化を要求。全シムへの適用状況は別途全数確認が必要 |

### 3.4 `rootHub` と `ControlService` 直接依存

| 残存項目 | 評価 | 根拠 |
|---|---|---|
| `Mapping.cs` の `Program.rootHub` | **Phase4／Phase3引継ぎ対象** | 全体計画 §5.5 は `IDeviceStateAccessor` による循環依存解消を明示。ただし現在も Mapping の複数箇所に残存 |
| `ProfileEditor`、`MainWindow`、ControllerReadings 等の `App.rootHub` | **Phase4対象または判断保留** | UI が ControlService の多数の機能へ直接依存。最小アクセサ分割か ControlService Factory 化か、設計選択が必要 |
| `AutoProfileChecker` の rootHub／Global 依存 | **判断保留** | 自動プロファイル実行はプロファイル切替・デバイス監視・アプリ状態をまたぐため、単一サービスへ機械的に移せない |

## 4. 現時点の結論

Phase4 の計画基準で「Legacy 経路が完全に解消済み」とは判定できない。少なくとも次を残存課題として扱う。

1. `App.xaml.cs` の旧インライン DI 構築と `AppHost` の二重 Composition Root。
2. `ControlService` の DI コンテナ未登録と App 側直接生成。
3. ViewModel の Factory 失敗時フォールバック、および Pattern A/B/C の未接続 ViewModel。
4. `Mapping.cs` の `Program.rootHub` 直接依存。
5. UI／AutoProfile 系の `App.rootHub` 直接依存。
6. `Global` シムの `[Legacy]` Trace 可視化の網羅性。

一方、次は機械的に Phase4 残存と数えない。

- `Global.MAX_DS4_CONTROLLER_COUNT` 等の定数参照
- `Global.Clamp` 等の純粋関数
- KBM／デバイス出力、環境、パス、デバイス状態など別サービス境界または後続フェーズの範囲
- Factory 内で実行時引数付き ViewModel を生成する `new`
- 互換シムそのもの。ただし利用状況とログは監査対象

## 5. 判断確認が必要な項目

実装方法が複数あり、計画書だけでは一意に決められないため、次の確認が必要である。

### 確認 A: 二重 Composition Root

`App.xaml.cs` の旧 `ServiceCollection` を削除し、Action 系の登録も `AppHost` 側へ完全統合する方針でよいか。

### 確認 B: `ControlService` の DI 化

`ControlService` を `ServiceRegistration` に Singleton 登録し、`App.xaml.cs` は `IServiceProvider` から解決する方針でよいか。`Program.rootHub` は移行中の互換シムとして残す前提とする。

### 確認 C: `rootHub` 直接依存

UI／AutoProfile／Mapping の `rootHub` 参照を、次のどちらで進めるか。

- **C-1: 最小アクセサ方式**: `IDeviceStateAccessor` 等へ必要なメンバーだけ切り出し、static 構造を維持する
- **C-2: ControlService 注入方式**: View／サービスへ `ControlService` を注入し、直接依存を段階的に置換する

### 確認 D: ViewModel の残存直接生成

Factory の互換フォールバックまで Phase4 で削除するか、それとも実機 CP4 完了まで存置し、利用時だけ `[Legacy]` ログを追加するか。

## 6. 次の作業

確認 A〜D の回答を受けるまで、上記判断保留項目の実装変更は行わない。回答後、承認された方針だけを Phase4-Step10-2-C の次段階として実装計画化する。
