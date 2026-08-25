# DS4Windows-Vader4Pro アプリ全体 DI化 最終プラン書

作成日: 2026-08-25
対象ブランチ: `インターフェース化`
対象リポジトリ: https://github.com/gwin7ok/DS4Windows-Vader4Pro
保存想定パス: `docs/DI-App-Wide-Migration-Plan.md`

本書は `01-現状分析.md` 〜 `05-段階移行ロードマップ.md`（調査ステージ1〜4）および
`04-理想の3層構造と現状のズレ.md`（補足ステージ）で行った調査・設計を統合し、
単独で読んで完結する**最終プラン書**として一本化したものである。

---

## 0. エグゼクティブサマリー

DS4Windows-Vader4Pro は、コントローラー入力をポーリングで監視し（入力監視層）、設定に基づいて
出力内容を決定し（信号変換層）、仮想コントローラーやキーボード/マウスとして実際に出力する（信号出力層）、
という3層のデータフローを持つ .NET 8 / WPF デスクトップアプリである。

現状の最大の課題は、**約469個の `public static` メンバを持つ `Global` クラス（God Object）**、
**`Program.rootHub` という静的シングルトン**、**33個のViewModelすべてが `new` で直接生成される**こと、
そして**信号変換層（`Mapping.cs`、8,827行）が「決定」だけでなく出力の「実行」まで直接担ってしまっている**
ことにある。これらにより、単体テストによる検証がほぼ不可能な状態になっている。

既にリポジトリの `docs/` には `Mapping.cs` の副作用切り出しに限定した移行計画（`DI-Migration-Plan.md` 等3点）
が存在するが、これは**アプリ全体のDI化ではなく、Actionsサブシステムに限定したスコープ**である。
本プランは、この既存3点を「フェーズ1」として取り込みつつ、その上位に**アプリ全体のDI化ロードマップ**を
新設するものである。

移行は、データフロー3層モデル（後述）における**「変換層が抱える出力実行の権限を、影響範囲が小さいものから
順に剥がしていく」**という考え方を軸に、以下の6フェーズ・**概算12〜18週間**で進める。

| フェーズ | 内容 | 見積もり |
|---|---|---|
| 0. 基盤整備 | DIパッケージ導入、`AppHost.cs` 正式運用化 | 1〜2日 |
| 1. SpecialAction判定・実行の分離 | 既存 `DI-Migration-Plan.md` を採用 | 2〜4週間 |
| 2. KBM出力の抽象化 | `IVirtualKBM` | 3〜5日 |
| 3. 入力監視層・信号変換層の整理 | `IDs4DeviceRegistry`, 循環依存解消 | 1〜2週間 |
| 4. `Global` 分割 + ViewModel DI化 | 10サービス + 33ViewModel | 6〜8週間 |
| 5. 仕上げ・整流化 | シム削除判断、`AppHost` 完全移行 | 1〜2週間 |

---

## 1. アプリ概要

DS4Windows-Vader4Pro は DualShock4 / DualSense / Switch Pro Con / Joy-Con などのゲームコントローラーを
Windows 上で Xbox360/DualShock4 相当の仮想デバイスとして再マッピングし、キーボード/マウス出力・
マクロ・特殊アクション（プロファイル切替、外部プログラム起動等）を行う .NET 8 / WPF デスクトップアプリ。

- `OutputType`: WinExe / `TargetFramework`: net8.0-windows / `UseWPF`: true
- UI は WPF + 疑似 MVVM（View: `DS4Forms/*.xaml`、ViewModel: `DS4Forms/ViewModels/*.cs` 33ファイル）
- コア処理系は `DS4Control` 名前空間に集約
- テストプロジェクトが2系統存在（`DS4WindowsTests`＝xUnit、`StandaloneTests`）

### 1.1 主要モジュールとサイズ

| ファイル/フォルダ | 行数/サイズ | 役割 |
|---|---|---|
| `DS4Windows/App.xaml.cs` | 1,044行 | アプリ起動シーケンス（Composition Root相当） |
| `DS4Windows/DS4Control/ScpUtil.cs` | 11,077行 / 548KB | 設定永続化。**`Global` 静的クラスの実体** |
| `DS4Windows/DS4Control/Mapping.cs` | 8,827行 / 456KB | 入力→出力マッピングの中核。副作用の直接呼び出しが集中 |
| `DS4Windows/DS4Control/ControlService.cs` | 3,300行 / 144KB | デバイス検出〜出力デバイス管理の中枢サービス |
| `DS4Windows/DS4Forms/MainWindow.xaml.cs` | 2,104行 | メインウィンドウ（View + 一部ロジック） |
| `DS4Windows/DS4Forms/ViewModels/*` | 33ファイル | 各設定画面のViewModel（`new` で直接生成） |
| `DS4Windows/Actions/*` | 17ファイル / 80KB | **既に部分的にDI/インターフェース化済み**の先行事例 |
| `DS4Windows/DI/*` | 3ファイル | DIの雛形（現状ほぼ未使用） |

---

## 2. 現状の問題点

### 2.1 巨大な `Global` static クラス（God Object）
`DS4Windows/DS4Control/ScpUtil.cs` 537行目に定義される `public class Global` は、**約3,180行・
469個の `public static` メンバ**（メソッド概算235個、フィールド/プロパティ概算175個）を持つ。
プロファイル、言語、テーマ、デバイスオプション、パス、フラグ等アプリ設定のほぼ全てがここに集約し、
**75ファイル**が `Global.xxx` を直接参照している。static のためテスト時のモック差し替えが不可能。

### 2.2 `Program.rootHub` / `App.rootHub` という静的シングルトン
`DS4Windows/DS4Control/Program.cs` の `public static ControlService rootHub;` に、`App.xaml.cs` の
`CreateControlService()` 内で `new DS4Windows.ControlService(parser)` が直接代入される。
**15ファイル**がこの静的フィールド経由で `ControlService` にアクセスしている。

### 2.3 `new` による直接インスタンス化
33個のViewModelはすべて `new` で直接生成され、DIコンテナを経由しない。`ControlService` 自体も
`new` で生成される。View/ViewModel/Model の責務分離も不完全。

### 2.4 直接副作用呼び出しの散在（`Mapping.cs` 中心）
`outputKBMHandler.PerformKeyPress*` 等（KBM出力）、`PlayMacro`（マクロ再生）、`Global.ApplyProfile`
（プロファイル切替）、`Process.Start`（20箇所以上）が `Mapping.cs` から直接呼ばれている。

### 2.5 部分的に先行しているDI領域（`Actions` サブシステム）
`DS4Windows/Actions/` 配下は既にインターフェース設計が進んでいる（`IManagedActionManager`,
`IActionFactory`, `IActionRegistry` 等）。`ActionManager`（static facade）が
`ServiceProviderHolder.Provider` 経由でDI実装へ委譲するアダプタパターンを採用している。
ただし調査の過程で、この先行実装内にも `Global.MAX_DS4_CONTROLLER_COUNT` という残存する `Global` 依存が
1件見つかっており、「先行事例」といえども完全ではない点に留意が必要。

### 2.6 DIコンテナの二重・未整合な初期化
`DS4Windows/DI/AppHost.cs`(`Host.CreateDefaultBuilder()` を使う「正式な」Host構築ルート)が
**用意されているにもかかわらず一切呼び出されていない**。実際に使われているのは `App.xaml.cs` の
`OnStartup` 内でその場限りに `new ServiceCollection()` を組み立てる簡易版であり、`AppHost.cs` /
`ServiceRegistration.cs` は事実上デッドコードになっている。登録されているサービスは
`IActionFactory` 等5つのみで、`ControlService`, ViewModel群, `Global` 相当のサービス, `IVirtualKBM` 等は
DI未登録。

### 2.7 既存ドキュメント資産のスコープ
リポジトリの `docs/` には `DI-Migration-Plan.md`, `DI-Implementation-Guide.md`, `DI/DI-ObjectGraph.md`,
`Direct-Callsites-Inventory.md` という4点のたたき台が存在するが、いずれも
「`Mapping.cs` の副作用を `ActionManager` 経由にする」という**Actionsサブシステム限定の移行計画**であり、
`Global`, `ControlService`, 33個のViewModel、Updater/UI層を含む**アプリ全体のDI化はスコープ外**として
明示的に扱われている（例:「Updater / UI の `Process.Start` 系は仕様上直接呼ぶケースが合理的」との記述）。
→ 本プランはこの4点を「フェーズ1」として取り込みつつ、上位にアプリ全体のロードマップを新設する。

---

## 3. データフロー3層モデル（理想形と現状のズレ）

### 3.1 提示された理想の3層構造

| 層 | 役割 |
|---|---|
| 1. 入力監視層 | コントローラーからの入力信号をポーリングで監視。機種差はここで吸収。感知した入力信号を信号変換層に渡す。 |
| 2. 信号変換層 | 入力信号から最終的な出力信号を決定（1入力→1出力）。SpecialAction成立判定・成立時の元入力の出力防止。マクロの開始判定と各出力信号への分解。最終的に各出力信号を信号出力層へ渡す。 |
| 3. 信号出力層 | 受け取った出力信号を、設定された仮想コントローラー（DualShock4 / Xbox）の規格に従って実際に出力。仮想コントローラーの機種差はここで吸収。 |

### 3.2 実コード追跡による検証結果

| 提案の層 | 対応する既存コード | 一致度 | 備考 |
|---|---|---|---|
| 1. 入力監視層 | `DS4Device`（`DS4Library`） | ◎ ほぼ一致 | デバイスごとに専用スレッド `ds4Input` でHIDレポートをポーリング。機種差の吸収は既に達成済み |
| 2. 信号変換層 | `Mapping.cs` + `ControlService.On_Report` | △ 一致するが責務過多 | 「決定」だけでなく `outputKBMHandler` 呼び出し・`PlayMacro`・`Global.ApplyProfile`・`Process.Start` という「出力実行」まで直接担っている |
| 3. 信号出力層（仮想コントローラー） | `outputDevices[ind]` | ◎ 概念は一致（範囲が狭い） | `Xbox360OutDevice`/`DS4OutDeviceBasic`等。仮想コントローラー出力のみを指しており、KBM出力等が含まれていない |

### 3.3 拡張版3層モデル（改訂版: SpecialActionの「判定」と「実行」を分離）

実コード調査の結果、「仮想コントローラー出力」以外に、変換層（`Mapping.cs`）から**直接**呼ばれている
出力系統（KBM出力、マクロ実行、プロファイル切替、外部プログラム起動）が判明した。これらは性質上
「出力層」に属するべきものだが、現状は変換層に埋め込まれている。

ただし、これらの出力系統のうち **SpecialAction（マクロ／プロファイル切替／プロセス起動）については、
「実行そのもの」と「実行すべきかどうかの判定・どのアクションかの選択」が異なる性質を持つ**ことに注意が
必要である。実コード（`Mapping.TryDispatchSATriggerEstablished`）を確認すると、複数入力の組み合わせで
トリガー成立を判定し `TriggerContext` を組み立てて `ActionManager` にディスパッチする処理と、実際に
出力を行う `KeyButtonActionController` への委譲は、既に概念上分離されかけている。

「トリガー成立判定」「どのアクションを選ぶか」「マクロをどう分解するか」は、副作用を伴わない**決定**の
プロセスであり、1入力→1出力の通常マッピングと同じ**変換層（2）の仕事**である。一方、決定された内容を
実際にOS/ドライバへ送出する（`Process.Start`、ファイルロード、`SendInput`等）ことは、副作用を伴う
**出力層（3）の仕事**である。この区分に従い、3層モデルを以下のように再整理する。

```
1. 入力監視層              … コントローラーの機種差を吸収し、DS4State に正規化して上位へ渡す

2. 信号変換層（拡張版）      … 入力から「何を出力すべきか」を決定する（実行はしない）
   2-a. 基本マッピング決定    … 1入力→1出力（コントローラー信号／KBM信号）の対応表引き
   2-b. SpecialActionトリガー判定 … 複数入力の組み合わせで成立/解除を判定し、元入力の出力を抑制するか決定
   2-c. アクション選択・パラメータ決定 … 成立したSpecialActionが「マクロ／プロファイル切替／プロセス起動／
                                        KBM出力」のどれかを判定し、実行に必要なパラメータ
                                        （マクロ内容、プロファイル名、起動パス等）を確定する
   2-d. マクロの分解          … トリガーされたマクロを、時系列のKBM出力信号列（何をいつ押す/離すか）に分解する

3. 信号出力層（拡張版）      … 決定された内容を実際に副作用として実行する
   3-a. 仮想コントローラー出力 … 2-aの結果をDS4/Xbox360規格で実出力（outputDevices[ind]、既存ほぼ完成形）
   3-b. KBM出力               … 2-aの結果、および2-dで分解されたマクロの信号列を、実際に時系列で送出
                                  （outputKBMHandler、タイマー駆動の逐次実行を含む）
   3-c. アプリ内アクション実行 … 2-cで決定されたプロファイル切替・プロセス起動を実際に実行
                                  （ファイルロード／Global状態更新／Process.Start呼び出し。権限昇格・
                                  多重起動チェックも含む）
```

**この再整理による重要な副産物**: マクロは「時間差のあるキー/マウス操作の連続」という性質上、
その大部分が **3-c（アプリ内アクション実行）ではなく 3-b（KBM出力）に属する**ことになる。マクロの
「定義をどう分解するか」（2-d）は決定プロセスだが、「分解された操作列を実際に送出する」（3-b）は
KBM出力そのものだからである。旧モデルで一括りにしていた「3-c: SpecialAction実行」は、実際には
性質の異なる2グループ（KBM出力系＝マクロ・キー/マウス出力 と、アプリ内アクション実行系＝プロファイル
切替・プロセス起動）に分かれていたことになる。

この整理により、**変換層（`Mapping.cs`）の責務は「どのアクション種別を、どのパラメータで、どの出力層
コンポーネントに渡すか決定するだけ」**にさらに純化され、実行そのものは全て3-a/3-b/3-cのいずれかに
委譲される形になる。

**結論**: 3層構造は骨格として正しく、現行コードの設計意図とも合致している。修正すべきは
「信号出力層」の範囲の捉え方に加え、**SpecialActionの「判定・選択」を変換層（2）側に位置づける**点であり、
この再整理後の3層モデルを**本プラン全体の設計・移行ロードマップの軸**として採用する。

---

## 4. DI化対象の詳細棚卸し

### 4.1 `Global` 静的メンバの分類

| カテゴリ | 件数目安 | 代表例 | DI化の考え方 |
|---|---|---|---|
| プロファイル設定値の get/set 群 | 約100 | `getLSDeadzone`, `getGyroSensitivity` | `IProfileSettingsService` に集約。件数最大の塊。機械的ラップが効率的。 |
| プロファイル管理（読込/保存/切替） | 約25 | `Load`, `Save`, `ApplyProfile` | `IProfileRepository` に集約。優先度高（`ProfileSwitchAction` 案、および3-c層のプロファイル切替実行と直結）。 |
| アクション（SpecialAction）管理 | 約20 | `SaveAction`, `LoadActions`, `GetAction` | `ISpecialActionRepository`（新設、§5.4 #3）としてプロファイルへの定義の保存/読込を集約。既存`Actions/`のランタイムレジストリ（`IActionRegistry`）とは別の関心事。 |
| デバイス/コントローラ状態管理 | 約15 | `IsFirstConnection`, `MarkConnected` | `IDeviceConnectionTracker`。`ControlService` 分割と合わせて設計。 |
| システム環境判定 | 約10 | `IsWin8OrGreater`, `IsAdministrator` | `IEnvironmentInfoProvider`。読み取りのみで着手しやすい。 |
| OSC/UDPサーバ設定 | 約15 | `isUsingOSCServer`, `getUDPServerPortNum` | `IServerSettingsService`。 |
| 出力ハンドラ初期化・KBM/マウス設定 | 約27 | `InitOutputKBMHandler`, `outputKBMHandler` | `Actions/IOutputAction` との統合が本命。 |
| パス/ファイル位置 | 約8 | `appdatapath`, `exelocation` | `IAppPathsProvider`。Composition Root直前の特別扱いが必要。 |
| 言語/UI/テーマ | 約6 | `UseLang`, `SetCulture` | `IAppearanceSettingsService`。 |
| ユーティリティ計算（純粋関数） | 約10 | `HuetoRGB`, `Clamp` | **DI化不要**。`ColorUtil`/`VersionUtil` 等へ移設のみ。 |
| モニタ/座標変換 | 約4 | `TranslateCoorToAbsDisplay` | `IDisplayInfoProvider`。優先度低。 |
| その他フラグ/設定値 | 約100+ | `firstRun`, `touchpadActive[]` 等 | 上記カテゴリに紐づく設定値が大半。個別精査が必要。 |

**重要な構造的事実**: `ControlService.cs`, `Mapping.cs`, `DS4LightBar.cs` の3ファイルは
`using static DS4Windows.Global;` で無条件展開を行っており、`Global` 分割の影響を最も強く受ける。
`Global` は単一の巨大クラスというより、**実質10種類前後のサービスが1つのクラスに同居している**状態であり、
DI化は「1つの `Global` を削除する」のではなく「10個前後のインターフェース付きサービスに再編する」作業である。

### 4.2 `ControlService` の依存関係

コンストラクタが受け取る外部依存は `ArgumentParser` 1つのみだが、内部実装では以下を直接参照している。

| 参照先 | 出現回数 | 備考 |
|---|---|---|
| `Global.` | 136回 | 設定値の読み書き、イベント購読 |
| `Mapping.` | 44回 | 入力→出力マッピング処理の呼び出し |
| `DS4Devices.` | 17回 | デバイス列挙・接続監視 |

`DS4Devices`（`public class` だが実質全メンバ static、一度もインスタンス化されたことがない）は、
`Global` 同様の「隠れた静的シングルトン」として新たに発見された。また `Mapping` が
`Program.rootHub.DS4Controllers[device]` を参照する箇所が2箇所あり、**`ControlService ⇄ Mapping` の
循環依存が実地確認により確定**した。

### 4.3 ViewModel群（33ファイル）の生成パターン分類

| パターン | 件数 | 特徴 | DI化方針 |
|---|---|---|---|
| A: 引数なしで生成可能 | 11件 | `MainWindowsViewModel`, `SettingsViewModel` 等 | `services.AddTransient<VM>()` の単純登録 |
| B: 共有依存（`ControlService` 等）を受け取る | 5件 | `LogViewModel(App.rootHub)` 等 | `ControlService` がDI登録されれば機械的に置換可能 |
| C: 画面表示時に決まる実行時パラメータを受け取る | **17件（過半数）** | `ProfileSettingsViewModel(device)`, `RecordBoxViewModel(deviceNum, controlSettings, shift, repeatable)` 等 | 単純DI登録は不可能。**ファクトリインターフェース方式が必須** |

パターンCが過半数を占めるため、通常の `services.AddTransient<VM>()` 型の登録では対応できず、
`IXxxViewModelFactory.Create(...)` のようなファクトリをDI登録し、内部でDI解決可能な依存と実行時引数を
合成する設計が必要になる。

### 4.4 `Process.Start` 系呼び出しの要否判定

| 分類 | 目的 | DI化要否 |
|---|---|---|
| ① SpecialAction「プログラム起動」機能 | ユーザー設定の外部プログラム起動（`specActionLaunchProc`） | **要（優先度高）**。既存 `LaunchProcessAction` 案がそのまま適用可能。 |
| ② 昇格・権限関連の子プロセス起動 | デバイス再有効化のための管理者権限再起動 | **要（中優先度）**。`IElevatedProcessLauncher` として抽象化。 |
| ③ UI からの外部ツール起動 | コントロールパネル項目を開く（`joy.cpl` 等） | **不要〜低優先度**。決め打ちの単純起動で条件分岐を持たない。 |
| ④ 外部URL/ブラウザ起動 | ヘルプ・製品ページを開く | **不要〜低優先度**。`IBrowserLauncher` 化はコストが低ければ任意。 |
| ⑤ Updater/Updater2 関連 | アプリ自身の更新・再起動 | **対象外**。別実行ファイル（別プロセス境界）であり本プランのDIコンテナのスコープ外。 |
| ⑥ 多重起動チェック・ヘルパープロセス起動 | `Process.GetProcesses()` 等 | **要（中優先度）**。`IProcessInspector`/`IProcessLauncher` に切り出し。 |

全26箇所以上のうち、DI化（抽象化）すべきは①②⑥の中核ロジック系（合計10箇所程度）のみ。
③④⑤は「テストで検証すべきロジックが伴わない起動」「別プロセス境界にある起動」として意図的に対象外とする。

**§3.3の3層モデルとの対応**: ①（SpecialAction起動）は「どのプログラムを起動するか」の決定（2-c層）と
「実際に`Process.Start`を呼ぶ」実行（3-c層）に分かれる。②⑥は決定を伴わない純粋な実行ロジックであり、
最初から3-c相当（`ControlService`/`Global`側の中核ロジック）として扱う。

---

## 5. 目標アーキテクチャ

### 5.1 層 × DIインターフェース対応表（改訂版: SpecialActionの判定/実行分離を反映）

| 層 | 対応する既存コード | 対応するDIインターフェース |
|---|---|---|
| 1. 入力監視層 | `DS4Device`, `DS4Devices` | `IDs4DeviceRegistry` |
| 2-a. 基本マッピング決定 | `Mapping.MapCustom` 内の1:1対応部分 | `IDeviceStateAccessor`（循環依存解消用）／`Mapping` 自体は当面static維持 |
| 2-b. SpecialActionトリガー判定 | `TryDispatchSATriggerEstablished/Released`, `DispatchInputEdge` | `IActionRegistry`／`IManagedActionManager`（既存）。**副作用を持たない純粋な判定ロジックとして再定義** |
| 2-c. アクション選択・パラメータ決定 | `ActionManager.DispatchTriggerEstablished` 内のディスパッチ処理 | 同上。SpecialActionの`typeID`に応じてどの`IOutputAction`実装を呼ぶか決定する部分 |
| 2-d. マクロの分解 | `PlayMacro`前段のマクロ定義パース部分 | 新設 `IMacroDecomposer`（マクロ定義→KBM出力イベント列への純粋変換。モックなしでテスト可能） |
| 3-a. 仮想コントローラー出力 | `outputDevices[ind]` | （既存抽象化のまま。変更不要） |
| 3-b. KBM出力 | `outputKBMHandler.PerformKeyPress*`、`PlayMacroTask`のタイマーループ | `IVirtualKBM`（マクロの逐次送出もここに統合） |
| 3-c. アプリ内アクション実行 | `Global.ApplyProfile`, `Process.Start`系（`specActionLaunchProc`） | `IProfileRepository`（`ApplyProfile`相当、プロファイル切替の実行。既存§5.4 #2をそのまま使用）／`IProcessLauncher`系（プロセス起動の実行） |
| （層横断） | `Global`（ScpUtil.cs） | `IProfileSettingsService` 等10種 |

**旧モデルからの主な変更点**: 従来「3-c: SpecialAction実行」として出力層に一括りにしていたマクロ・
プロファイル切替・プロセス起動のうち、**「成立判定」「アクション選択」「マクロ分解」は変換層（2-b〜2-d）**
に位置づけ直した。特にマクロは実行段階（時系列でのKBM送出）が大部分を占めるため、**3-cではなく3-bに
再分類**される。3-cとして出力層に残るのは、プロファイル切替・プロセス起動という「アプリ内アクション実行」
のみである。

**層構造がフェーズ分割の軸になる理由**: 変換層が持つ実行系の依存を、影響範囲の小さいものから順に
剥がしていくのが最もリスクの低い順序である。

1. **SpecialAction関連（2-b/2-c/2-d の決定ロジックと、3-b/3-c の実行ロジック）の分離を最優先**: 既存
   `DI-Migration-Plan.md` がまさにこのスコープであり、既に設計のたたき台がある領域。着手コストが最も低い。
   決定ロジック（`IActionRegistry`/`IManagedActionManager`、`IMacroDecomposer`）は副作用がなく単体テストが
   容易なため、まずここを固めてから実行ロジック（`IVirtualKBM`, `IProfileRepository`, `IProcessLauncher`）
   の切り出しに進む、という2段階アプローチが取れる。
2. **3-b（KBM出力、マクロの実送出を含む）の分離**: `IVirtualKBM` 抽象化。
3. **1（入力監視層）と2-a（基本マッピング決定）自体の整理**: `IDs4DeviceRegistry` 化、`IDeviceStateAccessor`
   による循環依存解消。SpecialAction関連の分離完了後に着手する方が安全。
4. **3-a（仮想コントローラー出力）**: 既に比較的完成された抽象化がされているため、優先度は最も低い。

### 5.2 Composition Root 全体方針

1. **`AppHost.cs`（`Host.CreateDefaultBuilder()`）を正式なComposition Rootとして採用**し、現状
   `App.xaml.cs` にインラインで書かれている簡易 `ServiceCollection` 構築を置き換える。
2. 起動順序をフェーズ0〜3に分割する（※ 移行ロードマップのフェーズ0〜5とは別軸の、
   Composition Root内部の起動手順）：
   - **フェーズ0（Pre-Host）**: パス解決・ログ初期化。DIを介さない最小限のブートストラップとして残す
     （Host構築の前提を作るためのDIが必要、という自己矛盾を避けるため）。
   - **フェーズ1（Host構築）**: `AppHost.CreateHost(configuration)` で `IHost` を構築。
   - **フェーズ2（アプリ初期化）**: DI解決したサービス経由でプロファイル等を読み込む。
   - **フェーズ3（UI起動）**: `MainWindow` をDIコンテナから解決し表示する。
3. **`ControlService ⇄ Mapping` の循環依存を解消**するため、`Mapping` が必要とする `ControlService` の
   機能を `IDeviceStateAccessor` として最小限に切り出し、`ControlService` がこれを実装する
   （依存性逆転の原則）。
4. **`Global` は10種類程度のインターフェース付きサービスへ段階的に再編**し、移行中は
   `Global` の各静的メンバを「新サービスへの薄いデリゲート」に置き換える（**Strangler Fig パターン**）
   ことで、既存75ファイルの呼び出し元を一度に触らずに済ませる。
5. 既存 `Actions` サブシステムはそのまま活用し、`ActionManager` static facade の
   「静的ファサード → DI実装へ委譲」パターンを他の静的ファサードにも横展開する。

**推奨パッケージ**: `Microsoft.Extensions.Hosting`（未参照・追加要）、
`Microsoft.Extensions.DependencyInjection`（導入済み）、`Microsoft.Extensions.Logging`
（NLog連携検討）、`Microsoft.Extensions.Configuration`、`Scrutor`（規約ベース自動登録、導入推奨）。

### 5.3 サービスライフタイム方針

| 層 | サービス分類 | 代表インターフェース | 推奨ライフタイム |
|---|---|---|---|
| 横断 | プロファイル設定値 | `IProfileSettingsService` | Singleton |
| 横断 | プロファイル管理 | `IProfileRepository` | Singleton |
| 2-b/2-c | SpecialActionトリガー判定・アクション選択（既存Actions） | `IActionRegistry`／`IManagedActionManager`（既存拡張） | Singleton（内部状態はデバイス毎に管理、副作用は持たない） |
| 2-d | マクロ分解（新設） | `IMacroDecomposer` | Singleton（純粋変換、状態を持たない） |
| 1 | デバイス/コントローラ状態管理 | `IDeviceConnectionTracker` | Singleton |
| 横断 | システム環境判定 | `IEnvironmentInfoProvider` | Singleton |
| 横断 | OSC/UDPサーバ設定 | `IServerSettingsService` | Singleton |
| 3-b | 出力ハンドラ（KBM/マウス、マクロの逐次送出含む） | `IVirtualKBM` | Singleton（将来デバイス毎Scoped検討） |
| 横断 | パス/ファイル位置 | `IAppPathsProvider` | Singleton（Host構築前に値確定） |
| 横断 | 言語/UI/テーマ | `IAppearanceSettingsService` | Singleton |
| 横断 | モニタ/座標変換 | `IDisplayInfoProvider` | Singleton |
| 3-c | プロファイル切替・プロセス起動の実行（中核ロジック） | `IProfileRepository`／`IProcessLauncher`/`IElevatedProcessLauncher`/`IProcessInspector` | Singleton（機能別分割） |
| 2-a | `ControlService` | 具象型のまま | Singleton |
| 1 | `DS4Devices` | `IDs4DeviceRegistry`（新設） | Singleton |
| - | ViewModel（パターンA・B） | 各具象型 | Transient |
| - | ViewModel（パターンC）用ファクトリ | `IXxxViewModelFactory` | Singleton（ファクトリ自体） |

### 5.4 `Global` 分割後のインターフェース一覧（確定版）

| # | インターフェース | 元 `Global` メンバ数目安 | 優先度 |
|---|---|---|---|
| 1 | `IProfileSettingsService` | 約100 | 高（件数最大） |
| 2 | `IProfileRepository` | 約25 | 高（既存Migration-Planと直結） |
| 3 | `ISpecialActionRepository`（新設） | 約20 | 高（起動時に読み込んだ定義を`IActionRegistry`に登録する橋渡し役） |
| 4 | `IDeviceConnectionTracker` | 約15 | 中（`ControlService`分割と連動、1層） |
| 5 | `IEnvironmentInfoProvider` | 約10 | 低〜中 |
| 6 | `IServerSettingsService` | 約15 | 中 |
| 7 | `IOutputHandlerSettingsService` | 約27 | 高（`Actions/IOutputAction`と重複整理必須、3-b層） |
| 8 | `IAppPathsProvider` | 約8 | 特別枠（Host構築前に必要） |
| 9 | `IAppearanceSettingsService` | 約6 | 低 |
| 10 | `IDisplayInfoProvider` | 約4 | 低 |
| - | 純粋関数群（DI化不要） | 約10 | `ColorUtil`, `VersionUtil` 等へ移設のみ |

**`ISpecialActionRepository` と既存 `IActionRegistry` の違い（本改訂で明確化）**: 両者は名前が似ているが
別の関心事を扱う。`ISpecialActionRepository`（新設）は `Global.SaveAction`/`LoadActions`/`GetAction` 等が
担う「プロファイルへのSpecialAction**定義**の保存/読込」という**設定データアクセス層**の機能である。
一方、既存 `DS4Windows/Actions/IActionRegistry.cs` は `Register`/`Unregister`/`GetBindingsForDevice` のみを
持つ**ランタイムのバインディング登録レジストリ**であり、§5.1 の2-b/2-c層（トリガー判定・アクション選択）
が参照する実行時の状態である。アプリ起動時に `ISpecialActionRepository` が読み込んだ定義を
`IActionRegistry.Register(...)` で登録する、という橋渡し関係になる。
また、SpecialAction成立時の「プロファイル切替」「プロセス起動」の**実行**は、§5.1で整理した通り
既存の `IProfileRepository`（§5.4 #2）と `IProcessLauncher` 系がそのまま担い、新規インターフェースは
不要である。

`Global` クラス自体は完全削除せず、**後方互換シム**として当面残す：

```csharp
public static class Global
{
    public static double getLSDeadzone(int index) =>
        DS4Windows.DI.ServiceProviderHolder.GetRequiredService<IProfileSettingsService>().GetLSDeadzone(index);
}
```

これにより75ファイルある既存の `Global.xxx` 呼び出し元を一度に全て書き換える必要がなくなる。

### 5.5 循環依存解消方針

```csharp
public interface IDeviceStateAccessor
{
    DS4Device GetController(int deviceIndex);
    // 必要最小限のプロパティ/メソッドのみ
}
```

`ControlService` がこれを実装し、`Mapping` は `Program.rootHub` への直接参照を除去して
`IDeviceStateAccessor` に依存する形に変更する。`DS4Devices` は `IDs4DeviceRegistry` としてインターフェース化。

**`Mapping` 自体の完全instance化は本プランでは見送る。** 8,827行・単一責任を大きく逸脱したファイルの
完全インスタンス化は影響範囲が非常に大きいため、まずは直接副作用呼び出し（3-b/3-c）の切り出しを優先し、
完全DI化の要否は切り出し完了後に再評価するチェックポイントを設ける。

### 5.6 ViewModel群のDI戦略

- **パターンA・B（16件）**: 標準的なコンストラクタインジェクション。`services.AddTransient<VM>()`。
- **パターンC（17件）**: ファクトリインターフェース方式を推奨。

```csharp
public interface IProfileSettingsViewModelFactory
{
    ProfileSettingsViewModel Create(DS4Device device);
}

public class ProfileSettingsViewModelFactory : IProfileSettingsViewModelFactory
{
    private readonly IProfileSettingsService _profileSettings;
    public ProfileSettingsViewModelFactory(IProfileSettingsService profileSettings)
        => _profileSettings = profileSettings;

    public ProfileSettingsViewModel Create(DS4Device device)
        => new ProfileSettingsViewModel(device, _profileSettings);
}
```

View側は `new ProfileSettingsViewModel(device)` の代わりに `factory.Create(device)` を呼ぶだけで済み、
Viewの改修は最小限に抑えられる。17件全てに個別インターフェースを作るとボイラープレートが多くなるため、
`Scrutor` の規約ベース登録（`IXxxFactory` → `XxxFactory`）の採用を推奨する。

`SpecialActionViewModel(5)`/`(8)`/`(9)` のような固定値パラメータは、`Create(int purposeId)` ファクトリで
対応可能だが、根本的には用途ごとにクラスを分割する方が将来の可読性・テスト性は高い。ただし機能分割は
リスクが高いリファクタリングのため本プランのスコープ外とし、現状の固定値をそのまま渡す現実的な移行に留める。

---

## 6. 段階移行ロードマップ

### 6.1 全体構成

| フェーズ | 名称 | 対応する層 | 主内容 | 既存資産の活用 |
|---|---|---|---|---|
| フェーズ0 | 基盤整備 | - | DIパッケージ導入、`AppHost.cs` 正式運用化、テスト基盤整備 | `DI-Implementation-Guide.md` |
| フェーズ1 | SpecialAction判定・実行の分離 | 2-b/2-c/2-d, 3-c | `Mapping.cs` の副作用を `ActionManager` 経由に統一（判定と実行を分離） | `DI-Migration-Plan.md` をほぼそのまま採用 |
| フェーズ2 | KBM出力の抽象化 | 3-b | `outputKBMHandler` を `IVirtualKBM` としてDI登録（マクロの逐次送出も統合） | 新規設計 |
| フェーズ3 | 入力監視層・信号変換層の整理 | 1, 2 | `IDs4DeviceRegistry` 化、`IDeviceStateAccessor` による循環依存解消 | 新規設計 |
| フェーズ4 | `Global` 分割とViewModel DI化 | 横断/UI層 | `Global` を10種のサービスへ分割、33件のViewModelをDI/ファクトリへ移行 | 新規設計 |
| フェーズ5 | 仕上げ・整流化 | 3-a/全体 | 仮想コントローラー出力の最終確認、`Global`シム削除判断 | 新規設計 |

### 6.2 フェーズ0: 基盤整備（1〜2日）

1. `.csproj` に `Microsoft.Extensions.Hosting` を追加。
2. `Microsoft.Extensions.Logging` + `NLog.Extensions.Logging` の導入検討。
3. `AppHost.cs` を実際に `App.xaml.cs` から呼び出す（動作確認のみのPRに留め、`ServiceRegistration.ConfigureServices`
   の中身は後続フェーズで順次移設）。
4. `DS4WindowsTests` にモック基盤（`IManagedActionManager` の軽量モック等）を整備。

**完了判定基準**: `AppHost.CreateHost()` がエラーなく呼ばれ既存の起動結果と一致すること（回帰なし）。
**PR粒度**: PR-0.1（パッケージ追加）／PR-0.2（AppHost配線）／PR-0.3（テスト基盤）。

### 6.3 フェーズ1: SpecialAction判定・実行の分離（2-b/2-c/2-d, 3-c層、2〜4週間）

**既存 `docs/DI-Migration-Plan.md` のステップA〜Eをそのまま採用する。**

| ステップ | 内容 | 見積もり |
|---|---|---|
| A. インベントリ&テスト基盤 | `IManagedActionManager` モック作成 | 1〜2日 |
| B. 低リスクな置換 | `TryDispatchSATriggerEstablished/Released` を厳密化（フォールバック残し） | 数日 |
| C. 機能別移行 | Key送出→`KeyOutputAction`、Mouse→`MouseOutputAction`、Macro→`MacroAction`、Profile切替→`ProfileSwitchAction`、プロセス起動→`LaunchProcessAction` | Key/Mouse: 2〜4日、Macro: 3〜5日、Profile/Launch: 1〜3日 |
| D. フォールバック削除 | 機能ごとに1PRで直接send呼び出しを削除 | 機能数分 |
| E. ドキュメント更新・ロールアウト | インベントリ更新、実機回帰テスト | - |

`LaunchProcessAction`（C5）は `Process.Start` 分類①に対応。分類②・⑥はフェーズ3のスコープ。

**§3.3の再整理を踏まえた補足**: 本フェーズで移行する「トリガー判定・アクション選択」（`ActionManager`の
ディスパッチ処理、2-b/2-c）は副作用を持たない決定ロジックであり、`IVirtualKBM`や`IProcessLauncher`のモック
なしで軽量にテストできる。一方「マクロ分解」（2-d、`IMacroDecomposer`として新設）と「実行」（3-b/3-c）は
副作用を伴うため別個にテストする。ステップCの「Macro: 3〜5日」の見積もりには、この決定（分解）と実行
（逐次送出）を分離する作業を含む。

**完了判定基準**: `Mapping.cs` 内の直接呼び出し件数が0になり `ActionManager` 経由に統一されること。
**PR粒度**: 1PR = 1機能（各機能のフォールバック削除も別PR）。

### 6.4 フェーズ2: KBM出力の抽象化（3-b層、3〜5日）

1. `IVirtualKBM` インターフェースを定義（フェーズ1で洗い出し済みのAPIをそのまま踏襲、振る舞い変更なし）。
2. `services.AddSingleton<IVirtualKBM, VirtualKBMHandlerAdapter>();` でDI登録。
3. `Global.outputKBMHandler` を `IVirtualKBM` への薄いデリゲートに置き換え（Strangler Fig）。

フェーズ1完了済みなら `Mapping.cs` 側の変更は不要（`Actions/` 配下に閉じる）。なお§3.3の再整理により、
マクロの逐次送出（`PlayMacroTask`のタイマーループ）も3-b（KBM出力）としてこのフェーズで`IVirtualKBM`
経由に統合する（`IMacroDecomposer`が生成したイベント列を`IVirtualKBM`が時系列で送出する形）。

**完了判定基準**: `Actions/` 配下が `IVirtualKBM` のみを参照し、モックによる単体テストが可能なこと。
**リスク**: `outputKBMHandler` は物理リソース（ドライバハンドル）を保持するため、初期化タイミング依存の
参照が他にないか全数洗い出しが必要。

### 6.5 フェーズ3: 入力監視層・信号変換層の整理（1・2層、1〜2週間）

1. **`DS4Devices` の `IDs4DeviceRegistry` 化**: デバイス列挙・接続監視をインターフェース化、静的イベントを
   インスタンスイベントに変換。
2. **`IDeviceStateAccessor` による循環依存解消**: `Mapping` の `Program.rootHub` 参照2箇所を置換。
   `Mapping` は「メソッド引数として `IDeviceStateAccessor` を受け取る」形に変更（完全instance化は見送り）。
3. **`Process.Start` 分類②・⑥の抽象化**: 権限昇格を `IElevatedProcessLauncher`、多重起動チェックを
   `IProcessInspector` に切り出し。

**完了判定基準**: `Mapping.cs` 内の `Program.rootHub` 参照が0件、`ControlService.cs` から `DS4Devices.`
直接参照が除去されること。
**リスク**: HID通信を含む低レイヤ処理のため、実機での接続/切断シナリオの手動確認を必須とする。

### 6.6 フェーズ4: `Global` 分割とViewModel DI化（横断/UI層、6〜8週間）

**`Global` 分割の推奨着手順序**（1PR = 1インターフェース基本）:

| 順序 | インターフェース | 理由 |
|---|---|---|
| 1 | `IProfileRepository` | フェーズ1の`ProfileSwitchAction`と直結、フェーズ1完了後すぐ着手可能 |
| 2 | `IProfileSettingsService` | 件数最大（約100）だが機械的ラップが中心 |
| 3 | `ISpecialActionRepository` | 既存Actions基盤（`IActionRegistry`）との橋渡し整理、フェーズ1完了が前提 |
| 4 | `IOutputHandlerSettingsService` | フェーズ2完了後に着手（重複整理のため） |
| 5 | `IDeviceConnectionTracker` | フェーズ3完了後に着手（`ControlService`分割と連動） |
| 6〜10 | 残り5インターフェース | 相互依存が薄く任意の順序で並行着手可能 |

各インターフェースの移行手順: ①インターフェース定義+実装クラス作成（ロジック変更なし） →
②DI登録 → ③`Global`の該当メンバをシム化 → ④呼び出し元75ファイルはこの時点では変更しない
（当該ファイルを別の理由で触る際に「ついでに」置き換える長期戦略）。

**ViewModel DI化**: パターンA（11件、1PR=3〜5件でまとめ可）→ パターンB（5件、フェーズ3完了が前提）
→ パターンC（17件、1PR=1ViewModel、`Scrutor`導入で後半高速化）。

**リスク**: `Global`シム化時のスレッド競合（暗黙のスレッドセーフが崩れる可能性）→ 元メンバの呼び出し元
スレッドを`grep`で洗い出しロック追加を検討。

### 6.7 フェーズ5: 仕上げ・整流化（3-a層+全体、1〜2週間）

1. 3-a（仮想コントローラー出力）の最終確認（大規模変更は想定せず）。
2. `Global`シムの削除判断（全75ファイルの移行が完了していれば削除、未完了ならシム存置継続）。
3. `AppHost.cs`への完全移行（`App.xaml.cs`のインライン`ServiceCollection`構築コードを削除）。
4. `Process.Start`分類③④⑤の再確認（原則は「対象外のまま」）。

**完了判定基準**: `Application_Startup`が§5.2のComposition Root設計と一致し、`ServiceRegistration.ConfigureServices`
に全サービスが登録され、`new XxxViewModel(`のDIファクトリ非経由箇所が0件であること。

### 6.8 全体スケジュール見積もり

| フェーズ | 見積もり |
|---|---|
| フェーズ0（基盤整備） | 1〜2日 |
| フェーズ1（SpecialAction判定・実行分離） | 2〜4週間 |
| フェーズ2（3-b: KBM出力抽象化） | 3〜5日 |
| フェーズ3（1・2層: 入力/変換層整理） | 1〜2週間 |
| フェーズ4（Global分割 + ViewModel DI化） | 6〜8週間 |
| フェーズ5（仕上げ・整流化） | 1〜2週間 |
| **合計** | **概算12〜18週間**（単独作業者想定。チーム体制・レビュー体制により変動） |

### 6.9 共通のPR運用方針

1. **1 PR = 1機能 or 1インターフェース**を基本単位とする。呼び出し元の一括置換は別PRに分離。
2. **フォールバックを残した移行**: 新経路の動作確認後、別PRでフォールバックを削除。
3. **各PRにユニットテストを含める**: 新設インターフェースには最低1つのモックベーステスト。
4. **既存インベントリドキュメント（`Direct-Callsites-Inventory.md`等）を都度更新**し、進捗を文書と同期。

### 6.10 進捗の定量指標（ダッシュボード案）

| 指標 | ベースライン | 目標 |
|---|---|---|
| `Global.xxx` を参照するファイル数 | 75ファイル | フェーズ4完了時点でシム経由のみ・新規増加なし |
| `Program.rootHub`/`App.rootHub` を参照するファイル数 | 15ファイル | フェーズ3〜5で段階的に減少 |
| `Mapping.cs`内の直接副作用呼び出し件数 | 複数箇所（分類済み） | フェーズ1〜3完了時点で0 |
| DIコンテナ登録サービス数 | 5 | フェーズ5完了時点で全10+αサービス登録済み |
| `new XxxViewModel(` のうちDI非経由の件数 | 33件 | フェーズ4完了時点で0 |
| `AppHost.cs` が実際に呼ばれているか | 呼ばれていない（デッドコード） | フェーズ0で呼ばれる状態に、フェーズ5で完全移行 |

---

## 7. 既存 `docs/` たたき台との統合方針

| 既存ドキュメント | 本プランでの位置づけ |
|---|---|
| `docs/DI-Migration-Plan.md` | **フェーズ1（SpecialActionの判定・実行の分離）**としてそのまま採用。 |
| `docs/DI-Implementation-Guide.md` | 推奨パッケージ・導入手順の土台として採用。`AppHost.cs`を「未使用」から「正式運用」に格上げする点が追加事項。 |
| `docs/DI/DI-ObjectGraph.md` | Composition Root設計の土台として採用。本書§5.2がその具体化・アプリ全体版。 |
| `docs/Direct-Callsites-Inventory.md` | §4.4の分類のベースとして活用済み。今後も進捗管理に継続利用。 |

---

## 8. リスク総括

| リスク | 該当フェーズ | 回避策 |
|---|---|---|
| マクロの分解ロジックが元の挙動（連打・リピート等）を正しく再現できるか | フェーズ1 | `IMacroDecomposer`の出力（KBM出力イベント列）を、既存`PlayMacro`の実行結果と比較する比較テスト |
| マクロ実行の内部呼び出し先を`outputKBMHandler`直接呼び出しから`IVirtualKBM`に差し替える際のタイミング再現性 | フェーズ2 | 差し替え前後でイベント列の送出タイミングをログ比較、実機での連打・ホールド動作を回帰テスト項目化 |
| 外部プロセス起動の権限/コンテキスト問題 | フェーズ1 | `LaunchProcessAction`に呼び出し元情報を明示 |
| `outputKBMHandler`の初期化タイミング依存 | フェーズ2 | `IVirtualKBM`化前に全参照箇所を洗い出し |
| HID通信のタイミング依存不具合 | フェーズ3 | 実機接続/切断シナリオを回帰テスト項目化、手動確認必須 |
| `Mapping`のstatic中心設計との整合 | フェーズ3 | 引数渡し方式を採用し完全instance化を待たない設計に |
| 設定値アクセスのスレッド競合顕在化 | フェーズ4 | 元`Global`メンバの呼び出し元スレッドを洗い出しロック追加 |
| ViewModelパターンCのファクトリ設計コスト | フェーズ4 | ステージ2で洗い出した17件リストをToDoとして使用 |
| UI/Updater系の起動処理が仕様上置換不可な場合 | 全体 | 分類③④⑤は対象外とし、Integration noteとして別途記録 |

---

## 9. 付録

### 9.1 用語集

| 用語 | 説明 |
|---|---|
| Composition Root | アプリ起動時にDIコンテナを構築し、全依存関係を解決する唯一の場所 |
| Strangler Fig パターン | 既存コードを段階的に新実装へ置き換える手法。新旧を共存させながら徐々に移行する |
| God Object | 過剰に多くの責務を持つ巨大なクラス（本プランでは`Global`が該当） |
| 循環依存 | AがBに依存し、BもAに依存する関係（テスト・保守を困難にする） |

### 9.2 参照ドキュメント一覧（本プラン策定の調査過程）

| # | ドキュメント名 | 内容 |
|---|---|---|
| 1 | `01-現状分析.md` | アプリ全体の構造・現状の問題点の把握 |
| 2 | `02-DI化対象の詳細棚卸し.md` | `Global`分類、`ControlService`依存関係、ViewModel生成パターン、`Process.Start`要否判定 |
| 3 | `03-目標アーキテクチャ設計.md` | Composition Root、サービスライフタイム、3層モデル対応 |
| 4 | `04-理想の3層構造と現状のズレ.md` | 3層モデルの理想形と実コードの対応検証（補足ステージ） |
| 5 | `05-段階移行ロードマップ.md` | フェーズ分割、PR粒度、リスクと回避策、スケジュール見積もり |
| - | `docs/DI-Migration-Plan.md`（既存） | フェーズ1のベースとして採用 |
| - | `docs/DI-Implementation-Guide.md`（既存） | 推奨パッケージ・導入手順のベースとして採用 |
| - | `docs/DI/DI-ObjectGraph.md`（既存） | Composition Root設計のベースとして採用 |
| - | `docs/Direct-Callsites-Inventory.md`（既存） | `Process.Start`等の分類のベースとして活用 |

### 9.3 次のアクション

1. 本プラン書を `docs/DI-App-Wide-Migration-Plan.md` としてリポジトリにコミットする。
2. フェーズ0（基盤整備）のPR-0.1（NuGetパッケージ追加）から着手する。
3. `DS4WindowsTests` にモック基盤を整備し、フェーズ1のステップAに接続する。
4. 各フェーズ完了時に、§6.10の定量指標を計測し、本書または後継ドキュメントに記録する。

---

*本書はステージ1〜4の調査（`01-現状分析.md` 〜 `05-段階移行ロードマップ.md`）および補足ステージ
（`04-理想の3層構造と現状のズレ.md`）の内容を統合したものである。各ステージの詳細な調査過程・
根拠データ（コード行数・参照回数等の実測値）は、それぞれの元ドキュメントを参照のこと。*
