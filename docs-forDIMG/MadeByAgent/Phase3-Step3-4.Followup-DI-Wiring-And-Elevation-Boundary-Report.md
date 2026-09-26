# Phase3-Followup-DI-Wiring-And-Elevation-Boundary-Report.md

作成日: 2026-08-30
対応計画: `docs-forDIMG/MadeByAgent/Phase3-Followup-DI-Wiring-And-Elevation-Boundary-Plan.md`

## 実施結果

### Step F-0（メンバー突合）
`DS4Devices.cs` の public static メンバー12件を実測し、`IDs4DeviceRegistry`/`Ds4DeviceRegistryAdapter` との
1:1対応を確認。欠員なし。詳細は `Phase3-Followup-StepF0-Member-Audit-Report.md`。

### Step F-1（IDeviceStateAccessor 配線）
- `ServiceRegistration.cs`: `services.AddSingleton<IDeviceStateAccessor>(_ => Program.rootHub);` を追加。
  新規シムクラスなし（計画の禁止事項どおり）。
- `Mapping.cs`（マクロ rumble、旧6504行目付近）: 解決口を `ServiceProviderHolder.Provider`（未登録のため
  常に失敗していた）から `DS4WinWPF.AppHost.GetService<IDeviceStateAccessor>()`（`IVirtualKBM` と同じ、
  生きているAppHost経路）に統一。`accessor == null` 時の `Program.rootHub` フォールバックは変更していない。

### Step F-2（IDs4DeviceRegistry コンストラクタ注入）
- `ControlService.cs`: フィールド `_deviceRegistry` を追加し、コンストラクタで `IDs4DeviceRegistry` を受け取る形に
  変更。実行時の直接 `DS4Devices.` 呼び出し14箇所（イベント購読/デリゲート代入4、読み取り6、実行4）をすべて
  `_deviceRegistry.` 経由に置換。メソッド単位での静的 `DS4Devices` へのフォールバック分岐は置いていない
  （計画の禁止事項どおり、経路は常にアダプタ1本）。
- `App.xaml.cs` の `CreateControlService`（生成1箇所）: `AppHost.GetService<IDs4DeviceRegistry>()` で取得し、
  `?? new Ds4DeviceRegistryAdapter()` を起動順の安全網として温存（同一アダプタクラスの再生成であり第二実装
  ではない）。`parser.ReenableDevice` 側の子プロセス枝（649行目、静的 `DS4Devices.reEnableDevice` 直接呼び出し）
  は計画どおり無変更。

### Step F-3（文書更新およびビルド・テスト検証）
- `Phase3-Status.md` を更新（3-Fを完了に更新、進捗カウントを5/7に更新、残課題の境界確定を反映）。
- XAML 2パスコンパイルの整合性確保（`ProfileNotificationWindow.xaml` と `StickCalibrationWindow.xaml` に `xmlns:local` を追加）。
- 全ユニットテストの実行確認:
  - `DS4Windows.Actions.Tests`: 全24件 成功（100%）
  - `StandaloneTests`: 全13件 成功（100%）
  - 合計37件のテストが全件パス。

## 完了判定基準チェック（計画書§6）

- [x] public static実行メンバーとIDs4DeviceRegistryの突合結果を文書化
- [x] IDeviceStateAccessorの登録はServiceRegistrationの遅延ファクトリのみ、新規シムクラスなし
- [x] Mapping.csマクロrumbleがAppHost解決を優先し、フォールバックが残っている
- [x] ControlServiceがIDs4DeviceRegistryをコンストラクタで受け取り、内部の実行時DS4Devices.がフィールド経由
- [x] ControlService内部に静的DS4Devicesへのメソッド単位フォールバックなし
- [x] CreateControlServiceがAppHostからレジストリを渡している（null時のみ同じアダプタをnew）
- [x] App.xaml.csの-re-enabledevice枝は静的reEnableDeviceのまま
- [x] IElevatedProcessLauncherとReEnableDeviceの境界（Followup-Plan §2.4）がStatusに残っている
- [x] ビルドが通っている（DS4WinWPF, Actions.Tests, StandaloneTests 全件コンパイル成功・テスト37件通過）

## 次のアクション

Step 3-5（`IElevatedProcessLauncher`、権限昇格の抽象化）に進む。
Step 3-5 では `ControlService.DS4Devices_RequestElevation` の Process.Start 部分のみを対象とし、`IDs4DeviceRegistry.ReEnableDevice` には触れないこと。