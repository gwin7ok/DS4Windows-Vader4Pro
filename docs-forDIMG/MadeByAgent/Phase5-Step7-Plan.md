# フェーズ5-Step7 計画書: プロファイルアクション解決・連鎖処理の責務分離

作成日: 2026-09-02
対象ブランチ: `For-DI-migration-work`
前提ドキュメント:
- `docs-forDIMG/DI-App-Wide-Migration-Plan.md`（全体計画書・全体4層モデル定義。`ControlService ⇄ Mapping.cs`循環依存の既知課題を含む）
- `docs-forDIMG/MadeByAgent/Phase5-Plan.md` §2, §3 Step7（Phase5詳細計画書。Step1監査で新規発見した対象）
- `docs-forDIMG/MadeByAgent/Phase5-Status.md`（Phase5進捗管理）
- `docs-forDIMG/MadeByAgent/Phase5-Step1-legacy-delegation-audit-report.md` §4-1（本Stepの発見根拠）
- `.github/copilot-instructions.md`（エージェント作業ルール。§3.2 巨大ファイル編集方針が本Stepに直接関わる）

---

## ルール確認（作業開始前に毎回読む）

- **§2.1 フォールバック実装・シム維持の原則**:
  - `Global.getProfileActions`／`GetProfileAction`／`Mapping.DispatchProfileActionEdge`は削除せず、新しい薄いラッパー経由の呼び出しに置き換えるのみとする。
- **§2.2 現在の機能の完全維持 (No Feature Drop)**:
  - SpecialActionの連鎖発火条件（`uTrigger.Count == 0`かつ`automaticUntrigger`でない、同一`controls`を持つアクションのみ再発火）を一切変更しない。
- **§3.2 巨大ファイル（`Mapping.cs`）の編集方針**:
  - `Mapping.cs`は`ControlService`との循環依存が既知の巨大ファイルである。本Stepでは`Mapping.cs`の内部実装（`DispatchProfileActionEdge`本体）には一切手を入れず、その呼び出し境界のみをDI化する。ファイル全体の再生成は絶対に行わない。

---

## 0. Step7の位置づけと現状分析

### 0.1 Step1監査結果に基づく対象範囲
`Phase5-Step1-legacy-delegation-audit-report.md` §4-1に基づき、以下2つのDIサービスを対象とする。両者は依存関係が密（`ProfileActionChainService`が`IProfileActionProvider`を利用）であるため、1つの責務分離単位として扱う。

- `IProfileActionProvider`→`ProfileActionProvider`（#8）: `Global.getProfileActions`／`Global.GetProfileAction`
- `IProfileActionChainService`→`ProfileActionChainService`（#9）: 静的`Mapping.DispatchProfileActionEdge`

### 0.2 現状のコード構造（GitHub実コード確認済み）

**`ProfileActionProvider`**: `Global.getProfileActions(index)`（`BackingStore.profileActions[index]`を返す）、`Global.GetProfileAction(device, name)`（`BackingStore.profileActionDict[device]`を参照）という、いずれも**単純な読み取り専用フィールドアクセス**への委譲である。これらのフィールドはプロファイル読込時（`CacheExtraProfileInfo`→`CalculateProfileActionDicts`）に構築される、プロファイルスコープのキャッシュであり、`SpecialActionRepository`（Step5）のような「二重管理・非同期」の問題は無い。単に`Global`という静的な迂回路を経由しているだけである。

**`ProfileActionChainService`**: 既に`IProfileActionProvider`をコンストラクタ注入で受け取っており、DIとしての構成自体は健全である。問題は`DispatchNextActions`内部で静的`Mapping.DispatchProfileActionEdge(nextAction, deviceIndex, true)`を直接呼び出している点のみである。

```csharp
public void DispatchNextActions(int deviceIndex, SpecialAction sourceAction)
{
    if (sourceAction == null || sourceAction.uTrigger.Count != 0 || sourceAction.automaticUntrigger)
        return;

    var actionNames = _actionProvider.GetProfileActionNames(deviceIndex);
    for (int index = 0; index < actionNames.Count; index++)
    {
        string actionName = actionNames[index];
        SpecialAction nextAction = _actionProvider.GetProfileAction(deviceIndex, actionName);
        if (nextAction != null && nextAction.controls == sourceAction.controls)
            Mapping.DispatchProfileActionEdge(nextAction, deviceIndex, true);
    }
}
```

### 0.3 `Mapping.cs`への対応方針（重要な制約）
`Mapping.cs`は、プロジェクトの既知課題として`ControlService`との循環依存が確認されている巨大ファイルである。`DispatchProfileActionEdge`メソッド自体を`Mapping.cs`から抽出・移設することは、本Stepの「マイクロステップでの進行」の原則に反し、循環依存の解消という別の大規模作業を誘発するリスクが高い。

**したがって本Stepのゴールは、`Mapping.DispatchProfileActionEdge`自体をDI化することではなく、その呼び出し境界を薄いインターフェースで包み、`ProfileActionChainService`が静的クラスを直接知らない状態にすることに限定する。** `Mapping.cs`の内部実装・循環依存の解消はPhase5の対象外とし、将来の独立した検討課題として明記する。

### 0.4 全体4層モデルにおける位置づけ
`ProfileActionProvider`／`ProfileActionChainService`は第4層 4-cに属する。`Mapping.DispatchProfileActionEdge`は入力マッピングエンジン（第1〜2層寄り）に属するため、本Stepの境界整理は層をまたぐ呼び出しの明確化という意味も持つ。

---

## 1. 設計方針とアーキテクチャ

### 1.1 `ProfileActionProvider`の`Global`迂回路排除
`Global.getProfileActions`／`Global.GetProfileAction`は単純なフィールドアクセスであるため、`BackingStore`を直接注入し、`Global`を経由せず`_config.profileActions`／`_config.profileActionDict`を直接参照するよう変更する（Step2で`ProfileRepository`に対して行った設計、Step6で`ProfileSettingsService`が既に採用しているパターンと同一の方針）。

```csharp
public class ProfileActionProvider : IProfileActionProvider
{
    private readonly BackingStore _config;

    public ProfileActionProvider(BackingStore config = null)
    {
        _config = config ?? Global.store;
    }

    public IReadOnlyList<string> GetProfileActionNames(int deviceIndex)
    {
        if (deviceIndex < 0 || deviceIndex >= ProfileSettingsService.TEST_PROFILE_ITEM_COUNT)
            return Array.Empty<string>();
        var actionNames = _config.profileActions[deviceIndex];
        ...
    }
    ...
}
```

### 1.2 `IMappingActionDispatcher`（仮称）による`Mapping`静的呼び出しの境界化
`Mapping.DispatchProfileActionEdge`をラップする薄いインターフェースを新設し、`ProfileActionChainService`はこのインターフェース経由でのみディスパッチを行うようにする。

```csharp
namespace DS4Windows.DI
{
    // Mapping.cs (巨大・循環依存あり) への呼び出し境界を明示するための薄い契約。
    // Mapping.cs 自体のDI化・循環依存解消は本インターフェースのスコープ外。
    public interface IMappingActionDispatcher
    {
        void DispatchProfileActionEdge(SpecialAction action, int deviceIndex, bool state);
    }
}
```

```csharp
public class MappingActionDispatcher : IMappingActionDispatcher
{
    public void DispatchProfileActionEdge(SpecialAction action, int deviceIndex, bool state)
    {
        Mapping.DispatchProfileActionEdge(action, deviceIndex, state);
    }
}
```

`ProfileActionChainService`のコンストラクタに`IMappingActionDispatcher`を追加注入し、`Mapping.DispatchProfileActionEdge`直接呼び出しを`_dispatcher.DispatchProfileActionEdge(...)`に置換する。

### 1.3 テスト容易性の向上（本Stepの副次効果）
1.2の境界化により、`ProfileActionChainService`の単体テストで`IMappingActionDispatcher`をモック化できるようになる。これにより、`Mapping.cs`の実処理（HID操作を伴う可能性がある）を経由せずに連鎖発火ロジック（同一`controls`を持つアクションの絞り込み、`uTrigger`／`automaticUntrigger`条件判定）を検証できる。

---

## 2. 成果物一覧

| ファイルパス | 種別 | ライフサイクル | 内容 |
|---|---|---|---|
| `DS4Windows/DS4Control/Services/ProfileActionProvider.cs` | 更新 | **DI永続資産** | `BackingStore`直接注入への変更、`Global`迂回路の排除 |
| `DS4Windows/DI/IMappingActionDispatcher.cs` | 新規 | **DI永続資産** | `Mapping.DispatchProfileActionEdge`呼び出しの境界インターフェース |
| `DS4Windows/DS4Control/Services/MappingActionDispatcher.cs` | 新規 | **DI永続資産** | `IMappingActionDispatcher`の実装（`Mapping.cs`への薄い委譲。`Mapping.cs`自体は無編集） |
| `DS4Windows/DS4Control/Services/ProfileActionChainService.cs` | 更新 | **DI永続資産** | `IMappingActionDispatcher`注入、静的呼び出しの置換 |
| `DS4Windows/DI/ServiceRegistration.cs` | 更新 | **DI永続資産** | `IMappingActionDispatcher`のSingleton登録追加 |
| `DS4WindowsTests/ProfileActionChainServiceTests.cs` | 新規 | **テスト資産** | `IMappingActionDispatcher`をモック化した連鎖発火ロジックの単体テスト（同一`controls`判定、`uTrigger`／`automaticUntrigger`条件） |
| `docs-forDIMG/MadeByAgent/Phase5-Step7-Plan.md` | 新規 | ドキュメント | 本計画書 |
| `docs-forDIMG/MadeByAgent/Phase5-Step7-Completion-Report.md` | 新規 | ドキュメント | Step7完了報告書（`Mapping.cs`本体の扱いについての注記を含む） |
| `docs-forDIMG/MadeByAgent/Phase5-Status.md` | 更新 | ドキュメント | Step7進捗ステータス更新 |

---

## 3. 作業手順（マイクロタスク分割）

### タスク Step7-1: `ProfileActionProvider`の`BackingStore`直接参照化
- コンストラクタに`BackingStore`（既定値`Global.store`）を追加する。
- `GetProfileActionNames`／`GetProfileAction`内の`Global.getProfileActions`／`Global.GetProfileAction`呼び出しを`_config.profileActions`／`_config.profileActionDict`への直接アクセスに置換する。
- 既存の境界チェック（`deviceIndex`範囲、`null`チェック）を維持する。

### タスク Step7-2: `IMappingActionDispatcher`の新設
- `IMappingActionDispatcher.cs`（`DS4Windows/DI/`）を作成する。
- `MappingActionDispatcher.cs`（`DS4Windows/DS4Control/Services/`）を作成し、`Mapping.DispatchProfileActionEdge`への単純な委譲として実装する。**`Mapping.cs`自体には一切変更を加えない。**

### タスク Step7-3: DIコンテナ登録
- `ServiceRegistration.cs`に`IMappingActionDispatcher`のSingleton登録を追加する。

### タスク Step7-4: `ProfileActionChainService`のリファクタリング
- コンストラクタに`IMappingActionDispatcher`を追加注入する。
- `DispatchNextActions`内の`Mapping.DispatchProfileActionEdge`直接呼び出しを`_dispatcher.DispatchProfileActionEdge(...)`に置換する。
- 既存の連鎖発火条件（`uTrigger.Count != 0`、`automaticUntrigger`、`controls`一致判定）を一切変更しない。

### タスク Step7-5: 単体テスト作成と自動テスト実行
- `ProfileActionChainServiceTests.cs`を作成し、`IMappingActionDispatcher`をモック化した上で以下を検証する。
  - `sourceAction.uTrigger.Count != 0`の場合、連鎖発火が行われないこと。
  - `sourceAction.automaticUntrigger`が`true`の場合、連鎖発火が行われないこと。
  - 同一`controls`を持つ複数のプロファイルアクションが正しく連鎖発火されること。
- 既存回帰テスト（`DS4WindowsTests`／`StandaloneTests`）が全件通過することを確認する。

### タスク Step7-6: ビルド検証、進捗更新、完了報告書の作成
- `dotnet build DS4WindowsWPF.sln --nologo` を実行し警告0・エラー0を確認する。
- `Phase5-Status.md`のStep7欄を更新し、`Phase5-Step7-Completion-Report.md`を作成する。完了報告書には「`Mapping.cs`本体の循環依存解消は本Stepのスコープ外であり、将来の独立検討課題である」旨を明記する。

---

## 4. リスクと回避策

| リスク | 該当タスク | 回避策 |
|---|---|---|
| `Mapping.DispatchProfileActionEdge`の抽出・DI化まで踏み込みたくなり、`Mapping.cs`の巨大な内部実装に手を入れてしまう | Step7-2, Step7-4 | §3.2ルールを厳守し、`MappingActionDispatcher`はあくまで1行の委譲のみとする。`Mapping.cs`側のコードは一切参照・編集しない。 |
| `BackingStore`直接注入への変更により、プロファイル未読込時（`profileActions[deviceIndex]`が`null`）の挙動が変化する | Step7-1 | 変更前の`Global.getProfileActions`の`null`ハンドリング（`m_Config.profileActions[index]`をそのまま返す）と完全に同じ挙動になるよう、既存の`null`チェックロジックを1行単位で踏襲する。 |
| `IMappingActionDispatcher`のモック化テストが、実際の`Mapping.cs`の挙動と乖離し、統合時に想定外の問題が発生する | Step7-5 | 単体テストに加えて、既存の回帰テスト（Actions関連）とProfile切替の実機確認（Step10で実施予定）で実際の`Mapping.cs`との統合動作を確認する。 |

---

## 5. 完了判定基準

- [ ] `ProfileActionProvider`が`Global.getProfileActions`／`Global.GetProfileAction`を経由せず、`BackingStore`を直接参照している。
- [ ] `IMappingActionDispatcher`／`MappingActionDispatcher`が新設され、`ServiceRegistration.cs`に登録されている。
- [ ] `ProfileActionChainService`が`Mapping.DispatchProfileActionEdge`を直接呼び出さず、`IMappingActionDispatcher`経由になっている。
- [ ] `Mapping.cs`自体にはコード変更が加えられていない（差分が本Stepの成果物一覧のファイルに限定されている）。
- [ ] 新設した`ProfileActionChainServiceTests`および既存の全回帰テスト（`DS4WindowsTests`／`StandaloneTests`）が成功する。
- [ ] ソリューションビルドが警告0・エラー0で成功する。
- [ ] `Phase5-Status.md`が更新され、`Phase5-Step7-Completion-Report.md`が作成されている（`Mapping.cs`循環依存に関する今後の課題の明記を含む）。