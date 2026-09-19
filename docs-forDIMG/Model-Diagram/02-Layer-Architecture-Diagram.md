# レイヤー構成図（スタック図）
**（Layered Stack Architecture Specification）**

本ドキュメントは、巨大ファイル群の1クラス1ファイル解体（パイプライン＆プロセッサ化、通信とパケットパースの分離、UIのMediator化）を反映した、DS4Windows-Vader4Pro の実行時スタック構造、データパイプライン、およびレイヤー間の責務境界を定義する。

---

## 1. 実行時スタック構造図

```text
┌──────────────────────────────────────────────────────────────────────────────────────────────────┐
│                                   第4層: UI層 (Presentation Layer)                               │
│  WPF Views (MainWindow, ProfileEditor, BindingWindow, etc.)                                      │
│  ├── Singleton ViewModels: MainWindowsViewModel / ControllersViewModel                           │
│  ├── Transient ViewModels: SettingsViewModel / LogViewModel / AboutViewModel                     │
│  └── ProfileEditor 分割ViewModels (Mediatorパターンによる調停):                                   │
│       ProfileSettingsViewModel (親Mediator: 保存・集約・全体設定)                                 │
│       ├── StickSettingsSubVM (LS/RS 感度/デッドゾーン/カーブ)   TriggerSettingsSubVM (L2/R2/モーター)│
│       ├── ButtonMappingSubVM (ボタンリマップ/シフト)          SpecialActionsSubVM (アクション管理) │
│       └── IViewModelFactory (動的パラメータ注入・サブVM群の生成)                                  │
└────────────────────────────────────────────────┬─────────────────────────────────────────────────┘
                                                 │ 依存 (UIバインド・要求発行・イベント購読)
                                                 ▼
┌──────────────────────────────────────────────────────────────────────────────────────────────────┐
│                               第2層: 信号変換層 (Domain & Application Core)                       │
│  【ループ統括】 ControlService (ファサード) ──> IInputLoopCoordinator (高速ループ実行エンジン)       │
│  【状態管理】   MappingPipelineContext Pool (スロット別常駐バッファ: インプレース更新 / Zero-GC)   │
│  【切替制御】   IProfileApplicationService (適用/復帰/Halt) / IAutoProfileService (自動切替)     │
│  【アクション】 IManagedActionManager (アクション実行・トグル) ──> IMappingActionDispatcher        │
│  【マッピング】 IInputMappingPipeline (変換パイプライン親: Context統括)                           │
│                 ├── IButtonProcessor (ボタン変換・シフト)   IStickProcessor (軸/カーブ/AntiSnap)   │
│                 ├── ITriggerProcessor (2段階トリガー)      ITouchGyroProcessor (タッチ/ジャイロ)  │
│                 └── IMouseEngine (Mouse.cs解体先: マウス加速度/OneEuroFilter平滑化/Trackball)     │
└───────────────────────┬──────────────────────────────────────────────────┬───────────────────────┘
                        │                                                  │
          依存 (生データ要求)                                               │ 依存 (変換後データ送出)
                        ▼                                                  ▼
┌───────────────────────────────────────────────┐  ┌───────────────────────────────────────────────┐
│          第1層: 入力監視層 (Ingress)            │  │          第3層: 信号出力層 (Egress)            │
│  IDs4DeviceRegistry (デバイス検出・管理)       │  │  3-a 仮想パッド: IOutputSlotService           │
│  ├─ IDeviceHotplugMonitor (Win32挿抜検知)     │  │  │   └─ ViGEm Client (Xbox360 / DS4 OutDevice) │
│  ├─ IHidTransport (USB / Bluetooth 通信)      │  │  3-b KBM/マクロ: IVirtualKBM (SendInput/Faker)│
│  └─ IInputReportParser / OutputReportEncoder  │  │  │   └─ IMacroPlayer (非同期時系列マクロ再生)  │
│     ├─ DS4ReportParser / Encoder              │  │  3-c 副作用実行:                               │
│     ├─ Vader4ProReportParser / Encoder        │  │      ├─ IProcessLauncher / ElevatedLauncher    │
│     └─ DualSenseReportParser / Encoder        │  │      ├─ IProfileSwitcher (アクション内切替)     │
│                                               │  │      ├─ IUdpServerService (Cemuhook Motion)    │
│                                               │  │      └─ ILightbarService (LED/発光色計算)      │
└───────────────────────────────────────────────┘  └───────────────────────────────────────────────┘
  ▲                                                  │
  │ 【データパイプライン (Data Pipeline: 250Hz〜1000Hz)】 │
  └───────────── 生HIDパケット ───────────> 変換・補正 ───┴───────────> 仮想ゲームパッド / OS入力
                                              │
                                              ▼
┌──────────────────────────────────────────────────────────────────────────────────────────────────┐
│                          横断基盤・永続化層 (Cross-Cutting Infrastructure)                        │
│  【設定・プロファイル】 IProfileRepository / IProfileXmlStore (Profile.xml CRUD・シリアライズ)    │
│  【アプリ設定】         IAppSettingsService (AppSettings.xml 永続化)                              │
│  【スロット/デバイス】  IOutputSlotStore (OutputSlots.xml) / IDeviceOptionRepository (*OptsDTO)    │
│  【排他制御・OS基盤】   XmlIoLock (プロセス内/スレッド間ロック) / IPathService / IEnvironmentService│
└──────────────────────────────────────────────────────────────────────────────────────────────────┘
```

---

## 2. 採用アーキテクチャ方針の詳細解説

### 方針1：ステートレスSingletonプロセッサ ＋ `MappingPipelineContext` によるZero-GCインプレース更新
* **課題：** スティックのアンチスナップバックや `OneEuroFilter` などの平滑化には「前フレームの座標や時間」という内部状態が必要だが、毎秒1000回のループでインスタンスを `new` するとGCプレッシャーで遅延が発生する。
* **解決策：**
  * 各プロセッサ（`StickProcessor` 等）は**完全ステートレスなSingleton**とする。
  * スロットごとに1つ常駐するバッファオブジェクト `MappingPipelineContext` を事前確保（プール）する。
  * このコンテキスト内に「現在フレームの生入力」「前フレームの状態履歴（HistoryState）」「変換後出力バッファ」を保持し、各プロセッサが順次**インプレース（破壊的書き換え）**で更新していく。これにより、毎フレームのアロケーションを完全にゼロ（Zero-GC）化する。

### 方針2：Layer 1 における通信（Transport）と解析（Parser/Encoder）の分離
* **課題：** `DS4Device.cs` や `Vader4ProDevice.cs` にOS通信ハンドルとバイト解析コードが同居していると、実機をPCに接続しないとテストができない。
* **解決策：**
  * OSとの入出力（`ReadFile`/`WriteFile`）を担当する `IHidTransport` と、バイト列を構造体にパースする `IInputReportParser`、フィードバック用パケットを構築する `IOutputReportEncoder` を分離。
  * パケット解析クラスはOS非依存の純粋なロジックとなるため、Vader 4 Pro の独自ボタン（C/Zボタン、背面M1〜M4）のデコード検証などをテストコードで100%自動検証可能にする。

### 方針3：UI層における Mediator パターンによる親・子ViewModelの調停
* **課題：** `ProfileSettingsViewModel.cs`（158KB）を分割する際、サブVM同士が過度なイベント駆動で通信すると循環参照や更新順序の破綻が起きやすい。
* **解決策：**
  * 親の `ProfileSettingsViewModel` が Mediator となり、各サブVM（`StickSubVM`, `TriggerSubVM` 等）を直接保持・調停する。
  * 「保存」ボタンが押された際は、親VMが各子VMから設定スライスを吸い上げて `ProfileDTO` に統合し、`IProfileXmlStore` に一括保存を要求する。

### 方針4：3-b KBM出力における「送出」と「ライフサイクル」の分離
* **課題：** 現行の `OutputKBMHandlerAdapter` は `Global.outputKBMHandler` への読み取り専用委譲で、出力ハンドラの生成・差し替え・フォールバック切替の口が無く、`ControlService` が `Global` の static フィールドを直接書き換えている。
* **解決策：**
  * 送出（キー・マウス出力）は `IVirtualKBM`、ハンドラの生成・破棄・フォールバック切替・マッピング初期化は `IVirtualKBMLifecycle` に分ける。
  * 上記のスタック図の 3-b `IVirtualKBM` に付随するインターフェースとして扱う（図の枠は変更しない）。ライフサイクル操作を使うのは第2層の `ControlService` のみで、送出側の利用者（Actions・`IMouseEngine`）には公開しない。

---

## 3. 「データフロー」と「依存関係」の比較対比

```text
【データの流れる方向 (Data Flow - 毎秒250〜1000回)】
  [物理コントローラー]
         │ (生HIDパケット)
         ▼
  [1. 入力監視層] (IHidTransport ──> IInputReportParser)
         │ (Parsed RawInputState)
         ▼
  [2. 信号変換層] (IInputMappingPipeline ──[Contextインプレース更新]──> 各Processor)
         │ (Transformed State / SpecialActions)
         ▼
  [3. 信号出力層] (IOutputSlotService / IVirtualKBM)
         │ (仮想パッド信号 / キーマウ)
         ▼
     [PCゲーム / OS]

【プログラムの依存方向 (Dependency Flow - DIP原則)】
  [4. UI層 (Views / ViewModels)]
         │
         │ (利用)
         ▼
  [2. 信号変換層] ────────────┐
   │            │             │
   │ (利用)     │ (利用)      │ (利用)
   ▼            ▼             ▼
  [1. 入力監視層] [3. 信号出力層] [横断基盤・永続化層]
```

---

%% 注釈補強（2026-09-19 Phase6-Step2 実地確認・決定D1〜D3反映）
%% - 方針4（3-b の送出とライフサイクルの分離、`IVirtualKBMLifecycle`）を追加（決定D3）。スタック図・依存方向図の構造は変更なし。
%% - 依存方向（上位→下位）に反する過渡期の逆依存（`OutputSlotService` → `ControlService` 等）が現状実装に存在する。目標構造は変更せず、04-Service-Lifecycle-Spec.md §4 で技術的負債として管理する。

%% 注釈補強（2026-09-18監査結果反映、構造変更なし）
%% - データパイプライン（生HID→変換→仮想パッド/OS入力）は現状のホットパス（ControlService入力ループ、Mouse系加速度処理）と一致。Zero-GCインプレース更新（MappingPipelineContext）の原則を維持。
%% - 依存方向（UI→変換→入力/出力→横断基盤）はDIP原則に従い、現状の監査結果（Global型503メンバの各層分散、ScpUtilの横断基盤層分解、MappingのPipeline+Processor分解）と完全一致。
%% - ScpUtil.cs（255件静的宣言）の横断基盤層への分解は理想構造と一致するが、参照と宣言の区別を維持（§2抽出問題記録済み）。
%% - Phase5証跡（Step14/15未完了）の前提条件を維持（証跡存在確認済み、内容検証別途必要：Phase5-Evidence-Check.md 参照）。
%% - c除外（≈304件）の改修対象外原則を維持（Classification-Metrics.md, C-Classification-Count.md 参照）。