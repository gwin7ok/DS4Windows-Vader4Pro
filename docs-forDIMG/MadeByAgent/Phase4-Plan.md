# フェーズ4 計画書: `Global` 分割と ViewModel DI 化

作成日: 2026-08-31
対象ブランチ: For-DI-migration-work
前提ドキュメント:
- `docs-forDIMG/DI-App-Wide-Migration-Plan.md` §5、§6.6（全体方針・フェーズ4計画）
- `docs-forDIMG/MadeByAgent/Phase2-Plan.md`／`Phase2-Status.md`（Phase2 完了結果）
- `docs-forDIMG/MadeByAgent/Phase3-Plan.md`／`Phase3-Status.md`（Phase3 完了結果・引継ぎ事項）
- `docs-forDIMG/MadeByAgent/Phase3-Step4-Automated-Test-Coverage-Report.md`
- `docs-forDIMG/MadeByAgent/Phase3-Step5-RealDevice-Verification-Checklist.md`
- `.github/copilot-instructions.md`（移行ルール、巨大ファイル編集方針、段階的実施方針）

## ルール確認（作業開始前に毎回読む）

- §2.1: 新 DI 経路の動作確認までは旧方式をシムとして残す。1機能に複数の新実装経路を作らない。
- §2.2: 既存機能、条件分岐、エッジケース、配列インデックス、状態管理を維持する。
- §2.3: 既存ログを削除・変更しない。新規ログは必要性を確認して最小限にする。
- §3.1: 新しい依存は原則コンストラクタ・インジェクションで受け取る。実行時引数付き ViewModel は Factory を使用する。
- §3.2: `ScpUtil.cs`（`Global`）や `Mapping.cs` を全体再生成せず、対象メンバー単位でピンポイント変更する。
- §4: 1ステップずつ実施し、各ステップのビルド・テスト・結果記録後に確認を挟む。

## 文書の役割と更新方針

本書は Phase4 作業時の詳細計画の正本とする。各 Step の着手前に、対象ファイル・実コード・依存関係・完了基準を再確認し、調査で判明した差分を本書へ反映する。各 Step 完了後は、実施結果、変更したサービス／ViewModel、テスト結果、残課題、次 Step への引継ぎを本書へ追記・更新する。

全体計画書 §3 の4層モデルにおいて、本Phaseの主対象は第4層（UI層）である。UI層は実行時信号の第4段ではなく、設定・プロファイル・状態をサービス経由で実行時3層へ反映する制御面として扱う。

`docs-forDIMG/DI-App-Wide-Migration-Plan.md` は他フェーズを含む全体像、フェーズ間の依存関係、全体の完了条件を確認するための文書とし、Phase4 の細かな対象一覧や作業手順は重複して管理しない。両文書に差分が生じた場合、Phase4 の実装判断・Step 状態・対象一覧は本書、全体のフェーズ構成・依存関係は全体計画書を更新する。

---

## 0. 着手前調査で判明した事実

### 0.1 `Global` の実体

`Global` は独立した `Global.cs` ではなく、`DS4Windows/DS4Control/ScpUtil.cs` 内の `public class Global` として定義されている。プロファイル、入力、出力、環境情報、UI レイアウト、通知・キャッシュ、純粋ヘルパーが一つの静的クラスに混在している。

したがって、フェーズ4ではクラス全体を置換せず、責務別にメンバーと呼び出し元を棚卸しし、サービスを一つずつ追加してから既存 static API を薄いシムへ変更する。

### 0.2 DI と起動経路

- `DS4Windows/DI/ServiceRegistration.cs` が `AppHost` の正式登録先である。
- `IProfileSettingsService` は現状 `ProfileSettingsServicePlaceholder` の登録に留まっている。
- `App.xaml.cs` は `ServiceProviderHolder` 用の簡易 `ServiceCollection` を構築し、その後 `AppHost.CreateHost()` を別途呼び出している。
- Phase4 ではこの二重構成を整理し、最終的にアプリのサービス解決先を `AppHost` 側へ一本化する。ただし、既存呼び出し元を一度に変更せず、移行中は互換シムを維持する。

### 0.3 ViewModel の実体

`DS4Windows/DS4Forms` 配下では、XAML コードビハインドから ViewModel を直接 `new` している箇所が少なくとも16ファイルある。対象には次の3種類が混在する。

1. 引数なしで生成できる画面 ViewModel。
2. `ControlService`、`ProfileList`、`OutputSlotManager` 等の共有依存を必要とする ViewModel。
3. `deviceNum`、`device`、`SpecialAction`、`DS4ControlSettings`、`version` 等の実行時引数を必要とする ViewModel。

3 の種類を Singleton として登録すると画面状態の混線や実行時値の欠落につながるため、明示的な Factory で生成する。

### 0.4 Phase3 からの引継ぎ

Phase3 の実装と自動テストは完了しているが、次の項目は Phase4 で DI 経路と合わせて扱う。

- `Mapping.cs` の `ApplyProfileDirect`／`RestoreProfileDirect` に残る `Program.rootHub` と `ControlService` 型引数依存。
- `LaunchProgram` の実機確認で `×` となった、外部プログラム起動・多重起動防止経路。
- Bluetooth 切断後の再接続、非管理者 UAC、UAC 承認／拒否、ラムブル、`IDeviceStateAccessor` 経路などの `△`／未実施項目。
- `IDs4DeviceRegistry.ReEnableDevice` と `IElevatedProcessLauncher` の責務境界。Phase3 の境界設計を維持し、Phase4 で不用意に再統合しない。

---

## 1. フェーズ4の目的・スコープ

### 1.1 目的

`Global` の横断的な責務をサービスへ段階分割し、ViewModel の生成を `AppHost`／Factory 経由に統一する。最終的には UI 層が `Global`、`Program.rootHub`、旧 `ServiceProviderHolder`、直接生成に依存しない状態を目指す。

### 1.1.1 全体4層モデルとの責務境界

全体計画書で採用した4層モデルに従い、Phase4は第4層（UI層）と、UI層から実行時3層へ設定・状態を渡すサービス境界を主対象とする。入力監視層、信号変換層、信号・アクション実行層（3-a／3-b／3-c）の実行順序と責務を、Phase4のサービスやViewModelへ移動・重複させない。

- UI層は設定・プロファイル・デバイス状態を表示・編集し、サービス経由で実行時3層へ反映する。UIやViewModelが直接キー送出、仮想コントローラー出力、プロファイル切替、プロセス起動を実行しない。
- 信号変換層は入力やSpecialActionを、種別・対象・値・順序・タイミングを持つ実行指示へ分解する。Phase4のリポジトリはSpecialActionの定義データを管理するが、実行指示の生成・実行そのものは担当しない。
- 信号・アクション実行層は実行指示を3-a（仮想コントローラー）、3-b（KBM）、3-c（アプリ内アクション）へ振り分けて実行する。混在マクロの順序・遅延・キャンセルを含む実行責務はこの層に残す。

### 1.2 対応対象

| 対象 | Phase4での扱い | 方針 |
|---|---|---|
| `IProfileSettingsService` Placeholder | 対応する | 実際の設定読書き・保存・既定値・変更通知を実装し Placeholder を置換 |
| `Global` のプロファイル管理 | 対応する | `IProfileRepository` として分離。Phase3 の `ApplyProfileDirect`／`RestoreProfileDirect` もここで整理 |
| `Global` の SpecialAction 管理 | 対応する | `ISpecialActionRepository` としてデータアクセスを分離。Action 実行責務とは混在させない |
| 入力・出力設定 | 対応する | `IInputBehaviorSettingsService`、`IOutputHandlerSettingsService` 等へ段階移行 |
| デバイス接続状態 | 対応する | Phase3 の `IDs4DeviceRegistry`／`IDeviceStateAccessor` と重複しない状態管理サービスにする |
| 環境情報・UI レイアウト・通知 | 対応する | 既存の `IEnvironmentInfoProvider`、`IAppPathsProvider`、`IAppearanceSettingsService` 等へ整理し、純粋定数・純粋関数は残す |
| ViewModel 直接生成 | 対応する | 引数なしは DI、共有依存はコンストラクタ注入、実行時引数は Factory |
| `DS4Devices.ReEnableDevice` と昇格サービスの再設計 | 原則対応しない | Phase3 の責務境界を維持し、実機確認・呼び出し元整理に限定 |
| `Global` の全メンバー一括置換 | 対応しない | 巨大ファイル破損と挙動変更を避け、責務単位・PR単位で移行 |
| 信号変換層・信号／アクション実行層の再設計 | 原則対応しない | 4層モデルの境界を維持し、UI・設定サービスが3-a／3-b／3-cの実行責務を直接持たないことを確認 |

---

## 2. ステップ分割（各ステップ完了後にビルド・テスト・記録・確認を行う）

| ステップ | 内容 | 完了基準 | PR粒度 |
|---|---|---|---|
| **4-0** | 現状棚卸し・基準テスト | `Global` メンバー、全呼び出し元、ViewModel 直接生成箇所、起動順序、イベント購読、既存ログを一覧化。Phase2/3 の自動テスト成功 | 調査・記録1件 |
| **4-1** | `IProfileSettingsService` 実装化 | Placeholder を実装へ置換し、設定の既定値・保存・読込・変更通知・配列境界を維持 | 1サービス |
| **4-2** | `IProfileRepository` 分離 | プロファイル読込・保存・選択・切替を移行し、Phase3 の `ApplyProfileDirect`／`RestoreProfileDirect` 依存を整理 | 1サービス |
| **4-3** | `ISpecialActionRepository` 分離 | SpecialAction の取得・保存・正規化を移行し、`ActionManager` の実行責務と分離 | 1サービス |
| **4-4** | 入力・出力・デバイス状態サービス | `IInputBehaviorSettingsService`、`IOutputHandlerSettingsService`、`IDeviceConnectionTracker` を責務単位で導入 | 1サービスまたは小機能 |
| **4-5** | 環境・UI・通知サービス | `IEnvironmentInfoProvider`、`IAppPathsProvider`、`IAppearanceSettingsService`、通知／キャッシュ責務を必要最小限移行 | 1サービス単位 |
| **4-6** | Composition Root 一本化 | `App.xaml.cs` の簡易 DI と `ServiceProviderHolder` 依存を整理し、`AppHost`／`ServiceRegistration` を正式な解決経路にする | 起動経路1件 |
| **4-7** | ViewModel パターンA移行 | 引数なし ViewModel の依存を明示し、DI 登録と画面取得へ移行 | 3〜5 ViewModel/PR |
| **4-8** | ViewModel パターンB移行 | 共有依存をコンストラクタ注入し、画面ライフサイクルとイベント解除を維持 | 3〜5 ViewModel/PR |
| **4-9** | ViewModel パターンC Factory 化 | 実行時引数付き ViewModel を `IXxxViewModelFactory.Create(...)` 経由へ移行 | 1 ViewModel/PR |
| **4-10** | Phase3 引継ぎ再確認・シム整理 | `LaunchProgram` 等の引継ぎ項目を DI 経路で再確認し、呼び出し元ゼロのシムだけ削除。残課題は記録 | 検証・整理1件 |

一つのステップ内でも、対象サービスまたは ViewModel 群ごとに小さな PR に分割する。各 PR で旧経路を削除する場合は、新経路の自動テスト・起動確認・必要な実機確認が済んでいることを前提とする。

---

## 3. 各ステップの詳細

### Step 4-0: 現状棚卸し・基準テスト

1. `ScpUtil.cs` 内の `Global` メンバーを、プロファイル、入力、出力、デバイス、環境、UI、通知、純粋ヘルパーに分類する。
2. `rg "Global\\." DS4Windows` で全呼び出し元を抽出し、移行対象サービスと対応 PR を記録する。
3. `new XxxViewModel(...)` を XAML コードビハインド単位で一覧化し、パターンA/B/Cに分類する。
4. `App.xaml.cs`、`AppHost.cs`、`ServiceRegistration.cs`、`ServiceProviderHolder.cs` の起動・解決順を図示する。
5. 移行前の `dotnet build`、`dotnet test`、主要画面起動、既存ログ出力を基準結果として保存する。

### Step 4-1: `IProfileSettingsService` 実装化

現状の `ProfileSettingsServicePlaceholder` を廃止対象とし、`Global` の `BackingStore` に存在する設定値を、既存の保存形式・既定値・スロット数・変更通知を保ったままサービスへ移す。`Global` の既存プロパティは当面サービスへ委譲するシムとして残す。

検証対象は設定の読込、保存、既定値、`TEST_PROFILE_ITEM_COUNT` の境界、プロファイル変更通知、異常時の既存ログとする。

### Step 4-2: `IProfileRepository` 分離

`ProfilePath`、`OlderProfilePath`、`SelectedProfile`、`LinkedProfileUI`、`ProfileActions`、`LoadProfile`、`ApplyProfile`、保存処理を、プロファイルのデータアクセス・切替責務として整理する。

Phase3 で残した `ApplyProfileDirect`／`RestoreProfileDirect` は、`ControlService` 自体を `Global` の API に渡す構造をそのまま新インターフェースへコピーしない。必要な操作をプロファイルサービスの責務として再定義し、`Mapping` が `Program.rootHub` を直接取得しなくて済む境界を設計する。挙動変更を避けるため、既存の切替順序・一時プロファイル・距離プロファイル・ログを比較する。

### Step 4-3: `ISpecialActionRepository` 分離

SpecialAction の読込・保存・名前正規化・無効アクション記録をリポジトリへ移す。`IActionFactory` はActionの生成を担当し、`IManagedActionManager` は実行指示の実行層へのディスパッチを担当する。リポジトリは定義データの保持だけを担当する。SpecialActionの実行先が3-a／3-b／3-cのいずれであっても、この責務分離を維持する。

`SpecialActionEditor` と `SpecialActionsListViewModel` の既存編集・再表示・削除・無効アクションログを回帰対象とする。

### Step 4-4: 入力・出力・デバイス状態サービス

- `IInputBehaviorSettingsService`: タッチパッド、ジャイロ、スティック、トリガー、感度、デッドゾーン、反転、デバウンス等を担当。
- `IOutputHandlerSettingsService`: 出力タイプ、ViGEm／FakerInput／HidHide の状態、出力先設定を担当。Phase2 の `IVirtualKBM` 自体や3-a／3-bの実行責務は置換しない。設定サービスは実行層が参照する設定を提供するだけとする。
- `IDeviceConnectionTracker`: 初回接続フラグ、接続中状態、デバイス単位の一時状態を担当。HID 列挙そのものは Phase3 の `IDs4DeviceRegistry` に残す。

リアルタイム入力スレッドと WPF UI スレッドの両方から参照される値は、呼び出し元スレッドを棚卸しし、必要な同期だけを追加する。全 `Global` を一つのロックで囲まない。

### Step 4-5: 環境・UI・通知サービス

- `IEnvironmentInfoProvider`: バージョン、管理者権限、OS・ドライバー存在判定。
- `IAppPathsProvider`: exe／AppData パス。Host 構築前に必要な値の初期化順序を管理する。
- `IAppearanceSettingsService`: 言語、テーマ、MainWindow／ProfileEditor／Controller タブの位置・サイズ・列幅。
- 通知／キャッシュ責務: `ProfileChanged` 等のイベント、通知選択、UI キャッシュを既存の適切なサービスへ配置し、独立インターフェース化が必要かは Step 4-0 の棚卸しで決定する。静的イベントの購読解除は必ず維持する。

`Clamp`、バージョン番号計算、単純な変換など副作用のない関数は、テスト可能性のためだけに DI サービスへ移さない。

### Step 4-6: Composition Root 一本化

1. `ServiceRegistration.AddAppServices` を全サービスの登録先とする。
2. `App.xaml.cs` の簡易 `ServiceCollection` と `ServiceProviderHolder` の利用箇所を一覧化する。
3. 既存テストや移行途中の呼び出し元を壊さない互換層を残しながら、解決処理を `AppHost.GetService`／コンストラクタ注入へ寄せる。
4. `Program.rootHub` を必要とするサービスは、アプリが `ControlService` を生成した後に遅延解決する。Host 構築時に null の実体を Singleton 化しない。
5. 正式ルートでの解決確認後、旧 Provider の本番利用を削除する。テストだけが旧 Provider に依存している場合は、テストを先に正式ルートへ移す。

### Step 4-7〜4-9: ViewModel 移行

#### パターンA: 引数なし ViewModel

`MainWindowsViewModel`、`SettingsViewModel`、`LogViewModel`、`ChangelogViewModel`、`LanguagePackViewModel`、`PresetOptionViewModel`、`RenameProfileViewModel`、引数なし SpecialAction ViewModel 等を対象とする。内部の `Global` 参照や静的イベントを依存として明示してから `ServiceRegistration` に登録する。

#### パターンB: 共有依存 ViewModel

`ControllerListViewModel`、`TrayIconViewModel`、`CurrentOutDeviceViewModel`、`AutoProfilesViewModel`、`FirstLauchUtilViewModel`、`ControllerRegDeviceOptsViewModel` 等を対象とする。`ControlService`、`ProfileList`、`OutputSlotManager` 等をコンストラクタで受け取り、画面の DataContext 再設定・破棄・イベント解除を維持する。

#### パターンC: 実行時引数付き ViewModel

`BindingWindowViewModel`、`ProfileSettingsViewModel`、`MappingListViewModel`、`SpecialActionsListViewModel`、`SpecialActEditorViewModel`、`RecordBoxViewModel`、`TouchButtonUserControlViewModel`、`UpdaterWindowViewModel`、`LoadProfileViewModel` 等を対象とする。

各 Factory はアプリ共有サービスをコンストラクタで受け取り、`Create` には device、profile、action、settings、version 等の実行時値だけを渡す。`SpecialActionViewModel(5/8/9)` の固定値は呼び出し元に埋め込まず、用途を表す Factory メソッドまたは明示的な purpose 値として管理する。

### Step 4-10: Phase3 引継ぎ再確認・シム整理

`Phase3-Step5-RealDevice-Verification-Checklist.md` の `△`／`×`／未実施項目を、Composition Root 一本化後の DI 経路で再確認する。特に `LaunchProgram` は、`IProcessInspector` 解決、プロファイル適用、外部プログラム起動の順に原因を切り分ける。

あわせて、UI／設定サービスから3-a／3-b／3-cを直接実行していないこと、混在マクロやSpecialActionの実行指示が既存の実行層へ渡ることを確認する。

旧 `Global` シム、旧 Provider、フォールバックは呼び出し元とテストがゼロになったものだけを個別に削除する。残す場合は、残置理由・削除条件・対象フェーズを Status または完了報告へ記録する。

---

## 4. リスクと回避策

| リスク | 該当ステップ | 回避策 |
|---|---|---|
| `Global` の初期化順序が変わり設定・出力状態が null になる | 4-1〜4-6 | 遅延委譲、既定値比較、起動ログ比較、サービス解決テストを行う |
| `ServiceProviderHolder` と `AppHost` が別インスタンスを返す | 4-6 | 正式 Provider を一つに固定し、同一 Singleton 解決テストを追加する |
| `ScpUtil.cs` の大規模編集で既存処理を欠損させる | 全ステップ | 対象メンバーのみをピンポイント変更し、毎ステップビルドする |
| 設定配列のスロット番号・既定値が変わる | 4-1〜4-4 | 境界値・全スロットの単体テストと設定ファイルの保存／再読込比較を行う |
| UI／入力スレッド間の競合やイベントリークが発生する | 4-4〜4-9 | 呼び出し元スレッドと購読解除を一覧化し、必要最小限の同期を追加する |
| ViewModel を Singleton 化して画面状態が混線する | 4-7〜4-9 | 画面状態は Transient、実行時引数は Factory、共有状態だけ Singleton とする |
| `ApplyProfileDirect` の依存解消でプロファイル切替挙動が変わる | 4-2 | 切替順序、一時プロファイル、距離プロファイル、ログ、実機結果を移行前後で比較する |
| Phase3 の実機未対応項目をサービス移行後も再現できない | 4-10 | 各項目に再現条件・使用デバイス・確認ログ・対応状態を記録する |

---

## 5. 完了判定基準（Phase4全体）

- [ ] `IProfileSettingsService` が Placeholder ではなく実設定を扱い、既定値・保存・読込・変更通知が既存挙動と一致する
- [ ] `IProfileRepository`、`ISpecialActionRepository`、入力・出力・デバイス状態・環境・UI サービスが `ServiceRegistration` から解決できる
- [ ] Phase3 の `ApplyProfileDirect`／`RestoreProfileDirect` の `Program.rootHub` 依存が解消済み、または再現条件・理由・次の対応が明記されている
- [ ] `AppHost` が唯一の本番 Composition Root となり、`ServiceProviderHolder` の本番利用と二重登録が解消されている
- [ ] 移行対象 XAML コードビハインドの直接 `new XxxViewModel(...)` がなく、DI または Factory 経由で生成される
- [ ] ViewModel の Singleton／Transient／Factory のライフタイムが各画面の実態に合い、DataContext とイベント解除が維持されている
- [ ] Phase3 の `LaunchProgram`、UAC、Bluetooth 再接続、ラムブル等の引継ぎ項目が確認済み、または未対応事項として文書化されている
- [ ] Phase2/3 を含む全自動テスト、ビルド、主要画面の起動・操作確認が成功している
- [ ] 新経路の確認前に旧シムを削除しておらず、削除したシムには削除根拠がある
- [ ] UI／設定サービスが実行時3層の実行責務を直接持たず、SpecialAction／混在マクロの実行指示が3-a／3-b／3-cへ正しく引き渡される

---

## 6. テスト計画

1. 各サービスについて、既定値、読書き、保存、配列境界、イベント通知、異常時挙動を単体テストする。
2. 各 Factory について、実行時引数の伝達、依存差し替え、画面再生成時の状態分離をテストする。
3. `AppHost` の全 Phase0〜4 サービス解決、Singleton 同一性、Host 構築時の null／初期化順序をテストする。
4. `rg` による `Global.`、`Program.rootHub`、`ServiceProviderHolder.Provider`、`new XxxViewModel(` の残存検査をステップごとに行う。
5. Phase2/3 の既存自動テストを各ステップで実行し、設定保存・プロファイル切替・主要画面を手動または UI 起動確認する。
6. Step 4-10 で Phase3-Step5 の実機引継ぎ項目を再確認し、結果を `Phase4-Status.md` または完了報告へ記録する。

## 7. 次のアクション

1. 本計画書と全体計画書 §6.6 のステップ・サービス名・完了基準を突合する。
2. Step 4-0 の棚卸し表を作成し、`Global` メンバーと ViewModel の全対象を確定する。
3. Step 4-1 の `IProfileSettingsService` 実装化に着手する。
4. 各ステップ完了後にビルド、全自動テスト、必要な実機確認を実施し、結果を `docs-forDIMG/MadeByAgent/` に記録する。
