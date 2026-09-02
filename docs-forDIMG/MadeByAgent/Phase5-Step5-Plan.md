# フェーズ5-Step5 計画書: SpecialAction 永続化の責務分離

作成日: 2026-09-02
対象ブランチ: `For-DI-migration-work`
前提ドキュメント:
- `docs-forDIMG/DI-App-Wide-Migration-Plan.md`（全体計画書・全体4層モデル定義）
- `docs-forDIMG/MadeByAgent/Phase5-Plan.md` §2, §3 Step5（Phase5詳細計画書）
- `docs-forDIMG/MadeByAgent/Phase5-Status.md`（Phase5進捗管理）
- `docs-forDIMG/MadeByAgent/Phase5-Step1-legacy-delegation-audit-report.md`（Step1監査結果。本Stepの対象根拠）
- `.github/copilot-instructions.md`（エージェント作業ルール）

---

## ルール確認（作業開始前に毎回読む）

- **§2.1 フォールバック実装・シム維持の原則**:
  - `Global.LoadActions`／`SaveActions`／`SaveAction`／`RemoveAction`は、新しいDI経由の実装が完成し動作確認が取れるまで削除しない。
- **§2.2 現在の機能の完全維持 (No Feature Drop)**:
  - Actions.xmlのCRUD、重複防止、Invalid actionログ抑制（`Global.loggedInvalidActions`）、`ActionManager`のruntime状態再構築を100%維持する。
- **§2.3 ログ出力の厳格な維持**:
  - `AppLogger.LogToGui`／`AppLogger.LogTrace`を維持する。
- **§3.2 巨大ファイルの編集方針**:
  - `ScpUtil.cs`（`Global`クラス・`BackingStore`クラス）はピンポイント置換のみ行う。

---

## 0. Step5の位置づけと現状分析

### 0.1 Step1監査結果に基づく対象範囲
`Phase5-Step1-legacy-delegation-audit-report.md` §2 表の#12（`ISpecialActionRepository`→`SpecialActionRepository`）に基づき、`Global.LoadActions`／`Global.SaveActions`への再委譲を分離する。

### 0.2 【重要発見】`SpecialActionRepository`と`BackingStore.actions`が二重管理・非同期の状態にある
本計画書作成にあたり`SpecialActionRepository.cs`の実コードを確認した結果、Step1監査で記録した「`Global.LoadActions`／`SaveActions`への委譲」よりも深刻な問題を発見した。

`SpecialActionRepository`は、**独自のインメモリリスト`_actions`（`ActionList`プロパティで公開）を保持している**が、これは`Global`／`BackingStore`が実際に使用している`m_Config.actions`（`ActionManager`、`Mapping`、`ProfileActionProvider`、`Global.GetAction`／`GetActionIndexOf`が参照する「本当のデータ」）とは**完全に別のリストであり、同期されていない**。

具体的には:
- `LoadActions()`は`Global.LoadActions()`（`m_Config.actions`へロード）を呼び出すが、その結果を`_actions`へコピーしていない。**`_actions`は常に空か初期状態のまま。**
- `AddAction`／`RemoveAction`／`ReplaceAction`は`_actions`のみを操作し、`Global.SaveAction`／`Global.RemoveAction`（Actions.xmlへの実書き込みと重複防止ロジックを持つ）を一切呼び出していない。
- `SaveActions()`は`Global.SaveActions()`を呼ぶが、これは`m_Config.actions`（`_actions`とは別物）をXMLへ保存するだけであり、**`AddAction`等で`_actions`に加えた変更はこのSaveActionsでは一切永続化されない。**

**結論**: 現時点で`ISpecialActionRepository.AddAction`／`RemoveAction`／`ReplaceAction`を呼び出しても、実際に使用・保存されるActions.xmlのデータには一切反映されない。これはStep1監査が分類した「Legacy再委譲」よりも重大な、**サイレントに機能しないDI経路**である。

### 0.3 本Step着手前に必須の追加調査
上記0.2の状態が「現時点で誰も`AddAction`等を呼んでいないため症状が顕在化していない未使用コード」なのか、「既に呼ばれているが気づかれていないバグ」なのかを、タスクStep5-1で必ず確認する。既存のSpecialAction編集UI（ProfileEditor等）が`ISpecialActionRepository`ではなく`Global.SaveAction`／`Global.RemoveAction`を直接呼んでいる可能性が高いため、その呼び出し経路も併せて特定する。

### 0.4 全体4層モデルにおける位置づけ
`SpecialActionRepository`は第4層 4-cに属する。本Stepでは、Actions.xmlの実データ（`BackingStore.actions`）を唯一の情報源とし、`SpecialActionRepository`をその薄いDIラッパーとして再設計する（Step2で`ProfileRepository`に対して行った設計と同じ方針）。

---

## 1. 設計方針とアーキテクチャ

### 1.1 独自リスト`_actions`の廃止と`BackingStore.actions`への一本化
`SpecialActionRepository`が保持する独自の`_actions`フィールドを廃止し、常に`Global.store.actions`（`BackingStore.actions`）を参照・操作するように変更する。これにより「二重管理」状態を解消し、DI経路と既存Legacy経路が同一のデータを扱うことを保証する。

```csharp
public IReadOnlyList<SpecialAction> Actions
{
    get
    {
        lock (_actionLock)
        {
            return Global.store.actions.ToList().AsReadOnly();
        }
    }
}
```

### 1.2 CRUDメソッドの実データ操作への切替
`AddAction`／`RemoveAction`／`ReplaceAction`を、孤立した`_actions`への操作ではなく、`Global.SaveAction`（重複防止・Actions.xml書き込み・reload検証ロジックを内包）／`Global.RemoveAction`への委譲に置き換える。既存のUI（0.3調査で判明した呼び出し元）が直接`Global.SaveAction`等を呼んでいる場合、そちらとの整合性・重複防止ロジックの単一化も検討する。

```csharp
public bool AddAction(SpecialAction action)
{
    if (action == null || string.IsNullOrWhiteSpace(action.name))
        return false;

    lock (_actionLock)
    {
        Global.SaveAction(action.name, action.controls, (int)action.typeID,
            action.details, edit: GetActionIndex(action.name) >= 0);
        if (AppLogger.IsTraceEnabled)
            AppLogger.LogTrace($"[DI] SpecialActionRepository.AddAction: Action '{action.name}' added via DI (delegates to Global.SaveAction)");
        OnActionsChanged();
        return true;
    }
}
```

（実際のパラメータ構成は`Global.SaveAction`の全シグネチャ・`SpecialAction`クラスのプロパティ構成をタスクStep5-2で精査し確定する。）

### 1.3 `LoadActions`／`SaveActions`の整合性維持
`LoadActions()`・`SaveActions()`は現状通り`Global.LoadActions()`／`Global.SaveActions()`への委譲を維持する（1.1の変更により、これらは同一の`m_Config.actions`を参照するようになるため、参照ズレは解消される）。

### 1.4 `ActionManager`のruntime再構築との境界
`Global.SaveAction`／`RemoveAction`は内部で`ActionManager.ClearAllEntries()`を呼び既存のruntime状態をクリアしている。本Stepではこの呼び出し自体（Step8で扱う`ActionManager`静的委譲の対象）には触れず、`SpecialActionRepository`からの委譲経路を通しても従来通りこの再構築が行われることを確認するに留める。

---

## 2. 成果物一覧

| ファイルパス | 種別 | ライフサイクル | 内容 |
|---|---|---|---|
| `DS4Windows/DS4Control/Services/SpecialActionRepository.cs` | 更新 | **DI永続資産** | `_actions`独自リストの廃止、`Global.store.actions`への一本化、CRUDメソッドの`Global.SaveAction`／`RemoveAction`への委譲切替 |
| `DS4Windows/DI/ISpecialActionRepository.cs` | 確認・必要に応じ更新 | **DI永続資産** | インターフェースのシグネチャが1.1〜1.2の変更と整合しているか確認 |
| （0.3調査で特定するUI呼び出し元） | 更新（要確認） | **DI永続資産** | `Global.SaveAction`／`RemoveAction`を直接呼んでいる箇所を`ISpecialActionRepository`経由に統一するかを、影響範囲を見て判断 |
| `DS4WindowsTests/SpecialActionRepositoryTests.cs` | 新規 | **テスト資産** | `AddAction`／`RemoveAction`／`ReplaceAction`実行後、`Global.GetAction`／`Global.GetActions()`（実データ側）に変更が反映されることを検証する単体テスト（0.2の不具合の再発防止） |
| `docs-forDIMG/MadeByAgent/Phase5-Step5-Plan.md` | 新規 | ドキュメント | 本計画書 |
| `docs-forDIMG/MadeByAgent/Phase5-Step5-Investigation-Report.md` | 新規 | ドキュメント | タスクStep5-1の調査結果（`AddAction`等の既存呼び出し元の有無） |
| `docs-forDIMG/MadeByAgent/Phase5-Step5-Completion-Report.md` | 新規 | ドキュメント | Step5完了報告書 |
| `docs-forDIMG/MadeByAgent/Phase5-Status.md` | 更新 | ドキュメント | Step5進捗ステータス更新 |

---

## 3. 作業手順（マイクロタスク分割）

### タスク Step5-1: `AddAction`／`RemoveAction`／`ReplaceAction`の既存呼び出し元調査【最優先・本Step着手の前提】
- リポジトリ全体で`ISpecialActionRepository.AddAction`／`RemoveAction`／`ReplaceAction`の呼び出し箇所を検索する。
- 呼び出し元が存在する場合、現状それが「サイレントに機能していない」ことによる既知の不具合や違和感がユーザー側にないか、`Phase5-Step5-Investigation-Report.md`に記録した上で対応方針を確認する。
- 呼び出し元が存在しない場合（未使用コード）は、その旨を記録し、本Stepの修正が新規機能追加ではなく「将来の呼び出しに備えた土台の是正」であることを明確にする。
- SpecialAction編集UI（ProfileEditor等）が現在どの経路（`Global.SaveAction`直接 or 他の経路）でActions.xmlへ保存しているかを特定する。

### タスク Step5-2: `Global.SaveAction`のシグネチャ・`SpecialAction`クラス構成の精査
- `Global.SaveAction(name, controls, mode, details, edit, delayTime, extras)`の各パラメータと`SpecialAction`クラスのプロパティの対応関係を確認する。
- `AddAction`／`ReplaceAction`から`Global.SaveAction`を正しいパラメータで呼び出せるようマッピングを設計する。

### タスク Step5-3: `SpecialActionRepository`の実データ一本化実装
- `_actions`フィールドを廃止し、`Actions`／`ActionList`プロパティが`Global.store.actions`を参照するよう変更する。
- `AddAction`／`RemoveAction`／`ReplaceAction`を`Global.SaveAction`／`Global.RemoveAction`への委譲に置き換える。
- `LoadActions`／`SaveActions`は現状の委譲を維持しつつ、動作確認を行う。

### タスク Step5-4: UI呼び出し元の統一検討（タスクStep5-1の結果次第）
- タスクStep5-1で、UIが`Global.SaveAction`を直接呼んでいることが判明した場合、影響範囲・リスクを評価した上で`ISpecialActionRepository`経由への統一を本Stepで行うか、別Stepとして切り出すかを判断する。

### タスク Step5-5: 単体テスト作成と自動テスト実行
- `SpecialActionRepositoryTests.cs`を作成し、`AddAction`実行後に`Global.GetAction`で同じアクションが取得できること、`RemoveAction`実行後に`Global.GetActionIndexOf`が`-1`を返すことなど、DI経路とLegacy経路の整合性を検証する。
- 既存回帰テスト（`DS4WindowsTests`／`StandaloneTests`、Actions 85/85件を含む）が全件通過することを確認する。

### タスク Step5-6: ビルド検証、進捗更新、完了報告書の作成
- `dotnet build DS4WindowsWPF.sln --nologo` を実行し警告0・エラー0を確認する。
- `Phase5-Status.md`のStep5欄を更新し、`Phase5-Step5-Completion-Report.md`を作成する（0.2の発見内容と対応結果を明記する）。

---

## 4. リスクと回避策

| リスク | 該当タスク | 回避策 |
|---|---|---|
| `_actions`独自リストが実は何らかの箇所で意図的に参照されており、一本化によって未知の機能が壊れる | Step5-1, Step5-3 | タスクStep5-1で`ActionList`プロパティの参照箇所も含めて全数調査してから変更に着手する。 |
| `Global.SaveAction`への委譲切替時、パラメータのマッピングを誤り、保存されるSpecialActionの内容が変化する（重複防止ロジックの副作用を含む） | Step5-2, Step5-3 | 変更前後で同一の入力に対する保存結果（Actions.xmlの内容）を比較するテストをStep5-5で必ず実施する。 |
| UI呼び出し元の統一（タスクStep5-4）が想定より広範囲に及ぶ | Step5-4 | 影響範囲が大きい場合は本Stepでの統一を見送り、独立Stepとして切り出す（Phase5-Plan.md／Phase5-Status.mdへの追記が必要になるため、切り出す場合は事前に確認を取る）。 |
| `Global.SaveAction`／`RemoveAction`内の`ActionManager.ClearAllEntries()`呼び出し（Step8の対象）に、本Stepの変更が意図せず影響する | Step5-3 | `Global.SaveAction`／`RemoveAction`自体の内部実装には手を入れず、`SpecialActionRepository`からの呼び出し方法のみを変更する。 |

---

## 5. 完了判定基準

- [ ] タスクStep5-1の調査結果が`Phase5-Step5-Investigation-Report.md`に記録されている。
- [ ] `SpecialActionRepository`が独自の`_actions`リストを保持せず、`Global.store.actions`（実データ）を参照している。
- [ ] `AddAction`／`RemoveAction`／`ReplaceAction`が`Global.SaveAction`／`Global.RemoveAction`へ正しく委譲され、実際にActions.xmlへ反映される。
- [ ] `LoadActions`／`SaveActions`の既存動作（重複防止、Invalid actionログ抑制、`ActionManager`のruntime再構築）が維持されている。
- [ ] 新設した`SpecialActionRepositoryTests`および既存の全回帰テスト（`DS4WindowsTests`／`StandaloneTests`、Actions 85/85件を含む）が成功する。
- [ ] ソリューションビルドが警告0・エラー0で成功する。
- [ ] `Phase5-Status.md`が更新され、`Phase5-Step5-Completion-Report.md`が作成されている（0.2の発見内容を含む）。