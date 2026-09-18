# Phase6-Step4 計画書: マウスエミュレーション系（`Mouse.cs`, `MouseCursor.cs`, `MouseWheel.cs`）のGlobal直参照解消

作成日: 2026-09-09  
改訂日: 2026-09-18（Phase6-Step1 コア参照エビデンスに基づく推奨案［Pure DIコンストラクタ引数注入 ＋ `MouseWheel.cs` 統合］採用・全面拡充改訂）  
状態: 計画書改訂・承認待ち（実装未着手）  
対象ブランチ: `For-DI-migration-work`  
上位計画書: `docs-forDIMG/MadeByAgent/Phase6-Plan.md`  
準拠指針: `.github/copilot-instructions.md`, `docs-forDIMG/DI-App-Wide-Migration-Plan.md`  
参照エビデンス:  
  - `Phase6-Step1-Core-Reference-Evidence.md` §4, §5, §8（マウス系実地調査エビデンス）  
  - `Phase6-Step1-ABC-Classification.md`（a/b/c分類台帳）  
  - `Phase6-Step1-Unique-ID-Manifest.md`（一意ID台帳）  

---

## 0. 背景と改訂の経緯

### 0.1 暫定27件から実質73参照への精査と `MouseWheel.cs` の統合
Phase6上位計画（`Phase6-Plan.md` §0.2）では、本ステップの対象件数を「暫定27件（`Mouse.cs`: 16件、`MouseCursor.cs`: 11件）」としていた。  
しかし、2026-09-18に実施された Step1 再監査（`Phase6-Step1-Core-Reference-Evidence.md` §4, §5）において、実コード走査の結果、`Mouse.cs` には48箇所（除外1件を除く実参照47箇所）、`MouseCursor.cs` には21箇所（除外1件を除く実参照20箇所）の Global 参照が存在することが判明した。  
さらに、`Mouse.cs` の内部コンポーネントとしてマウスホイール制御を担当する隣接ファイル **`MouseWheel.cs`**（実参照6箇所）が、マウスエミュレーションの不可分な構成要素であることが Step1 申し送り（§8）にて指摘された。

### 0.2 方針の確定: 【Pure DI コンストラクタ引数注入 ＋ `MouseWheel.cs` 正式統合】
`Mouse.cs`、`MouseCursor.cs`、`MouseWheel.cs` は、静的クラス（`ControlService` や `Mapping`）とは異なり、コントローラー接続時に動的に生成される**インスタンスクラス**である。  
本改修では、以下の推奨方針を採用してマウスサブシステム全体の Global 直接参照を完全に根絶する：

1. **Pure DI コンストラクタ引数注入（推奨案 選択肢1）**:
   各クラスのコンストラクタを拡張し、必要な DI サービス（`IProfileSettingsService`, `IVirtualKBM`, `IAppSettingsService`, `IEnvironmentService`）を引数として受け取る。インスタンス内の `private readonly` フィールドに保持し、毎レポート実行されるホットパス（`sixaxisMoved`, `touchesMoved` 等）での**実行時オーバーヘッドを極小（仮想呼び出し1回のみ・ゼロアロケーション）**とする。  
   ※過渡期互換のため、省略可能引数（`= null`）および `AppHost.GetService<T>()` / `Global` への安全なフォールバックを維持する。
2. **`MouseWheel.cs` の正式統合（推奨案 案X）**:
   マウスエミュレーションを構成する3ファイル（`Mouse.cs`, `MouseCursor.cs`, `MouseWheel.cs`、計73実参照）を一括で本Step4のスコープに含め、マウス関連の技術的負債を1ステップで完全解決する。
3. **明示的除外（2箇所）**:
   `Mouse.cs`（339行）および `MouseCursor.cs`（410行）の `Global.Clamp` は純粋計算ユーティリティ（状態非保持）であるため除外する。

---

## 1. 目的

1. `Mouse.cs`, `MouseCursor.cs`, `MouseWheel.cs` の全インスタンスクラスにおいて、コンストラクタ経由での Pure DI 注入構造を確立する。
2. 3ファイル内に存在する全73箇所の実利用 Global 直接参照を、注入された DI サービス経由の呼び出しへ完全置換する。
3. マウス・ジャイロ・ホイール操作の超高頻度ホットパス（1000Hz）において、GC アロケーションゼロおよび入力遅延ゼロを厳格に維持する。

---

## 2. 全件参照台帳（全73箇所・完全カタログ）

### 表1: `Mouse.cs` の全参照（ID: C4-M01 〜 C4-M47、実参照47箇所、除外1箇所）
すべて分類(a)（`IProfileSettingsService` が同一の BackingStore 連動プロパティ・配列を既に所持）。

| ID | 行番号 | メソッド名 | 参照メンバ | 分類 | ホットパス | 移行先サービス・メンバ |
|---|---|---|---|---|---|---|
| C4-M01 | 36 | `sixaxisMoved` | `Global.TouchSensitivity[deviceNum]` | (a) | **H** | `_profileSettings.TouchSensitivity[deviceNum]` |
| C4-M02 | 40 | `sixaxisMoved` | `Global.Trackball[deviceNum]` | (a) | **H** | `_profileSettings.Trackball[deviceNum]` |
| C4-M03 | 42 | `sixaxisMoved` | `Global.TrackballFriction[deviceNum]` | (a) | **H** | `_profileSettings.TrackballFriction[deviceNum]` |
| C4-M04 | 44 | `sixaxisMoved` | `Global.TouchpadJitterCompensation[deviceNum]` | (a) | **H** | `_profileSettings.TouchpadJitterCompensation[deviceNum]` |
| C4-M05 | 60 | `sixaxisMoved` | `Global.TouchpadInvert[deviceNum]` | (a) | **H** | `_profileSettings.TouchpadInvert[deviceNum]` |
| C4-M06 | 68 | `sixaxisMoved` | `Global.TouchpadClickPassthru[deviceNum]` | (a) | **H** | `_profileSettings.TouchpadClickPassthru[deviceNum]` |
| C4-M07〜M44 | 72〜244 | `touchesMoved`, `touchesBegan`, `touchesEnded` | `Global.TouchpadButton[deviceNum]`（タッチボタン判定38箇所） | (a) | **H** | `_profileSettings.TouchpadButton[deviceNum]`（配列直接参照） |
| C4-M45 | 258 | `touchesMoved` | `Global.TouchpadInvert[deviceNum]` | (a) | **H** | `_profileSettings.TouchpadInvert[deviceNum]` |
| C4-M46 | 270 | `touchesMoved` | `Global.TouchSensitivity[deviceNum]` | (a) | **H** | `_profileSettings.TouchSensitivity[deviceNum]` |
| C4-M47 | 331 | `sixaxisMoved` | `Global.GetGyroOutMode(deviceNum)` | (a) | **H** | `_profileSettings.GetGyroOutMode(deviceNum)` |
| C4-MEX1 | 339 | `sixaxisMoved` | `Global.Clamp(...)` | 除外 | **H** | 純粋計算ユーティリティ（状態非保持） |

---

### 表2: `MouseCursor.cs` の全参照（ID: C4-C01 〜 C4-C20、実参照20箇所、除外1箇所）

| ID | 行番号 | メソッド名 | 参照メンバ | 分類 | ホットパス | 移行先サービス・メンバ |
|---|---|---|---|---|---|---|
| C4-C01 | 36 | `calculate` | `Global.ButtonMouseSensitivity[deviceNumber]` | (a) | **H** | `_profileSettings.ButtonMouseSensitivity[deviceNumber]` |
| C4-C02 | 40 | `calculate` | `Global.ButtonMouseVerticalScale[deviceNumber]` | (a) | **H** | `_profileSettings.ButtonMouseVerticalScale[deviceNumber]` |
| C4-C03 | 44 | `calculate` | `Global.ButtonMouseOffset[deviceNumber]` | (a) | **H** | `_profileSettings.ButtonMouseOffset[deviceNumber]` |
| C4-C04 | 52 | `calculate` | `Global.Rainbow[deviceNumber]` | (a) | H条件 | `_profileSettings.Rainbow[deviceNumber]` |
| C4-C05 | 167 | `mouseStickMoved` | `Global.outputKBMHandler.MoveRelativeMouse` | (a) | **H** | `_virtualKBM.MoveRelativeMouse` |
| C4-C06 | 172 | `mouseStickMoved` | `Global.outputKBMHandler.MoveRelativeMouse` | (a) | **H** | `_virtualKBM.MoveRelativeMouse` |
| C4-C07 | 208 | `mouseStickMoved` | `Global.outputKBMHandler.MoveRelativeMouse` | (a) | **H** | `_virtualKBM.MoveRelativeMouse` |
| C4-C08 | 213 | `mouseStickMoved` | `Global.outputKBMHandler.MoveRelativeMouse` | (a) | **H** | `_virtualKBM.MoveRelativeMouse` |
| C4-C09 | 236 | `sixaxisMoved` | `Global.GyroMouseSmoothInfo[deviceNumber]` | (a) | **H** | `_profileSettings.GyroMouseSmoothInfo[deviceNumber]` |
| C4-C10 | 240 | `sixaxisMoved` | `Global.GyroMouseDeadzone[deviceNumber]` | (a) | **H** | `_profileSettings.GyroMouseDeadzone[deviceNumber]` |
| C4-C11 | 244 | `sixaxisMoved` | `Global.GyroMouseMinVelocity[deviceNumber]` | (a) | **H** | `_profileSettings.GyroMouseMinVelocity[deviceNumber]` |
| C4-C12 | 248 | `sixaxisMoved` | `Global.GyroMouseSensSettings[deviceNumber]` | (a) | **H** | `_profileSettings.GyroMouseSensSettings[deviceNumber]` |
| C4-C13 | 252 | `sixaxisMoved` | `Global.GyroMouseToggle[deviceNumber]` | (a) | **H** | `_profileSettings.GyroMouseToggle[deviceNumber]` |
| C4-C14 | 256 | `sixaxisMoved` | `Global.GyroMouseInvert[deviceNumber]` | (a) | **H** | `_profileSettings.GyroMouseInvert[deviceNumber]` |
| C4-C15 | 260 | `sixaxisMoved` | `Global.GyroMouseStickSmoothInfo[deviceNumber]` | (a) | **H** | `_profileSettings.GyroMouseStickSmoothInfo[deviceNumber]` |
| C4-C16 | 264 | `sixaxisMoved` | `Global.GyroMouseStickToggle[deviceNumber]` | (a) | **H** | `_profileSettings.GyroMouseStickToggle[deviceNumber]` |
| C4-C17 | 362 | `touchesMoved` | `Global.absUseAllMonitors` | (b) | **H** | `_appSettings.AbsUseAllMonitors`（シム追加） |
| C4-C18 | 364 | `touchesMoved` | `Global.TranslateCoorToAbsDisplay` | (c) | **H** | 画面座標計算（`_environmentService` または座標シム） |
| C4-C19 | 373 | `touchesMoved` | `Global.outputKBMHandler.MoveAbsoluteMouse` | (a) | **H** | `_virtualKBM.MoveAbsoluteMouse` |
| C4-C20 | 420 | `touchesMoved` | `Global.GyroMouseSensSettings[deviceNumber]` | (a) | **H** | `_profileSettings.GyroMouseSensSettings[deviceNumber]` |
| C4-CEX1 | 410 | `touchesMoved` | `Global.Clamp(...)` | 除外 | **H** | 純粋計算ユーティリティ（状態非保持） |

---

### 表3: `MouseWheel.cs` の全参照（ID: C4-W01 〜 C4-W06、実参照6箇所）

| ID | 行番号 | メソッド名 | 参照メンバ | 分類 | ホットパス | 移行先サービス・メンバ |
|---|---|---|---|---|---|---|
| C4-W01 | 55 | `touchesMoved` | `Global.ScrollSensitivity[deviceNumber]` | (a) | **H** | `_profileSettings.ScrollSensitivity[deviceNumber]` |
| C4-W02 | 82 | `touchesMoved` | `Global.outputKBMMapping.WHEEL_TICK_DOWN` | (a) | **H** | `_profileSettings.OutputKBMMapping.WHEEL_TICK_DOWN` |
| C4-W03 | 83 | `touchesMoved` | `Global.outputKBMMapping.WHEEL_TICK_UP` | (a) | **H** | `_profileSettings.OutputKBMMapping.WHEEL_TICK_UP` |
| C4-W04 | 85 | `touchesMoved` | `Global.outputKBMMapping.WHEEL_TICK_DOWN` | (a) | **H** | `_profileSettings.OutputKBMMapping.WHEEL_TICK_DOWN` |
| C4-W05 | 86 | `touchesMoved` | `Global.outputKBMMapping.WHEEL_TICK_UP` | (a) | **H** | `_profileSettings.OutputKBMMapping.WHEEL_TICK_UP` |
| C4-W06 | 88 | `touchesMoved` | `Global.outputKBMHandler.PerformMouseWheelEvent` | (a) | **H** | `_virtualKBM.PerformMouseWheelEvent` |

---

## 3. アーキテクチャ設計・Pure DI 伝搬

### 3.1 コンストラクタ拡張設計
すべてのマウス系クラスに対し、コンストラクタ引数で必要なサービスを受け取り、`readonly` フィールドに格納する設計とする。

```csharp
// Mouse.cs コンストラクタ拡張設計
public class Mouse : ITouchpadBehaviour
{
    private readonly IProfileSettingsService _profileSettings;
    private readonly IVirtualKBM _virtualKBM;
    private readonly IAppSettingsService _appSettings;
    private readonly IEnvironmentService _environmentService;

    public Mouse(
        int deviceID,
        DS4Device d,
        IProfileSettingsService profileSettings = null,
        IVirtualKBM virtualKBM = null,
        IAppSettingsService appSettings = null,
        IEnvironmentService environmentService = null)
    {
        this.deviceNum = deviceID;
        this.dev = d;
        this._profileSettings = profileSettings ?? AppHost.GetService<IProfileSettingsService>() ?? Global.ProfileSettingsServiceInstance;
        this._virtualKBM = virtualKBM ?? AppHost.GetService<IVirtualKBM>();
        this._appSettings = appSettings ?? AppHost.GetService<IAppSettingsService>();
        this._environmentService = environmentService ?? AppHost.GetService<IEnvironmentService>();

        this.cursor = new MouseCursor(deviceNum, d.GyroMouseSensSettings, _profileSettings, _virtualKBM, _appSettings, _environmentService);
        this.wheel = new MouseWheel(deviceNum, _profileSettings, _virtualKBM);
        ...
    }
}
```

```csharp
// MouseCursor.cs コンストラクタ拡張設計
public class MouseCursor : IInstanceIdentifiable
{
    private readonly IProfileSettingsService _profileSettings;
    private readonly IVirtualKBM _virtualKBM;
    private readonly IAppSettingsService _appSettings;
    private readonly IEnvironmentService _environmentService;

    public MouseCursor(
        int deviceID,
        GyroMouseSens sensSettings,
        IProfileSettingsService profileSettings = null,
        IVirtualKBM virtualKBM = null,
        IAppSettingsService appSettings = null,
        IEnvironmentService environmentService = null)
    {
        this.deviceNumber = deviceID;
        this.sensSettings = sensSettings;
        this._profileSettings = profileSettings ?? AppHost.GetService<IProfileSettingsService>() ?? Global.ProfileSettingsServiceInstance;
        this._virtualKBM = virtualKBM ?? AppHost.GetService<IVirtualKBM>();
        this._appSettings = appSettings ?? AppHost.GetService<IAppSettingsService>();
        this._environmentService = environmentService ?? AppHost.GetService<IEnvironmentService>();
        ...
    }
}
```

```csharp
// MouseWheel.cs コンストラクタ拡張設計
class MouseWheel
{
    private readonly IProfileSettingsService _profileSettings;
    private readonly IVirtualKBM _virtualKBM;

    public MouseWheel(
        int deviceID,
        IProfileSettingsService profileSettings = null,
        IVirtualKBM virtualKBM = null)
    {
        this.deviceNumber = deviceID;
        this._profileSettings = profileSettings ?? AppHost.GetService<IProfileSettingsService>() ?? Global.ProfileSettingsServiceInstance;
        this._virtualKBM = virtualKBM ?? AppHost.GetService<IVirtualKBM>();
        ...
    }
}
```

### 3.2 生成元（`DS4Device.cs` / `ControlService.cs`）でのサービス伝搬
`DS4Device` が `Mouse` をインスタンス化する箇所において、注入されたサービスインスタンスを直接コンストラクタ実引数として渡す。これにより、サービスロケータを迂回した Pure DI 呼び出しが確立される。

---

## 4. ホットパス性能維持とアーキテクチャ・ガードレール

1. **ゼロアロケーション（Zero Allocation）**:
   `sixaxisMoved`, `touchesMoved` は毎秒250〜1000回呼び出される。メソッド内部でのオブジェクト生成、クロージャ、LINQ呼び出しを完全排除する。
2. **BackingStore 直列配列参照の維持**:
   `_profileSettings.TouchpadButton[deviceNum]` 等の配列参照は、内部で固定長配列を直接返す getter を介して行われる。ディクショナリ検索や動的クローンを絶対に挟まない。
3. **ホットパス内のログ・サービス解決の厳禁**:
   毎レポートのイベントハンドラ内で `AppHost.GetService` や `[DI]` 追跡ログを呼び出さない。コンストラクタでキャッシュした `readonly` フィールドのみを参照する。

---

## 5. PR分割計画とマイクロステップ

変更の安全性と追跡性を確保するため、以下の**5つのサブPR（マイクロステップ）**に分割して実装を進める。各PRごとに `dotnet test` を実行し、全件グリーンを確認する。

```text
【Phase6-Step4 マイクロステップ構成】
├─ Step4-0: 着手前提検証・ベースライン記録（実装なし）
├─ Step4-1 (PR-1): MouseWheel.cs のコンストラクタ拡張と全6箇所置換（ID: C4-W01〜W06）
├─ Step4-2 (PR-2): MouseCursor.cs のコンストラクタ拡張と全20箇所置換（ID: C4-C01〜C20）
├─ Step4-3 (PR-3): Mouse.cs のコンストラクタ拡張と全47箇所置換（ID: C4-M01〜M47）
├─ Step4-4 (PR-4): DS4Device.cs / ControlService.cs における Mouse 生成引数配線（Pure DI 伝搬）
└─ Step4-5 (PR-5): 単体テスト追加・マイクロベンチマーク計測・実機体感検証・完了報告書作成
```

---

### Step4-0: 着手前提検証・ベースライン記録（実装なし）
1. Step3 完了後の作業ツリー状態、HEAD コミットハッシュ、未コミット差分ゼロを確認する。
2. ソリューション全体のビルド（`dotnet build -c Release`）および既存テスト（`dotnet test`）を実行し、全件合格を記録する。

---

### Step4-1 (PR-1): `MouseWheel.cs` のコンストラクタ拡張と置換
- **対象**: `MouseWheel.cs`（ID: C4-W01 〜 C4-W06、計6箇所）
- **作業内容**:
  1. `MouseWheel` コンストラクタに `IProfileSettingsService`, `IVirtualKBM` 引数を追加（互換フォールバック付き）。
  2. `Global.ScrollSensitivity`, `Global.outputKBMMapping.WHEEL_TICK_*`, `Global.outputKBMHandler.PerformMouseWheelEvent` を注入フィールド参照へ置換。
- **検証**:
  - `dotnet test` 全件合格を確認。

---

### Step4-2 (PR-2): `MouseCursor.cs` のコンストラクタ拡張と置換
- **対象**: `MouseCursor.cs`（ID: C4-C01 〜 C4-C20、計20箇所）
- **作業内容**:
  1. `IAppSettingsService` に `AbsUseAllMonitors` シムを追加。
  2. `IEnvironmentService` に `TranslateCoorToAbsDisplay` シムを追加。
  3. `MouseCursor` コンストラクタに4サービスを受け取る引数を追加。
  4. 相対移動（`MoveRelativeMouse`）、絶対移動（`MoveAbsoluteMouse`）、各種ジャイロ・ボタンマウス感度参照を置換。
- **検証**:
  - 単体テスト: モックを用いた相対・絶対マウス移動呼び出し検証。
  - `dotnet test` 全件合格を確認。

---

### Step4-3 (PR-3): `Mouse.cs` のコンストラクタ拡張と置換
- **対象**: `Mouse.cs`（ID: C4-M01 〜 C4-M47、計47箇所）
- **作業内容**:
  1. `Mouse` コンストラクタに4サービスを受け取る引数を追加。
  2. 内部で生成する `cursor` および `wheel` へ注入されたサービスインスタンスを伝搬。
  3. タッチ感度、反転、38箇所のタッチボタン判定、ジャイロ出力モード取得を置換。
- **検証**:
  - 単体テスト: `Mouse` インスタンス化およびタッチイベント発火時のモック呼び出し検証。
  - `dotnet test` 全件合格を確認。

---

### Step4-4 (PR-4): `DS4Device.cs` / `ControlService.cs` における生成引数配線
- **対象**: `DS4Device.cs` における `touchpad = new Mouse(...)` 呼び出し箇所
- **作業内容**:
  1. `DS4Device` が保持または受け取ったサービスインスタンスを `Mouse` コンストラクタに渡すように配線。
  2. これにより Pure DI パスを完成させ、内部フォールバック（`AppHost.GetService`）への依存を解消。
- **検証**:
  - `dotnet build -c Release` で警告・エラー0件を確認。
  - `dotnet test` 全件合格を確認。

---

### Step4-5 (PR-5): テスト追加・性能検証・実機検証・完了報告
- **作業内容**:
  1. `MouseDiWiringTests.cs` を新規作成し、DI経由での生成とモック結合をテスト。
  2. ポーリング処理時間マイクロベンチマーク計測。
  3. 実機コントローラー（タッチパッド操作・ジャイロマッピング・ホイールスクロール）による検証を実施。
  4. 完了報告書（`Phase6-Step4-Completion-Report.md`）を作成。

---

## 6. テスト・性能・実機検証計画

### 6.1 自動単体テスト計画
`DS4WindowsTests` に以下のテストクラスを追加する（1ファイル1型）：
1. **`MouseDiWiringTests.cs`**:
   - `Mouse`, `MouseCursor`, `MouseWheel` が全モックサービスを受け取って正常にインスタンス化されること。
   - `cursor` および `wheel` へ親のサービスインスタンスが正確に伝搬されていること。
2. **`MouseWheelShimTests.cs`**:
   - `MouseWheel` のホイールイベント発火時に `IVirtualKBM.PerformMouseWheelEvent` が正確なパラメータで呼び出されること。
3. **`MouseCursorMovementTests.cs`**:
   - スティック移動およびタッチ移動時に、`IVirtualKBM.MoveRelativeMouse` および `MoveAbsoluteMouse` が意図した座標で呼び出されること。

### 6.2 ホットパス性能検証（マイクロベンチマーク）
- **測定対象**: `Mouse.sixaxisMoved` および `Mouse.touchesMoved` の1回あたりの処理時間（μs）。
- **判定基準**:
  - 置換前後の平均処理時間差が ±5% 以内であること。
  - 100,000回実行時の Gen0 GC 発生回数が 0 回（完全ゼロアロケーション）であること。

### 6.3 実機動作検証項目（Phase6-Step11連携）
1. **タッチパッド操作**:
   - タッチパッド上での指の移動によるスムーズなマウスカーソル移動。
   - タッチパッドタップ・左右クリック・マルチタッチジェスチャーの正確な認識。
2. **ジャイロマッピング**:
   - コントローラーを傾けた際のマウスカーソル追従（ジャイロマウス）。
   - スムージングおよびデッドゾーン設定の反映。
3. **ホイールスクロール**:
   - タッチパッドまたは割り当てボタンによる上下ホイールスクロール動作の追従。

---

## 7. ロールバック方針

1. **PR単位の即時差し戻し**: 万が一入力遅延やカーソル飛び等の不具合が確認された場合は、該当PRを `git revert` して直前の健全状態へ復旧する。
2. **フォールバックの温存**: コンストラクタ引数にデフォルト値（`= null`）を設けているため、呼び出し元の配線を戻すだけで即座に旧動作へ切り戻し可能。

---

## 8. 完了判定チェックリスト

- [ ] `MouseWheel.cs` が本ステップのスコープに正式統合されていること。
- [ ] 表1〜表3に定義された全73箇所の実利用 Global 直接参照が、DI サービス経由の呼び出しへ置換されていること。
- [ ] 表1・表2の除外項目（`Global.Clamp` 2件）以外の `Global.` 直接参照が 3ファイル内から完全に根絶されていること。
- [ ] `Mouse`, `MouseCursor`, `MouseWheel` の全コンストラクタが Pure DI 引数注入に対応していること。
- [ ] `DS4Device.cs` から `Mouse` へのサービスインスタンス伝搬が配線されていること。
- [ ] ホットパスにおいてゼロアロケーション（GC Alloc = 0）および処理遅延なしが確認されていること。
- [ ] `dotnet build -c Release` でエラー・警告が0件であること。
- [ ] `DS4WindowsTests` および `StandaloneTests` の全自動テストが100%成功を維持していること。
- [ ] 実機コントローラーによるタッチパッド・ジャイロ・ホイール操作の追従性が正常であること。