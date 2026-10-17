# Phase6 追加作業計画書: Flydigi Vader 4 Pro デバイスサポートの削除（機能廃止）

作成日: 2026-10-14  
状態: 承認済み（2026-10-14「計画どおり実施」）・実装済み・ビルド、テスト、実機確認の結果待ち  
位置づけ: Phase6-Step10-2 の実機確認中に発見された既存不具合を契機とする、機能廃止の追加作業（DI 化の作業とは別のコミットで行う）  
準拠: `.github/copilot-instructions.md` §2.2（機能廃止の例外。ユーザーの明示的な承認を得たもの）、§2.3、§4

---

## 1. 経緯
- Step10-2-2 の実機確認で、「デバイスオプション」画面の「Key: ControllerSupportVader4Pro」のチェックを外して再起動すると、チェックが入った状態に戻ることが判明した（2026-10-14）。
- 原因（置換前からの不具合）:
  - `Vader4ProDeviceOptions.Enabled` は、設定ファイル（`AppSettingsDTO` の `InputDeviceOptions`）に保存・読み込みの定義が無い（DS4／DualSense／Switch Pro／JoyCon／DS3 には定義がある）。起動のたびに既定値（`true`）に戻る。
  - 表示名のリソース文字列 `ControllerSupportVader4Pro` が、どの `Strings.*.resx` にも無く、キー名がそのまま表示される（「Key: …」）。
- ユーザーの判断（2026-10-14、AskUserQuestion）: 「Vader4Pro のサポート全体を削除」（選択肢 C）。「このデバイスをサポートする機能自体をなくしてしまってもいい」との意向。

## 2. 決定
- **Flydigi Vader 4 Pro（VID `0x04B4`系、PID `0x2412`）のデバイスサポート全体を削除する。** 機能廃止の例外（§2.2）として、ユーザーが明示的に承認した。
- 削除の対象は「デバイスのサポート（検出、入力レポートの解析、デバイス固有の設定、設定画面）」のみ。アプリ・リポジトリの名称や、About／Updater のリポジトリ名の表記（`DS4Windows-Vader4Pro`、"Vader 4 Pro Edition"）は変更しない（フォークの名称であり、デバイスサポートではない）。
- 影響: Vader 4 Pro を接続しても、DS4Windows は専用デバイスとして認識しない。

## 3. 削除の範囲（現行コード 2026-10-14 の調査）
- **デバイス実装（ファイルごと削除）**:
  - `DS4Windows/DS4Library/InputDevices/Vader4ProDevice.cs`（402 行）
  - `DS4Windows/DS4Library/InputDevices/Vader4ProReport.cs`
  - `DS4Windows/DS4Control/DTOXml/Vader4ProControllerOptsDTO.cs`
- **デバイスの種別・検出**:
  - `InputDeviceFactory.cs`: `InputDeviceType.Vader4Pro` の列挙値と、`case InputDeviceType.Vader4Pro` の生成。
  - `DS4Devices.cs`: `VADER4PRO_PID`、`VidPidInfo` の登録（190 行）、`&mi_02` の絞り込み（258 行付近。`FLYDIGI_VID` が他で使われていなければ定数も削除）、シリアルの取得分岐（355 行付近）。
  - `HidDevice.cs`: `GetVader4ProMacAddress`。
- **設定・画面**:
  - `ControlServiceDeviceOptions.cs`: `Vader4ProDeviceOptions`、`Vader4ProControllerOptions`、`Vader4ProDeviceOpts` プロパティ。
  - `ControlService.cs`: 687〜688 行（`Enabled` の判定）、921 行付近（拡張ボタンの一覧）。
  - `ControllerRegDeviceOptsViewModel.cs`: `EnableVader4Pro`、`Vader4ProDeviceOpts`、`CurrentVader4ProOptions`、`Vader4ProControllerOptionsWrapper`、`switch` の 2 つの分岐。
  - `ControllerRegisterOptionsWindow.xaml`: チェックボックス（25 行）。Vader4Pro 用の設定パネルがあれば、それも削除（調査: `Vader4ProControllerOptionsWrapper` を参照する XAML は見つからなかった）。
  - `DS4WinWPF.csproj`、`ScpUtil.cs`: Vader4Pro の参照（`Vader`＋大小文字無視の grep で該当した 2 件。実装時に内容を確認し、デバイスサポートに関するものだけを削除する）。
- **対象外（変更しない）**:
  - `About.xaml.cs`、`AboutViewModel.cs`、`UpdaterWindow.xaml.cs`、`RepoConfig.cs`、各ドキュメント、`.github/*`: リポジトリ・製品名の表記。
  - `DS4Controls.BLP`／`BRP`／`FnL`／`FnR`／`SideL`／`SideR`／`Capture` の列挙値: 他のデバイス（JoyCon など）や、保存済みのプロファイル（`Profiles` の XML）で使われている可能性があるため、削除しない。
- **テスト**: `DS4WindowsTests`／`StandaloneTests` に `Vader` を参照するテストは見つからなかった。

## 4. 互換性・データへの影響
- **`Profiles.xml`（アプリ設定）**: Vader4Pro の有効／無効は保存されていなかった（不具合の原因）ため、影響なし。
- **`ControllerConfigs.xml`（コントローラー固有設定）**: Vader 4 Pro を接続して保存した場合に、MAC アドレスごとの `Vader4ProSupportSettings` が入っている可能性がある。削除後は、そのデバイスが専用デバイスとして検出されないため読み込まれない。設定ファイルの書式は変えない（残っていても無害）。
- **保存済みのプロファイル**: `BLP`／`BRP` などの割り当てが含まれていても、列挙値を残すため、読み込みの動作は変わらない。

## 5. 実装手順（案）
1. 3 ファイルの削除と、上記の参照の削除（1 コミット。DI 化の変更と混ぜない）。
2. ビルド、テストビルド、テスト実行（`ControllerRegDeviceOptsViewModel` のガードテスト［`Step10GroupTwoGlobalReferenceGuardTests`］を含む）。
3. 実機確認:
   - 「デバイスオプション」画面から「Key: ControllerSupportVader4Pro」の項目が無くなること。
   - DS4 コントローラー（接続済みのもの）が、従来どおり検出・動作すること。
   - （Vader 4 Pro を所持している場合）接続しても専用デバイスとして検出されないこと。
4. `Phase6-Status.md`、`Phase6-Step10-Plan.md`、`Phase6-Step11-Plan.md`（実機確認項目に Vader 4 Pro がある場合は取り下げ）を更新する。モデル図に Vader4Pro の記載があれば更新する。

## 6. リスク
- **デバイス種別の列挙値（`InputDeviceType`）の削除**: 数値でシリアライズしている箇所があると、後続の値がずれる。実装時に、列挙値の明示的な数値指定と、`InputDeviceType` を永続化している箇所（`ControllerConfigs.xml` など）を確認する。
- **`FLYDIGI_VID` の共用**: 他のデバイス定義が同じ VID を使っている場合、定数は残す。
- **ビルドの連鎖**: 3 ファイルの削除で、`using`、`partial`、XAML のデータテンプレートに残る参照があると、ビルドが失敗する。参照を grep で全件確認してから削除する。

---

## 7. 実施結果（2026-10-14）
- 削除したファイル（3）: `Vader4ProDevice.cs`、`Vader4ProReport.cs`、`Vader4ProControllerOptsDTO.cs`（`git rm` でインデックスに反映済み。コミットはユーザーが行う）。
- 参照の削除: `InputDeviceFactory.cs`（列挙値 `Vader4Pro`［最後の値のため、他の値は変わらない］と生成）、`DS4Devices.cs`（`FLYDIGI_VID`［他に使用なし］、`VADER4PRO_PID`、登録、`&mi_02` の絞り込み、シリアル取得の分岐）、`HidDevice.cs`（`GetVader4ProMacAddress`）、`ControlServiceDeviceOptions.cs`（`Vader4ProDeviceOptions`、`Vader4ProControllerOptions`、`Vader4ProDeviceOpts`）、`ControlService.cs`（2 分岐）、`ControllerRegDeviceOptsViewModel.cs`（プロパティ、2 つの `switch` の分岐、`Vader4ProControllerOptionsWrapper`）、`ControllerRegisterOptionsWindow.xaml`（チェックボックス）。
- 補足: `ControllerRegDeviceOptsViewModel.FindTabOptionsIndex` は Vader4Pro に対して `5` を返していたが、画面のタブは 0〜4 の 5 つしか無く、範囲外だった（削除により解消）。
- 残した項目: `InputDeviceFactory.CreateDevice` の `macAddress` 引数（Vader4Pro のために追加された。呼び出し側が渡しているため温存）、`VidPidFeatureSet` の列挙値、`DS4Controls` の列挙値。
- 実機確認の手順: 本書 §5 の 3。
