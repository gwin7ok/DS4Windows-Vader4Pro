# Phase6-Step10-3b 計画書: アクションの名前変更・削除を、全プロファイルへ反映する（機能追加）

作成日: 2026-10-14  
状態: **完了（2026-10-14）**（Step10-3b-1〜3b-4、ビルド・テスト・実機確認済み）  
位置づけ: DI 化（動作を変えない）とは別の、機能追加。Step10-3 の範囲に混ぜず、別のコミットで行う（R3＝B。2026-10-14 承認）  
準拠: `.github/copilot-instructions.md` §2.2、§2.3、§3.3、§4

---

## 1. 経緯
- アクションの名前を変えて保存すると、現在は、編集中のプロファイル（メモリ上のリスト）の参照だけが新名に変わる。他のプロファイルの XML（`<ProfileActions>`）には旧名が残り、そのプロファイルを読み込むと、アクションが黙って無効になる。
- アクションを一覧から削除した場合も、他のプロファイルに削除済みの名前が残る。
- ユーザーの要望（2026-10-14、R1＝B、R2＝B）: 名前変更と削除を、存在するすべてのプロファイルへ反映し、接続中のコントローラーに適用されているプロファイルは再適用する。**再適用のロジックは、既存の「プロファイル保存時の再適用」を再利用し、複数箇所に実装しない。**

## 2. 現行コードの調査結果
- **既存の再適用のロジック（再利用する）**:
  - `MainWindow.SyncProfileListAndControllers(oldProfile, newProfile)`（4 段階の同期）。プロファイル一覧を `ProfileListHolder.Refresh()` で読み直し、`Global.ProfilePath[i]` が対象のプロファイルと一致するスロットだけに、`Global.ApplyProfileToSlot(i, name, Manual)` で再適用する。
  - 呼び出し元は、`Editor_ProfileSaved`（`ProfileEditor.ProfileSaved` イベントの受け口）のみ。
- **名前変更**: `SpecialActionEditor`（`Global.RemoveAction`＋`SaveAction`）→ `ProfileEditor` の `Saved` ハンドラーが、編集スロットのメモリ上の `ProfileActions` だけを置換する。
- **一覧からの削除**: `SpecialActionsListViewModel.RemoveAction` は、`ISpecialActionRepository.RemoveAction`（メモリ上のリストから外すだけ。ファイルへ保存しない）を呼ぶ。
  - **要確認（置換前からの不具合の疑い）**: コード上、`Actions.xml` へ保存される経路は、その後に別のアクションを保存したとき（`SaveActionNew` が、メモリ上のリスト全体を書き出す）だけ。他に保存しないまま再起動すると、削除したアクションが復活する可能性がある。実機で確認し、不具合であれば修正の可否を確認する（本計画の削除の反映は、実際に削除（保存）されていることが前提のため、この修正を含める案とする）。

## 3. 設計案

### 3.1 参照の書き換え（ファイル）
- `IProfileRepository` に次の 2 つを追加する（プロファイル名単位の操作を集約する窓口のため）。実装は、プロファイルのフォルダ内の `*.xml` を走査して `<ProfileActions>` を書き換える。
  - `IReadOnlyList<string> ReplaceActionReference(string oldName, string newName)`
  - `IReadOnlyList<string> RemoveActionReference(string name)`
  - 戻り値は、書き換えたプロファイル名の一覧。
- 動作:
  - `/` で区切った項目と、完全一致で比較する（部分一致はしない）。
  - 一致したファイルだけを保存する。他のファイルは触らない。
  - `XmlIoLock`（同一 XML 排他ロック）を使う。ファイル 1 つの失敗が他のファイルの処理を止めないようにし、失敗は警告ログに記録する。
  - `[DI]` 接頭辞で、書き換えたプロファイル名を記録する。

### 3.2 再適用（既存のロジックを再利用）
- `MainWindow.SyncProfileListAndControllers` を、複数のプロファイル名を受け取れる形に一般化する（既存の呼び出しの動作は変えない）。再適用の実装は、この 1 か所だけにする。
- `ProfileEditor` に、書き換えたプロファイル名の一覧を通知するイベント（仮称 `ProfilesUpdatedExternally`）を追加し、`MainWindow` が受けて `SyncProfileListAndControllers` を 1 回呼ぶ。通知の経路を足すだけで、再適用のロジックは増やさない。

### 3.3 呼び出し位置
- 名前変更: `ProfileEditor` の `EditActionBtn_Click` の `Saved` ハンドラー（旧名と新名が分かる箇所）。
- 削除: `ProfileEditor.RemoveActionBtn_Click`（`specialActionsVM.RemoveAction(item)` の後）。実際の削除（`Actions.xml` への保存）は、Step10-3 で追加する `RemoveActionAndSave` を使う。
- 編集中のプロファイル自身のファイルも対象に含める。キャンセルしても、`Actions.xml` と整合する。

## 4. マイクロステップ（案）
- **Step10-3b-1**: 契約（`IProfileRepository` の 2 メソッド）と実装、契約テスト（複数のプロファイルを書き換える／無関係なプロファイルは書き換えない／部分一致はしない／ロック）。
- **Step10-3b-2**: `ProfileEditor` の呼び出しとイベント、`MainWindow.SyncProfileListAndControllers` の一般化。ガードテスト（再適用の実装が 1 か所であること）。
- **Step10-3b-3**: 一覧からの削除の保存（`RemoveActionAndSave` の利用）。置換前からの不具合の確認結果に応じて確定する。

## 5. 実機確認（案）
- 2 つ以上のプロファイルが同じアクションを使っている状態で、アクションの名前を変え、他のプロファイルを開いても、新名で有効であること。
- 接続中のコントローラーに適用中のプロファイルが、名前変更の直後に再適用されること（新名のアクションが発火すること）。
- アクションを削除し、他のプロファイルの一覧から消えること。再起動しても復活しないこと。

## 6. リスク
- **再適用の副作用**: 実行中の入力状態がリセットされる。既存の「保存時の再適用」と同じ動作で、編集操作の直後のため許容する。
- **ファイルの書式**: XML を読み込み直して保存するため、空白などの書式が変わる可能性がある（`CreateStdActions` と同じ）。値は変わらない。
- **削除の範囲**: 名前が似た別のアクションを巻き込まないよう、完全一致で比較する。

---

## 7. 改訂（2026-10-14 ユーザーの追加要望・実機の観察）。本章が §3.3・§4・§5 より優先する

### 7.1 実機の観察
- アクションの「除去」ボタンを押し、プロファイルの編集をキャンセルして、編集画面を開き直すと、アクションは一覧から消えている。ただしログに `invalid special action…` が出る。
- 解釈: `Actions.xml`（またはメモリ上のリスト）からは消えているが、プロファイルの XML の `<ProfileActions>` には名前が残っている。
- 注: 開き直しは同じプロセス内の確認のため、`Actions.xml` へ保存されているかは、この観察だけでは確定しない（再起動での確認が必要）。いずれにしても、削除時に保存する設計（`RemoveActionAndSave`）で解消する。

### 7.2 要望
1. 「除去」ボタンを押したとき、確認のプロンプトを出す。
2. 除去するときは、`Actions.xml` だけでなく、すべてのプロファイル設定文書（`*.xml`）からも、そのアクションの参照を消す。
3. 接続中のコントローラーに適用されているプロファイルの場合は、保存時と同じ再適用が走る（既存の再適用の経路を再利用する。ロジックを増やさない）。
4. 構造は、なるべくシンプルにする。
5. 以前は、`Actions.xml` に無く、プロファイルの XML にだけ残っているアクションが、スペシャルアクション一覧に「トリガー・アクション欄が空、名前がグレー」で表示され、そこから消せた。現在は表示されないため、再度表示されるようにする。

### 7.3 決定（設計）
- **除去の流れ（`ProfileEditor.RemoveActionBtn_Click` の 1 か所に集約）**:
  1. 確認のプロンプト（例: 「アクション '<名前>' を削除します。すべてのプロファイルからも取り除かれます。よろしいですか？」。はい／いいえ）。いいえなら何もしない。
  2. `ISpecialActionRepository.RemoveActionAndSave(name)`（Step10-3 で追加）で、`Actions.xml` から削除して保存する。Actions.xml に無い（グレー表示の）項目の場合は、何もしない（結果は無視する）。
  3. `IProfileRepository.RemoveActionReference(name)` で、すべてのプロファイル XML の `<ProfileActions>` から、完全一致の名前を取り除く。戻り値は、書き換えたプロファイル名の一覧。
  4. 一覧から項目を外す（現行どおり）。
  5. 書き換えたプロファイルの一覧を、`ProfileEditor` のイベント経由で `MainWindow` へ通知し、既存の `SyncProfileListAndControllers`（保存時と同じ再適用）を 1 回呼ぶ。
- **ユーザー要望 3 の「プロファイルの保存処理が走る」ことの解釈**: プロファイルの XML を直接書き換えたうえで、保存時と同じ「一覧の読み直し＋該当スロットへの再適用」の経路を呼ぶ。各プロファイルを、いったんスロットへ読み込んで保存し直す方式は、読み込みがスロットの状態を変えるため、副作用が大きく採らない。
- **グレー表示の復活**:
  - `SpecialActionsListViewModel.LoadActions` で、リポジトリのアクション一覧に加えて、読み込んだプロファイルの `ProfileActions` に含まれ、一覧に無い名前を、ダミーの `SpecialAction`（トリガー・アクションは空）で `IsMissing = true` の項目として追加する（`36d124a6` のリファクタで失われた処理。`40011c12` の実装と同じ）。チェック状態は、プロファイルで有効なため ON。
  - グレーの項目の名前の色は、既存の `MissingToGrayConverter` が使われている。XAML の変更は不要の見込み。
  - `SyncProfileActionsString` が `IsMissing` の項目を除外している現行の動作により、プロファイルを保存すると、存在しないアクションの参照は取り除かれる（現行どおり）。
- **名前変更**（元の依頼）は、§3.1・§3.2 のとおり、参照の置換を全プロファイルへ反映し、同じ通知で再適用する。

### 7.4 マイクロステップ（改訂）
- **Step10-3b-1**: `IProfileRepository` の 2 メソッド（`ReplaceActionReference`、`RemoveActionReference`）と実装、契約テスト。
- **Step10-3b-2**: グレー表示の復活（`LoadActions`）とテスト。
- **Step10-3b-3**: `ProfileEditor` の除去の流れ（確認プロンプト、保存付きの削除、参照の取り除き、通知）、`MainWindow.SyncProfileListAndControllers` の一般化。
- **Step10-3b-4**: 名前変更時の参照の置換と通知。ガードテスト、文書。

### 7.5 実機確認（改訂）
- 除去ボタンの確認プロンプトで「いいえ」を選ぶと、何も変わらない。「はい」で削除される。
- 2 つ以上のプロファイルが同じアクションを使っているとき、除去後に、他のプロファイルの編集画面にも、そのアクションが無く、ログに `invalid special action` が出ない。
- 接続中のコントローラーに適用中のプロファイルが、除去の直後に再適用される。再起動しても復活しない。
- プロファイルの XML にだけ名前が残っている状態（`Actions.xml` を編集して作る）で、一覧にグレーの名前で表示され、除去ボタンで消せる。

---

## 8. 進捗（2026-10-14）
- **Step10-3b-1（契約と実装、契約テスト）**: 実装済み・ビルド、テストの結果待ち。
  - `IProfileRepository.ReplaceActionReference(oldName, newName)`／`RemoveActionReference(name)` を追加し、`ProfileRepository` に実装した。
  - 動作: プロファイルのフォルダの `*.xml` を走査し、`DS4Windows/ProfileActions` を `/` で区切った項目と、完全一致（大文字小文字を区別）で比較する。一致したファイルだけを `XmlIoLock` の下で保存し、書き換えたプロファイル名（拡張子なし）を返す。新名が同じプロファイルに既にある場合は重複させない。ファイル 1 つの失敗（読めない XML など）は、`[DI]` の警告ログに記録して、他のファイルの処理を続ける。空白・同名の指定は、何もしない。
  - テスト: `Phase6Step10GroupThreeBContractTests`（全プロファイルの書き換え、部分一致では書き換えない、大文字小文字の区別、重複の防止、無変更時にファイルを触らない、削除、フォルダが無い場合、読めない XML があっても続行）。
  - この時点では、画面からの呼び出しは未接続（3b-3、3b-4 で接続する）。契約に、接続予定の Step をコメントで記載した。
- **Step10-3b-1**: 完了（ビルド・テスト実行成功）。
- **Step10-3b-2（グレー表示の復活）**: 実装済み・ビルド、テスト、実機確認の結果待ち。
  - `SpecialActionsListViewModel.LoadActions`: `Actions.xml` に無く、プロファイルの `ProfileActions` にだけ残っている名前を、ダミーの `SpecialAction`（トリガー・アクション欄が空）で `IsMissing = true`・`Active = true` の項目として追加する。名前の比較は大文字小文字を区別しない（`LoadProfileActionsString` と同じ）。
  - 取得元: 引数の `profileActionsString` があればそれ、無ければ `IProfileRepository.ProfileActions[deviceNum]`。
  - 保存時の扱い: チェックしたままなら、プロファイルに残る（`GetEnabledActionNames`）。チェックを外して保存すれば取り除かれる（`40011c12` の仕様どおり）。画面上の同期文字列（`ProfileActions`）は、読み込み直後はプロファイルの内容をそのまま保持し、チェックの切り替え後（`SyncProfileActionsString`）は、存在しないアクションを含めない（現行どおり）。
  - 見た目: 既存の `MissingToGrayConverter` で名前がグレーになる（XAML の変更なし）。
  - テスト: `SpecialActionsListViewModelMissingActionTests`（グレー表示、大文字小文字の重複排除、チェック解除による保存対象と同期文字列からの除外）。
- **Step10-3b-2**: 完了（ビルド・テスト実行成功、実機確認問題なし）。
- **Step10-3b-3（「除去」ボタンの流れ）**: 実装済み・ビルド、テスト、実機確認の結果待ち。
  - `ProfileEditor.RemoveActionBtn_Click`: 確認のプロンプト（はい／いいえ。「いいえ」なら何もしない）→ `SpecialActionsListViewModel.RemoveAction`（`Actions.xml` から保存付きで削除。グレーの項目は一覧から外すだけ）→ `IProfileRepository.RemoveActionReference`（全プロファイルの XML から参照を削除）→ 書き換えたプロファイル名を `ProfilesUpdatedExternally` で通知。
  - `SpecialActionsListViewModel.RemoveAction`: 従来はメモリ上のリストから外すだけ（`Actions.xml` へは、その後の別のアクションの保存まで反映されなかった）だったが、`RemoveActionAndSave`（`Actions.xml` へ保存）を使うよう変更した（`IsMissing` の項目は呼ばない）。
  - `MainWindow`: `SyncProfileListAndControllers` に `targetProfiles`（複数のプロファイル名）を追加した。既存の呼び出し（保存・リネーム）の動作は変えない。`Editor_ProfilesUpdatedExternally` は、この同じ経路を呼ぶだけで、再適用（`Global.ApplyProfileToSlot`）の実装箇所は増えていない（ガードテストで 2 箇所＝既存のまま、を検査）。
  - テスト: 除去が `Actions.xml` とメモリの両方から削除すること、グレーの項目ではファイルを書き換えないこと、除去の流れ（確認・参照の削除・通知・再適用の経路）のソース検査。
  - 名前変更時の反映は 3b-4。
- **Step10-3b-3**: 完了（ビルド・テスト実行成功、実機確認問題なし）。
- **Step10-3b-4（名前変更時の参照の置換と通知）**: 実装済み・ビルド、テスト、実機確認の結果待ち。
  - `ProfileEditor` の編集画面の `Saved` ハンドラー（`EditActionBtn_Click`）: 旧名と新名が異なるとき、`IProfileRepository.ReplaceActionReference(oldName, newName)` で全プロファイルの XML の参照を新名へ置換し、書き換えたプロファイル名を `ProfilesUpdatedExternally` で通知する。再適用は、3b-3 と同じ `MainWindow.SyncProfileListAndControllers` の 1 か所の経路。編集中のプロファイルのメモリ上の参照の置換（従来の処理）は、そのまま残している。
  - 編集中のプロファイル自身のファイルも置換の対象に含まれるため、保存せずに編集をキャンセルしても、`Actions.xml` と整合する。
  - ガードテスト: 名前変更も同じ通知の経路を使うこと（`ReplaceActionReference(` の呼び出しの検査）を追加した。
- **Step10-3b の完了条件**: 3b-4 の実機確認（下記）が通ること。
  - 2 つ以上のプロファイルが同じアクションを使っている状態で、アクション名を変えて保存し、他のプロファイルを開いても新名で有効であること（ログに `invalid special action…` が出ない）。
  - 接続中のコントローラーに適用中のプロファイルが、名前変更の直後に再適用され、新名のアクションが発火すること。
  - 名前を変えずに内容だけ変えて保存したときは、他のプロファイルの XML が書き換わらないこと。
- **Step10-3b-4 の実機確認で判明した追加事項（2026-10-14）**: 名前を変えず、内容だけ変えて保存したとき、接続中のコントローラーでは、変更前の内容のアクションが実行される。
  - 原因: アクションの保存（`Global.SaveAction`）は、`Actions.xml` の読み直しでメモリ上のアクション一覧を更新するが、各スロットのプロファイルは、適用時にアクションの実体を取り込んで保持している（`profileActionDict`）。再適用されるまで、古い内容のまま。プロファイルの XML の書き換えが無いため、これまでは再適用の通知も無かった。置換前からの動作（機能追加の過程で判明した既存の制約）。
  - 対応（ユーザーの要望、案 A: 既存の再適用の経路を使う）: 名前が同じで内容だけ変わった場合も、そのアクションを使っているプロファイル（接続中のスロットに適用中のもの）を、`ProfilesUpdatedExternally` で通知し、`MainWindow.SyncProfileListAndControllers`（保存時と同じ再適用）で再適用する。
  - 追加した契約: `IProfileRepository.GetAppliedProfilesUsingAction(actionName)`（スロットのメモリ上の `ProfileActions` を調べる。ファイルは読まない）。
  - 案 B（再適用せず、スロットのキャッシュだけを更新する）は、`ActionManager` の事前確保など実行時の内部状態に触れるため採らなかった。
- **Step10-3b-4**: 完了（ビルド・テスト実行成功、実機確認問題なし。内容だけ変わった場合の再適用の追加を含む）。

## 9. 完了報告（2026-10-14）
- **Step10-3b は完了**。機能追加（DI 化の範囲外）として、Step10-3 とは別のコミットで実施した。
- **成果**:
  - 「除去」ボタンに確認のプロンプトを追加し、`Actions.xml` と、すべてのプロファイルの XML から、そのアクションを取り除くようにした。
  - アクションの名前変更を、すべてのプロファイルの XML へ反映するようにした。
  - 名前が同じで内容だけが変わったときも、そのアクションを使っているプロファイルを再適用し、接続中のコントローラーへ新しい内容を反映するようにした。
  - `Actions.xml` に無く、プロファイルの XML にだけ残っている名前を、一覧にグレーで表示する機能（`40011c12`）を復活させた。
  - 一覧からの「除去」が `Actions.xml` へ保存されない（メモリ上のリストだけ）動作を、保存付きの削除（`RemoveActionAndSave`）へ変更した。
- **再適用の実装箇所**: `MainWindow.SyncProfileListAndControllers` の 1 か所のまま（保存時、除去、名前変更、内容変更のすべてが、この経路を呼ぶ）。
- **追加した契約**: `IProfileRepository.ReplaceActionReference`／`RemoveActionReference`／`GetAppliedProfilesUsingAction`（いずれも、Step10-3b の範囲内で使用）、`ISpecialActionRepository.SaveAction`／`RemoveActionAndSave`（Step10-3-1）。
- **追加したテスト**: `Phase6Step10GroupThreeBContractTests`、`SpecialActionsListViewModelMissingActionTests`、`Step10GroupThreeGlobalReferenceGuardTests` の除去・名前変更の流れの検査。
- **未実施・引き継ぎ**:
  - 不要になる可能性のある既存の `ISpecialActionRepository.RemoveAction`（メモリのみ）は、呼び出しが 1 件も残っていない可能性がある。Step10-4 の全域再走査で、§2.4 に従って要否を確認する。
  - SA の「キー押下」でコントローラーのボタンを割り当てられない件は、Step10-3c で調査する（`Phase6-Step10-3-Plan.md` §12）。
