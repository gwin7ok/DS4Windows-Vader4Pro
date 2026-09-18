# Phase6-Step3 計画書: `Mapping.cs` の局所的引数渡しおよびPhase7引き継ぎ台帳化

作成日: 2026-09-09  
改訂日: 2026-09-18（Phase6-Step1 コア参照エビデンスに基づく案A［局所的引数渡し＋Phase7完全引き継ぎ台帳化］採用・全面拡充改訂）  
状態: 計画書改訂・承認待ち（実装未着手）  
対象ブランチ: `For-DI-migration-work`  
上位計画書: `docs-forDIMG/MadeByAgent/Phase6-Plan.md`  
準拠指針: `.github/copilot-instructions.md`, `docs-forDIMG/DI-App-Wide-Migration-Plan.md` §5.5, §6.9  
参照エビデンス:  
  - `Phase6-Step1-Core-Reference-Evidence.md` §2（`Mapping.cs` 実地走査エビデンス）  
  - `Phase6-Step1-ABC-Classification.md`（a/b/c分類台帳）  
  - `Phase6-Step1-Unique-ID-Manifest.md`（一意ID台帳）  

---

## 0. 背景と方針確定（案Aの採用）

### 0.1 暫定12件から実質115参照への精査結果
Phase6上位計画（`Phase6-Plan.md` §0.2）における「暫定12件」は、`Mapping.cs` 内の `Global.` 明示修飾子による単純 grep 抽出に過ぎなかった。  
2026-09-18に実施された Step1 精密再監査（`Phase6-Step1-Core-Reference-Evidence.md` §2）において、同ファイル冒頭の `using static DS4Windows.Global;`（31行目）により、超高頻度ホットパス（`SetCurveAndDeadzone` 等）を中心に**合計115箇所（除外4件を除く実参照111箇所）**の Global 依存式が存在することが判明した。

### 0.2 方針の確定: 【案A: 局所的引数渡し＋Phase7引き継ぎ厳格化】
`Mapping.cs`（約8,500行）は全メソッドが `static` で構成され、毎秒250〜1000回呼び出される入力信号変換の中核である。  
全体移行計画書（`DI-App-Wide-Migration-Plan.md` §5.5, §6.9）および上位計画書（`Phase6-Plan.md` §0.3.2）で確定している**「`Mapping.cs` の完全インスタンス化はPhase7として独立させ、Phase6では既存の委譲・引数渡しパターンの範囲内でのみ安全に対応する」**という原則を堅持し、以下の通りスコープを厳密に分離する：

1. **Phase6-Step3 実装対象（10箇所）**:
   - 初期化、画面座標変換、コントローラー設定のXML永続化、プロファイル適用連携などの非ホットパス処理（N / N†）について、呼び出し元（`ControlService` 等）からのメソッド引数渡しによって `Global.*` 直接参照を安全に解消する。
2. **Phase7 完全引き継ぎ台帳化（101箇所）**:
   - `SetCurveAndDeadzone` 内のデッドゾーン・感度・ベジェ曲線設定（34箇所）、各種スティック・ボタン・ジャイロ・アクション設定（38箇所）、`outputKBMMapping` テーブル参照（29箇所）は、無理に引数を肥大化させず、ホットパス性能（毎秒1000Hz・ゼロアロケーション）を保護するため、**全件にIDを付与した完全引き継ぎ台帳**として記録し、Phase7のインスタンス化・ドメイン分割時に一括移行する。
3. **明示的除外（4箇所）**:
   - 純粋計算ユーティリティ（`Clamp`, `getTransitionedColor`）および真の定数（`MAX_DS4_CONTROLLER_COUNT` 等）は対象外とする。

---

## 1. 目的

1. `Mapping.cs` 内の非ホットパス処理における Global 直接参照（10箇所）を、DI サービスを受け取るメソッド引数渡し方式により安全に解消する。
2. 入力ポーリング超高頻度ホットパス（101箇所）について、現在の静的構造とゼロアロケーション性能を 100% 維持し、リグレッションを完全に防止する。
3. Phase7（Mapping完全インスタンス化・ドメイン責務分割）へ引き継ぐ全参照箇所のカタログ（行番号・メンバ名・移行先インターフェース・分割設計方針）を確定・公認化する。

---

## 2. 実装対象台帳（Phase6-Step3 実装対象: 全10箇所）

非ホットパスであり、メソッドシグネチャへのサービス引数追加によって安全に `Global.*` を解消できる全10箇所の一覧である。

| ID | 行番号・形式 | メソッド名 | 参照メンバ | 分類 | ホットパス | 移行方針（引数渡し設計） |
|---|---|---|---|---|---|---|
| C3-01 | 107 Q | `MapCustom`（初期化・画面設定） | `Global.absUseAllMonitors` | (b) | N | `IAppSettingsService.AbsUseAllMonitors` を引数渡しまたは初期化時取得 |
| C3-02 | 112 Q | `MapCustom`（座標系計算） | `Global.TranslateCoorToAbsDisplay` | (c) | N | 画面座標計算（過渡期ヘルパーまたは引数渡し） |
| C3-03 | 120 Q | `MapCustom`（マウス状態初期化） | `Global.ButtonAbsMouseInfos[slot]` | (a) | N | `IProfileSettingsService.ButtonAbsMouseInfos[slot]` を引数渡し |
| C3-04 | 128 Q | `MapCustom`（マウス状態初期化） | `Global.ButtonAbsMouseInfos[device]` | (a) | N | `IProfileSettingsService.ButtonAbsMouseInfos[device]` を引数渡し |
| C3-05 | 131 Q | `MapCustom`（プロファイルパス取得） | `Global.ProfilePath[device]` | (a) | N† | `IProfileRepository.ProfilePath[device]` を引数渡し |
| C3-06 | 133 Q | `MapCustom`（プロファイル適用） | `Global.ApplyProfile(device, ...)` | (b) | N† | プロファイル切替要求（引数渡しまたはコールバック委譲） |
| C3-07 | 135 Q | `MapCustom`（設定保存） | `Global.SaveControllerConfigs(device, ...)` | (b) | N | `IProfileXmlStore.SaveControllerConfigsForDevice(...)` を引数渡し |
| C3-08 | 139 Q | `MapCustom`（設定ロード） | `Global.LoadControllerConfigs(device)` | (b) | N | `IProfileXmlStore.LoadControllerConfigsForDevice(...)` を引数渡し |
| C3-09 | 140 Q | `MapCustom`（プロファイルパス取得） | `Global.ProfilePath[device]` | (a) | N† | `IProfileRepository.ProfilePath[device]` を引数渡し |
| C3-10 | 147 Q | `MapCustom`（出力デバイス種別取得） | `Global.OutContType[device]` | (a) | N† | `IProfileSettingsService.OutContType[device]` を引数渡し |

---

## 3. Phase7 完全引き継ぎ台帳（ホットパス残存分: 全101箇所）

以下の101箇所は、毎レポート（毎秒250〜1000回）実行されるホットパス、または高頻度マクロ・非同期処理であり、静的クラスのまま引数渡しを行うとメソッドシグネチャの爆発的肥大化とスタックオーバーヘッドを招くため、**Phase7（クラスのインスタンス化・ドメイン分割）で一括移行する引き継ぎ対象**として厳格に管理する。

### 3.1 `SetCurveAndDeadzone`（毎レポート超高頻度ホットパス H: 計34箇所）
すべて `IProfileSettingsService`（P）の配列・設定メンバであり、Phase7においてスティック曲線計算エンジン（`IStickCurveProcessor`）のインスタンスプロパティとして移行する。

| 引き継ぎID | 行番号・形式 | 対象メンバ | 移行先インターフェース | ホットパス | Phase7における移行方針 |
|---|---|---|---|---|---|
| C3-H01〜H02 | 871, 874 U | `LSRotation[device]`, `RSRotation[device]` | `IProfileSettingsService` | **H** | スティックプロセッサに設定サービスを注入 |
| C3-H03〜H04 | 884, 888 U | `LSAntiSnapbackInfo[device]`, `RSAntiSnapbackInfo[device]` | `IProfileSettingsService` | **H** | アンチスナップバック計算のインスタンス化 |
| C3-H05〜H06 | 895, 898 U | `LSModInfo[device]`, `RSModInfo[device]` | `IProfileSettingsService` | **H** | デッドゾーン・モディファイアの直接参照 |
| C3-H07〜H08 | 906, 912 U | `L2Sens[device]`, `R2Sens[device]` | `IProfileSettingsService` | **H** | トリガー感度計算のインスタンス化 |
| C3-H09 | 920 U | `SquStickInfo[device]` | `IProfileSettingsService` | **H** | スクエアスティック計算のインスタンス化 |
| C3-H10〜H13 | 928, 934, 942, 948 U | `lsOutBezierCurveObj`, `rsOutBezierCurveObj`, `l2Out*`, `r2Out*` | `IProfileSettingsService` | **H** | ベジェ曲線オブジェクト参照のインスタンス化 |
| C3-H14 | 955 U | `GyroOutputMode[device]` | `IProfileSettingsService` | **H** | ジャイロモード判定のインスタンス化 |
| C3-H15〜H16 | 962, 968 U | `L2OutputSettings[device]`, `R2OutputSettings[device]` | `IProfileSettingsService` | **H** | トリガー出力設定参照のインスタンス化 |
| C3-H17 | 975 U | `CustomLed[device]` | `IProfileSettingsService` | **H** | LED状態参照のインスタンス化 |
| C3-H18 | 982 U | `useDInputOnly[device]` | `IProfileSettingsService` | **H** | 出力バイパス判定のインスタンス化 |
| C3-H19〜H34 | 990〜1065 U | スティック軸・感度・デッドゾーン残存16参照 | `IProfileSettingsService` | **H** | スティックプロセッサ内部への完全カプセル化 |

### 3.2 その他のスティック・ボタン・アクション設定（H / H条件 / A: 計38箇所）
Phase7において、ボタンマッパー（`IButtonActionMapper`）、6軸センサープロセッサ（`ISixaxisProcessor`）、マクロ実行エンジン（`IMacroExecutor`）へ分割・インスタンス化する。

| 引き継ぎID | 行番号・形式 | 対象メンバ | 移行先インターフェース | 経路 | Phase7における移行方針 |
|---|---|---|---|---|---|
| C3-H35〜H38 | 1205〜1230 U | ドリフト補正用 `LSModInfo`, `RSModInfo` | `IProfileSettingsService` | **H** | スティックキャリブレーションクラスへ集約 |
| C3-H39 | 1340 U | `getProfileActionCount(device)` | `IProfileActionProvider` | **H** | アクションディスパッチャへ集約 |
| C3-H40〜H44 | 1410〜1455 U | `GetControlSettingsGroup(device, ...)` | `IProfileSettingsService` | **H** | コントロール設定プロバイダへ集約 |
| C3-H45〜H48 | 1512〜1560 U | `ProfileActions[device]` | `IProfileActionProvider` | **H** / A | プロファイルアクションプロバイダへ集約 |
| C3-H49〜H52 | 1620〜1680 U | `reverseX360ButtonMapping[device]` | `IProfileSettingsService` | **H** | 出力リマッパーへ集約（配列直接アクセス維持） |
| C3-H53〜H56 | 1710〜1750 U | `GetDS4CSetting(device, ...)` | `IProfileSettingsService` | **H** | コントローラー設定アクセスへ集約 |
| C3-H57〜H60 | 1820〜1860 U | `ActionList` | `ISpecialActionRepository` | A | スペシャルアクションリポジトリへ集約 |
| C3-H61〜H66 | 2105〜2180 U | `SXSens`, `SZSens` 等の6軸センサー感度 | `IProfileSettingsService` | **H** | 6軸センサープロセッサへ集約 |
| C3-H67〜H68 | 2300〜2320 U | `WheelSmoothInfo[device]` | `IProfileSettingsService` | **H** | マウスホイールエミュレータへ集約 |
| C3-H69〜H72 | 2450〜2490 U | `TouchpadActiveArray`, `TouchOutMode` | `IProfileSettingsService` | **H** | タッチパッドプロセッサへ集約 |

### 3.3 `outputKBMMapping` 関連（H / A: 計29箇所）
すべて `IProfileSettingsService.OutputKBMMapping` 経由のキーボード・マウス仮想変換テーブルおよび定数参照である。Phase7において仮想KBM出力トランスレータ（`IKBMTranslator`）へインスタンス化する。

| 引き継ぎID | 行番号・形式 | 対象メンバ | 移行先インターフェース | 経路 | Phase7における移行方針 |
|---|---|---|---|---|---|
| C3-H73〜H82 | 3010〜3250 U | `MOUSEEVENTF_*`, `WHEEL_TICK_*` | `IVirtualKBM` / `OutputKBMMapping` | **H** | 仮想KBM出力定数プロバイダへ集約 |
| C3-H83〜H92 | 3400〜3680 U | `GetRealEventKey(...)`, `macroKeyTranslate(...)` | `OutputKBMMapping` | **H** / A | キーコード変換エンジンへ集約 |
| C3-H93〜H101 | 4100〜4500 U | マッピングテーブル配列・定数参照各種 | `OutputKBMMapping` | **H** / A | マッピングトランスレータへ集約 |

---

## 4. 表3: 明示的除外項目台帳（ID: C3-EX01 〜 C3-EX04、計4参照）

| ID | 行番号 | 参照メンバ | 除外理由（根拠） |
|---|---|---|---|
| C3-EX01 | 複数 | `Global.Clamp(...)` | 純粋計算ユーティリティ（`Math.Clamp` 相当、状態非保持） |
| C3-EX02 | 複数 | `Global.getTransitionedColor(...)` | 純粋計算ユーティリティ（RGB色空間線形補間、状態非保持） |
| C3-EX03 | 複数 | `Global.MAX_DS4_CONTROLLER_COUNT` | 真の定数（`const int`、4層モデル共通値） |
| C3-EX04 | 複数 | `Global.TEST_PROFILE_ITEM_COUNT` | 真の定数（`const int`） |

---

## 5. アーキテクチャ設計・局所的引数渡し（Pure DI）

### 5.1 メソッドシグネチャ拡張設計
非ホットパスの対象メソッド（`MapCustom` 内の初期化・設定永続化ブロック等）に対し、必要なサービスを引数として追加する。

```csharp
// 局所的引数渡しのシグネチャ設計例（互換用オーバーロードまたはオプショナル引数）
public static void ProcessInitialCustomSettings(
    int device,
    DS4Device ds4Device,
    IProfileSettingsService profileSettings,
    IProfileRepository profileRepository,
    IProfileXmlStore profileXmlStore,
    IAppSettingsService appSettings)
{
    // C3-01: absUseAllMonitors
    bool useAllMonitors = appSettings.AbsUseAllMonitors;
    
    // C3-03, C3-04: ButtonAbsMouseInfos
    var mouseInfo = profileSettings.ButtonAbsMouseInfos[device];
    
    // C3-05, C3-09: ProfilePath
    string profilePath = profileRepository.ProfilePath[device];
    
    // C3-07: SaveControllerConfigs
    profileXmlStore.SaveControllerConfigsForDevice(ds4Device, profilePath);
    
    // C3-08: LoadControllerConfigs
    profileXmlStore.LoadControllerConfigsForDevice(ds4Device);
    
    // C3-10: OutContType
    var outType = profileSettings.OutContType[device];
}
```

### 5.2 呼び出し元（`ControlService`）との連携
Step2で `ControlService` に注入されたサービスインスタンス（`_profileSettings`, `_profileRepository`, `_profileXmlStore`, `_appSettings`）を、上記メソッド呼び出し時にそのまま実引数として渡す。  
これにより、静的サービスロケータ（`AppHost.GetService`）を `Mapping.cs` 内部に持ち込まず、Pure DI の原則を徹底する。

---

## 6. PR分割計画とマイクロステップ

リスクと変更範囲を最小限に抑えるため、以下の**3つのサブPR（マイクロステップ）**に分割して進める。

```text
【Phase6-Step3 マイクロステップ構成】
├─ Step3-0: 着手前提検証・ベースライン記録（実装なし）
├─ Step3-1 (PR-1): 画面座標・初期化設定の引数渡し（ID: C3-01 〜 C3-04）
├─ Step3-2 (PR-2): 設定永続化・プロファイル適用の引数渡し（ID: C3-05 〜 C3-10）
└─ Step3-3 (PR-3): Phase7完全引き継ぎ台帳のコードベース反映・自動テスト検証・完了報告
```

---

### Step3-0: 着手前提検証・ベースライン記録（実装なし）
1. Step2 完了後の作業ツリー状態、コミットハッシュ、未コミット差分ゼロを確認する。
2. ソリューション全体のビルド（`dotnet build -c Release`）および全単体テスト（`dotnet test`）が 100% グリーンであることを記録する。

---

### Step3-1 (PR-1): 画面座標・初期化設定の引数渡し
- **対象**: ID: C3-01 〜 C3-04（`absUseAllMonitors`, `TranslateCoorToAbsDisplay`, `ButtonAbsMouseInfos`）
- **作業内容**:
  1. `Mapping.cs` の対象ブロックをプライベートヘルパーメソッドとして抽出。
  2. `IAppSettingsService` および `IProfileSettingsService` を引数として受け取るシグネチャを定義。
  3. `ControlService` から注入済みサービスを渡すように配線。
- **検証**:
  - `dotnet test` で回帰がないことを確認。

---

### Step3-2 (PR-2): 設定永続化・プロファイル適用の引数渡し
- **対象**: ID: C3-05 〜 C3-10（`ProfilePath`, `ApplyProfile`, `SaveControllerConfigs`, `LoadControllerConfigs`, `OutContType`）
- **作業内容**:
  1. `IProfileRepository`, `IProfileXmlStore`, `IProfileSettingsService` を受け取る引数渡しを実装。
  2. `ControlService` 側の呼び出し箇所を更新。
  3. プロファイル設定の読み込み・保存の動作が以前と完全に一致することを確認。
- **検証**:
  - 単体テスト: プロファイルパス取得、XML保存呼び出しのモック検証テストを追加。
  - `dotnet test` 全件合格を確認。

---

### Step3-3 (PR-3): Phase7完全引き継ぎ台帳のコードベース反映・完了報告
- **作業内容**:
  1. `Mapping.cs` 内のホットパス残存部（101箇所）に、将来のPhase7作業者が容易に追跡できるよう、一意の引き継ぎコメント（例: `// [Phase7-Handover: C3-H01] IProfileSettingsService.LSRotation`）を付与。
  2. 完了報告書（`Phase6-Step3-Completion-Report.md`）を作成し、10件の実装解消と101件の引き継ぎ台帳を完全に突き合わせて記録。
- **検証**:
  - ホットパスのメソッド（`SetCurveAndDeadzone`）のロジックおよび命令列に意図しない変更・オーバーヘッドがないことを確認。
  - `dotnet test` 全件パスを確認。

---

## 7. テスト・回帰検証計画

### 7.1 自動単体テスト計画
`DS4WindowsTests` に以下のテストを追加する（1ファイル1型を厳守）：
1. **`MappingSettingsPassThroughTests.cs`**:
   - `Mapping` の引数渡しメソッドにモックサービス（`IProfileSettingsService`, `IProfileRepository`, `IProfileXmlStore`）を渡し、意図したプロパティが呼び出されることを検証。
   - スロットインデックスごとの分離が正確に行われることを検証。
2. **`MappingHotPathIntegrityTests.cs`**:
   - `SetCurveAndDeadzone` などのホットパスメソッドが、本改修によってシグネチャ変更や余分なヒープ割り当て（GC Alloc）を起こしていないことを検証。

### 7.2 回帰テスト基準
- `Actions.Tests` および `StandaloneTests` の全テストが 100% 成功を維持すること。
- `dotnet build -c Release` で警告・エラーが0件であること。

---

## 8. ロールバック方針

万が一、引数追加による予期せぬスタックオーバーヘッドや設定適用の不整合が発生した場合は、以下の手順で直ちにロールバックを行う：
1. **PR単位の即時 revert**: 影響を受けたPRのみを `git revert` し、直前の健全なコミットに復帰する。
2. **静的呼び出しの温存**: `Global.*` メンバ自体はコードベースに存在するため、引数受け渡し前の直接呼び出しに戻すことで即座に元の動作を回復できる。

---

## 9. 完了判定チェックリスト

- [ ] 表1に定義された非ホットパス全10箇所が、DIサービスを受け取る引数渡し方式に置換されていること。
- [ ] 表2-1〜表2-3に定義された全101箇所のホットパス残存部について、引き継ぎID・移行先インターフェース・設計方針が完全台帳化されていること。
- [ ] 表3に定義された除外項目（4件）以外の意図せぬ残存参照が存在しないこと。
- [ ] `SetCurveAndDeadzone` 等のホットパスにおいて、新たなヒープ割り当て（GC Alloc）が発生していないこと。
- [ ] `ControlService` から `Mapping` への引数渡しが Pure DI の原則に準拠していること。
- [ ] `dotnet build -c Release` でエラー・警告が0件であること。
- [ ] `DS4WindowsTests` および `StandaloneTests` の全自動テストが100%成功を維持していること。