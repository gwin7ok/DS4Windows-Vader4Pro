# DS4Windows-Vader4Pro アプリ全体 DI化 最終プラン書

作成日: 2026-08-25
改訂日: 2026-09-09（Phase6「残存Global実利用箇所の解体」、Phase7「Mapping.cs完全instance化（延期）」を追加。
DI化対象外項目をカテゴリA〜Fとして確定）
対象ブランチ: `インターフェース化`（以降の実装は `For-DI-migration-work` ブランチで実施）
対象リポジトリ: https://github.com/gwin7ok/DS4Windows-Vader4Pro
保存想定パス: `docs/DI-App-Wide-Migration-Plan.md`

本書は `01-現状分析.md` 〜 `05-段階移行ロードマップ.md`（調査ステージ1〜4）および
`04-理想の3層構造と現状のズレ.md`（補足ステージ）で行った調査・設計を統合し、
単独で読んで完結する**最終プラン書**として一本化したものである。

**2026-09-09改訂の要旨**: Phase1〜5（実質フェーズ0〜5の実装）が完了・進行し、`Global`静的クラスの
主要責務は10種以上のDIサービスへ再編されたが、実地調査（`git clone`によるリポジトリ全文grep）の結果、
呼び出し元側に193件の`Global.*`直接参照が残存していることが判明した。これを解消し「完全にDI化された
4層構造のアプリ」を完成させるため、**Phase6**（残存Global実利用箇所の解体、仮想コントローラー部分を除く）
と**Phase7**（`Mapping.cs`の完全instance化、Phase6完了後に独立実施）を新設する。あわせて、原理的に
DI化すべきでない、またはDI化する実益がない項目を**カテゴリA〜F**として明確に確定した（§4.5参照）。

---

## 0. エグゼクティブサマリー

DS4Windows-Vader4Pro は、コントローラー入力をポーリングで監視し（入力監視層）、設定に基づいて
出力内容を決定し（信号変換層）、仮想コントローラーやキーボード/マウスとして実際に出力する（信号出力層）、
という3層の実行時データフローを持つ。これに加えて、ユーザーが設定・プロファイル・デバイス状態を操作する
WPF の UI 層が存在し、設定サービスを通じて3層の動作を制御する。

現状の最大の課題は、**約469個の `public static` メンバを持つ `Global` クラス（God Object）**、
**`Program.rootHub` という静的シングルトン**、**33個のViewModelすべてが `new` で直接生成される**こと、
そして**信号変換層（`Mapping.cs`、8,827行）が「決定」だけでなく出力の「実行」まで直接担ってしまっている**
ことにある。これらにより、単体テストによる検証がほぼ不可能な状態になっている。

既にリポジトリの `docs/` には `Mapping.cs` の副作用切り出しに限定した移行計画（`DI-Migration-Plan.md` 等3点）
が存在するが、これは**アプリ全体のDI化ではなく、Actionsサブシステムに限定したスコープ**である。
本プランは、この既存3点を「フェーズ1」として取り込みつつ、その上位に**アプリ全体のDI化ロードマップ**を
新設するものである。

移行は、実行時3層＋UI層の4層モデル（後述）における**「変換層が抱える出力実行の権限を、影響範囲が小さいものから
順に剥がしていく」**という考え方を軸に進める。当初はフェーズ0〜5・**概算13〜19週間**で計画したが、
Phase5完了間際の実地調査により**Phase6・Phase7**を追加した（2026-09-09改訂）。

| フェーズ | 内容 | 当初見積もり | 実績 |
|---|---|---|---|
| 0. 基盤整備 | DIパッケージ導入、`AppHost.cs` 正式運用化 | 1〜2日 | 1日 |
| 1. SpecialAction判定・実行の分離 | 既存 `DI-Migration-Plan.md` を採用 | 2〜4週間 | 2日 |
| 2. KBM出力の抽象化 | `IVirtualKBM`（ステップA:マクロ14箇所／ステップB:通常マッピング48箇所） | 1〜2週間 | 3〜5日 |
| 3. 入力監視層・信号変換層の整理 | `IDs4DeviceRegistry`, 循環依存解消 | 1〜2週間 | 3日 |
| 4. `Global` 分割 + ViewModel DI化 | 10サービス + 33ViewModel | 6〜8週間 | 3日 |
| 5. 仕上げ・整流化（DIサービス内部Legacy経路監査） | シム削除判断、`AppHost` 完全移行 | 1〜2週間（実施の結果15ステップへ再編） | 進行中（Step1-13完了、Step14-15残） |
| **6.（新設）残存Global実利用箇所の解体** | 呼出元193件をDIサービス経由へ置換。仮想コントローラー部分は対象外 | 実績ベース見積り: 実働4〜8日 | 未着手 |
| **7.（新設・延期）`Mapping.cs`完全instance化** | 8,500行超のstatic中心ファイルを段階的にinstance化 | Phase6完了後に個別見積り | 未着手 |

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

**（2026-09-09注記）本問題はPhase4-Step6にて解消済み。`AppHost`／`ServiceRegistration`へのComposition Root
一本化が完了し、23以上のサービスが正式登録されている。**

### 2.7 既存ドキュメント資産のスコープ
リポジトリの `docs/` には `DI-Migration-Plan.md`, `DI-Implementation-Guide.md`, `DI/DI-ObjectGraph.md`,
`Direct-Callsites-Inventory.md` という4点のたたき台が存在するが、いずれも
「`Mapping.cs` の副作用を `ActionManager` 経由にする」という**Actionsサブシステム限定の移行計画**であり、
`Global`, `ControlService`, 33個のViewModel、Updater/UI層を含む**アプリ全体のDI化はスコープ外**として
明示的に扱われている（例:「Updater / UI の `Process.Start` 系は仕様上直接呼ぶケースが合理的」との記述）。
→ 本プランはこの4点を「フェーズ1」として取り込みつつ、上位にアプリ全体のロードマップを新設する。

---

## 3. 4層モデル（実行時3層 + UI層）

### 3.1 提示された実行時3層構造とUI層

| 層 | 役割 |
|---|---|
| 1. 入力監視層 | コントローラーからの入力信号をポーリングで監視。機種差はここで吸収。感知した入力信号を信号変換層に渡す。 |
| 2. 信号変換層 | 入力信号から最終的な出力信号を決定（1入力→1出力）。SpecialAction成立判定・成立時の元入力の出力防止。マクロの開始判定と各出力信号への分解。最終的に各出力信号を信号出力層へ渡す。 |
| 3. 信号出力層 | 受け取った出力信号を、設定された仮想コントローラー（DualShock4 / Xbox）の規格に従って実際に出力。仮想コントローラーの機種差はここで吸収。 |
| 4. UI層 | ユーザーがプロファイル、ボタン・ジャイロ・タッチパッド、SpecialAction、マクロ、出力先、アプリ設定を表示・編集する。設定・プロファイル・デバイス状態をサービス経由で管理し、実行時3層へ反映する。 |

UI層は入力信号が常時通過する実行時データフローの第4段ではなく、実行時3層を設定・監視する制御面である。UI の View は WPF 画面、ViewModel は画面状態、サービスは設定・プロファイル・デバイス状態の管理を担当する。

### 3.2 実コード追跡による検証結果

| 提案の層 | 対応する既存コード | 一致度 | 備考 |
|---|---|---|---|
| 1. 入力監視層 | `DS4Device`（`DS4Library`） | ◎ ほぼ一致 | デバイスごとに専用スレッド `ds4Input` でHIDレポートをポーリング。機種差の吸収は既に達成済み |
| 2. 信号変換層 | `Mapping.cs` + `ControlService.On_Report` | △ 一致するが責務過多 | 「決定」だけでなく `outputKBMHandler` 呼び出し・`PlayMacro`・`Global.ApplyProfile`・`Process.Start` という「出力実行」まで直接担っている |
| 3. 信号出力層（仮想コントローラー） | `outputDevices[ind]` | ◎ 概念は一致（範囲が狭い） | `Xbox360OutDevice`/`DS4OutDeviceBasic`等。仮想コントローラー出力のみを指しており、KBM出力等が含まれていない |
| 4. UI層 | `DS4Forms/*.xaml`、`DS4Forms/*.xaml.cs`、`DS4Forms/ViewModels/*` | △ 概念は存在するがDI境界が未整理 | 設定・プロファイル操作の画面は存在するが、ViewModel の直接生成、`Global`／`Program.rootHub` 参照が残っている。Phase4 で DI 化する |

### 3.3 4層モデル（実行時3層 + UI層、SpecialActionの「判定」と「実行」を分離）

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
**出力層（3）の仕事**である。この区分に従い、実行時3層とUI層を含む4層モデルを以下のように再整理する。

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

4. UI層（制御面）           … ユーザーが設定・プロファイル・状態を操作し、サービス経由で実行時3層へ設定を反映
   4-a. View                … WPF の画面・UserControl
   4-b. ViewModel           … 画面状態、入力値検証、画面イベントの調整
   4-c. 設定／状態サービス   … プロファイル、入力・出力設定、デバイス状態、環境情報をDI管理
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

**結論**: 実行時の3層構造は骨格として正しく、現行コードの設計意図とも合致している。これとは別に、設定・監視を担う
UI層を第4層（制御面）として明示する。修正すべきは
「信号出力層」の範囲の捉え方に加え、**SpecialActionの「判定・選択」を変換層（2）側に位置づける**点であり、
この再整理後の4層モデル（実行時3層＋UI層）を**本プラン全体の設計・移行ロードマップの軸**として採用する。

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
| ユーティリティ計算（純粋関数） | 約10 | `HuetoRGB`, `Clamp` | **DI化不要**（カテゴリB、§4.5参照）。`ColorUtil`/`VersionUtil` 等へ移設のみ。 |
| モニタ/座標変換 | 約4 | `TranslateCoorToAbsDisplay` | `IDisplayInfoProvider`。優先度低。 |
| その他フラグ/設定値 | 約100+ | `firstRun`, `touchpadActive[]` 等 | 上記カテゴリに紐づく設定値が大半。個別精査が必要。 |

**重要な構造的事実**: `ControlService.cs`, `Mapping.cs`, `DS4LightBar.cs` の3ファイルは
`using static DS4Windows.Global;` で無条件展開を行っており、`Global` 分割の影響を最も強く受ける。
`Global` は単一の巨大クラスというより、**実質10種類前後のサービスが1つのクラスに同居している**状態であり、
DI化は「1つの `Global` を削除する」のではなく「10個前後のインターフェース付きサービスに再編する」作業である。

**（2026-09-09注記）実地調査の結果**: Phase4・Phase5でDIサービス自体は用意されたものの、呼び出し元側に
`Global.*`の直接参照が193件残存していることが判明した（内訳: `ControlService.cs`39件、`Mouse.cs`27件、
`SettingsViewModel.cs`23件、`App.xaml.cs`22件、`ProfileEditor.xaml.cs`18件、`MouseCursor.cs`14件、
`Mapping.cs`12件、その他UI系ファイル計38件）。これがPhase6の対象である。

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

**（2026-09-09注記）`DS4Devices`はPhase3-Step3-1/3-2にて`IDs4DeviceRegistry`としてインターフェース化済み。
`ControlService.cs`のGlobal直接参照136回のうち、Phase5-Step10までで大半が整理されたが、2026-09-09時点の
実地調査では依然39件が残存しており、Phase6-Step2の対象となっている。**

### 4.3 ViewModel群（33ファイル）の生成パターン分類

| パターン | 件数 | 特徴 | DI化方針 |
|---|---|---|---|
| A: 引数なしで生成可能 | 11件 | `MainWindowsViewModel`, `SettingsViewModel` 等 | `services.AddTransient<VM>()` の単純登録 |
| B: 共有依存（`ControlService` 等）を受け取る | 5件 | `LogViewModel(App.rootHub)` 等 | `ControlService` がDI登録されれば機械的に置換可能 |
| C: 画面表示時に決まる実行時パラメータを受け取る | **17件（過半数）** | `ProfileSettingsViewModel(device)`, `RecordBoxViewModel(deviceNum, controlSettings, shift, repeatable)` 等 | 単純DI登録は不可能。**ファクトリインターフェース方式が必須** |

パターンCが過半数を占めるため、通常の `services.AddTransient<VM>()` 型の登録では対応できず、
`IXxxViewModelFactory.Create(...)` のようなファクトリをDI登録し、内部でDI解決可能な依存と実行時引数を
合成する設計が必要になる。

**（2026-09-09注記）Phase4-Step9にて全29箇所のViewModel直接new生成を完全全廃し、`IViewModelFactory`へ
一本化済み。ただし`SettingsViewModel.cs`等の内部実装には、コンストラクタ注入とは別に`Global.*`への
直接参照が残存しており（Step7の対象、計39件）、これはViewModel生成方式の問題ではなく個々のロジック内の
残存参照の問題としてPhase6で扱う。**

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

### 4.5 DI化対象外項目の確定（カテゴリA〜F、2026-09-09確定）

Phase6着手にあたり、「仮想コントローラー出力バックエンド（ViGEm関連）を除き、原則として全てDI化する」
という方針を採用した。この方針のもとで、**それでもDI化すべきでない、またはDI化する実益がない項目**を
以下のカテゴリA〜Fとして確定する。以降のフェーズ（Phase6・Phase7、および将来のフェーズ）は、これらを
恒久的な除外項目として扱う。

#### カテゴリA: DIコンテナの外側にあるべきもの（原理的に対象外）

| 項目 | 理由 |
|---|---|
| `AppHost.cs` / `ServiceRegistration.cs` / `ServiceProviderHolder.cs`自身 | DIコンテナを組み立てる側のコード。「注入する仕組み自体」は注入できない（自己言及の矛盾） |
| `App.xaml.cs`のPre-Host部分（Host構築前のパス解決・ログ初期化） | §5.2で既に整理済み。DIコンテナが存在する前提のコードなので、コンテナ構築前の処理はDI化しようがない |
| Win32 P/Invoke宣言（`[DllImport]`の`static extern`メソッドそのもの） | 言語仕様上static以外にできない。ただし、これを呼び出すラッパーメソッド（`IEnvironmentService`等）側はDI化対象のままでよい |

#### カテゴリB: 状態を持たず、注入する意味がないもの

| 項目 | 理由 |
|---|---|
| 真の定数（`Global.TEST_PROFILE_INDEX`, `RESOURCES_PREFIX`, `CONFIG_VERSION`等） | 不変値。モックする対象が存在しない |
| 純粋関数（`ColorUtil.HuetoRGB`, `VersionUtil`等） | 入力のみで出力が決まる関数。既に単体テストが容易であり、インターフェース化しても得るものがない |

#### カテゴリC: 自動生成コード

| 項目 | 理由 |
|---|---|
| `*.g.cs` / `*.g.i.cs`（XAMLコンパイル生成物） | 手書きコードではない。`.xaml`が変わらない限り触る対象にならない |
| `*.Designer.cs`（`.resx`生成物） | 同上 |

#### カテゴリD: データの入れ物（振る舞いを持たないPOCO/DTO）

| 項目 | 理由 |
|---|---|
| `ProfileDTO.cs`等のXMLシリアライズ用DTO | DIは「振る舞い（サービス）」を抽象化する仕組みであり、ただのデータ構造をインターフェース化する実益がない |

#### カテゴリE: 動的に生成される実行時ドメインオブジェクト

| 項目 | 理由 |
|---|---|
| `DS4Device`, `JoyConDevice`, `DualSenseDevice`, `SwitchProDevice`, `HidDevice` | 物理コントローラー1台につき1インスタンス、着脱のたびに動的に生成・破棄される。DIコンテナが管理するのは基本的に「アプリ全体で1つ」のシングルトンサービスであり、可変個の実行時オブジェクトをコンテナ登録するのは設計として不自然（ViewModelのパターンCで「Factory経由でnew」という結論に至ったのと同じ理由） |
| `Xbox360OutDevice`, `DS4OutDevice`（仮想コントローラー側） | 同上の理由に加え、そもそも「仮想コントローラー部分」として本プランの既存除外方針に含まれる |
| Viewそのもの（`MainWindow`, `ProfileEditor`等のWindow/UserControlインスタンス） | WPFのXAMLパーサー・ウィンドウライフサイクルが生成・管理しており、DIコンテナのライフタイム管理と衝突する。ただし**View内部で使うサービスは引き続きDI化対象**（`AppHost.GetService<T>()`パターンを維持） |

#### カテゴリF: 明示的な意図を持った例外

| 項目 | 理由 |
|---|---|
| テストファイル内の`Global.*`直接参照（`ProfileSettingsServiceTests.cs`等） | 新旧比較による孤立プロパティバグの再発防止という**意図的な**参照。DI化すると回帰検知能力を失う |
| `HidDevice.cs`/`HidDevices.cs`（HidLibrary） | 有名なOSSライブラリ（mikeobrien/HidLibrary）の設計をそのまま踏襲したコード。「単数形＝実体、複数形＝ファクトリ/リポジトリ」という正しい分業であり、DI化してもテストの実利（実際のHID通信をモックしても意味のある検証にならない）が薄い |

#### 除外方針の適用範囲

カテゴリA〜Fは、Phase6・Phase7を含む**以降の全フェーズに恒久的に適用**する。新たな除外候補が発見された
場合は、必ずどのカテゴリに該当するかを明記した上で本節に追記すること（理由のない除外は認めない）。

**仮想コントローラー出力バックエンド（ViGEm）について**: `ControlService.cs`が`as Xbox360OutDevice`/
`as DS4OutDevice`のダウンキャストで`Nefarius.ViGEm.Client`の型（`IXbox360Controller`, `IDualShock4Controller`
等）に直接アクセスしているフィードバック配線は、`Global`静的クラスの問題とは**無関係の別種の技術的負債**
である。本プランのPhase6・Phase7ではこれを対象外とし、将来的にバックエンド差し替え（ViGEm以外への移行）が
必要になった場合は、`IVirtualControllerBackend`のような新規抽象化を伴う**別イニシアチブ**として扱う。

---

## 5. 目標アーキテクチャ

### 5.1 層 × DIインターフェース対応表（改訂版: SpecialActionの判定/実行分離を反映）

| 層 | 対応する既存コード | 対応するDIインターフェース |
|---|---|---|
| 1. 入力監視層 | `DS4Device`, `DS4Devices` | `IDs4DeviceRegistry` |
| 2-a. 基本マッピング決定 | `Mapping.MapCustom` 内の1:1対応部分 | `IDeviceStateAccessor`（循環依存解消用）／`Mapping` 自体は当面static維持（Phase7で再評価） |
| 2-b. SpecialActionトリガー判定 | `TryDispatchSATriggerEstablished/Released`, `DispatchInputEdge` | `IActionRegistry`／`IManagedActionManager`（既存）。**副作用を持たない純粋な判定ロジックとして再定義** |
| 2-c. アクション選択・パラメータ決定 | `ActionManager.DispatchTriggerEstablished` 内のディスパッチ処理 | 同上。SpecialActionの`typeID`に応じてどの`IOutputAction`実装を呼ぶか決定する部分 |
| 2-d. マクロの分解 | `PlayMacro`前段のマクロ定義パース部分 | 新設 `IMacroDecomposer`（マクロ定義→KBM出力イベント列への純粋変換。モックなしでテスト可能） |
| 3-a. 仮想コントローラー出力 | `outputDevices[ind]` | （既存抽象化のまま。DI化対象外＝カテゴリE） |
| 3-b. KBM出力 | `outputKBMHandler.PerformKeyPress*`、`PlayMacroTask`のタイマーループ | `IVirtualKBM`（マクロの逐次送出もここに統合） |
| 3-c. アプリ内アクション実行 | `Global.ApplyProfile`, `Process.Start`系（`specActionLaunchProc`） | `IProfileRepository`（`ApplyProfile`相当、プロファイル切替の実行。既存§5.4 #2をそのまま使用）／`IProcessLauncher`系（プロセス起動の実行） |
| 4. UI層（制御面） | `DS4Forms/*.xaml`、`ViewModels/*` | ViewModel DI、`IXxxViewModelFactory`、`IProfileSettingsService` 等の設定・状態サービス |
| （層横断） | `Global`（ScpUtil.cs） | `IProfileSettingsService` 等10種以上（Phase4・5で構築、Phase6で呼び出し元を完全移行） |

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
4. **3-a（仮想コントローラー出力）**: 既に比較的完成された抽象化がされているため、優先度は最も低い
   （カテゴリE・§4.5によりDI化対象外として確定）。

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

**（2026-09-09注記）本方針はPhase4-Step6にて実現済み。**

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
| 9 | `IAppearanceSettingsService` | 約6 | 低（**2026-09-09注記: Phase5-Step14前クリーンアップにて新設完了**） |
| 10 | `IDisplayInfoProvider` | 約4 | 低 |
| - | 純粋関数群（DI化不要＝カテゴリB） | 約10 | `ColorUtil`, `VersionUtil` 等へ移設のみ |

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

**（2026-09-09注記）シム自体はPhase4・5で構築済みだが、呼び出し元側での実利用（193件）が残存しており、
Phase6でこれを解消する。Phase6完了後、呼出元0件となったシムメンバには`[Obsolete]`を付与し、削除候補として
管理する（§6.11参照）。**

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

**（2026-09-09改訂）この「見送り」は恒久的な除外ではなく、Phase7として正式に射程に入れる延期項目として
扱う。判断根拠は以下の通り:**

- `Mapping.cs`はアプリ全体で最大（8,500行超）かつ、これまで発見された重大バグの多くの温床であった
  （`Global.RemoveAction`の隠れ自動保存、`PresetMenuHelper`ネストクラスバグ等）。
- Phase6は`ControlService.cs`のホットパス対応という新規リスクを既に抱えており、そこに`Mapping.cs`の
  完全instance化という最大リスク要素を積み増すのは危険と判断した。
- Phase6-Step3で行う「12件の残存参照を`IDeviceStateAccessor`スタイルの引数渡しで解消する」作業は、
  後続の完全instance化と矛盾しない。静的結合を先に減らしておくことがPhase7の下地になる。
- Phase7は、Phase6完了後に専用の事前監査（Phase6-Step1と同様の手法）から独立して開始する。

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

**（2026-09-09注記）本方針はPhase4-Step9にて実現済み（全29箇所のViewModel直接new生成を完全全廃）。**

---

## 6. 段階移行ロードマップ

### 6.1 全体構成

| フェーズ | 名称 | 対応する層 | 主内容 | 既存資産の活用 | 状態（2026-09-09時点） |
|---|---|---|---|---|---|
| フェーズ0 | 基盤整備 | - | DIパッケージ導入、`AppHost.cs` 正式運用化、テスト基盤整備 | `DI-Implementation-Guide.md` | 完了 |
| フェーズ1 | SpecialAction判定・実行の分離 | 2-b/2-c/2-d, 3-c | `Mapping.cs` の副作用を `ActionManager` 経由に統一（判定と実行を分離） | `DI-Migration-Plan.md` をほぼそのまま採用 | 完了 |
| フェーズ2 | KBM出力の抽象化 | 3-b | `outputKBMHandler` を `IVirtualKBM` としてDI登録（ステップA:マクロ逐次送出14箇所、ステップB:通常1:1マッピング48箇所の2段階） | 新規設計 | 完了 |
| フェーズ3 | 入力監視層・信号変換層の整理 | 1, 2 | `IDs4DeviceRegistry` 化、`IDeviceStateAccessor` による循環依存解消 | 新規設計 | 完了 |
| フェーズ4 | `Global` 分割とViewModel DI化 | 横断/UI層 | `Global` を10種以上のサービスへ分割、33件のViewModelをDI/ファクトリへ移行 | 新規設計 | 完了 |
| フェーズ5 | DIサービス内部Legacy経路監査と責務分離 | 横断 | サービス内部の`Global`/`Program.rootHub`再委譲の監査・是正（Step1〜15） | 新規設計 | 進行中（Step1-13完了、Step14実機検証・Step15削除判断が残） |
| **フェーズ6（新設）** | **残存Global実利用箇所の解体** | 横断（信号変換層/入力監視層境界・信号出力層・UI層） | 呼出元193件をDIサービス経由へ置換。仮想コントローラー部分（カテゴリE）は対象外 | `Phase6-Plan.md` | 未着手（Phase5-Step14/15完了後に着手） |
| **フェーズ7（新設・延期）** | **`Mapping.cs`完全instance化** | 2-a（信号変換層） | `Mapping`のstatic中心設計を段階的にinstance化 | Phase6完了後に個別計画書を新設 | 未着手（Phase6完了後に検討開始） |

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

**実績**: 2026-08-26〜2026-08-27（実働2日）。詳細は`Phase1-Status.md`参照。

### 6.4 フェーズ2: KBM出力の抽象化（3-b層、3〜5日 → 通常マッピング分離により見積もり改訂、詳細は下記）

1. `IVirtualKBM` インターフェースを定義（フェーズ1で洗い出し済みのAPIをそのまま踏襲、振る舞い変更なし）。
2. `services.AddSingleton<IVirtualKBM, VirtualKBMHandlerAdapter>();` でDI登録。
3. `Global.outputKBMHandler` を `IVirtualKBM` への薄いデリゲートに置き換え（Strangler Fig）。

フェーズ1完了済みなら `Mapping.cs` 側の変更は不要（`Actions/` 配下に閉じる）という想定だったが、
実装フェーズでの詳細調査（`docs-forDIMG/MadeByAgent/Phase2-Plan.md`）により、`Mapping.cs` 内の
`outputKBMHandler` 直接呼び出しは合計62箇所存在し、そのうち **14箇所はマクロ実行（`PlayMacro`/`EndMacro`）**、
**残り48箇所は通常の1入力→1出力マッピング処理（`Mapping.cs` の中核データフロー）**であることが判明した。
このためフェーズ2を以下の**2つの独立したステップ**に分割する（詳細は `Phase2-Plan.md` 参照）：

- **ステップA（マクロ実行、14箇所）**: §3.3の再整理により、マクロの逐次送出（`PlayMacroTask`のタイマー
  ループ）も3-b（KBM出力）としてこのステップで`IVirtualKBM`経由に統合する（`IMacroDecomposer`が生成した
  イベント列を`IVirtualKBM`が時系列で送出する形）。影響範囲は`PlayMacro`/`EndMacro`内に閉じるため
  相対的に低リスク。
- **ステップB（通常の1:1マッピング処理、48箇所）**: `Mapping.cs` の中核データフロー（毎フレーム・
  毎ボタン押下で実行される経路）を対象とするため、ステップAより**影響範囲・リスクともに大きい**。
  機能カテゴリ別（キー押下/解放系、マウスボタン系、マウスホイール/移動系）にPRを分割し、
  各カテゴリ完了ごとに実機での回帰確認を行う。

**完了判定基準**: `Actions/` 配下が `IVirtualKBM` のみを参照し、モックによる単体テストが可能なこと
（ステップA）。加えて、`Mapping.cs` 内の `outputKBMHandler` 直接呼び出しが0件になり、全62箇所が
`IVirtualKBM` 経由（フォールバック付き）に統一されること（ステップB完了時点）。
**リスク**: `outputKBMHandler` は物理リソース（ドライバハンドル）を保持するため、初期化タイミング依存の
参照が他にないか全数洗い出しが必要。ステップBは入力→出力のリアルタイム経路そのものであるため、
連打・ホールド・同時押し等のタイミング依存挙動について実機回帰テストを必須とする。

**実績**: 2026-08-27〜2026-08-31（中核実装は約3日、Phase3着手後に判明したバグ修正3件の遡及対応を含めて
最大5日）。詳細は`Phase2-Status.md`参照。

### 6.5 フェーズ3: 入力監視層・信号変換層の整理（1・2層、1〜2週間）

1. **`DS4Devices` の `IDs4DeviceRegistry` 化**: デバイス列挙・接続監視をインターフェース化、静的イベントを
   インスタンスイベントに変換。
2. **`IDeviceStateAccessor` による循環依存解消**: `Mapping` の `Program.rootHub` 参照2箇所を置換。
   `Mapping` は「メソッド引数として `IDeviceStateAccessor` を受け取る」形に変更（完全instance化は見送り）。
3. **`Process.Start` 分類②・⑥の抽象化**: 権限昇格を `IElevatedProcessLauncher`、多重起動チェックを
   `IProcessInspector` に切り出し。

#### フェーズ3完了記録（2026-08-31）

フェーズ3の実装、自動テスト、実機確認結果の記録を完了し、本フェーズを完了扱いとする。自動テストは全件成功した。実機確認の詳細は `docs-forDIMG/MadeByAgent/Phase3-Step5-RealDevice-Verification-Checklist.md`、自動テストの詳細は `docs-forDIMG/MadeByAgent/Phase3-Step4-Automated-Test-Coverage-Report.md` を参照する。

実機確認で `△`、`×`、未実施となった項目は、DI 化完了後に対応する未対応事項として引き継ぐ。対象は、Bluetooth 切断後の再接続条件、非管理者起動時の UAC 昇格経路、UAC 承認／拒否時の結果反映、新経路の実使用確認、ラムブル動作および `IDeviceStateAccessor` 経路、`LaunchProgram` の起動・多重起動防止および `IProcessInspector` 経路である。これらは Phase 3 の実装完了を取り消すものではなく、後続の DI 化完了後に確認・修正する。

**完了判定基準**: `Mapping.cs` 内の `Program.rootHub` 参照が0件、`ControlService.cs` から `DS4Devices.`
直接参照が除去されること。
**リスク**: HID通信を含む低レイヤ処理のため、実機での接続/切断シナリオの手動確認を必須とする。確認結果に残る未対応事項は上記の完了記録に従い、DI 化完了後に対応する。

**実績**: 2026-08-29〜2026-08-31（実働3日）。積み残し事項（`ApplyProfileDirect`/`RestoreProfileDirect`の
`Program.rootHub`参照2箇所）はPhase5-Step3にて解消済み（2026-09-09実地確認済み、詳細は
`Phase5-Step13+14-Addendum-Findings-Report-Part2.md`参照）。

### 6.6 フェーズ4: `Global` 分割と ViewModel DI 化（横断/UI 層、6〜8週間）

#### 6.6.1 フェーズ4の位置づけ

フェーズ4は、`DS4Windows/DS4Control/ScpUtil.cs` 内の巨大な静的 `Global` と、`DS4Windows/DS4Forms` の ViewModel 直接生成を、責務別サービス・`AppHost`・Factory へ段階移行するフェーズである。既存機能、ログ、配列インデックス、初期化順序を維持し、旧 API は新経路の確認まで互換シムとして残す。

実コードの棚卸し、具体的なサービス境界、対象 ViewModel、Step ごとの作業手順は [Phase4-Plan.md](MadeByAgent/Phase4-Plan.md) を正本とする。

#### 6.6.2 全体から見た実施項目

詳細な Step 番号・対象ファイル・完了基準は単体計画書で管理し、全体計画書では次の大項目だけを追跡する。

1. `IProfileSettingsService` Placeholder の実装化。
2. プロファイル／SpecialAction 管理の `IProfileRepository`／`ISpecialActionRepository` 分離。
3. 入力・出力・デバイス状態・環境・UI レイアウト・通知の責務分離。
4. `AppHost`／`ServiceRegistration` への Composition Root 一本化。
5. ViewModel の DI／コンストラクタ注入／Factory 化。
6. Phase3 の積み残し（プロファイル適用時の `Program.rootHub`、`LaunchProgram`、実機確認項目）の再確認。

#### 6.6.3 全体レベルの完了条件

- `Global` の主要責務がサービスへ分割され、`ServiceRegistration` から解決できる。
- `IProfileSettingsService` の Placeholder と本番の二重 Composition Root が解消される。
- ViewModel の DI／Factory 化が完了し、画面のライフサイクルと既存機能が維持される。
- Phase3 からの積み残しが解消済み、または再現条件・理由・次の対応先とともに文書化される。
- Phase2/3 を含む全自動テスト、ビルド、主要画面の起動・操作確認が成功する。

詳細なサービス境界、Step 別完了基準、テストケース、リスク対策は [Phase4-Plan.md](MadeByAgent/Phase4-Plan.md) の更新内容を正とする。

**実績**: 2026-08-31〜2026-09-02（実働3日）。当初見積り6〜8週間に対し約14〜19倍の速度で完了。

### 6.7 フェーズ5: DIサービス内部 Legacy 経路監査と責務分離（期間は監査後に再見積もり）

Phase4 完了後、DIサービスの内部実装が `Global`／`Program.rootHub`／Legacy 実体へ再委譲している経路を監査し、優先度を決定したうえで責務分離を進める。詳細は [Phase5-Plan.md](MadeByAgent/Phase5-Plan.md) を正本とする。

1. DIサービス内部の Legacy 再委譲を網羅監査する。
2. プロファイル XML 読込・保存を専用責務へ分離する。
3. プロファイル適用・復帰を専用サービスへ集約し、通常 GUI 切替と編集画面 Save／Apply の適用契約を統一する。
4. Save／Apply の保存成否、再適用成否、ログ、デスクトップ通知を検証可能にする。
5. SpecialAction 永続化、パス、設定状態、デバイス状態、KBM の残存境界を担当計画へ振り分ける。
6. 自動テストと実機検証の後に、Legacy shim の削除可否を別変更として判断する。

**完了判定基準**: Phase5 の監査・責務分離・自動テスト・必要な実機検証が完了し、各 Legacy shim の存置または削除判断が記録されていること。

**進捗（2026-09-09時点）**: Step1〜13完了（ドメイン1〜3全完了、ドメイン4のStep13完了）。Step14（自動テスト・
実機検証）は準備完了・実施待ち、Step15（Legacy shim削除判断）は未着手。詳細は`Phase5-Status.md`参照。
実働見込み: Step1〜13で8日（2026-09-02〜2026-09-09）。

### 6.8 フェーズ6: 残存Global実利用箇所の解体（新設、横断、実働見込み4〜8日）

#### 6.8.1 フェーズ6の位置づけ

Phase4・5にて`Global`の主要責務はDIサービスへ再編されたが、呼び出し元側（`ControlService.cs`,
`Mapping.cs`, `Mouse.cs`, UIビュー・ViewModel群）には、DIサービスが既に存在するにもかかわらず
`Global.X`を直接呼び続けている箇所が実地調査の結果**193件**確認された。フェーズ6は、この呼び出し元側の
残存直接参照を解消し、**仮想コントローラー出力バックエンド（ViGEm、カテゴリE）を除く全レイヤーが
DIサービス経由で一貫して動作する状態**を完成させる。

目的は、完全にDI化された4層構造への移行そのものに加え、**新機能追加時の実装容易性を恒常的に向上させる
こと**である。詳細な対象棚卸し、Step別作業内容、ガードレールは [Phase6-Plan.md](MadeByAgent/Phase6-Plan.md)
を正本とする。

#### 6.8.2 全体から見た実施項目

1. 詳細監査による対象確定（Step1）。
2. コア層（`ControlService.cs`, `Mapping.cs`）のGlobal直参照解消（Step2〜3）。
3. 信号出力層（`Mouse.cs`, `MouseCursor.cs`）のGlobal直参照解消（Step4）。
4. 起動・UI層（`App.xaml.cs`, `ProfileEditor.xaml.cs`, ViewModel群）のGlobal直参照解消（Step5〜8）。
5. 自動テスト・実機検証（Step9）。
6. 呼出元0件シムの`[Obsolete]`化・削除判断（Step10）。

#### 6.8.3 スコープに含まないもの

- 仮想コントローラー出力バックエンド（ViGEm）の抽象化（§4.5「仮想コントローラー出力バックエンドについて」参照）。
- `Mapping.cs`の完全instance化（Phase7へ委譲）。
- カテゴリA〜Fに該当する項目全般（§4.5参照）。

#### 6.8.4 全体レベルの完了条件

- Step1で確定した対象の呼出元が、全てDIサービス経由の呼び出しに置換されていること。
- `ControlService.cs`等のホットパスにおいて、置換前後で体感可能な性能劣化がないこと。
- 既存の全自動テストが成功を維持していること。
- `ProfileEditor.xaml.cs`を含むUI層全体で、Phase5-Step13と同水準の静的参照撲滅が完了していること。

詳細は [Phase6-Plan.md](MadeByAgent/Phase6-Plan.md) および [Phase6-Status.md](MadeByAgent/Phase6-Status.md) を正とする。

**着手前提**: Phase5-Step14（自動テスト・実機検証）およびStep15（Legacy shim削除判断）の完了。

### 6.9 フェーズ7: `Mapping.cs`完全instance化（新設・延期、詳細は個別策定）

#### 6.9.1 フェーズ7の位置づけ

§5.5で述べた通り、`Mapping.cs`の完全instance化はPhase1〜6を通じて意図的に見送られてきた。フェーズ7は、
フェーズ6完了後にこれを正式な射程に入れ、`Mapping`のstatic中心設計を段階的にinstance化するフェーズである。

#### 6.9.2 フェーズ6に含めず独立させた理由

1. **リスクの隔離**: `Mapping.cs`はアプリ全体で最大（8,500行超）かつ、これまで発見された重大バグ
   （`Global.RemoveAction`の隠れ自動保存、`PresetMenuHelper`ネストクラスバグ等）の多くの温床であった。
   フェーズ6は`ControlService.cs`のホットパス対応というリスクを既に抱えており、そこに最大リスク要素を
   積み増すのは危険と判断した。
2. **元の判断ロジックとの整合**: §5.5の「見送り」判断は、単なる先延ばしではなく「周辺の副作用切り出しが
   完了してから再評価する」という意図的なチェックポイント設計であった。フェーズ6完了時点でも
   `ControlService.cs`のホットパス問題という新しいリスクが確定していない状態のため、この再評価の
   タイミングとしてはまだ早いと判断した。
3. **作業の非競合**: フェーズ6-Step3で行う「12件の残存参照を`IDeviceStateAccessor`スタイルの引数渡しで
   解消する」作業は、後のフル変換（static→instance化）と矛盾しない。静的結合を先に減らしておくことが
   フェーズ7でのフル変換の下地になる。

#### 6.9.3 現時点で確定している事項

- フェーズ6完了後、専用の事前監査（フェーズ6-Step1と同様の手法）から独立して開始する。
- 詳細なStep構成、見積もり、ガードレールは、フェーズ6完了後に`Phase7-Plan.md`として個別策定する。

**着手前提**: フェーズ6の完了。

### 6.10 全体スケジュール見積もり

| フェーズ | 当初見積もり | 実績／実績ベース見積り |
|---|---|---|
| フェーズ0（基盤整備） | 1〜2日 | 1日 |
| フェーズ1（SpecialAction判定・実行分離） | 2〜4週間 | 2日 |
| フェーズ2（3-b: KBM出力抽象化、ステップA+B） | 1〜2週間 | 3〜5日 |
| フェーズ3（1・2層: 入力/変換層整理） | 1〜2週間 | 3日 |
| フェーズ4（Global分割 + ViewModel DI化） | 6〜8週間 | 3日 |
| フェーズ5（DIサービス内部Legacy経路監査） | 1〜2週間（実施の結果15ステップへ再編） | Step1-13で8日、Step14-15残 |
| **フェーズ6（残存Global実利用箇所の解体）** | - | **実働4〜8日程度（見込み）** |
| **フェーズ7（Mapping.cs完全instance化）** | - | **フェーズ6完了後に個別見積り** |
| **合計（フェーズ0〜5）** | **概算13〜19週間** | **実働約20〜25日程度**（フェーズ5残作業含む見込み） |

※ AIエージェントによる実装のため、機械的な置換作業が中心のフェーズ（1・4）ほど当初見積もりに対する
速度倍率が大きい（14〜19倍）。実機確認が律速するフェーズ（3）は倍率が相対的に小さい（2〜5倍）。
フェーズ6・7の見積りはこの実績傾向を踏まえた概算である。

### 6.11 共通のPR運用方針

1. **1 PR = 1機能 or 1インターフェース**を基本単位とする。呼び出し元の一括置換は別PRに分離。
2. **フォールバックを残した移行**: 新経路の動作確認後、別PRでフォールバックを削除。
3. **各PRにユニットテストを含める**: 新設インターフェースには最低1つのモックベーステスト。
4. **既存インベントリドキュメント（`Direct-Callsites-Inventory.md`等）を都度更新**し、進捗を文書と同期。
5. **（フェーズ6以降で追加）実地確認の原則**: 計画書・報告書に記載する`Global`メンバ名・参照箇所数は、
   実装前に必ず`git`ベースの実地確認を経る（Phase5-Watchpoints報告書での教訓を反映）。

### 6.12 進捗の定量指標（ダッシュボード案）

| 指標 | ベースライン | 目標 | 2026-09-09時点の実績 |
|---|---|---|---|
| `Global.xxx` を参照するファイル数 | 75ファイル | フェーズ6完了時点でシム経由のみ・新規増加なし | 実利用箇所193件（十数ファイルに分散）が残存、フェーズ6の対象 |
| `Program.rootHub`/`App.rootHub` を参照するファイル数 | 15ファイル | フェーズ3〜6で段階的に減少 | `App.rootHub`は完全削除済み。`Program.rootHub`無条件直接代入は4ファイルに縮小（Phase5-Step14前クリーンアップで統一済み） |
| `Mapping.cs`内の直接副作用呼び出し件数 | 複数箇所（分類済み） | フェーズ1〜3完了時点で0 | 達成済み（Global参照12件は残存するが副作用呼び出しとは別分類） |
| DIコンテナ登録サービス数 | 5 | フェーズ5完了時点で全10+αサービス登録済み | 23以上のサービスが登録済み（達成） |
| `new XxxViewModel(` のうちDI非経由の件数 | 33件 | フェーズ4完了時点で0 | 0件（達成済み、Phase4-Step9） |
| `AppHost.cs` が実際に呼ばれているか | 呼ばれていない（デッドコード） | フェーズ0で呼ばれる状態に、フェーズ6で完全移行 | フェーズ0で解消済み。呼び出し元側の完全移行はフェーズ6で対応 |

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
| `Mapping`のstatic中心設計との整合 | フェーズ3〜6 | 引数渡し方式を採用し、完全instance化はフェーズ7へ独立させることで即時対応を回避 |
| 設定値アクセスのスレッド競合顕在化 | フェーズ4 | 元`Global`メンバの呼び出し元スレッドを洗い出しロック追加 |
| ViewModelパターンCのファクトリ設計コスト | フェーズ4 | ステージ2で洗い出した17件リストをToDoとして使用 |
| UI/Updater系の起動処理が仕様上置換不可な場合 | 全体 | 分類③④⑤は対象外とし、Integration noteとして別途記録 |
| **`ControlService.cs`のホットパス（毎秒250〜1000回の入力ポーリング）でのDI置換による性能劣化** | **フェーズ6** | **置換前後のベンチマーク比較を必須化。DIサービス側は内部キャッシュを持たずBackingStoreへ直接委譲する設計とする** |
| **ドキュメント記述がgrep未確認のまま後続計画の前提を誤らせる** | **フェーズ6以降全般** | **Phase5-Watchpoints報告書での教訓を反映し、`git`ベースの実地確認を経てから記載する運用ルールを徹底（§6.11参照）** |

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
| - | `docs-forDIMG/MadeByAgent/Phase4-Plan.md` / `Phase4-Status.md` | フェーズ4の実施計画・進捗 |
| - | `docs-forDIMG/MadeByAgent/Phase5-Plan.md` / `Phase5-Status.md` | フェーズ5の実施計画・進捗 |
| - | `docs-forDIMG/MadeByAgent/Phase5-Watchpoints-Investigation-Report.md` | UI層残存静的参照・スレッド安全性・二重実体化の調査（2026-09-09実地確認により一部訂正） |
| - | `docs-forDIMG/MadeByAgent/Phase5-Step13+14-Addendum-Findings-Report-Part1.md` / `-Part2.md` | Global参照・`Program.rootHub`残存状況の実地調査 |
| - | `docs-forDIMG/MadeByAgent/Phase5-Step14-pretest-cleanup-plan.md` / 完了報告書 | Step14前の`Global.*`残存参照解消・`Program.rootHub`統一 |
| - | `docs-forDIMG/MadeByAgent/Phase6-Plan.md` / `Phase6-Status.md` | フェーズ6の実施計画・進捗（新設） |
| - | `docs-forDIMG/MadeByAgent/Phase6-Step1-Plan.md` 以降の各Step計画書 | フェーズ6の個別Step計画（新設、順次作成） |
| - | `docs/DI-Migration-Plan.md`（既存） | フェーズ1のベースとして採用 |
| - | `docs/DI-Implementation-Guide.md`（既存） | 推奨パッケージ・導入手順のベースとして採用 |
| - | `docs/DI/DI-ObjectGraph.md`（既存） | Composition Root設計のベースとして採用 |
| - | `docs/Direct-Callsites-Inventory.md`（既存） | `Process.Start`等の分類のベースとして活用 |

### 9.3 次のアクション

1. Phase5-Step14（自動テスト実行・実機検証）を完了させる。
2. Phase5-Step15（Legacy shim削除判断）を完了させる。
3. フェーズ6-Step1（詳細監査）に着手し、`Phase6-Step1-Global-Usage-Classification-Report.md`を作成する。
4. フェーズ6の各Step完了時に、§6.12の定量指標を計測し、`Phase6-Status.md`に記録する。
5. フェーズ6完了後、フェーズ7（`Mapping.cs`完全instance化）の個別計画書を新規策定する。

---

*本書はステージ1〜4の調査（`01-現状分析.md` 〜 `05-段階移行ロードマップ.md`）および補足ステージ
（`04-理想の3層構造と現状のズレ.md`）の内容を統合したものである。各ステージの詳細な調査過程・
根拠データ（コード行数・参照回数等の実測値）は、それぞれの元ドキュメントを参照のこと。
2026-09-09改訂分（フェーズ6・7の新設、DI化対象外カテゴリA〜Fの確定）は、Phase5完了間際の実地調査
（`git clone`ベースのリポジトリ全文grep）に基づく。*