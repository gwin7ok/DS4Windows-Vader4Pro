# フェーズ4-Step5 完了報告書: 環境・UI・通知サービス

作成日: 2026-08-31
対象ブランチ: `For-DI-migration-work`
計画書: `docs-forDIMG/MadeByAgent/Phase4-Step5-Plan.md`
進捗管理表: `docs-forDIMG/MadeByAgent/Phase4-Status.md`

---

## 1. 実施概要

フェーズ4の第5ステップとして、`Global`（`ScpUtil.cs`）に集中していたファイルパス解決、OS/システム起動環境情報、ウィンドウ幾何情報、トースト通知管理を独立した DI サービスとして分離する **`IPathService`、`IEnvironmentService`、`INotificationService` の実装化** を完了しました。

全体計画書（`DI-App-Wide-Migration-Plan.md` §3.3）で規定された **全体4層モデル（実行時3層 ＋ UI層）** に基づき、これら 3 つのサービスを **第4層: UI層（制御面） 4-c. 設定／状態サービス** として確立しました。これにより、Step6（Composition Root 一本化）および Step7〜9（ViewModel DI 化）に向けて、UI が必要とするバックエンドの基盤サービス群が出揃いました。

---

## 2. 成果物一覧と配置アーキテクチャ

資材のライフサイクル（DI永続資産 vs 移行過渡期シム）を明確に区別して整理・配置しました。

| ファイルパス | 種別 | ライフサイクル | 変更内容 |
|---|---|---|---|
| `DS4Windows/DI/IPathService.cs` | 新規 | **DI永続資産** | 第4層 4-c パス解決の契約インターフェース（名前空間: `DS4Windows.DI`） |
| `DS4Windows/DI/IEnvironmentService.cs` | 新規 | **DI永続資産** | 第4層 4-c 環境・起動設定の契約インターフェース（名前空間: `DS4Windows.DI`） |
| `DS4Windows/DI/INotificationService.cs` | 新規 | **DI永続資産** | 第4層 4-c 通知管理・イベント通知の契約インターフェース（名前空間: `DS4Windows.DI`） |
| `DS4Windows/DS4Control/Services/PathService.cs` | 新規 | **DI永続資産** | `IPathService` の本番実装クラス。パス解決、フォールバック、拡張子正規化を実装 |
| `DS4Windows/DS4Control/Services/EnvironmentService.cs` | 新規 | **DI永続資産** | `IEnvironmentService` の本番実装クラス。ウィンドウ幾何情報、起動設定、変更通知を実装 |
| `DS4Windows/DS4Control/Services/AppNotificationService.cs` | 新規 | **DI永続資産** | `INotificationService` の本番実装クラス。静的クラス競合を回避し通知イベント発行を実装 |
| `DS4Windows/DI/ServiceRegistration.cs` | 更新 | **DI永続資産** | `IPathService`, `IEnvironmentService`, `INotificationService` に対する Singleton 登録 |
| `DS4Windows/DS4Control/ScpUtil.cs` | 更新 | **過渡期シム** | `Global.PathServiceInstance`, `Global.EnvironmentServiceInstance`, `Global.NotificationServiceInstance` プロパティ（安全なフォールバック付き）を追加 |
| `DS4WindowsTests/PathServiceTests.cs` | 新規 | **テスト資産** | パス解決、Profiles パス結合、拡張子正規化、シム同期を網羅する単体テスト |
| `DS4WindowsTests/EnvironmentServiceTests.cs` | 新規 | **テスト資産** | ウィンドウ既定幾何情報、設定変更イベント、シム同期を網羅する単体テスト |
| `DS4WindowsTests/NotificationServiceTests.cs` | 新規 | **テスト資産** | 通知有効/無効、イベント発行、シム同期を網羅する単体テスト |
| `docs-forDIMG/MadeByAgent/Phase4-Step5-Plan.md` | 新規 | ドキュメント | Step5 計画書（全体4層モデル正式定義準拠） |
| `docs-forDIMG/MadeByAgent/Phase4-Step5-Completion-Report.md` | 新規 | ドキュメント | 本完了報告書 |
| `docs-forDIMG/MadeByAgent/Phase4-Status.md` | 更新 | ドキュメント | 進捗ステータス更新（Step5完了） |

---

## 3. 設計・実装のポイント

### 3.1 全体4層モデルにおける責務境界と単一責任の原則（全体計画書 §3.3 準拠）
- **第4層: UI層（制御面） 4-c. 設定／状態サービス**
  - `IPathService`: アプリケーションの物理ファイルパス解決・ディレクトリ管理を担当。
  - `IEnvironmentService`: OS起動時実行、最小化起動、ウィンドウ位置・サイズ、言語設定を担当。
  - `INotificationService`: トースト通知、タスクバー点滅、通知イベント発行を担当。
- 各責務を単一責任の原則に基づき個別のサービスへ明確に分離し、UI ViewModel から疎結合に利用できるように設計。

### 3.2 安全なフォールバック機構とシム設計（ルール §2.1）
- `Global.PathServiceInstance`, `Global.EnvironmentServiceInstance`, `Global.NotificationServiceInstance` プロパティを新設：
  1. DI コンテナ初期化前や静的コンテキスト：静的フォールバックインスタンス（`fallbackPathService`, `fallbackEnvironmentService`, `fallbackNotificationService`）が自動稼働し `NullReferenceException` を完全防止。
  2. DI コンテナ起動後：`AppHost.GetService<T>()` を自動解決して Singleton インスタンスと完全に同期。
  3. 単体テスト時：明示的なモック/スタブの差し替えが可能。

### 3.3 既存静的クラスとの競合回避
- 既存コードに存在していた静的クラス `NotificationService` との型名衝突（CS0718）を回避するため、DI 実装クラス名を **`AppNotificationService`** として分離・解決。

---

## 4. テスト・検証結果

### 4.1 新設単体テスト
- **`PathServiceTests`**:
  - `AppDataPath_ShouldResolveValidDirectory`: パス（AppData パス解決確認）
  - `ProfilesPath_ShouldCombineWithAppDataPath`: パス（Profiles パス結合確認）
  - `GetProfilePath_ShouldNormalizeXmlExtension`: パス（.xml 拡張子正規化確認）
  - `GlobalShim_ShouldSynchronizeWithService`: パス（シム同期確認）
- **`EnvironmentServiceTests`**:
  - `Defaults_ShouldMatchExpected`: パス（ウィンドウ既定幅 782, 高さ 550 等の確認）
  - `MutatingProperty_ShouldFireEnvironmentSettingChanged`: パス（設定変更イベント発火確認）
  - `GlobalShim_ShouldSynchronizeWithService`: パス（シム同期確認）
- **`NotificationServiceTests`**:
  - `Defaults_ShouldHaveNotificationsEnabled`: パス（初期通知有効確認）
  - `SendNotification_ShouldFireNotificationTriggered_WhenEnabled`: パス（通知イベント発火確認）
  - `SendNotification_ShouldNotFire_WhenDisabled`: パス（通知無効時の非発火確認）
  - `GlobalShim_ShouldSynchronizeWithService`: パス（シム同期確認）

### 4.2 回帰テスト結果
- `DS4Windows.Actions.Tests`: **31 / 31 件 全件成功**（回帰ゼロ）
- `StandaloneTests`: **13 / 13 件 全件成功**（回帰ゼロ）

### 4.3 ソリューションビルド結果
- `dotnet build DS4WindowsWPF.sln --nologo`: **警告 0 件、エラー 0 件（完全成功）**

---

## 5. 次のステップ（Step6への引継ぎ事項）

Step5 で環境・UI・通知サービスが稼働し、バックエンドの全主要サービス（設定、プロファイル、Action、デバイス状態、出力スロット、パス、環境、通知）が出揃いました。
次は **Phase4-Step6: Composition Root 一本化 & 実機検証 Checkpoint 2** に着手します。

### Step6 引継ぎ事項:
1. **DIコンテナ二重起動の解消・起動シーケンス一本化**:
   - `App.xaml.cs` と `AppHost.cs` の間で発生している二重コンテナ構造を解消し、`AppHost.CreateHost()` を唯一の Composition Root として一本化する。
2. **実機動作検証（Checkpoint 2）の実施**:
   - 全バックエンドサービスの DI 化および起動シーケンス一本化が完了した節目として、実機コントローラーを用いた起動シーケンス・HID通信・仮想コントローラー出力・UAC昇格の動作検証を実施する。
