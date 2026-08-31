# フェーズ4-Step2 完了報告書: IProfileRepository 分離

作成日: 2026-08-31
対象ブランチ: `For-DI-migration-work`
計画書: `docs-forDIMG/MadeByAgent/Phase4-Step2-Plan.md`
進捗管理表: `docs-forDIMG/MadeByAgent/Phase4-Status.md`

---

## 1. 実施概要

フェーズ4の第2ステップとして、プロファイル XML ファイルの物理読込・保存・パス解決・一覧取得およびプロファイル切替ロジックを独立した DI サービスとして分離する **`IProfileRepository` の実装化** を完了しました。

Step1 で構築した `IProfileSettingsService` と連携し、メモリ上の設定値とファイル永続化の責務を綺麗に分離（単一責任の原則）するとともに、Phase3 から引き継がれた `ApplyProfileDirect` / `RestoreProfileDirect` の依存構造を整理しました。

---

## 2. 成果物一覧と配置アーキテクチャ

本ステップでも、資材のライフサイクル（DI永続資産 vs 移行過渡期シム）を明確に区別して整理・配置しました。

| ファイルパス | 種別 | ライフサイクル | 変更内容 |
|---|---|---|---|
| `DS4Windows/DI/IProfileRepository.cs` | 新規 | **DI永続資産** | プロファイル永続化・切替の契約インターフェース（名前空間: `DS4Windows.DI`） |
| `DS4Windows/DS4Control/Services/ProfileRepository.cs` | 新規 | **DI永続資産** | `IProfileRepository` の本番実装クラス。`IProfileSettingsService` をコンストラクタ注入し、XML 読み書き・パス解決・排他制御を実装 |
| `DS4Windows/DI/ServiceRegistration.cs` | 更新 | **DI永続資産** | `IProfileRepository` に対する `ProfileRepository` の Singleton 登録 |
| `DS4Windows/DS4Control/ScpUtil.cs` | 更新 | **過渡期シム** | `Global.ProfileRepositoryInstance` プロパティ（安全なフォールバック付き）を追加 |
| `DS4Windows/DS4Control/Mapping.cs` | 確認 | **過渡期シム整理** | `ApplyProfileDirect` / `RestoreProfileDirect` の依存関係を確認・整理 |
| `DS4WindowsTests/ProfileRepositoryTests.cs` | 新規 | **テスト資産** | パス解決、XML 拡張子補完、デフォルトプロファイルリセット、一時プロファイル切替、シム同期を網羅する単体テスト |
| `docs-forDIMG/MadeByAgent/Phase4-Step2-Plan.md` | 新規 | ドキュメント | Step2 計画書 |
| `docs-forDIMG/MadeByAgent/Phase4-Step2-Completion-Report.md` | 新規 | ドキュメント | 本完了報告書 |
| `docs-forDIMG/MadeByAgent/Phase4-Status.md` | 更新 | ドキュメント | 進捗ステータス更新（Step2完了） |

---

## 3. 設計・実装のポイント

### 3.1 4層モデルの責務境界と単一責任の原則
- **`IProfileSettingsService`（Step1）**: メモリ上のプロファイル設定値の保持、ゲッター/セッター、変更通知イベント、既定値初期化を担当。
- **`IProfileRepository`（Step2）**: プロファイル XML ファイルの物理読込・保存、ファイルパス解決（`appdatapath\Profiles`）、プロファイル一覧取得、一時プロファイル切替を担当。
- 設定値管理とファイル I/O を明確に分離し、密結合を解消しました。

### 3.2 安全なフォールバック機構とシム設計（ルール §2.1）
- `Global.ProfileRepositoryInstance` プロパティを新設：
  1. DI コンテナ初期化前や静的コンテキスト：静的フォールバックインスタンス（`fallbackProfileRepository`）が自動稼働し `NullReferenceException` を完全防止。
  2. DI コンテナ起動後：`AppHost.GetService<IProfileRepository>()` を自動解決して Singleton インスタンスと完全に同期。
  3. 単体テスト時：明示的なモック/スタブの差し替えが可能。

### 3.3 完全な機能・互換性維持（ルール §2.2）
- パス解決: `Global.appdatapath`（またはフォールバックのベースディレクトリ）配下の `Profiles` ディレクトリを安全に自動生成・解決。
- 拡張子補完: `.xml` 拡張子の有無にかかわらず安全に正規化してパスを解決。
- スレッドセーフティ: ファイル I/O 操作中の競合を防ぐための `lock (_fileLock)` 排他制御。

---

## 4. テスト・検証結果

### 4.1 新設単体テスト (`ProfileRepositoryTests`)
- `ProfilesPath_ShouldReturnValidPath`: パス（Profiles ディレクトリパスが正しく解決されることを確認）
- `GetProfilePath_ShouldAppendXmlExtension`: パス（`.xml` 拡張子の正規化・補完動作を確認）
- `LoadDefaultProfile_ShouldResetSlotSettings`: パス（デフォルトプロファイル読込時に設定サービスがリセットされることを確認）
- `ApplyAndRestoreProfileDirect_ShouldUpdateSettingsService`: パス（一時プロファイル適用と復元が設定サービスと連動することを確認）
- `GlobalShim_ShouldSynchronizeWithRepository`: パス（`Global.ProfileRepositoryInstance` 経由の変更がリポジトリ・設定サービスと完全に双方向同期することを確認）

### 4.2 回帰テスト結果
- `DS4Windows.Actions.Tests`: **31 / 31 件 全件成功**（回帰ゼロ）
- `StandaloneTests`: **13 / 13 件 全件成功**（回帰ゼロ）

### 4.3 ソリューションビルド結果
- `dotnet build DS4WindowsWPF.sln --nologo`: **警告 0 件、エラー 0 件（完全成功）**

---

## 5. 次のステップ（Step3への引継ぎ事項）

Step2 で `IProfileRepository` が稼働したため、次は **Phase4-Step3: `ISpecialActionRepository` 分離** に着手します。

### Step3 引継ぎ事項:
1. **SpecialAction 管理の分離**:
   - `Global`（`ScpUtil.cs`）および `ControlService.cs` に点在する SpecialAction（マクロ、プロファイル切替、バッテリー確認、プログラム起動等のカスタムアクション）のデータ永続化（Actions.xml の読込・保存）および一覧管理を `ISpecialActionRepository` / `SpecialActionRepository` として独立させる。
2. **`IProfileSettingsService` / `IProfileRepository` との連携**:
   - SpecialAction の実行トリガーや関連プロファイルとの連携を DI 経由でクリーンに統合する。
