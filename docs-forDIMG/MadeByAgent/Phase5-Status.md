# Phase 5 - Status（Step14/Step15集約版・Phase5クローズ）

作成日: 2026-09-18
最終更新日: 2026-09-18（Step15完了・Phase5クローズを追記）
対象ブランチ: `For-DI-migration-work`
位置づけ: 原計画書 `Phase5-Step14-Plan.md`（総合自動テストと実機検証）の完了報告であると同時に、Step14の名の下で実施された多数の実機不具合調査・修正作業の集約記録。あわせてStep15（Legacy shim棚卸し・削除判断）の結果を反映し、Phase5全体のクローズを記録する。
更新理由: `Phase5-Status.md`が2026-09-05時点の記載のまま更新されておらず、その後の実装・実機検証の実態を反映していなかったため、実コード・各個別文書と突き合わせて棚卸しし、本報告書として新規作成した。その後Step15の完了に伴い本文書を更新した。

---

## 1. 原計画（Step14-1〜14-4）に対する結果

| タスク | 計画内容 | 結果 |
|---|---|---|
| Step14-1 | 総合自動テスト網羅実行（169件） | ✅ 達成。以降の各修正コミットでも継続して`dotnet build`/`dotnet test`のグリーンを都度確認しており、テスト件数は169件から大幅に増加している（サブ設定完全性検証テスト、通知経路ブリッジテスト等を新規追加）。 |
| Step14-2 | 実機動作確認チェックリストの確定 | 🔶 チェックリスト自体（`Phase5-Step14-RealDevice-Verification-Checklist.md`）はテンプレートのまま未記入。ただし、想定していた検証観点（Halt保証、スレッド安全性、メモリリーク等）は、後述の個別不具合調査の過程で実質的に検証されている。 |
| Step14-3 | 実機総合動作検証（実機CP4） | 🔶 単発の総合検証セッションとしては未実施だが、複数回にわたる実機テストにより、個別の重大不具合（後述）は発見・修正・再検証済み。 |
| Step14-4 | 完了報告書作成・Status更新 | ✅ 本報告書および`Phase5-Status.md`の更新をもって達成。 |

**結論**: 原計画が想定していた「まとめて1回、網羅的に確認する」という進め方ではなく、**実機で問題が見つかるたびに調査→修正→再検証を繰り返す反復的な進め方**になった。結果として、原計画のチェックリスト消化以上の実質的な検証・改善が行われている。

---

## 2. Step14の名の下で実施された主要な実機不具合調査・修正（時系列）

### 2.1 プロファイル切替カスケードループの解決
- **症状**: スペシャルアクションによるプロファイル切替で、1回のトリガー成立に対し複数回・連鎖的に切替が発生する。
- **原因（3点複合）**:
  1. WPF ComboBoxの双方向イベントによる無限ループ。
  2. アクションチェーン（`DispatchNextActions`）による他プロファイルへの連鎖切替の暴発。
  3. `RequiresFreshPressAfterReset`ガードが、プロファイル切替時の瞬間的なゼロクリアで1フレームのみで解除されてしまう。
- **対応**: `CompositeDeviceModel.suppressSelectedIndexChanged`統合、`ProfileActionChainService`でのProfile種別チェーンスキップ、`ActionInstanceState.FreshPressReleaseHoldMs = 80`による連続80ms未成立でのガード解除。
- **実機確認**: ✅ 完了・解消確認済み。
- 詳細: `Phase5-Step14-Notification-Route-Consolidation-Step-Plan.md`【追記】。

### 2.2 プロファイル適用通知の二重表示バグ
- **症状**: 通知レベル「すべて」の場合、SpecialAction経由のプロファイル切替のみ、独自ウィンドウ＋トースト、またはトースト2回の二重表示が発生。
- **原因**: `ProfileApplicationService.ApplyFromAction`が、Halt短縮対応の過程で既存の自動通知経路とは別に独立した`LogToTray`呼び出しを追加していた。
- **対応**: 独立呼び出しを削除し、`LogProfileChanged`経由の単一経路に一本化。
- **実機確認**: ✅ 完了・仕様通り（独自ウィンドウON→ウィンドウのみ／OFFかつすべて→トーストのみ）の動作を確認済み。
- 詳細: 同上文書【追記2】。

### 2.3 コントローラー接続中のWindows全体重量化
- **症状**: コントローラー接続からプロファイル読込後、切断するまでの間ずっとWindows全体が重くなり、マウスポインタの追随が悪化。
- **原因**: `AppNotificationRegistration.ShowModernToast`が、呼び出しのたびに`AppDomain.CurrentDomain.GetAssemblies()`による全アセンブリ列挙を含むリフレクション解決をキャッシュなしで実行し、これがUIスレッド上で同期実行されていた。Phase5-Step14の通知経路統合により全通知がこの経路を通るようになったことで、接続中に継続的に発生するようになった。
- **対応**: 解決した`Type`/`MethodInfo`/`PropertyInfo`を`static`キャッシュ化。
- **実機確認**: ✅ 完了・重さが一切発生しなくなったことを確認済み。
- 詳細: 同上文書【追記3】。

### 2.4 プロファイル設定の同期・永続化統合（全16サブ設定の宣言的配線化）
- **背景**: 一部の設定項目（サブ設定オブジェクト）がプロファイル設定文書に保存されない不具合。
- **対応**:
  - `ProfileSubSettingBase`基底クラスを新設し、9カテゴリ（後に16項目まで拡充）のネスト設定クラスに適用。
  - `WireSubSettingsEvents`を、手書き配線からラベル＋アクセサ関数の宣言的リスト（`SubSettingDescriptors`）＋ループ方式へリファクタリング。
  - リフレクションによる完全性検証テストを追加し、新規サブ設定の配線漏れを構造的に検知できるようにした。
- **派生して発見・修正した問題**: `RumbleSettings`（DualSenseランブル設定）が、`BackingStore`ではなく`ProfileSettingsService`内の独立した並行コピー配列として実装されていたため、プロファイル切替時にXMLから読み込んだ新しい値が反映されず、実機に送信されるランブル強度が古い値のまま変わらないという実害あるバグを発見。`RumbleSettings`を他の15項目と同じく`BackingStore`側の実体に統合し直すことで解決。
- **実機確認**: ✅ 完了。
- 詳細: `Phase5-Step14-Profile-Configuration-Synchronization-and-Unified-Persistence-Plan.md` / `-Completion-Report.md`。

### 2.5 その他のIssue系対応
- **Issue7（Emulated Controller整合性是正）**: 実装完了（タスク1・2・3・6・7）。派生した(c)-1・(c)-2も実装完了。**(c)-3（クリーンビルド・全テスト実行の最終確認）は明示的な実施記録が残っていないが、以降の複数コミットで`dotnet build`/`dotnet test`のグリーンが継続して確認されているため、実質的には満たされていると判断する。**
- **Issue8-1（トリガー成立判定の仕様適合性）／Issue8-1(3)（出力抑制期間是正）／Issue8-3（MultiAction連射誤発火）**: いずれも実装・実機確認まで完了。
- **サブ設定イベント冪等化・DeltaAccel配線・ProfileActionsインターフェース整備**: 完了。

---

## 3. 未完了として引き継ぐ項目

すべてが解決したわけではなく、以下は本報告書の時点で**未着手・未確認のまま引き継ぐ**。

| 項目 | 状態 | 内容 |
|---|---|---|
| `Phase5-Step14-ProfileSync-And-ApplyUnified-Status.md` フェーズG | 承認済み・未着手 | `Global.ApplyProfileToSlot`を`IProfileApplicationService`への委譲へ書き換える対応。現状`ApplyProfileToSlot`は`isTemp`を常に`false`で渡す既知の副作用も残る（本報告書作成時点で未修正）。 |
| `Phase5-Step14-RealDevice-Verification-Checklist.md` | テンプレートのまま未記入 | 個別の実機検証はUI/永続化/通知/カスケード等で実施済みだが、この汎用チェックリスト形式への転記は行われていない。 |
| Phase5-Step15（Legacy shim棚卸し・削除判断） | ✅ **完了（2026-09-18）** | Step13完了報告書が引き継いだ3点のうち、①`FindValidProfile`相当は対応不要（解消済みと確認）、②`ProfileList`クラス内`Global.appdatapath`参照とUI層の`Program.rootHub`置換漏れ（`ControllerReadingsControl.xaml.cs`／`RecordBox.xaml.cs`／`ProfileEditor.xaml.cs`）を解消、③呼出元0件シムの網羅的物理削除はPhase6-Step1の監査スコープと重複するためStep15では対象外と判断（範囲確定は完了）。詳細は`Phase5-Step15-Completion-Report.md`参照。 |

---

## 4. 完了判定（Step14時点）

- [x] Step14が計画した自動テストは、原計画の169件を超える規模で継続してグリーンを維持している。
- [x] 実機での重大な不具合（カスケードループ、通知二重表示、Windows全体重量化、プロファイル永続化・ランブル同期）は発見・修正・再検証済み。
- [ ] 汎用チェックリスト形式での実機CP4記録は未実施（上記個別記録で代替されている状態）。

Step14は「原計画のチェックリスト消化」という意味では厳密には完了していないが、**それを上回る実質的な不具合修正と実機検証を伴って区切りに達した**と判断し、区切りとした。

---

## 5. Step15の結果（2026-09-18）

`Phase5-Step15-Plan.md`に従い、Step15-1（監査）で判明した「単純な移行漏れ」「フィールドがあるのに未使用」の箇所を、既存の確立済みDIフォールバックパターンに揃える形でコード修正した（Step15-2-a〜d）。詳細な変更内容・ファイル別対応は`Phase5-Step15-Completion-Report.md`を正本とする。

- `ProfileList.cs`・`AutoProfiles.xaml.cs`の`Global.appdatapath`直接参照: 0件化（達成、想定より2箇所多い計4箇所で確認・対応）。
- `ControllerReadingsControl.xaml.cs`（9箇所）・`RecordBox.xaml.cs`（2箇所）・`ProfileEditor.xaml.cs`（1箇所）の`Program.rootHub`直接呼び出し: DIフォールバック初期化行を除き0件化（達成）。
- `dotnet build`/`dotnet test`の実行および対象4画面の実機確認は、コード変更を行った環境の制約によりユーザー側での実施待ち（未検証のまま区切り）。

**未実施のまま引き継ぐ事項**:

| 項目 | 状態 | 内容 |
|---|---|---|
| Step15コード変更後のビルド・自動テスト・実機確認 | 未実施 | `Phase5-Step15-Completion-Report.md` §3参照。ユーザー環境での実施が必要。 |
| `Phase5-Step14-ProfileSync-And-ApplyUnified-Status.md` フェーズG | 承認済み・未着手 | `Global.ApplyProfileToSlot`を`IProfileApplicationService`への委譲へ書き換える対応。`isTemp`を常に`false`で渡す既知の副作用も残る。Phase6以降で扱う。 |
| `Phase5-Step14-RealDevice-Verification-Checklist.md` | テンプレートのまま未記入 | 個別の実機検証で代替。 |
| `Global`全体（約500メンバー）の網羅監査・呼出元0件シムの物理削除 | Phase6-Step1へ委譲 | Step15の対象外と明確化済み（`Phase5-Step15-Plan.md` §6）。 |

---

## 6. Phase5クローズ宣言

Step14（総合検証・実機不具合修正）およびStep15（Legacy shim棚卸し・UI層の移行漏れ解消）の完了をもって、**Phase5「DIサービス内部Legacy経路監査と責務分離」を本書時点でクローズする**。

- Phase5全体の実働: Step1〜13で8日（2026-09-02〜2026-09-09）＋Step14〜15（2026-09-09〜2026-09-18）。
- 残る未検証事項（本節§5の表）はPhase5の再オープン理由ではなく、通常のフォローアップ事項として扱う。
- 次フェーズは`DI-App-Wide-Migration-Plan.md` §6.8に定義された**Phase6（残存Global実利用箇所の解体）**であり、着手にはStep15のビルド・実機確認完了（ユーザー側実施）を前提とする。