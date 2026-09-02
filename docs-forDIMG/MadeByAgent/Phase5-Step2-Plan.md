# フェーズ5-Step2 計画書: プロファイル XML 読込・保存の責務分離

作成日: 2026-09-02
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
  - プロファイルのロード順、既定値、欠落設定時のフォールバック、`tempProfileDistance`／`loggedInvalidActions`等の付随状態管理を100%維持する。
- **§2.3 ログ出力の厳格な維持**:
  - `AppLogger.LogToGui`／`AppLogger.LogTrace`／`AppLogger.LogDebug` 等、既存のログ出力とログレベルを維持する。
- **§3.1 DI (Dependency Injection) の実装**:
  - コンテナ登録は `DS4Windows/DI/ServiceRegistration.cs` に行う。インターフェース名には `I` プレフィックスを付ける。
- **§3.2 巨大ファイルの編集方針**:
  - `ScpUtil.cs`（`Global`クラス・`BackingStore`クラス）はファイル全体を再生成せず、対象メソッドのみをピンポイントで置換する。

---

## 0. Step2の位置づけと現状分析

### 0.1 Step1監査結果に基づく対象範囲
`Phase5-Step1-legacy-delegation-audit-report.md` §2 表の#7（`IProfileRepository`→`ProfileRepository`）に基づき、以下を対象とする。

- `ProfileRepository.LoadProfile(deviceIndex, profileName)` 内部の `Global.LoadProfile(deviceIndex, false, control, false)` 呼び出し
- `ProfileRepository.SaveProfile(deviceIndex, profileName)` 内部の `Global.SaveProfile(deviceIndex, profileName)` 呼び出し
- `ProfileRepository.ProfilesPath` 内部の `Global.appdatapath` 直接参照

### 0.2 現状のコード構造（GitHub実コード確認済み）
`Global.LoadProfile`／`Global.SaveProfile`（`ScpUtil.cs`）は、単純な委譲ではなく以下の2種類の処理が1メソッド内に混在している。

1. **XML実I/O**: `BackingStore`（`m_Config`）インスタンスメソッドである `m_Config.LoadProfile(device, launchprogram, control, path, xinputChange, postLoad)` および `m_Config.SaveProfile(device, proName)` への委譲。実際のXMLパース・ノード生成・ファイル書き込みはここで行われる（巨大メソッドのため本Stepでは内部を変更しない）。
2. **状態調整ロジック**: `Global.loggedInvalidActions.Clear()`、`ProfileSettingsServiceInstance.SetTempProfileName`／`SetUseTempProfile`、`tempprofileDistance[device]` のリセットなど、XMLパースとは独立した付随状態の後処理。

`ProfileRepository` は現在、この2種類が混在した `Global.LoadProfile`／`Global.SaveProfile` を単一の呼び出しとして利用しており、「XMLパース」と「設定値反映（状態調整）」の境界が `ProfileRepository` の外（`Global`）に隠れている。

### 0.3 全体4層モデルにおける位置づけ
`DI-App-Wide-Migration-Plan.md` の4層モデルにおいて、`ProfileRepository` は **第4層 4-c 設定・プロファイル・アクション・環境・通知サービス** に属する（`ServiceRegistration.cs` のコメント区分に準拠）。本Stepはこの層内で、XML永続化（データアクセス）と状態調整（アプリケーションロジック）の責務を分離する。

---

## 1. 設計方針とアーキテクチャ

### 1.1 `IProfileXmlStore` インターフェース設計（新規、第4層 4-c）
XML実I/Oと状態調整を明確に分離するため、`BackingStore` への薄いラッパーとして契約を新設する。`DS4Windows/DI/IProfileXmlStore.cs`（名前空間 `DS4Windows.DI`）に定義する。

```csharp
namespace DS4Windows.DI
{
    public interface IProfileXmlStore
    {
        // 純粋なXML読込（ファイル存在確認・パース・BackingStoreへの反映のみ）
        bool LoadProfileXml(int deviceIndex, bool launchProgram, ControlService control,
            string overridePath = "", bool xinputChange = true, bool postLoad = true);

        // 純粋なXML書込
        void SaveProfileXml(int deviceIndex, string profileName);
    }
}
```

### 1.2 `ProfileXmlStore` 実装クラス設計
- `DS4Windows/DS4Control/Services/ProfileXmlStore.cs`（新規作成、名前空間: `DS4Windows`）。
- コンストラクタで `BackingStore` を受け取る（デフォルトは既存シムパターンに倣い `Global.store`）。
- `LoadProfileXml`／`SaveProfileXml` は `BackingStore.LoadProfile`／`BackingStore.SaveProfile` へそのまま委譲する（XML実装自体はPhase5の対象外、変更しない）。

### 1.3 状態調整ロジックの `ProfileRepository` への集約
`Global.LoadProfile`／`Global.SaveProfile` に混在していた状態調整ロジック（`loggedInvalidActions.Clear()`、一時プロファイルフラグのリセット等）を `ProfileRepository.LoadProfile`／`SaveProfile` 内へ直接移設する。`ProfileRepository` は `IProfileXmlStore`（XML I/O）と `IProfileSettingsService`（状態）を注入され、両者を組み合わせて既存と同一の振る舞いを実現する。

```csharp
// ProfileRepository.LoadProfile 内の想定変更（イメージ）
Global.loggedInvalidActions.Clear(); // 現状維持（Step6でGlobal委譲を再検討）
bool result = _profileXmlStore.LoadProfileXml(deviceIndex, false, control, "", true, true);
_profileSettings.SetTempProfileName(deviceIndex, string.Empty);
_profileSettings.SetUseTempProfile(deviceIndex, false);
// tempProfileDistance 相当のフラグ更新（IProfileSettingsService経由に統一するか本Stepで確認）
```

### 1.4 `Global.LoadProfile`／`Global.SaveProfile` のシム化
既存の `Global.LoadProfile`／`Global.SaveProfile` は、他の75ファイルからの呼び出し元互換のため即座には削除せず、`Global.ProfileRepositoryInstance.LoadProfile(...)` へ委譲する薄いシムとして残す（既存の `Phase4-Step1〜3` シムパターンに倣う）。

### 1.5 `ProfilesPath` の `IPathService` 経由への切替
`ProfileRepository.ProfilesPath` 内の `Global.appdatapath` 直接参照を、既にDI登録済みの `IPathService.AppDataPath` 経由に置き換える（`IPathService` はPhase3で導入済みのためコンストラクタ注入を追加するのみで完結する）。

---

## 2. 成果物一覧

| ファイルパス | 種別 | ライフサイクル | 内容 |
|---|---|---|---|
| `DS4Windows/DI/IProfileXmlStore.cs` | 新規 | **DI永続資産** | プロファイルXML読込・保存の専用契約インターフェース |
| `DS4Windows/DS4Control/Services/ProfileXmlStore.cs` | 新規 | **DI永続資産** | `IProfileXmlStore` の実装（`BackingStore`への薄いラッパー） |
| `DS4Windows/DI/ServiceRegistration.cs` | 更新 | **DI永続資産** | `IProfileXmlStore` の Singleton 登録追加 |
| `DS4Windows/DS4Control/Services/ProfileRepository.cs` | 更新 | **DI永続資産** | `Global.LoadProfile`／`Global.SaveProfile`直接呼び出しを`IProfileXmlStore`＋状態調整ロジックの組み合わせへ置換。`IPathService`注入によるパス解決変更 |
| `DS4Windows/DS4Control/ScpUtil.cs`（`Global`クラス） | 更新（ピンポイント） | 過渡期シム | `Global.LoadProfile`／`Global.SaveProfile` を `Global.ProfileRepositoryInstance` への委譲シムへ変更 |
| `DS4WindowsTests/ProfileXmlStoreTests.cs` | 新規 | **テスト資産** | `IProfileXmlStore`実装、および`ProfileRepository`の状態調整ロジック（一時プロファイルフラグリセット等）の単体テスト |
| `docs-forDIMG/MadeByAgent/Phase5-Step2-Plan.md` | 新規 | ドキュメント | 本計画書 |
| `docs-forDIMG/MadeByAgent/Phase5-Step2-Completion-Report.md` | 新規 | ドキュメント | Step2完了報告書 |
| `docs-forDIMG/MadeByAgent/Phase5-Status.md` | 更新 | ドキュメント | Step2進捗ステータス更新 |

---

## 3. 作業手順（マイクロタスク分割）

### タスク Step2-1: `IProfileXmlStore` & `ProfileXmlStore` の設計・作成
- `IProfileXmlStore.cs`（`DS4Windows/DI/`）および `ProfileXmlStore.cs`（`DS4Windows/DS4Control/Services/`）を作成する。
- `BackingStore.LoadProfile`／`SaveProfile` の既存シグネチャ・既定値・戻り値をそのまま踏襲する（内部ロジックは変更しない）。

### タスク Step2-2: DI コンテナ登録追加
- `DS4Windows/DI/ServiceRegistration.cs` に `IProfileXmlStore` の Singleton 登録を追加する。

### タスク Step2-3: `ProfileRepository` の責務分離実装
- コンストラクタに `IProfileXmlStore` を追加注入する（`IProfileSettingsService` は導入済み）。
- `LoadProfile`／`SaveProfile` メソッド内の `Global.LoadProfile`／`Global.SaveProfile` 呼び出しを、`IProfileXmlStore` 呼び出し＋状態調整ロジック（`loggedInvalidActions`クリア、一時プロファイルフラグリセット等）の組み合わせへピンポイント置換する。
- `ProfilesPath` プロパティを `IPathService.AppDataPath` 経由に変更する（コンストラクタに `IPathService` を追加注入）。

### タスク Step2-4: `Global.LoadProfile`／`Global.SaveProfile` のシム化
- `ScpUtil.cs` 内の該当メソッドを、`Global.ProfileRepositoryInstance.LoadProfile(...)`／`SaveProfile(...)` への委譲シムへピンポイント置換する。
- 既存の呼び出し元（75ファイル中の該当箇所）の戻り値・シグネチャ互換を維持する。

### タスク Step2-5: 単体テスト作成と自動テスト実行
- `DS4WindowsTests/ProfileXmlStoreTests.cs` を作成し、以下を検証する。
  - `IProfileXmlStore` 経由でのプロファイル読込・保存が既存 `BackingStore` 呼び出しと同一結果になること。
  - `ProfileRepository.LoadProfile` 実行後に一時プロファイルフラグ（`UseTempProfileArray`／`TempProfileNameArray`）が正しくリセットされること。
- 既存回帰テスト（`DS4WindowsTests`／`StandaloneTests`）が全件通過することを確認する。

### タスク Step2-6: ビルド検証、進捗更新、完了報告書の作成
- `dotnet build DS4WindowsWPF.sln --nologo` を実行し警告0・エラー0を確認する。
- `Phase5-Status.md` のStep2欄を更新し、`Phase5-Step2-Completion-Report.md` を作成する。

---

## 4. リスクと回避策

| リスク | 該当タスク | 回避策 |
|---|---|---|
| `Global.LoadProfile`が持つ状態調整ロジック（`loggedInvalidActions`、一時プロファイルフラグ）の移設漏れによる挙動差異 | Step2-3 | 移設前後でメソッド本体を1行単位で突き合わせ、全ての副作用（フィールド更新・ログ出力）を`ProfileRepository`側に過不足なく再現する。単体テストで一時プロファイルフラグの状態遷移を検証する。 |
| `Global.LoadProfile`／`Global.SaveProfile`をシム化した際、75ファイルに及ぶ既存呼び出し元の一部が未初期化のDIコンテナ経由で失敗する | Step2-4 | 既存シムパターン（`Global.ProfileRepositoryInstance`のフォールバックインスタンス）をそのまま流用し、DIコンテナ未初期化時は`fallbackProfileRepository`が使われることを確認する。 |
| `IPathService`未登録環境（テスト等）で`ProfilesPath`解決が失敗する | Step2-3 | `IPathService`は既にPhase3でDI登録済みのため通常経路では問題ないが、コンストラクタのデフォルト引数で`null`許容とし、`null`の場合は`Global.appdatapath`へのフォールバックを一時的に残す（Step6で解消を検討）。 |

---

## 5. 完了判定基準

- [ ] `IProfileXmlStore` が `DS4Windows/DI/` に定義されている（DI永続資産）。
- [ ] `ProfileXmlStore` が `DS4Windows/DS4Control/Services/` に実装されている（DI永続資産）。
- [ ] `ServiceRegistration.cs` に `IProfileXmlStore` が登録されている（DI永続資産）。
- [ ] `ProfileRepository.LoadProfile`／`SaveProfile` が `Global.LoadProfile`／`Global.SaveProfile` を直接呼び出さず、`IProfileXmlStore`＋状態調整ロジックの組み合わせに置き換わっている。
- [ ] `ProfileRepository.ProfilesPath` が `IPathService.AppDataPath` 経由でパス解決している。
- [ ] `Global.LoadProfile`／`Global.SaveProfile` が `Global.ProfileRepositoryInstance` への委譲シムになっている（既存呼び出し元は無修正で動作する）。
- [ ] 新設した `ProfileXmlStoreTests` および既存の全回帰テスト（`DS4WindowsTests`／`StandaloneTests`）が成功する。
- [ ] ソリューションビルドが警告0・エラー0で成功する。
- [ ] `Phase5-Status.md` が更新され、`Phase5-Step2-Completion-Report.md` が作成されている。