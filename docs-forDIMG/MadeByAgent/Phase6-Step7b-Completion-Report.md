# Phase6-Step7b 完了報告書: プロファイル編集画面の「編集内容の即時反映」廃止（保存・適用時に一括反映）

作成日: 2026-09-26  
状態: **完了確定（2026-09-26）**（Step7b-1〜7b-3 と範囲外の追加修正 2 件の各段階でビルド・テストビルド・テスト実行成功、実機確認済み。ステアリングホイール校正のみ Step11 へ先送り）  
対象ブランチ: `For-DI-migration-work`  
計画書: `docs-forDIMG/MadeByAgent/Phase6-Step7b-Plan.md`  
着手前の HEAD: `360cb75b`

---

## 1. 結果の要約
- プロファイル編集画面は、開き方にかかわらず常に作業スロット（`Global.TEST_PROFILE_INDEX`＝8）に対して読み書きするようになった。編集中の変更は、保存・適用を押すまで接続中のどのコントローラーにも届かない。持ち越し K4-3（Edit ボタンで開いた編集画面の変更が保存前にコントローラーへ即時反映される）を解消した。
- 実機（Edit ボタンを押したコントローラー）は、別の値 `targetDevice` として持つ（一覧経由は −1＝実機なし）。実機に触れる一時確認機能だけが `targetDevice` を使う: ランブルテスト、ライトバー色のプレビュー（編集画面本体、ボタン設定画面、マクロ記録画面、バッテリー確認アクション）、マクロ記録中のタッチパッド Passthru、Controller Readings、for readout、スティック再校正、Gyro Calibration、ステアリングホイール校正。
- Save と Apply は、どちらも保存後にそのプロファイルを使っているコントローラーにだけ再適用する。違いは画面を閉じるかどうかだけ（決定8）。
- 到達しなくなった即時フック 8 件と、呼出元 0 件の `ProfileEditor.DeviceNum` を削除した。
- 実機確認中に見つかった既存の問題 2 件を、ユーザー決定により範囲外の追加修正として直した（マクロ記録画面のボタン表示条件、スティックの Dead Zone 入力欄の反映タイミング）。
- 実機確認中に DS4Windows の異常終了が発生した。原因は特定できておらず、切り分けを含めて先送りした（持ち越し K7b-1）。

## 2. 計画書からの主な変更点
- **着手前再確認（2026-09-26）で判明した旧調査の漏れ**: マクロ記録画面（ライトバー色プレビュー、記録中のタッチパッド Passthru）とバッテリー確認アクションの色プレビューも実機に触れていた。付け替えの範囲に加えた（決定1＝A）。
- **マイクロステップの順序**: 旧計画の「編集スロット固定 → 付け替え → 保存・適用」では途中のビルドで適用が効かなくなるため、「VM・ファクトリ拡張（挙動変化なし）→ サブ画面への受け渡し（挙動変化なし）→ 編集スロット固定と保存・適用・キャンセル整理」に改めた。
- **`Reload` の引数**: 計画の `Reload(int targetDevice, ProfileEntity)` ではなく `Reload(ProfileEntity profile = null)` にした。ViewModel はコンストラクタで `targetDevice` を使って生成されるため、`Reload` でも受け取ると食い違う余地が生まれるから。
- **Apply の仕様（決定8）**: 計画では「Apply は `ApplyProfileToSlot(targetDevice, …)`」としていたが、実機確認の前にユーザーから Save／Apply の仕様が示され、Apply が 2026-09-16（`36d124a6`）以降仕様から外れていた（New Profile から Apply すると、そのコントローラーが新しいプロファイルに切り替わる等）ことが分かった。Apply も Save と同じく `ProfileSaved` を通知する形に是正した。

## 3. ユーザー決定（詳細は計画書 §2A）
- **決定1＝A**: 実機に触れる処理は、サブ画面まですべて `targetDevice` に付け替える。
- **決定2＝P1**: 画面（`ProfileEditor`、`BindingWindow`、`RecordBox`、`RecordBoxWindow`、`SpecialActionEditor`）のコンストラクタは必須引数、ViewModel とファクトリは省略可能（既定 −1）。
- **決定3＝M1**: モデル図 03（`IViewModelFactory` の理想形のシグネチャと注釈）・04（§1-3 と注釈）に `targetDevice` を追記。
- **決定4＝I1**: ランブルテストの左右反転は、編集中の値（作業スロット）を読む。
- **決定5＝D1**: `ProfileEditor.DeviceNum` を削除（2019 年に新規作成時のコントローラー切り替えのために作られ、2026-09-14 に利用箇所が削除されていた）。
- **決定6＝L1**: 確認用の `LogDebug` を追加（Opened／Reload／Save・Apply／Cancel）、`[DI]` Trace ログに `targetDevice` を付け足し。
- **決定7＝H1**: 到達しなくなった即時フック 8 件を削除（承認済みの機能廃止の本体）。
- **決定8**: Save と Apply はどちらも (1) 保存し、(2) そのプロファイルを使っているコントローラーにだけ再適用し、(3) Save は閉じ、Apply は閉じない。
- **範囲外の追加修正（実機確認中の決定）**: マクロ記録画面の Add Rumble／Change Lightbar Color を Record Delays の有無にかかわらず記録中に表示。スティックの Dead Zone 入力欄の `UpdateSourceTrigger=LostFocus` を撤廃。
- **K7b-1**: 異常終了の件は、切り分けを含めて対策を先送り。

## 4. 実施した変更
- **着手前再確認（`b073f6f7`）**: 計画書の改訂（旧調査との乖離、台帳の再作成、決定1〜7、マイクロステップ、実機確認項目）。
- **Step7b-1（`a3a2d49c`）: `ProfileSettingsViewModel` と `IViewModelFactory` の拡張**
  - `ProfileSettingsViewModel` に `targetDevice`／`TargetDevice`／`HasTargetDevice` を追加。`FuncDevNum` は `targetDevice`（実機なしならコントローラー0）。ライトバー強制色の 3 メソッドを `targetDevice` に付け替え。
  - `IViewModelFactory.CreateProfileSettingsViewModel(int device, int targetDevice = -1)` と `ViewModelFactory`。
- **Step7b-2（`8b34f6f3`）: サブ画面への `targetDevice` の受け渡し**
  - `BindingWindowViewModel`・`RecordBoxViewModel`（省略可能）、`BindingWindow`・`RecordBox`・`RecordBoxWindow`・`SpecialActionEditor`（必須）、`IViewModelFactory.CreateRecordBoxViewModel`。
  - マクロ記録中の Passthru は実機があるときだけ設定し、設定した場合だけ 1 回戻す（2 回呼ぶと `TouchOutMode` が `None` になる潜在的な問題も解消）。
- **Step7b-3（`1a9d0da8`）: 編集スロットの固定と保存・適用・キャンセルの整理**
  - `ProfileEditor`: `deviceNum` を `readonly` で作業スロットに固定、`targetDevice` はコンストラクタでだけ受け取る、定数 `NoTargetDevice`、`Reload(ProfileEntity)`、Controller Readings・for readout・スティック再校正・ランブルテスト・Gyro Calibration の付け替え、キャンセルの再読み込み削除、`DeviceNum` と即時フック 2 件（XAML 属性を含む）の削除、確認用ログ。
  - `ExecuteSaveOrApply`: Save・Apply 共通で `ProfileSaved` を通知（決定8）。
  - `ProfileSettingsViewModel`: 即時フック 6 件の削除。
  - `MainWindow`: `ShowProfileEditor(int targetDevice, ProfileEntity entity)`、一覧経由は `ProfileEditor.NoTargetDevice`、`EmitMissingActionLogsForDevice` は作業スロット。
- **範囲外の追加修正 1（`3deed852`）**: `RecordBox.xaml.cs` の `RecordBtn_Click`（Record Delays の表示条件の撤廃）。
- **範囲外の追加修正 2（`06653fee`）**: `ProfileEditor.xaml`（LS／RS Dead Zone）と `AxialStickUserControl.xaml`（Dead Zone X／Y）の `UpdateSourceTrigger=LostFocus` を撤廃。
- **Step7b-4（本報告書）**: 文書の更新と引き継ぎの登録（§7）。

## 5. 追加・変更したテスト（`DS4WindowsTests`）
- `ProfileSettingsViewModelTargetDeviceTests`（新規、6 件）: 編集スロットと `targetDevice` の分離、実機なし・省略時のコントローラー0へのフォールバック、範囲外の値の扱い、ライトバー強制色の対象。
- `BindingWindowViewModelTargetDeviceTests`（新規、5 件）、`RecordBoxViewModelTargetDeviceTests`（新規、5 件。独立した `BackingStore` で Passthru の設定と復元を検証）。
- `PatternCViewModelTests`（変更）: 実際の DI ホストから解決したファクトリで `targetDevice` が 2 種類の ViewModel に届くことを確認（K7-1 の教訓）。
- `ProfileEditorTargetDeviceGuardTests`（新規、9 件のソース走査ガード）: 作業スロットの固定、キャンセル、Save／Apply の通知（決定8）、`MainWindow` の再適用条件、`targetDevice` の受け渡し、削除したコードの再混入。
- `RecordBoxExtraButtonsVisibilityGuardTests`（新規、1 件）、`StickDeadZoneBindingGuardTests`（新規、4 件）。
- 既存の `ProfileEditorMappingDevTypeGuardTests` は変更なしで合格（`UpdateMappingDevType` の呼び出しと位置を維持）。

## 6. 検証結果
- **ビルド・テスト**: 各段階で、ビルド・テストビルド・テスト実行がすべて成功（ユーザー確認）。途中で出た xUnit2013 の警告（`Assert.Equal` で件数を比較）は `Assert.Single` に修正した。
- **実機確認（2026-09-26、詳細は計画書 §4.7）**: 主目的（Edit ボタンで開いて Output Mode を Mouse にしてもポインタが動かず、適用で動き出す）、Save／Apply の反映（決定8）、キャンセル、一時確認機能、校正、一覧経由、新規作成のすべてで問題なし。
  - 再接続後にジャイロマウスが動かなくなった件は、切断後に「Turn Behavior - Turns Gyro」のチェックを外したためで、Step7b の不具合ではなかった。
  - 計画書の確認手順の不正確な記述（Record A Macro のライトバー色ステップの出し方、for readout の場所、Gyro Calibration の確認方法）を訂正した。
- **Step11 への先送り（`Phase6-Step11-Plan.md` §3.3 に登録）**: ステアリングホイール校正（vJoy 環境がないため）。

## 7. 持ち越し事項と他 Step への引き継ぎ
- **K7b-1（`Phase6-Status.md` §6.5）**: プロファイル一覧から編集中に、Controller Readings を表示した状態で Lightbar の色選択画面を開くと、DS4Windows が異常終了することがある。3 回とも落ちた場所と種類が違い（入力スレッドの `IndexOutOfRangeException`、`coreclr.dll` のアクセス違反、UI スレッドの `AccessViolationException`）、メモリ破壊が疑われる。再現手順の経路は Step7b で変わっておらず、回帰の証拠はないが未検証。切り分け手順（`360cb75b` での再現確認ほか）とともに先送りした。
- **Step8（`Phase6-Step8-Plan.md` 冒頭）**: `ProfileEditor.xaml.cs` の行番号と、保存・適用・キャンセル・`Reload` の内容が変わったため、Step8-0 で台帳を作り直す。B9（`ApplyProfileToSlot`）の前提が変わった（編集画面から直接呼ばない）。維持が必要なソース走査ガードの一覧。
- **既存の問題として記録したもの（本 Step では変えない）**: マクロ記録の入力元が常にコントローラー0（`RecordBoxViewModel.ProcessDS4Tick`）、第 4・第 5 マウスボタンの Stop 時の終了ステップ自動追加が動いていない、マクロ再生の終了時に押したままのボタン・ライトバー・振動を戻さない、Controller Readings の RS 出力の差分判定の変数の書き間違いとデッドゾーンの円の縦位置、一時プロファイルが有効なスロットへの再適用の扱い（計画書 §1A.5、§3 Step7b-3 の記録）。
- **観察（別件としてユーザー判断）**: New Profile で作成したプロファイルへコントローラーを切り替える上流の機能は、2026-09-14 の保存処理の統合で外れている。`Mouse.IsGyroTriggerActive` は Mouse／MouseJoystick モードでも Gyro Controls 用のトグル設定を参照している（2024-08 の上流から）。

## 8. 完了判定チェックリストの結果
- [x] 着手前再確認（台帳の再作成、旧調査との乖離の記録、決定事項の提示）
- [x] 決定1〜8 のユーザー確認と記録
- [x] 編集スロットが常に `TEST_PROFILE_INDEX`、編集中の変更が保存・適用まで実機に届かない
- [x] Save／Apply で、そのプロファイルを使っている全スロットに再適用され、使っていないコントローラーには適用されない
- [x] キャンセルでコントローラーのスロットが変化しない
- [x] 一時確認機能と校正が `targetDevice` で動く
- [x] 一覧経由の編集の動作が従来と変わらない（決定8 による Apply の再適用を除く）
- [x] ビルド・テストビルド・テスト実行の成功
- [x] 実機確認の完了（ステアリングホイール校正は Step11 の先送り台帳に登録）
- [x] `Phase6-Status.md`／`Phase6-Plan.md`／`Phase6-Step8-Plan.md`／`Phase6-Step11-Plan.md`、モデル図 03・04 の更新と、本報告書の作成
