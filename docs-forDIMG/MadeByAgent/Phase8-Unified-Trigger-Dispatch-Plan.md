# Phase8 計画書: Controls／SpecialActions 統一トリガーディスパッチ

作成日: 2026-09-23
改訂日: 2026-09-23（決定事項D1〜D5を確定。KeyOutputActionに関するリスクと対応方針を追記）
状態: **決定事項確定。実装は Phase7 完了後に Step1（詳細監査）から着手**
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
であることが判明しました。

**2026-09-23、決定事項D1〜D5を確定しました（§4）。** 統一抽象化の**構造は未配線の系統A
（`IActionBinding`等）を土台とし、中身は稼働実績のある系統B（`Actions.Action`系）から
移植・委譲する**という方針を採用します。さらに詳細調査により、Macro／プロファイル切替／
プログラム起動の3種類は既に`IOutputAction`を実装済み（系統A・B両対応の「二重国籍」状態）で
あることが判明し、移植作業の大半は既に完了していることも分かりました。唯一Keyタイプについては
個別の対応が必要です（§4 D1の注記参照）。

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

**現状との対比（2026-09-23、実機確認で確認・裏付け済み）**: 現行の `MapCustom`/`MapCustomAction` は、
上記③に相当する処理を「毎レポートごとに、有効なアクション**名**の一覧をループし、名前ごとに
`IProfileActionProvider.GetProfileAction` で辞書を都度検索して定義を取り直す」という形で行っている
（プロファイルが変わらない限り毎回同じ結果になる、無駄な再検索）。この設計（③の`TriggerBinding`を
プロファイル読込時＝①で1回だけ解決しておく）は、この無駄を構造的に解消する。Step3の時点で
暫定キャッシュ対応を行うかどうかを比較検討し、**先送り（Phase8で本設計として一度だけ実装する）を
決定した**（`Phase6-Status.md` §6.5 K4参照）。

---

## 3. 既存の型をどう活かすか（対応表、決定確定後の内容）

| ご提案の要素 | 対応する型 | 現状（2026-09-23調査） |
|---|---|---|
| トリガー条件 | `IInputAction`（`Evaluate(DS4State) : ITriggerContext`） | 系統A。未実装（`Evaluate`の実装クラスが1つもない）。D2の決定に従い新設する |
| 処理（実行） | `IOutputAction`（構造は系統A、中身は系統Bから移植・委譲） | **Macro／プロファイル切替／プログラム起動の3種類は調査の結果、既に`IOutputAction`を実装済み**（`MacroAction`/`ProfileSwitchAction`/`LaunchProcessAction`が系統B用Adapterから内部的に呼ばれると同時に、それ自体が`IOutputAction.Execute/Stop`も実装する「二重国籍」状態）。**Keyタイプのみ`KeyOutputAction`が系統Bの`KeyAction`に委譲しておらず、独自の未検証実装になっている（要修正、D1の注記参照）** |
| トリガー＋処理の組 | `IActionBinding`（構造は系統A） | 既存`KeyActionBinding`は`SpecialAction`に直結した実装のため、Controls タブ由来のデータも扱えるよう改修が必要（Step3/4） |
| デバイスごとの登録簿 | `IActionRegistry`（系統Aをそのまま活用） | `Register`/`GetBindingsForDevice`は既存の形をそのまま使う。現在は未配線のため、Step5〜7で実際の呼び出し元を新設する |
| コントローラーとの紐付け | `IActionController`／`IControllerRegistry` | 既存（系統Bの実行側で既に使われている `KeyButtonActionController` 等）をほぼそのまま流用できる見込み |

---

## 4. 決定事項（2026-09-23確定）

以下、検討時に提示した選択肢を記録として残し、採用した案には**採用**、不採用の案には理由を付す。

### D1: 新しい統一抽象化を、系統A（`IActionBinding`系）と系統B（`Actions.Action`系）のどちらの型を土台に作るか

**決定: A2（構造は系統A、中身は系統Bをベースに改修）を採用。**

| 案 | 内容 | メリット | デメリット |
|---|---|---|---|
| A1 | 系統B（`Actions.Action`／`IActionFactory`／各Adapter）を「処理」側の土台として維持し、「トリガー条件」側だけを新設する。系統Aの型は削除する | 実際に稼働実績のあるコードを壊さずに済む | 系統Aの型（ご提案に最も近い形）を活かせない |
| **A2（採用）** | 系統A（`IActionBinding`／`IOutputAction`）の構造を土台とし、処理の中身は系統B（`Actions.Action`系）の実装をベースに改修して`IOutputAction`側に実装する | 型の名前・形（`Input`＋`Outputs`のペア）がご提案の発想に最も近い。系統Bの実績あるロジックを移植することで「動いたことのない設計」のリスクを避けられる | 系統B側（`Actions.Action`／各Adapter）は移行完了後に不要になるため、最終的に削除対象になる（D5参照） |

**A2採用に伴う追加調査（2026-09-23）と注記**:

A2を採用するにあたり、系統Bの「中身」が実際にどこまで系統A（`IOutputAction`）へ移植しやすい形になっているかを4種類のアクション（Key／Macro／プロファイル切替／プログラム起動）について個別に確認した。

- **Macro／プロファイル切替／プログラム起動の3種類（朗報）**: `MacroActionAdapter`／`ProfileSwitchActionAdapter`／`LaunchProcessActionAdapter`（系統B）は、いずれも内部で保持している`MacroAction`／`ProfileSwitchAction`／`LaunchProcessAction`に処理を委譲しているだけであり、**この内部クラス自体が既に`IOutputAction`（系統A）を実装済み**であることが判明した。`IOutputContext`（`Device`＋`OutputHandler`のみの薄いインターフェース）もこの3種類で実際に使われて過不足がないことを確認済み。つまりこの3種類については、「系統Bの中身を系統Aに移植する」作業は実質的に完了しており、Step3/4では**既存の`MacroAction`/`ProfileSwitchAction`/`LaunchProcessAction`をそのまま`IActionBinding.Outputs`に組み込むだけ**でよい。
- **Keyタイプ（要注意・対応方針決定済み）**: Keyタイプのみ事情が異なる。系統B側は`KeyAction`という実績のあるクラスが押下・トグル・リピート等の状態管理を担っているが、系統A側の`KeyOutputAction`は**`KeyAction`に委譲しておらず、独自に`TriggerContextImpl`を組み立てて`IActionController.Handle`経由で再ディスパッチする、別実装**になっている。しかもこの`KeyOutputAction`は実際の動作経路から一度も呼ばれたことがなく（呼び出し元ゼロ）、動作検証もされていない。このまま採用すると、最も利用頻度の高いKeyタイプについてだけ未検証の別ロジックを本番投入することになるため、**Step2の詳細設計およびStep3の実装時に、`KeyOutputAction`を書き直し、他の3種類と同じパターンで`KeyAction`に委譲する形に改める**ことを、本書の対応方針として確定する（§5 Step2/Step3、§6 リスク表にも反映）。

### D2: トリガー条件（`IInputAction`相当）を、Controls タブ用・SpecialActions タブ用で別クラスにするか、1つの汎用クラスにするか

**決定: B1（1つの汎用クラス）を採用。**

| 案 | 内容 | メリット | デメリット |
|---|---|---|---|
| **B1（採用）** | 「1つ以上のボタンのAND条件＋押下/解放エッジ検出＋任意のデバウンス」を表す**1つの汎用クラス**（例: `ComboTriggerInputAction`）を新設し、Controls タブの1ボタンは「AND条件の要素数が1」という特殊ケースとして扱う | Controls タブとSpecialActionsタブの違いが「ボタン数の違い」だけになり、実行時ロジックが完全に1本化される。将来ボタン数以外の差異が出ても拡張しやすい | 既存の `CheckForSpecialActionSuppression` 等の細かい仕様（抑制期間、トグル等）をすべてこの1クラスに正しく移植する必要があり、初期の移植作業の難度が上がる |
| B2 | Controls タブ用（`SingleButtonInputAction`）とSpecialActions タブ用（`ComboInputAction`）を別クラスとして用意する | 既存ロジックの移植が単純 | 「統一した」と言いつつ実行時ロジックが2系統のまま残る |

### D3: 移行の進め方（既存経路との共存期間の設け方）

**決定: C1（並行構築・段階的切替）を採用。**

| 案 | 内容 | メリット | デメリット |
|---|---|---|---|
| **C1（採用）** | 新しい統一ディスパッチを既存の `Mapping.cs` 手続き型ディスパッチと**並行して構築**し、アクション種別ごと（まずKeyのみ→Macro→Profile切替→…）に少しずつ新経路へ切り替える。切り替え済みの種別は `Mapping.cs` 側の対応コードを削除する（Strangler Fig、本プロジェクトの一貫した方針） | 既存動作を壊すリスクを最小化できる。1機能ごとに実機確認できる | 移行完了まで新旧2つの経路が一時的に共存し、コードの見通しが一時的に悪化する |
| C2 | 一括で `Mapping.cs` の該当ロジックを新経路に置き換える | 移行期間が短い | `Mapping.cs`（8,600行超、全ユーザーの入力処理の心臓部）に対する一括変更は、Phase6-Step3で既に確認された「実地確認なしの計画は簡単に外れる」というリスクが極めて高い形で顕在化する。不採用 |

### D4: この統合作業をどのフェーズとして位置づけるか

**決定: D4-1（独立フェーズ、Phase7完了後着手）を採用。**

| 案 | 内容 | メリット | デメリット |
|---|---|---|---|
| **D4-1（採用）** | 独立した **Phase8** として新設し、Phase7（`Mapping.cs` 完全instance化）の後に着手する | Phase7が対象とする「static→instance化」という関心事と、Phase8が対象とする「トリガーディスパッチの統合」という関心事が明確に分離される。Phase7でstatic結合を減らしておくことがPhase8の下地にもなる（Phase6→Phase7の関係と同じ考え方） | フェーズ数が増える |
| D4-2 | Phase7に統合し、「instance化」と「ディスパッチ統合」を同時に行う | フェーズ数が増えない | 2つの大きな関心事を同時に扱うことになり、問題発生時にどちらが原因か切り分けにくくなる |

### D5: 系統A（未配線の `IActionBinding` 等）の扱い

**決定: E2の変形（系統Aを削除・凍結せず、実際に中身を実装して本採用する）を採用。**

D1でA2（構造は系統A、中身は系統B）を採用したことに伴い、当初想定していたE1（系統Aを削除する）・
E2（系統Aを`Obsolete`として凍結し、別の新設計を作る）のどちらとも異なる帰結になる。系統Aの型自体は
削除・凍結されず、中身を実装されたうえで**本番コードとして採用**される。かわりに、移行完了後に
不要になるのは系統B側（`Actions.Action`／`IActionFactory`／各種Adapter）であり、これはD3（C1、
Strangler Fig）の一環として、種別ごとの切替が完了した時点で順次削除する（§5 Step8）。

| 案 | 内容 | 2026-09-23時点の扱い |
|---|---|---|
| E1 | 系統Aを削除する | 不採用（D1でA2を選んだため、系統Aは削除対象ではなく本採用の対象になった） |
| E2 | 系統Aを`Obsolete`にして凍結し、別途新設計を作る | 不採用（文字通りの形では取らない） |
| **E2の変形（採用）** | 系統Aの構造はそのまま活かし、中身を系統Bから移植して実装する（＝系統Aを完成させて本採用する）。系統B（`Actions.Action`系）を、移行完了後に削除する暫定コードの位置づけに切り替える | 「動いていないコードを削除する」のではなく「動いていないコードを完成させて使う」という選択。ただし系統B側の`Actions.Action`／`IActionFactory`／Adapter群は、Step7の種別ごと切替が完了するたびに削除対象として扱う（3系統が恒久的に併存する状態にはしない） |

---

## 5. 想定ステップ構成（決定確定を踏まえて詳細化。個別Planは着手時に新設）

| Step | 内容 |
|---|---|
| Step1 | 詳細監査: `MapCustomAction`内のトリガー判定ロジックの完全な棚卸し（デバウンス・抑制・トグル等の仕様を1件ずつ文書化）。あわせて系統Aの現在の呼び出し元が本当にゼロのままか（D1決定後も変化がないか）再確認する |
| Step2 | 詳細設計: `IActionBinding`／`IInputAction`／`IOutputAction`／`IActionRegistry`の具体的なメンバー確定、モデル図03・04の更新案作成。**`KeyOutputAction`を`KeyAction`に委譲する形へ書き直す設計をここで確定する**（D1注記） |
| Step3 | トリガー条件クラス（`ComboTriggerInputAction`、D2）の新設・単体テスト（Controls タブの単純ケースから）。**あわせて`KeyOutputAction`を`KeyAction`へ委譲する形に書き直し、単体テストで系統B（`KeyActionAdapter`経由）との挙動一致を検証する** |
| Step4 | SpecialActions タブの複雑なケース（組み合わせ・デバウンス・抑制）への拡張・単体テスト。Macro／プロファイル切替／プログラム起動の3種類は、既存の`MacroAction`/`ProfileSwitchAction`/`LaunchProcessAction`をそのまま`IActionBinding.Outputs`に組み込む（移植不要、動作確認のみ） |
| Step5 | プロファイル読込時の `TriggerBinding` リスト構築ロジックの新設 |
| Step6 | コントローラー接続時の紐付けロジックの新設（既存 `IActionController`／`IControllerRegistry` の再利用） |
| Step7 | アクション種別ごとに `Mapping.cs` の手続き型ディスパッチから新経路へ切替（D3=C1の方針、1種別＝1PR）。切替完了した種別から、系統B（`Actions.Action`／`IActionFactory`／該当Adapter）を削除する（D5） |
| Step8 | 全種別の切替完了後、`Mapping.cs` 側の旧ディスパッチコードと系統Bの残り（`IActionFactory`本体等）を削除する |
| Step9 | 総合検証・実機確認 |

---

## 6. リスク

| リスク | 対応 |
|---|---|
| `MapCustomAction`のトリガー判定ロジック（デバウンス・抑制・トグル等）を完全に移植できず、既存の細かい挙動が変わる | Step1の棚卸しを徹底し、各仕様に対する特性化テストをStep3/4着手前に用意する |
| Controls タブの「シフト」機能（`shiftTrigger`、副ボタンによる別割り当て切替）が、単純な「AND条件」モデルに素直に収まらない可能性 | D2の設計時に個別検討が必要な既知の論点として明記し、Step2で具体設計する |
| Phase7完了前に着手すると、`Mapping.cs`のstatic結合と新設計の同時変更でリスクが積み増しになる | D4-1（独立フェーズ・Phase7完了後着手）で対応済み |
| **（新規、D1決定に伴い判明）** Keyタイプの`KeyOutputAction`（系統A）が、実績のある`KeyAction`（系統B）に委譲しておらず、独自の未検証実装（`IActionController.Handle`経由の再ディスパッチ）になっている。呼び出し元ゼロで動作検証もされていないため、そのまま採用すると最も利用頻度の高いKeyタイプで未検証ロジックが本番投入される | Step2で`KeyOutputAction`を`KeyAction`へ委譲する形に書き直す設計を確定し、Step3で実装・単体テストにより系統B（`KeyActionAdapter`経由）との挙動一致を検証してから採用する |

---

## 7. モデル図への影響（要確認・現時点では未変更）

本設計は、`02-Layer-Architecture-Diagram.md` の「2-b. SpecialActionトリガー判定」層と、
現在モデル化されていない「Controlsタブの基本マッピング決定（2-a）」の一部を統合するものであり、
`01`（コンポーネント依存図）・`02`（層構成図）・`03`（クラス図、`IActionBinding`等の記載追加）・
`04`（サービスライフサイクル、新設サービスの追加）**すべてに影響します**。

D1〜D5の決定が確定したため、具体的な変更案（新設インターフェースのメンバー構成、2-a/2-bの層区分の
見直し、`IActionBinding`/`IInputAction`/`IOutputAction`/`IActionRegistry`のモデル図03への追加、
Step2で確定する`KeyOutputAction`の委譲構造の反映等）は、Step2（詳細設計）で具体的に作成します。
**モデル図の実際の変更は、Step2でのご提示内容についてご指示をいただいてから行います。**