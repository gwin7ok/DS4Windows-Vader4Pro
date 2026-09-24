# Phase6-Step6 完了報告書: ProfileEditor のマッピング一覧の機種表示追従と、未接続 API（PluginSlot／UnplugSlot）の整理

作成日: 2026-09-24  
状態: **完了確定（2026-09-24）**（ビルド・テストビルド・テスト実行成功、実機確認 1〜4 問題なし）  
対象ブランチ: `For-DI-migration-work`  
計画書: `docs-forDIMG/MadeByAgent/Phase6-Step6-Plan.md`

---

## 1. 結果の要約
- プロファイル編集画面を開いたとき、およびプリセットを適用したときに、マッピング一覧のボタン名表記（Xbox 360 の A/B/X/Y ⇄ DS4 の Cross/Circle 等）が、読み込んだプロファイルの出力種別と常に一致するようにした。
- `IOutputSlotService.PluginSlot`／`UnplugSlot`（呼出元 0 件）は、Phase5-Step12 で UI 接続のために作られたが未接続だったと判明したため、削除せず温存し、TODO を「未接続の DI 契約・Step10 で接続予定」に書き換えた。接続作業を `Phase6-Step10-Plan.md` §8 に追加した。
- 未参照コードの扱いの判断手順を `.github/copilot-instructions.md` §2.4 として明文化した。
- 実行コードの変更は `ProfileEditor.xaml.cs` の 2 行のみ。ホットスワップ経路（`ScpUtil.cs` の `PostLoadSnippet`）、契約、`ServiceRegistration.cs`、モデル図の変更はなし。

## 2. 計画書からの主な変更点（Step6-0 の台帳再作成）
- 旧計画の「エディタを開いたまま別プロファイルを `Reload` で読み込むと追従しない」は、そのような操作が存在しないため誤り（`Reload` の呼び出しはエディタを開くときの 1 箇所のみ）。実在する問題は、`mappingListVM` がプロファイル読み込み前の出力種別で生成され、以後の追従を出力種別コンボボックスの `SelectionChanged` の発生有無に頼っていたこと。プリセット適用（`RefreshEditorBindings`）も同じ仕組みに依存していたため、是正対象に追加した（C6-01b）。
- 旧計画の「`PluginSlot`／`UnplugSlot` は常に `false` を返すダミー」は誤り。実装は `ControlService.AttachUnboundOutDev`／`DetachUnboundOutDev` を中継する実体を持つ。
- 決定1は当初案Q（削除）としたが、ユーザーの指摘により経緯を再調査し、案R（温存し Step10 で接続）に変更した（`Phase6-Step6-Plan.md` §3）。

## 3. 実施した変更
- **Step6-1**: `ProfileEditor.xaml.cs` の `Reload` と `RefreshEditorBindings` で、`profileSettingsVM.UpdateLateProperties()` の直後に `mappingListVM.UpdateMappingDevType(profileSettingsVM.ContType)` を呼ぶ（各 1 行、理由のコメント付き）。既存のコンボボックス経由の呼び出しと重なっても、ボタン名を作り直すだけで副作用はない。
- **Step6-2**（コメントのみ）: `IOutputSlotService.cs` と `OutputSlotService.cs` の `PluginSlot`／`UnplugSlot` の TODO を書き換え（経緯、正式な入口として温存する方針、ホットスワップとは別経路であること、接続時に是正する差分 4 点）。`OutputSlotService` のコンストラクタの `Program.rootHub` フォールバックに、循環依存を避けた Pure DI 化の TODO を付与。
- **Step6-3**: ソース走査ガード `ProfileEditorMappingDevTypeGuardTests` を新設。文書を更新し、本報告書を作成。
- **規約**: `.github/copilot-instructions.md` §2.4「未参照コード（呼出元0件のクラス・メソッド等）の扱い」を追加（経緯の調査 → Pure DI／4層モデルに照らした判断 → 使うべきものは TODO と接続 Step の計画化、使うべきでないものだけ削除 → 記録と承認）。

## 4. 追加したテスト（`DS4WindowsTests`）
- `ProfileEditorMappingDevTypeGuardTests`（1 件、ソース走査ガード）: `Reload` と `RefreshEditorBindings` が `mappingListVM.UpdateMappingDevType(profileSettingsVM.ContType)` を、`UpdateLateProperties()` の後に呼ぶこと。`ProfileEditor` は WPF の UserControl で単体駆動が困難なため、ソース走査で担保する。

## 5. 他 Step への引き継ぎ
- **Step7b**: `Reload` の書き換え時に、上記の呼び出しと順序を維持する（`Phase6-Step7b-Plan.md` に追記済み。ガードテストが検査する）。
- **Step10 §8**: `PluginSlot`／`UnplugSlot` の是正（ディスパッチ、事前条件、Pure DI での `ControlService` 取得、イベント）と、出力スロット管理画面・UDP コマンドからの接続。

## 6. 検証結果
- **ビルド・テスト（ユーザー確認、2026-09-24）**: Step6-3 の追加テストを含め、ビルド・テストビルド・テスト実行がすべて成功。コミットしてリモートリポジトリへ反映済み。
- **実機確認（2026-09-24）**: 下記 1〜4 はすべて問題なし。
- **副次的な観察（アプリの回帰ではない）**: 項目4 の確認中、仮想コントローラーの種別が変わったときに以前は表示されていた Windows のトースト通知（DS4／X360 コントローラーの接続）が表示されなかった。DS4Windows には該当のトーストを出すコードがなく、抜き差し経路も Step4〜6 で未変更、さらにトーストが出ていた頃のブランチでも表示されなかったため、Windows 側が原因と判断した。別途調査として `Phase6-Step11-Plan.md` §3.3 の先送り台帳に登録した。
  1. 出力種別が DS4／Xbox 360 のプロファイルを、Edit ボタンとプロファイル一覧のそれぞれから開き、マッピング一覧のボタン名が出力種別と一致すること（逆の順でも確認）。
  2. 出力種別コンボボックスの切り替えで、ボタン名が即座に切り替わること。
  3. プリセット適用後も、ボタン名が出力種別と一致すること。
  4. （念のため）出力種別の異なるプロファイルへの切り替えで、仮想コントローラーの種別が切り替わること。

## 7. 完了判定チェックリストの結果
- [x] `Reload` と `RefreshEditorBindings` で `UpdateMappingDevType` を呼ぶ
- [x] 編集画面を開いたとき・プリセット適用後のボタン名が出力種別と一致する（実機確認済み）
- [x] `PluginSlot`／`UnplugSlot` の TODO を案R の内容に書き換え、接続作業を Step10 §8 に登録
- [x] ホットスワップ機構に手を加えていない
- [x] Step6-3 の追加テストを含むビルド・テスト全件成功
- [x] 実機確認が完了（副次的な観察は Step11 の先送り台帳に登録）
- [x] `Phase6-Status.md`／`Phase6-Plan.md`／`Phase6-Step7b-Plan.md`／`Phase6-Step10-Plan.md` を更新
