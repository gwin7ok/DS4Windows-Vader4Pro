# フェーズ4 進捗管理表: Global 分割と ViewModel DI 化

最終更新日: 2026-08-31
対象ブランチ: `For-DI-migration-work`
全体計画書: `docs-forDIMG/DI-App-Wide-Migration-Plan.md`
Phase4計画書: `docs-forDIMG/MadeByAgent/Phase4-Plan.md`

---

## 1. 全体進捗サマリ

| ステップ | 名称 | 状態 | 完了日 | 成果物・備考 |
|---|---|---|---|---|
| Step 0 | 現状棚卸し・基準テスト | **完了** | 2026-08-31 | `Phase4-Step0-Plan.md`, `Phase4-Step0-Completion-Report.md`, Globalメンバー442件/ViewModel生成29件棚卸し |
| Step 1 | IProfileSettingsService 実装化 | **完了** | 2026-08-31 | `IProfileSettingsService.cs`, `ProfileSettingsService.cs`, DI登録, Globalシム, `ProfileSettingsServiceTests.cs` |
| Step 2 | IProfileRepository 分離 | **完了** | 2026-08-31 | `IProfileRepository.cs`, `ProfileRepository.cs`, DI登録, Globalシム, `ProfileRepositoryTests.cs` |
| Step 3 | ISpecialActionRepository 分離 | **完了** | 2026-08-31 | `ISpecialActionRepository.cs`, `SpecialActionRepository.cs`, DI登録, Globalシム, `SpecialActionRepositoryTests.cs`, **実機検証CP1全件合格** |
| **実機CP1** | **データ中核層 実機検証** | **完了** | 2026-08-31 | `Phase4-Step3-RealDevice-Verification-Checklist.md` (全12項目 ○ 合格) |
| Step 4 | 入力・出力・デバイス状態サービス | **未着手 (次)** | - | Input/Output/DeviceState 各サービスの分離 |
| Step 5 | 環境・UI・通知サービス | 未着手 | - | Path/Environment/UI/Notification 各サービスの分離 |
| Step 6 | Composition Root 一本化 | 未着手 | - | DIコンテナ二重起動解消・一本化 |
| **実機CP2** | **全バックエンドDI＋Root一本化 実機検証** | 未着手 | - | バックエンド完成・全サービス結合実機検証（Step6完了時） |
| Step 7 | ViewModel DI 移行 (Pattern A) | 未着手 | - | 引数なし ViewModel の DI 登録・移行 |
| Step 8 | ViewModel DI 移行 (Pattern B) | 未着手 | - | 共有依存 ViewModel の DI 登録・移行 |
| Step 9 | ViewModel DI 移行 (Pattern C) | 未着手 | - | 実行時引数付き ViewModel の Factory 移行 |
| **実機CP3** | **全ViewModel DI移行完了 実機検証** | 未着手 | - | 全画面 UI 結合・ViewModel 直接 new 全廃実機検証（Step9完了時） |
| Step 10 | Phase3 引継ぎ再確認・シム整理 | 未着手 | - | 残存シムの監査と全体健全性確認 |
| **実機CP4** | **Phase4 最終総合 E2E 実機検証** | 未着手 | - | 残存シム整理後・フェーズ4完了総合実機検証（Step10完了時） |

---

## 2. 詳細ステータス

### Step 1: IProfileSettingsService 実装化 (完了)
- **DI契約 (永続資産)**: `DS4Windows/DI/IProfileSettingsService.cs`（名前空間 `DS4Windows.DI`）を本番仕様に拡張。
- **サービス実装 (永続資産)**: `DS4Windows/DS4Control/Services/ProfileSettingsService.cs`（スロット別配列、既定値、変更イベント、排他制御）。
- **DI登録 (永続資産)**: `DS4Windows/DI/ServiceRegistration.cs` にて `ProfileSettingsService` を Singleton 登録。
- **過渡期シム (Strangler Fig)**: `DS4Windows/DS4Control/ScpUtil.cs` 内の `Global` プロパティを `ProfileSettingsServiceInstance` へのシム委譲へピンポイント置換。
- **単体テスト**: `DS4WindowsTests/ProfileSettingsServiceTests.cs`（全件通過、回帰ゼロ）。

### Step 2: IProfileRepository 分離 (完了)
- **DI契約 (永続資産)**: `DS4Windows/DI/IProfileRepository.cs`（名前空間 `DS4Windows.DI`）を新規作成。プロファイル XML 入出力、パス解決、一覧取得、切替（`ApplyProfileDirect` / `RestoreProfileDirect`）を定義。
- **サービス実装 (永続資産)**: `DS4Windows/DS4Control/Services/ProfileRepository.cs`（`IProfileSettingsService` をコンストラクタ注入、スレッドセーフなファイル操作、プロファイル切替ロジック）。
- **DI登録 (永続資産)**: `DS4Windows/DI/ServiceRegistration.cs` にて `ProfileRepository` を Singleton 登録。
- **過渡期シム (Strangler Fig)**: `DS4Windows/DS4Control/ScpUtil.cs` に `Global.ProfileRepositoryInstance` プロパティ（安全なフォールバック付き）を追加。
- **単体テスト**: `DS4WindowsTests/ProfileRepositoryTests.cs`（全件通過、回帰ゼロ）。

### Step 3: ISpecialActionRepository 分離 & 実機検証CP1 (完了)
- **DI契約 (永続資産)**: `DS4Windows/DI/ISpecialActionRepository.cs`（名前空間 `DS4Windows.DI`）を新規作成。SpecialAction の XML 永続化・CRUD・変更通知を定義。
- **サービス実装 (永続資産)**: `DS4Windows/DS4Control/Services/SpecialActionRepository.cs`（スレッドセーフな CRUD 操作、XML 永続化、`ActionsChanged` イベント通知）。
- **DI登録 (永続資産)**: `DS4Windows/DI/ServiceRegistration.cs` にて `SpecialActionRepository` を Singleton 登録。
- **過渡期シム (Strangler Fig)**: `DS4Windows/DS4Control/ScpUtil.cs` に `Global.SpecialActionRepositoryInstance` プロパティ（安全なフォールバック付き）を追加。
- **単体テスト**: `DS4WindowsTests/SpecialActionRepositoryTests.cs`（全件通過、回帰ゼロ）。
- **実機動作検証 (Checkpoint 1)**: `Phase4-Step3-RealDevice-Verification-Checklist.md` に基づき、実機コントローラー・UI・物理 XML ファイル（`Profiles/*.xml`, `Actions.xml`）の結合動作を検証。**全12項目すべて ○（正常動作）で合格**。
