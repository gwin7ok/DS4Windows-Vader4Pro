# Phase7-Step1 完了報告書: 整理と台帳の確定

作成日: 2026-10-17  
対象ブランチ: `For-DI-migration-work`  
計画書: `Phase7-Step1-Plan.md`（各 Wave の記録は §8）。調査: `Phase7-0-Survey.md`  
状態: **実装完了。W1〜W4 のビルド（警告 0）・テストビルド・テスト実行は、すべて成功（ユーザーが各 Wave で実行）。実アプリでの確認 U-10 は、手順 A ○、手順 B は値の保持まで ○（通知の抑制は U-7 とあわせて確認）。**

---

## 1. 結果の要約

- W1: `BackingStore` の孤立メソッド 10 名（396 行）を削除。`Global.*` 747 → 733。
- W2: K2 の是正（`GetProfileActionIndexOf` が未検出で -1 を返す）と、未使用変数 `indexNext` の削除。
- W3: 不具合 B の是正（アプリ設定の DTO 読込の後処理の欠落）。
- W4: `ServiceProviderHolder`（23 件）をラチェットと台帳に追加。
- ラチェット基準値（全域、走査で確認）: `Global.*` **733**、`AppHost` **40**、`Program.rootHub` **22**、`ServiceProviderHolder` **23**（新規）。

## 2. Wave ごとの実績

- W1（`BackingStore.cs` 6,952 → 6,556 行）
  - 削除: `SaveOld`、`UsedSavedProfileString`、`RebuildProfileActionDicts`、`GetDS4Action`×2、`GetDS4Extra`、`GetDS4KeyType`、`GetDS4STrigger`×2、`GetGyroOutModeString`、`GetGyroOutModeType`、`UpdateDS4CLightbarMacro`。いずれも本体・テスト・XAML の参照が 0 件。経緯は `Phase7-0-Survey.md` §1.1。
  - 残した: `PostProcessLoad`（W3 で接続）、孤立定数 8 件（Step10 の定数の再配置）。
  - ガード: 新規 `Phase7Step1DeadMethodGuardTests`（2 件）。
- W2
  - `ProfileActionProvider.GetProfileActionIndexOf`: `TryGetValue` の戻り値で判定。未検出（名前が null を含む）は -1。
  - 影響: `profileActionDict` と `profileActionIndexDict` は同じ名前で同時に作られるため、定義済みのアクションの挙動は変わらない。未検出の名前は、これまで `action == null` で読み飛ばされていたものが、`index < 0` で先に読み飛ばされるだけ。
  - テスト: 既存 1 件を改名・更新（未検出と null は -1、インデックス 0 の実在する名前は 0）。
- W3
  - `BackingStore.Load()` の DTO 経路で、`PostProcessLoad()` と、偽の exe 名の読み込み（`LoadFakeExeName()` に切り出し）を呼ぶ。`PostProcessLoad` の Distance 判定に null 保護を追加。
  - これにより復元されるもの: 偽の exe 名（保存のたびに既存ファイルが空で上書きされる不具合も解消）、`lastVersionCheckedNum`、OSC／UDP ポートの範囲の丸め、`distanceProfiles`。
  - テスト: 新規 `BackingStoreAppSettingsLoadTests`（8 件）。
- W4
  - 新規 `Phase7Step1ServiceLocatorRatchetGuardTests`（1 件）、新規 `Phase7-Residual-Ledger.md`、`04-Service-Lifecycle-Spec.md` に注記。

## 3. 回帰かどうかの確認

- 不具合 B: 回帰ではない。移行の直前（`391df580`）の `ScpUtil.cs` も同じ構造で、`105575a7`（2025-11-11）から続いていた。
- K2: 回帰ではない（移行前の `Global.GetProfileActionIndexOf` と同一の挙動）。

## 4. 挙動の差（意図したもの）

- アプリ設定の読込後、偽の exe 名ファイル（`custom_exe_name.txt`）の内容が `FakeExeName` に復元される。
- 「スキップしたバージョン」の数値が、再起動後も算出される。
- XML にポート番号の要素が無く 0 だった場合は、1024 になる（旧形式の読込と同じ扱い）。
- ログの出力とレベルは変えていない。

## 5. 追加・変更したテスト

- 追加: `Phase7Step1DeadMethodGuardTests`（2）、`BackingStoreAppSettingsLoadTests`（8）、`Phase7Step1ServiceLocatorRatchetGuardTests`（1）。
- 変更: `Phase6Step3ContractExtensionTests`（1 件の期待値と名前）、`Step10FourGlobalRatchetGuardTests`（`BackingStore.cs` の基準値 182 → 168）。
- 削除したテスト: なし。
- 総数の増減: +11（ユーザー側の実行結果の件数と照合すること。実行結果の件数は未記録）。

## 6. 未完了・持ち越し

- **U-10（実アプリでの確認、2026-10-17）**: 偽の exe 名は、起動後も設定画面に表示され、保存後もファイルの内容が空にならないことを確認（○）。スキップしたバージョンは、起動・保存を経て値が保持されることまで確認（○）。更新通知が実際に抑制されることは、新しいバージョンの公開時に U-7 とあわせて確認（未）。
- U-1〜U-7 は、機材・環境が揃うまで未確認（リスク受容のまま）。
- 孤立定数 8 件、`BackingStore` の分解は、Step10。

## 7. 次の Step

- **Step2: DI インターフェースの 0 件メンバー（約 50 件）の個別判断**（§2.4）。`Phase7-0-Survey.md` §1.6 の一覧から始める。経緯の調査 → 判断の記録 → 承認 → 削除または接続の計画。`Actions/` の 11 件は Phase8 へ移す。
