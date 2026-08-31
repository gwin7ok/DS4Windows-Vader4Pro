# フェーズ4-Step2 計画書: IProfileRepository 分離

作成日: 2026-08-31
対象ブランチ: `For-DI-migration-work`
前提ドキュメント:
- `docs-forDIMG/DI-App-Wide-Migration-Plan.md` §4.1, §5, §6.6（全体計画）
- `docs-forDIMG/MadeByAgent/Phase4-Plan.md` §1.2, §2, §3 Step2（Phase4詳細計画）
- `docs-forDIMG/MadeByAgent/Phase4-Status.md`（Phase4進捗管理）
- `docs-forDIMG/MadeByAgent/Phase4-Step1-Completion-Report.md`（Step1完了報告）
- `docs-forDIMG/MadeByAgent/Phase4-Step0-Global-Member-Inventory.md`（Global棚卸し一覧）
- `.github/copilot-instructions.md`（エージェント作業ルール）

---

## ルール確認（作業開始前に毎回読む）

- **§2.1 フォールバック実装・シム維持の原則**:
  - `Global` のプロファイル永続化メソッド（`LoadProfile`, `SaveProfile` 等）は削除せず、新設する `IProfileRepository` への薄いデリゲートシムとして残す。
- **§2.2 現在の機能の完全維持 (No Feature Drop)**:
  - プロファイル XML のフォーマット、設定項目の読込・保存ロジック、パス解決（Appdata / ProgramData / カスタムパス）、一時プロファイル切替、カルチャ（en-US）の挙動を 100% 維持する。
- **§2.3 ログ出力の厳格な維持**:
  - `AppLogger.LogToGui` 等の既存ログ出力を厳格に維持する。
- **§3.1 DI (Dependency Injection) の実装**:
  - インターフェース契約は `DS4Windows/DI/IProfileRepository.cs`（名前空間 `DS4Windows.DI`）に配置（**DI永続資産**）。
  - 実装クラスは `DS4Windows/DS4Control/Services/ProfileRepository.cs`（名前空間 `DS4Windows`）に配置（**DI永続資産**）。
  - コンテナ登録は `DS4Windows/DI/ServiceRegistration.cs` に行う。
- **§3.2 巨大ファイルの編集方針**:
  - `ScpUtil.cs` / `Mapping.cs` は全体を再生成せず、対象メソッド・プロパティのみをピンポイントで置換する。
- **資産のライフサイクル識別**:
  - DI永続資産（残るもの）と過渡期シム（Strangler Fig 移行用）を明確に区別して管理する。

---

## 0. Step2の位置づけと現状分析

### 0.1 Step1の成果とStep2のスコープ
- **Step1 で完了したこと**:
  - メモリ上のプロファイル設定値を保持・管理する `IProfileSettingsService` / `ProfileSettingsService` が稼働し、`Global` の対象プロパティがシム化された。
- **Step2 で行うこと**:
  - プロファイル XML ファイルの物理読込（`Load` / `LoadProfile`）、保存（`Save` / `SaveProfile`）、プロファイル一覧取得、プロファイル切り替えロジックを `IProfileRepository` / `ProfileRepository` として分離する。
  - Phase3 から引き継がれた `Mapping.cs` 内の `ApplyProfileDirect` / `RestoreProfileDirect` に残る `Program.rootHub` 依存を `IProfileRepository` と `IProfileSettingsService` を介した設計へ整理する。

### 0.2 4層モデルにおける責務境界（全体計画書 §3）
- **`IProfileSettingsService`（Step1: 第4層 設定サービス）**: メモリ上の設定データ保持・既定値・変更イベント。
- **`IProfileRepository`（Step2: 第4層 リポジトリサービス）**: プロファイル XML ファイル入出力、ファイルパス解決、プロファイル一覧・切替・永続化。
- **第3層（信号・アクション実行層）**: アクション実行・仮想コントローラー出力（本Stepの責務外）。

---

## 1. 設計方針とアーキテクチャ

### 1.1 `IProfileRepository` インターフェース設計
契約インターフェースは `DS4Windows/DI/IProfileRepository.cs`（名前空間 `DS4Windows.DI`）に定義する。

```csharp
namespace DS4Windows.DI
{
    public interface IProfileRepository
    {
        string ProfilesPath { get; }
        string GetProfilePath(string profileName);
        
        bool LoadProfile(int deviceIndex, string profileName);
        bool SaveProfile(int deviceIndex, string profileName);
        
        bool LoadDefaultProfile(int deviceIndex);
        bool LoadProfileToSlot(int deviceIndex, string profileName);
        
        IReadOnlyList<string> GetProfileNames();
        bool ProfileExists(string profileName);
        
        bool ApplyProfileDirect(int deviceIndex, string profileName);
        bool RestoreProfileDirect(int deviceIndex);
    }
}
```

### 1.2 `ProfileRepository` 実装クラス設計
- `DS4Windows/DS4Control/Services/ProfileRepository.cs`（新規作成）。
- コンストラクタで `IProfileSettingsService` を注入（Constructor Injection）。
- XML のシリアライズ/デシリアライズ処理を安全に行い、`IProfileSettingsService` と設定値をやり取りする。
- スレッドセーフティ: ファイル I/O 操作中の競合を防ぐためのロック機構を保持。

### 1.3 `Global` (in `ScpUtil.cs`) シム設計
`Global.LoadProfile`, `Global.SaveProfile` 等の既存静的メソッドは、`ProfileRepositoryInstance` を介して新サービスを呼び出す薄いシムとする。

```csharp
private static DS4Windows.DI.IProfileRepository profileRepository = null;
private static readonly DS4Windows.DI.IProfileRepository fallbackProfileRepository = new ProfileRepository(ProfileSettingsServiceInstance);

public static DS4Windows.DI.IProfileRepository ProfileRepositoryInstance
{
    get
    {
        if (profileRepository != null) return profileRepository;
        try
        {
            var service = AppHost.GetService<DS4Windows.DI.IProfileRepository>();
            if (service != null)
            {
                profileRepository = service;
                return profileRepository;
            }
        }
        catch { }
        return fallbackProfileRepository;
    }
    set => profileRepository = value;
}
```

### 1.4 Phase3 引継ぎ依存の整理 (`Mapping.cs`)
- `Mapping.cs` 内の `ApplyProfileDirect` および `RestoreProfileDirect` は、直接 `Program.rootHub` を参照する旧方式から、`IProfileRepository`（または `Global.ProfileRepositoryInstance`）へ委譲する構造へピンポイントで整理する。

---

## 2. 成果物一覧

| ファイルパス | 種別 | ライフサイクル | 内容 |
|---|---|---|---|
| `DS4Windows/DI/IProfileRepository.cs` | 新規 | **DI永続資産** | プロファイル永続化・切替の契約インターフェース |
| `DS4Windows/DS4Control/Services/ProfileRepository.cs` | 新規 | **DI永続資産** | `IProfileRepository` の本番実装クラス |
| `DS4Windows/DI/ServiceRegistration.cs` | 更新 | **DI永続資産** | `IProfileRepository` の Singleton 登録 |
| `DS4Windows/DS4Control/ScpUtil.cs` | 更新 | **過渡期シム** | `Global` のプロファイル永続化メソッドを新サービスへのシムへピンポイント置換 |
| `DS4Windows/DS4Control/Mapping.cs` | 更新 | **過渡期シム整理** | `ApplyProfileDirect` / `RestoreProfileDirect` の依存整理 |
| `DS4WindowsTests/ProfileRepositoryTests.cs` | 新規 | **テスト資産** | XML 読込・保存・切替・シム同期の単体テスト |
| `docs-forDIMG/MadeByAgent/Phase4-Step2-Plan.md` | 新規 | ドキュメント | 本計画書 |
| `docs-forDIMG/MadeByAgent/Phase4-Step2-Completion-Report.md` | 新規 | ドキュメント | Step2完了報告書 |
| `docs-forDIMG/MadeByAgent/Phase4-Status.md` | 更新 | ドキュメント | Step2進捗ステータス更新 |

---

## 3. 作業手順（マイクロタスク分割）

### タスク Step2-1: `IProfileRepository` インターフェース定義（契約）
- `DS4Windows/DI/IProfileRepository.cs` を新規作成（名前空間: `DS4Windows.DI`）。
- プロファイル入出力、パス解決、一覧取得、切替メソッドのシグネチャを定義。

### タスク Step2-2: `ProfileRepository` 実装クラス作成（実体）
- `DS4Windows/DS4Control/Services/ProfileRepository.cs` を新規作成（名前空間: `DS4Windows`）。
- `IProfileSettingsService` を依存注入し、XML の読込・保存、プロファイル切替ロジックを実装。

### タスク Step2-3: DI コンテナ登録更新
- `DS4Windows/DI/ServiceRegistration.cs` に `IProfileRepository` / `ProfileRepository` の Singleton 登録を追加。

### タスク Step2-4: `Mapping.cs` 依存整理 と `Global` ピンポイントシム化
- `Mapping.cs` の `ApplyProfileDirect` / `RestoreProfileDirect` を整理。
- `ScpUtil.cs` の `Global.LoadProfile`, `Global.SaveProfile` 等を `ProfileRepositoryInstance` へのシム委譲へ置換。

### タスク Step2-5: 単体テスト作成と自動テスト実行
- `DS4WindowsTests/ProfileRepositoryTests.cs` を新規作成し、XML 保存・読込・切替動作をテスト。
- 全テスト（`Actions.Tests` 31件, `StandaloneTests` 13件, 新設テスト）の通過を確認。

### タスク Step2-6: ビルド検証、進捗更新、完了報告書の作成
- `dotnet build DS4WindowsWPF.sln --nologo` を実行し警告0・エラー0を確認。
- `Phase4-Status.md` を更新し、`Phase4-Step2-Completion-Report.md` を作成。

---

## 4. リスクと回避策

| リスク | 該当タスク | 回避策 |
|---|---|---|
| XMLフォーマット不整合によるプロファイル破損 | Step2-2 | 既存の XML 読み書きスキーマ、ノード構造、属性名を100%維持する。 |
| ファイルパス解決（AppData等）の環境差 | Step2-2 | `Global.appdataurl` / `Global.ProfilesPath` のパス解決ロジックを踏襲する。 |
| 巨大ファイル `ScpUtil.cs` / `Mapping.cs` のコード欠損 | Step2-4 | ピンポイント置換を徹底し、ファイル全体の再生成を行わない。 |
| テスト環境でのファイル I/O 競合 | Step2-5 | テスト用の一時ディレクトリ（`Path.GetTempPath()`）を使用して独立性を確保する。 |

---

## 5. 完了判定基準

- [ ] `IProfileRepository` が `DS4Windows/DI/` に定義されている（DI永続資産）。
- [ ] `ProfileRepository` が `DS4Windows/DS4Control/Services/` に実装されている（DI永続資産）。
- [ ] `ServiceRegistration.cs` に登録され、`AppHost` から解決できる。
- [ ] `Mapping.cs` の `ApplyProfileDirect` / `RestoreProfileDirect` の依存が整理されている。
- [ ] `Global` のプロファイル永続化メソッドがシム化され、既存コードが正常に動作する。
- [ ] 新設した `ProfileRepositoryTests` および既存の回帰テストが全件成功する。
- [ ] ソリューションビルドが警告0・エラー0で成功する。
- [ ] `Phase4-Status.md` が更新され、`Phase4-Step2-Completion-Report.md` が作成されている。
