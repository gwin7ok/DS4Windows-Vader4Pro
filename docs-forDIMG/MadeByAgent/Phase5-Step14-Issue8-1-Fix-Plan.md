# Phase5-Step14 Issue8-1 実装計画書: プロファイル連続切替の暴走ループ是正、および同一SpecialAction実行重複禁止

作成日: 2026-09-12
対象ブランチ: `For-DI-migration-work`
前提文書: `docs-forDIMG/MadeByAgent/Phase5-Step14-Issue8-1-Trigger-Spec-Compliance-Analysis.md`（設計方針・確認事項は全て確定済み）
関連文書: `docs-forDIMG/MadeByAgent/Phase5-Step14-RealDevice-Investigation-and-Fix-Report.md` §7.2
参照ルール: `.github/copilot-instructions.md`（§2 No Feature Drop、§3.2 巨大ファイルのピンポイント編集、§4 マイクロステップ原則）

---

## 0. 位置づけとスコープ

### 0.1 目的
実機ログ解析・コード追跡により確定した2件の是正を実装する。

1. **仕様④是正**: `Global.ApplyProfile`等が`ActionManager.ClearAllEntries()`/`ClearDeviceState()`を呼ぶたびに`ActionInstanceState`が無条件に再生成され、その時点で物理的に押されたままのトリガーを「新規成立」と誤認して即座に再実行してしまう不具合（プロファイル連続切替の暴走ループの直接原因）。
2. **新規要件（仕様③の厳格化）**: 同一の`SpecialAction`（同一デバイス）について、実行が完了するまでは新たなトリガー成立があっても実行を開始せず、完了後に1回だけ再実行する「深さ1のキュー」方式で重複実行を防止する。

### 0.2 スコープに含むもの
* `ActionInstanceState`（`DS4Windows/DS4Control/ActionManager.cs`）への新規フィールド追加。
* `Mapping.cs`のトリガー判定箇所（`Profile`/`Program`/`GyroCalibrate`型が`GetBeingTriggered`ベースで実行可否を判定している箇所）への、上記2件のガード追加。
* 上記変更に対応する単体テストの追加。

### 0.3 スコープに含まないもの（別タスクとして切り出し済み）
* **仕様⑤是正（トリガー構成ボタンの出力抑制期間の延長）**: `Phase5-Step14-Issue8-1-Trigger-Spec-Compliance-Analysis.md` §2.4の通り、「Issue 8-1(3)」として本計画書完了後に個別計画書を作成する。
* **MultiAction（Guide複合キー）連射の原因調査・是正**: `Phase5-Step14-RealDevice-Investigation-and-Fix-Report.md` §7.4「Issue 8-3」として別管理する。
* `Mapping`の完全instance化、ViGEmバックエンド抽象化等、全体計画書§4.5のカテゴリA〜Fに該当する事項。

---

## 1. 実地確認結果（2026-09-12時点、リポジトリ最新コミットに対する`grep`確認）

### 1.1 `ActionInstanceState`の定義箇所
`DS4Windows/DS4Control/ActionManager.cs` 12-24行目付近。`BeingTriggered`・`IsMacroRunning`等、本計画書で追加する新規フィールドと同じ形式（`public bool フィールド名 = 既定値;`）で定義されている。

### 1.2 状態再生成（バグの直接原因）の全呼び出し箇所
`ActionManager.ClearAllEntries()` / `ClearDeviceState(int)`（`DS4Windows/DS4Control/ActionManager.cs`、実体は`DefaultActionManager.cs`経由）は、`DS4Windows/DS4Control/ScpUtil.cs`内の以下6箇所から呼ばれていることを確認した。

| 行番号（目安） | 呼び出し元メソッド | 用途 |
|---|---|---|
| 3225, 3236 | `ApplyProfile`（プロファイル切替） | 本バグの直接の発生源 |
| 3346 | `SaveAction`（個別SpecialAction保存） | gwin7ok氏の仕様④説明にある「プロファイル保存時」に該当 |
| 3375 | `SaveActions`（SpecialAction一括保存） | 同上 |
| 3389 | `RemoveAction`（SpecialAction削除） | 同上 |
| 9558, 9571, 9613 | `LoadActions`（SpecialAction読込・既定値作成） | コントローラー接続時等に該当 |

**重要**: 本計画書の設計（新規フィールドをデフォルト値`true`で持つ）は、`ActionInstanceState`が新規生成される**全ての**箇所に自動的に適用される。上記6箇所すべてに個別の追加コードを書く必要はなく、`ActionInstanceState`のコンストラクタ（フィールド既定値）を変更するだけで、これら全ての呼び出し元に一律で保護がかかる。これはgwin7ok氏の仕様④説明「プロファイル切換後や、コントローラー接続時、プロファイル保存時などに発生する」という複数シナリオの列挙と正確に一致しており、設計の妥当性を裏付けている。

### 1.3 トリガー実行可否判定の箇所
`Mapping.cs` 5079行目`if (triggeractivated)`ブロック内、`Profile`型（5133行目付近）を含む複数の分岐が`!GetBeingTriggered(index, action, device)`を実行可否条件として使っている。`GetBeingTriggered`（1041行目）は最終的に`ActionManager.IsBeingTriggered(action, device)`→`GetStateFor(action, device).BeingTriggered`を参照する。

### 1.4 `IsMacroRunning`という既存の類似パターン
`ActionManager.cs` 20行目に`public bool IsMacroRunning = false;`が既に存在し、`Mapping.cs`の`PlayMacroTask`（6080-6324行目）がマクロの多重実行防止に使っている。本計画書で追加する`IsExecuting`/`PendingReExecutionRequested`は、この既存パターンを`Profile`/`Program`/`GyroCalibrate`型向けに一般化したものである。

---

## 2. マイクロタスク breakdown

### タスク1: `ActionInstanceState`への新規フィールド追加

- [ ] `DS4Windows/DS4Control/ActionManager.cs`の`ActionInstanceState`クラスに、以下3フィールドを追加する。

  ```csharp
  // Issue8-1是正(1): プロファイル適用後は、以後発生した成立イベントのみを成立とみなす（仕様④）。
  // ActionInstanceStateが新規生成された直後はtrue。以後、当該アクションのトリガーが
  // 一度でも「未成立（トリガー構成ボタンのいずれかが押されていない）」状態として観測されるまでは、
  // たとえ全ボタン押下状態が検出されても新規成立とみなさない。
  public bool RequiresFreshPressAfterReset = true;

  // Issue8-1是正(2): 同一アクションの実行重複禁止（仕様③の厳格化）。
  // Macro型はIsMacroRunningで既に管理されているため、それ以外の種別
  // （Profile/Program/GyroCalibrate等）向けに新設する。
  public bool IsExecuting = false;

  // 実行中に新たな成立イベントが発生した場合、完了後にもう一度だけ実行してほしいという
  // 要求を記録する（実行キュー方式・キュー深さ1。無制限キューによる際限のない滞留を防ぐため）。
  public bool PendingReExecutionRequested = false;
  ```

- [ ] 既存の`BeingTriggered`・`IsMacroRunning`と同じ書式・並び順に揃える。

### タスク2: `Mapping.cs` — トリガー成立判定への「フレッシュプレス要求」ガード追加

- [ ] `triggeractivated`が確定した直後（5079行目`if (triggeractivated)`の手前）に、共通のアーム処理を追加する。

  ```csharp
  if (!triggeractivated)
  {
      // 未成立状態を観測 = 以後の成立イベントは正規のものとして扱ってよい
      var __armState = ActionManager.GetStateFor(action, device);
      if (__armState != null) __armState.RequiresFreshPressAfterReset = false;
  }
  ```

  > 変数名`__armState`は既存のローカル変数（`st`等）との衝突を避けるための仮称。実装時に周辺のコーディング規約・既存変数名と整合する名前に調整する。

- [ ] `Profile`型の実行可否判定（5133行目付近）に、上記フラグのチェックを追加する。

  ```csharp
  var __gateState = ActionManager.GetStateFor(action, device);
  bool __blockedByFreshPressGuard = __gateState != null && __gateState.RequiresFreshPressAfterReset;
  if (!GetBeingTriggered(index, action, device) && !__blockedByFreshPressGuard && (...))
  {
      // 既存の実行処理
  }
  ```

- [ ] `Program`型・`GyroCalibrate`型など、同様に`GetBeingTriggered`を実行可否判定に使っている他の箇所を`grep`で洗い出し、漏れなく同一のガードを追加する。

### タスク3: `Mapping.cs` — 同一アクション実行重複禁止（キュー深さ1）の追加

- [ ] タスク2で特定した各実行箇所（`Profile`型を優先実装し、`Program`型・`GyroCalibrate`型に横展開）を、実行前後を`try/finally`で囲み`IsExecuting`を管理する形に変更する。

  ```csharp
  if (!GetBeingTriggered(index, action, device) && !__blockedByFreshPressGuard && (...))
  {
      var __execState = ActionManager.GetStateFor(action, device);
      if (__execState != null && __execState.IsExecuting)
      {
          // 実行中 → キューに積む（1件のみ。既にキュー済みなら何もしない）
          __execState.PendingReExecutionRequested = true;
      }
      else
      {
          ExecuteProfileSpecialAction(action, device, __execState);
      }
  }

  // 既存の実行処理を、再実行にも使えるよう小さなローカル関数に切り出す
  void ExecuteProfileSpecialAction(SpecialAction action, int device, ActionInstanceState state)
  {
      if (state != null) state.IsExecuting = true;
      try
      {
          // 既存の実行処理（ApplyProfile 呼び出し等）をそのまま呼び出す
          ...
      }
      finally
      {
          if (state != null)
          {
              state.IsExecuting = false;
              if (state.PendingReExecutionRequested)
              {
                  state.PendingReExecutionRequested = false;
                  ExecuteProfileSpecialAction(action, device, state); // 1回だけ再実行
              }
          }
      }
  }
  ```

  > 実装時は、既存コードのネスト構造・スコープ（ローカル関数化が難しい場合はメソッド抽出等）に応じて具体的な形を調整する。**再実行は「もう一度だけ」であることをコード構造上保証し、`while`ループ等で無制限に繰り返さない。**

- [ ] `Program`型・`GyroCalibrate`型の実行内容を確認し、真に重複実行を避けるべき型にのみ適用する（Fix-Plan.md §4.4のリスク欄参照）。

### タスク4: 単体テストの追加

- [ ] `DS4WindowsTests`に、`Mapping`のトリガー判定ロジックを直接検証する既存テストがあるかを確認する（`Mapping`はほぼstaticかつ内部状態が多いため、既存テスト手法を踏襲する）。
- [ ] 最低限、以下のシナリオを検証するテストを追加する。
  1. `ActionInstanceState`を新規生成した直後は`RequiresFreshPressAfterReset == true`であること。
  2. `RequiresFreshPressAfterReset == true`の状態からトリガー非成立（`triggeractivated == false`相当の操作）を経ると`false`になること。
  3. `IsExecuting == true`の間に新たな成立が発生した場合、`PendingReExecutionRequested`が`true`になり、実行が即座には行われないこと。
  4. `IsExecuting`が`false`に戻った時点で、`PendingReExecutionRequested == true`なら1回だけ再実行され、その後`PendingReExecutionRequested`が`false`に戻ること。
- [ ] 既存の`Phase5-Step14-Issue7`関連テスト（`ProfileSettingsServiceTests.cs`等）に影響がないことを確認する。

### タスク5: ビルド・実機検証

- [ ] `dotnet build`・`dotnet test`のクリーン実行確認。
- [ ] 実機シナリオ1: 「L2+PSホールド状態を維持したままプロファイル切替を繰り返す」→ 暴走ループが発生しないこと（プロファイルが1回だけ切り替わり、それ以上自動では切り替わらないこと）を確認する。
- [ ] 実機シナリオ2: 「プロファイル切替の完了前に、意図的に素早くもう一度トリガーを成立させる」→ 実行がオーバーラップせず、1回目完了後に2回目が実行される（またはキューされて後から1回だけ実行される）ことを確認する。
- [ ] 既存の正常系（トリガーを1回押して離す、離してから再度押す、を意図通りのタイミングで行う操作）に回帰がないことを確認する。

---

## 3. リスクと回避策（`Phase5-Step14-Issue8-1-Trigger-Spec-Compliance-Analysis.md` §3.4・§4.4を集約）

| リスク | 対応 |
|---|---|
| `RequiresFreshPressAfterReset`のチェック漏れ箇所が残る | `GetBeingTriggered`を実行可否判定に使っている全箇所を`grep`で洗い出し、漏れなく追加する |
| プロファイル適用**以外**の理由（§1.2の6箇所）でガードがかかることが、それぞれのシナリオで許容されるか | §1.2の通り、6箇所全てがgwin7ok氏の仕様④説明が想定する「プロファイル適用後」の一形態に該当することを確認済み。個別の除外は不要と判断する |
| `finally`ブロックからの再実行が無限再帰・デッドロックを起こす | 再実行は「もう一度だけ」に限定する構造（`while`を使わない）を徹底する |
| `Program`型・`GyroCalibrate`型など、重複実行してもそもそも問題にならない型まで一律にガードしてしまう | 型ごとの実処理内容を実装時に再確認し、真に必要な型にのみ適用する |
| ホットパスへの影響（`ActionManager.GetStateFor`呼び出し増加） | `GetStateFor`は既存の`GetBeingTriggered`からも呼ばれている既存パスであり、追加のオーバーヘッドは軽微と見込む。念のため実機シナリオでの体感確認を行う |

---

## 4. 完了条件

1. `ActionInstanceState`に`RequiresFreshPressAfterReset`・`IsExecuting`・`PendingReExecutionRequested`が追加されていること。
2. `Profile`型（および該当する`Program`/`GyroCalibrate`型）のトリガー実行判定が、上記フィールドを用いたガードを経由するよう修正されていること。
3. §2タスク4の単体テストが全て追加され、PASSすること。
4. 実機シナリオ1・2（§2タスク5）で、報告されていた暴走ループおよび実行オーバーラップが再現しないことを確認すること。
5. 既存の全自動テストが回帰なくPASSすること。

---

## 5. 進行ルール

- 本計画書の内容について、着手前にgwin7ok氏の最終確認を得る。
- タスク1〜3は「1タスク=1PR」を基本とし、各PRでビルド・既存テストの実行を確認する。
- 巨大ファイル（`Mapping.cs`、`ScpUtil.cs`）の編集はピンポイント編集を徹底する。
- 実装完了後、`Phase5-Step14-RealDevice-Investigation-and-Fix-Report.md` §7.2・§5、および`Phase5-Step14-Issue8-1-Trigger-Spec-Compliance-Analysis.md`を完了報告に基づいて更新する。
- 成果物は`present_files`による直接ダウンロード形式で提供する（PowerShellスクリプト形式は使用しない）。

---

## 6. 次のアクション

1. gwin7ok氏より本計画書の承認を得る。
2. タスク1〜5を順次実施し、完了報告書を作成する。
3. Issue 8-1(3)（仕様⑤是正）の個別計画書を、本計画書の完了後に新規作成する。