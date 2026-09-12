# Issue 8-1 トリガー仕様適合性分析と対応方針

作成日: 2026-09-12
対象ブランチ: `For-DI-migration-work`
関連文書: `docs-forDIMG/MadeByAgent/Phase5-Step14-RealDevice-Investigation-and-Fix-Report.md` §7.2（Issue 8-1 一次調査）
本書の位置づけ: gwin7ok氏が整理した「SpecialActionトリガー成立についての仕様」5項目に対し、現行コードの適合状況を1項目ずつ実地確認し、バグの所在を確定したうえで対応方針（設計方針）を提示する。**本書は方針検討のための文書であり、コード実装はまだ行っていない。**

---

## 1. gwin7ok氏が整理した仕様（原文要約・5項目）

| # | 仕様項目 |
|---|---|
| ① | トリガーとして設定された全ボタンが押された時点でトリガー成立とみなしSpecialActionを実行する。実行完了までは、再度トリガーが成立しても新たな実行は行わない。 |
| ② | トリガー成立の始点は「トリガー設定された全ボタンのうち最後に押されたボタンが押された瞬間」。終点は「トリガー設定されたボタンのうちどれか1つでも離された瞬間」。 |
| ③ | （②の具体例）L2→PSの順に押すとPS押下時が成立、PS解放時が解除。この時点でL2はまだ押されていても、再度PSを押せば2回目の成立となるが、1回目のSpecialAction実行が完了していないなら2回目の実行は阻止されるべき。 |
| ④ | プロファイル適用後（プロファイル切替・コントローラー接続・プロファイル保存等）は、それ以後に発生したトリガー成立イベントのみを成立とみなす。プロファイル適用直後に全ボタンが既に押された状態にあっても、それだけでは成立とみなさない。**gwin7ok氏はこの項目の違反がバグの発生源である可能性を自ら指摘している。** |
| ⑤ | トリガー成立時、「最後に押されたボタン」自身のデフォルト信号／通常マッピング／KBM／単体SpecialActionは実行されない。それより前に押されたボタンの単体出力はトリガー成立前に既に出力されているため影響を受けない。 |

---

## 2. 現行コードの実地追跡結果（項目別）

### 2.1 ①・②（成立の始点＝全ボタンANDのエッジ、終点＝いずれか1つの解放）→ **仕様通り実装されている**

`Mapping.cs` の `MapCustomAction` 内、SpecialActionのトリガー判定は以下の構造になっている（4930行目以降、`triggeractivated`の算出ブロック）。

```csharp
// 5038-5046行目付近（既定の判定分岐）
for (int i = 0, arlen = action.trigger.Count; i < arlen; i++)
{
    DS4Controls dc = action.trigger[i];
    if (!getBoolSpecialActionMapping(device, dc, cState, eState, tp, fieldMapping))
    {
        triggeractivated = false;
        break;
    }
}
```

これは「トリガーに設定された全ボタンが現在押されているか」を**毎回のマッピング評価サイクルで再計算する水準（レベル）判定**である。「成立の瞬間＝最後のボタンが押された瞬間」は、この水準判定の結果を１つ前のサイクルの状態（`ActionManager.IsBeingTriggered`、後述）と比較し、`false→true`に変化した回だけを「新規成立」として扱う仕組みで実現されている（5079-5136行目、`if (triggeractivated) { ... if (!GetBeingTriggered(...)) { 実行 } }`）。全ボタンが押されるまでは水準判定は`false`のままであり、最後の1つが押された瞬間に初めて`true`に切り替わるため、**数学的に「最後に押されたボタンが押された瞬間」と同一の意味になる**。この部分は仕様①・②の前半（成立の始点）と一致している。

成立の終点（②後半）についても、`else`側分岐（5550-5651行目）で、水準判定が`false`（＝トリガーに設定された全ボタンのうち少なくとも1つが押されていない）になった場合に、`Profile`型を含む多くのアクション種別で`DispatchOrSetBeingTriggered(action, device, false)`が呼ばれ、`BeingTriggered`ラッチが即座に解除される実装になっている。これは「トリガー設定されたボタンのうちどれか1つでも離された時点で成立状態が終わる」という仕様と一致している。

**結論**: 仕様①・②（特にその前半：成立と解除のタイミング自体）は、現行コードで概ね正しく実装されている。

### 2.2 ③（実行完了までの再実行阻止）→ **明示的な仕組みは存在しない（現状は物理的な「離す」動作に依存した代替実装）**

現行コードで「再実行を阻止」しているのは、`BeingTriggered`ラッチが`true`のまま維持される間は`!GetBeingTriggered(...)`が`false`になり再実行がブロックされる、という仕組みのみである。これは「物理的にボタンが押されっぱなしである間は再実行しない」という意味では機能するが、**仕様③が求めている「SpecialActionの実行そのものが完了したかどうか」という基準ではない**。

`Profile`型SpecialActionの実行（`Global.ApplyProfile`）は現状同期的に呼ばれており、呼び出し元のマッピング処理スレッドはこの完了を待ってから次の入力評価に進む構造になっているため、**「実行中に同一トリガーが物理的に２回成立する」という状況は、少なくとも同一スレッド上では構造的に発生しにくい**（1回目の実行が終わるまで次の入力自体を評価できないため）。このため、③自体は今回報告された暴走ループの直接原因ではないと判断する。ただし、将来的に非同期化された処理（マクロの逐次送出、外部プロセス起動等）に対しては③の明示的な保護が今後必要になる可能性があるため、恒久対応の一部として触れておく（§4.3）。

### 2.3 ④（プロファイル適用後は"以後の"成立イベントのみを成立とみなす）→ **【違反を確認・これがバグの直接原因】**

`Global.ApplyProfile`（`ScpUtil.cs`）は、プロファイル適用のたびに以下を無条件に実行する。

```csharp
// ScpUtil.cs 3223-3241行目付近
try { DS4Windows.ActionManager.ClearAllEntries(); } catch { }
...
DS4Windows.Mapping.ClearKeyButtonControllersForDevice(device);
try { DS4Windows.ActionManager.ClearDeviceState(device); } catch { }
```

`ClearDeviceState(device)`の実体（DI経由で実際に使われる`DefaultActionManager.ClearDeviceState`、`DS4Windows/DS4Control/DefaultActionManager.cs` 193-225行目）は、当該デバイスの全SpecialActionについて、以下の1行で状態を無条件に**新品へ差し替える**。

```csharp
// DefaultActionManager.cs 209行目
ent.States[device] = new ActionInstanceState();
```

`ActionInstanceState`（`ActionManager.cs` 12-24行目）の`BeingTriggered`は、内部フィールド`_beingTriggered`の既定値`false`をそのまま返す設計であり、**「現在その物理トリガーが押されているかどうか」を一切考慮せずに常に`false`から再出発する**。

一方、トリガー成立の実行判定（`Mapping.cs` 5133行目、`Profile`型の場合）は次の通りである。

```csharp
if (!GetBeingTriggered(index, action, device) && (...))
{
    // ApplyProfile を実行
}
```

`GetBeingTriggered`は最終的に`ActionManager.IsBeingTriggered(action, device)`→`GetStateFor(action, device).BeingTriggered`を返すだけであり、上記の「プロファイル適用のたびに`false`へリセットされる」状態をそのまま参照する。**この2つを組み合わせると、プロファイル適用の瞬間に物理的にトリガーが押されたままであった場合、その押しっぱなし状態を「新規の立ち上がりエッジ」と誤認し、即座に次のSpecialActionを実行してしまう。**

これは仕様④が明示的に禁止している状況——「プロファイル適用直後に全ボタンが既に押された状態にあっても、それだけでは成立とみなさない」——にまさに違反しており、gwin7ok氏がご自身で指摘された通り、**これが本バグの直接の技術的原因である**と確定した。

#### 実機ログとの整合性
提供いただいた実機ログ（`ds4windows_log.txt`）では、`原神DS4□`→`ACO`→`原神DS4_for_gwin`→`原神DS4□`→…という3プロファイルが約1.0〜1.4秒間隔で自動的に巡回し続けており、各回の実行直前には必ず`SpecialAction PROFILE: beingTriggered=False`が記録されている。これは、各プロファイルの適用完了直後、ユーザーが物理トリガーをまだ離していない状態で`ClearDeviceState`によって`BeingTriggered`が`false`にリセットされ、その直後のマッピング評価サイクル（実測500Hz超）で押しっぱなし状態が「新規成立」と誤認され、新プロファイル側の同一トリガーに割り当てられた別のプロファイル切替アクションが即座に発火する、というサイクルが自己持続的に繰り返された結果と、技術的に完全に整合する。

### 2.4 ⑤（成立時、最後に押されたボタン自身の単体出力を抑制）→ **未検証・本バグへの寄与は薄いと判断し本書では深追いしない**

`Mapping.cs` 5092-5096行目に、トリガー成立時に`action.trigger`内の全ボタンに対し`ResetToDefaultValue`を呼ぶ処理があるが、これは「成立の瞬間に押されている全トリガーボタンの出力を一律にリセットする」処理であり、仕様⑤が求める「"最後に押されたボタン"のみ抑制し、それより前に押されたボタンの出力（成立前に既に出た分）には影響しない」という細かい区別とは、少なくとも文面上は異なる可能性がある。ただし、この項目は**今回報告された「連続切替の暴走」バグの発生機序には直接関与していない**と判断されるため、本書では深追いせず、別途必要であれば独立した調査項目として切り出すことを推奨する（§5参照）。

### 2.5 補足: MultiAction型（Guide併用）の連射は別メカニズムであり、本バグの主要因とは別系統

ログ後半（5886行目以降）で観測された`0101_GI_マップ`（`Type:MultiAction`）の高頻度連射は、`Profile`型と異なり`ActionInstanceState.BeingTriggered`を一切使わない別実装（`Mapping.cs` 5707-5840行目付近、`activeCur`/`activePrev`による前回DS4State比較ベースのタップ/ホールド検出）に基づいていることを確認した。このため、④の是正（下記§3）だけでは、このMultiAction側の連射は解消しない可能性がある。これは本バグ（Issue 8-1）とは別の技術的原因を持つ**隣接する別問題**である可能性が高く、Guide/PSボタンの状態が`d.getPreviousStateRef()`に正しく反映されているかどうかの追加調査が別途必要と考えられる。**本書では、報告いただいたプロファイル連続切替の暴走（3プロファイル巡回パターン）を主対象として方針を確定し、MultiAction側の連射は後続の別課題として切り出すことを提案する（§5）。**

---

## 3. 対応方針（設計案）

### 3.1 基本方針

仕様①・②（成立・解除のタイミング判定自体）は正しく実装されているため変更しない。**仕様④の違反のみをピンポイントで是正する。** 具体的には、「プロファイル適用によって`ActionInstanceState`が再生成された直後は、少なくとも1回、対象トリガーが『成立していない（＝ボタンが押されていない）』状態を観測するまでは、たとえ全ボタンが押された状態が検出されても新規成立とみなさない」というガードを追加する。

### 3.2 具体的な設計

1. `ActionInstanceState`（`ActionManager.cs`）に新しいフラグを追加する。

   ```csharp
   // プロファイル適用直後は true。以後、当該アクションのトリガーが
   // 一度でも「未成立（全ボタン押下でない）」状態として観測されるまでは、
   // たとえ全ボタン押下状態が検出されても新規成立とみなさない
   // （Issue8-1是正: 仕様④「プロファイル適用後は、以後発生した成立イベントのみを成立とみなす」）
   public bool RequiresFreshPressAfterReset = true;
   ```

2. `Mapping.cs`のトリガー判定箇所（`triggeractivated`確定直後、5079行目 `if (triggeractivated)` の手前）に、共通の「アーム」処理を追加する。

   ```csharp
   if (!triggeractivated)
   {
       // 未成立状態を観測 = 以後の成立イベントは正規のものとして扱ってよい
       var st = ActionManager.GetStateFor(action, device);
       if (st != null) st.RequiresFreshPressAfterReset = false;
   }
   ```

3. 成立時の実行判定（`Profile`/`Program`/`GyroCalibrate`等、`!GetBeingTriggered(...)`で実行可否を判定している全箇所）に、上記フラグのチェックを追加する。

   ```csharp
   var st = ActionManager.GetStateFor(action, device);
   bool blockedByFreshPressGuard = st != null && st.RequiresFreshPressAfterReset;
   if (!GetBeingTriggered(index, action, device) && !blockedByFreshPressGuard && (...))
   {
       // 実行
   }
   ```

4. `ActionInstanceState`の既定値は`RequiresFreshPressAfterReset = true`のままでよい。`ClearDeviceState`が`new ActionInstanceState()`で差し替えるたびに、自動的にこのフラグは`true`（＝要フレッシュプレス）に戻るため、プロファイル適用直後の防御が自然に効く。

### 3.3 この設計の妥当性

* **既存の正しい挙動（仕様①・②）を一切変更しない**: `triggeractivated`の算出ロジックにも、成立時・解除時の既存処理にも手を入れない。追加するのはあくまで「新規成立を許可するかどうか」の追加ゲートのみ。
* **仕様④の要求を過不足なく満たす**: プロファイル適用直後に全ボタンが押されたままであれば`RequiresFreshPressAfterReset`は`true`のままなので実行はブロックされ、その後ユーザーが実際に1つでもボタンを離した瞬間（`triggeractivated == false`を観測した瞬間）にフラグが解除され、以後は通常通り動作する。
* **全SpecialAction種別に横展開可能**: `ActionInstanceState`はアクション種別を問わない共通の状態オブジェクトであるため、`Profile`型に限らず`Program`型・`GyroCalibrate`型など、同じ`GetBeingTriggered`ゲートを使う全ての種別に対して同一の保護が自動的に及ぶ。
* **既存の250msデバウンス（`DefaultProfileSwitcher.cs`）と役割が重複しない**: 既存デバウンスは「短時間内の連続切替」を防ぐ時間ベースの保険であり、今回追加するガードは「プロファイル適用直後の押しっぱなし誤検知」を防ぐ状態ベースの是正である。両者は独立した防御層として併存させる。

### 3.3 リスクと確認事項

| リスク | 対応 |
|---|---|
| `RequiresFreshPressAfterReset`のチェック漏れ箇所が残ると、その種別だけ仕様④違反が temporarily 残存する | `GetBeingTriggered`を実行可否判定に使っている全箇所を`grep`で洗い出し、漏れなく追加する（Program型・GyroCalibrate型・Profile型を確認済み、他の型の要否も実装時に再確認する） |
| プロファイル適用**以外**の理由で`ClearDeviceState`が呼ばれるケース（コントローラー切断等）で、意図せずガードがかかり続ける可能性 | `ClearDeviceState`の全呼び出し元を洗い出し、それぞれのケースで「以後、一度もボタンを離さなくても正規動作させたい」特殊ケースがないかを確認する |
| MultiAction型の連射（§2.5）は本設計では解消しない | 別課題として切り出し、`d.getPreviousStateRef()`のGuideボタン反映状況を別途調査する（§5） |

---

## 4. 実装に進む前の確認事項（gwin7ok氏へのご相談）

1. **§3.2の設計方針でよいか**: `ActionInstanceState`への新規フラグ追加、および`Mapping.cs`側でのアーム／ゲート処理の追加という方式で問題ないか。
2. **③（実行完了ベースの再実行阻止）への対応要否**: 本書§2.2の分析の通り、`Profile`型は同期実行のため③の明示的な保護がなくても現状のバグには寄与していないと判断したが、将来の非同期化（マクロ逐次送出等）に備えた保護を今回のスコープに含めるか、それとも別課題として切り出すか。
3. **⑤（成立時の最後押下ボタンの出力抑制）の扱い**: 本バグとは無関係と判断し本書では対象外としたが、別途調査が必要かどうか。
4. **MultiAction（Guide複合キー）の連射（§2.5）の扱い**: 本Issue 8-1のスコープに含めるか、Issue 8-1とは別の課題（例: Issue 8-1b）として切り出すか。

上記についてご確認・ご承認をいただき次第、`Phase5-Step14-Issue8-1-Fix-Plan.md`として個別の実装計画書（マイクロタスク分解・テスト計画・完了条件を含む）を作成し、その後実装に着手する。

---

## 5. 次のアクション

1. gwin7ok氏より本書§4の確認事項についてご回答をいただく。
2. ご承認後、`Phase5-Step14-Issue8-1-Fix-Plan.md`を作成する（`copilot-instructions.md`のマイクロステップ原則に従い、実装前に個別計画書を用意する）。
3. 実装後、`dotnet build`/`dotnet test`に加え、実機での「L2+PSホールド状態を維持したままプロファイル切替を繰り返す」シナリオでの回帰確認を行う。
4. MultiAction側の連射（§2.5）を別課題として切り出すかどうかを確定し、切り出す場合は`Phase5-Step14-RealDevice-Investigation-and-Fix-Report.md`に追記する。