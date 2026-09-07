# Phase 5 アーキテクチャ・コード品質 見逃し注意点（Watchpoints）調査報告書

## 1. 調査の背景と目的
DS4Windows の DI 移行作業において、Phase 5-Step 13-8（UI層 ViewModel および MainWindow の DI 結合）まで完了した。
本報告書は、Phase 5 の最終マイルストーン（Step 13-9 テスト拡充、Step 14 総合テスト、Step 15 Shim削除判断、実機CP4検証）へ進むにあたり、潜在的な不整合・リグレッション要因を未然に排除するため、以下の **3大見逃し注意点（Watchpoints）** について現存調査を行い、解決策の比較・推奨案を策定したものである。

1. **Watchpoint 1**: `MainWindow.xaml.cs` の残存静的参照（理由付きで対象外とした約20件）の再点検
2. **Watchpoint 2**: UI スレッド安全性（Dispatcher）とイベント購読解除（メモリリーク・二重発火防止）の現存調査
3. **Watchpoint 3**: DI サービスと旧静的クラスの「二重インスタンス・状態解離」の残存チェック

---

## 2. Watchpoint 1: `MainWindow.xaml.cs` 残存静的参照の現存調査と評価

### 2.1 現存調査結果
`MainWindow.xaml.cs` では Step 13-7 / 13-8 により約 50 箇所の静的アクセスが DI サービス呼び出しへ置換された。残存した約 20 件の呼び出しを分類・精査した結果は以下の通りである。

| 分類 | 参照対象の例 | 件数規模 | 性質 | リスク判定 |
| :--- | :--- | :---: | :--- | :---: |
| **A. 定数・型定義・固定値** | `Global.blMacro`, `Global.CONFIG_VERSION`, `Global.STEAM_APPROVED_APP_ID` | 約 7 件 | 純粋な不変値・識別子参照 | **安全（低）** |
| **B. UI専用ヘルパー/外観** | `Global.RefreshTheme()`, `Global.GetRealThemePath()`, `WindowPlacementHelper.*` | 約 6 件 | WPF ウィンドウ配置やテーマ適用 | **安全（低）** |
| **C. ユーティリティ/存在判定** | `Global.FindValidProfile(name)`, `Global.ProfileExists(name)` | 約 4 件 | プロファイル名の重複チェック・存在確認 | **注意（中）** |
| **D. レガシー互換のログ/設定** | `Global.SaveSettings()` 等のフォールバック呼出 | 約 3 件 | 例外時等の安全策 | **注意（中）** |

### 2.2 潜在的リスクの分析
* 分類 C（`FindValidProfile` 等）は、実体として `IProfileRepository` が保持しているプロファイル一覧とディスク上のプロファイル XML の両方に関連する。現状では `Global.FindValidProfile` は内部でパスやキャッシュを参照しているため、プロファイル作成・複製（`DupProfBtn_Click` / `NewProfBtn_Click`）時に、リポジトリ側の管理状態と乖離する可能性がゼロではない。
* 分類 D（例外ハンドラ内等のレガシー呼出）は、通常系では DI サービス（`IAppSettingsService.SaveSettings()`）が呼ばれるため致命的ではないが、残存したまま放置するとテスト網から漏れる恐れがある。

### 2.3 解決策の選択肢

* **案 1-A: 現状維持（Phase 5 完了後に Phase 6 / Step 15 で整理）**
  * **メリット**: 現在グリーンな MainWindow の安定性を壊さず、ビルド破損リスクが最も低い。
  * **デメリット**: 分類 C の存在判定が DI サービス（`IProfileRepository`）を経由しない状態が残る。
* **案 1-B: 分類 C（プロファイル存在判定）のみ `IProfileRepository` へ移行し、定数・UIテーマは維持（推奨）**
  * **メリット**: 業務ロジック（プロファイル検証）を DI サービスへ一本化でき、状態乖離リスクを完全に解消。かつ UI テーマ等の安全な静的参照には手を加えないためピンポイント安全。
  * **デメリット**: `IProfileRepository` に `ProfileExists(string name)` / `FindValidProfileName(string baseName)` 相当のメソッドが必要。
* **案 1-C: 全静的参照（テーマ・定数含む）を `IThemeService` 等を作って完全排除**
  * **メリット**: 純粋アーキテクチャとしての完成度は極大。
  * **デメリット**: 過剰設計（Over-engineering）。UIテーマ等の変更で大規模な工数増とリグレッションのリスクが発生。

### 2.4 推奨案
**【推奨案 1-B】**
* 分類 A（定数）および分類 B（テーマ/ウィンドウ位置）は「UI表現層固有のユーティリティ」として MainWindow 内での維持を正式に許容する。
* 分類 C のプロファイル関連判定のみ、`IProfileRepository` のメソッドへ切り替え、ドメインロジックの漏れを塞ぐ。

---

## 3. Watchpoint 2: UI スレッド安全性とイベント購読解除の現存調査と評価

### 3.1 現存調査結果
DS4Windows では、バックグラウンドスレッド（`ControlService` のワーカースレッド、UDPサーバー受信スレッド、`AutoProfileChecker` のタイマースレッド等）が自律動作している。

1. **スレッド安全性（Cross-thread UI 操作）**:
   * `AutoProfileService.AutoProfileSystemChange` や `Ds4DeviceRegistryAdapter` から発火するイベントは**非UIスレッド（ワーカースレッド）から発火**される。
   * Step 13 で DI 化した各 ViewModel（`AutoProfilesViewModel`, `ControllerListViewModel` 等）が、バックグラウンド発火のイベントをそのまま購読し `ObservableCollection` を変更した場合、WPF の `InvalidOperationException`（クロススレッドアクセス違反）でクラッシュするリスクがある。
2. **イベント購読解除（Unsubscribe / メモリリーク）**:
   * `IAutoProfileService`、`IDs4DeviceRegistry`、`IOutputSlotService`、`IDeviceStateService` などの DI サービスはすべて **Singleton（単一インスタンス）** として登録されている。
   * これに対し、WPF の Window（`MainWindow` 等）や再生成される ViewModel が `+=` で購読し、`-=` による解除を行わない場合、Singleton サービスが Window/ViewModel への強参照を保持し続け、**メモリリーク** および **二重発火バグ** を引き起こす。

### 3.2 潜在的リスクの分析
* 過去の Step 2-1〜2-7 でも、バックグラウンドタイマーと UI スレッドの競合がバグ（キーリピート不整合など）を引き起こした経緯がある。
* 特にコントローラー着脱時や自動プロファイル検知時において、Dispatcher 経由でのコレクション更新が徹底されていない箇所が存在すると、実機動作時に不規則なクラッシュを誘発する。

### 3.3 解決策の選択肢

* **案 2-A: ViewModel のイベントハンドラ内での Dispatcher 委譲と、Window/VM の明示的 Unsubscribe 徹底（推奨）**
  * **メリット**: WPF 標準の最も確実で素直な実装。追加ライブラリ不要。スレッド安全とメモリ解放の両方が保証される。
  * **デメリット**: 各 ViewModel / MainWindow の `Closed` / `Unloaded` 処理で明示的に `-=` を記述する確認作業が発生する。
* **案 2-B: `WeakEventManager`（弱参照イベントパターン）の全面採用**
  * **メリット**: `-=` を明示的に書かなくてもガベージコレクションが可能。
  * **デメリット**: コードが冗長化し、イベント購読の追跡が困難。クロススレッド問題そのものは解決しない。
* **案 2-C: DI サービス側で UI スレッドへ同期してイベントを発火させる（`SynchronizationContext`）**
  * **メリット**: 呼び出し側（ViewModel/View）がスレッドを意識しなくて済む。
  * **デメリット**: サービス層が「UIの都合」に汚染され、全体4層モデルの原則（§0.2: サービス層はUI非依存）に違反する。テスト時にモックが必要になり脆弱化する。

### 3.4 推奨案
**【推奨案 2-A】**
* 全体4層モデルを守るため、**DIサービス側は純粋なバックグラウンドスレッドでイベントを発火**させる。
* UI層（ViewModel および MainWindow）側において：
  1. イベント受信時に `Application.Current.Dispatcher.Invoke / BeginInvoke` を用いて UI スレッドへ切り替えてからコレクション／プロパティを更新する。
  2. `MainWindow_Closed` 等の破棄タイミングで、Singleton サービスに対するイベント購読（`+=`）を確実に解除（`-=`）する。

---

## 4. Watchpoint 3: DI サービスと旧静的クラスの「二重インスタンス・状態解離」の現存調査と評価

### 4.1 現存調査結果
Step 13-4 において、`AutoProfilesViewModel` が「DIサービス `IAutoProfileService`」と「旧静的ホルダー `AutoProfileHolder.defaultAutoProfileHolder`」の2つの別インスタンスを混在参照していたため、状態解離バグが発生した。

この同型問題が他のサービス・ドメインに残存していないかを調査した結果は以下の通りである。

| サービス・ドメイン | DI 実装クラス | 旧静的アクセス元 | 現存調査結果と状態 |
| :--- | :--- | :--- | :--- |
| **AutoProfiles** | `AutoProfileService` | `AutoProfileHolder` | **Step 13-4 で是正済**。DIサービスを唯一のインスタンスとして一本化。 |
| **Profiles** | `ProfileRepository` | `Global.ProfilePath`, `Global.ProfileListHolder` | **要注意**。`Global.ProfileListHolder` は `MainWindow` 等でまだ参照されている。 |
| **AppSettings** | `AppSettingsService` | `Global.Instance` の一部フィールド | **概ね良好**。主要な設定読み書きは `IAppSettingsService` に委譲されている。 |
| **SpecialActions** | `SpecialActionRepository` | `Global.Actions` | **良好**。Step 7 で `SpecialActionRepository` 内部リストとの同期委譲構造を構築済。 |
| **OutputSlots** | `OutputSlotStore` / `Service` | `OutputSlotPersist`, `Global.OutSlotSettings` | **良好**。Step 11 で DTO 読み書きがストア側に一本化済。 |
| **Devices** | `Ds4DeviceRegistryAdapter` | `ControlService.devices`, `Program.rootHub` | **Step 13-8 で完全削除完了**。実体 `ControlService` 参照へ統一。 |

### 4.2 潜在的リスクの分析
* **`ProfileListHolder` の解離リスク**:
  `MainWindow` 内の `ProfileListHolder` はプロファイル一覧（UIバインド用）を保持している。プロファイル追加・削除・名称変更時に、`IProfileRepository` 側のコレクションと `ProfileListHolder` が二重管理されている場合、画面上のリストと実際のデータに不一致が生じる恐れがある。

### 4.3 解決策の選択肢

* **案 3-A: 旧静的クラス・プロパティを「DI サービスの透過的ゲッター/セッター」へシム化（推奨）**
  * **メリット**: 後方互換性（フォールバック原則 §2.1）を維持したまま、内部実体（Single Source of Truth: SSOT）を DI サービスインスタンスへ完全一本化できる。
  * **デメリット**: 静的クラス側に DI コンテナからのインスタンス参照を渡す必要がある。
* **案 3-B: 旧静的プロパティを即時削除し、参照箇所をすべて DI サービスへ書き換える**
  * **メリット**: 中間シムが消え、最もクリーンになる。
  * **デメリット**: 未移行のサブウィンドウ等で広範なエラーが発生し、Phase 5 のスコープが肥大化する。
* **案 3-C: 旧静的プロパティに `[Obsolete]` 属性を付与し、残存呼び出し箇所を警告として可視化し順次置換**
  * **メリット**: どこで旧静的クラスが使われているかがコンパイル時に一目瞭然となり、漏れを機械的に撲滅できる。
  * **デメリット**: ビルド時の警告数が増加する。

### 4.4 推奨案
**【推奨案 3-A ＋ 3-C の併用】**
1. `ProfileListHolder` などの旧静的ホルダーは、実体を自前で持たず、**DIサービス（`IProfileRepository`）のプロパティをそのまま返す透過的シム** に統一する（SSOT の保証）。
2. 旧静的アクセサに `[Obsolete]` を付与し、Step 13 以降のコードで新規利用されることを防止する。

---

## 5. 総合結論と今後の推奨ロードマップ

今回の現存調査により、全体として DI 移行のコア設計は極めて健全に成立していることが確認された。
発見された注意点に対する処置は、**「Step 13-9 〜 Step 15」の既存計画の流れの中に自然に統合して解決可能** である。

* **【直近タスク: Step 13-9】**
  * ViewModel 単体テストの拡充（モック検証）
  * [Watchpoint 2 の確認]: Dispatcher 考慮およびイベント解除ハンドラのテスト
* **【タスク: Step 13-10】**
  * ビルド検証・Phase5-Step13 完了報告書の作成
* **【タスク: Step 14】**
  * Phase 5 総合自動テストの実行
* **【タスク: Step 15 (Shim 棚卸し)】**
  * [Watchpoint 1 の処置]: MainWindow 分類 C（存在判定）の `IProfileRepository` への移譲
  * [Watchpoint 3 の処置]: 残存静的シムの透過委譲化 ＆ `[Obsolete]` 化の実施
* **【最終ゲート: 実機CP4】**
  * 実機を用いた E2E 動作検証（クロススレッド例外なし・状態解離なしの確認）

これにより、手戻りや無駄なスコープ拡大を起こすことなく、安全かつ堅牢に Phase 5 を完了できる。