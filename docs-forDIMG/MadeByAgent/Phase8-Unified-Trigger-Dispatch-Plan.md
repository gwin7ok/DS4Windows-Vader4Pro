# Phase8 計画書: Controls／SpecialActions 統一トリガーディスパッチ

作成日: 2026-09-23
状態: 計画立案（実装未着手、決定事項の承認待ち）
対象ブランチ: `For-DI-migration-work`
上位計画書: `docs-forDIMG/DI-App-Wide-Migration-Plan.md`（本書を Phase8 として §6 に追加する）
関連: `docs-forDIMG/Model-Diagram/*`（本設計により更新が必要。§7 参照。**モデル図の変更はユーザーの明示的な指示を得てから行う**）
発端: ユーザー提案（2026-09-23）「Controls タブ／SpecialActions タブは設定場所が異なるだけで、
『トリガーが成立したら処理を実行する』という点では同じ。UI・保存/読込は分離したまま、
実行時の『トリガー監視→処理実行』部分だけを1つに統合できないか」

---

## 0. 結論（先出し）

**実現可能です。** しかも、コードベースには既にこの発想に近い汎用抽象化
（`IActionBinding`／`IInputAction`／`IOutputAction`／`IActionRegistry`）が存在していました。
ただし調査の結果、**この抽象化は実際の動作経路に一切配線されておらず（呼び出し元ゼロ）、
実際に稼働しているのは別系統の仕組み（`Actions.Action`／`IActionFactory`／各種Adapter）**
であることが判明しました。この「死んだ骨組み」の扱いを最初に決める必要があります（§4 D5）。

---

## 1. 現状調査で判明した事実

### 1.1 SpecialActions側: 二重の抽象化が存在する

| 系統 | 主な型 | 実際に稼働しているか |
|---|---|---|
| **系統A（未配線・死んだ骨組み）** | `IActionBinding`, `IInputAction`, `IOutputAction`, `IActionRegistry`, `ITriggerContext`（実装: `TriggerContextImpl`）, `KeyActionBinding`, `KeyOutputAction` | **いいえ**。`IActionRegistry.Register`/`GetBindingsForDevice` はリポジトリ全体で呼び出し元ゼロ。`KeyActionBinding.Input` は `=> null;` のまま未実装 |
| **系統B（実際に稼働）** | `Actions.Action`（抽象基底）, `MappingContext`, `IActionFactory`/`DefaultActionFactory`, `KeyActionAdapter`/`MacroActionAdapter`/`ProfileSwitchActionAdapter`/`LaunchProcessActionAdapter` | **はい**。`DefaultActionManager.DispatchTriggerEstablished` が `ent.ActionImpl.OnTrigger(device, ctx)` として直接呼んでいる（Phase1で構築・配線済み） |

`docs-forDIMG/DI-App-Wide-Migration-Plan.md` 自身も §2.5・§4.1 で `IActionRegistry` を「既存のランタイムレジストリ」として言及しているが、実際に稼働しているかどうかまでは言及していない。今回の調査で「宣言はあるが未稼働」という実態が明らかになった。

### 1.2 トリガー成立判定（「押されたか」の判定）は、どちらの系統にも属さず `Mapping.cs` に直書きされている

`Actions.Action`（系統B）は `OnTrigger`/`OnRelease`、つまり**「成立した後に何をするか」**しか扱わない。
「今この瞬間、定義された組み合わせ（例: `PS+R1`）が実際に押されているか」を判定するロジックは、
`Mapping.cs` の `MapCustomAction`（巨大メソッド）内に手続き的に埋め込まれており、
`TryDispatchSATriggerEstablished`／`CheckForSpecialActionSuppression`（7848行目）等の
ヘルパーはあるが、汎用的な `IInputAction.Evaluate(DS4State)` のような形にはなっていない。

**この「トリガー成立判定」こそが、Controls タブ（1ボタン）と SpecialActions タブ（複数ボタンの組み合わせ、
デバウンス、抑制ロジック等）の間で最も差異が大きい部分であり、統合の中心課題になる。**

### 1.3 Controls タブ側: 対応する抽象化が一切ない

`DS4ControlSettings`（`ScpUtil.cs` 124行目）は、1物理ボタン（`control: DS4Controls`）につき1つの
`ActionType`（Key/Button/Macro）を持つだけの単純な構造。`IActionBinding` 相当の統一的な扱いは
されておらず、実行は `Mapping.cs` の `MapCustom`／`PlayMacro` に直書きされている
（Phase6-Step3 の副次バグ調査で扱った経路そのもの）。

### 1.4 結論: ご提案の構造は「新しい発想の持ち込み」ではなく「既存の未完成の設計意図を完成させる」作業に近い

系統A（`IActionBinding` 等）の型の形（`Input`＋`Outputs`のペア、`OnTriggered`/`OnReleased`）は、
ご提案の「トリガーキーと処理の組み合わせをオブジェクト化する」という発想とほぼ一致している。
おそらく初期の設計者も同じ方向を目指したが、実装が `Actions.Action`／`IActionFactory` 系統（系統B）に
移行する過程で、系統Aは完成されないまま取り残されたと推測される（確証はなく、リポジトリ内の
コミット履歴等での裏付けは本書の調査範囲外）。

---

## 2. 目標とする実行時構造（ご提案の3段階をそのまま踏襲）

```
① プロファイル読込時
   Controls タブの DS4ControlSettings（トリガー定義あり）と
   SpecialActions タブの SpecialAction を、共通の「トリガー条件 + 処理」の
   オブジェクト（仮称: TriggerBinding）のリストに変換する。
   → プロファイルスロットごとに 1つの IReadOnlyList<TriggerBinding> ができる
     （Controls 由来か SpecialActions 由来かの区別はここで消える）

② コントローラー接続時
   接続されたデバイスのインデックスと、そのプロファイルスロットの
   IReadOnlyList<TriggerBinding> を紐づける
   （既存の IActionController／IControllerRegistry の概念に近い）

③ 毎入力レポート
   紐づいた TriggerBinding のリストを順に見て、各々の「トリガー条件」を
   現在のコントローラー状態と照合し、成立していれば「処理」を実行する
   （成立/解除のエッジ検出、既存の suppressedTriggerButtons 相当の抑制も
     ここに集約する）
```

---

## 3. 既存の型をどう活かすか（対応表）

| ご提案の要素 | 対応する既存の型（案） | 現状 |
|---|---|---|
| トリガー条件 | `IInputAction`（`Evaluate(DS4State) : ITriggerContext`） | 系統A、未実装（`Evaluate`の実装クラスが1つもない） |
| 処理（実行） | `IOutputAction` **ではなく** `Actions.Action`（系統B） | 系統Bが実際に稼働中。`KeyActionAdapter`等をそのまま流用できる |
| トリガー＋処理の組 | `IActionBinding` 相当（新設） | 系統Aの形を流用し、`Outputs: IOutputAction` ではなく `Impl: Actions.Action` を持つ形に作り直す |
| デバイスごとの登録簿 | `IActionRegistry` 相当（新設） | 系統Aの形（`Register`/`GetBindingsForDevice`）を流用可能。中身の型だけ差し替える |
| コントローラーとの紐付け | `IActionController`／`IControllerRegistry` | 既存（系統Bの実行側で既に使われている `KeyButtonActionController` 等）をほぼそのまま流用できる見込み |

---

## 4. 決定が必要な論点（メリット・デメリット・推奨）

### D1: 新しい統一抽象化を、系統A（`IActionBinding`系）と系統B（`Actions.Action`系）のどちらの型を土台に作るか

| 案 | 内容 | メリット | デメリット |
|---|---|---|---|
| **A1（推奨）** | 系統B（`Actions.Action`／`IActionFactory`／各Adapter）を「処理」側の土台として維持し、「トリガー条件」側だけを新設する。系統Aの型は§ D5の判断に従い削除または再設計する | 実際に稼働実績のあるコードを壊さずに済む。SpecialActionの実行結果（挙動）を変えるリスクが最小 | 系統Aの型をそのまま復活させるわけではないため、「既存の資産を活かす」度合いはA2よりやや低い |
| A2 | 系統A（`IActionBinding`／`IOutputAction`）を土台とし、`Outputs` の中身を`Actions.Action`系の処理へ委譲するラッパーを書く | 型の名前・形が既にあり、それらしく見える | 「動いたことのない設計」の上に構築することになり、隠れた設計ミス（`Input=>null`のような）を踏襲するリスクがある。系統Bとの二重管理になりやすい |

### D2: トリガー条件（`IInputAction`相当）を、Controls タブ用・SpecialActions タブ用で別クラスにするか、1つの汎用クラスにするか

| 案 | 内容 | メリット | デメリット |
|---|---|---|---|
| **B1（推奨）** | 「1つ以上のボタンのAND条件＋押下/解放エッジ検出＋任意のデバウンス」を表す**1つの汎用クラス**（例: `ComboTriggerInputAction`）を新設し、Controls タブの1ボタンは「AND条件の要素数が1」という特殊ケースとして扱う | Controls タブとSpecialActionsタブの違いが「ボタン数の違い」だけになり、実行時ロジックが完全に1本化される。将来ボタン数以外の差異が出ても拡張しやすい | 既存の `CheckForSpecialActionSuppression` 等の細かい仕様（抑制期間、トグル等）をすべてこの1クラスに正しく移植する必要があり、初期の移植作業の難度が上がる |
| B2 | Controls タブ用（`SingleButtonInputAction`）とSpecialActions タブ用（`ComboInputAction`）を別クラスとして用意する | 既存ロジックの移植が単純（今ある2つの処理をほぼそのまま別クラスに分けるだけ）。移植時の挙動保持の検証がしやすい | 「統一した」と言いつつ実行時ロジックが2系統のまま残る。将来Controlsタブに複数ボタンの組み合わせ機能を追加したくなった場合、また分岐が増える |

### D3: 移行の進め方（既存経路との共存期間の設け方）

| 案 | 内容 | メリット | デメリット |
|---|---|---|---|
| **C1（推奨）** | 新しい統一ディスパッチを既存の `Mapping.cs` 手続き型ディスパッチと**並行して構築**し、アクション種別ごと（まずKeyのみ→Macro→Profile切替→…）に少しずつ新経路へ切り替える。切り替え済みの種別は `Mapping.cs` 側の対応コードを削除する（Strangler Fig、本プロジェクトの一貫した方針） | 既存動作を壊すリスクを最小化できる。1機能ごとに実機確認できる | 移行完了まで新旧2つの経路が一時的に共存し、コードの見通しが一時的に悪化する |
| C2 | 一括で `Mapping.cs` の該当ロジックを新経路に置き換える | 移行期間が短い | `Mapping.cs`（8,600行超、全ユーザーの入力処理の心臓部）に対する一括変更は、Phase6-Step3で既に確認された「実地確認なしの計画は簡単に外れる」というリスクが極めて高い形で顕在化する。強く非推奨 |

### D4: この統合作業をどのフェーズとして位置づけるか

| 案 | 内容 | メリット | デメリット |
|---|---|---|---|
| **D4-1（推奨）** | 独立した **Phase8** として新設し、Phase7（`Mapping.cs` 完全instance化）の後に着手する | Phase7が対象とする「static→instance化」という関心事と、Phase8が対象とする「トリガーディスパッチの統合」という関心事が明確に分離される。Phase7でstatic結合を減らしておくことがPhase8の下地にもなる（Phase6→Phase7の関係と同じ考え方） | フェーズ数が増える |
| D4-2 | Phase7に統合し、「instance化」と「ディスパッチ統合」を同時に行う | フェーズ数が増えない | 2つの大きな関心事を同時に扱うことになり、問題発生時にどちらが原因か切り分けにくくなる |

### D5: 系統A（未配線の `IActionBinding` 等）の扱い

| 案 | 内容 | メリット | デメリット |
|---|---|---|---|
| **E1（推奨）** | Phase8-Step1（詳細監査）で改めて全参照ゼロを確認したうえで削除する（型の形は設計の参考として本書 §3 に記録済みなので失われない） | コードベースから「動いていないのに存在する紛らわしい抽象化」がなくなり、Phase8の新設計との混同を防げる | 万が一、本書の調査で見落とした参照があった場合に備え、削除前の再確認が必須 |
| E2 | 残したまま、Phase8の新設計で作り直す型に `Obsolete` 属性を付けて共存させる | 削除の判断を先送りできる | 3系統（A・B・Phase8新設計）が一時的に併存し、かえって混乱を招く |

---

## 5. 想定ステップ構成（決定後に個別Planへ詳細化）

| Step | 内容 |
|---|---|
| Step1 | 詳細監査: 系統A参照ゼロの再確認（E1採用時）、`MapCustomAction`内のトリガー判定ロジックの完全な棚卸し（デバウンス・抑制・トグル等の仕様を1件ずつ文書化） |
| Step2 | D1〜D5の決定を踏まえた詳細設計（新インターフェース定義、モデル図03・04の更新案作成） |
| Step3 | トリガー条件クラス（D2の決定に従う）の新設・単体テスト（Controls タブの単純ケースから） |
| Step4 | SpecialActions タブの複雑なケース（組み合わせ・デバウンス・抑制）への拡張・単体テスト |
| Step5 | プロファイル読込時の `TriggerBinding` リスト構築ロジックの新設 |
| Step6 | コントローラー接続時の紐付けロジックの新設（既存 `IActionController`／`IControllerRegistry` の再利用） |
| Step7 | アクション種別ごとに `Mapping.cs` の手続き型ディスパッチから新経路へ切替（C1の方針、1種別＝1PR） |
| Step8 | 全種別の切替完了後、`Mapping.cs` 側の旧ディスパッチコードと系統A（E1採用時）を削除 |
| Step9 | 総合検証・実機確認 |

---

## 6. リスク

| リスク | 対応 |
|---|---|
| `MapCustomAction`のトリガー判定ロジック（デバウンス・抑制・トグル等）を完全に移植できず、既存の細かい挙動が変わる | Step1の棚卸しを徹底し、各仕様に対する特性化テストをStep3/4着手前に用意する |
| Controls タブの「シフト」機能（`shiftTrigger`、副ボタンによる別割り当て切替）が、単純な「AND条件」モデルに素直に収まらない可能性 | D2の設計時に個別検討が必要な既知の論点として明記し、Step2で具体設計する |
| Phase7完了前に着手すると、`Mapping.cs`のstatic結合と新設計の同時変更でリスクが積み増しになる | D4-1（独立フェーズ・Phase7完了後着手）で対応済み |

---

## 7. モデル図への影響（要確認・現時点では未変更）

本設計は、`02-Layer-Architecture-Diagram.md` の「2-b. SpecialActionトリガー判定」層と、
現在モデル化されていない「Controlsタブの基本マッピング決定（2-a）」の一部を統合するものであり、
`01`（コンポーネント依存図）・`02`（層構成図）・`03`（クラス図、`IActionBinding`等の記載追加）・
`04`（サービスライフサイクル、新設サービスの追加）**すべてに影響します**。

具体的な変更案（新設インターフェースの追加、2-a/2-bの層区分の見直し等）は、
D1〜D5の決定が確定してから提示します。**モデル図の実際の変更は、その提示内容についてご指示を
いただいてから行います。**