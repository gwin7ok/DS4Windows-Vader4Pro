# Phase 5 Step14前クリーンアップ計画書: `Global.*`残存参照の解消と`Program.rootHub`直接代入の統一

作成日: 2026-09-09
対象ブランチ: `For-DI-migration-work`
背景資料: `Phase5-Watchpoints-Investigation-Report.md`（2026-09-09訂正版）、
`Phase5-Step13+14-Addendum-Findings-Report-Part1.md`／`-Part2.md`
目的: Phase5-Step14の実機テスト着手前に、`MainWindow.xaml.cs`の`Global.*`残存参照と、
4ファイルに残る`Program.rootHub`無条件直接代入を解消し、DI経由へ統一する。

---

## 0. 前提（実コード確認結果）

`git clone`によるリポジトリ全文検索で以下を確認済み：

- `MainWindow.xaml.cs`内の`Global.*`参照は実質24シンボル・36箇所（コメントアウト2件を除く）。
- 既存DIサービス一覧（`DS4Windows/DI/`）を確認した結果、全体プラン書§5.4 #9で計画されていた
  `IAppearanceSettingsService`が**Phase4・Phase5のどちらでも未実装**であることが判明した。
- `AppSettingsService.cs`の既存プロパティ（`Notifications`等）は、過去の「孤立プロパティバグ」
  修正後、`Global`への薄いシムとして実装されている（独立フィールドを持たない）。新規追加分も
  同一パターンに揃える。
- `Program.rootHub`の無条件直接代入（フォールバックなし）は`MainWindow.xaml.cs`, `StickCalibrationWindow.xaml.cs`,
  `BindingWindow.xaml.cs`, `ProfileEditor.xaml.cs`の4ファイル。`ServiceRegistration.cs`の
  DIファクトリ自体が`Program.rootHub`と同一インスタンスを返すため、統一しても機能変化はない。

---

## 1. 対象棚卸しと受け皿の割り当て

| 分類 | 対象シンボル | 受け皿 | 対応要否 |
|---|---|---|---|
| ① 真の定数 | `Global.TEST_PROFILE_INDEX`, `Global.RESOURCES_PREFIX` | `Global`の`const`参照を継続 | **対応不要**（元プラン§4.1「純粋関数群」相当） |
| ② `IAppSettingsService`拡張 | `LogMinLevel`, `CheckUpdateStartupEnabled`, `CheckEveryValue`, `CheckEveryUnit`, `LastChecked`, `LastVersionCheckedNum`, `firstRun`, `runHotPlug` | 8プロパティ追加 | **対応（低リスク）** |
| ③ `IPathService`拡張 | `exedirpath`（既存`ExecutableDirectory`で代替可）, `appDataPpath`（既存`AppDataPath`で代替可）, `exelocation`（新規`ExecutablePath`要） | 1プロパティ追加＋既存2件の呼び出し置換 | **対応（低リスク）** |
| ④ `IEnvironmentService`拡張 | `IsAdministrator()`, `exeversion`, `RefreshHidHideInfo()`, `RefreshFakerInputInfo()` | 4メンバ追加 | **対応（低リスク）** |
| ⑤ `IAppearanceSettingsService`新設 | `UseCurrentTheme`, `iconChoiceResources`, `UseIconChoice` | 新設サービス | **対応（要実機確認）** |

---

## 2. 設計詳細

### 2.1 `IAppSettingsService`拡張（分類②）

```csharp
// IAppSettingsService.cs 追加分
string LogMinLevel { get; set; }
bool CheckUpdateStartupEnabled { get; set; }
int CheckEveryValue { get; set; }
int CheckEveryUnit { get; set; }
DateTime LastChecked { get; set; }
ulong LastVersionCheckedNum { get; }
bool FirstRun { get; set; }
bool RunHotPlug { get; set; }
```

```csharp
// AppSettingsService.cs 追加分（Global.Notifications と同一の薄いシムパターン）
public string LogMinLevel
{
    get => Global.LogMinLevel;
    set { if (Global.LogMinLevel != value) { Global.LogMinLevel = value; NotifyChanged(nameof(LogMinLevel)); } }
}

public bool CheckUpdateStartupEnabled
{
    get => Global.CheckUpdateStartupEnabled;
    set { if (Global.CheckUpdateStartupEnabled != value) { Global.CheckUpdateStartupEnabled = value; NotifyChanged(nameof(CheckUpdateStartupEnabled)); } }
}

public int CheckEveryValue
{
    get => Global.CheckEveryValue;
    set { if (Global.CheckEveryValue != value) { Global.CheckEveryValue = value; NotifyChanged(nameof(CheckEveryValue)); } }
}

public int CheckEveryUnit
{
    get => Global.CheckEveryUnit;
    set { if (Global.CheckEveryUnit != value) { Global.CheckEveryUnit = value; NotifyChanged(nameof(CheckEveryUnit)); } }
}

public DateTime LastChecked
{
    get => Global.LastChecked;
    set { if (Global.LastChecked != value) { Global.LastChecked = value; NotifyChanged(nameof(LastChecked)); } }
}

public ulong LastVersionCheckedNum => Global.LastVersionCheckedNum; // 読み取り専用（Global側も読み取り専用）

public bool FirstRun
{
    get => Global.firstRun;
    set { if (Global.firstRun != value) { Global.firstRun = value; NotifyChanged(nameof(FirstRun)); } }
}

public bool RunHotPlug
{
    get => Global.runHotPlug;
    set { if (Global.runHotPlug != value) { Global.runHotPlug = value; NotifyChanged(nameof(RunHotPlug)); } }
}
```

`MainWindow.xaml.cs`側の置換例：

```csharp
// Before
logMinLevelComboBox.SelectedValue = Global.LogMinLevel;
// After（appSettingsServiceは既にコンストラクタで注入済み）
logMinLevelComboBox.SelectedValue = appSettingsService.LogMinLevel;
```

同様のパターンで `CheckUpdateStartupEnabled`, `CheckEveryValue`, `CheckEveryUnit`, `LastChecked`,
`LastVersionCheckedNum`, `firstRun`, `runHotPlug` の呼び出し箇所（計8箇所前後）を置換する。

### 2.2 `IPathService`拡張（分類③）

```csharp
// IPathService.cs 追加分
string ExecutablePath { get; }
```

```csharp
// PathService.cs 追加分
public string ExecutablePath => Global.exelocation;
```

`MainWindow.xaml.cs`側：

```csharp
// Before
notifyIcon.CustomName = Global.exelocation;
startInfo.FileName = Global.exelocation;
// After
notifyIcon.CustomName = pathService.ExecutablePath;
startInfo.FileName = pathService.ExecutablePath;
```

```csharp
// Before
if (pathService.AppDataPath != Global.exedirpath)
    dialog.InitialDirectory = Path.Combine(Global.appDataPpath, "Profiles");
else
    dialog.InitialDirectory = Global.exedirpath + @"\Profiles\";
// After
if (pathService.AppDataPath != pathService.ExecutableDirectory)
    dialog.InitialDirectory = Path.Combine(pathService.AppDataPath, "Profiles");
else
    dialog.InitialDirectory = pathService.ExecutableDirectory + @"\Profiles\";
```

（`Global.exedirpath` → `pathService.ExecutableDirectory`、`Global.appDataPpath` → `pathService.AppDataPath`
は既存メンバで代替可能なため、`IPathService`自体への追加は`ExecutablePath`の1件のみ）

### 2.3 `IEnvironmentService`拡張（分類④）

```csharp
// IEnvironmentService.cs 追加分
bool IsAdministrator();
string ApplicationVersion { get; }
void RefreshHidHideInfo();
void RefreshFakerInputInfo();
```

```csharp
// EnvironmentService.cs 追加分
public bool IsAdministrator() => Global.IsAdministrator();
public string ApplicationVersion => Global.exeversion;
public void RefreshHidHideInfo() => Global.RefreshHidHideInfo();
public void RefreshFakerInputInfo() => Global.RefreshFakerInputInfo();
```

`MainWindow.xaml.cs`側の置換対象: `Global.IsAdministrator()`（2箇所）, `Global.exeversion`（1箇所）,
`Global.RefreshHidHideInfo()` / `Global.RefreshFakerInputInfo()`（各1箇所）。

> **設計上の注記**: `IEnvironmentService`は現状「永続化されるウィンドウ・起動設定」を扱うサービスとして
> 実装されているのに対し、`IsAdministrator()`等は「実行時のシステム状態の読み取り専用プローブ」であり、
> 厳密には性質が異なる。本計画では既存インターフェース名（Environment）の意味範囲内と判断し追加する方針
> とするが、将来的に責務が肥大化するようであれば`ISystemInfoProvider`のような別インターフェースへの
> 分離を検討してもよい（Phase5のスコープでは過剰設計を避けるため見送る）。

### 2.4 `IAppearanceSettingsService`新設（分類⑤）

```csharp
// DS4Windows/DI/IAppearanceSettingsService.cs（新規）
using System.Collections.Generic;

namespace DS4Windows.DI
{
    public interface IAppearanceSettingsService
    {
        AppThemeChoice UseCurrentTheme { get; set; }
        TrayIconChoice UseIconChoice { get; set; }
        string GetIconResourcePath(TrayIconChoice choice);
    }
}
```

```csharp
// DS4Windows/DS4Control/Services/AppearanceSettingsService.cs（新規、Globalへの薄いシム）
namespace DS4Windows.Services
{
    public class AppearanceSettingsService : DS4Windows.DI.IAppearanceSettingsService
    {
        public AppThemeChoice UseCurrentTheme
        {
            get => Global.UseCurrentTheme;
            set => Global.UseCurrentTheme = value;
        }

        public TrayIconChoice UseIconChoice
        {
            get => Global.UseIconChoice;
            set => Global.UseIconChoice = value;
        }

        public string GetIconResourcePath(TrayIconChoice choice) => Global.iconChoiceResources[choice];
    }
}
```

`ServiceRegistration.cs`へSingleton登録を追加。`MainWindow.xaml.cs`側：

```csharp
// Before
AppThemeChoice choice = Global.UseCurrentTheme;
...
trayIconVM.IconSource = Global.iconChoiceResources[Global.UseIconChoice];
// After
AppThemeChoice choice = appearanceSettingsService.UseCurrentTheme;
...
trayIconVM.IconSource = appearanceSettingsService.GetIconResourcePath(appearanceSettingsService.UseIconChoice);
```

---

## 3. `Program.rootHub`無条件直接代入の統一（項目2）

| ファイル:行 | Before | After |
|---|---|---|
| `MainWindow.xaml.cs:113` | `controlService = Program.rootHub;` | `controlService = DS4WinWPF.AppHost.GetService<DS4Windows.ControlService>() ?? Program.rootHub;` |
| `StickCalibrationWindow.xaml.cs:20` | `_controlService = Program.rootHub;` | `_controlService = DS4WinWPF.AppHost.GetService<DS4Windows.ControlService>() ?? Program.rootHub;` |
| `BindingWindow.xaml.cs:67` | `controlService = DS4Windows.Program.rootHub;` | `controlService = DS4WinWPF.AppHost.GetService<DS4Windows.ControlService>() ?? DS4Windows.Program.rootHub;` |
| `ProfileEditor.xaml.cs:279` | `controlService = DS4Windows.Program.rootHub;` | `controlService = DS4WinWPF.AppHost.GetService<DS4Windows.ControlService>() ?? DS4Windows.Program.rootHub;` |

`ServiceRegistration.cs`のDIファクトリ（`services.AddSingleton<ControlService>(sp => Program.rootHub ?? new ControlService(...))`）
により、`AppHost.GetService<ControlService>()`は常に`Program.rootHub`と同一インスタンスを返す。
したがって本変更は**機能的には無変化（no-op）のスタイル統一**であり、4箇所とも既存の同一ファイル内の
他サービス初期化と同じ書式にするだけである。

---

## 4. PR粒度と実施順序（推奨）

1. **PR-A（最優先・最低リスク）**: `Program.rootHub`統一（§3）。4行のみの機械的置換。
2. **PR-B**: `IAppSettingsService`拡張（§2.1）＋`MainWindow.xaml.cs`側の該当8箇所置換。
3. **PR-C**: `IPathService`拡張（§2.2）＋`MainWindow.xaml.cs`側の該当4箇所置換。
4. **PR-D**: `IEnvironmentService`拡張（§2.3）＋`MainWindow.xaml.cs`側の該当5箇所置換。
5. **PR-E（要実機確認）**: `IAppearanceSettingsService`新設（§2.4）＋DI登録＋`MainWindow.xaml.cs`側の
   該当2箇所置換。テーマ切替・トレイアイコン表示の目視確認を実施。

PR-A〜Dはいずれも「既存の確立された薄いシムパターンへの機械的追加」であり、ビルド・既存単体テストへの
影響はない（新規プロパティの追加のみで、既存シグネチャの変更を伴わない）。PR-Eのみ新規サービスのため、
最小限の実機確認（テーマ変更・トレイアイコン表示）を推奨する。

---

## 5. 完了判定基準

- `MainWindow.xaml.cs`から`Global.*`直接参照が、分類①（真の定数2件）を除きゼロになること。
- `Program.rootHub`の無条件直接代入が、4ファイルとも`AppHost.GetService<ControlService>() ??`形式に統一されること。
- 既存の自動テスト（Actions/Standalone）が全件成功を維持すること。
- PR-Eについては、テーマ切替・トレイアイコン表示が従来通り動作することを目視確認すること。

---

## 6. リスク

| リスク | 対応 |
|---|---|
| `IAppearanceSettingsService`が新設サービスであるため、DI未登録時にNull参照が発生する可能性 | `MainWindow`側では他サービス同様`?? Global`直呼び出しへのフォールバックを当面残す（Strangler Fig） |
| `IEnvironmentService`への責務追加（設定 vs システムプローブの混在） | 本計画では許容するが、肥大化時は分離を将来検討する旨を明記済み（§2.3注記） |
| 8+4+1個のプロパティ追加によるインターフェース肥大化 | 既存の`IAppSettingsService`は既に20項目超を持つため実質的な増分リスクは小さい |