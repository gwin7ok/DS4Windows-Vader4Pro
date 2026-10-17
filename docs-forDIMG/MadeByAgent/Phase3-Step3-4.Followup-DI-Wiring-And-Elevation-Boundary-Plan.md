# フェーズ3 フォローアップ計画書: DI配線の穴埋めと権限昇格の境界整理

作成日: 2026-08-30
改訂: 2026-08-30（最終形への無駄作業を減らす方針へ更新。二重コンテナとフォールバックはフェーズ5まで残す）
対象ブランチ: For-DI-migration-work
前提ドキュメント:
- `docs-forDIMG/DI-App-Wide-Migration-Plan.md` §5.2（Composition Root）、§5.5（循環依存解消）、§6.5（フェーズ3）、§6.7（フェーズ5）
- `docs-forDIMG/MadeByAgent/Phase3-Plan.md`
- `docs-forDIMG/MadeByAgent/Phase3-Status.md`
- `docs-forDIMG/MadeByAgent/Phase3-Step3-1to3-4-Completion-Report.md`
- `.github/copilot-instructions.md`

## ルール確認（作業開始前に毎回読む）
- §2.1 修正版: 古い方式を残して移行OK。新方式の動作確認後に削除。複数候補同時実装はNG。
- §2.2 機能100%維持、§2.3 ログ維持。
- §3.1 コンテナ登録は `AppHost.cs`（またはそこから呼ばれる拡張）に行う。クラスへの依存は原則コンストラクタインジェクション。
- §3.2 巨大ファイル（`ControlService.cs`, `Mapping.cs`, `App.xaml.cs`）はピンポイント置換のみ。
- §4: マイクロステップ。1ステップ完了ごとに確認を挟む。一度に全ステップを実装しない。

---

## 0. 本計画の位置づけと設計原則

Phase3-Plan の Step 3-1〜3-4 はコード生成としては完了している。しかし現状確認（2026-08-30）で、**新経路が DI から解決されない／登録済みサービスが呼び出し元から見えない／権限昇格とデバイス再有効化の境界が未整理**であることが分かった。

本計画は Step 3-5（`IElevatedProcessLauncher`）に入る **前** に片付けるフォローアップである。Step 3-5/3-6 の実装詳細は Phase3-Plan.md に残す。

### 0.1 全体ロードマップとの役割分担（改訂の根拠）

| 時期 | 残してよいもの | やってはいけないこと |
|---|---|---|
| **本フォローアップ〜フェーズ4** | 二重コンテナ（インライン `ServiceCollection` と `AppHost`）。Mapping 等の **呼び出し側フォールバック**（確認後フェーズ5で削除） | フェーズ5で捨てる専用シムクラス、サービスロケータ用の遅延プロパティ、呼び出し箇所ごとの二重分岐 |
| **フェーズ5** | コンテナ一本化、フォールバック削除、`ControlService` の生成をコンテナへ移す | 本フォローアップで入れたコンストラクタ引数や `IDs4DeviceRegistry` 呼び出しをやり直すこと |

**原則: 今入れるコードは、フェーズ5では「登録の1行を差し替える／フォールバックを消す」だけで最終形になること。** 中間専用の型や、後で全呼び出しを二度書きする経路は作らない。

フェーズ5で残る作業（本計画では実施しない）:

1. インラインコンテナをやめ、`ServiceProviderHolder` を `AppHost.Services` に接続するか廃止する。
2. `AddSingleton<IDeviceStateAccessor>(_ => Program.rootHub)` を、登録済み `ControlService` インスタンスへのエイリアスに差し替える。
3. `Mapping` の `Program.rootHub` フォールバックを削除する。
4. Composition Root の `new ControlService(...)` をコンテナ登録へ移す（コンストラクタ形はそのまま使う）。

### 0.2 対象4点

| # | 問題 | 本計画での扱い |
|---|---|---|
| 1 | `IDeviceStateAccessor` が DI に載っておらず、`Mapping` の新経路が死んでいる | **対応する**（Step F-1）。追加クラスは作らない |
| 2 | `IDs4DeviceRegistry` は AppHost 側に登録済みだが `ControlService` が使っていない | **対応する**（Step F-2）。**コンストラクタ注入**（最終形）。呼び出し内フォールバックは置かない |
| 3 | `IDs4DeviceRegistry` と `DS4Devices` public static の対応が grep 未実施 | **対応する**（Step F-0）。調査のみ |
| 4 | `ReEnableDevice` と Step 3-5 の役割重複 | **境界を文書で確定**（§2.4）。実装は Step 3-5 |

`ApplyProfileDirect` / `RestoreProfileDirect` の `Program.rootHub` 2箇所は、Phase3-Plan §1.2 どおり **本計画のスコープ外**（フェーズ4 `IProfileRepository`）。

---

## 1. 着手前調査で確定した事実

### 1.1 ルート原因: DIコンテナが二重で、呼び出し先が食い違っている

起動シーケンス（`App.xaml.cs`）は次の順である。

1. インライン `new ServiceCollection()` に Actions 系5サービスだけを登録し `BuildServiceProvider()`。
2. そのプロバイダを `ServiceProviderHolder.SetProvider(sp)` に渡す。
3. 別系統で `AppHost.CreateHost()` を呼ぶ。こちらだけが `ServiceRegistration.AddAppServices`（`IVirtualKBM`, `IDs4DeviceRegistry`, `IProfileSettingsService`）を持つ。
4. `CreateControlService` で `rootHub = new ControlService(parser)`。`ControlService` はどちらのコンテナにも登録されない。生成箇所は **`App.xaml.cs` の1箇所のみ**。

`Mapping.cs` の解決先がサービスごとに違う。

| 依存 | 解決先 | 現状 |
|---|---|---|
| `IVirtualKBM` | `AppHost.GetService<IVirtualKBM>()`（フォールバック `Global.outputKBMHandler`） | AppHost に登録済みのため **新経路が生きている** |
| Actions 系 | `ServiceProviderHolder.Provider` | インラインコンテナに登録済みのため生きている |
| `IDeviceStateAccessor` | `ServiceProviderHolder.Provider.GetService(...)` | **インラインコンテナに未登録。常に失敗し `Program.rootHub` フォールバックだけが動く** |
| `IDs4DeviceRegistry` | どこからも未解決 | AppHost には登録済みだが、`ControlService` は `DS4Devices.` を直接呼ぶ |

本フォローアップではコンテナを統合しない。代わりに、**フェーズ5で残る側（AppHost / `ServiceRegistration`）にだけ登録し、Mapping の解決口を既に成功している `IVirtualKBM` と同じ `AppHost.GetService` に揃える。** インラインコンテナへ同じインターフェースを二重登録しない。

### 1.2 `ControlService` は `IDeviceStateAccessor` を実装済み

`GetController(int)` は境界チェック付きで `DS4Controllers[deviceIndex]` を返す。欠けているのは、稼働中インスタンスを AppHost から `IDeviceStateAccessor` として取れることだけである。

`CreateHost()` は `rootHub` 生成より前に終わる。そのため **Build 時点でインスタンスを渡すことはできない**。解決は「遅延ファクトリ（初回 `GetService` 時に `Program.rootHub` を返す）」であり、追加の実装クラスは不要である。`GetService` が実際に呼ばれるマクロ rumble 時点では `rootHub` は既に存在する。

### 1.3 `DS4Devices` の public static メンバー実測（Step F-0 の結論先行）

`DS4Windows/DS4Library/DS4Devices.cs` の `public static` は次で尽きる。

| メンバー | `IDs4DeviceRegistry` |
|---|---|
| `RequestElevation` | あり |
| `PrepareDS4Init` / `PostDS4Init` / `PreparePendingDevice` | あり |
| `isExclusiveMode` | `IsExclusiveMode` としてあり |
| `findControllers` / `getDS4Controllers` / `stopControllers` | あり |
| `On_Removal` / `RemoveDevice` / `UpdateSerial` | あり |
| `reEnableDevice` | `ReEnableDevice` としてあり（Phase3-Plan コード例には無かった追加） |

VID/PID 定数（`SONY_VID` 等）は `internal const` であり、レジストリインターフェースの対象外でよい。欠員は無い想定。Step F-0 で再 grep して閉じる。

### 1.4 権限昇格とデバイス再有効化は別レイヤ

現行フロー:

```
[親プロセス / 非管理者]
  DS4Devices.findControllers
    exclusive open 失敗
      RequestElevation イベント発火
        ControlService.DS4Devices_RequestElevation
          Process.Start(exelocation, Verb=runas, args="re-enabledevice {instanceId}")
          最大30秒 WaitForExit / タイムアウト時 Kill
          args.StatusCode = 子の ExitCode
      成功なら hDevice.OpenDevice 再試行

[親プロセス / 既に管理者]
  DS4Devices.findControllers 内で DS4Devices.reEnableDevice を直接呼ぶ
  （UAC 子プロセスを挟まない）

[子プロセス / 管理者として再起動された DS4Windows]
  App.xaml.cs  parser.ReenableDevice == true
    DS4Devices.reEnableDevice(parser.DeviceInstanceId)
    即 Shutdown（通常の ControlService は起動しない）
```

`reEnableDevice` の中身は `SetupDi*` による HID デバイスの disable/enable であり、`Process.Start` ではない。

---

## 2. 採用する方針（検討した案と採否）

### 2.1 問題1: `IDeviceStateAccessor` を生きた経路にする

| 案 | 内容 | 採否 | フェーズ5での運命 |
|---|---|---|---|
| A | `ControlService` の生成自体をコンテナに移し、同じインスタンスを `IDeviceStateAccessor` としても登録 | **今は不採用**（起動スレッド・`ArgumentParser`・既存 `new` を本計画で動かさない） | フェーズ5の本命。今はやらない |
| B | `ServiceProviderHolder` にだけ登録し、Mapping の GetService はそのまま | **不採用**。インラインコンテナはフェーズ5で消える側。Mapping と `IVirtualKBM` の口も割れたまま |
| C | 専用シムクラス `ControlServiceDeviceStateAccessor` を新設 | **不採用**。フェーズ5で型ごと削除になる無駄 |
| D | `ServiceRegistration` に遅延ファクトリだけ足し、実装は既存 `ControlService`。Mapping は `AppHost.GetService<IDeviceStateAccessor>()`。フォールバックは残す | **採用** |

案 D の登録（概念）:

```csharp
services.AddSingleton<IDeviceStateAccessor>(_ => Program.rootHub);
```

`ControlService` が既に `IDeviceStateAccessor` を実装しているので、追加ファイルは不要。初回解決は `CreateControlService` の後なので `rootHub` は非 null が前提。null なら Mapping 側フォールバックが拾う。

Mapping ピンポイント:

- 解決を `AppHost.GetService<IDeviceStateAccessor>()` に変更（`IVirtualKBM` と同じ口。フェーズ5でもこの口が残る）
- `accessor == null` または `GetController` が null のとき、既存どおり `Program.rootHub?.DS4Controllers[device]`（フェーズ5でこの分岐だけ削除）
- 空 catch は維持（ログ新設禁止）

`ServiceProviderHolder` 側には登録しない。

### 2.2 問題2: `IDs4DeviceRegistry` を `ControlService` から使う

| 案 | 内容 | 採否 | フェーズ5での運命 |
|---|---|---|---|
| A | コンストラクタに `IDs4DeviceRegistry` を追加。Composition Root（`CreateControlService`）で `AppHost.GetService` して渡す。クラス内部はレジストリのみ | **採用** | コンストラクタと内部呼び出しは最終形のまま。フェーズ5は `new` をコンテナ登録に移すだけ |
| B | 呼び出し箇所ごとに `AppHost.GetService` | **不採用**。サービスロケータがクラス内に散る。後でコンストラクタ化するとき全箇所やり直し |
| C | 遅延プロパティ + 各呼び出しで静的 `DS4Devices` フォールバック | **不採用**。プロパティはフェーズ5で捨てる。分岐は二重購読リスクがあり、後でまた全箇所から落とす |

`CreateControlService`（`App.xaml.cs`、生成1箇所）:

```csharp
var registry = AppHost.GetService<IDs4DeviceRegistry>()
    ?? new Ds4DeviceRegistryAdapter();
rootHub = new DS4Windows.ControlService(parser, registry);
```

`?? new Ds4DeviceRegistryAdapter()` は **同じアダプタクラスの再生成**であり第二実装ではない。`CreateHost` 成功後なら GetService は非 null のはずで、null 合体は起動順の安全網。フェーズ5でコンテナが `ControlService` を組み立てるときにこの行は消える。

`ControlService` 内部:

- フィールドに受け取った `IDs4DeviceRegistry` を保持
- `DS4Devices.` の実行時呼び出しをフィールド経由に置換
- **メソッド内で静的 `DS4Devices` へ戻る分岐は置かない**（経路は常にアダプタ1本。アダプタが静的へ委譲する）

これによりイベント購読が二重にならない。

`App.xaml.cs` の `parser.ReenableDevice` 枝（子プロセス）は **置換しない**。Host も `ControlService` も起動せずすぐ終了する経路であり、静的 `DS4Devices.reEnableDevice` のままが正しい（最終形でもこの短命プロセスは Composition Root 外でよい）。

`DualSenseDevice` 等の VID 定数参照はレジストリ化しない。

Phase3-Plan Step 3-3 の「コンストラクタ変更見送り」は、本改訂で **意図的に上書き**する。見送りのまま遅延プロパティにすると、F-2 の置換をフェーズ5で二度やる無駄が出る。生成が1箇所なのでコンストラクタ追加の影響は Composition Root に閉じる。

### 2.3 問題3: インターフェース網羅

欠員は §1.3 のとおり無い想定。Step F-0 で grep 再実行し、差分があれば F-2 の前に `IDs4DeviceRegistry` / アダプタへ **足りないメンバーだけ**足す。`ReEnableDevice` は削除しない。

### 2.4 問題4: 境界の確定（Step 3-5 への制約）

| 関心事 | 担当 | 副作用の種類 |
|---|---|---|
| UAC 付きで自分自身を子プロセス起動し、終了コードを待つ | **`IElevatedProcessLauncher`（Step 3-5 で新設）** | `Process.Start` + `runas` |
| HID デバイスの disable/enable | **`IDs4DeviceRegistry.ReEnableDevice`（既存。Step 3-5 では触らない）** | SetupAPI |
| 子プロセス側のエントリ（`-re-enabledevice`） | **`App.xaml.cs` の既存枝。本計画・Step 3-5 とも変更しない** | 起動直後に `reEnableDevice` して終了 |

Step 3-5 でやってはいけないこと:

- `IElevatedProcessLauncher` に `ReEnableDevice` 相当を持たせる
- `IDs4DeviceRegistry` から `ReEnableDevice` を削除してランチャ側へ移す
- `findControllers` 内の「既に管理者なら `reEnableDevice`」をランチャ経由に変える

Step 3-5 でやること（Phase3-Plan の範囲。本計画では実装しない）:

- `ControlService.DS4Devices_RequestElevation` の **Process.Start 部分だけ**を `IElevatedProcessLauncher` 優先 + 既存直接呼び出しフォールバックにする（このフォールバックもフェーズ5で削除予定）
- 引数文字列 `"re-enabledevice " + args.InstanceId`、30秒タイムアウト、Kill、`StatusCode` 代入、空 catch は維持

---

## 3. タスク分割（マイクロステップ）

| ステップ | 内容 | 完了基準 | コード変更 |
|---|---|---|---|
| **F-0** | `DS4Devices.` / `public static` の grep 再抽出と、§1.3 表の突合 | 欠員の有無を文書化。欠員時のみ F-0b | 原則なし |
| **F-1** | `IDeviceStateAccessor` を AppHost の遅延ファクトリで登録し、Mapping の解決口を揃える | `ServiceRegistration` にファクトリ1行。`Mapping` rumble が `AppHost.GetService`。フォールバック残存。**新クラスなし**。ビルド成功 | `ServiceRegistration.cs` + `Mapping.cs` ピンポイント |
| **F-2** | `ControlService` へ `IDs4DeviceRegistry` をコンストラクタ注入し、内部の `DS4Devices.` 実行時呼び出しを置換 | 生成1箇所がレジストリを渡す。内部はレジストリのみ。子プロセス枝は未変更。ビルド成功 | `ControlService.cs` + `App.xaml.cs` の `CreateControlService` のみ |
| **F-3** | 文書更新 | Status と本計画のチェックが同期。フェーズ5申し送りが Status にある | md のみ |

**F-0 → F-1 → F-2 → F-3 の順。各ステップ完了後に確認を挟む。**

F-2 内部の置換塊（同一ステップ。確認は F-2 完了時で可）:

1. コンストラクタ引数とフィールド
2. イベント / デリゲート代入
3. `FindControllers` / `GetDS4Controllers` / `StopControllers` / `RemoveDevice`
4. `IsExclusiveMode`
5. `OnRemoval` / `UpdateSerial` のイベントフック

---

## 4. 各ステップの詳細

### Step F-0: メンバー突合

- `DS4Devices.cs` の `public static`
- `ControlService.cs` / `App.xaml.cs` の `DS4Devices.`

無い実行メンバーが出た場合のみ F-0b。定数・コメントアウトは対象外。

### Step F-1: アクセサ配線（クラス追加なし）

対象:

- `DS4Windows/DI/ServiceRegistration.cs`
- `DS4Windows/DS4Control/Mapping.cs`（マクロ rumble、現行 6504 行付近）

やってはいけないこと:

- `ControlServiceDeviceStateAccessor` 等の新規型
- `ApplyProfileDirect` / `RestoreProfileDirect` を触る
- `ServiceProviderHolder` と AppHost の両方に登録する
- `ControlService` から `IDeviceStateAccessor` 実装を外す
- Mapping のフォールバックをこのステップで削除する

### Step F-2: レジストリのコンストラクタ注入

対象:

- `DS4Windows/DS4Control/ControlService.cs`
- `DS4Windows/App.xaml.cs` の `CreateControlService` のみ

やってはいけないこと:

- クラス内の `AppHost.GetService` / 遅延プロパティ
- 各メソッドでの静的 `DS4Devices` フォールバック
- 子プロセス枝（`parser.ReenableDevice`）の変更
- `ControlService` の生成をコンテナに移す（フェーズ5）

テストや他ファイルに `new ControlService(parser)` が残っていないことは F-2 着手時に再 grep する（本計画作成時点では `App.xaml.cs` のみ）。

### Step F-3: 文書

- `Phase3-Status.md`（F-0〜F-2、既知残課題、次着手 = Step 3-5、フェーズ5で消すもの＝Mapping フォールバックと `Program.rootHub` ファクトリ）
- 実装完了時に `Phase3-Followup-DI-Wiring-And-Elevation-Boundary-Report.md` を新設
- `Phase3-Plan.md` の Step 3-1 namespace 例（`DS4Windows.Actions` → 実態 `DS4Windows.Services`）と、Step 3-3 の「コンストラクタ変更見送り」を本改訂に合わせて直してよい

---

## 5. リスクと回避策

| リスク | 該当 | 回避策 |
|---|---|---|
| ファクトリ実行時に `Program.rootHub` が null | F-1 | Mapping フォールバック。マクロ rumble は `rootHub` 稼働後 |
| コンストラクタ引数追加でコンパイル割れ | F-2 | 生成箇所は1箇所。着手時に `new ControlService` を再 grep |
| F-2 で巨大ファイルを壊す | F-2 | メソッド単位のピンポイント。一括再生成しない |
| Step 3-5 が `ReEnableDevice` を巻き込む | 文書 | §2.4 を Status に転記 |
| コンテナ二重は残る | 全体 | 意図的（フェーズ5）。本計画は AppHost 側へ最終形の部品を載せるだけ |

---

## 6. 完了判定基準（本フォローアップ全体）

- [ ] `DS4Devices` の public static 実行メンバーと `IDs4DeviceRegistry` の突合結果が文書化されている
- [ ] `IDeviceStateAccessor` の登録が `ServiceRegistration` の遅延ファクトリのみで、**新規シムクラスが無い**
- [ ] `AppHost.GetService<IDeviceStateAccessor>()` が稼働中 `ControlService` を返す（`rootHub` 設定後）
- [ ] `Mapping.cs` マクロ rumble が AppHost 解決を優先し、フォールバックが残っている
- [ ] `ControlService` が `IDs4DeviceRegistry` をコンストラクタで受け取り、内部の実行時 `DS4Devices.` がフィールド経由になっている
- [ ] `ControlService` 内部に静的 `DS4Devices` へのメソッド単位フォールバックが無い
- [ ] `CreateControlService` が AppHost からレジストリを渡している（null 時のみ同じアダプタを new）
- [ ] `App.xaml.cs` の `-re-enabledevice` 枝は静的 `reEnableDevice` のままである
- [ ] `IElevatedProcessLauncher` と `ReEnableDevice` の境界（§2.4）が Status に残っている
- [ ] ビルドが通っている

---

## 7. 本計画の外（申し送り）

- Step 3-5 / 3-6（本計画完了後、Phase3-Plan に戻る）
- `ApplyProfileDirect` / `RestoreProfileDirect` の `Program.rootHub`（フェーズ4）
- 二重コンテナの統合、`ServiceProviderHolder` 廃止、Mapping フォールバック削除、`IDeviceStateAccessor` ファクトリの `ControlService` エイリアス化、`ControlService` 生成のコンテナ化（**フェーズ5**。本計画の成果を破棄せず差し替える）
- 実機の接続/切断・UAC・マクロ rumble（ユーザー確認。コード完了条件には含めない）

---

## 8. 次のアクション

1. 本改訂計画の確認を得る。
2. Step F-0（grep 突合）から着手する。欠員が無ければ F-1 の許可を取る。
3. F-1 → F-2 → F-3 の順。F-3 完了後に Step 3-5 の可否を確認する。
