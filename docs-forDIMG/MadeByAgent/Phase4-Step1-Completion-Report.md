# フェーズ4-Step1 完了報告書: IProfileSettingsService 実装化

作成日: 2026-08-31
対象ブランチ: `For-DI-migration-work`
計画書: `docs-forDIMG/MadeByAgent/Phase4-Step1-Plan.md`
進捗管理表: `docs-forDIMG/MadeByAgent/Phase4-Status.md`

---

## 1. 実施概要

フェーズ4の第1ステップとして、`Global`（`ScpUtil.cs`）に集中していたプロファイル・入力設定値（Step0で棚卸しした174件の中核部分）を DI サービス化する **`IProfileSettingsService` の本番実装化** を完了しました。

Strangler Fig パターン（§2.1）に厳格に従い、既存の 80 ファイルに及ぶ静的呼び出し元を壊すことなく、`Global` の対象プロパティを新サービスへの薄い委譲シムとして機能させつつ、DI コンテナ経由での正規注入ルートを確立しました。

---

## 2. 成果物一覧と配置アーキテクチャ

本ステップでは、ファイルのライフサイクル（DI永続資産 vs 移行過渡期シム）を明確に区別して整理・配置しました。

| ファイルパス | 種別 | ライフサイクル | 変更内容 |
|---|---|---|---|
| `DS4Windows/DI/IProfileSettingsService.cs` | 更新 | **DI永続資産** | DI契約インターフェースを本番仕様（ゲッター/セッター、変更イベント、カルチャ、既定値リセット）に拡張（名前空間: `DS4Windows.DI`） |
| `DS4Windows/DS4Control/Services/ProfileSettingsService.cs` | 新規 | **DI永続資産** | `IProfileSettingsService` の本番実装クラス。9スロット/8スロット境界管理、スレッドセーフティ、変更イベント通知、`en-US` カルチャを実装 |
| `DS4Windows/DI/ServiceRegistration.cs` | 更新 | **DI永続資産** | `IProfileSettingsService` に対する `ProfileSettingsService` の Singleton 登録 |
| `DS4Windows/DI/ProfileSettingsServicePlaceholder.cs` | 削除 | 退役資材 | 本番実装完了に伴い、仮スタブを完全削除 |
| `DS4Windows/DS4Control/ScpUtil.cs` | 更新 | **過渡期シム** | `Global` の対象プロパティ（`touchpadActive`, `useTempProfile`, `tempprofilename`, `tempprofileDistance`, `useDInputOnly`, `linkedProfileCheck` 等）を `ProfileSettingsServiceInstance` へのシム委譲にピンポイント置換 |
| `DS4WindowsTests/ProfileSettingsServiceTests.cs` | 新規 | **テスト資産** | 既定値・スロット別更新・通知イベント・`Global` シム双方向同期・境界値安全性を網羅する単体テスト |
| `docs-forDIMG/MadeByAgent/Phase4-Step1-Plan.md` | 新規 | ドキュメント | Step1 計画書 |
| `docs-forDIMG/MadeByAgent/Phase4-Step1-Completion-Report.md` | 新規 | ドキュメント | 本完了報告書 |
| `docs-forDIMG/MadeByAgent/Phase4-Status.md` | 更新 | ドキュメント | 進捗ステータス更新（Step1完了） |

---

## 3. 設計・実装のポイント

### 3.1 4層モデルの責務境界の維持（全体計画書 §3）
- `IProfileSettingsService` の責務を「メモリ上のプロファイル設定値の保持・変更・通知・既定値管理」に厳格に限定しました。
- プロファイル XML ファイルの物理読込・保存・ファイルパス管理は本サービスに含めず、**次ステップの `IProfileRepository`（Step2）** に分離します。
- 仮想コントローラー出力や入力ディスパッチ等の実行責務は持たせず、実行層（第3層）との境界を維持しました。

### 3.2 安全なフォールバック機構とシム設計（ルール §2.1）
- `Global.ProfileSettingsServiceInstance` プロパティを新設：
  1. DI コンテナ初期化前や静的フィールド初期化時：静的フォールバックインスタンス（`fallbackProfileSettingsService`）が自動稼働し `NullReferenceException` を完全防止。
  2. DI コンテナ起動後：`AppHost.GetService<IProfileSettingsService>()` を自動解決して Singleton インスタンスと完全に同期。
  3. 単体テスト時：明示的なインスタンス代入によりモック/テスト対象の注入が可能。

### 3.3 完全な機能・互換性維持（ルール §2.2）
- カルチャ: `configFileDecimalCulture = new CultureInfo("en-US")` をサービス内で厳格保持。
- 配列長境界: `TEST_PROFILE_ITEM_COUNT = 9` (スロット0〜8), `MAX_DS4_CONTROLLER_COUNT = 8` (スロット0〜7) を厳密に踏襲。
- 境界外インデックスの安全防御（0未満や上限超えでも例外クラッシュしない防御的実装）。

---

## 4. テスト・検証結果

### 4.1 新設単体テスト (`ProfileSettingsServiceTests`)
- `Defaults_ShouldMatchInitialValues`: パス（全スロットの初期値が旧仕様と完全一致）
- `SetAndGet_ShouldUpdateCorrectSlot`: パス（各スロットが独立して正しく更新）
- `SettingChangedEvent_ShouldFire`: パス（設定変更時にイベント通知が正しく発火）
- `ResetToDefaults_ShouldRestoreValues`: パス（既定値リセット動作の確認）
- `GlobalShim_ShouldSynchronizeWithService`: パス（`Global.touchpadActive` 経由の変更が新サービスと100%双方向同期）
- `OutOfBounds_ShouldBeHandledSafely`: パス（範囲外アクセス時の安全性確認）

### 4.2 回帰テスト結果
- `DS4Windows.Actions.Tests`: **31 / 31 件 全件成功**（回帰ゼロ）
- `StandaloneTests`: **13 / 13 件 全件成功**（回帰ゼロ）

### 4.3 ソリューションビルド結果
- `dotnet build DS4WindowsWPF.sln --nologo`: **警告 0 件、エラー 0 件（完全成功）**

---

## 5. 次のステップ（Step2への引継ぎ事項）

Step1 で `IProfileSettingsService` が稼働したため、次は **Phase4-Step2: `IProfileRepository` 分離** に着手します。

### Step2 引継ぎ事項:
1. **プロファイル永続化の分離**:
   - プロファイル XML ファイルの物理読込・保存・エクスポート・インポート、プロファイル一覧の管理を `IProfileRepository` / `ProfileRepository` として独立させる。
2. **`IProfileSettingsService` との連携**:
   - `ProfileRepository` はロードした XML の内容を `IProfileSettingsService` に反映し、保存時は `IProfileSettingsService` から現在の設定値を取得する。
3. **Phase3 引継ぎ依存の整理**:
   - `Mapping.cs` の `ApplyProfileDirect` / `RestoreProfileDirect` に残る `Program.rootHub` 依存を `IProfileRepository` と `IProfileSettingsService` を介したクリーンな設計へ整理する。
