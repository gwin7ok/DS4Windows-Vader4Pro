﻿# Step 3-6 計画書: IProcessInspector（多重起動チェックの抽象化）＋ IDeviceStateAccessor配線修正

作成日: 2026-08-30
対象ブランチ: For-DI-migration-work
前提ドキュメント:
- `docs-forDIMG/MadeByAgent/Phase3-Plan.md`（Step 3-6定義）
- `docs-forDIMG/MadeByAgent/Phase3-Status.md`（§5 申し送り事項）
- `docs-forDIMG/MadeByAgent/Phase3-Step3-5-IElevatedProcessLauncher-Design.md`（§0.5、食い違いの初出）
- `docs-forDIMG/MadeByAgent/Phase3-Step3-5-Completion-Report.md`
- `.github/copilot-instructions.md`

## ルール確認（作業開始前に毎回読む）

- §2.1 修正版: 古い方式を残して移行OK。新方式の動作確認後に削除。複数候補同時実装はNG。
- §2.2 機能100%維持、§2.3 ログ維持。
- §3.1 コンテナ登録は `AppHost.cs`（またはそこから呼ばれる拡張）に行う。
- §3.2 巨大ファイル（`ScpUtil.cs` 内の `Global` クラス）はピンポイント置換のみ。全体再生成しない。
- §4: マイクロステップ。1ステップ完了ごとに確認を挟む。

本ステップはユーザー指示により、Phase3-Status.md §5の申し送り事項（`IDeviceStateAccessor`配線の食い違い）への対応（(a) Step 3-6と並行して対応）を **Step 3-6-A** として内包し、Phase3-Plan.md本来のStep 3-6内容（`IProcessInspector`）を **Step 3-6-B** として実施する2部構成とする。

---

## 0. 着手前調査で判明した事実

### 0.1 `Process.GetProcesses()` の実装箇所の再確認（`ScpUtil.cs`）

Phase3-Plan §0.4で「分類②（多重起動チェック）は `ScpUtil.cs` 側に存在する想定のまま」とされていた箇所を実測で再確認した。

**重要な追加発見**: この `Process.GetProcesses()` 呼び出しは、`ScpUtil.cs` 内で定義されている `public class Global`（469メンバーのGod Object。`copilot-instructions.md` §3.2で言及されている「巨大ファイル」の実体）の `LoadProfile` メソッド内に存在する。つまり `ScpUtil.cs` はファイル名こそ `Global.cs` ではないが、`Global` クラス本体を含む巨大ファイルであり、本ステップの対象コードは §3.2の「巨大ファイル編集方針（ピンポイント置換のみ）」の対象そのものである。

該当箇所（`Global.LoadProfile` 内、プロファイルの `LaunchProgram` 設定に基づき関連アプリを自動起動する機能）:

```csharp
if (launchprogram == true && launchProgram[device] != string.Empty)
{
    string programPath = launchProgram[device];
    System.Diagnostics.Process[] localAll = System.Diagnostics.Process.GetProcesses();
    bool procFound = false;
    for (int procInd = 0, procsLen = localAll.Length; !procFound && procInd < procsLen; procInd++)
    {
        try
        {
            string temp = localAll[procInd].MainModule.FileName;
            if (temp == programPath)
            {
                procFound = true;
            }
        }
        // Ignore any process for which this information
        // is not exposed
        catch { }
    }

    if (!procFound)
    {
        Task processTask = new Task(() =>
        {
            Thread.Sleep(5000);
            System.Diagnostics.Process tempProcess = new System.Diagnostics.Process();
            tempProcess.StartInfo.FileName = programPath;
            tempProcess.StartInfo.WorkingDirectory = new FileInfo(programPath).Directory.ToString();
            //tempProcess.StartInfo.UseShellExecute = false;
            try { tempProcess.Start(); }
            catch { }
        });

        processTask.Start();
    }
}
```

**意味の確認**: これは「DS4Windows自身の多重起動防止」ではなく、**プロファイルに紐付いた外部プログラム（`LaunchProgram`設定）が既に起動済みかどうかをチェックし、未起動なら5秒後に起動する**機能である。Phase3-Plan §0.4のいう「分類②（多重起動チェック）」の実体はこれである。

**DS4Windows自身の多重起動防止**は別の仕組み（`App.xaml.cs` の `EventWaitHandle`／`SingleAppComEventName`によるIPC名前付きイベント、および `ipcSingleTaskMutex` という別の `Mutex`）で行われており、**本ステップのスコープ外**（`Process.GetProcesses()` を使っていない、全く別の仕組みのため）。

### 0.2 `IProcessInspector` として抽象化すべき範囲の確定

上記0.1の `procFound` 判定ループのみを対象とする。`Task` による5秒待機後の起動処理（`tempProcess.Start()`）は「単純起動」であり、`IProcessLauncher`（Phase1 C5で既存）の責務と重複するが、今回のスコープでは変更しない（Phase3-Plan §2 Step3-6の完了基準が「多重起動チェック（`IsProcessRunning`相当）の抽象化」のみを指定しているため）。

### 0.3 `IDeviceStateAccessor` 配線の食い違いの根本原因（§0.5の深掘り）

Phase3-Status.md §5で申し送りとした食い違いについて、実コードを深掘りしたところ、当初の想定より根が深いことが判明した。

**発見1: DIコンテナが2つ並存している**

| コンテナ | 定義場所 | 構築タイミング | 登録内容 |
|---|---|---|---|
| `DS4Windows.DI.ServiceProviderHolder` | `DS4Windows/DI/ServiceProviderHolder.cs` | `App.xaml.cs` 起動処理の中盤（`ServiceCollection` を手動構築） | `IActionFactory`, `IManagedActionManager`, `IKeyActionCreator`, `IKeyButtonActionControllerFactory`, `IControllerRegistry` のみ（Actions系、Phase1由来の「古い簡易版」。コード中のコメント: 「古い簡易ServiceCollectionは削除せず残す」） |
| `DS4WinWPF.AppHost` | `DS4Windows/DI/AppHost.cs` | `ServiceProviderHolder` 構築の直後、`AppHost.CreateHost()` にて `Microsoft.Extensions.Hosting.Host` を構築（コード中のコメント: 「フェーズ0-3: AppHost正式ルートの動作確認」） | `ServiceRegistration.AddAppServices()` 経由。`IProfileSettingsService`, `IVirtualKBM`, `IDs4DeviceRegistry`, `IElevatedProcessLauncher`（Step 3-5で追加） |

`Mapping.cs` 6504行目付近は **`ServiceProviderHolder.Provider`（＝古い簡易版、Actions系専用）** を参照している。`IDeviceStateAccessor` はそもそも **どちらのコンテナにも登録されていない**ため、`Mapping.cs` 側の参照先を仮に `AppHost.GetService` に変更するだけでは解決しない（`AppHost` 側にも登録が必要）。

**発見2: `IDeviceStateAccessor` の実装元（`ControlService`）はDIコンテナ管理下にない**

```csharp
// ControlService.cs 41行目
public class ControlService : IInstanceIdentifiable, DS4Windows.Services.IDeviceStateAccessor
```

`ControlService` は `IDeviceStateAccessor` を実装しているが、そのインスタンスは `App.xaml.cs` の `CreateControlService` メソッド内で `new DS4Windows.ControlService(parser, registry)` により手動生成され、`Program.rootHub` として保持される。DIコンテナ（`AppHost`）を通して生成されたインスタンスではない。

このため、単純に `services.AddSingleton<IDeviceStateAccessor, ControlService>();` を追加すると、DIコンテナが**独自に新しい `ControlService` インスタンスを生成してしまい**、実際に動作中の `Program.rootHub`（実機のコントローラ一覧を保持する本物のインスタンス）とは別物になる。これは重大な機能不具合（`GetController` が常に空のデバイスリストを参照する）を引き起こすため、絶対に避けなければならない。

**正しい修正方針**: `Program.rootHub` を指すファクトリ委譲で登録する。

```csharp
services.AddSingleton<IDeviceStateAccessor>(sp => (IDeviceStateAccessor)DS4Windows.Program.rootHub);
```

`AddSingleton` のファクトリ形式は「初回解決時に評価され、以後キャッシュされる」という遅延評価の性質を持つ。`App.xaml.cs` の起動順序は「`AppHost.CreateHost()` → `CreateControlService(parser)`（`Program.rootHub` 設定）」であり、`Mapping.cs` 側で `IDeviceStateAccessor` が実際に解決されるタイミング（ゲームパッドのランブルイベント発生時）は起動完了後・`Program.rootHub` 設定後であるため、実運用上は問題ない。ただし「起動シーケンス中に誰かが `Program.rootHub` 設定前に解決してしまうと `null` がキャッシュされ続ける」というリスクがあり、設計時に明記し実装時に注意する（詳細は §2.1）。

---

## 1. 採用する方針

### 1.1 スコープ

| 項目 | 対応するか | 理由 |
|---|---|---|
| Step 3-6-A: `IDeviceStateAccessor` の配線修正 | **対応する** | ユーザー指示(a)。Phase3-Status.md §5の申し送り事項。Step 3-4/3-Fで「完了」と報告されていた内容が実際には機能していなかった不具合の是正 |
| Step 3-6-B: `IProcessInspector`（多重起動チェック抽象化） | **対応する** | Phase3-Plan.md 本来のStep 3-6スコープ |
| DS4Windows自身の多重起動防止（IPC/Mutex機構） | **対応しない** | §0.1確認の通り別の仕組み（`Process.GetProcesses()`を使わない）であり、Phase3-Plan.md の対象外 |
| `LaunchProgram` の起動処理（`tempProcess.Start()`）の抽象化 | **対応しない** | Phase3-Plan §2 Step3-6の完了基準は「多重起動チェック」の抽象化のみ。起動処理まで広げると`IProcessLauncher`との責務重複が生じ、スコープ膨張になる |

### 1.2 Step 3-6-Aを2つのサブステップに分ける理由

ユーザー指示「ステップを分けた方が良いときはStep3-6のなかで分けて」に従い、以下の2サブステップに分割する。

- **3-6-A-1**: `ServiceRegistration.cs` へのファクトリ登録追加（新規行のみ、既存コード変更なし）
- **3-6-A-2**: `Mapping.cs` 6504行目付近の参照先を `ServiceProviderHolder.Provider` から `AppHost.GetService` に変更（ピンポイント置換）

理由: 3-6-A-1は追加のみで無害だが、3-6-A-2は「現在動いている（ように見えて実際は常にフォールバックしている）コード」を変更するため、影響範囲を分離してレビューしやすくする。3-6-A-1のみ適用してもMapping.cs側は動作不変（既存のフォールバックのまま）であることをステップの境目として明確にする。

---

## 2. Step 3-6-A: `IDeviceStateAccessor` 配線修正

### 2.1 Step 3-6-A-1: DI登録の追加

`DS4Windows/DI/ServiceRegistration.cs` に1行追加。

```csharp
// Phase 3 Step 3-6-A: Device State Accessor (ControlService/Program.rootHub への委譲)
// 注意: ControlServiceはDIコンテナ管理下ではなく、App.xaml.cs の CreateControlService() で
// 手動生成され Program.rootHub に保持される。DIコンテナが独自に新しい ControlService を
// 生成しないよう、必ず Program.rootHub を指すファクトリ委譲で登録すること。
services.AddSingleton<IDeviceStateAccessor>(sp => (IDeviceStateAccessor)DS4Windows.Program.rootHub);
```

**リスク**: `Program.rootHub` が `null` の状態で最初に解決されると、`AddSingleton` のキャッシュ機構により `null` が固定されてしまう。ただし現状の唯一の呼び出し元（`Mapping.cs` のラムブルイベント処理）はゲームパッド接続後にのみ発火するため、起動シーケンス上のリスクは低いと判断する。念のため、Step 3-6-A-2側のフォールバック（既存の `Program.rootHub?.DS4Controllers[device]` 直接参照）を残すことでこのリスクを吸収する（§2.1修正版の「古い方式を残す」原則にも合致）。

### 2.2 Step 3-6-A-2: `Mapping.cs` の参照先修正（ピンポイント置換）

現状（`Mapping.cs` 6504行目付近）:

```csharp
DS4Device d = null;
try
{
    var accessor = DS4Windows.DI.ServiceProviderHolder.Provider?
        .GetService(typeof(DS4Windows.Services.IDeviceStateAccessor)) as DS4Windows.Services.IDeviceStateAccessor;
    if (accessor != null) d = accessor.GetController(device);
}
catch { }
if (d == null) d = Program.rootHub?.DS4Controllers[device];
```

変更後:

```csharp
DS4Device d = null;
try
{
    var accessor = DS4WinWPF.AppHost.GetService<DS4Windows.Services.IDeviceStateAccessor>();
    if (accessor != null) d = accessor.GetController(device);
}
catch { }
if (d == null) d = Program.rootHub?.DS4Controllers[device];
```

変更点は参照先コンテナのみ（`ServiceProviderHolder.Provider` → `AppHost.GetService`）。フォールバック（`Program.rootHub?.DS4Controllers[device]`）はそのまま維持する。

---

## 3. Step 3-6-B: `IProcessInspector`（多重起動チェックの抽象化）

### 3.1 インターフェース

配置: `DS4Windows/DS4Control/Services/IProcessInspector.cs`（Step 3-1〜3-5と同じ `DS4Windows.Services` namespace）。

```csharp
namespace DS4Windows.Services
{
    /// <summary>
    /// 指定した実行ファイルパスのプロセスが既に起動しているかを調べる抽象化。
    /// Global.LoadProfile 内の LaunchProgram（プロファイル関連付けアプリの自動起動）
    /// における多重起動防止チェック専用。Phase 3 Step 3-6.
    /// </summary>
    public interface IProcessInspector
    {
        /// <summary>
        /// 実行中の全プロセスを走査し、MainModule.FileName が exePath と一致するものが
        /// あるかどうかを返す。情報取得に失敗したプロセス（アクセス権限不足等）は無視する。
        /// </summary>
        bool IsProcessRunning(string exePath);
    }
}
```

### 3.2 既定実装

配置: `DS4Windows/DS4Control/Services/DefaultProcessInspector.cs`。既存ループロジックをそのまま移設。

```csharp
using System.Diagnostics;

namespace DS4Windows.Services
{
    public class DefaultProcessInspector : IProcessInspector
    {
        public bool IsProcessRunning(string exePath)
        {
            Process[] localAll = Process.GetProcesses();
            bool procFound = false;
            for (int procInd = 0, procsLen = localAll.Length; !procFound && procInd < procsLen; procInd++)
            {
                try
                {
                    string temp = localAll[procInd].MainModule.FileName;
                    if (temp == exePath)
                    {
                        procFound = true;
                    }
                }
                // Ignore any process for which this information
                // is not exposed
                catch { }
            }
            return procFound;
        }
    }
}
```

### 3.3 `Global.LoadProfile` 内のピンポイント置換

現状（`ScpUtil.cs`、`Global.LoadProfile` 内）:

```csharp
if (launchprogram == true && launchProgram[device] != string.Empty)
{
    string programPath = launchProgram[device];
    System.Diagnostics.Process[] localAll = System.Diagnostics.Process.GetProcesses();
    bool procFound = false;
    for (int procInd = 0, procsLen = localAll.Length; !procFound && procInd < procsLen; procInd++)
    {
        try
        {
            string temp = localAll[procInd].MainModule.FileName;
            if (temp == programPath)
            {
                procFound = true;
            }
        }
        // Ignore any process for which this information
        // is not exposed
        catch { }
    }

    if (!procFound)
    {
        ...（起動処理、変更なし）...
    }
}
```

変更後（新経路優先＋フォールバック、`handled` フラグで一本化）:

```csharp
if (launchprogram == true && launchProgram[device] != string.Empty)
{
    string programPath = launchProgram[device];
    bool procFound = false;
    bool handled = false;
    try
    {
        var inspector = DS4WinWPF.AppHost.GetService<DS4Windows.Services.IProcessInspector>();
        if (inspector != null)
        {
            procFound = inspector.IsProcessRunning(programPath);
            handled = true;
        }
    }
    catch { }

    if (!handled)
    {
        System.Diagnostics.Process[] localAll = System.Diagnostics.Process.GetProcesses();
        for (int procInd = 0, procsLen = localAll.Length; !procFound && procInd < procsLen; procInd++)
        {
            try
            {
                string temp = localAll[procInd].MainModule.FileName;
                if (temp == programPath)
                {
                    procFound = true;
                }
            }
            // Ignore any process for which this information
            // is not exposed
            catch { }
        }
    }

    if (!procFound)
    {
        ...（起動処理、変更なし）...
    }
}
```

`ScpUtil.cs` は `Global` クラス本体を含む巨大ファイルのため、置換対象はこのブロックのみとし、`LoadProfile` メソッド全体や `Global` クラスの他部分には一切触れない。

### 3.4 DI登録

`ServiceRegistration.cs` に1行追加（Step 3-5と同じ運用方針）:

```csharp
// Phase 3 Step 3-6-B: Process Inspector (multi-launch check)
services.AddSingleton<IProcessInspector, DefaultProcessInspector>();
```

---

## 4. タスク分割

| ステップ | 内容 | 変更ファイル |
|---|---|---|
| **3-6-A-1** | `IDeviceStateAccessor` のファクトリ登録追加 | `DS4Windows/DI/ServiceRegistration.cs`（追加のみ） |
| **3-6-A-2** | `Mapping.cs` の参照先修正（`ServiceProviderHolder`→`AppHost`） | `DS4Windows/DS4Control/Mapping.cs`（ピンポイント置換1箇所） |
| **3-6-B-1** | `IProcessInspector` インターフェース新設 | `DS4Windows/DS4Control/Services/IProcessInspector.cs`（新規） |
| **3-6-B-2** | `DefaultProcessInspector` 実装 | `DS4Windows/DS4Control/Services/DefaultProcessInspector.cs`（新規） |
| **3-6-B-3** | DI登録 | `DS4Windows/DI/ServiceRegistration.cs`（追加のみ） |
| **3-6-B-4** | `Global.LoadProfile`（`ScpUtil.cs`）のピンポイント置換 | `DS4Windows/DS4Control/ScpUtil.cs`（メソッド内1ブロックのみ） |
| **3-6-5** | ビルド確認・文書更新（`Phase3-Status.md`、完了報告書新設） | ビルドのみ、mdファイル |

Step 3-5と同じ運用: 新規ファイル追加系（3-6-B-1〜3-6-B-3）は既存コードに影響しないため、既存コード変更を伴う3-6-A-2／3-6-B-4とまとめて1回の実装作業として着手することを想定。

---

## 5. リスクと回避策

| リスク | 該当ステップ | 回避策 |
|---|---|---|
| `Program.rootHub` が `null` の状態でDI解決され、`null` がSingletonキャッシュされる | 3-6-A-1 | フォールバック（`Program.rootHub?.DS4Controllers[device]` 直接参照）を維持することでリスクを吸収。実機でのラムブル動作確認を完了判定基準に含める |
| `Mapping.cs` の参照先変更により、既存の（実際には常にフォールバックしていた）挙動が変化する可能性 | 3-6-A-2 | Step 3-6-A-1で正しく登録されていれば、新経路が正常に解決されるようになるだけで、`GetController` の返す値自体は同じ（`Program.rootHub.DS4Controllers[device]`）。実機でのラムブル動作確認で最終確認する |
| `Global`（`ScpUtil.cs`）内の巨大な `LoadProfile` メソッドを誤って広範囲に変更してしまう | 3-6-B-4 | 対象ブロック（`procFound` 判定ループのみ）を寸分違わずピンポイント置換。前後の起動処理コード（`Task processTask = ...`）には一切触れない |
| `handled` フラグの分岐漏れで新旧が二重実行される | 3-6-B-4 | Step 3-5と同一パターンのため実装ミスのリスクは低いが、コードレビュー時に確認する |
| 改行コード（CRLF/LF）差異によるピンポイント置換の不一致（Step 3-5-4で発生した問題） | 全ピンポイント置換ステップ | Step 3-5完了報告書§4.1の手法（比較前にLF正規化、書き戻し前に元の改行コードへ復元）をスクリプトに標準実装する |

---

## 6. 完了判定基準

### Step 3-6-A（配線修正）

- [ ] `ServiceRegistration.cs` に `IDeviceStateAccessor` のファクトリ登録（`Program.rootHub` 委譲）が追加されている
- [ ] `Mapping.cs` の参照先が `AppHost.GetService<IDeviceStateAccessor>()` に変更され、フォールバックが維持されている
- [ ] 実機でのラムブル動作確認（Mapping.cs 6504行目付近の経路が実際に新経路で解決されることの確認）

### Step 3-6-B（IProcessInspector）

- [ ] `IProcessInspector` が新設され、`IsProcessRunning` のシグネチャが本計画書通り
- [ ] `DefaultProcessInspector` が既存の `procFound` 判定ループをそのまま移設したものである（ロジック変更なし）
- [ ] `ServiceRegistration.cs` に `IProcessInspector` のSingleton登録が追加されている
- [ ] `Global.LoadProfile`（`ScpUtil.cs`）の `procFound` 判定部分のみが新経路優先＋フォールバックの形に置換され、起動処理部分（`Task processTask`）には触れていない
- [ ] DS4Windows自身の多重起動防止機構（IPC/Mutex）には一切触れていない

### 共通

- [ ] ビルド・テストビルド・テスト実行が全て成功する
- [ ] `Phase3-Status.md` が更新され、Step 3-6完了・§5の申し送り事項解消が記録されている
- [ ] `Phase3-Step3-6-Completion-Report.md` が作成されている

---

## 7. 次のアクション

1. 本計画書の確認を得る。
2. 承認後、実装作業に着手（3-6-A-1〜3-6-A-2、3-6-B-1〜3-6-B-4を1回の実装作業としてまとめる想定。改行コード正規化ロジックを標準搭載したスクリプトで提供）。
3. ビルド確認後、3-6-5（文書更新）を実施。
4. フェーズ3全体の完了判定（Phase3-Plan.md §5）を確認し、フェーズ4（Global分割＋ViewModel DI化）着手の要否を判断する。
