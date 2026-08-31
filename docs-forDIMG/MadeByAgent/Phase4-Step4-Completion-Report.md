# フェーズ4-Step4 完了報告書: 入力・出力・デバイス状態サービス

作成日: 2026-08-31
対象ブランチ: `For-DI-migration-work`
計画書: `docs-forDIMG/MadeByAgent/Phase4-Step4-Plan.md`
進捗管理表: `docs-forDIMG/MadeByAgent/Phase4-Status.md`

---

## 1. 実施概要

フェーズ4の第4ステップとして、`Global`（`ScpUtil.cs`）に集中していたデバイス状態管理および出力スロット・仮想コントローラー管理を独立した DI サービスとして分離する **`IDeviceStateService` および `IOutputSlotService` の実装化** を完了しました。

全体計画書（`DI-App-Wide-Migration-Plan.md` §3.3）で規定された **全体4層モデル（実行時3層 ＋ UI層）** に基づき、**第1層（入力監視層）** の物理デバイス状態および **第3層（信号出力層 3-a. 仮想コントローラー出力）** の出力スロット状態を、**第4層 4-c（設定／状態サービス）** へ安全に公開する DI サービス基盤を確立しました。

---

## 2. 成果物一覧と配置アーキテクチャ

資材のライフサイクル（DI永続資産 vs 移行過渡期シム）を明確に区別して整理・配置しました。

| ファイルパス | 種別 | ライフサイクル | 変更内容 |
|---|---|---|---|
| `DS4Windows/DI/IDeviceStateService.cs` | 新規 | **DI永続資産** | 第1層デバイス状態を第4層へ公開する契約インターフェース（名前空間: `DS4Windows.DI`） |
| `DS4Windows/DI/IOutputSlotService.cs` | 新規 | **DI永続資産** | 第3層 3-a 仮想出力スロット管理の契約インターフェース（名前空間: `DS4Windows.DI`） |
| `DS4Windows/DS4Control/Services/DeviceStateService.cs` | 新規 | **DI永続資産** | `IDeviceStateService` の本番実装クラス。スレッドセーフな内部配列 `_devices`、接続数カウント、変更イベント通知を実装 |
| `DS4Windows/DS4Control/Services/OutputSlotService.cs` | 新規 | **DI永続資産** | `IOutputSlotService` の本番実装クラス。`OutContType` 出力タイプ管理、仮想出力デバイス管理、変更イベント通知を実装 |
| `DS4Windows/DI/ServiceRegistration.cs` | 更新 | **DI永続資産** | `IDeviceStateService`, `IOutputSlotService` に対する Singleton 登録 |
| `DS4Windows/DS4Control/ScpUtil.cs` | 更新 | **過渡期シム** | `Global.DeviceStateServiceInstance` および `Global.OutputSlotServiceInstance` プロパティ（安全なフォールバック付き）を追加 |
| `DS4WindowsTests/DeviceStateServiceTests.cs` | 新規 | **テスト資産** | デバイス登録・取得、接続数カウント、変更イベント、境界外アクセス安全性を網羅する単体テスト |
| `DS4WindowsTests/OutputSlotServiceTests.cs` | 新規 | **テスト資産** | 既定出力タイプ、出力切替、プラグイン状態、変更イベント、境界外アクセス安全性を網羅する単体テスト |
| `docs-forDIMG/MadeByAgent/Phase4-Step4-Plan.md` | 新規 | ドキュメント | Step4 計画書（全体4層モデル正式定義準拠） |
| `docs-forDIMG/MadeByAgent/Phase4-Step4-Completion-Report.md` | 新規 | ドキュメント | 本完了報告書 |
| `docs-forDIMG/MadeByAgent/Phase4-Status.md` | 更新 | ドキュメント | 進捗ステータス更新（Step4完了） |

---

## 3. 設計・実装のポイント

### 3.1 全体4層モデルにおける責務境界と単一責任の原則（全体計画書 §3.3 準拠）
- **第1層: 入力監視層**
  - コントローラー実機の接続・切断、バッテリー、通信種別（BT/USB）の状態管理を `IDeviceStateService` を通じて第4層 4-c へ型安全に公開。
- **第2層: 信号変換層（拡張版）**
  - 信号マッピングや SpecialAction 判定等の変換責務と分離。
- **第3層: 信号出力層（拡張版） 3-a. 仮想コントローラー出力**
  - 仮想コントローラー（ViGEmBus: Xbox 360 / DS4）の出力スロット管理を `IOutputSlotService` を通じて第4層 4-c へ型安全に公開。
- **第4層: UI層（制御面） 4-c. 設定／状態サービス**
  - UI ViewModel がこれらサービスを DI 注入して購読・バインドできる基盤を確立。

### 3.2 安全なフォールバック機構とシム設計（ルール §2.1）
- `Global.DeviceStateServiceInstance` および `Global.OutputSlotServiceInstance` プロパティを新設：
  1. DI コンテナ初期化前や静的コンテキスト：静的フォールバックインスタンス（`fallbackDeviceStateService`, `fallbackOutputSlotService`）が自動稼働し `NullReferenceException` を完全防止。
  2. DI コンテナ起動後：`AppHost.GetService<T>()` を自動解決して Singleton インスタンスと完全に同期。
  3. 単体テスト時：明示的なモック/スタブの差し替えが可能。

### 3.3 完全な機能・互換性維持（ルール §2.2）
- 配列長境界: `MAX_SLOTS = 8` の境界チェックを徹底し、範囲外スロットへのアクセスでもクラッシュしない防御的実装。
- 正式型名準拠: 仮想コントローラー出力種別を `OutContType`（`None`, `X360`, `DS4`）に完全準拠。
- 出力デバイス基底型: `OutputDevice`（`DS4OutDevice`, `Xbox360OutDevice` の基底）による型安全なスロット管理。

---

## 4. テスト・検証結果

### 4.1 新設単体テスト
- **`DeviceStateServiceTests`**:
  - `InitialState_ShouldBeEmpty`: パス（初期状態の安全確認）
  - `SetDevice_ShouldUpdateSlotAndCount`: パス（スロット更新と接続台数カウント）
  - `DeviceStateChangedEvent_ShouldFire`: パス（デバイス状態変更イベント通知）
  - `OutOfBounds_ShouldBeHandledSafely`: パス（範囲外アクセスの安全性確認）
  - `GlobalShim_ShouldSynchronizeWithService`: パス（`Global` シムとの双方向同期確認）
- **`OutputSlotServiceTests`**:
  - `InitialState_ShouldHaveDefaultTypes`: パス（初期既定タイプ Xbox 360 の確認）
  - `SetOutputDeviceType_ShouldUpdateSlot`: パス（出力タイプ切替動作確認）
  - `OutputSlotChangedEvent_ShouldFire`: パス（出力スロット変更イベント通知）
  - `OutOfBounds_ShouldBeHandledSafely`: パス（範囲外アクセスの安全性確認）
  - `GlobalShim_ShouldSynchronizeWithService`: パス（`Global` シムとの双方向同期確認）

### 4.2 回帰テスト結果
- `DS4Windows.Actions.Tests`: **31 / 31 件 全件成功**（回帰ゼロ）
- `StandaloneTests`: **13 / 13 件 全件成功**（回帰ゼロ）

### 4.3 ソリューションビルド結果
- `dotnet build DS4WindowsWPF.sln --nologo`: **警告 0 件、エラー 0 件（完全成功）**

---

## 5. 次のステップ（Step5への引継ぎ事項）

Step4 でデバイス状態・出力スロットサービスが稼働したため、次は **Phase4-Step5: 環境・UI・通知サービス** に着手します。

### Step5 引継ぎ事項:
1. **環境・UI・通知サービスの分離**:
   - `Global`（`ScpUtil.cs`）に点在するファイルパス解決（`appdatapath` 等）、OS/システム環境情報（`runAtStartup`, `runMinimized` 等）、トースト通知管理、UI状態管理を `IPathService`, `IEnvironmentService`, `INotificationService` 等として分離・DI化する。
2. **Step6（バックエンド完成・実機CP2）に向けた準備**:
   - Step5 完了後、Step6 で `AppHost` の起動シーケンスを一本化し、第2回実機検証（Checkpoint 2）を実施する。
