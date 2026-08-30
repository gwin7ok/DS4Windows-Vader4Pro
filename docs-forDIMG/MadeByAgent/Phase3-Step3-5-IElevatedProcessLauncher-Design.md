﻿# Step 3-5 設計書: IElevatedProcessLauncher（権限昇格の抽象化）

作成日: 2026-08-30
対象ブランチ: For-DI-migration-work
前提ドキュメント:
- `docs-forDIMG/MadeByAgent/Phase3-Plan.md`（Step 3-5定義）
- `docs-forDIMG/MadeByAgent/Phase3-Status.md`
- `docs-forDIMG/MadeByAgent/Phase3-Followup-DI-Wiring-And-Elevation-Boundary-Plan.md` §2.4（境界確定）
- `docs-forDIMG/MadeByAgent/Phase1-C5-LaunchProcessAction-Design.md`（同種インターフェースの先行パターン）
- `.github/copilot-instructions.md`

## ルール確認（作業開始前に毎回読む）

- §2.1 修正版: 古い方式を残して移行OK。新方式の動作確認後に削除。複数候補同時実装はNG。
- §2.2 機能100%維持、§2.3 ログ維持。
- §3.1 コンテナ登録は `AppHost.cs`（またはそこから呼ばれる拡張）に行う。クラスへの依存は原則コンストラクタインジェクション。ただし本ステップの対象コード（`ControlService.DS4Devices_RequestElevation`、イベントハンドラ）は既存パターン（`Mapping.cs`／`LaunchProcessAction`）にならい `AppHost.GetService` 経由＋フォールバックとする（後述§2）。
- §3.2 巨大ファイル（`ControlService.cs`）はピンポイント置換のみ。全体再生成しない。
- §4: マイクロステップ。1ステップ完了ごとに確認を挟む。

---

## 0. 着手前調査で判明した事実

### 0.1 対象コードの確定（`ControlService.cs`、実測済み）

`ControlService.DS4Devices_RequestElevation`（`Ds4DeviceRegistryAdapter` 経由で `DS4Devices.RequestElevation` イベントに購読、コンストラクタ内 `_deviceRegistry.RequestElevation += DS4Devices_RequestElevation;`）の実装は以下の通り（Phase3-Plan §0.4の記載と一致、変更なしを確認）。

```csharp
private void DS4Devices_RequestElevation(RequestElevationArgs args)
{
    // Launches an elevated child process to re-enable device
    ProcessStartInfo startInfo =
        new ProcessStartInfo(Global.exelocation);
    startInfo.Verb = "runas";
    startInfo.Arguments = "re-enabledevice " + args.InstanceId;
    startInfo.UseShellExecute = true;

    try
    {
        Process child = Process.Start(startInfo);
        if (!child.WaitForExit(30000))
        {
            child.Kill();
        }
        else
        {
            args.StatusCode = child.ExitCode;
        }
        child.Dispose();
    }
    catch { }
}
```

呼び出し元（`DS4Library/DS4Devices.cs` 300〜325行目付近）は次の通り。管理者権限で起動されていない場合のみこのイベントが発火し、`args.StatusCode == RequestElevationArgs.STATUS_SUCCESS`（`0`）のときだけ `OpenDevice` を再試行する。

```csharp
RequestElevationArgs eleArgs = new RequestElevationArgs(Global.GetInstanceIdFromDevicePath(hDevice.DevicePath));
RequestElevation?.Invoke(eleArgs);
if (eleArgs.StatusCode == RequestElevationArgs.STATUS_SUCCESS)
{
    hDevice.OpenDevice(isExclusiveMode);
}
```

`RequestElevationArgs.StatusCode` の既定値は `STATUS_INIT_FAILURE`（`-1`）。ハンドラが何もしなければ失敗扱いのまま、というのが既存の暗黙契約である。

### 0.2 既存の `IProcessLauncher` との違い（重複させない境界）

`DS4Windows/Actions/IProcessLauncher.cs`（Phase1 C5で新設済み）:

```csharp
public interface IProcessLauncher
{
    void Launch(string filePath);
    void Launch(string fileName, string arguments, bool useShellExecute, bool hidden);
}
```

`DefaultProcessLauncher` は `Process.Start` を呼ぶだけで `Verb=runas` も `WaitForExit` も `Kill` も持たない。`LaunchProcessAction` がこれを `AppHost.GetService<IProcessLauncher>() ?? new DefaultProcessLauncher()` で使用している（Phase1 C5、単純起動・fire-and-forget用途）。

`DS4Devices_RequestElevation` が必要とするのは「`runas` 昇格＋30秒待機＋タイムアウト時Kill＋終了コード取得」という同期的な待ち合わせセマンティクスであり、`IProcessLauncher` の責務（単純起動）とは異なる。**`IProcessLauncher` を拡張するのではなく、Phase3-Plan/Followup-Plan §2.4の通り新規インターフェース `IElevatedProcessLauncher` として分離する。**

### 0.3 `IDs4DeviceRegistry.ReEnableDevice` との境界（Followup-Plan §2.4で確定済み、再確認）

| 関心事 | 担当 | 副作用 |
|---|---|---|
| UAC付きで自分自身を子プロセス起動し終了コードを待つ | **`IElevatedProcessLauncher`（本ステップ）** | `Process.Start` + `runas` |
| HIDデバイスのdisable/enable | `IDs4DeviceRegistry.ReEnableDevice`（既存、変更しない） | SetupAPI |
| 子プロセス側のエントリ（`App.xaml.cs` の `parser.ReenableDevice` 枝、静的 `DS4Devices.reEnableDevice` 直接呼び出し） | 変更しない | - |

本ステップは `DS4Devices_RequestElevation` 内の `Process.Start` 部分のみを対象とし、上記の他2つには一切触れない。

### 0.4 参考: `IProcessLauncher` 系の実装状況（本ステップのスコープ外だが設計上参考にした事実）

`ServiceRegistration.cs` を実測したところ、`IProcessLauncher` は未登録（`LaunchProcessAction` は `AppHost.GetService` が null を返すため常に `new DefaultProcessLauncher()` フォールバックで動作している）。これは「フェーズ5でDI登録を仕上げる」という既存方針と整合しており、本ステップでも同じ運用（**インターフェース定義＋アダプタ実装＋フォールバック付き利用を先に作り、正式なDI登録の要否はこのステップ内で軽量に済ませる**）を踏襲する。

### 0.5 別件: `IDeviceStateAccessor` 配線に関する食い違い（本ステップのスコープ外、要報告）

`Phase3-Followup-DI-Wiring-And-Elevation-Boundary-Report.md` は F-1 完了として「`ServiceRegistration.cs` に `AddSingleton<IDeviceStateAccessor>` を追加」「`Mapping.cs` の解決口を `AppHost.GetService` に変更」と報告しているが、今回の実コード確認で **どちらも現状のコードには反映されていない**ことを確認した（`ServiceRegistration.cs` に該当行なし、`Mapping.cs` 6504行目付近は今も `ServiceProviderHolder.Provider` を参照し続けている＝常に失敗しフォールバックのみが動く状態）。

これはStep 3-5の実装対象ではないため本設計書では変更しないが、`Phase3-Status.md` の「F-1完了」表記の正確性に関わる。本ステップの完了後、別途確認・対応要否を判断いただきたい。

---

## 1. 採用する設計

### 1.1 インターフェース

配置: `DS4Windows/DS4Control/Services/IElevatedProcessLauncher.cs`（`IDs4DeviceRegistry`/`IDeviceStateAccessor` と同じ namespace `DS4Windows.Services`、`ControlService.cs` は既に `using DS4Windows.Services;` 済みのため追加using不要）。

```csharp
namespace DS4Windows.Services
{
    /// <summary>
    /// 自プロセスをUAC昇格(runas)で子プロセス再起動し、終了を待ち合わせる抽象化。
    /// DS4Devices.RequestElevation（デバイス再有効化のための昇格要求）専用。
    /// IProcessLauncher（Actions/、単純起動用）とは責務が異なるため統合しない。
    /// </summary>
    public interface IElevatedProcessLauncher
    {
        /// <summary>
        /// Global.exelocation を runas 昇格で子プロセスとして起動し、
        /// 最大 timeoutMs ミリ秒 WaitForExit する。タイムアウト時は子プロセスを Kill する。
        /// </summary>
        /// <param name="arguments">起動引数（例: "re-enabledevice {instanceId}"）</param>
        /// <param name="timeoutMs">WaitForExit のタイムアウト(ms)。既定30000（既存動作と同一）。</param>
        /// <returns>
        /// 時間内に終了した場合は子プロセスの ExitCode。
        /// タイムアウト(Kill)・起動失敗時は null（呼び出し元は StatusCode を更新しない＝既存の「失敗のまま」の暗黙契約を維持）。
        /// </returns>
        int? RelaunchElevated(string arguments, int timeoutMs = 30000);
    }
}
```

`int?` を採用した理由: `RequestElevationArgs.StatusCode` の既定値は `-1`（失敗）であり、ハンドラが「何もしない」ことで失敗を表す既存の暗黙契約がある。`null` はこれをそのまま踏襲する（新たに「失敗を表す特別なint値」を発明しない）。

### 1.2 既定実装（アダプタ）

配置: `DS4Windows/DS4Control/Services/DefaultElevatedProcessLauncher.cs`。既存の `DS4Devices_RequestElevation` の中身をそのまま移設するのみ（ロジック変更なし、§2.2機能維持の原則）。

```csharp
using System.Diagnostics;

namespace DS4Windows.Services
{
    public class DefaultElevatedProcessLauncher : IElevatedProcessLauncher
    {
        public int? RelaunchElevated(string arguments, int timeoutMs = 30000)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo(Global.exelocation);
            startInfo.Verb = "runas";
            startInfo.Arguments = arguments;
            startInfo.UseShellExecute = true;

            try
            {
                Process child = Process.Start(startInfo);
                int? result = null;
                if (!child.WaitForExit(timeoutMs))
                {
                    child.Kill();
                }
                else
                {
                    result = child.ExitCode;
                }
                child.Dispose();
                return result;
            }
            catch
            {
                return null;
            }
        }
    }
}
```

`Global.exelocation` の参照・`Verb`/`UseShellExecute`/30秒既定タイムアウト/Kill/Dispose/空catchは全て既存のまま移設。ロジックの変更は一切ない。

### 1.3 `ControlService.DS4Devices_RequestElevation` の置換（ピンポイント）

既存パターン（`Mapping.cs` 6504行目、`LaunchProcessAction`）と同じ「新経路優先＋例外/未登録時は既存直接呼び出しへフォールバック」の形にする。**新旧を同時に実行しない**（§2.1修正版の「複数候補同時実装NG」に従い、`handled` フラグで一本化）。

```csharp
private void DS4Devices_RequestElevation(RequestElevationArgs args)
{
    // Phase 3 Step 3-5: try DI-based IElevatedProcessLauncher first,
    // fall back to the original direct Process.Start implementation.
    bool handled = false;
    try
    {
        var launcher = DS4WinWPF.AppHost.GetService<IElevatedProcessLauncher>();
        if (launcher != null)
        {
            int? exitCode = launcher.RelaunchElevated("re-enabledevice " + args.InstanceId, 30000);
            if (exitCode.HasValue)
            {
                args.StatusCode = exitCode.Value;
            }
            handled = true;
        }
    }
    catch { }

    if (!handled)
    {
        // Launches an elevated child process to re-enable device
        ProcessStartInfo startInfo =
            new ProcessStartInfo(Global.exelocation);
        startInfo.Verb = "runas";
        startInfo.Arguments = "re-enabledevice " + args.InstanceId;
        startInfo.UseShellExecute = true;

        try
        {
            Process child = Process.Start(startInfo);
            if (!child.WaitForExit(30000))
            {
                child.Kill();
            }
            else
            {
                args.StatusCode = child.ExitCode;
            }
            child.Dispose();
        }
        catch { }
    }
}
```

`handled` は「新経路の呼び出し自体が例外なく完了したか」を表すのみで、`exitCode` の有無（成功/タイムアウト）とは独立している。タイムアウト時も `DefaultElevatedProcessLauncher` 内部で正しく Kill 済みのため `handled = true` で問題ない（既存動作と同じ「`StatusCode` は更新されず `-1` のまま」という結果になる）。

### 1.4 DI登録

`ServiceRegistration.cs` に1行追加（既存の `IProcessLauncher` が未登録運用であるのと同様、こちらも登録しておくことで `AppHost.GetService` が有効経路になる。フォールバックのみで十分という判断もあり得るが、`IDs4DeviceRegistry`/`IVirtualKBM` は登録済みで生きている経路であるため、揃えて登録する）:

```csharp
// Phase 3 Step 3-5: Elevated Process Launcher
services.AddSingleton<IElevatedProcessLauncher, DefaultElevatedProcessLauncher>();
```

---

## 2. 検討した代替案と不採用理由

| 案 | 内容 | 採否 |
|---|---|---|
| A. `IProcessLauncher` に `Launch(..., waitAndElevate: true)` のようなオプションを追加 | 既存の単純起動セマンティクスに待ち合わせ・Kill・ExitCode取得を混在させる | **不採用**。責務が混ざり、`LaunchProcessAction`（fire-and-forget）側のテスト・挙動に影響するリスク。Followup-Plan §2.4の境界方針とも矛盾 |
| B. `IDs4DeviceRegistry.ReEnableDevice` に権限昇格ロジックを統合 | `ReEnableDevice` はSetupAPI（HID有効化）専用で `Process.Start` を含まない | **不採用**。Followup-Plan §2.4で明示的に禁止されている混在 |
| C. `ControlService` のコンストラクタに `IElevatedProcessLauncher` を追加注入 | Step 3-F の `IDs4DeviceRegistry` と同じ「コンストラクタ注入が最終形」パターン | **今回は不採用**（フェーズ5で本命）。理由は次項 |
| D. `AppHost.GetService` 呼び出し＋フォールバック（既存 `Mapping.cs`／`LaunchProcessAction` と同じパターン） | 本設計で採用 | **採用** |

案Cを今回見送る理由: `DS4Devices_RequestElevation` はイベントハンドラ1箇所のみで使用され、`ControlService` の他の場所からは参照されない。案Cを採るとコンストラクタシグネチャが変わり `App.xaml.cs` の生成箇所も触る必要が生じるが、Step 3-Fで `IDs4DeviceRegistry` を注入したばかりであり、同じPRで立て続けにコンストラクタを変更するのはリスクが高い。`IProcessLauncher`（Phase1 C5）も同じ `AppHost.GetService` パターンで運用されており、プロジェクト内で既に確立した安全なパターンのため、これに揃える。フェーズ5でのコンテナ一本化時に、必要であれば案Cへ格上げできる（`Phase3-Followup-Plan.md` §0.1の「フェーズ5でやる」方針と同じ扱い）。

---

## 3. タスク分割

| ステップ | 内容 | 変更ファイル |
|---|---|---|
| **3-5-1** | `IElevatedProcessLauncher` インターフェース新設 | `DS4Windows/DS4Control/Services/IElevatedProcessLauncher.cs`（新規） |
| **3-5-2** | `DefaultElevatedProcessLauncher` 実装（既存ロジックの移設のみ） | `DS4Windows/DS4Control/Services/DefaultElevatedProcessLauncher.cs`（新規） |
| **3-5-3** | DI登録 | `DS4Windows/DI/ServiceRegistration.cs`（1行追加） |
| **3-5-4** | `ControlService.DS4Devices_RequestElevation` のピンポイント置換（新経路＋フォールバック） | `DS4Windows/DS4Control/ControlService.cs`（メソッド1つのみ） |
| **3-5-5** | ビルド確認・文書更新（`Phase3-Status.md`、完了報告書新設） | ビルドのみ、mdファイル |

3-5-1〜3-5-3は新規ファイル追加のみで既存コードに影響しないため、3-5-4（唯一の既存コード変更）とまとめて1回の確認で進めることを想定。3-5-5は文書化のみ。

---

## 4. リスクと回避策

| リスク | 回避策 |
|---|---|
| `runas` 昇格を伴うため、テスト環境（CI等）での自動検証ができない | ビルド確認のみを本ステップの完了条件とし、実機でのUAC昇格シナリオ（管理者権限なし起動→再有効化フロー）確認はPhase3-Status.mdの「検証・確認事項」に記録し、ユーザーによる実機確認待ちとする（Phase3-Plan §4のリスク表と同じ扱い） |
| `handled` フラグの分岐漏れで新旧が二重実行される | コードレビュー時に「`if (!handled)` ブロックの外側で新経路のコードが実行されないこと」を確認。既存の `Mapping.cs`/`LaunchProcessAction` と同一パターンのため実装ミスのリスクは低い |
| `args.StatusCode` の意味（`-1`=失敗、`0`=成功）を誤って変更してしまう | `DefaultElevatedProcessLauncher` は `RequestElevationArgs` に一切触れず `int?` のみを返す設計とし、`StatusCode` の解釈は呼び出し元（`ControlService`）に閉じ込める |
| `IElevatedProcessLauncher` をDI登録しても`AppHost.GetService`が別コンテナ（`ServiceProviderHolder`）を見てしまう | `Mapping.cs`/`LaunchProcessAction` と同じ `DS4WinWPF.AppHost.GetService<T>()` を使用（`ServiceProviderHolder` は使わない）。§0.5で確認した食い違いの再発を防ぐため、実装時に呼び出しAPIを再確認する |

---

## 5. 完了判定基準

- [ ] `IElevatedProcessLauncher` が新設され、`RelaunchElevated` のシグネチャが本設計書通り
- [ ] `DefaultElevatedProcessLauncher` が既存の `DS4Devices_RequestElevation` のロジック（`Verb=runas`, 30秒タイムアウト, Kill, ExitCode取得, 空catch）をそのまま移設したものである（ロジック変更なし）
- [ ] `ServiceRegistration.cs` に `IElevatedProcessLauncher` のSingleton登録が追加されている
- [ ] `ControlService.DS4Devices_RequestElevation` が新経路（`AppHost.GetService`）優先＋フォールバック（既存直接`Process.Start`）の形になっており、両方が同時実行されることがない
- [ ] `IDs4DeviceRegistry.ReEnableDevice` には一切触れていない
- [ ] `App.xaml.cs` の `parser.ReenableDevice` 枝（子プロセスエントリ）には一切触れていない
- [ ] ビルドが通っている（DS4WinWPF, Actions.Tests, StandaloneTests）
- [ ] `Phase3-Status.md` が更新され、§0.5で判明した `IDeviceStateAccessor` 配線の食い違いが申し送り事項として記録されている

---

## 6. 次のアクション

1. 本設計書の確認を得る。
2. 承認後、3-5-1〜3-5-4を1回の実装作業として着手（新規ファイル3点＋ピンポイント置換1箇所）。
3. ビルド確認後、3-5-5（文書更新）を実施し、`Phase3-Step3-5-Completion-Report.md` を新設。
4. §0.5の `IDeviceStateAccessor` 配線の食い違いについて、対応要否をユーザーに確認する（Step 3-6着手前 or 別途）。
5. Step 3-6（`IProcessInspector`、多重起動チェックの抽象化）へ進む。
