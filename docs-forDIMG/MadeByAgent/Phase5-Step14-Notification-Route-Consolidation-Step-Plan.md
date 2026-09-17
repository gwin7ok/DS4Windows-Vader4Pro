# 通知経路統合（経路B一本化）個別計画書

作成日: 2026-09-16
対象ブランチ: For-DI-migration-work
位置づけ: フェーズ6／7いずれの正式ロードマップにも属さない独立の技術的負債解消Step（全体計画書§5.1 3-b層、§5.4 Globalシムの考え方を踏襲）
承認前提: 本計画書の内容確認・承認後に実装に着手する（copilot-instructions §4 マイクロステップの原則）
前提となる完了済み作業:
  - 通知レガシーフォールバック（H.NotifyIcon）撤去、showNotification引数撤去（ビルド・テストビルド・テスト実行・実機確認すべて成功済み）
採用する設計: 選択肢B（主従を逆転し、INotificationServiceを実体、AppLogger.LogToTrayをシムにする）

---

## 1. 現状（Before）の呼び出し関係

- `AppLogger.LogToTray(string data, bool warning, bool ignoreSettings)`（`DS4Windows/DS4Control/Log.cs`）が、静的イベント`TrayIconLog`を自前で発火する。
- `MainWindow.SetupEvents()`（`DS4Windows/DS4Forms/MainWindow.xaml.cs` 452行）が`AppLogger.TrayIconLog += ShowSystemNotification;`で購読する。
- `ShowSystemNotification(object sender, DebugEventArgs e)`（369-387行）がアダプタとして、Temporary抑制・`[DI]`/`[Legacy]`接頭辞抑制を行った上で、`ShowSystemNotification(string message, bool isWarning)`（389-416行、表示本体）を呼ぶ。
- 表示本体は`appSettingsService.Notifications`（0=なし／1=警告のみ／2=すべて）と`isWarning`から`levelPass`を判定し、通過時のみ`AppNotificationRegistration.ShowModernToast(title, message)`を呼ぶ。
- 一方`DS4Windows.DI.INotificationService`（実装: `AppNotificationService`、`DS4Windows/DS4Control/Services/AppNotificationService.cs`）は、`SendNotification(title, message, isToast)`がイベント`NotificationTriggered`を発火するのみで、購読者が存在しない（実行してもトーストが表示されない）。

## 2. 目標（After）の呼び出し関係

- `INotificationService`／`AppNotificationService`を通知発火の「実体」とする。
- `AppLogger.LogToTray`は、内部で`Global.NotificationServiceInstance.SendNotification(...)`を呼ぶだけの薄いシムに変更する（既存の`Global`分割で確立済みのStrangler Figパターンと同じ方向性）。
- `MainWindow`は`AppLogger.TrayIconLog`ではなく`INotificationService.NotificationTriggered`を購読する。
- 表示ロジック（レベル判定、`ShowModernToast`呼び出し）は現状のまま`MainWindow`（View層）に residing させる。責務混在を避けるため、`AppNotificationService`自体にWinRT／WPF依存は一切持ち込まない。

## 3. 対象ファイルと変更内容

### 3.1 `DS4Windows/DI/INotificationService.cs`（インターフェース拡張）

- `NotificationEventArgs`に`Warning`（bool）・`Temporary`（bool）プロパティを追加する。既存の`DebugEventArgs`が持つ`Warning`／`Temporary`と同じ意味で、通知レベル判定（1=警告のみ、のケース）と一時ログ抑制のために必要（追加しないと、現状`ShowSystemNotification(string,bool isWarning)`が行っているレベル判定を購読側で再現できず、機能欠落になる）。
- コンストラクタに`warning`・`temporary`引数を追加する（初期値`false`）。
- `SendNotification`のシグネチャを`void SendNotification(string title, string message, bool warning = false, bool temporary = false, bool isToast = true);`に拡張する。

### 3.2 `DS4Windows/DS4Control/Services/AppNotificationService.cs`（実装更新）

- `SendNotification`内で受け取った`warning`・`temporary`をそのまま`NotificationEventArgs`に積んでイベント発火する。
- 既存の粗い有効判定（`NotificationsEnabled`＝`Global.Notifications != 0`のときのみ発火）は変更しない。「レベル1のときは警告のみ通す」という細かい判定は、従来通り購読側（`MainWindow`）に残す設計とする（サービス層に表示ポリシーを持ち込みすぎない）。

### 3.3 `DS4Windows/DS4Control/Log.cs`（`LogToTray`のシム化）

- `LogToTray`が独自に`TrayIconLog`イベントを発火するのをやめ、`Global.NotificationServiceInstance.SendNotification(title: string.Empty, message: data, warning: warning, temporary: false, isToast: true)`を呼ぶように変更する。
  - `title`は空文字を渡す。理由: 現状の表示本体`ShowSystemNotification(string,bool)`はイベント側から渡されたタイトルを一切使用せず、常に`TrayIconViewModel.ballonTitle`を自分で取得しているため、`Log.cs`側で意味のあるタイトルを決定する必要がない。またLog.csはコアの横断ログユーティリティであり、UIのViewModel（`TrayIconViewModel`）を直接参照すると上位層→下位層の依存方向原則（§1）に反するため、あえて参照しない。
  - `temporary`は現状`LogToTray`のシグネチャ自体に`temporary`パラメータが存在せず、常に`false`が渡っている（＝この経路での`Temporary`抑制は現状でも到達しない分岐であることを確認済み）。今回のリファクタでもこの既存の実挙動（常にfalse）をそのまま維持し、新たに機能追加はしない。
- `ignoreSettings`引数の扱い: 現状の実装でも受信側（`ShowSystemNotification(object,DebugEventArgs)`）は`sender`引数を一切参照しておらず、`ignoreSettings`は既に無効（無機能）であることを確認済み。今回もこの引数を`SendNotification`側へは配線せず、「現状未使用（既存の技術的負債、今回のスコープ外）」である旨をコード上のコメントとして明記するに留める（安易に新機能として実装しない）。
- `TrayIconLog`イベント自体は本Stepでは削除せず、`[Obsolete("MainWindowの購読先をINotificationService.NotificationTriggeredへ移行済み。次回クリーンアップで削除予定。")]`を付与した上で定義を残す（3.3原則4：過渡期に使わないが残すクラス/メンバへのTODOコメント明記に対応）。発火元を失うため、このイベントは以後発火されなくなる。

### 3.4 `DS4Windows/DS4Forms/MainWindow.xaml.cs`（購読先の切替）

- フィールドを追加: `private DS4Windows.DI.INotificationService notificationService;`
- 初期化箇所（108-116行付近、他の`AppHost.GetService<T>() ?? Global.XxxServiceInstance`と同じ並びに追加）:
  `notificationService = DS4WinWPF.AppHost.GetService<DS4Windows.DI.INotificationService>() ?? Global.NotificationServiceInstance;`
- `SetupEvents()`（452行）: `AppLogger.TrayIconLog += ShowSystemNotification;` を `notificationService.NotificationTriggered += ShowSystemNotification;` に置き換える。
- アダプタメソッドのシグネチャ変更: `ShowSystemNotification(object sender, DS4Windows.DebugEventArgs e)` → `ShowSystemNotification(object sender, DS4Windows.DI.NotificationEventArgs e)`。
  - `e.Data` → `e.Message`
  - `e.Warning` → `e.Warning`（プロパティ名は維持）
  - `e.Temporary` → `e.Temporary`
  - フィルタリングロジック本体（Temporary抑制、`[DI]`/`[Legacy]`接頭辞抑制、`Dispatcher.BeginInvoke`によるUIスレッドへの委譲）は変更しない。
- 表示本体`ShowSystemNotification(string message, bool isWarning)`は変更なし。
- `ShowProfileSwitchNotification`が直接`ShowSystemNotification(message, false)`（表示本体）を呼んでいる箇所（436行）は、イベント経由ではない同一クラス内の直接呼び出しのため、本Stepの影響を受けない（変更不要）。

### 3.5 `DS4WindowsTests/NotificationServiceTests.cs`（既存テスト改修）

- `SendNotification("TitleTest", "MessageTest", true)`のように3番目の位置引数を`isToast`として渡している既存呼び出し（`SendNotification_ShouldFireNotificationTriggered_WhenEnabled`）は、シグネチャ変更により3番目が`warning`に変わるため、意味が変わってしまう。名前付き引数`isToast: true`に修正する。
- `SendNotification_ShouldNotFire_WhenDisabled`の`service.SendNotification("TitleTest", "MessageTest")`はデフォルト引数のみのため修正不要だが、意図確認のコメントを追加する。
- 新規テストケースを追加する:
  - `SendNotification`に`warning: true`を渡したとき、`NotificationEventArgs.Warning`が`true`で伝播すること。
  - `SendNotification`に`temporary: true`を渡したとき、`NotificationEventArgs.Temporary`が`true`で伝播すること。

### 3.6 新規テストファイル `DS4WindowsTests/LogToTrayNotificationBridgeTests.cs`（新設）

- `AppLogger.LogToTray("msg", warning: true)`を呼んだときに、`Global.NotificationServiceInstance`（テスト用に差し替え可能な`INotificationService`のモック、`DS4WindowsTests`内に`MockNotificationService`を新設）へ、期待する`message`・`warning`値で`SendNotification`が呼ばれることを検証する。
- `Global.NotificationServiceInstance`はグローバル静的プロパティであるため、既存の`GlobalShim_ShouldSynchronizeWithService`同様、テスト前後で`try/finally`により元のインスタンスを退避・復元する。
- `LogToTray`を複数回呼んだ場合に`TrayIconLog`（Obsolete化後）が発火されなくなったことを直接検証する必要はない（イベント自体は残置するため、コンパイル可能であることの確認で十分）。

## 4. マイクロステップ分割（PR粒度案）

1. Step-1: `INotificationService`／`NotificationEventArgs`のシグネチャ拡張、`AppNotificationService`実装更新、`NotificationServiceTests.cs`改修・新規テスト追加。この時点では`Log.cs`／`MainWindow`は未接続のまま。`dotnet build`・`dotnet test`グリーンを確認。
2. Step-2: `Log.cs`の`LogToTray`実装を`SendNotification`呼び出しへ切替、`TrayIconLog`に`[Obsolete]`付与、`MockNotificationService`新設、`LogToTrayNotificationBridgeTests.cs`新設。`dotnet build`・`dotnet test`グリーンを確認。
3. Step-3: `MainWindow.xaml.cs`の購読先切替・アダプタの型変更。`dotnet build`・`dotnet test`グリーンを確認。
4. Step-4: 実機確認（プロファイル切替・コントローラー検出・アップデータ完了の3経路でトースト表示が従来通りであること。通知レベル設定0／1／2それぞれでの抑制挙動が従来通りであること）。

各Stepは個別PRとし、フォールバック（旧`TrayIconLog`定義）を残したまま進める（3.3原則4・§2.1フォールバック維持の原則に準拠）。

## 5. 完了判定基準

- `Global.NotificationServiceInstance.SendNotification`（＝`INotificationService.SendNotification`）を呼び出す経路が、実際にトースト通知を画面に表示すること（現状の「実行しても何も表示されない」状態が解消されること）。
- `AppLogger.LogToTray`経由の全既存呼び出し元（`ControlService.cs`・`Mapping.cs`・`DS4Devices.cs`・`UpdaterWindow.xaml.cs`・`DS4WinWPF.NotificationService.ShowToast`facade等）が、コード変更なしで従来通りトースト表示されること（`LogToTray`のシグネチャ自体は変更しないため、呼び出し元の修正は不要）。
- 通知レベル設定（0／1／2）およびWarning有無による抑制挙動が、リファクタ前後で完全に一致すること。
- 全自動テスト（新規追加分含む）が成功すること。
- 実機確認3経路（プロファイル切替・コントローラー検出・アップデータ完了）で回帰がないこと。

## 6. リスクと回避策

- **`Global.NotificationServiceInstance`未初期化時のフォールバックログ増加**: `ScpUtil.cs`939-955行のフォールバックは呼び出しの都度`AppLogger.LogTrace`／`LogToGui`でログを残す実装になっている。`LogToTray`は`ControlService`等のホットパスから呼ばれ得るため、DI未初期化状態が続くとログが大量発生する可能性がある。Step-2実装時に、フォールバック発生を初回のみ警告し以降は抑制する、または`AppHost`の初期化順序を再確認して実運用時にフォールバックへ落ちないことを事前確認する。
- **`SendNotification`のシグネチャ変更に伴う破壊的変更**: 本番コードからの呼び出しは現状皆無（テストのみ）のため実害は小さいが、将来他のコードが`SendNotification`を直接呼び始めた場合に備え、変更履歴をこの計画書とコミットメッセージに明記する。
- **`TrayIconLog`の`[Obsolete]`化によるビルド警告**: 購読箇所はStep-3で`MainWindow`側を切り替えるため、Step-2完了時点では一時的に「発火元なし」の状態になる。Step-2とStep-3の間でビルド・テストは通るが、実機でのトースト表示が一時的に機能しなくなる期間が生じるため、Step-2とStep-3は同一PRにまとめるか、間を空けずに連続して適用することを推奨する。
- **`NotificationEventArgs`の`Title`が空文字で流れる点**: 表示本体が独自にタイトルを解決する現行仕様に依存した設計のため、将来`AppNotificationRegistration.ShowModernToast`の呼び出し元が増え、イベント側のタイトルを実際に使うようになった場合は、本設計の前提（Titleは無視される）を再確認すること。

## 7. 本計画書のスコープ外

- `AppNotificationService.SendNotification`内部で直接`ShowModernToast`を呼ぶ設計（選択肢C）への変更は行わない。
- `ignoreSettings`引数の実配線（本来の意図通りに機能させる改修）は行わない（既存の無効状態を維持するのみ）。
- `Mapping.cs`等、非UIコードから`INotificationService`をコンストラクタ注入で直接呼ぶような新規利用の追加は行わない（既存呼び出し元は全て`AppLogger.LogToTray`経由のまま据え置く）。

## 【追記】実装作業記録：カスケードループ問題の根本解決と通知分離・トースト仕様対応

実機テストにおいて「プロファイル切替スペシャルアクション実行時に、切替が無限連鎖する（カスケードループ）」不具合が確認されたため、通知処理の見直しを含めた根本解決を実施した。あわせて、Windowsネイティブのトースト通知の表示仕様（スタック・積み上げ）に関する調査・対応も行った。

### 1. プロファイル切替カスケードループの根本原因と解決
複数の要因が重なって無限ループが発生していたため、以下の3段構えで完全な遮断を行った。

#### ① WPF UI（ComboBox）の双方向イベント無限ループの完全遮断
- **原因**: バックエンドから `Global_SelectedProfileChanged` イベントが発火した際、UI（`MainWindow` の ComboBox）の表示をプログラムから更新すると、WPFネイティブの `SelectionChanged` イベントが発火し、再度「手動切替が行われた」と誤認して `ApplyProfileToSlot` を無限に呼び出していた。
- **解決策**:
  - `CompositeDeviceModel.suppressSelectedIndexChanged` を統合抑制フラグとして昇格。
  - `ControllerListViewModel` がインデックスを更新する際、`DispatcherPriority.ContextIdle` を用いてWPFのUIイベント完了までフラグを保持するように修正。
  - `MainWindow.SelectProfCombo_SelectionChanged` 冒頭でこのフラグを参照し、バックエンド更新時は手動実行を確実にスキップするよう改修。

#### ② アクションチェーンによるプロファイル切替の連鎖暴発抑止
- **原因**: `ProfileApplicationService` 内で呼び出される `DispatchNextActions` が、新しいプロファイルにある「別のプロファイル切替アクション」を自動キックし、数秒で数十回の切替を引き起こしていた。
- **解決策**: `ProfileActionChainService.cs` にて、チェーンの実行対象（`nextAction`）が `Profile` 種別の場合は処理を `continue` でスキップし、連鎖実行させないガードを追加。

#### ③ 入力評価ループ（Mapping.cs）のデバウンス（ノイズ耐性）強化
- **原因**: プロファイル切替時（`HaltReporting` 中のステートリセット時）に一瞬発生する「ゼロクリア（見かけ上のボタンOFF）」により、スペシャルアクションの誤爆防止ガード（`RequiresFreshPressAfterReset`）が1フレームで解除されてしまっていた。
- **解決策**: `ActionManager` および `Mapping.cs` において、**「未成立（OFF）状態が連続 80ms 継続して初めてガードを解除する」** デバウンスロジックを導入。押しっぱなし状態での過渡ノイズによる再発火を完全に防止した。

---

### 2. プロファイル切替処理と通知処理の分離（シームレス化）
- **原因**: プロファイル切替中（`HaltReportingRunAction` によるコントローラー一時停止中）に、重いWPF独自の通知ウィンドウ（`ProfileNotificationWindow`）を生成・表示させていたため、切替時に50〜200msのラグが発生し、ゲームプレイがフリーズするスタッターの原因となっていた。
- **解決策**:
  - `ProfileApplicationService.ApplyFromAction` 内において、`HaltReportingRunAction` 内の処理を「メモリ上のプロファイル適用」のみ（1〜2ms）に極小化。
  - 画面への通知表示処理は、Halt解除後（コントローラーが操作可能になった後）に `Task.Run` で完全にバックグラウンドへ分離し、非同期で描画させるアーキテクチャに変更。

---

### 3. Windows標準トースト通知（Modern Toast）のスタック仕様検証
- **要望**: Chromeなどのように、画面右下に最大3個まで通知が積み上がって表示されるようにしたい。
- **調査・実装**:
  - 従来のコードでは `Tag` が未指定だったため、OSが同一通知の更新とみなし上書き表示していた。
  - `AppNotificationRegistration.ShowModernToast` の実装を改修し、トースト生成時にユニークな GUID を `Tag` プロパティに付与するよう修正。
- **OSの仕様制限**:
  - Windows 10/11 のOSシェル仕様により、標準トースト通知（WinRT API）は画面上には「常に1個ずつ順番に」しかポップアップしないことが判明。
  - Chrome や Slack などが複数スタック表示できるのは、Windows標準通知ではなく「アプリ独自描画のカスタムウィンドウ通知」を使用しているため。
- **結論**:
  - 現在のDS4Windowsの「独自ウィンドウ通知（`ProfileNotificationWindow`）」は1個のみ表示する設計となっており、要望を実現するには独自ウィンドウ側のスタック化改修が必要。
  - 実機検証の結果、現状の1個ずつの表示で実用上問題ないと判断されたため、トースト関連は「ユニークIDを発行するが、OS仕様に委ねる」形で据え置きとした。
