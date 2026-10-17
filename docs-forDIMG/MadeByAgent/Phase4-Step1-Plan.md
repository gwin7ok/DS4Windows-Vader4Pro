# フェーズ4-Step1 計画書: IProfileSettingsService 実装化

作成日: 2026-08-31
対象ブランチ: For-DI-migration-work
前提ドキュメント:
- `docs-forDIMG/DI-App-Wide-Migration-Plan.md` §4.1, §5, §6.6（全体計画）
- `docs-forDIMG/MadeByAgent/Phase4-Plan.md` §1.2, §2, §3 Step1（Phase4詳細計画）
- `docs-forDIMG/MadeByAgent/Phase4-Status.md`（Phase4進捗管理）
- `docs-forDIMG/MadeByAgent/Phase4-Step0-Plan.md`（Step0計画書）
- `docs-forDIMG/MadeByAgent/Phase4-Step0-Completion-Report.md`（Step0完了報告）
- `docs-forDIMG/MadeByAgent/Phase4-Step0-Global-Member-Inventory.md`（Globalメンバー棚卸し実測値: 442件、プロファイル・入力設定174件、呼び出し元80ファイル）
- `docs-forDIMG/MadeByAgent/Phase4-Step0-ViewModel-Inventory.md`（ViewModel直接生成29件）
- `docs-forDIMG/MadeByAgent/Phase4-Step0-DI-Startup-Sequence.md`（DI起動順序と二重コンテナ構造）
- `docs-forDIMG/MadeByAgent/Phase4-Step0-Baseline-Test-Report.md`（基準ビルド・テスト結果: ビルド警告0、Tests 31/31・13/13通過）
- `.github/copilot-instructions.md`（エージェント作業ルール）

---

## ルール確認（作業開始前に毎回読む）

- **§2.1 フォールバック実装の原則（修正版）**:
  - `Global` の既存静的メンバーは削除せず、新設する `IProfileSettingsService` / `ProfileSettingsService` への薄いデリゲートシムとして残す。
  - 1つの設定アクセスに対して複数の新実装経路を同時に作成しない（単一路線の徹底）。
- **§2.2 現在の機能の完全維持 (No Feature Drop)**:
  - プロファイル設定の既定値、配列境界（`TEST_PROFILE_ITEM_COUNT` = 9, `MAX_DS4_CONTROLLER_COUNT` = 8）、カルチャ（`configFileDecimalCulture` = en-US）、特殊状態フラグ（`touchpadActive`, `useTempProfile`, `tempprofilename` 等）の挙動を100%維持する。
  - 既存の条件分岐や例外ハンドリングを「最適化」として省略・改変しない。
- **§2.3 ログ出力の厳格な維持**:
  - 既存ログ関数（`AppLogger.LogToGui` 等）およびログレベルを厳格に維持する。`Console.WriteLine` 等を新設しない。
- **§3.1 DI (Dependency Injection) の実装**:
  - インターフェースには `I` プレフィックスを付与（`IProfileSettingsService`）。
  - コンテナ登録は `DS4Windows/DI/ServiceRegistration.cs`（`AppHost`）に行う。
- **§3.2 巨大ファイル (`ScpUtil.cs` / `Global`) の編集方針**:
  - `ScpUtil.cs` 全体を再生成せず、対象メンバーのみをピンポイントで置換する。
- **§4 / §5 マイクロステップ進行**:
  - タスク Step1-1 〜 Step1-6 に分割し、各タスク完了毎にビルド・テスト確認を行う。

---

## 0. Step1の位置づけと現状分析

### 0.1 Step0の棚卸し結果からの連携
Step0の棚卸し調査（`Phase4-Step0-Global-Member-Inventory.md`）により、以下が確定している：
1. `Global` クラス（`ScpUtil.cs` 内）に存在する 442 件の static メンバー中、**「プロファイル・入力設定」カテゴリに属するメンバーは 174 件**存在する。
2. `Global.` を参照している呼び出し元ファイルはプロジェクト全体で **80 ファイル**に及ぶ。
3. `IProfileSettingsService` は現在 `DS4Windows/DI/ServiceRegistration.cs` に `ProfileSettingsServicePlaceholder` として暫定登録されており、実体は未実装である。
4. DI 起動シーケンス上、`AppHost.CreateHost()` でサービスが登録され、UI および各機能から参照可能な基盤が整っている。

### 0.2 Step1の目的
- `ProfileSettingsServicePlaceholder` を、実際の設定保持・読書き・変更通知・既定値管理を行う本番実装クラス `ProfileSettingsService` へ置換する。
- `IProfileSettingsService` インターフェースを本番仕様に拡張・確定する。
- `Global` (in `ScpUtil.cs`) のプロファイル設定関連メンバーの実体を `ProfileSettingsService` に委譲し、`Global` 側は薄いシムとして残すことで呼び出し元 80 ファイルのビルド破損を完全に防止する。
- 単体テスト（`ProfileSettingsServiceTests`）を新設し、設定値の整合性・既定値・配列境界・カルチャを自動検証する。

### 0.3 4層モデル（UI層 / 実行時3層）との責務境界
全体計画書 §3 の 4 層モデルに従い、`IProfileSettingsService` の責務を厳格に限定する：
- **担当する責務**:
  - 各種プロファイル設定値（スティック設定、デッドゾーン、感度、ボタンマッピング定義、ジャイロ感度、ライトバー色、タッチパッド動作設定等）のメモリ上での保持と取得/更新。
  - 設定値変更時のイベント通知（`ProfileSettingChanged` 等）。
  - 設定値の既定値初期化。
- **担当しない責務（Step2以降または実行層の責務）**:
  - プロファイル XML ファイルの物理読込・保存・ファイルパス管理（→ **Step2: `IProfileRepository` の責務**）。
  - SpecialAction のデータ永続化・一覧管理（→ **Step3: `ISpecialActionRepository` の責務**）。
  - 仮想コントローラーへの出力送出、KBM 入力送出、実行指示のディスパッチ（→ **第3層: 信号・アクション実行層の責務**）。

---

## 1. 対象範囲と設計方針

### 1.1 `IProfileSettingsService` インターフェース設計
`IProfileSettingsService` はプロファイル設定値への型安全なアクセスを提供し、コントローラーインデックス（0〜7 または一時プロファイル用 8）ごとの設定を管理する。

主な責務メソッド・プロパティ構成案:
```csharp
namespace DS4Windows
{
    public interface IProfileSettingsService
    {
        // カルチャ情報
        CultureInfo ConfigDecimalCulture { get; }

        // スロット別プロファイル設定値の取得・設定
        // （ボタンマッピング、スティック、トリガー、ジャイロ、ライトバー、タッチパッド等）
        bool GetTouchpadActive(int deviceIndex);
        void SetTouchpadActive(int deviceIndex, bool value);

        bool GetUseTempProfile(int deviceIndex);
        void SetUseTempProfile(int deviceIndex, bool value);

        string GetTempProfileName(int deviceIndex);
        void SetTempProfileName(int deviceIndex, string value);

        bool GetTempProfileDistance(int deviceIndex);
        void SetTempProfileDistance(int deviceIndex, bool value);

        bool GetUseDInputOnly(int deviceIndex);
        void SetUseDInputOnly(int deviceIndex, bool value);

        bool GetLinkedProfileCheck(int deviceIndex);
        void SetLinkedProfileCheck(int deviceIndex, bool value);

        // マッピング・コントロール関連
        X360Controls[] GetDefaultButtonMapping();
        DS4Controls[] GetReverseX360ButtonMapping();

        // 設定値変更通知イベント
        event EventHandler<ProfileSettingChangedEventArgs> ProfileSettingChanged;

        // 既定値へのリセット / 初期化
        void ResetToDefaults(int deviceIndex);
        void ResetAllToDefaults();
    }
}
```

### 1.2 `ProfileSettingsService` 実装クラス設計
- `DS4Windows/Services/ProfileSettingsService.cs`（新規作成）。
- 内部にスロット毎（`TEST_PROFILE_ITEM_COUNT` = 9）の配列構造および設定バッキングストアを保持。
- スレッドセーフティ: 設定値の更新と読み取りにおけるレースコンディションを防ぐための同期機構（`lock` 等）。
- イベント発火: UI や他サービスが設定変更を検知できるよう変更イベントを発行。

### 1.3 `Global` (in `ScpUtil.cs`) シム（デリゲート）設計
`Global` の対象 static メンバーは、`AppHost.GetService<IProfileSettingsService>()`（または初期化時に設定されるインスタンス）へ委譲するシムとする。

```csharp
// Global (ScpUtil.cs) 側のシム例
public static bool[] touchpadActive
{
    get => ProfileSettingsServiceInstance.TouchpadActiveArray;
    set => ProfileSettingsServiceInstance.TouchpadActiveArray = value;
}
```
※既存コードの参照形態（配列直接アクセス `Global.touchpadActive[i]` やプロパティアクセス）を壊さないよう、バッキング配列またはインデクサ/アクセサを適切に公開する。

### 1.4 DI コンテナ登録方針
`DS4Windows/DI/ServiceRegistration.cs` 内の登録を更新：
```csharp
// 旧: services.AddSingleton<IProfileSettingsService, ProfileSettingsServicePlaceholder>();
// 新:
services.AddSingleton<IProfileSettingsService, ProfileSettingsService>();
```

### 1.5 カルチャと配列境界の維持
- `configFileDecimalCulture`: 必ず `CultureInfo("en-US")` を維持し、小数点のカンマ/ドット不整合を防ぐ。
- 配列長:
  - `TEST_PROFILE_ITEM_COUNT = 9` (スロット0〜3: 主コントローラ, 4〜7: 拡張スロット, 8: 一時/テストスロット)
  - `MAX_DS4_CONTROLLER_COUNT = 8`
  - 配列アクセス時のインデックス範囲外例外を防ぐ境界チェックを実装に内包する。

---

## 2. 成果物一覧

| ファイルパス | 変更種別 | 内容 |
|---|---|---|
| `DS4Windows/Services/IProfileSettingsService.cs` | 更新 | 正式インターフェース定義への拡張 |
| `DS4Windows/Services/ProfileSettingsService.cs` | 新規 | `IProfileSettingsService` 本番実装クラス |
| `DS4Windows/Services/ProfileSettingsServicePlaceholder.cs` | 削除/整理 | Placeholder を削除し、本番実装へ完全移行 |
| `DS4Windows/DI/ServiceRegistration.cs` | 更新 | `ProfileSettingsService` の Singleton 登録 |
| `DS4Windows/DS4Control/ScpUtil.cs` | 更新 | `Global` のプロファイル設定メンバーを新サービスへのシムへ置換（ピンポイント置換） |
| `DS4WindowsTests/ProfileSettingsServiceTests.cs` | 新規 | 設定値保持・配列境界・既定値・イベントの単体テスト |
| `docs-forDIMG/MadeByAgent/Phase4-Step1-Plan.md` | 新規 | 本計画書 |
| `docs-forDIMG/MadeByAgent/Phase4-Step1-Completion-Report.md` | 新規 | Step1完了報告書 |
| `docs-forDIMG/MadeByAgent/Phase4-Status.md` | 更新 | Step1進捗状況の更新 |

---

## 3. 作業手順（マイクロタスク分割）

### タスク Step1-1: `IProfileSettingsService` インターフェース定義の確定
- `DS4Windows/Services/IProfileSettingsService.cs` を整備し、プロファイル設定値の取得・設定・イベント・既定値初期化メソッドを定義する。
- 4層モデルの境界に基づき、ファイルI/Oやアクション実行のシグネチャを含めない。

### タスク Step1-2: `ProfileSettingsService` 実装クラスの作成
- `DS4Windows/Services/ProfileSettingsService.cs` を新規作成。
- スロット別配列、既定値設定、変更イベント発行、`en-US` カルチャ保持を実装。

### タスク Step1-3: DI コンテナ登録の更新
- `DS4Windows/DI/ServiceRegistration.cs` を更新し、`ProfileSettingsService` を Singleton 登録する。
- `ProfileSettingsServicePlaceholder.cs` を削除（または不要化）。

### タスク Step1-4: `Global` (in `ScpUtil.cs`) のピンポイントシム化
- `ScpUtil.cs` 内の `Global` クラスにおいて、プロファイル設定関連メンバーを新サービス委譲シムへピンポイント置換する。
- 既存の呼び出し元（80ファイル）の動作を壊さないよう、互換プロパティ／バッキング配列を維持。

### タスク Step1-5: 単体テスト作成と自動テスト実行
- `DS4WindowsTests/ProfileSettingsServiceTests.cs` を作成し、以下を検証：
  1. 既定値初期化の正確性
  2. スロット別設定値の取得・設定
  3. 配列境界（0〜8）の検証
  4. 設定変更イベントの発火確認
  5. `Global` シム経由での値読み書きの双方向整合性
- `DS4Windows.Actions.Tests` (31件) 及び `StandaloneTests` (13件) を含む全テストを実行し、全件通過を確認。

### タスク Step1-6: ビルド確認、進捗更新、完了報告書の作成
- `dotnet build DS4WindowsWPF.sln --nologo` を実行し、警告0・エラー0を確認。
- `Phase4-Status.md` を更新（Step1: 完了）。
- `Phase4-Step1-Completion-Report.md` を作成し、差分サマリ・テスト結果・次Stepへの引継ぎを記録。

---

## 4. リスクと回避策

| リスク | 該当タスク | 回避策 |
|---|---|---|
| 巨大ファイル `ScpUtil.cs` の編集によるコード欠損 | Step1-4 | ファイル全体を再生成せず、対象プロパティ・フィールド定義ブロックのみをピンポイント置換する。 |
| 配列インデックスのオフバイワン（8 vs 9） | Step1-2, Step1-4 | `TEST_PROFILE_ITEM_COUNT` (=9) と `MAX_DS4_CONTROLLER_COUNT` (=8) の使い分けを厳密に踏襲する。 |
| 小数点フォーマットによる設定値化け | Step1-2 | `configFileDecimalCulture = new CultureInfo("en-US")` をサービス内で厳格に保持・適用する。 |
| DIコンテナ解決前の早期アクセスによるNullReference | Step1-4 | `Global` シム内部で初期化前フォールバック（遅延解決または静的既定インスタンス）を保持し、起動直後のアクセスでも安全にする。 |
| メッセージ切れ・作業中断 | 全タスク | 各タスク（Step1-1〜1-6）単位で区切り、タスク毎に成果物スクリプトを実行・確認する。 |

---

## 5. 完了判定基準

- [ ] `IProfileSettingsService` が正式に定義され、ファイルI/Oや実行層の責務を含まない。
- [ ] `ProfileSettingsService` が実装され、Placeholder が置換されている。
- [ ] `DS4Windows/DI/ServiceRegistration.cs` にて `ProfileSettingsService` が登録され、`AppHost` から解決できる。
- [ ] `Global` (in `ScpUtil.cs`) のプロファイル設定関連メンバーがシム化され、既存の呼び出し元コードが無変更でコンパイル・動作する。
- [ ] 新設した `ProfileSettingsServiceTests` が全件成功する。
- [ ] 既存の `DS4Windows.Actions.Tests`（31件）および `StandaloneTests`（13件）がすべて通過する（回帰ゼロ）。
- [ ] ソリューション全体のビルド（`dotnet build DS4WindowsWPF.sln`）が警告0・エラー0で成功する。
- [ ] `Phase4-Status.md` が更新され、`Phase4-Step1-Completion-Report.md` が作成されている。

---

## 6. テスト計画

### 6.1 新設単体テスト (`ProfileSettingsServiceTests`)
- `Defaults_ShouldMatchInitialValues`: 初期状態の全設定値が旧 `Global` の初期値と一致すること。
- `SetAndGet_ShouldUpdateCorrectSlot`: 各スロット（0〜8）で独立して設定値が保持されること。
- `SettingChangedEvent_ShouldFire`: 設定値更新時に `ProfileSettingChanged` イベントが通知されること。
- `GlobalShim_ShouldSynchronizeWithService`: `Global.touchpadActive` 等のシム経由の操作がサービスインスタンスと完全に同期すること。

### 6.2 回帰テスト
- `dotnet test DS4WindowsTests/DS4Windows.Actions.Tests.csproj --nologo --no-restore` (31件成功確認)
- `dotnet test StandaloneTests/StandaloneTests.csproj --nologo --no-restore` (13件成功確認)

---

## 7. 次のアクション（Step2への引継ぎ）

Step1 完了後は **Phase4-Step2: `IProfileRepository` 分離** に着手する。
- Step1 で整備した `IProfileSettingsService` を利用し、プロファイル XML ファイルの物理読込・保存・ファイルパス管理およびプロファイル切り替えロジックを `IProfileRepository` / `ProfileRepository` として独立させる。
- Phase3 から引き継がれた `Mapping.cs` の `ApplyProfileDirect` / `RestoreProfileDirect` に残る `Program.rootHub` 依存の整理を進める。
