# Step0 ViewModel直接生成インベントリ

## 調査結果

対象は`DS4Windows/DS4Forms`配下の`.xaml.cs`である。`new XxxViewModel(...)`を検索し、コメントアウトされた旧コンストラクタは実行件数に含めず、参考として注記した。

| 指標 | 実測値 | 計画書の暫定値 | 差分・注記 |
|---|---:|---:|---|
| 生成元ファイル数 | 16 | 少なくとも16 | 一致 |
| 実行される直接生成件数 | 29 | — | `new`文ベース |
| コメントアウト生成件数 | 1 | — | `ControllerListViewModel`の旧コンストラクタ |
| A: 引数なし | 11 | 11 | 一致 |
| B: 共有依存 | 8 | 5 | `ControlService`、`ProfileList`等の共有オブジェクトを受け取るもの |
| C: 実行時引数 | 10 | 17 | device、deviceNum、settings、version等を受け取るもの |

計画書のA/B/C推定値（11/5/17）は生成「型」の延べ件数を想定した値と考えられるが、現状コードの実測では29件（コメントアウトを含めると30件）である。B/Cの境界は、共有サービス・共有状態だけを受け取るものをB、画面表示時に決まる値や対象デバイスを受け取るものをCとした。

## 一覧

| 分類 | ViewModel | 生成元 | 行 | コンストラクタ引数 | 判断 |
|---|---|---|---:|---|---|
| C | `AxialStickControlViewModel` | `DS4Forms/AxialStickUserControl.xaml.cs` | 44 | `stickDeadInfo` | 画面対象のスティック設定値 |
| B | `AutoProfilesViewModel` | `DS4Forms/AutoProfiles.xaml.cs` | 94 | `autoProfileHolder, profileList` | 共有の自動プロファイル／プロファイル一覧 |
| C | `BindingWindowViewModel` | `DS4Forms/BindingWindow.xaml.cs` | 67 | `deviceNum, settings` | デバイス番号と画面対象設定 |
| A | `ChangelogViewModel` | `DS4Forms/ChangelogWindow.xaml.cs` | 47 | なし | 引数なし |
| B | `ControllerRegDeviceOptsViewModel` | `DS4Forms/ControllerRegisterOptionsWindow.xaml.cs` | 50 | `deviceOptions, service` | 共有ControlService系依存 |
| B | `FirstLauchUtilViewModel` | `DS4Forms/FirstLaunchUtilWindow.xaml.cs` | 48 | `serviceDeviceOpts` | 共有ControlService系依存 |
| A | `LanguagePackViewModel` | `DS4Forms/LanguagePackControl.xaml.cs` | 48 | なし | 引数なし |
| A | `MainWindowsViewModel` | `DS4Forms/MainWindow.xaml.cs` | 103 | なし | 引数なし |
| A | `SettingsViewModel` | `DS4Forms/MainWindow.xaml.cs` | 107 | なし | 引数なし |
| B | `LogViewModel` | `DS4Forms/MainWindow.xaml.cs` | 109 | `App.rootHub` | アプリ共有ControlService |
| B | `ControllerListViewModel` | `DS4Forms/MainWindow.xaml.cs` | 121 | `App.rootHub, profileListHolder` | アプリ共有サービス・一覧 |
| B | `TrayIconViewModel` | `DS4Forms/MainWindow.xaml.cs` | 132 | `App.rootHub, profileListHolder` | アプリ共有サービス・一覧 |
| B | `CurrentOutDeviceViewModel` | `DS4Forms/OutputSlotManagerControl.xaml.cs` | 56 | `controlService, outputMan` | 共有出力管理依存 |
| A | `PresetOptionViewModel` | `DS4Forms/PresetOptionWindow.xaml.cs` | 35 | なし | 引数なし |
| C | `ProfileSettingsViewModel` | `DS4Forms/ProfileEditor.xaml.cs` | 281 | `device` | 対象デバイス |
| C | `MappingListViewModel` | `DS4Forms/ProfileEditor.xaml.cs` | 285 | `deviceNum, profileSettingsVM.ContType` | 対象デバイス・実行時設定 |
| C | `SpecialActionsListViewModel` | `DS4Forms/ProfileEditor.xaml.cs` | 286 | `device` | 対象デバイス |
| C | `RecordBoxViewModel` | `DS4Forms/RecordBox.xaml.cs` | 61 | `deviceNum, controlSettings, shift, repeatable` | 実行時の記録対象・操作条件 |
| A | `RenameProfileViewModel` | `DS4Forms/RenameProfileWindow.xaml.cs` | 39 | なし | 引数なし |
| C | `SpecialActEditorViewModel` | `DS4Forms/SpecialActionEditor.xaml.cs` | 89 | `deviceNum, specialAction` | 対象デバイス・編集対象 |
| A | `MacroViewModel` | `DS4Forms/SpecialActionEditor.xaml.cs` | 90 | なし | 引数なし |
| A | `LaunchProgramViewModel` | `DS4Forms/SpecialActionEditor.xaml.cs` | 91 | なし | 引数なし |
| B | `LoadProfileViewModel` | `DS4Forms/SpecialActionEditor.xaml.cs` | 92 | `profileList` | 共有プロファイル一覧 |
| A | `PressKeyViewModel` | `DS4Forms/SpecialActionEditor.xaml.cs` | 93 | なし | 引数なし |
| C | `SpecialActionViewModel` | `DS4Forms/SpecialActionEditor.xaml.cs` | 95 | `5` | 固定action indexだが用途固有の実行時値 |
| A | `CheckBatteryViewModel` | `DS4Forms/SpecialActionEditor.xaml.cs` | 96 | なし | 引数なし |
| A | `MultiActButtonViewModel` | `DS4Forms/SpecialActionEditor.xaml.cs` | 97 | なし | 引数なし |
| C | `SpecialActionViewModel` | `DS4Forms/SpecialActionEditor.xaml.cs` | 98 | `8` | 固定action indexだが用途固有の実行時値 |
| C | `SpecialActionViewModel` | `DS4Forms/SpecialActionEditor.xaml.cs` | 99 | `9` | 固定action indexだが用途固有の実行時値 |
| C | `TouchButtonUserControlViewModel` | `DS4Forms/TouchButtonUserControl.xaml.cs` | 48 | `deviceIndex` | 対象デバイス |
| C | `UpdaterWindowViewModel` | `DS4Forms/UpdaterWindow.xaml.cs` | 51 | `newversion` | 実行時バージョン |

## コメントアウトされた生成

`DS4Forms/OutputSlotManagerControl.xaml.cs:63`の`PermanentOutDevViewModel`生成はコメントアウトされており、`DS4Forms/ViewModels/ControllerListViewModel.cs:62`には旧コンストラクタのコメントアウトがある。いずれも現行の実行経路には含めない。

## 移行時の注意

- Aは`ServiceRegistration`への登録候補だが、静的イベントや`Global`参照の有無を先に確認する。
- Bは共有依存をコンストラクタ注入する。`App.rootHub`を新しい`ControlService`で置き換えない。
- CはSingleton登録せず、Factoryの`Create`へdevice、settings、action、version等を渡す。
- `SpecialActionViewModel(5/8/9)`の固定値は、移行時に意味を失わないよう用途名または明示的なpurpose値として管理する。
