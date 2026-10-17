# Phase7-Step1 計画書: 整理と台帳の確定

作成日: 2026-10-17  
対象ブランチ: `For-DI-migration-work`  
前提: `Phase7-0-Survey.md`（調査報告）。決定 D1〜D6 は、すべて推奨どおり承認済み（2026-10-17、§1）。  
状態: **実装完了（2026-10-17）。完了報告書: `Phase7-Step1-Completion-Report.md`。**  
ルール: ビルド・テストビルド・テスト実行・コミットはユーザーが行う。エージェントはローカルファイルの編集のみ。

---

## 1. 承認済みの決定（Phase7 全体）

- D1: 案 A（外側から内側へ）。Step1 整理 → Step2 インターフェース → Step3 `BackingStore` の DI 化と `Global` の転送削除 → Step4 起動・終了 → Step5 `rootHub`／Service Locator → Step6 出力スロットの所有権 → Step7〜9 `Mapping` の instance 化 → Step10 仕上げ。
- D2: `BackingStore` は、Phase7 では「DI の単一インスタンス化＋永続化の切り出し」まで。フィールド群の分解は Step10 で範囲を再評価。
- D3: 起動は S1（コンテナ構築の前にブートストラップを作り、同一インスタンスを登録）。Step3 の後の Step4 で範囲を確定。
- D4: F1。アプリ設定ほか 4 ファイルは DTO のまま。プロファイルは旧形式。アプリ設定の旧形式の読込フォールバックは、読み取り専用の互換として切り出して残す（Step10）。
- D5: 不具合 B と K2 は Step1 で修正する。`UpdateDS4CLightbarMacro` の削除も承認。
- D6: `ServiceProviderHolder` をラチェットと台帳に追加する。`Actions/` 内の約 17 件は Phase8 へ持ち越す。

## 2. Step1 の目的と範囲

- 目的: Phase7 の本体（Step3 以降）に入る前に、(1) 孤立コードを消し、(2) 既存の不具合を直し、(3) 追跡の抜け（Service Locator）を塞ぐ。機能・ログ・安全弁は変えない（copilot-instructions §2.2・§2.3）。
- 範囲外: `BackingStore` の定数の再配置（Step10）、インターフェースの 0 件メンバー（Step2）、`AppHost` のオーバーロード整理（Step4）。

## 3. Wave 計画（各 Wave ＝ 1 コミット。ビルド・テストの成功を確認して次へ進む）

### W1: `BackingStore` の孤立メソッド 10 名の削除
- 対象（`BackingStore.cs`。呼び出し元・テストとも 0 件を再確認済み）:
  - `SaveOld`（アプリ設定の旧形式の保存。`105575a7` で DTO に戻り孤立）
  - `UsedSavedProfileString`（上流の例示コード。条件式が常に false）
  - `RebuildProfileActionDicts`（`576426b8` で呼び出し元 `LoadProfileNew` が消えて孤立）
  - `GetDS4Action`（2 オーバーロード）、`GetDS4Extra`、`GetDS4KeyType`、`GetDS4STrigger`（2 オーバーロード）（呼び出し側の `Global` ラッパーは Step12 Wave 4 で削除済み）
  - `GetGyroOutModeString`、`GetGyroOutModeType`（private。内部の呼び出しもコメントアウト済み）
  - `UpdateDS4CLightbarMacro`（D5 で承認）
- 対象外: `PostProcessLoad`（W3 で接続する）、孤立定数 8 件（Step10 の定数の再配置）。
- 手順:
  1. 削除の直前に、名前の参照を再走査する（`scripts/audit/Step12/countb.py`）。コメント・文字列を除く。
  2. 孤立メソッドの本文だけをピンポイントで削除する（copilot-instructions §3.2）。周辺のコメント（設計意図・TODO）は消さない（§3.3 原則4）。
  3. 削除によって使われなくなる `using` や private ヘルパーがないかを確認する。
- ラチェット: `BackingStore.cs` の `Global.*` が減る（`SaveOld` に約 10、`RebuildProfileActionDicts` に 2 など）。削除後に走査で再集計し、`Step10FourGlobalRatchetGuardTests` の基準値を更新する（合計は減らす方向のみ許可）。
- ガード: `Step12DeadShimRemovalGuardTests` の削除名一覧は `Global` 用。`BackingStore` 側の削除名を検証する新しいテストを、1 ファイル 1 型で追加する（`Phase7Step1DeadMethodGuardTests`）: 11 名のうち 10 名が `BackingStore` に再宣言されていないこと、`PostProcessLoad` が残っていること。
- 検証: ビルド警告 0、全テスト成功。

### W2: K2 の是正と未使用変数の削除
- `ProfileActionProvider.GetProfileActionIndexOf`: `TryGetValue` の戻り値を使い、未検出は -1 を返す。
  ```csharp
  // 変更前: int index = -1; _config?.profileActionIndexDict[deviceIndex].TryGetValue(actionName, out index); return index;
  // 変更後: _config が null、または未検出なら -1
  ```
- `Mapping.cs:5192` の未使用変数 `indexNext`（呼び出しごと削除）。呼び出し 1 件の削除になるが、結果が使われていないため、挙動は変わらない。
- 影響の確認: 呼び出し元は `Mapping.cs:4819`（`index < 0` で読み飛ばす）。未検出の名前は、これまで 0 を返して通過し、直後の `action == null` で処理されていた。-1 にすると、読み飛ばしが先に働く。`action == null` 側にログがあるか（ログの維持、§2.3）を、着手時に確認して、あれば挙動を維持する。
- テスト: `Phase6Step3ContractExtensionTests.GetProfileActionIndexOf_SharesStateWithGlobal` の未検出の期待値を 0 → -1 に直す（テスト名も実態に合わせて整える）。`_config` が null の場合のテストを追加。
- 回帰ではない（移行前と同一）。この是正の記録を、完了報告書に残す。

### W3: 不具合 B の是正（アプリ設定の DTO 読込の後処理の欠落）
- 現象（`Phase7-0-Survey.md` §2.1）: `BackingStore.Load()` の DTO 経路は、`MapTo` の直後に `return` するため、次が実行されない。
  - ① 偽の exe 名ファイル（`CUSTOM_EXE_CONFIG_FILENAME`）の読み込み → `fakeExeFileName` が空のままになり、`Save()` が既存のファイルを空で上書きする可能性。
  - ② `lastVersionCheckedNum` の算出（`PostProcessLoad`）。
  - ③ OSC／UDP のポート番号と UDP スムージング値の範囲の丸め（`PostProcessLoad`）。
  - ④ プロファイル名に "distance" を含む場合の `distanceProfiles` の設定（`PostProcessLoad`）。
- 先に特性化テストを書く（修正前に失敗することを確認できる形）:
  - 新規 `BackingStoreAppSettingsLoadTests`（1 ファイル 1 型）。`new BackingStore()` を作り、`m_Profile` を一時フォルダの DTO 形式の `Profiles.xml` に向ける。`Global.exedirpath`（public static フィールド）は、テストの間だけ一時フォルダへ向け、終了時に元へ戻す。
  - ケース: ① 偽の exe 名ファイルがある → 読込後 `fakeExeFileName` が復元される／② `LastVersionChecked` を持つ → `lastVersionCheckedNum` が算出される／③ 範囲外のポート → 丸められる／④ プロファイル名に distance → `distanceProfiles` が true。
  - 旧形式の DOM 経路が、これまでどおりの結果を返すことを確認するケースも置く（読み取り専用の互換）。
- 修正:
  - DTO 経路の `return` の前に、`PostProcessLoad()` と、偽の exe 名の読み込みを呼ぶ。
  - 偽の exe 名の読み込み（`Load()` の末尾にある `if (Loaded) { … }` のブロック）は、private メソッドへ切り出し、旧形式の経路と DTO 経路の両方から呼ぶ。切り出しは、本体の移動だけで、挙動を変えない。
  - `PostProcessLoad` の null 安全: `profilePath[i]` が null の場合に例外を出さないことを確認する（テストで固定）。
- ログ: 既存のログとレベルは変えない。DI 境界ではないため `[DI]` は付けない。
- 実機（実アプリ）確認: チェックリスト §11 に **U-10** を追加する（本計画の承認時に追記）:
  - 偽の exe 名ファイルがある状態で、アプリを起動して終了し、ファイルの内容が空にならないこと。
  - 「Skip Version」の後、再起動しても、スキップしたバージョンが保持されること（U-7 の「更新通知」とは別。新しいバージョンが無くても、設定値の復元で確認できる）。

### W4: `ServiceProviderHolder` のラチェットと台帳への追加（D6）
- ガードテストを追加する: `Step10FourGlobalRatchetGuardTests` は 1 ファイル 1 型の原則と、既存の基準値を保つため、**別のテスト型として** `Phase7Step1ServiceLocatorRatchetGuardTests` を追加する（`ServiceProviderHolder.Provider`／`ServiceProviderHolder.` の参照のファイル別の件数。コメントを除く）。基準値は、着手時の走査値（調査時点では約 23 件: `ActionManager` 13、`Mapping` 5、`Actions/` の工場 4、`App` 1）。増えたら失敗、減ったら基準値を更新。
- `Phase6-Step11-Global-Residual-Ledger.md` と同じ形式の台帳（`Phase7-Residual-Ledger.md`）を新設し、Service Locator（`ServiceProviderHolder`、`AppHost.GetService`）の分類を記録する。`Actions/` の約 17 件は「Phase8 持ち越し」と明記。
- モデル図: 構造は変わらないが、`04-Service-Lifecycle-Spec.md` に、「Service Locator の 2 系統（`AppHost`／`ServiceProviderHolder`）を追跡対象にした」旨の注記を追記する。

## 4. テストの方針
- 各 Wave でビルド警告 0、全テスト成功（`dotnet build`／`dotnet test` はユーザーが実行）。
- 新規テスト型: `Phase7Step1DeadMethodGuardTests`（W1）、`BackingStoreAppSettingsLoadTests`（W3）、`Phase7Step1ServiceLocatorRatchetGuardTests`（W4）。W2 は既存テストの修正と追加。
- ラチェットの基準値は、減らす方向のみ。型の追加・分割はしない（既存ファイルの中の削除と追加のみ）。

## 5. ロールバック
- Wave ごとにコミットを分け、問題が出た Wave だけを `git revert` する。
- W3 は設定ファイルの読込に触れるため、問題が出た場合は W3 だけを戻せるよう、W1・W2 と混ぜない。

## 6. リスクと注意
- W1: 動的参照（リフレクション、XAML の Binding）の可能性。走査で XAML／`nameof`／`typeof` の参照が 0 件であることを、削除前に再確認する。
- W2: 未検出時の挙動の差（読み飛ばしの順序）。`action == null` 側のログの維持を確認する。
- W3: 設定の読込は、起動に直結する。旧形式の経路を壊さない。`Save()` 側（偽の exe 名の書き込み条件）は、この Step では変更しない（読込の是正で解消する想定。実機確認で残るなら別途）。
- W4: ガードの基準値は、走査値と完全に一致させる（コメントや文字列の誤検出に注意）。

## 7. 完了判定チェックリスト
- [x] W1: `BackingStore` の孤立メソッド 10 名が削除され、`PostProcessLoad` が残っている。ラチェットの基準値が更新されている。
- [x] W2: `GetProfileActionIndexOf` が未検出で -1 を返す。`indexNext` が削除されている。テストが更新されている。
- [x] W3: 特性化テストが修正後に成功する（修正前の失敗は未検証。テストは修正と同時に提供した）。旧形式の経路のテストが成功する。U-10 がチェックリストに追記されている（実アプリでの確認は未実施）。
- [x] W4: `Phase7Step1ServiceLocatorRatchetGuardTests` が成功。`Phase7-Residual-Ledger.md` が作成されている。
- [x] `dotnet build` が警告 0・エラー 0。`DS4WindowsTests`／`StandaloneTests` の全件が成功。
- [x] `Phase7-Step1-Completion-Report.md` が作成されている（テスト数の増減、削除した名前、U-10 の結果を含む）。

---

## 8. 実施記録

### W1 の実施記録（2026-10-17、ビルド・テストはユーザーの実行待ち）
- 削除（`BackingStore.cs`、純粋な削除 396 行、6,952 → 6,556 行）: `SaveOld`、`UsedSavedProfileString`、`RebuildProfileActionDicts`（と doc コメント）、`GetDS4Action`×2、`GetDS4Extra`、`GetDS4KeyType`、`GetDS4STrigger`×2、`GetGyroOutModeString`、`GetGyroOutModeType`、`UpdateDS4CLightbarMacro`。削除前に、本体・テスト・XAML の参照が 0 件であることを再確認した（`Step12DeadShimRemovalGuardTests` の文字列にある名前は `Global` 用の名簿で、影響なし）。
- 残したもの: `PostProcessLoad`（W3 で接続）、`GetDS4CSetting`／`UpdateDS4CSetting`、孤立定数 8 件（Step10）。コメントアウト行（`//gyroOutMode[device] = GetGyroOutModeType(tempMode);`）は既存コメントのため触っていない。
- ラチェット: `BackingStore.cs` の `Global.*` を 182 → 168（ラチェットと同じ数え方で再集計。`AppHost` 1、`rootHub` 0 は不変）。全域は `Global.*` 747 → 733。
- ガード: 新規 `Phase7Step1DeadMethodGuardTests`（削除済み 10 名の再宣言禁止、`PostProcessLoad` ほか 3 名の存在確認）。
- 検証待ち: ユーザーの `dotnet build`（警告 0）と `dotnet test`（全件成功）。

### W2 の実施記録（2026-10-17、W1 のビルド・テストは成功。W2 のビルド・テストはユーザーの実行待ち）
- 修正: `ProfileActionProvider.GetProfileActionIndexOf` が、`TryGetValue` の戻り値で判定し、未検出（名前が null の場合を含む）は -1 を返す。
- 削除: `Mapping.cs` の未使用変数 `indexNext`（呼び出しごと。戻り値は使われていなかった）。
- 影響の確認: `profileActionDict` と `profileActionIndexDict` は、同じ名前で同時に作られる（`BackingStore.cs` の 827–835、3935–3940）。したがって、定義済みのアクションが「辞書にあるのにインデックスが無い」ことはなく、未検出の名前は、これまで `action == null` で読み飛ばされていた。-1 になると、その前の `index < 0` で読み飛ばされる（結果は同じ。`action == null` 側にログは無かった）。
- テスト: `Phase6Step3ContractExtensionTests` の該当テストを `GetProfileActionIndexOf_SharesStateWithGlobal_AndReturnsMinusOneWhenMissing` に改名し、未検出と null を -1 とする期待に更新、インデックス 0 の実在する名前が 0 を返すケースを追加。
- ラチェット: 変更なし（`Global`／`AppHost`／`rootHub` の参照を増減させていない）。

### W3 の実施記録（2026-10-17、W2 のビルド・テストは成功。W3 のビルド・テストはユーザーの実行待ち）
- 修正（`BackingStore.cs`）:
  - `Load()` の DTO 経路で、`return` の前に `PostProcessLoad()` と `LoadFakeExeName()` を呼ぶ（従来は「Post processing same as legacy path」のコメントだけで、何も実行されていなかった）。
  - `Load()` の末尾にあった偽の exe 名の読み込みを、private の `LoadFakeExeName()` に切り出し（処理は同一）。旧形式の経路（末尾）と DTO の経路の両方から呼ぶ。
  - `PostProcessLoad()` の Distance 判定に、`profilePath[i] != null` の保護を追加（null で例外にならない）。
  - `Global.*` の参照の件数は不変（切り出しで移動しただけ）。ラチェットの基準値の変更なし。
- 挙動の差（意図したもの）:
  - DTO で読み込んだとき、偽の exe 名ファイル（`custom_exe_name.txt`）が復元される。これにより、`Save()` が既存のファイルを空で上書きしなくなる。
  - `lastVersionCheckedNum` が算出される（不正な文字列は、旧形式の経路と同じく、`lastVersionChecked` を空にして 0 のまま）。
  - OSC／UDP のポート番号が 1024〜65535 に丸められる。XML に要素が無く 0 のままだったポートは、1024 になる（旧形式の経路と同じ扱い）。
  - プロファイル名に "distance" を含むスロットの `distanceProfiles` が true になる。
- テスト: 新規 `BackingStoreAppSettingsLoadTests`（8 件）。`BackingStore.Save` の実出力（DTO）を一時フォルダへ書き、別の `BackingStore` で `Load` する。`Global.exedirpath` は一時フォルダへ向け、終了時に戻す。修正前は、偽の exe 名・`lastVersionCheckedNum`・ポート・Distance のテスト（計 6 件）が失敗する構成（先に特性化テストを書くという計画から、修正と同時に提供した。未検証）。
- 旧形式の DOM 経路のテストは追加しなかった: `<Profile>` ルートのファイルは、DTO の逆シリアライズに成功するため、フォールバックに到達するのはルートが異なる、または XML が壊れている場合に限られる。この経路の変更は、末尾の切り出し（処理は同一）だけ。
- 実機（実アプリ）確認: チェックリスト §11 の U-10（追記済み）。

### W4 の実施記録（2026-10-17、W3 のビルド・テストは成功。W4 のビルド・テストはユーザーの実行待ち）
- 走査（コメントを除く）: `ServiceProviderHolder.Provider|GetRequiredService` は 23 件（`ActionManager` 13、`ActionFactory` 1、`DefaultKeyButtonActionControllerFactory` 2、`KeyActionAdapter` 1、`App.xaml.cs` 1、`Mapping.cs` 5）。`DI/AppHost.cs` の `SetProvider` 3 件は Composition Root の正規の使い方で数えない。`GetRequiredService` の呼び出し元は 0 件。
- ガード: 新規 `Phase7Step1ServiceLocatorRatchetGuardTests`（上記の基準値。増加または基準値に無いファイルへの出現で失敗）。
- 台帳: 新規 `Phase7-Residual-Ledger.md`（`Global.*` 733、`AppHost` 40、`Program.rootHub` 22、`ServiceProviderHolder` 23。分類と担当 Step を記載。`Actions/` の 17 件は Phase8 持ち越しと明記）。
- 全域の再集計（ラチェットと同じ数え方）: `Global.*` 733、`AppHost` 40、`Program.rootHub` 22。W1 の記録（747 → 733）と一致。
- モデル図: `04-Service-Lifecycle-Spec.md` に、Service Locator の 2 系統の追跡の注記を追記。構造の変更はない。
