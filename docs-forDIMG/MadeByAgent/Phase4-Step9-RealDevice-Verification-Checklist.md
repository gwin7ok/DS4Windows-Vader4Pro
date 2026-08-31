# フェーズ4 実機動作確認リスト CP3（Phase4-Step9-RealDevice-Verification-Checklist.md）

正式名称: `docs-forDIMG/MadeByAgent/Phase4-Step9-RealDevice-Verification-Checklist.md`
作成日: 2026-09-01
対象ブランチ: `For-DI-migration-work`
前提ドキュメント:
- `docs-forDIMG/DI-App-Wide-Migration-Plan.md` §3.3, §4.1
- `docs-forDIMG/MadeByAgent/Phase4-Plan.md` §1.1.1, §2, §3.3
- `docs-forDIMG/MadeByAgent/Phase4-Status.md`
- `docs-forDIMG/MadeByAgent/Phase4-Step8-Completion-Report.md`
- `docs-forDIMG/MadeByAgent/Phase4-Step9-Audit-Report.md`（Step9-4-α 監査合格）

---

## 本リストの位置づけ

本リストは、Phase4 の **全 ViewModel の DI 移行（Pattern A, Pattern B, Pattern C 全網羅）および直接 `new ViewModel()` の全廃（Step 9）** が完了したマイルストーンにおいて、**実際の WPF UI 画面操作・ダイアログ開閉・実機コントローラー入力** を通じた全画面 UI 結合動作確認を行うためのチェックリストである。

自動テスト（96件）および Step9-4-α の全体監査で担保済みの DI コンテナ解決・単体ロジックと切り分け、**実際の Windows / WPF 画面表示およびユーザー操作でしか確認できない項目** を対象とする。

各項目は、確認後に結果（`○`: 正常動作、`△`: 一部制限あり、`×`: 不具合あり、`未実施`）およびメモを記入すること。

---

## 1. メイン画面 & コントローラー一覧シナリオ (Pattern B: `MainWindow`, `ControllersViewModel`, `MainWindowsViewModel`)

| # | 確認内容 | 確認手順 | 結果 | メモ |
|---|---|---|---|---|
| 1-1 | アプリ起動時にメインウィンドウが正常に立ち上がり、DI 経由で DataContext がバインドされる | アプリを通常起動し、タイトル・ステータスバー・タブコントロールが正常に表示されることを確認 | [ ] | |
| 1-2 | コントローラー一覧で実機接続が認識され、バッテリー残量・通信種別（USB/BT）が正常に表示・更新される | コントローラーを接続・切断し、メイン画面の各スロット表示が即座に同期・追従することを確認 | [ ] | |
| 1-3 | プロファイル切替ドロップダウンの操作が正常に動作する | コントローラースロットのプロファイルドロップダウンを変更し、プロファイルが即時切り替わることを確認 | [ ] | |

---

## 2. 設定・ログ・情報画面シナリオ (Pattern A: `SettingsUserControl`, `LogUserControl`, `AboutUserControl`)

| # | 確認内容 | 確認手順 | 結果 | メモ |
|---|---|---|---|---|
| 2-1 | Settings タブで各種環境設定（最小化起動、通知設定、言語設定等）が正常に表示・変更・保存される | Settings タブを開き、各チェックボックスや数値を変更してアプリを再起動し、変更が維持されることを確認 | [ ] | |
| 2-2 | Log タブでログメッセージがリアルタイムに追記・表示され、クリアボタンやレベルフィルタが動作する | Log タブを開き、コントローラー操作やプロファイル切替時のログが正常に追記されることを確認 | [ ] | |
| 2-3 | About タブでバージョン文字列、アプリタイトル、GitHub リンクが正常に表示・動作する | About タブを開き、バージョン番号（`v...`）が正しく表示され、リンククリックでブラウザが開くことを確認 | [ ] | |

---

## 3. ダイアログ・編集画面シナリオ (Pattern C: `ProfileEditor`, `RecordBox`, `SpecialActionEditor`, `AutoProfiles`)

| # | 確認内容 | 確認手順 | 結果 | メモ |
|---|---|---|---|---|
| 3-1 | ProfileEditor（プロファイル編集ダイアログ）が Factory 経由で正常に開き、設定の変更・保存ができる | Controllers タブまたは Profiles タブから Edit をクリックし、編集画面が正常に開き保存できることを確認 | [ ] | |
| 3-2 | RecordBox（マクロ記録ダイアログ）が Factory 経由で正常に開き、キー入力の記録・保存ができる | プロファイル編集画面または SpecialAction 画面から Record Macro を開き、キー記録が動作することを確認 | [ ] | |
| 3-3 | SpecialActionEditor（アクション編集ダイアログ）が Factory 経由で正常に開き、アクションの作成・編集ができる | Special Actions タブから New / Edit を開き、マクロ・プロファイル切替・プログラム起動等の設定ができることを確認 | [ ] | |
| 3-4 | AutoProfiles（自動プロファイル画面）が Factory 経由で正常に開き、プログラム割り当ての追加・削除ができる | Auto Profiles タブを開き、アプリケーションへのプロファイル割り当ての追加・削除が正常に行えることを確認 | [ ] | |

---

## 4. 統合安定性 & 直接 new 全廃の総合検証

| # | 確認内容 | 確認手順 | 結果 | メモ |
|---|---|---|---|---|
| 4-1 | 画面間のタブ切り替え、複数ダイアログの連続開閉を繰り返しても、クラッシュや例外が発生しない | 全てのタブを順次切り替え、各ダイアログを開いて閉じる操作を繰り返して安定動作を確認 | [ ] | |
| 4-2 | Log タブに DI 解決エラー（NullReference や InvalidOperationException）が一切出力されていない | Log タブを開き、全画面操作中に赤字エラーや警告がログに記録されていないことを確認 | [ ] | |

---

## 5. 実施記録

| 実施日 | 確認者 | 実施項目 | 結果概要 |
|---|---|---|---|
| （未実施） | - | - | - |

---

## 6. 次のアクション

1. 実機動作確認を行い、本リストに結果を記録する。
2. 全項目合格（○）を確認後、Step 9 完了報告書（`Phase4-Step9-Completion-Report.md`）を作成する。
3. フェーズ4の最終ステップである **Phase4-Step10: Phase3 引継ぎ再確認・シム整理・全体健全性監査** へ進む。
