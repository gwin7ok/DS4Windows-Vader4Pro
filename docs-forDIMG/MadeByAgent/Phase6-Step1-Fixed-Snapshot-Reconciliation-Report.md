# Phase6-Step1 固定スナップショット最終照合

実施日: 2026-09-18

## 1. 結論と固定対象

**固定ソースの同一性確認・文字列集計再現・抽出可能なCore/UI参照表の位置照合を完了した。全件分類監査の完了とは区別する。**

| 対象 | 固定値 |
|---|---|
| ソースコミット | `62e69387ebb99d32003897b261ca2ac9fb3bebd8` |
| 初期監査コミット | `59d46db47cc9d20f212b0f6fa881515b767c91ed` |
| 両コミットのDS4Windowsツリー | `8eb02167bfed9d6c775bd7f5177ba10e9b5e2c66`（同一） |
| 照合した証拠文書のコミット | `cbf1dd172f2197be4f88574099e40d2adc1c9b9e` |
| 対象ルート | `G:\Cursor_Folder\DS4Windows-Vader4Pro` |
| C#マニフェストSHA-256 | `431637dc8a0f828bb2a9943f7bf6914c5bdd21b487d86eea1b0a3e056a751d39` |

証拠文書はソースより後のコミットに保存されているため、両者の版を別々に固定した。初期・固定コミット間のDS4Windows差分はゼロ。作業開始時のHEADは文書コミットと同じで、未コミット差分はゼロだった。

再現用: [Phase6-Step1-Verify-Snapshot.py](Phase6-Step1-Verify-Snapshot.py)。Python 3とGitを使用し、標準出力へJSONを返す。Gitオブジェクトの読取りだけを行い、チェックアウト・作業ツリー変更・ビルド・アプリ実行はしない。マニフェストはpath/blobの配列をpath順に並べ、スクリプトのJSON直列化条件でUTF-8化したSHA-256。JSONには286ファイル全件のpath/blob、97ファイルの集計、不一致・スキップ情報を出力する。

## 2. 固定ソースでの再集計

`git ls-tree` で固定コミット内の `DS4Windows/**/*.cs` を列挙し、bin/objディレクトリを除外。各blobをUTF-8で復号し、行ごとに大文字小文字を区別する `\bGlobal\s*\.` を集計した。

| 指標 | 前回作業ツリー走査 | 固定Gitオブジェクト走査 | 差 |
|---|---:|---:|---:|
| 走査ファイル | 286 | 286 | 0 |
| 一致ファイル | 97 | 97 | 0 |
| 一致物理行 | 1,255 | 1,255 | 0 |
| 一致出現回数 | 1,337 | 1,337 | 0 |

**1,337は文字列出現数であり、DI移行対象件数ではない。** コメント・文字列・const・無効な条件コンパイル分岐を含み、無修飾利用を含まない。手書き／生成コードの意味的な区別はしていない。前回はファイルシステム列挙、今回はGit管理ファイル列挙であり、総数の一致以上の同一性を推定しない。

| ファイル（DS4Windows配下） | 一致行 | 出現回数 |
|---|---:|---:|
| DS4Control/ControlService.cs | 84 | 93 |
| DS4Control/Mapping.cs | 69 | 69 |
| DS4Control/DS4LightBar.cs | 7 | 7 |
| DS4Control/Mouse.cs | 66 | 66 |
| DS4Control/MouseCursor.cs | 26 | 26 |
| DS4Control/MouseWheel.cs | 6 | 6 |
| DS4Control/ScpUtil.cs | 255 | 259 |
| App.xaml.cs | 53 | 55 |
| DS4Forms/MainWindow.xaml.cs | 24 | 24 |
| DS4Forms/ProfileEditor.xaml.cs | 96 | 102 |
| DS4Forms/ViewModels/SettingsViewModel.cs | 83 | 92 |

主レポートに記載した11ファイルの値も全て一致した。`using static DS4Windows.Global` はControlService:36、DS4LightBar:22、Mapping:31の3箇所で再現した。Global内部の自己参照や別形式の名前解決を網羅する検査ではない。

## 3. 保存済み参照表の照合

| 文書・対象 | 照合組数 | 位置の文字列一致 | 不一致 |
|---|---:|---:|---:|
| Core：§1–5の認識可能な参照表 | 468 | 468 | 0 |
| UI：§2–5の認識可能な参照表 | 436 | 436 | 0 |
| 合計 | 904 | 904 | 0 |
| Service：HidDevicesの2箇所のみ別途確認 | 2 | 2 | 0 |

組数とは、表から取り出した「文書行／ソースファイル／ソース行／メンバ名」の検査回数。重複排除済みの参照式数ではなく、同一行の複数出現も数え直していない。

検査方法:

- セクションからファイルを決定し、表の行番号とコード表記の先頭メンバを照合する。
- `outputKBMHandler.Sync` 等では根元メンバの単語存在を検査する。子メンバ・オーバーロード・Q/U形式・実際のシンボル束縛は検証しない。
- 「同上」は直前のメンバを継承し、outputKBMMapping専用表はその根元名を使用する。
- 認識した表内での範囲表記・メンバ不明によるスキップは0。**表として認識しなかった文章・表が存在しないという意味ではない。**
- Core §6以降のコメント除外表・根拠表、UI §1の契約比較・§6以降の提案や除外説明、Service全表はこの904組の機械検査対象外。分類・ホットパス・除外の妥当性もこの検査では確定しない。

## 4. 重要位置の個別確認と訂正

固定Git blobをUTF-8で直接復号して以下を確認した。

| 対象 | 確認結果 |
|---|---|
| ControlService:1416 | `Global.OutContType[index]` |
| ControlService:1792 | `Global.IsUsingUDPServerSmoothing()` |
| ControlService:2775 | `Global.GetGyroOutMode(device.JointDeviceSlotNumber)` |
| ControlService:2890 | `Global.GetSASteeringWheelEmulationAxis(ind)` |
| IProfileSettingsService:123 / 197 / 221 | GetGyroOutMode / GetSASteeringWheelEmulationAxis / OutContTypeの宣言 |
| ProfileApplicationService:103 | `deviceIndex < 0 || deviceIndex >= 4` |
| 同:116–118 | Global.ApplyProfile呼出しの戻り値を使用せず、`success = true` |
| 同:123–129 | MappingActionは直接実行し、それ以外のデバイスあり経路でHaltReportingRunAction |
| ProfileSettingsService:602–605 | GetReverseX360ButtonMappingがGlobal配列のCloneを返す |

**訂正1件**: Core別紙§7の逆引きgetter根拠位置「約619–622行」を「602–605行」に訂正した。Cloneによる割当の注意自体は維持する。

途中のPowerShellネイティブ出力配列では行位置が食い違ったため、その結果は不採用とした。同一blobをPythonのcheck_outputからUTF-8で直接復号すると、上記の主レポート行番号と一致することを再確認した。ControlServiceのLF区切りとsplitlinesの行数はともに3330で、他の行区切り文字は存在しなかった。主レポートの2775／2890／221を誤って変更していない。

## 5. 後続HidLibrary変更の影響

固定ソースから文書コミットまでの本番ソース差分は次の3ファイルのみ（18行追加／14行削除）。本作業による変更ではない。

- `HidLibrary/Extensions.cs`: null/空入力とNUL不在時の処理。
- `HidLibrary/HidDevices.cs`: デバイス説明のUTF-16変換と整形。
- `HidLibrary/NativeMethods.cs`: Win32 Unicodeエントリポイント指定と整形。

両固定コミットの `git grep -n` を比較し、3ファイル内の `Global.` はHidDevices:74、87の `Global.DeviceOptions.VerboseLogMessages` のみで、行番号・行内容とも一致した。Core/UI/DIソースに後続差分はない。

## 6. 完了範囲・残作業

- [x] 固定SHAとソースツリー・証拠文書版を特定。
- [x] 固定ソース286ファイルの文字列集計を再現。
- [x] Core/UI表から抽出した904組の位置を照合。
- [x] 重要契約位置とHidLibrary差分影響を確認。
- [x] 発見した根拠行番号の誤記1件を訂正。
- [ ] Global型の完全メンバ台帳、修飾／無修飾の全参照式台帳。
- [ ] Service表の全件展開、a/b/c・除外・ホットパスの全件確定。
- [ ] 分類別実測集計とPhase5完了証跡。

本番・テスト・DI登録は変更していない。ビルド・dotnet test・実機検証は行っていない。追加／更新物は照合文書、再現用読取り専用スクリプト、既存監査文書の追記・訂正だけである。Step2計画・承認状態は変更しない。

**最終判定: 固定スナップショット照合は上記範囲で完了。Phase6-Step1の全件分類監査は引き続き未完了。**
