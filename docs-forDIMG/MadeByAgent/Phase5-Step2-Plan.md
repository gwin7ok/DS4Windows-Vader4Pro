# フェーズ5-Step2 計画書: プロファイル XML 読込・保存の責務分離

作成日: 2026-09-02（改訂日: 2026-09-03）
対象ブランチ: `For-DI-migration-work`
前提ドキュメント:
- `docs-forDIMG/DI-App-Wide-Migration-Plan.md`（全体計画書・全体4層モデル定義）
- `docs-forDIMG/MadeByAgent/Phase5-Plan.md` §2, §3 Step2（Phase5詳細計画書）
- `docs-forDIMG/MadeByAgent/Phase5-Status.md`（Phase5進捗管理）
- `docs-forDIMG/MadeByAgent/Phase5-Step1-legacy-delegation-audit-report.md`（Step1監査結果。本Stepの対象根拠）
- `docs-forDIMG/MadeByAgent/Phase4-Step10-2-C-5-3-Nested-Legacy-Audit-Report.md`（Phase4基準監査）
- `.github/copilot-instructions.md`（エージェント作業ルール）

---

## ルール確認（作業開始前に毎回読む）

- **§2.1 フォールバック実装・シム維持の原則**:
  - 古い経路（`Global.LoadProfile`／`Global.SaveProfile`）は、新しいDI経由の実装が完成し動作確認が取れるまで削除しない。新旧を同時に複数経路実装することはしない。
- **§2.2 現在の機能の完全維持 (No Feature Drop)**:
  - プロファイルのロード順、既定値、欠落設定時のフォールバック、`tempProfileDistance`／`loggedInvalidActions` 等の付随状態管理を100%維持する。
- **§2.3 ログ出力の厳格な維持**:
  - `AppLogger.LogToGui`／`AppLogger.LogTrace`／`AppLogger.LogDebug` 等、既存のログ出力とログレベルを維持する。
- **§3.1 DI (Dependency Injection) の実装**:
  - コンテナ登録は `DS4Windows/DI/ServiceRegistration.cs` に行う。インターフェース名には `I` プレフィックスを付ける。
- **§3.2 巨大ファイルの編集方針**:
  - `ScpUtil.cs`（`Global` クラス・`BackingStore` クラス）はファイル全体を再生成せず、対象メソッドのみをピンポイントで置換する。

---

## 0. Step2の位置づけと現状分析

### 0.1 Step1監査結果に基づく対象範囲
`Phase5-Step1-legacy-delegation-audit-report.md` §2 表の#7（`IProfileRepository` → `ProfileRepository`）に基づき、以下を対象とする。

- `ProfileRepository.LoadProfile(deviceIndex, profileName)` 内部の `Global.LoadProfile(deviceIndex, false, control, false)` 呼び出し
- `ProfileRepository.SaveProfile(deviceIndex, profileName)` 内部の `Global.SaveProfile(deviceIndex, profileName)` 呼び出し
- `ProfileRepository.ProfilesPath` 内部の `Global.appdatapath` 直接参照

### 0.2 現状のコード構造（GitHub実コード確認済み）
`Global.LoadProfile`／`Global.SaveProfile`（`ScpUtil.cs`）は、単純な委譲ではなく以下の2種類の処理が1メソッド内に混在している。

1. **XML実I/O**: `BackingStore`（`m_Config`）インスタンスメソッドである `m_Config.LoadProfile(device, launchprogram, control, path, xinputChange, postLoad)` および `m_Config.SaveProfile(device, proName)` への委譲。実際のXMLパース・ノード生成・ファイル書き込みはここで行われる（巨大メソッドのため本Stepでは内部を変更しない）。
2. **状態調整ロジック**: `Global.loggedInvalidActions.Clear()`、`ProfileSettingsServiceInstance.SetTempProfileName`／`SetUseTempProfile`、`tempprofileDistance[device]` のリセットなど、XMLパースとは独立した付随状態の後処理。

`ProfileRepository` は現在、この2種類が混在した `Global.LoadProfile`／`Global.SaveProfile` を単一の呼び出しとして利用しており、「XMLパース」と「設定値反映（状態調整）」の境界が `ProfileRepository` の外（`Global`）に隠れている。

### 0.3 全体4層モデルにおける位置づけ
`DI-App-Wide-Migration-Plan.md` の4層モデルにおいて、`ProfileRepository` は **第4層 4-c 設定・プロファイル・アクション・環境・通知サービス** に属する。本Stepはこの層内で、XML永続化（データアクセス）と状態調整（アプリケーションロジック）の責務を明確に分離する。

---

## 1. 設計方針とアーキテクチャ

### 1.1 `IProfileXmlStore` インターフェース設計（新規、第4層 4-c）
XML実I/Oと状態調整を明確に分離するため、`BackingStore` への薄いラッパーとして契約を新設する。`DS4Windows/DI/IProfileXmlStore.cs`（名前空間 `DS4Windows.DI`）に定義する。

#### 【仕様調整: SaveProfileXml の戻り値を bool に統一】
Step4（結果・通知の統一）において、保存成否を呼び出し元へ伝播させログやUI通知を行う設計となっている。手戻りを防ぐため、**本Step（Step2）の設計時点で最初から戻り値を `bool`（`BackingStore.SaveProfile` の成否結果）として定義**する。

```csharp
namespace DS4Windows.DI
{
    public interface IProfileXmlStore
    {
        // 純粋なXML読込（ファイル存在確認・パース・BackingStoreへの反映のみ）
        bool LoadProfileXml(int deviceIndex, bool launchProgram, ControlService control,
            string overridePath = "", bool xinputChange = true, bool postLoad = true);

        // 純粋なXML書込（BackingStore.SaveProfile の成否 bool をそのまま返す）
        // ※ Step4 との整合性により最初から bool で定義
        bool SaveProfileXml(int deviceIndex, string profileName);
    }
}
```

---

### 1.2 `ProfileXmlStore` 実装クラス設計
- `DS4Windows/DS4Control/Services/ProfileXmlStore.cs`（新規作成、名前空間: `DS4Windows`）。
- コンストラクタで `BackingStore` を受け取る（デフォルトは既存シムパターンに倣い `Global.store`）。
- `LoadProfileXml`／`SaveProfileXml` は `BackingStore.LoadProfile`／`BackingStore.SaveProfile` へそのまま委譲し、その成否（`bool`）を返す（XML実装自体はPhase5の対象外、変更しない）。

---

### 1.3 状態調整ロジックの `ProfileRepository` への集約
`Global.LoadProfile`／`Global.SaveProfile` に混在していた状態調整ロジック（`loggedInvalidActions.Clear()`、一時プロファイルフラグのリセット等）を `ProfileRepository.LoadProfile`／`SaveProfile` 内へ直接移設する。
`ProfileRepository` は `IProfileXmlStore`（XML I/O）と `IProfileSettingsService`（状態）を注入され、両者を組み合わせて既存と同一の振る舞いを実現する。

```csharp
// ProfileRepository.LoadProfile 内の想定変更（イメージ）
Global.loggedInvalidActions.Clear(); // 現状維持（Step6でGlobal委譲を再検討）
bool result = _profileXmlStore.LoadProfileXml(deviceIndex, false, control, "", true, true);
_profileSettings.SetTempProfileName(deviceIndex, string.Empty);
_profileSettings.SetUseTempProfile(deviceIndex, false);

// ProfileRepository.SaveProfile 内の想定変更（イメージ）
// IProfileXmlStore.SaveProfileXml の戻り値 bool を受け取り、成否を伝播
bool saveSuccess = _profileXmlStore.SaveProfileXml(deviceIndex, profileName);
return saveSuccess;
```

---

### 1.4 `Global.LoadProfile`／`Global.SaveProfile` のシム化
既存の `Global.LoadProfile`／`Global.SaveProfile` は、他の多数のファイルからの呼び出し元互換のため即座に削除せず、内部で `IProfileRepository`（または `IProfileXmlStore`）を呼び出す薄いシム（委譲ラッパー）とする。
これにより、段階的な移行期間中も既存コードの動作を100%維持する（§2.1 準拠）。

### 1.5 ProfilesPath の `IPathService` 経由への切替
`ProfileRepository.ProfilesPath` 内部で参照されている静的 `Global.appdatapath` を、Phase3 で導入済みの `IPathService.ProfilesPath`（または `PathService` 経由）に切り替え、ファイルシステムパスの取得を DI 化する。

---

## 2. 成果物一覧

| 種別 | ファイルパス | 変更内容 |
|---|---|---|
| インターフェース | `DS4Windows/DI/IProfileXmlStore.cs` | 純粋な XML I/O を表す新規契約（`SaveProfileXml` は `bool` を返す） |
| サービス実装 | `DS4Windows/DS4Control/Services/ProfileXmlStore.cs` | `BackingStore` への薄い委譲ラッパー実装 |
| リポジトリ改修 | `DS4Windows/DS4Control/Services/ProfileRepository.cs` | `IProfileXmlStore` 注入、状態調整ロジックの内包、`ProfilesPath` の DI 参照化 |
| DI 登録 | `DS4Windows/DI/ServiceRegistration.cs` | `IProfileXmlStore` → `ProfileXmlStore` の Singleton 登録 |
| シム化 | `DS4Windows/DS4Control/ScpUtil.cs` | `Global.LoadProfile`／`Global.SaveProfile` をシム化 |
| 単体テスト | `DS4WindowsTests/ProfileXmlStoreTests.cs` | `IProfileXmlStore` の読込・保存成否モック検証テスト新設 |
| 単体テスト拡充 | `DS4WindowsTests/ProfileRepositoryTests.cs` | 責務分離後の状態更新および成否伝播テストの拡充 |

---

## 3. 作業手順（マイクロタスク分割）

### タスク Step2-1: `IProfileXmlStore` & `ProfileXmlStore` の設計・作成
1. `DS4Windows/DI/IProfileXmlStore.cs` を新規作成し、`LoadProfileXml` および `bool SaveProfileXml` を定義する。
2. `DS4Windows/DS4Control/Services/ProfileXmlStore.cs` を新規作成し、`BackingStore` 呼び出しを実装する（`SaveProfile` の戻り値をそのまま返す）。

### タスク Step2-2: DI コンテナ登録追加
1. `DS4Windows/DI/ServiceRegistration.cs` に `services.AddSingleton<IProfileXmlStore, ProfileXmlStore>();` を追加する。

### タスク Step2-3: `ProfileRepository` の責務分離実装
1. `ProfileRepository` のコンストラクタに `IProfileXmlStore` を追加注入する。
2. `LoadProfile` 内で `Global.LoadProfile` 呼び出しを廃止し、`_profileXmlStore.LoadProfileXml` + 状態調整ロジックに置き換える。
3. `SaveProfile` 内で `_profileXmlStore.SaveProfileXml` を呼び出し、成否（`bool`）を戻り値として反映する。
4. `ProfilesPath` プロパティを `_pathService` 経由に変更する。

### タスク Step2-4: `Global.LoadProfile`／`Global.SaveProfile` のシム化
1. `Global.LoadProfile` および `Global.SaveProfile` の内部を、DI サービス（`AppHost.ServiceProvider` 経由の `IProfileRepository` 等）へ委譲する形にピンポイント置換する。

### タスク Step2-5: 単体テスト作成と自動テスト実行
1. `ProfileXmlStoreTests.cs` を新設し、XML I/O 委譲および `SaveProfileXml` の `true`/`false` 戻り値を検証する。
2. `ProfileRepositoryTests.cs` を拡充し、読込時の状態調整ロジック（一時プロファイル名クリア等）が実行されることを検証する。
3. `dotnet test` を実行し、全テストパスを確認する。

### タスク Step2-6: ビルド検証、進捗更新、完了報告書の作成
1. Debug / Release ビルドの成功を確認する。
2. `Phase5-Status.md` の Step2 進捗を「完了」に更新する。
3. `Phase5-Step2-Completion-Report.md` を作成する。

---

## 4. リスクと回避策

| リスク | 影響度 | 回避策 |
|---|---|---|
| **ScpUtil.cs 編集による破壊** | 高 | `ScpUtil.cs` 全体を書き換えず、`Global.LoadProfile`／`Global.SaveProfile` の本体のみをピンポイントで委譲シムに置換する（§3.2）。 |
| **状態調整の欠落** | 高 | `tempProfileDistance` や `loggedInvalidActions` のクリア順序を既存の `Global.LoadProfile` と完全に一致させる（§2.2）。 |
| **Step4 との仕様不整合** | 低 | `SaveProfileXml` の戻り値を最初から `bool` に統一し、保存失敗の握りつぶしを未然に防止する。 |

---

## 5. 完了判定基準

- [ ] `IProfileXmlStore` が新規作成され、`bool SaveProfileXml(...)` として成否を返す契約になっていること。
- [ ] `ProfileXmlStore` が `BackingStore` に純粋に委譲していること。
- [ ] `ProfileRepository` が `IProfileXmlStore` を利用し、状態調整ロジックが内包されていること。
- [ ] `Global.LoadProfile`／`Global.SaveProfile` がシム化され、既存の呼び出し元互換が維持されていること。
- [ ] 単体テストが新規作成・拡充され、すべてパスすること。
- [ ] ビルドエラーおよび警告の増加がないこと。
