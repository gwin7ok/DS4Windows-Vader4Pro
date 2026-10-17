# フェーズ4-Step10-2-A Stage1 完了報告書

作成日: 2026-09-02
対象ブランチ: `For-DI-migration-work`
対象: Step10-2-A（A-1〜A-9）
実機確認リスト: `docs-forDIMG/MadeByAgent/Phase4-Step10-2-A-RealDevice-Verification-Checklist.md`

## 1. 実施概要

Step10-2-Aの全9サブタスクについて、`IProfileSettingsService`への契約追加、`ProfileSettingsService`から`BackingStore`への委譲、`Global`後方互換シムの接続を完了した。

Stage1完了後、Vader 4 Proを使用したベースライン実機検証を実施した。検証結果は、`○`、`△`、`未実施`として実機確認リストへ記録した。

## 2. 実機検証結果

### 正常確認（○）

- Debug x64ビルドの起動、コントローラー認識
- プロファイルバックアップ、保存、再起動後の再読込
- DIホスト解決、ViewModel FactoryによるViewModel生成
- スティック、トリガー、ライトバー、基本ボタン入力
- Bluetooth接続・切断、仮想コントローラーのDS4／Xbox切替
- プロファイル変更通知、Controlsタブのボタン設定
- 仮想コントローラー、KBM出力、SpecialAction
- 全画面の基本操作、UIバインディング、ログのエラー確認

### DI化完了後に調査・検討する項目（△）

以下は、今回のStage1ベースラインでは機能の一部制限、既存症状、またはDI経路未接続を確認した項目である。Stage2以降のDI化完了後に、原因調査、仕様確認、必要な修正および追加実機検証を行う。

- `2-2`: プロファイル読込・保存・切替は現状`Global`経路で実行され、`ProfileRepository`のDIログが出力されない。
- `2-3`: 多くの設定変更はGlobal配列のLegacyシム経由で、`ProfileSettingsService`の変更ログが出力されない。
- `1-4`: 設定値全体を既定値へ戻す機能の要否・仕様が未確定。GUIウィンドウサイズ等の既存復元機能は正常動作。
- `3-3`／`4-3`: タッチパッドのタップによるクリックが反映されない。Controlsタブの画像と実際のボタン位置にも既存のずれがある。
- `3-4`／`4-4`: ジャイロマウスモードでポインタが上へ動き続ける既存症状。
- `3-6`: マクロレコード画面でクリックは記録されるが座標が記録されない既存症状。仕様か不具合かを確認する。
- `3-7`／`4-5`: SA設定・対応デバイスを実機で確認できなかったため、機能の有無と検証方法を確認する。
- `5-3`: KBMマッピング初期化機能の有無と検証方法を確認する。
- `5-5`: UAC昇格プログラム起動の条件と管理者権限の扱いを確認する。
- `6-3`: SpecialActionの1回のトリガーで複数回のプロファイル切替が発生する、または連続切替が停止しない既存症状。

### 未実施

- `6-4`: 長時間稼働、コントローラー抜き差し、スリープ復帰の確認は今回未実施。DI化完了後に実施する。

なお、ChromeのGamepad Testerサイトだけ反応せずWaterfoxでは反応した事象は、仮想コントローラー出力自体の不具合とは断定せず、ブラウザー差異として記録した。

## 3. Stage1の判定

- A-1〜A-9の実装、自動テスト、デバッグビルドは完了。
- Stage1の実機ベースライン検証は完了。
- `△`および`未実施`項目は残課題として記録済み。
- これらの残課題は、DI化完了後に調査・検討・追加実機検証を行う。
- Stage2では、まず`ControlService.cs`、`Mapping.cs`、`ProfileSettingsViewModel.cs`の呼び出し元DI直接参照化を進める。

## 4. 成果物

- `DS4Windows/DI/IProfileSettingsService.cs`
- `DS4Windows/DS4Control/Services/ProfileSettingsService.cs`
- `DS4Windows/DS4Control/ScpUtil.cs`
- `docs-forDIMG/MadeByAgent/Phase4-Step10-2-A-1-Completion-Report.md`〜`Phase4-Step10-2-A-9-Completion-Report.md`
- `docs-forDIMG/MadeByAgent/Phase4-Step10-2-A-RealDevice-Verification-Checklist.md`

## 5. 次のステップ

Stage2（Step10-2-B）では、Stage1で接続したDIサービスを対象に、3ファイルをカテゴリ単位で段階的にDI直接参照化する。Stage2完了後、本チェックリストを再実施し、今回の`△`および未実施項目を再評価する。
