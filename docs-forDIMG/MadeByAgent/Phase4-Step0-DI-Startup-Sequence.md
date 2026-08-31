# Step0 DI起動・解決順序

## 結論

現状は`ServiceProviderHolder`用の簡易コンテナと、`AppHost`用の正式コンテナが同一起動処理内に存在する。Phase4-Step6で整理すべき二重Composition Rootである。`ControlService`はDIコンテナから生成されず、`App.xaml.cs`の`CreateControlService`で手動生成され、`App.rootHub`と`Program.rootHub`へ保持される。

## 起動時系列

| 順序 | 場所 | 処理 | 解決先・状態 |
|---:|---|---|---|
| 1 | `App.Application_Startup` | 設定場所の検出、ログローテーション、起動引数解析 | `Global`の静的状態を使用 |
| 2 | `App.Application_Startup` | `new ServiceCollection()`し、Actions系サービスを登録 | 簡易コンテナの構築開始 |
| 3 | `App.Application_Startup` | `BuildServiceProvider()`、`ServiceProviderHolder.SetProvider(sp)` | `ServiceProviderHolder.Provider`へ保存 |
| 4 | `App.Application_Startup` | 簡易Providerから`IManagedActionManager`を解決し事前確保 | 旧／Actions専用経路 |
| 5 | `App.Application_Startup` | `AppHost.CreateHost(...)` | `ServiceRegistration.AddAppServices`を通る正式Providerを構築 |
| 6 | `App.Application_Startup` | `CreateControlService(parser)` | `AppHost.GetService<IDs4DeviceRegistry>()`を利用し、`ControlService`を手動生成 |
| 7 | `App.CreateControlService` | `App.rootHub`および`Program.rootHub`へ代入 | `IDeviceStateAccessor`のファクトリ委譲が参照する実体を確定 |
| 8 | `App.Application_Startup` | `Global.Load()`、`LoggerHolder(rootHub)`、主要画面生成 | UI／実行層が静的`Global`と`rootHub`を継続使用 |
| 9 | `App.OnExit` | `ServiceProviderHolder.Provider`から`IControllerRegistry`を取得 | 終了処理では簡易Providerを使用 |

## コンテナ別登録・利用範囲

| コンテナ | 登録・利用 | 現状の問題 |
|---|---|---|
| `ServiceProviderHolder` | `IActionFactory`、`IManagedActionManager`、`IKeyActionCreator`、`IKeyButtonActionControllerFactory`、`IControllerRegistry` | `App.xaml.cs`内で手動構築。AppHostとは別Provider・別Singletonになる可能性がある |
| `AppHost` | `IProfileSettingsService`（Placeholder）、`IVirtualKBM`、`IDs4DeviceRegistry`、`IElevatedProcessLauncher`、`IDeviceStateAccessor`、`IProcessInspector` | 正式登録先だが、全アプリサービスの唯一の解決先にはなっていない |
| 手動生成 | `ControlService` | `AppHost`管理外。`App.rootHub`／`Program.rootHub`に依存する既存構造を維持中 |

## Phase4で解消すべき境界

1. `ServiceRegistration.AddAppServices`を正式な登録先として維持する。
2. Actions系登録を既存利用箇所の確認後にAppHost側へ集約する。
3. `ControlService`をHost構築時に無理にSingleton化せず、現在の遅延確定（`CreateControlService`後に`rootHub`が設定される）を維持する。
4. `IDeviceStateAccessor`の`Program.rootHub`ファクトリ委譲は、null時の挙動を含めてテストで固定する。
5. 旧Providerの利用箇所を全数確認し、正式Providerへの移行後にのみ旧経路を削除する。

## 根拠箇所

- `DS4Windows/App.xaml.cs:296-322`: 簡易`ServiceCollection`の構築と`AppHost.CreateHost`の連続呼出し。
- `DS4Windows/App.xaml.cs:738-768`: `ControlService`の手動生成と`rootHub`への代入。
- `DS4Windows/App.xaml.cs:1044-1047`: 終了処理で`ServiceProviderHolder.Provider`を参照。
- `DS4Windows/DI/ServiceRegistration.cs`: `AppHost`側のサービス登録。
- `DS4Windows/DI/ServiceProviderHolder.cs`: 簡易Providerを静的に保持する実装。
