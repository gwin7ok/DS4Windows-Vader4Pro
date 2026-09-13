# Phase5-Step14 Issue8-1(3) 実装計画書: トリガー構成ボタンの出力抑制期間の是正（仕様⑤）

作成日: 2026-09-12
対象ブランチ: `For-DI-migration-work`
前提文書: `docs-forDIMG/MadeByAgent/Phase5-Step14-Issue8-1-Trigger-Spec-Compliance-Analysis.md` §2.4（仕様⑤の明確化・乖離確定・切り出し確定）
前フェーズ: `Phase5-Step14-Issue8-1-Fix-Plan.md`（Issue 8-1本体、実装・実機検証完了済み）
参照ルール: `.github/copilot-instructions.md`（§2 No Feature Drop、§3.2 巨大ファイルのピンポイント編集、§4 マイクロステップ原則）

---

## 0. 位置づけとスコープ

### 0.1 目的
gwin7ok氏により明確化された仕様⑤を実装する。

> トリガーが成立したら、そのトリガーを構成する全てのボタンに設定されているボタン信号・KBM出力信号・機能実行・SpecialActionの実行（トリガー成立前に実行済み・実行中のものを除く）などが、**トリガーを構成したボタン全てが押されていない状態になるまで**、出力されない形になるのが望ましい。

現行コードは、トリガー全体の成立/解除判定（`triggeractivated`、いずれか1つでも離れたら`false`）と全く同じタイミングで出力抑制を解除してしまっており、「トリガーを構成した個々のボタンが、それぞれ実際に離されるまで」という、より長く続くべき抑制期間を実装できていない（詳細は前提文書§2.4）。

### 0.2 スコープに含むもの
* 通常マッピング処理側の出力抑制（`ProcessControlSettingAction`の冒頭にある既存の`CheckForSpecialActionSuppression`機構の拡張）。
* SpecialAction判定ブロック内の出力抑制（`ResetToDefaultValue`呼び出し、5092-5096行目）の抑制条件変更。
* 上記に対応する単体テスト。

### 0.3 スコープに含まないもの
* Issue 8-1本体（仕様③④、実装・実機検証完了済み）の変更。
* Issue 8-3（MultiAction/Guide複合キーの連射原因調査）。別課題として管理。
* `Mapping`の完全instance化等、全体計画書§4.5のカテゴリA〜Fに該当する事項。

---

## 1. 実地確認結果（2026-09-12時点、リポジトリ最新コミットに対する`grep`・実装追跡）

### 1.1 【重要な発見】通常マッピング処理側には、既に「SpecialAction抑制」専用の choke point が存在する

前提文書§2.4の設計案5「通常のマッピング処理側にも横断的なチェックが必要」について、実装への影響範囲を懸念していたが、実地確認の結果、**この横断的なチェックは既に`ProcessControlSettingAction`（`Mapping.cs` 4370行目）の冒頭に実装済みであった**ことが判明した。

```csharp
private static void ProcessControlSettingAction(DS4ControlSettings dcs, int device, ...)
{
    // Check if this control press would trigger a Special Action
    // If so, suppress the normal mapping to prevent conflict
    if (CheckForSpecialActionSuppression(device, dcs.control, cState, eState, tp, fieldMapping))
    {
        // Special Action would be triggered - reset the control to default and return without processing
        ResetToDefaultValue(dcs.control, MappedState, outputfieldMapping);
        return;
    }
    ...
}
```

`ProcessControlSettingAction`は、LS/RS/各ボタン等**全ての通常マッピング処理で共通して呼ばれる関数**であり（`MapCustom`本体からループ呼び出しされる）、この関数の**入口1箇所**を修正するだけで、仕様⑤が要求する「通常出力の横断的な抑制」を実現できることが確定した。当初懸念していたような、多数の個別マッピング処理箇所への散在的な修正は不要である。

### 1.2 現行の`CheckForSpecialActionSuppression`の実装（是正対象）

```csharp
// Mapping.cs 7673-7706行目
private static bool CheckForSpecialActionSuppression(int device, DS4Controls control, ...)
{
    List<string> profileActions = getProfileActions(device);
    for (...)
    {
        SpecialAction action = GetProfileAction(device, actionname);
        if (action == null || action.trigger == null) continue;

        bool controlInTrigger = /* action.trigger に control が含まれるか */;
        if (!controlInTrigger) continue;

        // Use the extracted trigger logic from MapCustomAction
        if (IsSpecialActionTriggered(action, device, cState, eState, tp, fieldMap))
        {
            return true; // Special Action would be triggered - suppress normal mapping
        }
    }
    return false;
}
```

`IsSpecialActionTriggered`（7601-7650行目付近）は、`MapCustomAction`内の`triggeractivated`算出ロジックの複製であり、「トリガーに設定された全ボタンが**現在**押されているか」という**その場限りの水準判定**を行っているのみである。これが、仕様⑤が求める「離されるまで抑制を継続する」という**状態を持った**判定になっていないことの直接的な原因である。

### 1.3 SpecialAction判定ブロック内の抑制処理（是正対象2）

```csharp
// Mapping.cs 5092-5096行目付近（if (triggeractivated) ブロック内）
for (int i = 0, arlen = action.trigger.Count; i < arlen; i++)
{
    DS4Controls dc = action.trigger[i];
    ResetToDefaultValue(dc, MappedState, outputfieldMapping);
}
```

こちらも`triggeractivated`が`true`である間だけ実行される点で、§1.2と全く同じ性質の問題を抱えている。

---

## 2. 設計方針（前提文書§2.4からの改訂）

### 2.1 状態の持たせ方（改訂）

前提文書§2.4は、抑制対象ボタン集合`SuppressedTriggerButtons`を`ActionInstanceState`（アクションごと・デバイスごと）に持たせる案を示していた。しかし実装検討の結果、**この状態は「物理ボタン単位・デバイス単位」で管理する方が実装がシンプルかつ正確になる**と判断し、設計を改める。

理由:
* 抑制の実体は「特定の物理ボタンが、いつ通常出力を再開してよいか」という、ボタン側の状態である。特定の`SpecialAction`インスタンスに紐づける必然性がない。
* 複数の`SpecialAction`が同じ物理ボタンをトリガーに含む場合（まれだが起こり得る）、`ActionInstanceState`単位で管理すると、集合の突き合わせや二重管理が発生し得る。デバイス単位の集合であれば、この種の重複も自然に吸収される。
* `CheckForSpecialActionSuppression`（通常マッピング側の唯一の choke point）は`SpecialAction`ではなく`DS4Controls control`を主キーに判定する関数であるため、同じ主キー（物理ボタン・デバイス）で状態を持つ方が自然に対応する。

```csharp
// Mapping.cs に新設する static フィールド（他の per-device 配列と同じ書式に合わせる）
private static readonly HashSet<DS4Controls>[] suppressedTriggerButtons =
    InitSuppressedTriggerButtonsArray();

private static HashSet<DS4Controls>[] InitSuppressedTriggerButtonsArray()
{
    var arr = new HashSet<DS4Controls>[/* 既存の per-device 配列と同じ最大デバイス数定数 */];
    for (int i = 0; i < arr.Length; i++) arr[i] = new HashSet<DS4Controls>();
    return arr;
}
```

> 実装時に、既存の`deviceRuntime`等、他のper-device静的配列の初期化パターン（配列サイズの定数）に厳密に合わせる。

### 2.2 抑制対象への追加（`MapCustomAction`）

`MapCustomAction`の`triggeractivated`確定直後（Issue8-1是正(1)の`RequiresFreshPressAfterReset`アーム処理と同じ場所）に、以下を追加する。

```csharp
if (triggeractivated)
{
    for (int i = 0, arlen = action.trigger.Count; i < arlen; i++)
    {
        suppressedTriggerButtons[device].Add(action.trigger[i]);
    }
}
```

`HashSet.Add`は既に含まれる要素に対しても安全（何もしない）なため、毎フレーム呼んでも問題ない。

### 2.3 抑制対象からの解除（`MapCustomAction`冒頭、per-action ループの手前）

`MapCustomAction`の冒頭、per-actionループ（4937行目）が始まる直前に、**デバイスにつき1回だけ**、現在抑制中のボタンそれぞれについて実際に離されたかを確認し、離されていれば集合から取り除く処理を追加する。

```csharp
// Issue8-1(3)是正: 抑制中のボタンのうち、実際に離されたものを解除する。
// トリガー全体の成立/解除（triggeractivated）とは独立に、ボタン単位で判定する。
if (suppressedTriggerButtons[device].Count > 0)
{
    // 列挙中の変更を避けるためコピーしてから判定する
    var currentlySuppressed = suppressedTriggerButtons[device].ToArray();
    for (int i = 0; i < currentlySuppressed.Length; i++)
    {
        DS4Controls c = currentlySuppressed[i];
        if (!getBoolSpecialActionMapping(device, c, cState, eState, tp, fieldMapping))
        {
            suppressedTriggerButtons[device].Remove(c);
        }
    }
}
```

### 2.4 `CheckForSpecialActionSuppression`の簡素化

既存の「全アクションを走査して`IsSpecialActionTriggered`を呼ぶ」ロジックを、抑制集合への単純な所属チェックに置き換える。

```csharp
private static bool CheckForSpecialActionSuppression(int device, DS4Controls control, ...)
{
    // Issue8-1(3)是正: 「現在トリガーが成立しているか」ではなく、
    // 「このボタンが、いずれかのトリガー成立によりまだ抑制継続中か」を判定する。
    // 判定に必要な状態は suppressedTriggerButtons（§2.2, §2.3）で管理される。
    return suppressedTriggerButtons[device].Contains(control);
}
```

**この置き換えにより、`IsSpecialActionTriggered`（`Mapping.cs` 7601-7650行目付近）は`CheckForSpecialActionSuppression`から呼ばれなくなる。** 他に呼び出し元がないか確認し、なければ「呼出元0件の重複ロジック」として`[Obsolete]`相当のコメントを付与し残置する（削除は別途判断、`copilot-instructions.md` §3.3-4準拠）。

### 2.5 SpecialAction判定ブロック内（§1.3）の抑制処理の統一

`if (triggeractivated) { ResetToDefaultValue(...) }`（5092-5096行目）を、§2.2で全ボタンを`suppressedTriggerButtons`に追加した**後**の処理として扱い、`ResetToDefaultValue`自体の呼び出し条件は変更せず維持する（この時点では`triggeractivated == true`なので、いずれにせよ抑制対象であることに変わりはない）。実質的な変更は不要だが、コメントで§2.2との関係を明記する。

### 2.6 処理順序上の整合性

`MapCustom`は`MapCustomAction`（SpecialAction判定、抑制集合の更新を含む）を呼んだ**後**に、`ProcessControlSettingAction`（通常マッピング、抑制集合の参照）のループへ進む（`Mapping.cs` 2886行目以降の構造、§1.1で確認済み）。このため、**同一フレーム内で「抑制集合の更新」→「通常マッピングでの参照」の順序が保証されており、1フレームの抜け漏れは発生しない**。

---

## 3. マイクロタスク breakdown

### タスク1（【2026-09-12実装済み】）: `Mapping.cs`への抑制集合フィールド追加
- [x] `suppressedTriggerButtons`（`HashSet<DS4Controls>[]`、`Global.MAX_DS4_CONTROLLER_COUNT`分）を`deviceRuntime`の直後に追加した。
- [x] 初期化パターンを既存の`deviceRuntime`配列と揃えた。

### タスク2（【2026-09-12実装済み】）: `MapCustomAction`への追加・解除ロジックの実装
- [x] per-actionループ手前（§2.3）に解除判定パスを追加した。列挙中の変更を避けるため`HashSet.CopyTo`で配列に複製してから判定している。
- [x] `triggeractivated`確定箇所（Issue8-1是正(1)の`RequiresFreshPressAfterReset`アーム処理と同じ`if (triggeractivated)`ブロック内、既存ロジックの直前）に、抑制対象への追加処理を実装した。

### タスク3（【2026-09-12実装済み】）: `CheckForSpecialActionSuppression`の簡素化
- [x] §2.4の通りロジックを置き換えた（`suppressedTriggerButtons[device].Contains(control)`の単純な判定に変更）。
- [x] `IsSpecialActionTriggered`の他の呼び出し元を`grep`で確認した結果、**`Mapping.cs` 3297行目付近（`Button`型SpecialActionのトレースログ用、`AppLogger.LogTrace`での状態変化検知）に別の呼び出し箇所が存在し、現役で使用されていることを確認した。** 当初想定していた「呼出元0件」ではなかったため、技術的負債コメントの付与は不要と判断し、`IsSpecialActionTriggered`自体には変更を加えていない（今回の変更は`CheckForSpecialActionSuppression`側の呼び出し方法のみ）。

### タスク4（【2026-09-12実装済み】）: 単体テストの追加
**実装メモ**: `suppressedTriggerButtons`が`public static`フィールドであるため直接操作・検証が可能であり、`CheckForSpecialActionSuppression`（`private static`）は既存の`SpecialActionsListViewModelTests.cs`等で使われているreflectionパターンを踏襲して呼び出した。他テストとの副作用回避のため、本テスト専用のデバイス添字（3）を使用した。

- [x] `CheckForSpecialActionSuppression_ReturnsFalse_WhenButtonNotSuppressed`: 抑制集合に含まれないボタンは抑制されないことを確認。
- [x] `CheckForSpecialActionSuppression_ReturnsTrue_WhenButtonAddedToSuppressedSet`: 抑制集合に追加したボタンが抑制されることを確認。
- [x] `SuppressedTriggerButtons_ReleasingOneButton_DoesNotAffectOtherStillHeldButton`: **仕様⑤の核心シナリオ**。L2+PSを抑制集合に追加後、PSのみを取り除いた場合、L2は抑制継続・PSは解除されることを確認（本Issueの主目的そのものの検証）。
- [x] `SuppressedTriggerButtons_ArrayIsSizedForAllDevices`: 配列が全デバイス分正しく初期化されていることを確認。

新規テストファイル`DS4WindowsTests/MappingSpecialActionSuppressionTests.cs`として追加した。

### タスク5（【未実施・要gwin7ok氏実施】）: ビルド・実機検証
- [ ] `dotnet build`/`dotnet test`のクリーン実行確認。
- [ ] 実機シナリオ: 「L2+PSでSpecialAction成立後、PSのみを先に離し、L2は押したまま維持する」→ L2自身の通常出力（デフォルト信号／通常マッピング／KBM等）が、L2自身を離すまで再開しないことを確認する。
- [ ] 実機シナリオ: 上記の後、L2を離す→L2の通常出力が正しく再開することを確認する。
- [ ] 既存の正常系（トリガー無関係の通常ボタン操作）に回帰がないことを確認する。
- [ ] Issue 8-1本体（プロファイル連続切替）のシナリオに回帰がないことを再確認する（`CheckForSpecialActionSuppression`はSpecialAction全種別に影響するため）。

---

## 4. リスクと回避策

| リスク | 対応 |
|---|---|
| `CheckForSpecialActionSuppression`の簡素化により、既存の`IsSpecialActionTriggered`ベースの判定と、新しい`suppressedTriggerButtons`ベースの判定とで、極端なケース（同一ボタンが複数アクションのトリガーに含まれる等）で挙動差が出ないか | タスク5の実機検証で、複数SpecialActionが重複するボタンを使う既存プロファイルがあれば重点的に確認する |
| `suppressedTriggerButtons`が何らかの理由でボタンを解除し損ね、正常出力が永久に止まってしまう（フェイルクローズのリスク） | §2.3の解除判定は`MapCustomAction`の**入口**で確実に毎フレーム実行されるようにし、例外発生時も握りつぶして処理継続する（既存の`try/catch`パターンを踏襲） |
| ホットパス（`ProcessControlSettingAction`は全ボタン・毎フレーム呼ばれる）への影響 | `HashSet.Contains`はO(1)であり、既存の「全アクション走査＋`IsSpecialActionTriggered`呼び出し」より**むしろ軽量化される**見込み。念のため実機で体感確認する |
| `IsSpecialActionTriggered`を削除せず残置することによるコードの重複 | 削除は別途判断とし、技術的負債コメントを付与するに留める（タスク3） |

---

## 5. 完了条件

1. `suppressedTriggerButtons`が`Mapping.cs`に追加され、トリガー成立時に全トリガーボタンが追加されること。
2. 個々のボタンが実際に離された時点で、そのボタンのみが抑制集合から解除されること（トリガー全体の成立/解除とは独立に判定されること）。
3. `CheckForSpecialActionSuppression`が抑制集合ベースの判定に置き換えられ、通常マッピング処理側の出力抑制が仕様⑤通りに動作すること。
4. 実機シナリオ（§2タスク5）で、L2+PSの例における仕様⑤の挙動が確認されること。
5. Issue 8-1本体のシナリオに回帰がないこと。
6. 既存の全自動テストが回帰なくPASSすること。

---

## 6. 進行ルール

- 本計画書の内容について、着手前にgwin7ok氏の承認を得る。特に§2.1の設計変更（`ActionInstanceState`ベースから per-device 静的集合ベースへの変更）について確認をお願いしたい。
- タスク1〜3は「1タスク=1PR」を基本とし、各PRでビルド・既存テストの実行を確認する。
- 巨大ファイル（`Mapping.cs`）の編集はピンポイント編集を徹底する。
- 実装完了後、`Phase5-Step14-RealDevice-Investigation-and-Fix-Report.md`、`Phase5-Step14-Issue8-1-Trigger-Spec-Compliance-Analysis.md`を完了報告に基づいて更新する。
- 成果物は`present_files`による直接ダウンロード形式で提供する（PowerShellスクリプト形式は使用しない）。

---

## 7. 次のアクション

1. gwin7ok氏より、本計画書（特に§2.1の設計変更）の承認を得る。
2. 承認後、タスク1〜5を順次実施し、完了報告書を作成する。