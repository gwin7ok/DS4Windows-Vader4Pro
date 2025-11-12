# Changelog

All notable changes to this project are recorded in this file.

## [3.11.3] - 2025-11-12
- (7dc4860a) CI: Build/package x64+x86 and attach zips to Release (use Directory.Build.props & CHANGELOG)
- (9a38278c) CI: Add Windows build+release workflow using Directory.Build.props; fix other workflow files formatting
- (88630d2f) Use Directory.Build.props as version source; update tasks and changelog; README points to CHANGELOG
- (d620058a) 更新履歴およびリリース管理の方法の変更
- (ac20f682) 修正検討過程で追加された不要なメソッドや変数を削除
- (4c33ea94) 無効なスペシャルアクションがプロファイル設定ファイルから削除されたときにinfoでログ出力するように変更
- (48430dfb) profile action の走査を配列スナップショットで行うように修正、MapCustomAction にコメントを追加
- (cf8ef1dd) ログ出力メッセージを分かりやすい文に変更
- (86854867) スペシャルアクション名前チェックを共通メソッド化
- (10e78add) 無効スペシャルアクション検出時のログ出力を整理（適切なタイミングで一度だけ出力）
- (260c7525) *.xml の 3.11.3 <> 3.9.9 間の互換性チェック（結果：OK）
- (813efce2) LastChecked のパースを複数フォーマット対応にして文化依存の失敗を回避
- (c585e4e9) app_version/config_version を XmlSerializer の属性として扱うよう ScpUtil.Save() を修正
- (105575a7) DTO方式で設定ファイルを読み込み保存する方式に戻した
- (7f27cfcc) LoadOld を Load() に整理
- (576426b8) 3.9.9以前との互換性のための保存読込メソッド整理（旧方式を維持）
- (777420ba) 列幅・左右エリア幅の保存が動作するよう修正
- (142c24c8) Profile.xml を 3.9.9 互換の形に変更
- (31a85068) 日本語ログを英語に修正（英語化の一環）
- (6d6cdff5) ハードコーディングされた日本語文をローカライズ参照へ変更
- (136a8e50) HidHide 設定クライアント起動リンク表示機能の修正
- (b5db9c18) URL等の修正
- (7e78c34c) URL等の参照先を変更
- (f893622e) 更新履歴の表示形式修正
- (83cbd5b1) アップデート確認先 URI を変更
- (470babcb) ログのアーカイブ数を 1000 に変更（NLog.config）
- (6f04ecf6) ログ出力の追加と整理
- (09af1441) SortDescriptions のカウンターチェックとクリア処理を削除
- (5018df78) 旧処理の削除（不要な残滓をクリーンアップ）
- (204ab347) プロフィール編集画面を閉じる時の列名オブジェクト割当処理修正
- (6d739001) さらに無駄な処理を削除
- (c9717a75) スペシャルアクション列名処理の無駄な部分を削除
- (5f0a39d7) ウィンドウ幅およびプロフィール編集画面左エリア幅を拡大（初期値）
- (bbd837c2) チェックボックス列幅初期値を 32 に、ウィンドウ初期化ボタンで復元可能に
- (134544ee) チェックボックス列名を空白化、ソート時は▲▼を表示
- (9c9516bd) スペシャルアクション一覧のチェックボックスを左端列に独立配置
- (4891588d) 列幅超過時の端の移動方向を右側ではなく左側へ変更
- (a4e0ea3c) 無駄な列オブジェクト取得処理を回避
- (8d44d589) 初期表示前のソート処理を1回に修正
- (bc6aedce) 不要なコード削除（段階2）
- (5ee0f9dc) 初期表示時に列名に▲が表示されるよう修正
- (7624f4cd) 見かけ上列クリックでソートができる状態の改善（初期表示のみ）
- (72a99958) ソート基準列に▲▼を表示
- (6ca74dfa) テスト削除
- (f77ccedd) 初期表示と列クリック時で同メソッドを使用するよう変更
- (d83a87a9) 列のどの部分をクリックしてもソートできるように改善
- (7d790f37) 見た目上の列クリックでソート可能に（ただし列名部をクリックする実装）
- (232ebd9b) テストコードの削除
- (3fb26fbe) スペシャルアクション一覧のソートを昇降トグルに修正
- (a024a4ee) 列名にボタンを配置してソート可能に
- (ceb2b330) スペシャルアクション一覧のアクション列に「(無効なスペシャルアクション)」を表示
- (40011c12) 無効なスペシャルアクション名をグレーアウトして表示する実装
- (fecb41b6) 無効な名前をグレーアウト表示し、チェックをOFFにして保存すれば削除可能である旨を追加説明
- (56e259b2) 不要なスペシャルアクション存在チェックコードを削除
- (0044537c) 細かい不要コードの削除
- (a5bb8fb5) 無効スペシャルアクションが存在する場合にデバッグログエラーが出続ける不具合修正
- (994b8f28) 未使用変数`e`の削除
- (bc937107) プロフィール編集画面の左右境界の余白変更、ウィンドウ幅初期値の調整
- (513d4822) プロフィール編集画面の左右境界線を太くする変更

## [3.11.2] - (previous)
- See repository tag `v3.11.2` for previous release details.

> Note: CI/GitHub Actions will extract the section matching the version in `Directory.Build.props` to populate release notes.

## [3.11.2]
- Fixed Profile Switching Failure: Resolved an issue where Special Actions for profile switching would fail to work if the action was not positioned at the end of the actions.xml file after DS4Windows startup.

## [3.11.1]
- Custom Notification Window System: profile changes use an independent notification window instead of Windows Action Center. Notifications appear at top-right, are focus-independent, and include system beep.
- Profile Notification Accuracy: controller connection notifications now display the actual active profile name.

## [3.11.0]
- Special Action Button Suppression Fix: when a Special Action is triggered, the last pressed button's other assigned function is now prevented from executing alongside the Special Action to avoid duplicate inputs.

