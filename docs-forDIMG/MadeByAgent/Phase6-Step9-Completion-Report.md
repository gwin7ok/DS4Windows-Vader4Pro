# Phase6-Step9 完了報告書: 主要4大 ViewModel の Pure DI 化

完了日: 2026-10-14  
対象ブランチ: `For-DI-migration-work`  
計画書: `docs-forDIMG/MadeByAgent/Phase6-Step9-Plan.md`（§0A が現行の正）  
着手前の HEAD: `f13d5c7c`

---

## 1. 結果の要約
- 主要 4 大 ViewModel の `Global.` 参照 114 件（`SettingsViewModel` 89、`MainWindowsViewModel` 9、`TrayIconViewModel` 6、`ProfileSettingsViewModel` 10）をすべて解消した。残るのは定数のみ（`TrayIconViewModel` の `RESOURCES_PREFIX` 12、`ProfileSettingsViewModel` の `ASSEMBLY_RESOURCE_PREFIX` 1）。
- 4 つの ViewModel（と `PresetMenuHelper`、`ViewModelFactory`）のコンストラクタの依存を必須引数にした。`?? AppHost.GetService`／`?? Global.*Instance`／`?? Program.rootHub` のフォールバックを削除した。
- 契約を追加した（いずれも `Global` への薄い委譲）: `IAppSettingsService` 4、`IEnvironmentService` 4、`IPathService.ProgramDirectory`、`IAppearanceSettingsService.BatteryChanged`、`IProfileSettingsService.RefreshActionAlias`。
- 呼出元 0 件の更新関連メンバー（`MainWindowsViewModel` の 6 件、`SettingsViewModel` のハンドラ 1 件）を、経緯を調べたうえで削除した（決定6）。
- 実機確認で、Step9 の回帰ではない既存不具合を 2 件発見し、修正した（K9-1: OSC 送信の競合によるアプリ終了、K9-2: 偽装 exe 名の設定ファイルのパス誤り）。

## 2. 計画書からの主な変更点
1. **件数**: 旧 79 件 → 現行 114 件。旧計画は同じ行の複数参照を 1 件と数え、`SettingsViewModel` の台帳は現行コードに存在しないメソッド・フィールド（`_environmentService` など）を前提にしていた。台帳は現行コードから作り直した（§0A.2）。
2. **`ProfileSettingsViewModel` は 11 件ではなく 10 件**（Step8 申し送りの数え違い）。
3. **`exedirpath` の置換先**: 旧計画の `IPathService.ExecutablePath` 経由の式でも、`ExecutableDirectory` でもなく、新設の `IPathService.ProgramDirectory`（決定3）。`ExecutableDirectory`（`AppContext.BaseDirectory`）は値が異なるため使えない。
4. **`BatteryChanged` の置き場所**: 旧計画の `IDeviceStateService`（B12）ではなく、発行側と同じ `IAppearanceSettingsService`（決定4）。
5. **`RefreshActionAlias` の置き場所**: 旧計画の `ISpecialActionRepository`（B6）ではなく、マッピングの実体（`OutputKBMMapping`）と同じ `IProfileSettingsService`（決定5）。
6. **マイクロステップ**: 計画の 9-1〜9-5 に、実機確認で見つかった修正として 9-3c（OSC）・9-3d（偽装 exe 名）を追加した。`SettingsViewModel` のコンストラクタの必須引数化は 9-3b から 9-3a へ前倒しした。
7. **`ControlService` のみ null 許容**: `ProfileSettingsViewModel` の `ControlService` は、ジャイロ系 setter の引数としてだけ使われ、テストでは従来も null だったため、必須引数だが null を許容した。決定1「すべて必須」からの小さな差。
8. **`TrayIconViewModel` の単体テスト**: `ContextMenu` を生成するため STA スレッドが必要で、動作テストは書かず、契約テスト（`BatteryChanged`）とソース走査、実機確認で代えた。

## 3. ユーザー決定（詳細は計画書 §0A.3。すべて推奨案）
- 決定1＝S9-A（`Global.` 全件＋コンストラクタのフォールバック解消。`App.logHolder` などは Step10 へ）
- 決定2＝C1（`ViewModelFactory` が `Func<ControlService>` で遅延解決）
- 決定3＝P2（`IPathService.ProgramDirectory` を新設）
- 決定4＝B1（`IAppearanceSettingsService.BatteryChanged`）
- 決定5＝A1（`IProfileSettingsService.RefreshActionAlias`）
- 決定6＝D1（呼出元 0 件のコードを削除）
- 決定7＝T1（後始末のない既存テストを `finally` で修正）
- 決定8＝K1（K7b-1 は Step11 まで先送り。異常終了時のみ基準コミット `f13d5c7c` で判別）
- 追加決定（2026-10-14）: OSC 送信の競合を Step9-3c として修正、偽装 exe 名の不具合を Step9-3d として修正（いずれも推奨案）。

## 4. 変更した主なファイル
- ViewModel: `DS4Forms/ViewModels/SettingsViewModel.cs`、`MainWindowsViewModel.cs`、`TrayIconViewModel.cs`、`ProfileSettingsViewModel.cs`（`PresetMenuHelper` を含む）
- 生成側: `DS4Forms/MainWindow.xaml.cs`（3 行）、`DS4Forms/ProfileEditor.xaml.cs`（レガシーフォールバックの削除）、`DS4Control/Services/ViewModelFactory.cs`、`DI/ServiceRegistration.cs`（`Func<ControlService>`）
- 契約と実装: `DI/IAppSettingsService.cs`／`DS4Control/Services/AppSettingsService.cs`、`DI/IEnvironmentService.cs`／`EnvironmentService.cs`、`DI/IPathService.cs`／`PathService.cs`、`DI/IAppearanceSettingsService.cs`／`AppearanceSettingsService.cs`、`DI/IProfileSettingsService.cs`／`ProfileSettingsService.cs`
- 既存不具合の修正: `DS4Control/ControlService.cs`（`SendOsc`、26 箇所）、`SettingsViewModel.cs`（`CreateFakeExeFiles`／`DeleteFakeExeFiles`）
- テスト（新規）: `Phase6Step9ContractExtensionTests`、`MainWindowsAndTrayViewModelGlobalReferenceGuardTests`、`SettingsViewModelGlobalReferenceGuardTests`、`ProfileSettingsViewModelGlobalReferenceGuardTests`、`ProfileSettingsViewModelTestFactory`、`ControlServiceOscSenderRaceTests`、`SettingsViewModelFakeExeTests`
- テスト（更新）: `ProfileSettingsServiceSubSettingsTests`（後始末）、`ProfileSettingsViewModelTests`、`ProfileSettingsViewModelTargetDeviceTests`、`ProfileEditorTargetDeviceGuardTests`
- 文書: `Phase6-Step9-Plan.md`、`Phase6-Status.md`、`Phase6-Plan.md`、`Phase6-Step10-Plan.md`（申し送り）、`Phase6-Step11-Plan.md`（§3.3 先送り台帳）、モデル図 `03-Class-Interface-Diagram.md`・`04-Service-Lifecycle-Spec.md`

## 5. 検証結果
- 各マイクロステップ（9-1〜9-4）で、ユーザーの `dotnet build`、テストビルド、`dotnet test` が成功（9-3c で、テストプロジェクトが `SharpOSC` を参照していないことによるテストのコンパイルエラーが 1 回あり、リフレクションに変更して解消）。
- 実機確認（すべて問題なし。K9-3 を除く）:
  - 起動・メイン画面（`[DI] MainWindowsViewModel: created …`、`[DI] TrayIconViewModel: subscribed …` を Trace ログで確認）
  - 設定タブの各項目の変更・再起動後の維持
  - OSC 送信のオン／オフ（修正後）
  - 偽装 exe 名（設定・変更・空にする。修正後）
  - Steam フォルダ、ログ設定
  - トレイ（バッテリー表示、ツールチップ、メニュー）
  - プロファイル編集（Edit、保存、`[DI] ViewModelFactory: Created ProfileSettingsViewModel …` のログ）、Bezier カーブエディタ（プログラムフォルダの `BezierCurveEditor\index.html` が開く）、プリセットによる割り当て内容

## 6. 持ち越し・先送り
- **K9-3（先送り）**: コントローラー図の右クリックのプリセットメニューで、サブメニューへマウスを移すと消える（キーボードでは選べ、設定内容は正常）。Step9 では変更していない部分。ベースラインでの再現確認は未実施。`Phase6-Status.md` §6.5。
- **K7b-1（先送りのまま）**: Step9 の実機確認中に再現しなかった。
- **Step11 §3.3 に登録**: ViGEmBus 未導入環境での `-driverinstall` の昇格起動、プリセット（WASD／Arrow Keys）の実際のキー出力、OSC 受信側アプリを接続した送受信、K9-3 の単一モニター・同一 DPI での確認。
- **Step10 の台帳へ**: `App.logHolder`（`SettingsViewModel` 5 箇所）、`StartupMethods.*`（静的クラス）、`Util.GetHidHideClientPath()`、`TrayIconViewModel` の `App.Current.Dispatcher`、`MainWindow` のコンストラクタ内の `AppHost.GetService`（10 件以上）と `Program.rootHub`／`Global.*Instance` のフォールバック。

## 7. 後続への影響
- **Step10**: `Phase6-Step10-Plan.md` 冒頭の申し送りを参照。置換先として `IPathService.ProgramDirectory`（`SaveWhere` 5、`Util.cs` 2、`StartupMethods` 4）、`IProfileSettingsService.RefreshActionAlias`（`BindingWindowViewModel` 2）、`IAppSettingsService` の Steam フォルダ（`AutoProfiles.xaml.cs`）を使える。`ViewModelFactory` の他のメソッドの ViewModel を必須引数にするときは、ファクトリの必須引数も追加する。
- **Step10b**: `ProfileSettingsViewModel.cs` の複数型（`PresetMenuHelper`、`TriggerModeChoice` など）は、本 Step で型を追加していない。分割は Step10b。
- **Step12**: `Global.exedirpath`、`Global.RefreshActionAlias`、`Global.BatteryChanged` などのシムの呼出元が、Step9 の範囲で減った。`IPathService.ExecutableDirectory` と `ProgramDirectory` の統合は Step12 で判断する（`IPathService` の TODO）。
- **モデル図**: 03・04 を更新済み（4 つの ViewModel の依存、`ViewModelFactory` の依存、追加した契約）。01・02 は構造変更なし。
