# Phase6 完了報告書: アプリケーション全域の Pure DI 化

作成日: 2026-10-17  
対象ブランチ: `For-DI-migration-work`  
状態: **完了（2026-10-17 宣言）**

---

## 1. 目的と結論

- 目的: アプリ全域の静的ファサード `Global`（旧 `ScpUtil.cs`）への直接参照を、コンストラクタ注入された DI サービスへ置き換える。機能・ログ・安全弁は維持する（`.github/copilot-instructions.md` §2）。
- 結論: 目的を達成した。ただし「直接参照ゼロ」ではなく、**薄いシム（§2.1）と Phase7 持ち越しを温存した、分類済みの残存**（ラチェット基準値: `Global.*` 747、`AppHost` 40、`Program.rootHub` 22）で完了とする。残存の全件は、`Phase6-Step11-Global-Residual-Ledger.md` の分類（A〜H）に属し、根拠がある。

## 2. Step ごとの成果

- Step1: 監査（Core 層・UI 層）。Step2〜Step10: サービス契約の追加と、呼び出し元の Pure DI 化（約 404 箇所）。Step3: `Mapping.cs` の実参照 150 件を解消。Step5〜6: 出力スロット、未接続 API の整理。Step7〜7b: 起動（Pre-Host）とプロファイル編集画面。Step8〜9b: ProfileEditor の MVVM 移設、主要 4 ViewModel の Pure DI 化、レビュー指摘の是正。Step10: 残存 View／ViewModel。
- Step10b: 1 ファイル 1 型（複数型ファイル 55 件を分割、現在 0）。Step13: ファイル配置の統一（44 件）。
- Step11: 総合検証。自動テスト 595 件成功、実機チェックリスト 70 項目のうち ○67／不能 3／× 0。判定 GO。
- Step12: 呼び出し元 0 件の旧シムの物理削除（`Global` の 251 名、`Global.cs` 3,472 → 2,414 行）と、デバウンスの回帰の是正。詳細は `Phase6-Step12-Completion-Report.md`。

## 3. 数値の推移（全域の直接参照）

- Step11 時点: `Global.*` 780、`AppHost` 48、`Program.rootHub` 25。
- Step12 後: `Global.*` 747、`AppHost` 40、`Program.rootHub` 22。
- 複数型ファイル: 0。ビルド警告: 0（Wave 4 まで確認）。

## 4. 確認不能（リスク受容）の項目

チェックリスト §11 の U-1〜U-7（振動、スロット 5〜8、UDP コマンド、OSC／UDP、vJoy、Joy-Con 結合ジャイロ、更新通知）。U-8（デバウンスの即時反映）は 500ms で効くことを確認済み（再起動後の保持は未確認）、U-9（Step12 Wave 3 後の起動・接続・プロファイル切替）は ○。

## 5. Phase7 への引き継ぎ

`Phase6-Step12-Plan.md` §4 を正とする。主な項目: `BackingStore` の解体（旧保存系メソッド 10 件の判断を含む）、`Global` に残る実体（分類 D の 144 名）のサービス側への移管、`Mapping` の instance 化（静的 `profileSettings` 束縛 P2、温存 3 件、`SyntheticDispatcher`、第 1 層）、Pre-Host の起動シーケンスの近代化（K7-1 を含む）、`Program.rootHub` 22 件、互換 `AppHost`、`outDevTypeTemp`／`activeOutDevType` の所有権移動、DI インターフェースの 0 件メンバー約 52 件の個別判断、K2。その後に Phase8（Controls／SpecialActions 統合ディスパッチ）。

## 6. 最終確認

- Step12 Wave 1〜6 のビルド・テスト: すべて成功（2026-10-17）。
- 実機確認: U-9 は ○。U-8 は 500ms で効くことを確認（○。再起動後の保持は未確認）。U-1〜U-7 は機材・環境が揃うまで未確認（リスク受容）。
- 判定: **Phase6 の完了条件（自動検証の全件成功、実施可能な実機確認の合格、残存の分類と引き継ぎ台帳）を満たした。** 宣言日: 2026-10-17（ユーザー承認）。
