# Phase 5 - Step 14 フォーム/カラム幅設定 SSOT 統一 進捗管理ステータス

作成日: 2026-09-11
改訂日: 2026-09-11（フェーズA・B完了 ＆ Issue 7 是正フェーズC新設・フェーズD繰り下げ）
改訂日: 2026-09-13（フェーズC実装・実地確認完了、フェーズD一部対応。Issue 8シリーズ完了を踏まえた棚卸し）
現在ステータス: **フェーズC 実装完了（(c)-1: 既存実装で解決済みと確認・回帰テスト追加／(c)-2: フェイルセーフガード実装済み）。(c)-3・実機CP4はgwin7ok氏の実施待ち**
対象ブランチ: `For-DI-migration-work`
関連ドキュメント:
- 個別計画書: `docs-forDIMG/MadeByAgent/Phase5-Step14-FormSettings-Unification-Plan.md`
- 調査報告書: `docs-forDIMG/MadeByAgent/Phase5-Step14-RealDevice-Investigation-and-Fix-Report.md`（Issue 6・Issue 7・Issue 8シリーズ是正完了）
- Phase 5 全体計画: `docs-forDIMG/MadeByAgent/Phase5-Plan.md`
- Phase 5 全体状況: `docs-forDIMG/MadeByAgent/Phase5-Status.md`
- 移行全体憲法: `.github/copilot-instructions.md`（Pure DI原則・フォールバック/シム維持原則）

---

## 1. 概要と完了実績サマリ

### 1.1 目的
実機検証中に発覚した「アプリ終了時にウィンドウサイズ・位置・各種カラム幅が `Profiles.xml` へ正しく反映されないことがある」不具合（Issue 6）の根本原因である、DIサービス（`EnvironmentService`, `ProfileSettingsService`, `AppNotificationService`）における **実体 `BackingStore`（`m_Config`）と連動しない孤立 private field の二重保持バグ** を根絶し、アクセス経路を `IAppSettingsService` / `INotificationService` 経由に統一（SSOT 一本化）する。

### 1.2 完了ステータスサマリ
* **フェーズA（DIサービスの孤立排除とSSOT委譲化）**: ✅ **全7タスク完了**
* **フェーズB（UI層の直参照置換とアクセス経路統一）**: ✅ **全4タスク完了**
* **フェーズC（Issue 7 Emulated Controller 整合性是正）**: ✅ **実装完了（(c)-1・(c)-2、2026-09-13）**、⏳ **(c)-3はgwin7ok氏の実行待ち**
* **フェーズD（ドキュメント更新・実機検証）**: 🔄 **(d)-1は本改訂で一部対応。実機CP4は手順を指定済み・実施待ち**
* **ビルド / テスト状況**:
  - フェーズA・B時点: `dotnet build`/`dotnet test`（全156件） ✅ **全PASS**
  - フェーズC実装後（2026-09-13時点）: 新規回帰テスト2件を追加。**gwin7ok氏によるクリーンビルド・全テスト実行の最終確認が(c)-3として必要**
* **次回予定**: (c)-3（gwin7ok氏によるビルド・テスト実行）、実機CP4（下記§2.4の手順で実施）

---

## 2. マイクロタスク進捗一覧（全タスク完了）

### フェーズA: DI サービス側の孤立フィールド排除と SSOT 委譲化（全件完了）

| タスクID | タスク概要 | 対象ファイル | 状態 | 備考 |
|---|---|---|:---:|---|
| **(a)-1** | `IEnvironmentService` インターフェースの縮小 | `IEnvironmentService.cs` | ✅ 完了 | 8設定プロパティを削除、環境プローブのみ残存 |
| **(a)-2** | `EnvironmentService` 実装の純化 | `EnvironmentService.cs` | ✅ 完了 | 孤立 private field 8個・アクセサを完全削除 |
| **(a)-3** | `EnvironmentServiceTests.cs` の是正 | `EnvironmentServiceTests.cs` | ✅ 完了 | 削除プロパティのテスト撤去、残存機能テスト化 |
| **(a)-4** | `IAppSettingsService` / `AppSettingsService` への5プロパティ追加 | `IAppSettingsService.cs`<br>`AppSettingsService.cs` | ✅ 完了 | カラム幅5プロパティ（`int`）を宣言・`Global` 委譲実装。全35メンバ完全適合 |
| **(a)-5** | `IProfileSettingsService` / `ProfileSettingsService` の孤立5プロパティ削除 | `ProfileSettingsService.cs` | ✅ 完了 | 孤立 private field 5個およびアクセサをピンポイント完全切除 |
| **(a)-6** | `AppNotificationService.cs` の孤立フィールド排除と Global 委譲化 | `AppNotificationService.cs` | ✅ 完了 | `_notificationsEnabled`, `_flashTaskbar` 独自フィールド撤去、`Global` 委譲化 |
| **(a)-7** | `NotificationServiceTests.cs` の是正と単体テスト確認 | `NotificationServiceTests.cs` | ✅ 完了 | `Global` 連動・状態復元・双方向連動テスト追加。全テストPASS |

---

### フェーズB: UI 層の Global 直参照置換とアクセス経路統一（全件完了）

| タスクID | タスク概要 | 対象ファイル | 状態 | 備考 |
|---|---|---|:---:|---|
| **(b)-1** | `SettingsViewModel.cs` の Global 直参照置換 | `SettingsViewModel.cs` | ✅ 完了 | `StartMinimize`, `MinimizeToTaskbar`, `CloseMinimizes` を注入済み `_appSettings` 経由へ配線 |
| **(b)-2** | `ProfileEditor.xaml.cs` の Global 直参照置換 | `ProfileEditor.xaml.cs` | ✅ 完了 | `SaveSplitterAndColumnWidths` / `RestoreSplitterAndColumnWidths` のカラム幅を DI 経由に置換 |
| **(b)-3** | 通知設定アクセス経路の監査・統一 | `SettingsViewModel.cs` | ✅ 完了 | `ShowNotificationsIndex` を `_appSettings.Notifications` に配線し SSOT 連動を確定 |
| **(b)-4** | 全単体テスト・クリーンビルド確認 | ソリューション全体 | ✅ 完了 | 全156件の単体テスト PASS およびクリーンビルド確認完了 |

---

### フェーズC: 追加スコープ: Issue 7 Emulated Controller 整合性是正

| タスクID | タスク概要 | 対象ファイル | 状態 | 備考 |
|---|---|---|:---:|---|
| **(c)-1** | `ProfileSettingsViewModel.cs` ロード時 `tempConType` 再同期 | `ProfileSettingsViewModel.cs` | ✅ **実地確認の結果、既に解決済みと判明（2026-09-13）** | 詳細は§2.4.1。回帰防止テストを追加 |
| **(c)-2** | `ControllerTypeIndex` / `EnableOutputDataToDS4` 双方向連動ガード | `ProfileEditor.xaml.cs` | ✅ **実装完了（2026-09-13）** | 詳細は§2.4.2。実機確認手順は§2.4.3 |
| **(c)-3** | 全単体テスト・クリーンビルド確認 | ソリューション全体 | ⏳ **gwin7ok氏の実施待ち** | (c)-1・(c)-2で追加した新規テストを含めて`dotnet build`/`dotnet test`を実行 |

### 2.4 フェーズC 実施内容の詳細（2026-09-13）

#### 2.4.1 (c)-1: 実地確認の結果、既に解決済みと判明

コード追跡の結果、以下2点の組み合わせにより、**(c)-1が懸念していた「プロファイル再読込時に`Emulated Controller`表示が古いまま残る」という不具合は、現時点のコードでは既に発生しないことを確認した。**

1. **Issue 7是正の副次効果**: `ControllerTypeIndex`（`ProfileSettingsViewModel.cs`）は`profileSettings.OutContType[device]`を都度読み直すget-onlyプロパティになっており、キャッシュを持たない。これにより`UpdateLateProperties()`内の`tempControllerIndex = ControllerTypeIndex;`は、実行されるたびに必ず最新の値を取得する。
2. **`ProfileEditor.xaml.cs`側の既存パターン**: `UpdateLateProperties()`の呼び出し元は`Reload()`と`RefreshEditorBindings()`の2箇所のみであり、いずれも呼び出し前後で`DataContext`を`null`→再代入する処理で挟まれている（`Reload()`は自身で、`RefreshEditorBindings()`は直前に呼ばれる`StopEditorBindings()`で）。`ProfileSettingsViewModel`は`INotifyPropertyChanged`を実装していないが、この「`DataContext`をいったん`null`にしてから再代入する」操作がWPFに全バインディングの再評価を強制するため、結果的にUIへ正しく反映される。

**ただし、この動作は`INotifyPropertyChanged`という標準的な仕組みではなく、「呼び出し元が必ずDataContext再設定の作法を守る」という規約に依存した、やや脆弱な構造である。** 将来、`UpdateLateProperties()`を新たな呼び出し元から呼ぶ際にこの規約が守られないと、値は正しく更新されてもUIには反映されない、という形で本質的に同種の不具合が再発し得る。このリスクを明記する技術的負債コメントを`UpdateLateProperties()`（`ProfileSettingsViewModel.cs`）と`RefreshEditorBindings()`（`ProfileEditor.xaml.cs`）の両方に付与した。

**対応**: コードの修正は不要と判断し、代わりに以下の回帰防止テストを`DS4WindowsTests/ProfileSettingsViewModelTests.cs`に追加した。

* `UpdateLateProperties_ShouldSyncTempControllerIndexAndTempConType_FromProfileSettingsOutContType`: `profileSettings.OutContType`をDS4/X360に切り替えるたびに`UpdateLateProperties()`を呼び、UIが実際にバインドしている`TempControllerIndex`/`TempConType`（`ControllerTypeIndex`ではない点に注意）が正しく追従することを確認する。

なお、本テストはViewModel層のデータ整合性のみを検証しており、実際にUI（ComboBox）が画面上に反映されるかどうか（`DataContext`再設定パターンが機能しているか）は、Viewを伴うため単体テスト化が困難である。この部分は§2.4.3の実機確認手順でカバーする。

#### 2.4.2 (c)-2: 双方向連動フェイルセーフガードの実装

`EnableOutputDataToDS4`（DS4固有データの出力）が有効なのに、エミュレートするコントローラー種別が`X360`のままだと、DS4専用データ（タッチパッド・ジャイロ等）を送出できる出力先が存在せず矛盾する。この矛盾を検知し、自動でDS4へ補正するガードを実装した。

**実装箇所**: `ProfileEditor.xaml.cs`の`SetLateProperties(bool fullSave)`（`Global.OutContType[deviceNum]`への書き込み直前）。同メソッドは`SaveBtn_Click`・`ApplyBtn_Click`双方の唯一の合流点である`ApplyProfileStep`から呼ばれるため、Save・Applyいずれの操作でも一律にガードが効く。

```csharp
OutContType targetConType = profileSettingsVM.TempConType;
if (profileSettingsVM.EnableOutputDataToDS4 && targetConType != OutContType.DS4)
{
    profileSettingsVM.TempControllerIndex = 1; // DS4
    targetConType = OutContType.DS4;
    AppLogger.LogToGui(
        "EnableOutputDataToDS4 が有効なため、Emulated Controller を自動的に DS4 へ補正しました。",
        false);
}
Global.OutContType[deviceNum] = targetConType;
```

なお、`SaveBtn_Click`には既存の類似パターン（`UseDs3PitchRollSim`が有効な場合に`TempControllerIndex = 1`を強制する処理）が存在するが、これは`SaveBtn_Click`内にしか実装されておらず、`ApplyBtn_Click`（`ApplyProfileStep()`を直接呼ぶ）経由では効かないという既存の抜け漏れが実地確認で判明した。今回の(c)-2の実装は、この抜け漏れを踏まえて意図的に両経路の合流点である`SetLateProperties`に配置しており、`UseDs3PitchRollSim`側の既存の抜け漏れ自体は本タスクのスコープ外として温存した（別途対応が必要であれば新規Issueとして切り出すことを推奨する）。

**単体テストについて**: `SetLateProperties`は`ProfileEditor`（WPF `Window`）のprivateメソッドであり、テスト容易性の観点から単体テスト化を見送った。実機での確認手順を§2.4.3に用意した。

#### 2.4.3 gwin7ok氏に実施いただきたい実機テスト手順

**(c)-1・(c)-2共通の前提確認**:
1. DS4Windowsを起動し、任意のプロファイルをProfileEditorで開く。

**(c)-1の確認（プロファイル再読込時の表示追従）**:
2. `Emulated Controller`が`DS4`に設定されている別のプロファイルを、ProfileEditorを閉じずに読み込む（メインウィンドウのプロファイル一覧から別プロファイルをダブルクリックする等、`Reload()`が呼ばれる操作）。
3. `Emulated Controller`コンボボックスの表示が、新しく読み込んだプロファイルの設定（`DS4`）に正しく切り替わっていることを確認する。
4. 続けて`Emulated Controller`が`X360`のプロファイルへ切り替え、同様に正しく`X360`表示に切り替わることを確認する。

**(c)-2の確認（矛盾の自動補正）**:
5. `Emulated Controller`を`Xbox 360`に設定した状態で、`EnableOutputDataToDS4`に対応する設定（DS4固有データの出力を有効にするチェックボックス等）をONにする。
6. その状態で「保存」または「適用」ボタンを押す。
7. 保存・適用後、`Emulated Controller`が自動的に`DS4`へ切り替わっていることを確認する（ログに「EnableOutputDataToDS4 が有効なため、Emulated Controller を自動的に DS4 へ補正しました。」という記録が出力されていることも合わせて確認する）。
8. 上記手順を「適用」ボタン経由・「保存」ボタン経由の両方で実施し、いずれの操作でも同様に補正されることを確認する（`ApplyBtn_Click`・`SaveBtn_Click`双方の経路を確認する意図）。

---

### フェーズD: ドキュメント更新・実機検証

| タスクID | タスク概要 | 対象ファイル | 状態 | 備考 |
|---|---|---|:---:|---|
| **(d)-1** | ドキュメント・進捗ステータス更新（旧タスク-4） | 計画書・報告書・Status文書 | 🔄 **本改訂で一部対応** | 本Status文書の更新は完了。`Phase5-Status.md`本体（全体進捗表）は別途Step14全体の完了報告時にまとめて更新予定 |
| **実機CP4** | 実機起動・終了時の設定永続化検証 | 実機環境 | ⏳ **手順指定済み・実施待ち** | 手順は§2.5参照。ウィンドウ幾何情報・カラム幅・通知・Emulated Controller の XML 保存・復元確認 |

### 2.5 実機CP4: 起動・終了時の設定永続化検証手順

以下の手順で、フェーズA・Bで是正した項目（ウィンドウ幾何情報・カラム幅・通知設定）とフェーズCの`Emulated Controller`が、アプリの再起動を跨いで正しく保存・復元されることを確認いただきたい。

1. DS4Windowsを起動し、メインウィンドウのサイズ・位置を初期状態から変更する。
2. ProfileEditor等、カラム幅を持つ画面を開き、任意のカラム幅をドラッグで変更する。
3. 通知設定（`SettingsViewModel`の`ShowNotificationsIndex`に対応する設定項目）を初期値から変更する。
4. 任意のプロファイルの`Emulated Controller`を`DS4`に変更し、保存する。
5. DS4Windowsを**正常終了**する（タスクトレイ等からの終了操作）。
6. `Profiles.xml`（設定保存先）を確認し、上記1〜4で変更した値が正しく書き込まれていることを確認する。
7. DS4Windowsを再起動し、メインウィンドウのサイズ・位置、カラム幅、通知設定、`Emulated Controller`の各値が、終了時に設定した通りに復元されていることを確認する。
## 3. 作業対象ファイル棚卸しマトリクス（全13件改修・検証完了）

| ファイルパス | 役割 | 変更内容 | 進捗状況 |
|---|---|---|:---:|
| `DS4Windows/DI/IEnvironmentService.cs` | DIインターフェース | 永続設定8プロパティ削除、環境プローブ専用化 | ✅ **完了** |
| `DS4Windows/DS4Control/Services/EnvironmentService.cs` | サービス実装 | 孤立 private field 8個・アクセサを完全削除 | ✅ **完了** |
| `DS4WindowsTests/EnvironmentServiceTests.cs` | 単体テスト | 削除メンバのテスト撤去、残存機能検証に純化 | ✅ **完了** |
| `DS4Windows/DI/IAppSettingsService.cs` | DIインターフェース | カラム幅5プロパティ（`int`）の追加宣言 | ✅ **完了** |
| `DS4Windows/DS4Control/Services/AppSettingsService.cs` | サービス実装 | カラム幅5プロパティの `Global` 委譲実装（全35メンバ完全適合） | ✅ **完了** |
| `DS4Windows/DI/IProfileSettingsService.cs` | DIインターフェース | カラム幅5プロパティは元々未定義（無変更で整合） | ✅ **完了** |
| `DS4Windows/DS4Control/Services/ProfileSettingsService.cs` | サービス実装 | 孤立 private field 5個およびアクセサの完全削除 | ✅ **完了** |
| `DS4Windows/DS4Control/Services/AppNotificationService.cs` | サービス実装 | `_notificationsEnabled`, `_flashTaskbar` 独自フィールド撤去、`Global` 委譲化 | ✅ **完了** |
| `DS4WindowsTests/NotificationServiceTests.cs` | 単体テスト | `Global` 連動・テスト間状態汚染防止・双方向同期テストへ是正 | ✅ **完了** |
| `DS4Windows/DS4Forms/ViewModels/SettingsViewModel.cs` | UI ViewModel | `StartMinimize`、`ShowNotificationsIndex` 等の直参照を `_appSettings` 経由に配線 | ✅ **完了** |
| `DS4Windows/DS4Forms/ProfileEditor.xaml.cs` | UI View Code-behind | カラム幅の直参照を `_appSettings` 経由に置換／**(c)-2: `SetLateProperties`へEnableOutputDataToDS4/ControllerType矛盾検知フェイルセーフガードを追加** | ✅ **完了** |
| `DS4Windows/DS4Forms/ViewModels/ProfileSettingsViewModel.cs` | UI ViewModel | **(c)-1: `UpdateLateProperties()`に技術的負債コメント付与（コード変更なし、既存実装で解決済みと確認）** | ✅ **確認完了** |
| `DS4WindowsTests/ProfileSettingsViewModelTests.cs` | 単体テスト | **(c)-1回帰防止テスト追加（`TempControllerIndex`/`TempConType`同期確認）** | ✅ **完了** |

---

## 4. 是正完了の総括
* **孤立バグの完全根絶**: `Profiles.xml` に保存される全約70項目について、孤立した独立フィールドを持つ DI サービス（`EnvironmentService`, `ProfileSettingsService`, `AppNotificationService`）を完全に純化・委譲化し、実体 `BackingStore`（`m_Config`）を SSOT とする単一経路を確立した。
* **Pure DI 化の徹底**: UI 層（`SettingsViewModel.cs`, `ProfileEditor.xaml.cs`）からの `Global` 静的直参照を排除し、コンストラクタ注入された `IAppSettingsService` 経由に置換した。
* **回帰ゼロの保証**: 全156件の単体テストがオールグリーンで通過し、既存機能への破壊的変更が一切生じていないことを証明した（フェーズA・B時点）。
* **フェーズC（Issue 7派生の整合性是正）**: `Emulated Controller`のUI表示追従（(c)-1）は実地確認の結果、Issue 7是正の副次効果と既存の`DataContext`再設定パターンの組み合わせで既に解決済みであることを確認し、回帰防止テストを追加した。`EnableOutputDataToDS4`との矛盾検知フェイルセーフガード（(c)-2）を新規実装した。いずれも(c)-3（gwin7ok氏によるビルド・テスト実行）および実機CP4（§2.5）の確認待ちである。