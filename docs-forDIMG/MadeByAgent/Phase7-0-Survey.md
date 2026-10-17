# Phase7-0 調査報告: Phase7 の対象の洗い出しと Step 分割案

作成日: 2026-10-17  
対象ブランチ: `For-DI-migration-work`（HEAD `1c97977f`、作業ツリーはクリーン）  
状態: 調査のみ（実装なし）。**決定事項 D1〜D6 は、すべて推奨どおり承認済み（2026-10-17）。次は `Phase7-Step1-Plan.md`**  
引き継ぎ台帳の出典: `Phase6-Step12-Plan.md` §4

---

## 0. 調査の方法と限界

- 現行コードを走査して確認した（古い文書の数値は鵜呑みにしていない）。`dotnet` は使っていない。
- 走査の道具: `scripts/audit/Step12/audit.py`（コメント除去）を再利用し、新規に `scripts/audit/Step12/iface.py`（DI インターフェースのメンバー別の参照数）を追加した。
- 精度の限界:
  - 名前で数えるため、0 件の判定は確実だが、1 件以上の判定は同名の偶然で過大になる。
  - `Global` の「転送だけ／実体あり」の分類は、本文の文字列による推定（±数件の誤差）。
  - ラチェット基準値（`Global.*` 747、`AppHost` 40、`Program.rootHub` 22）のうち、`Program.rootHub` 22 は再集計で一致を確認した。`Global.*` と `AppHost` は再集計していない（テスト実行で確認する）。

---

## 1. 引き継ぎ台帳の項目別の現状

### 1.1 BackingStore の旧保存系メソッド
- 現状: `BackingStore.cs` は 6,952 行。呼び出し元（本体・テスト・内部）が 0 件のメソッドは **11 名**（台帳は 10 件。`UpdateDS4CLightbarMacro` が加わった。呼び出し側の `Global` ラッパーが Wave 4 で消えたため）:
  - `SaveOld`、`PostProcessLoad`、`UsedSavedProfileString`、`RebuildProfileActionDicts`
  - `GetDS4Action`（2 オーバーロード）、`GetDS4Extra`、`GetDS4KeyType`、`GetDS4STrigger`（2 オーバーロード）
  - `GetGyroOutModeString`、`GetGyroOutModeType`（どちらも private）
  - `UpdateDS4CLightbarMacro`
- ほかに、呼び出し元 0 件の定数が 8 件（`DEFAULT_*` 系）。定数は防壁 4 のため、定数の再配置の Step で扱う。
- §2.4 の経緯調査（`git log -S`）の結果:
  - `SaveOld`: アプリ設定（`Profiles.xml`）の旧形式の保存。`105575a7`（2025-11-11）「DTO方式で設定ファイルを読み込み保存する方式に戻した」で、保存が DTO に戻り、呼ばれなくなった。**使うべきでない（重複した並行実装）→ 削除してよい。**
  - `GetDS4Action`／`GetDS4Extra`／`GetDS4KeyType`／`GetDS4STrigger`: 旧式の名前引き。呼び出し側の `Global` ラッパーは Step12 Wave 4 で削除済み。現行は `ControlSettingsGroup` 経由。**使うべきでない → 削除してよい。**
  - `RebuildProfileActionDicts`: `fecb41b6`（2025-11-09）で追加。`576426b8`（2025-11-11）で、呼び出し元の `LoadProfileNew` が消えて孤立した。**削除してよい。**
  - `UsedSavedProfileString`: 上流の例示コード（2022-09 の `c590ca0e`）からの孤立品。条件式が `index < 0 && index > MAX`（常に false）の誤り。**削除してよい。**
  - `GetGyroOutModeString`／`GetGyroOutModeType`: private で内部の呼び出しもコメントアウト済み。**削除してよい。**
  - `UpdateDS4CLightbarMacro`: `c4658507`（2024-09、上流）。呼び出し元なし。**削除してよい**（レガシーのライトバーマクロの書き込み。ただし機能廃止の例外に当たるかは、削除前にユーザーへ確認）。
  - **`PostProcessLoad` だけは「使うべきもの」**。理由は §2.1 の不具合 B。削除せず、読込の経路へ接続する案を推奨（ユーザーの承認が要る）。

### 1.2 Global の生存する実体（分類 D の 144 名）
- 現状: `Global.cs` は 2,396 行、**232 名**。
- 外部（本体の他ファイル）の呼び出し元が 0 件の名前は 34 名。ただし、ほとんどは `Global` 内の別メンバーから使われる内部部品で、真の孤立は 1 名だけ（連鎖を含めて削除可）。ここから新しい削除は出ない。
- `Services/*` から参照される名前は 156 名（台帳の 144 は、他からも参照される名前を除いた数）。推定の内訳:
  - 純粋な転送（`m_Config`＝`BackingStore` への委譲）: 約 90 名。`AppSettingsService`（185 参照）が大半。
  - `BackingStore` への転送＋少しの処理: 約 15 名。
  - `Global` 自身が実体（状態または処理）を持つ: 約 51 名。例:
    - 環境・パス: `exedirpath`、`appdatapath`、`exelocation`、`exeversion`、`AdminNeeded`、`IsAdministrator`、`IsWin8OrGreater`／`IsWin10OrGreater`、ViGEm／HidHide／FakerInput の情報（`Refresh…`、`Is…Installed`）。
    - 出力: `outputKBMHandler`、`outDevTypeTemp`、`activeOutDevType`、`InitOutputKBMHandler`／`InitOutputKBMMapping`。
    - プロファイル適用: `ApplyProfile`（35 行）、`ApplyProfileToSlot`（30 行）、`CreateStdActions`、`CreateAutoProfiles`、`defaultButtonMapping`、`reverseX360ButtonMapping`、`SaveDefault`、`SetCulture`、`RefreshActionAlias`。
    - イベント: `BatteryChanged`、`SelectedProfileChanged`、`ProfileChangedNotification`、`UDPServerSmoothing…Changed`、`OnDeviceSerialChange`。
- **構造上の事実**: `BackingStore` の唯一の実体は `Global.m_Config`（`protected static BackingStore m_Config = new BackingStore()`、`Global.store` で公開）。DI には登録されておらず、`ServiceRegistration` が `Global.store` を渡す形で 3 つのサービスに配っている。`AppSettingsService` などは、`Global` 経由で間接的に使う。つまり「`Global` の実体の移管」は、「`BackingStore` を DI の単一インスタンスにする」ことが前提になる。

### 1.3 Mapping の instance 化
- 現状: `Mapping.cs` は 8,196 行。クラスは `public class Mapping`（static クラスではない）だが、メンバーのほぼすべてが static（8 空白インデントの宣言で、static のフィールド系約 78、メソッド系約 83）。
- 束縛の現状:
  - 静的 `profileSettings`（行 79–81）: `AppHost.GetService<IProfileSettingsService>() ?? Global.ProfileSettingsServiceInstance`。`Mapping.cs` 内の使用は 62 箇所（P2 方針の結果）。
  - 静的 `profileApplication`（行 82–83）: `AppHost.GetService<IProfileApplicationService>()`。
  - **互換 `AppHost` の唯一の利用者**: 行 86 の `VirtualKBM`（`AppHost.GetService<IVirtualKBM>() ?? Global.outputKBMHandler`）。ほかの 5 件は `DS4WinWPF.AppHost`（行 80、83、5077、5913、6477）。
  - `ServiceProviderHolder.Provider`（Service Locator）が 5 箇所（行 220、256、309、1282、1361）。`Program.rootHub` が 1 箇所（行 6481）。
  - `Global.*` は 53 件（うち定数 50: `MAX_DS4_CONTROLLER_COUNT` 24、`Clamp` 23、`TEST_PROFILE_ITEM_COUNT` 3。残りは `ApplyProfile`、`outputKBMHandler`、`ProfileSettingsServiceInstance` 各 1）。
- 外部の利用者（`Mapping.<名前>` の静的参照）:
  - `ControlService`: 約 45 参照（`getBool*Mapping`、`MapCustom`、`Commit`、`SetCurveAndDeadzone`、`flickMappingData`、`deltaAccelProcessors` ほか）。
  - `Mouse`（約 13）、`SyntheticDispatcher`（約 15。`deviceState`／`globalState`／`SyntheticState`）、`MouseCursor`（4）、`Actions/*`（4）、`RecordBoxViewModel`（2）、`ControllerReadingsControl`、`ProfileSettingsViewModel`（定数）、`Global`（2）。
  - **下位層から上位層への逆参照**: `BackingStore.cs` が `Mapping.globalState`／`deviceState`／`macrodone`／`SyntheticState` を直接触る（約 8 参照）。
  - テストは約 15 ファイル。
- 評価: Phase7 で最大のリスク。ホットパスで、実機確認の不能項目（U-2 スロット 5〜8、U-5 ステアリング、U-6 Joy-Con 結合ジャイロ）がこの経路にかかる。

### 1.4 Pre-Host 12 箇所
- すべて `App.xaml.cs`。内容（行は現行）:
  - `ApplyLanguageSetting`（105–143）: `Global.UseLang`、`Global.SetCulture`（共用ヘルパー）。
  - `Application_Startup` の冒頭（180–188）: `Global.FindConfigLocation()`、`Global.appdatapath`、`Global.LogMaxArchiveFiles`、`Global.LogMinLevel`（ログのローテーションと最小レベルの早期適用）。
  - 315 と 688: `Global.RefreshViGEmBusInfo()`。
  - 669: `Global.appdatapath = null`（`AttemptSave`）。
  - `-driverinstall` 分岐（688–699）: `FindConfigLocation`、`Global.Load()`、`UseLang`、`UseCurrentTheme`。
- `Global.Load()`（`Global.cs:2043`）は `DS4WinWPF.AppHost.GetService<IAppSettingsService>()` を呼ぶため、この分岐に来ると、ホストが暗黙に作られる（K7-1 の残存経路。通常の起動では、明示的な `CreateHost(config, parser)`［App.xaml.cs:330］が先。起動ログ `Host creation path: explicit …` で確認済み）。

### 1.5 Program.rootHub（22 件）と Service Locator
- `Program.rootHub` 22 件の内訳（再集計で一致を確認）:
  - `App.xaml.cs` 12: `LoggerHolder` の生成（410）、`LogDebug`／`CheckHidHidePresence`／`LoadPermanentSlotsConfig`（534–541）、`PrepareAbort`（583、596）、`AppHost` からの代入（809–810）、`CleanShutdown`（1070–1077）。
  - `Global.cs` 6: `fallbackProfileSettingsService` のラムダ（223）、`ApplyProfile` の既定引数（1714）、コントローラー設定の保存・読込（2220–2233）。
  - `ServiceRegistration.cs` 1（123: `Program.rootHub ?? new ControlService(...)`）、`Mapping.cs` 1（6481）、`ProfileEditor.xaml.cs` 1（314。`AppHost` で解決した値のフォールバック）、`DS4SixAxis.cs` 1（51。同上）。
- 構造: `ControlService` は DI のシングルトンだが、`App` が解決の結果を `Program.rootHub` に代入し、ほかの層がそれを読む。`ServiceRegistration` のファクトリーも `rootHub` を先に見る（循環に近い）。
- **台帳に無かった発見（F-A）**: もう一つの Service Locator として `ServiceProviderHolder.Provider` が、`ActionManager.cs` 13、`Mapping.cs` 5、`ActionFactory`／`DefaultKeyButtonActionControllerFactory`／`KeyActionAdapter` 計 4、`App.xaml.cs` 1（計約 23）に使われている。ラチェットテストと台帳の対象外。
- `AppHost.GetService` の呼び出し元（本体、約 40）は `MainWindow.xaml.cs` が 17、`Mapping.cs` 6、`App.xaml.cs` 5、`ProfileEditor.xaml.cs` 4、`Global.cs` 4 ほか。`MainWindow` のコンストラクターが、Composition Root 隣接の画面として、サービスを 14 件解決している。

### 1.6 DI インターフェースの 0 件メンバー
- 走査（`iface.py`。インターフェースの宣言ファイルと実装クラスのファイルを除く、本体の参照が 0 件）: 50 インターフェース、534 メンバーのうち **61 件**。台帳の 52 件とのずれは、台帳の集計時点が Wave 4〜6 より前だったことと、走査範囲（`Actions/` の抽象を含む）の差。
- Phase8 に属するもの（`Actions/` の未配線の抽象。Phase6-Step9b から Phase8 へ申し送り済み）: 11 件（`IActionBinding` 3、`IKeyController` 3、`IActionRegistry` 1、`IControllerRegistry` 1、`IInputAction` 1、`IMacroPlayer` 1、`ITrigger` 1）。→ Phase7 の対象から外し、Phase8 の台帳へ移す。
- Phase7 の対象（約 50 件）:
  - `IProfileSettingsService` 21、`IDeviceStateService` 6、`IOutputSlotService` 6、`IProfileRepository` 4、`IDs4DeviceRegistry` 3、`ISpecialActionRepository` 3、`INotificationService` 2、ほか（`IControllerRegistry`、`IEnvironmentService`、`IOutputHandler`、`IPathService`、`IUdpServerService`、`IVirtualKBM` 各 1）。
  - うち、テストからだけ参照されるもの（本体の呼び出しなし）が多い（例: `IProfileSettingsService.ProfileSettingChanged`＋`WireSubSettingsEvents` はテスト 10 参照、`IDeviceStateService.IsDeviceConnected` 4）。「契約だけ作って切り替え未実施」の疑いが強いものと、孤立実装のものを、個別に仕分ける必要がある（§2.4）。

### 1.7 K2（GetProfileActionIndexOf の戻り値）
- 現状: `ProfileActionProvider.GetProfileActionIndexOf` は `int index = -1; dict.TryGetValue(name, out index);`。`TryGetValue` は未検出時に `out` を 0 で上書きするため、**未検出でも 0 を返す**（移行前の `Global.GetProfileActionIndexOf` と同一。回帰ではない）。
- 呼び出し元は `Mapping.cs` の 2 箇所だけ:
  - 4819: `index < 0 || index >= total` で読み飛ばし → 未検出の 0 は通過するが、直後の `action == null` の判定で処理される。実害は出ていない。
  - 5192: 戻り値 `indexNext` は**使われていない**（未使用変数）。
- 修正は 1 行（`TryGetValue` の戻り値を使い、未検出は -1）。テスト `Phase6Step3ContractExtensionTests.GetProfileActionIndexOf_SharesStateWithGlobal` の期待値（現状は 0 を固定）も直す。挙動への影響は、事実上なし（上記のとおり別の判定で守られている）。

### 1.8 「旧形式だけに統一」を広げる範囲
- 現状（読み書きの方式）:
  - プロファイル（`Profiles\*.xml`）: 旧形式のみ（DTO は Wave 6 で削除済み）。
  - アプリ設定（`Profiles.xml`）: **保存は DTO のみ**（`BackingStore.Save`）。**読込は DTO を先に試し、失敗したら旧形式の DOM 読み込み**（約 410 行。`BackingStore.Load`）。旧形式の保存 `SaveOld` は孤立（§1.1）。
  - `Actions.xml`、`LinkedProfiles.xml`、`Auto Profiles.xml`、`OutputSlots.xml`: **読み書きとも DTO のみ**（旧形式の実装は無い）。
- 経緯: `142c24c8`（2025-11-11）で一度 3.9.9 互換に寄せ、同日の `105575a7` でアプリ設定は DTO に戻した。つまり、アプリ設定は「最新の決定は DTO」で、旧形式が正本なのはプロファイルだけ。
- 二重実装が残るのは、アプリ設定の読込（DTO＋旧形式のフォールバック）だけ。ほかの 4 ファイルは単一実装で、統一の対象となる二重管理が存在しない。

### 1.9 AppHost.CreationNote と Host creation path ログ
- 現状: `AppHost.CreationNote`（`AppHost.cs`）と、`App.xaml.cs:332` の Info ログ。ユーザー決定により残す（2026-10-17）。
- 付随して見つけた整理候補（F-C）: `AppHost` に `CreateHost()`／`CreateHost(config, parser)`／`CreateHost(string[])` の 3 つのオーバーロードがあり、本体の呼び出しは `CreateHost(config, parser)` の 1 件だけ（引数の `configuration` は使われない）。`CreateHost(string[])` と `GetService(Type)` は、本体・テストとも呼び出し元 0 件。`CreateHost()` はテスト用（約 60 箇所）。
- 扱い: 起動シーケンスの整理（Step4）の最後に、残すか判断する（台帳の記載どおり）。

### 1.10 DS4WinWPF.AppHost.Dispose()
- 現状: 呼び出し元 **0 件**（互換ラッパーの `Dispose` は Wave 5 で削除済み）。`CleanShutdown`（App.xaml.cs:1065–）は、`rootHub` の停止、設定の保存、コントローラーレジストリの破棄を行うが、**ホスト（DI コンテナ）を破棄しない**。
- 破棄を足す場合の注意: 防壁6（ドライバ破棄順序）。`ControlService.Stop`／`ShutDown` の後、ログのフラッシュの前に置く必要がある。実機確認（終了時のハング・ViGEm の残留）が要る。

---

## 2. 調査で見つけた既存の課題（修正前に承認が必要）

### 2.1 不具合 B: アプリ設定の DTO 読込で後処理が行われない
- 事実:
  - `BackingStore.Load()` の DTO 経路は、`dto.MapTo(this)` の直後に「`// Post processing same as legacy path`」とコメントして `return` する。しかし後処理は実行されない。
  - 結果として、DTO で読み込んだとき、次が欠落する:
    - ① `fakeExeFileName`（`custom_exe_name.txt` の読み込み）。この読み込みは `Load()` の後半（DTO 経路が `return` した後）にしかない。
    - ② `lastVersionCheckedNum`（`PostProcessLoad` が `lastVersionChecked` から計算する）。
    - ③ OSC／UDP のポート番号の範囲の丸め（1024〜65535）、UDP のスムージング値の範囲の丸め。
    - ④ プロファイル名に "distance" を含む場合の `distanceProfiles` の設定。
- 影響の推定（要・実機確認）:
  - ①: 起動のたびに `fakeExeFileName` が空になる。`Save()` は「`fakeExeFileName` が空でも、ファイルが存在すれば書く」ため、**保存のたびに、既存の偽の exe 名ファイルが空で上書きされる**可能性がある。
  - ②: `LastVersionCheckedNum` が 0 のままになり、「スキップしたバージョン」が再起動後に効かない可能性がある（`MainWindow.xaml.cs:324` が使用。U-7 に関係）。
- **回帰か**: 回帰ではない。移行の直前（`391df580`、2025-12-20）の `ScpUtil.cs` も同一の構造で（`PostProcessLoad` は定義のみ、DTO 経路は `PrepareAbsMonitorBounds` だけ呼んで `return`）、`105575a7`（2025-11-11）から続く不具合。
- 修正案: 後処理（`PostProcessLoad` と偽の exe 名の読み込み）を、DTO 経路の終わりと旧形式の経路の両方で呼ぶ。範囲は小さいが、設定の読込に触るため、特性化テスト（DTO で読んだ後に各値が復元されること）を先に書く。
- 実機確認が要る項目（`Phase6-Step11-RealDevice-Verification-Checklist.md` §11 の U-10 候補。承認後に追記する）: 偽の exe 名ファイルがある状態でのアプリ起動→終了後の内容、スキップしたバージョンの再起動後の保持。

### 2.2 K2（§1.7）
- 修正は 1 行。回帰ではない（移行前と同一）。

### 2.3 Service Locator の未追跡（F-A、§1.5）
- 台帳・ラチェットの対象外。追加を提案する（D6）。

---

## 3. Step（Wave）の分割案

### 3.1 前提となる依存関係
- 起動（Pre-Host）は `Global` の静的な実体（`appdatapath`、`FindConfigLocation`、ログ設定）に依存する。`Global` の環境・パスの実体をサービスへ移すには、コンテナ構築の前に、それらを作る**ブートストラップ**が要る。
- `BackingStore` の DI 登録（単一インスタンス化）が、`Global` の転送の削除（約 90 名）の前提。
- `Mapping` の instance 化の前提: コンストラクターで受けるサービスが、`Global`／`rootHub`／Service Locator を使わずに解決できること。さもないと、一時的な静的アクセスを書いて、あとで書き直すことになる（二度手間）。
- `Mapping` の instance 化は、Phase8（Controls／SpecialActions の統合ディスパッチ）の前提でもある。
- 出力スロットの所有権（`outDevTypeTemp`／`activeOutDevType`）は、`Mapping` から独立している（`Mapping.cs` は使っていない）。

### 3.2 案 A（推奨）: 外側から内側へ
- **Step1: 整理と台帳の確定**（低リスク）
  - W1: `BackingStore` の孤立メソッド 10 名の削除（`PostProcessLoad` を除く）。
  - W2: K2 の是正と未使用変数の削除。
  - W3: 不具合 B の是正（D5 の承認後）。特性化テストが先。
  - W4: `ServiceProviderHolder` をラチェット・台帳に追加（ガードテスト）。
- **Step2: DI インターフェースの 0 件メンバー（約 50 件）の個別判断**（§2.4）。経緯の調査→判断の記録→承認→削除または接続の計画。接続すると決めた分の実装は、該当する後続 Step に割り当てる。
- **Step3: BackingStore の DI 化と Global の転送の削除**
  - 3a: `BackingStore` を DI のシングルトンとして登録（実体は `Global.store` と同一）。`AppSettingsService` ほかのコンストラクターへ注入。
  - 3b: 純粋な転送の約 90 名を、サービスが `BackingStore` を直接使うようにして、`Global` から削除（サービス単位の Wave）。
  - 3c: イベント（`BatteryChanged`、`SelectedProfileChanged`、`ProfileChangedNotification` ほか）とプロファイル適用・プリセットの処理（`ApplyProfile` など）を、サービス側へ移管。
- **Step4: 起動・終了シーケンスの近代化**
  - ブートストラップ（パス・設定の場所・ログ設定を、コンテナ構築の前に作り、同じインスタンスをコンテナへ登録）。Pre-Host の 12 箇所と共用ヘルパーを整理。
  - 環境・パスの実体（`exedirpath`、`appdatapath`、ViGEm／HidHide の情報ほか）を `PathService`／`EnvironmentService` へ移管。
  - `AppHost` のオーバーロード整理（F-C）、`Dispose` の呼び出し（§1.10）、`CreationNote` の扱いの判断、`Global.Load()` の暗黙構築の経路の解消。
- **Step5: Program.rootHub と Service Locator の削除**
  - `rootHub` 22 件を、コンストラクター注入または `App` の保持に置き換える。`ServiceRegistration` の `rootHub ?? new ControlService` を解消。
  - `AppHost.GetService`（`MainWindow` 17、`ProfileEditor` 4 ほか）と `ServiceProviderHolder`（`Mapping` 5、`App` 1。`Actions/` の約 17 件は Phase8 へ）を整理。
- **Step6: 出力スロットの所有権の移動**（`outDevTypeTemp`／`activeOutDevType` → `OutputSlotService`。約 45 参照）。`Step5` と入れ替え可能。
- **Step7〜9: Mapping の instance 化**（3 分割）
  - Step7: 特性化テスト（入力→出力の固定）と、静的な状態（`deviceState`、`globalState`、`macrodone` ほか）の保持クラスへの切り出し。`BackingStore`→`Mapping` の逆参照の解消。
  - Step8: `Mapping` を instance 化（DI のシングルトン＋抽象の窓口）。呼び出し元（`ControlService`、`Mouse`、`SyntheticDispatcher`、`Actions/*`）の切り替え。静的な `profileSettings` 束縛を解消。
  - Step9: 互換 `AppHost`（`DI/Compat/AppHost.cs`）の削除、`Mapping.cs` の `Global` 参照（定数・`Clamp`）の再配置、K3（マクロの二重ガード）の再設計の要否を判断。
- **Step10: 仕上げ**
  - `Global` の残り（定数・純粋計算）の再配置、`BackingStore` の永続化の切り出し（`LegacyProfileXmlSerializer`、アプリ設定の読み書き）とゴールデンテスト、保存形式の方針の反映（D4）、モデル図の更新、Phase7 の総合検証と完了報告。
  - 名前空間＝フォルダ（最終目標）は、`BackingStore` の分解の範囲（D2）とあわせて、Phase7 に入れるか、別フェーズにするかを判断。

### 3.3 案 B: Mapping 先行
- 利点: Phase8 に早く着手できる。`Mapping.cs` の `Global` 定数参照が先に片づく。
- 欠点: `Mapping` のコンストラクターが受けるべきサービスが、まだ `Global`／`rootHub`／Service Locator 経由なので、一時的な静的アクセスを書いて、あとで書き直す二度手間になる。ホットパスの変更が、ほかの変更と同時期に重なり、不能項目（U-2、U-5、U-6）の確認が難しいまま、原因の切り分けも難しくなる。

### 3.4 案 C: Phase8 に必要な最小限だけ
- 内容: Step1・Step7〜9 だけを行い、`Global`／`BackingStore`／起動／`rootHub` は Phase7 の後へ持ち越す。
- 利点: 期間が短い。欠点: `Global`（2,396 行）、`BackingStore`（6,952 行）、約 22＋40＋23 件の静的アクセスが残り、Phase6 の「分類済みの残存」が恒久化する。Pure DI 化の目的（§3.1）を満たさない。

### 3.5 リスク（案 A）
- Step1: W3 は設定ファイルの読込に触れる。特性化テストと、実機（またはファイル）での確認が要る。
- Step2: 接続すると決めた分の実装は、別 Step の範囲に混ぜない（§2.4 の手順 3）。
- Step3: 変更のファイル数が多い（`AppSettingsService` だけで 185 参照）。サービス単位の Wave に分け、各 Wave でビルド・テストを確認する。ラチェットの基準値の引き継ぎを厳密に行う。
- Step4: 起動順序の変更。ログ・設定の場所・言語の初期化の順序を崩すと、起動不能になる。`-driverinstall` 分岐も確認が要る。ホストの `Dispose` は、終了時のハングや ViGEm の残留を実機で確認する。
- Step5: `rootHub` を介して暗黙に共有している状態の順序（代入のタイミング）に注意。
- Step6: 実行時状態の静的配列。UI 3 箇所＋`ControlService` の参照キャッシュ。マルチスレッドの読み取りに注意。スロット 5〜8（U-2）は確認不能のまま。
- Step7〜9: 最大のリスク。ホットパス（毎レポート実行）、ゼロアロケーションの維持（`MappingHotPathAllocationTests`）、マクロの別スレッド、確認不能の U-2・U-5・U-6 が同じ経路にかかる。**Step7 で、入力→出力の特性化テストを先に整備することを必須とする**。
- 並行実施: Step1 と Step2 はファイルが重ならない部分もあるが、テストの書き換えが重なるため、順番に行うことを推奨する。

---

## 4. 実機確認の候補（承認後にチェックリスト §11 へ追記）

- U-10（候補）: 偽の exe 名ファイルと「スキップしたバージョン」の再起動後の保持（不具合 B。機材は不要だが、実アプリで確認する）。
- Step4 の後: 終了時のホスト破棄（ハング・ViGEm の残留）、`-driverinstall` 分岐の起動。
- Step6 の後: スロットの付け外し（U-2 との関係で、5〜8 は確認不能のまま）。
- Step7〜9 の後: 通常入力・スティックのカーブ・タッチパッド・ジャイロ・マクロ・スペシャルアクション（U-5、U-6 は確認不能のまま）。

---

## 5. ご判断をお願いしたい事項（選択肢と推奨）

- **D1: 全体の順序** — 案 A（外側から内側へ）／案 B（Mapping 先行）／案 C（最小限）。**推奨: 案 A。**
- **D2: `BackingStore` の扱い** — (i) Phase7 では「DI 化（単一インスタンス）＋永続化の切り出し」までとし、フィールド群のドメインエンティティへの分解は、Step10 の時点で範囲を再評価する／(ii) Phase7 で全面的に分解する。**推奨: (i)。**（6,952 行の分解は、`Mapping` の instance 化と同規模で、同じ Phase に入れると検証が破綻する）
- **D3: 起動シーケンス** — S1（コンテナ構築の前にブートストラップを作り、同一インスタンスを登録する）／S2（Pre-Host の静的呼び出しを恒久的な例外として残す）。**推奨: S1（Step4）。** 先に Step3 を済ませ、Step4 で範囲を確定する。
- **D4: 保存形式の統一の範囲** — F1（アプリ設定は DTO のまま。旧形式の読込フォールバックは読み取り専用の互換として切り出して残す。ほかの 4 ファイルも DTO のまま。プロファイルは旧形式。往復のゴールデンテストで固定）／F2（5 ファイルすべてを旧形式の DOM 方式に書き換える）。**推奨: F1。**（二重実装が残るのはアプリ設定の読込だけ。書き換えは機能の利点がなく、リスクだけが増える）
- **D5: 既存の課題の修正** — 不具合 B（§2.1）と K2（§1.7）を Step1 で修正してよいか。**推奨: 修正する**（B は特性化テストを先に書く。回帰ではなく、移行前から続く不具合）。`UpdateDS4CLightbarMacro` の削除（ライトバーのマクロ書き込みの孤立実装）の可否も合わせて確認したい。
- **D6: Service Locator の追跡** — `ServiceProviderHolder` をラチェット・台帳へ追加する（`Actions/` の約 17 件は Phase8 の持ち越し）。**推奨: 追加する（Step1 W4）。**
- **D7: Step1 の計画書へ進む** — D1〜D6 の承認後、`Phase7-Step1-Plan.md` を作成する（案 A の Step1 の内容に従う）。

---

## 6. 付録: 数値の一覧（2026-10-17、現行コード）

- `Global.cs`: 2,396 行、232 名。`BackingStore.cs`: 6,952 行。`Mapping.cs`: 8,196 行。
- `Program.rootHub`: 22（`App` 12、`Global` 6、`ServiceRegistration`・`Mapping`・`ProfileEditor`・`DS4SixAxis` 各 1）。
- `ServiceProviderHolder.Provider`: 約 23（`ActionManager` 13、`Mapping` 5、`Actions/` の工場 4、`App` 1）。
- DI インターフェースの 0 件メンバー: 61（Phase8 行き 11、Phase7 約 50）。
- `BackingStore` の孤立メソッド: 11 名（`PostProcessLoad` は接続の案）、孤立定数 8 件。
