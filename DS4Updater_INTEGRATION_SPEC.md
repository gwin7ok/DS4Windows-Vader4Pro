## DS4Updater 統合仕様（canonical）

作成日: 2025-12-12

目的: DS4Windows の「更新を確認」機能が DS4Updater と連携して最新版の検出・ダウンロード・インストール（GUI）を行う際の責務分担、引数、動作フロー、エラーハンドリングを一元化する。

前提
- DS4Windows は GUI モードで Updater を呼び出す（`--ci` は使用しない）。
- Updater のリリースアセットは x64 アーカイブ（例: `DS4Updater_x64.zip`）を使用する。
- Updater 本体は最終的に `%ProgramFiles%\\DS4Updater\\DS4Updater.exe` に配置され、ローカルコピーを起動する。
- Updater 自身は `AdminNeeded()` 判定、セルフアップデート（バッチでの置換）、プロセス停止・置換を行う仕様を持つ（Updater側の実装に委譲）。

高レベル責務
- DS4Windows の責務:
 - DS4Windows の責務:
  - 最新リリースの検出（既存の `Changelog` 実装を利用）。
  - リリースから x64 アーカイブアセットをダウンロードし、一時ディレクトリ（`%TEMP%`）に保存して検証する（HTTP ステータス / サイズ等）。
  - ダウンロードしたアーカイブを一時フォルダで解凍し、内部の `DS4Updater.exe`（または実行可能ファイル）を取り出す（Aプラン: ダウンロード→tmp格納→解凍→移動）。
  - ローカルに `...\\DS4Updater\\DS4Updater.exe` が存在しない場合、取得した `DS4Updater.exe` を `...\\DS4Updater\\DS4Updater.exe` に移動する試行を行う（移動 = 単純ファイル配置/インストール）。
    - この移動操作は初回インストールに限定し、以降のプロセス停止／セルフアップデート／置換等の複雑な処理は `DS4Updater` 側の責務とする（DS4Windows は Updater のセルフ置換ロジックを実行しない）。
    - 移動が失敗した場合（アクセス拒否などの権限エラー）、ユーザーに昇格確認を表示し、同意があれば管理者権限で移動を再試行する。
    - 昇格は `ProcessStartInfo.Verb = "runas"` を使うか、同等のシェル昇格手段で行う。
  - ローカルの `DS4Updater.exe` を GUI モード引数で起動する（下記参照）。
  - 失敗時はリリースページを開く案内を行う。

- DS4Updater の責務:
  - 必要であればさらに自分自身の置換（セルフアップデート）やサービス停止／再配置を行う。
  - `--ci` 以外の GUI フローを提供する（ユーザー確認や進捗表示）。

起動引数（DS4Windows → DS4Updater）
- 最低限渡すもの（GUI モード）:
  - `--ds4windows-path "<DS4Windows 実行パス>"`
  - `--ds4updater-path "<DS4Updater 実行パス>"` 
  - `-autolaunch` （更新処理後に DS4Windows を自動的に再起動したい場合に使用）
  - `--launchExe "<DS4Windows.exe の相対/絶対パス>"`（必要に応じて）
  - GUI のため `--ci` は渡さない。

ファイル／パスルール
- 優先度:
 1. 既に `%ProgramFiles%\\DS4Updater\\DS4Updater.exe` が存在する場合はそれを使用して起動。
 2. 存在しない場合は GitHub Releases から x64 アセットを `%TEMP%` にダウンロード → 移動（上記） → ローカル起動。

アセット選択
- 常に x64 ビルドを使用する。アーキテクチャ自動判定は不要（プロジェクトは x64 を想定）。

UI 表示要素（追加済みリソースキー）
- 英語 / 日本語で追加したキーの一覧（`Translations/Strings.resx` / `Strings.ja.resx`）:
  - `UpdaterMissing_Title`, `UpdaterMissing_Body`, `Install_LatestBtn`, `UpdaterMissing_OpenReleaseBtn`, `UpdaterMissing_CancelBtn`
  - `Elevation_Title`, `Elevation_Body`, `Elevation_Yes`, `Elevation_No`, `Elevation_Footer`
  - `InstallSuccess_Notification`, `InstallFailed_Title`, `InstallFailed_Body`, `InstallFailed_OpenReleaseBtn`, `InstallFailed_CloseBtn`

エラーハンドリング
- ダウンロード失敗: リリースページを開く案内を表示。
- 移動失敗（権限エラー）: ユーザーへ昇格の確認を行い、同意で昇格して再試行。キャンセルなら手動インストール案内。
- 起動失敗: エラーメッセージを表示しログを残す。

セキュリティ留意点
- ダウンロード元は公式 GitHub Releases のみを使用し、チェックサム等による追加検証を将来的に検討する。
- 実行ファイルの配置は最小限のタイミングで昇格を要求する（最初の移動時のみ）。

運用とデバッグ
- ログ: ダウンロード URL、HTTP ステータス、移動の成功/失敗（権限エラーコード）をログに残す。
- リカバリ: 移動に失敗した場合 `%TEMP%` に残したダウンロード済みファイルのパスをユーザーに提示して手動インストールを案内。

今後の実装TODO（優先順）
1. `UpdaterInstaller` ヘルパークラスを実装（ダウンロード→tmp保存→移動→起動、昇格プロンプト含む）。
2. `UpdaterWindow` の「リリースページを開く」ボタンを `Install Latest`（`Install_LatestBtn` リソース）に差し替え、`UpdaterInstaller` を呼び出す。
3. 起動引数の最終調整と再起動フローの検証（`-autolaunch` の利用条件など）。
4. 統合テスト: 権限あり/なし環境でのインストール・起動テスト。

付記: このファイルを今後の仕様変更の基準（canonical）としてください。仕様変更がある場合はここを更新し、実装との整合を取ること。

ユーザー操作から完了までの最終フロー
---------------------------------
以下はユーザーが `今すぐ更新を確認`（UI のボタン）を押してから、Updater のインストールが完了し DS4Windows が起動されるまでの順序付きフロー（最終版）です。

前提:
- ボタンは `CheckUpdateNow`（既存）で、Updater が未インストール時はダイアログを表示する。
- 追加表示はリソースキーを利用する: `UpdaterMissing_Title`, `UpdaterMissing_Body`, `UpdaterMissing_InstallBtn`, `UpdaterMissing_OpenReleaseBtn`, `UpdaterMissing_CancelBtn`, `Elevation_*`, `InstallSuccess_Notification`, `InstallFailed_*`。

1. ユーザーが `今すぐ更新を確認` をクリックする。
2. アプリは既存の `Changelog` ロジックで GitHub Releases の最新版を確認する。
  - 新しいリリースがなければ通常の通知を表示して終了する。
3. 新しいバージョンがあると判定された場合、アプリは **更新履歴ウィンドウ（`UpdaterWindow`）を表示** する。表示内容はリリースノート（Changelog）で、ウィンドウは次の3つの操作ボタンを持つ。ボタン文言はローカル言語のリソースを使用する（日本語なら `Strings.ja.resx` の値を表示）。

 - 左ボタン — `SkipVersion` (`SkipVersion` リソース):
  - 挙動: 当該バージョンを「スキップ済み」として記録し、ウィンドウを閉じます。
    - 実装例: `updaterWinVM.SetSkippedVersion()` を呼び、`Global.LastVersionChecked` にバージョンを保存する（現在の実装に準拠）。

 - 中央ボタン — `Install Latest` (`Install_LatestBtn` リソース):
  - 挙動（合意済み）: メインのインストール／起動アクションを実行します。
    - ローカルに `%ProgramFiles%\\DS4Updater\\DS4Updater.exe` が存在する場合: そのローカル `DS4Updater.exe` を GUI 引数で起動する（`--ds4windows-path` 等を付与）。
    - 存在しない場合: `UpdaterInstaller` を呼び出して Aプランのインストール（ダウンロード→%TEMP%→解凍→移動→昇格再試行→ローカル起動）を行う。

 - 右ボタン — `Close` (`CloseButton` リソース):
  - 挙動: 単純にウィンドウを閉じます。スキップ記録は行われません（`SkipVersion` と動作が異なります）。

4. ユーザーが `Install Latest`（`Install_LatestBtn`）を選択したら、アプリは以下を順に行う（`Install / Update` 選択時の最初のローカル確認を含む）:
  a) まずローカルに `%ProgramFiles%\\DS4Updater\\DS4Updater.exe` が存在するかを確認する。
    - 存在する場合: そのローカル `DS4Updater.exe` を GUI 引数で起動する（`--ds4windows-path` 等）。→5へ進む。
    - 存在しない場合: 以下の `UpdaterInstaller` の手順に従ってインストールを行う。
  b) GitHub Releases から x64 アーカイブアセット（zip 等）をダウンロードして `%TEMP%` に保存する。HTTP ステータスとサイズを検証し、アーカイブを解凍して中の `DS4Updater.exe` を取り出す。
  c) ダウンロード成功後、ターゲットフォルダ `%ProgramFiles%\\DS4Updater\\` を確認し、存在しなければ作成を試みる（非昇格でファイル作成できるか一度試す）。
  d) ダウンロードしたファイル（`%TEMP%\\<asset>`）を `...\\DS4Updater\\DS4Updater.exe` に移動しようとする。
    - 移動が成功すれば次へ進む。
    - 移動が失敗し、エラーが書き込み権限（アクセス拒否）に起因する場合は `Elevation` 確認ダイアログを表示する:
      - タイトル: `Elevation_Title`
      - 本文: `Elevation_Body`
      - 選択: `Elevation_Yes`（昇格して移動を再試行） / `Elevation_No`（キャンセル、手動案内）
  d) ユーザーが昇格を承認した場合、昇格プロセスを新しいプロセス（`ProcessStartInfo.Verb = "runas"`）で起動し、管理者権限で移動を実行する。昇格後のプロセスは元のダウンロードファイルを指定してターゲットへ配置する。
  e) 昇格での移動が成功したら一時ファイルを削除し、UI に `InstallSuccess_Notification` を表示する（通知/トースト）。
  f) 移動がキャンセルまたは失敗した場合、`InstallFailed_Title` / `InstallFailed_Body` ダイアログを表示し、`InstallFailed_OpenReleaseBtn` でリリースページを開けるよう案内する。
5. ローカル `...\\DS4Updater\\DS4Updater.exe` が用意できたら、DS4Windows はローカル Updater を起動する。
  - 渡す引数の例（GUI モード）:
    - `--ds4windows-path "<DS4Windows 実行パス>"`
    - `--ds4updater-path "<DS4Updater 実行パス>"` （明示推奨）
    - 必要に応じて `-autolaunch` と `--launchExe` を付与して、更新完了後に DS4Windows を自動再起動させるフローを指定する。
6. Updater の動作（起動後）:
  - `DS4Updater` は自身の `AdminNeeded()` 判定やセルフアップデート（プロセス停止→バッチでの置換）など、複雑な置換処理を管理する。
  - 必要に応じて `-autolaunch` を使って DS4Windows を再起動する。    
6. `DS4Updater` は GUI フローで更新を実行する（自身の `AdminNeeded()` 判定やセルフアップデート処理を行う）。
  - Updater が自身の置換を行う際は Updater 側のバッチ置換手順に従う（既存仕様）。
7. Updater が更新を完了し、必要に応じて DS4Windows を再起動する（`-autolaunch` が指定されている場合）。
8. ユーザーには完了通知（および失敗時は手動インストール案内）が表示される。
    `DS4Updater` 側の更新処理がインストール失敗で終了した（非ゼロ終了コードや明示的な失敗状態を返した）場合は、既定のブラウザで該当の GitHub Releases ページを開いて手動ダウンロード／確認を促す（`InstallFailed_*` ダイアログの代替／補完として即時オープンすること）。

重要な UX の条件
- 昇格は最小限に留め、ユーザーに必ず事前確認を行うこと。
- すべての失敗ケースでリリースページを開くオプションを提示する。

ログ出力
- 各ステップ（ダウンロード開始/完了、移動試行/成功/失敗、昇格の有無、Updater 起動コマンド）をログに残す。
