## DS4Updater 統合仕様（canonical）

作成日: 2025-12-12

目的: DS4Windows の「更新を確認」機能が DS4Updater と連携して最新版の検出・ダウンロード・インストール（GUI）を行う際の責務分担、引数、動作フロー、エラーハンドリングを一元化する。

前提
- DS4Windows は GUI モードで Updater を呼び出す（`--ci` は使用しない）。
- Updater のリリースアセットは x64 アーカイブ（例: `DS4Updater_x64.zip`）を使用する。
- Updater 本体は最終的に `%ProgramFiles%\\DS4Updater\\DS4Updater.exe` に配置され、ローカルコピーを起動する。
- Updater 自身は `AdminNeeded()` 判定、セルフアップデート（バッチでの置換）、プロセス停止・置換を行う仕様を持つ（Updater側の実装に委譲）。

現状の実装状況（2025-12-14）
--------------------------------
- DS4Windows は `Changelog` ロジックで最新リリース判定を行い、ローカルに `DS4Updater.exe` が無ければ GitHub Releases の latest から x64 アーカイブをダウンロードして `%TEMP%` に保存、解凍して `DS4Updater.exe` を取り出す一連の流れを実装しています（実装箇所: `DS4Windows\DS4Control\ScpUtil.cs`、`externals/DS4Updater/Updater2/*` および `utils/post-build.py` に関連処理あり）。
- ダウンロード後の配置はまず非昇格でターゲットディレクトリへ移動を試み、失敗した場合は `ProcessStartInfo.Verb = "runas"` を使って昇格プロセスを起動し、`--complete-install` 系の引数で移動を完了させるフローをサポートします（実装は仕様どおりの引数形式を利用しています）。
- DS4Windows はローカルの `DS4Updater.exe` を GUI モード引数で起動します。起動時に渡す引数の一部（`--ds4windows-path`, `--ds4updater-path`, `-autolaunch`, `--launch-mode`, `--launchExe` 等）は実装済みで、`-autolaunch` を付与した場合 Updater 側で更新完了後に DS4Windows を自動的に再起動する想定です（呼び出し箇所: `externals/DS4Updater/Updater2/MainWindow.xaml.cs` 等）。
- 起動引数や Updater の配置ルールについては大半が実装されていますが、現時点で以下の運用差分・未解決点があります:
  - 実行時の二重起動の回避（Updater が自動で DS4Windows を再起動するケースと、Updater UI の "Run DS4Windows" ボタンをユーザーが押した場合の二重起動）が観測されています。これは Updater と UI 両方が `Process.Start` で起動を行うため発生しており、起動前に既存プロセスを検出して抑止する簡易対策を適用することを推奨します（未適用）。
  - インストール / 移動フローのログ出力は存在しますが、権限エラーや昇格キャンセル時のユーザー向けハンドリング（説明ダイアログや明示的な手動インストール案内）の一部は GUI 表示の整備が残っています。
  - `post-build.py` のアーカイブ生成や出力パスに関して、ローカルビルドと CI で出力レイアウトの差異があったためスクリプトを修正済み（ステージング挙動の統一／重複 zip の削除）。ただし、ビルドタスク（`.vscode/tasks.json`）と `release-and-publish.ps1` の期待する出力パスが完全に一致していないため、開発時に複数のフォルダレイアウトが観測される場合があります。

このセクションは実装と運用で見つかった現状のギャップを短くまとめたもので、後続の変更で差分が発生したら更新してください。

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
 
高レベル責務（現状の実装に合わせた明細）
- `DS4Windows` 側（実装済み/主要実装）:
  - 既存の `Changelog` ロジックで最新リリース判定を行う（実装済み）。
  - `DS4Updater` の x64 アーカイブを `latest` リリースからダウンロードして `%TEMP%` に保存し、解凍して `DS4Updater.exe` を取り出す処理を実装済み。
  - 取得した `DS4Updater.exe` をまず非昇格で `%ProgramFiles%\\DS4Updater\\DS4Updater.exe` へ移動し、書き込み権限エラー時は昇格（`ProcessStartInfo.Verb = "runas"` 相当）で `--complete-install` 系引数を使って移動を完了するフローを実装済み。
  - ローカルの `DS4Updater.exe` を GUI モード引数で起動する呼び出しを実装済み。渡す引数には `--ds4windows-path`、`--ds4updater-path`、`-autolaunch`、`--launch-mode`、`--launchExe` 等が含まれる（主要な引数は実装済み）。
  - ダウンロード／移動／起動の各ステップでログ出力を残す実装がある（主要イベントをログに出すようにしている）。
  - ビルド後パッケージング（`utils/post-build.py`）の修正により、重複 ZIP の生成とネストした `DS4Windows/DS4Windows` フォルダが発生しないよう調整済み（ローカルビルドの出力整形を改善）。

- `DS4Updater` 側（既存の責務、いくつかの改善を適用済み）:
  - Updater のセルフアップデート（自身の置換／バッチ置換）、プロセス停止、昇格判定 (`AdminNeeded`) 等は Updater 側で処理する設計で、DS4Windows はそのインターフェイスを利用する実装になっている。
  - Updater 起動後の自動再起動フロー（`-autolaunch`）により更新完了後に DS4Windows を再起動する責務は Updater にある。
  - 実装上の運用差分として、起動前の二重起動抑止（Updater の自動再起動と Updater UI の手動 Run 操作が重複して DS4Windows を同時に起動するケース）に対して、起動ヘルパー側で既存プロセス検出を行い抑止する簡易的な対策を適用済み（`StartProcessDetached` の前チェック等）。

注意（未完／運用上の差分）:
 - 昇格ダイアログや権限エラーの GUI 表示まわりは UX の追加整備がまだ必要（現在はログと基本ダイアログで誘導する実装）。
 - `.vscode/tasks.json` と `release-and-publish.ps1` の期待する出力パスに若干の差があり、開発中に観測される出力レイアウト差は残っている（ビルドタスクと CI の出力配置の整合性は今後の整備対象）。
起動引数（DS4Windows → DS4Updater）
- 最低限渡すもの（GUI モード）:
  - `--ds4windows-path "<DS4Windows 実行パス>"`
  - `--ds4updater-path "<DS4Updater 実行パス>"` 
  - `-autolaunch` （更新処理後に DS4Windows を自動的に再起動したい場合に使用）
  - `--launch-mode <admin|user>` （追加: 起動モード指定）
    - 説明: Updater に対して更新後に DS4Windows をどの権限モードで起動するかを明示する。`admin` を指定すると Updater は管理者（昇格）で起動を試み、`user` を指定すると通常ユーザー権限で起動する。
    - 備考: DS4Windows は Updater を起動する際、自身の現在の実行モード（管理者か通常ユーザーか）に合わせてこの引数を付与すること。
  - `--launchExe "<DS4Windows.exe の相対/絶対パス>"`（必要に応じて）
  - GUI のため `--ci` は渡さない。

ファイル／パスルール
- 優先度:
 1. 既に `%ProgramFiles%\\DS4Updater\\DS4Updater.exe` が存在する場合はそれを使用して起動。
 2. 存在しない場合は GitHub Releases から x64 アセットを `%TEMP%` にダウンロード → 移動（上記） → ローカル起動。

アセット選択
- 常に x64 ビルドを使用する。アーキテクチャ自動判定は不要（プロジェクトは x64 を想定）。

アセット URL の決定方法（明確化）
- `DS4Windows` が `DS4Updater.exe` をローカルで発見できない場合、DS4Windows は必ず起動時に参照する Updater リポジトリ（デフォルト: `gwin7ok/DS4Updater`、オーバーライド可能な引数: `--ds4updater-repo`）の**最新（latest）リリースに含まれる x64 向けアーカイブ**をダウンロードしてインストールします。
- この取得はリリースの "latest" タグにあるアセットから直接行うものであり、DS4Updater 側の Changelog を参照する必要はありません。ダウンロード URL はリポジトリのリリース配下にある形式になります（例: `https://github.com/gwin7ok/DS4Updater/releases/download/v3.0.0/DS4Updater_3.0.0_x64.zip`）。
- 実装上は GitHub Releases API を使って最新リリースのタグ名とアセット一覧を取得し、アセット名に `x64` を含む適切なアーカイブを選択してそのダウンロード URL を使用してください。アセット名にはバージョン番号や日付が含まれることがあるため、URL をハードコーディングしないこと。
- ダウンロード時には HTTP ステータスに加え、レスポンスヘッダの `Content-Length` 等でサイズを検証すること（チェックサム検証は将来対応でよい）。

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
    - 昇格時の引数伝搬（仕様）:
      - 昇格プロセスには、移動元の一時パスと移動先を明示的に渡すこと。
      - 推奨引数: `--complete-install "<sourcePath>" --complete-install-target "<targetPath>"`。
        - 例: `--complete-install "%TEMP%\\DS4Updater_x64.zip" --complete-install-target "%ProgramFiles%\\DS4Windows\\DS4Updater\\DS4Updater.exe"`
      - 実装上の扱い: エレベーション用のプロセスは（既存の実行ファイルを使うなら）`DS4Windows.exe` の特別モードまたは専用ヘルパーを用いて起動し、受け取った引数で移動を行い、終了コード `0` を返すと成功、それ以外は失敗とする。
      - ユーザーが UAC をキャンセルした場合、昇格プロセスは起動しないため元のプロセスはキャンセル扱いとして手動インストール案内へ遷移すること。
  e) 昇格での移動が成功したら一時ファイルを削除し、UI に `InstallSuccess_Notification` を表示する（通知/トースト）。
  f) 移動がキャンセルまたは失敗した場合、`InstallFailed_Title` / `InstallFailed_Body` ダイアログを表示し、`InstallFailed_OpenReleaseBtn` でリリースページを開けるよう案内する。
5. ローカル `...\\DS4Updater\\DS4Updater.exe` が用意できたら、DS4Windows はローカル Updater を起動する。以下は実装に合わせた具体的な挙動差分です。

  - 起動は DS4Windows の起動ヘルパー（`StartProcessDetached` 相当）を経由して行います。ヘルパーは起動対象が DS4Windows を再起動する用途（`-autolaunch` 等）である場合、既存の `DS4Windows` プロセスを検出して重複起動を抑止する前処理を行います。これにより、Updater の自動再起動と Updater UI の手動「Run」が同時に DS4Windows を起動してしまう二重起動リスクを低減しています。

  - DS4Windows は自身の実行権限に応じて `--launch-mode=admin` か `--launch-mode=user` を付与します。自動再起動を要求する操作（`Install Latest` の場合など）では `-autolaunch` を付与します。

  - 引数例（GUI モード）:
    - `--ds4windows-path "<DS4Windows 実行パス>"`
    - `--ds4updater-path "<DS4Updater 実行パス>"`
    - `-autolaunch`（必要時）
    - `--launch-mode <admin|user>`
    - `--launchExe "<フルパスまたは実行ファイル名>"`（推奨: フルパス）

  - 手動チェック（ユーザーが `今すぐ更新を確認` を押す動作）は `SkipVersion` を無視して常に最新リリースを問い合わせる挙動が実装されています。一方、`SkipVersion` はアプリ起動時の自動チェックを抑止するために記録されます。

6. Updater の処理完了／再起動挙動:
  - `DS4Updater` は更新処理を行い、`-autolaunch` が付与されていれば更新完了後に DS4Windows の起動を試みます。起動時も前述の起動ヘルパー側の前処理が働き、既に DS4Windows が存在する場合は起動をスキップします（簡易的な重複抑止）。
  - DS4Windows 側は Updater の終了コードを `Process.WaitForExit` で受け取り、成功/失敗をログに残します。
6. Updater の動作（起動後）:
  - `DS4Updater` は自身の `AdminNeeded()` 判定やセルフアップデート（プロセス停止→バッチでの置換）など、複雑な置換処理を管理する。
  - 必要に応じて `-autolaunch` を使って DS4Windows を再起動する。    
  - `DS4Updater` は GUI フローで更新を実行する（自身の `AdminNeeded()` 判定やセルフアップデート処理を行う）。
  - Updater が自身の置換を行う際は Updater 側のバッチ置換手順に従う（既存仕様）。
7. Updater が更新を完了し、必要に応じて DS4Windows を再起動する（`-autolaunch` が指定されている場合）。
8. ユーザーには完了通知（および失敗時は手動インストール案内）が表示される。
    `DS4Updater` 側の更新処理がインストール失敗で終了した（非ゼロ終了コードや明示的な失敗状態を返した）場合は、既定のブラウザで該当の GitHub Releases ページを開いて手動ダウンロード／確認を促す（`InstallFailed_*` ダイアログの代替／補完として即時オープンすること）。


DS4Updater.exe 起動引数一覧と DS4Windows 側の設定
以下、DS4Windows が Updater を GUI モードで起動する際に実際に渡す主要引数と、実装上の補足です。

- `--ds4windows-path "<path>"`
  - 説明: DS4Windows の実行ルート/インストールフォルダを明示する（推奨: フルパス）。
  - 実装: 実行中プロセスの実行パスから親ディレクトリを自動取得して渡します。

- `--ds4updater-path "<path>"`
  - 説明: 起動する `DS4Updater.exe` の所在ディレクトリ。
  - 実装: `Path.Combine(ds4WindowsDir, "DS4Updater")` 相当で自動生成して渡します。

- `-autolaunch`
  - 説明: 更新完了後に DS4Windows を自動再起動するフラグ（必要時に付与）。

- `--launch-mode "<admin|user>"`
  - 説明: 更新後に DS4Windows をどの権限モードで起動するかを指定します。DS4Windows の現行実行権限に応じて `admin` か `user` を渡します。

- `--launchExe "<フルパス|実行ファイル名>"`
  - 説明: 更新後に起動する実行ファイルを指定。実装では可能な限りフルパスを渡すことを推奨します。

- `--ds4updater-repo "<owner/repo>"`（任意）
  - 説明: Updater のリポジトリをオーバーライドします。開発／検証時に別のリポジトリからアセットを取得したい場合に使用します。
  - 例: `--ds4updater-repo "gwin7ok/DS4Updater"`

 - `--complete-install "<sourcePath>"`（昇格／インストール用、内部）
  - 説明: ダウンロード済みの一時ファイルや解凍済み実行ファイルを管理者権限でターゲットへ移動するために昇格プロセスへ渡す引数。通常は `DS4Windows` が昇格を要求する際に使用します。
  - 使い方（推奨）: 昇格プロセスを起動する際に `--complete-install "%TEMP%\\DS4Updater_x64.zip" --complete-install-target "%ProgramFiles%\\DS4Updater\\DS4Updater.exe"` のようにして渡します。

 - `--complete-install-target "<targetPath>"`（昇格／インストール用、内部）
  - 説明: `--complete-install` で指定されたソースを配置する最終ターゲットパスを指定します。昇格後プロセスはこのパスへファイルを移動・配置します。

 - `--ci`（オプション: CI / 非 GUI モード）
  - 説明: Updater の非対話（CI）モードを有効にするフラグ。DS4Windows は GUI 連携向けの呼び出しではこのフラグを渡しません（GUI フロー用に `--ci` を渡さないことが前提）。

実装上の注意 / 引数の安全な渡し方
 - Windows のコマンドラインは空白や特殊文字で壊れやすいため、可能であれば .NET の `ProcessStartInfo.ArgumentList` を使って個々の引数を安全に渡すことを推奨します。`ArgumentList` が使えない場合は、`ProcessStartInfo.Arguments` へ渡す際に十分にクオートして（例: `"..."`）ください。
 - 昇格用に `ProcessStartInfo.Verb = "runas"` を使う場合、引数の受け渡しにシェルの解釈差異が出るため、昇格対象プロセス側で受け取った引数の検証を必ず行ってください。
 - DS4Windows は通常、`--ci` を渡さず GUI モードで Updater を起動します。CI 用の挙動をテストする場合は明示的に `--ci` を付与してください。

互換性と運用ノート
 - `--ds4updater-repo` / `--ds4windows-repo` は開発／テスト用のオーバーライド引数です。本番運用では指定しない運用が標準です。
 - 昇格フローで渡す `--complete-install` 系引数は一時ファイルのパスやターゲットパスを明示することになり、ログに出力する際は秘密情報（ユーザーのホームディレクトリなど）を不用意に晒さないよう取り扱いに注意してください。
  - 説明: Updater のリポジトリをオーバーライドする開発／テスト用引数。通常は渡しません（デフォルト: `gwin7ok/DS4Updater`）。

- `--ds4windows-repo "<owner/repo>"`（任意）
  - 説明: DS4Windows のリポジトリをオーバーライドする引数。通常は渡しません（デフォルト: `gwin7ok/DS4Windows-Vader4Pro`）。開発／テスト目的でリリース参照先を差し替える場合に使用します。

実装に合わせた補足:
- DS4Windows は Updater を起動する際に起動ヘルパー（`StartProcessDetached` 相当）を使用します。ヘルパーは起動対象が DS4Windows の再起動を伴う場合に既存プロセスを検出し、重複起動を抑止する前処理を行います。
- `-autolaunch` を付与して Updater が更新完了後に DS4Windows を起動する場合でも、DS4Windows 側の起動ヘルパーで二重起動が抑止されます（簡易対策）。
- 引数は安全にクオートして渡してください（空白や特殊文字対応）。.NET の `ProcessStartInfo.ArgumentList` の利用を推奨します。

引数の選定・検証:
- DS4Windows 側は Updater が見つからない場合に GitHub Releases の latest から x64 アセットを選んでダウンロードします。アセット選択は `x64` を含むファイル名でフィルタし、HTTP ステータスと `Content-Length` で基本検証を行います（チェックサムは将来対応）。

重要な UX の条件
- 昇格は最小限に留め、ユーザーに必ず事前確認を行うこと。
- 失敗ケースではリリースページを開くオプションを提示すること。

ログ出力
- 各ステップ（ダウンロード開始/完了、移動試行/成功/失敗、昇格の有無、Updater 起動コマンド、Launcher の重複抑止トリガー）をログに残すこと。

実際に DS4Windows が Updater を起動する際に設定している引数（実装で使用される文字列）
 - 既存の `DS4Updater.exe` を起動する（非昇格／直接起動）場合の Arguments 文字列例:
   --ds4windows-path "{ds4WindowsDir}" --ds4updater-path "{ds4UpdaterDir}" -autolaunch --launchExe "{ds4WindowsExe}" --launch-mode={admin|user}
   （実装箇所: `DS4Windows\DS4Forms\UpdaterWindow.xaml.cs` — ProcessStartInfo.Arguments）

 - 初回インストール後に非昇格で起動する場合（インストール成功・非昇格版）の Arguments 文字列例:
   --ds4windows-path "{ds4WindowsDir}" --ds4updater-path "{ds4UpdaterDir}" -autolaunch --launchExe "{ds4WindowsExe}"
   （実装ではこのケースでは `--launch-mode` が付与されません）

 - 初回インストール後に昇格して起動する場合（昇格後の起動）の Arguments 文字列例:
   --ds4windows-path "{ds4WindowsDir}" --ds4updater-path "{ds4UpdaterDir}" -autolaunch --launchExe "{ds4WindowsExe}" --launch-mode={admin|user}

実装ノート:
 - 引数中のプレースホルダは実際に `AppDomain.CurrentDomain.BaseDirectory` や `Process.GetCurrentProcess().MainModule.FileName`、`Global.IsAdministrator()` の戻り値で埋められます。
 - 起動は `ProcessStartInfo`（実装では `UseShellExecute = false`）を使って直接 `Process.Start` しています。失敗時には Updater のリリースページを `Util.StartProcessHelper(url)` で開くフォールバックが使われます。
 - 上記は DS4Windows の現行実装での実際の引数フォーマットであり、Updaters 側の仕様書（`externals/DS4Updater/Updater2/DS4Updater_SPEC.md`）に示す引数群と整合しています。
