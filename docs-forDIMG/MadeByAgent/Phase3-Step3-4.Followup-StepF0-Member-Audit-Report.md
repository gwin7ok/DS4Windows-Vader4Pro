# Phase3-Followup Step F-0: DS4Devices public static メンバー突合レポート

作成日: 2026-08-30
方法: リポジトリ実体（For-DI-migration-work ブランチ、ソミット時点最新）を取得し、`grep -n "public static"` で
`DS4Windows/DS4Library/DS4Devices.cs` を再抽出。`ControlService.cs` / `App.xaml.cs` の呼び出し箇所を
`grep -n "DS4Devices\."` でリポジトリ全体から再抽出した。

## 1. DS4Devices.cs の public static メンバー（実測12件）

| # | メンバー | 種別 | IDs4DeviceRegistry対応 |
|---|---|---|---|
| 1 | RequestElevation | event | RequestElevation |
| 2 | PrepareDS4Init | field(delegate) | PrepareDS4Init |
| 3 | PostDS4Init | field(delegate) | PostDS4Init |
| 4 | PreparePendingDevice | field(delegate) | PreparePendingDevice |
| 5 | isExclusiveMode | field(bool) | IsExclusiveMode |
| 6 | findControllers() | method | FindControllers() |
| 7 | getDS4Controllers() | method | GetDS4Controllers() |
| 8 | stopControllers() | method | StopControllers() |
| 9 | On_Removal(object,EventArgs) | method | OnRemoval(object,EventArgs) |
| 10 | RemoveDevice(DS4Device) | method | RemoveDevice(DS4Device) |
| 11 | UpdateSerial(object,EventArgs) | method | UpdateSerial(object,EventArgs) |
| 12 | reEnableDevice(string) | method | ReEnableDevice(string) |

**結論: 欠員なし。12件すべてが1:1で対応している。** Phase3-Followup-Plan §1.3の事前調査結果と完全一致。
VID/PID定数（`SONY_VID`等）は`internal const`でありレジストリ対象外（想定通り）。

## 2. 呼び出し元の実測（ControlService.cs / App.xaml.cs）

### ControlService.cs（DS4Devices.直接呼び出し 14行）

| 行番号 | 内容 | 分類 |
|---|---|---|
| 236 | `DS4Devices.RequestElevation += ...` | インベント購読 |
| 237 | `DS4Devices.PrepareDS4Init = ...` | デリグート代入 |
| 238 | `DS4Devices.PostDS4Init = ...` | デリグート代入 |
| 239 | `DS4Devices.PreparePendingDevice = ...` | デリグート代入 |
| 930 | `DS4Devices.getDS4Controllers()`（ChangeMotionEventStatus内） | 読み取る |
| 969 | `DS4Devices.getDS4Controllers()`（UseUDPPort内） | 読み取る |
| 1014 | `DS4Devices.isExclusiveMode`（WarnExclusiveModeFailure内） | 読み取る |
| 1604 | `DS4Devices.isExclusiveMode = ...` | 書き込み |
| 1611 | `DS4Devices.isExclusiveMode`（ログ出力） | 読み取る |
| 1640 | `DS4Devices.findControllers()`（Start内） | 実行 |
| 1643 | `DS4Devices.getDS4Controllers()`（Start内） | 読み取る |
| 1860 | `DS4Devices.RemoveDevice(tempDevice)` | 完行 |
| 1887 | `DS4Devices.stopControllers()` | 実行 |
| 1942 | `DS4Devices.findControllers()`（HotPlug内） | 実行 |
| 1945 | `DS4Devices.getDS4Controllers()`（HotPlug内） | 読み取る |
| 2077 | `device.Removal += DS4Devices.On_Removal` | インベントハンドラ登録 |
| 2079 | `device.SyncChange += DS4Devices.UpdateSerial` | インベントハンドラ登録 |

（表は18箇所を含むが、236-239の4行を1グループとして数えるとF-2着手時の置換単位は実質14箇所。Phase3-Plan.md §0.1相当の`Program.rootHub`参照2箇所とは無関係。）

### App.xaml.cs（DS4Devices.直接呼び出し 1行）

| 行番号 | 内容 | 扱い |
|---|---|---|
| 649 | `DS4Windows.DS4Devices.reEnableDevice(parser.DeviceInstanceId)` | **F-2では変更しない**（`-re-enabledevice`子プロセス枝。Followup-Plan §2.2/§2.4の方針通り） |

### 対象外（ソメントアウト・定数、変更しない）

- `ControllerListViewModel.cs:148`、`TrayIconViewModel.cs:308,332,349` — ソメントアウト済み `DS4Devices.getDS4Controllers()`
- `DualSenseDevice.cs:355` — `DS4Devices.SONY_VID`（`internal const`、レジストリ対象外）

## 3. 結論

欠員なし。F-0bは不要。F-1（アクセサ配線）に進む。