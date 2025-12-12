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
    - 昇格時の引数伝搬（仕様）:
      - 昇格プロセスには、移動元の一時パスと移動先を明示的に渡すこと。
      - 推奨引数: `--complete-install "<sourcePath>" --complete-install-target "<targetPath>"`。
        - 例: `--complete-install "%TEMP%\\DS4Updater_x64.zip" --complete-install-target "%ProgramFiles%\\DS4Windows\\DS4Updater\\DS4Updater.exe"`
      - 実装上の扱い: エレベーション用のプロセスは（既存の実行ファイルを使うなら）`DS4Windows.exe` の特別モードまたは専用ヘルパーを用いて起動し、受け取った引数で移動を行い、終了コード `0` を返すと成功、それ以外は失敗とする。
      - ユーザーが UAC をキャンセルした場合、昇格プロセスは起動しないため元のプロセスはキャンセル扱いとして手動インストール案内へ遷移すること。
  e) 昇格での移動が成功したら一時ファイルを削除し、UI に `InstallSuccess_Notification` を表示する（通知/トースト）。
  f) 移動がキャンセルまたは失敗した場合、`InstallFailed_Title` / `InstallFailed_Body` ダイアログを表示し、`InstallFailed_OpenReleaseBtn` でリリースページを開けるよう案内する。
5. ローカル `...\\DS4Updater\\DS4Updater.exe` が用意できたら、DS4Windows はローカル Updater を起動する。
  - 渡す引数の例（GUI モード）:
    - `--ds4windows-path "<DS4Windows 実行パス>"`
    - `--ds4updater-path "<DS4Updater 実行パス>"` （明示推奨）
    - 必要に応じて `-autolaunch` と `--launchExe` を付与して、更新完了後に DS4Windows を自動再起動させるフローを指定する。
      - DS4Updater起動後は DS4Windows側は `Process.WaitForExit` で終了コードを取得して判定すること（成功時は `ExitCode==0`、失敗時は非ゼロ）。
      - `--launchExe` の扱い: `--launchExe` には起動したい実行ファイルの**フルパス**を渡すことを推奨する（例: `"C:\Program Files\DS4Windows\DS4Windows.exe"`）。Updater 側が実行ファイル名のみを受け取る実装でもフルパスは互換的に扱えるが、フルパスを渡すと起動先が明確になる。
6. Updater の動作（起動後）:
  - `DS4Updater` は自身の `AdminNeeded()` 判定やセルフアップデート（プロセス停止→バッチでの置換）など、複雑な置換処理を管理する。
  - 必要に応じて `-autolaunch` を使って DS4Windows を再起動する。    
  - `DS4Updater` は GUI フローで更新を実行する（自身の `AdminNeeded()` 判定やセルフアップデート処理を行う）。
  - Updater が自身の置換を行う際は Updater 側のバッチ置換手順に従う（既存仕様）。
7. Updater が更新を完了し、必要に応じて DS4Windows を再起動する（`-autolaunch` が指定されている場合）。
8. ユーザーには完了通知（および失敗時は手動インストール案内）が表示される。
    `DS4Updater` 側の更新処理がインストール失敗で終了した（非ゼロ終了コードや明示的な失敗状態を返した）場合は、既定のブラウザで該当の GitHub Releases ページを開いて手動ダウンロード／確認を促す（`InstallFailed_*` ダイアログの代替／補完として即時オープンすること）。


DS4Updater.exe 起動引数一覧と DS4Windows 側の設定
以下、両仕様書（DS4Updater_INTEGRATION_SPEC.md と DS4Updater_SPEC.md）を基に抽出した起動引数一覧と DS4Windows 側での設定内容のまとめ。

--ds4windows-path "<path>"
説明: DS4Windows の実行/ルートパスを Updater に明示する。
推奨: 常に渡す。値は DS4Windows のインストール実行ファイルの親ディレクトリ（例: C:\Program Files\DS4Windows）。
→ 自動取得可: `DS4Windows.exe` の親ディレクトリ（例: `%ProgramFiles%\DS4Windows`）。
  - 実装上の注意: `DS4Windows` 側では実行中プロセスの実行パスから自動検出可能なので、通常はその自動取得値を使って渡す。

--ds4updater-path "<path>"
説明: Updater の実行ディレクトリ（self-update で使用）。
推奨: 常に渡す。値は起動する DS4Updater.exe のディレクトリ（例: %ProgramFiles%\DS4Updater または DS4Windows サブフォルダに配置する場合のパス）。
→ DS4Updater.exe を含むディレクトリのパス（例: `%ProgramFiles%\DS4Windows\DS4Updater`）。
  - 実装上の注意: `DS4Windows` 側では `Path.Combine(ds4WindowsDir, "DS4Updater")` のように自動生成して渡してください。複雑なフォールバック探索はテストと保守を困難にするため追加しないでください。

-autolaunch
説明: 更新完了後に DS4Windows を自動再起動するフラグ。
推奨: ユーザーが「Install Latest」でインストールを開始した場合に付与する（自動再起動を行いたい場合）。オプションで UI にチェックを出して制御しても良い。
→付加

--launchExe "<name>"
説明: Updater に対して更新後に起動する実行ファイル名を指定。
推奨: -autolaunch を付与する場合に合わせて渡す（通常は DS4Windows.exe）。フルパスではなく実行ファイル名を使う既存仕様に沿う。
説明: -autolaunch を付ける場合、--launchExe を合わせて渡すことで Updater が更新後に指定実行ファイルを起動する。
推奨: 自動再起動を有効にするなら両方渡す。
伝達方式 / 値の取得
→"DS4Windows.exe"

--ds4updater-repo "<url|owner/repo>"
説明: Updater 自身のリポジトリをオーバーライドする（テストや別ビルド向け）。
推奨: 通常は渡さない（デフォルトを使用）。開発／テスト目的でのみ明示的に渡す。
→"gwin7ok/DS4Updater"

--ds4windows-repo "<url|owner/repo>"
説明: DS4Windows のリポジトリを指定して Releases を参照させる。
推奨: 通常は渡さない（内蔵設定を使用）。リポジトリを差し替える必要がある環境のみ渡す。
→"gwin7ok/DS4Windows-Vader4Pro"

--base-url "<legacy>"
説明: 互換用（--ds4windows-repo と同等扱い）。
推奨: 使用しない（互換性のみ）。新実装では --ds4windows-repo を使う。
→なし

--ci
説明: CI/非対話モード（JSON 出力、昇格プロンプト抑制、終了コードで判定）。
推奨: DS4Windows は GUI フローで呼ぶため 渡さない（仕様に従う）。内部自動化／テスト用に別で使う場合のみ。
→なし

-skipLang
説明: 言語パックダウンロードをスキップするフラグ。
推奨: 通常は渡さない。言語パックを別で管理したい特殊ケースのみ。
→なし

-user
説明: AutoOpen 時にユーザー権限で起動することを強制するフラグ（Updater 側の挙動に影響）。
推奨: 通常は渡さない。昇格を避けたい特殊フローでのみ検討。
（補助）--launchExe と -autolaunch の併用挙動
→なし

引数についての注意事項
パスに空白が含まれる可能性があるため、ProcessStartInfo.ArgumentList（.NET）や確実なクオート方法で安全に渡すこと。


重要な UX の条件
- 昇格は最小限に留め、ユーザーに必ず事前確認を行うこと。
- すべての失敗ケースでリリースページを開くオプションを提示する。

ログ出力
- 各ステップ（ダウンロード開始/完了、移動試行/成功/失敗、昇格の有無、Updater 起動コマンド）をログに残す。
