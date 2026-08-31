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
| Step 3 | ISpecialActionRepository 分離 | **未着手 (次)** | - | SpecialAction 管理・永続化の分離 |
| Step 4 | 入力・出力・デバイス状態サービス | 未着手 | - | Input/Output/DeviceState 各サービスの分離 |
| Step 5 | 環境・UI・通知サービス | 未着手 | - | Path/Environment/UI/Notification 各サービスの分離 |
| Step 6 | Composition Root 一本化 | 未着手 | - | DIコンテナ二重起動解消・一本化 |
| Step 7 | ViewModel DI 移行 (Pattern A) | 未着手 | - | 引数なし ViewModel の DI 登録・移行 |
| Step 8 | ViewModel DI 移行 (Pattern B) | 未着手 | - | 共有依存 ViewModel の DI 登録・移行 |
| Step 9 | ViewModel DI 移行 (Pattern C) | 未着手 | - | 実行時引数付き ViewModel の Factory 移行 |
| Step 10 | Phase3 引継ぎ再確認・シム整理 | 未着手 | - | 残存シムの監査と全体健全性確認 |

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
- **ビルド・テスト検証**: 全プロジェクトビルド警告0・エラー0、既存テスト（31件/13件）および新設テスト全件成功。
