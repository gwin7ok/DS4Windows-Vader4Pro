# Phase7-Step8 完了報告書: `Mapping` の instance 化と、呼び出し元の切り替え

作成日: 2026-10-17  
対象ブランチ: `For-DI-migration-work`  
計画書: `Phase7-Step8-Plan.md`（Q1〜Q9 はすべて推奨どおり承認）  
状態: 実装完了。W1〜W4 のビルド・テストビルド・テスト実行は成功（ユーザーが各 Wave で実行。W2〜W3 は各 1〜3 回の再ビルド・再テスト）。W5 は文書のみ。実機確認 U-18 は未。

---

## 1. 結果の要約

- `Mapping`（約 7,800 行。メンバーのほぼすべてが static だった）を、DI の Singleton（窓口 `IMapping`、8 メンバー）にした。状態は instance のフィールドになり、サービスは `AppHost`／`ServiceProviderHolder`／`Global` のフォールバックをやめて、コンストラクターで受ける。
- 状態を持たない純粋な関数と定数（`getBool*Mapping` 4 つ、`remainderCutoff`、`compare`、`DS4ControltoInt`、表と定数ほか）は static のまま残した（Q2＝A）。
- 保持クラス 3 つ（`SyntheticInputState`、`MacroRuntimeState`、`StickMotionState`）の `Shared`（暫定の静的な窓口）と、`Mapping` の転送プロパティを削除した。
- キー／ボタンのコントローラーの登録簿を `KeyButtonControllerRegistry` に切り出し、`Mapping` ⇔ `ProfileApplicationService` の循環を解消した。
- 出力 KBM ハンドラの実体を `OutputKBMHandlerAdapter` に移し、`Global.outputKBMHandler`／`outputKBMMapping`／`InitOutputKBMHandler`／`InitOutputKBMMapping` を削除した。
- 合成キーの送出を `ISyntheticDispatcher`（`SyntheticDispatcherService`）にした。
- 数値: `Global.*` 432 → **399**、`AppHost.GetService` 11 → **5**、`ServiceProviderHolder` 22 → **17**（残りは `Actions/` で Phase8）、`Program.rootHub` 0。累計（733 から）−334。
- 計算のロジックは変えていない。既存の不具合の是正はなし。

## 2. Wave ごとの実績

- W1: `Mouse`／`ControlService` が保持クラスをコンストラクターで受ける。`MapClick`／`ResetRuntimeStateForKey` を `SyntheticInputState` へ（`Global.SaveAction` の末尾の呼び出しは `SpecialActionRepository` へ）。呼び出しが 0 件だった `Mapping` の内部メンバー 4 つを削除（Q7＝A）。ビルド・テストは 1 回で成功。
- W2: `IMapping`／`Mapping`／`KeyButtonControllerRegistry`。スクリプトで `static` を 108 メンバーから除去（残すものは名前の一覧で指定）し、差分で本体の変更が束縛の置き換えだけであることを確認した。`ControlService` に `MappingService`。テストの書き換え（直接呼び出し、反射、`static` を含む文字列のマーカー）と、`TestMappingFactory` の新設。再ビルド・再テストは 4 回（テストの名前空間、`StandaloneTests` のコンストラクター、ほか。下の §3）。
- W3: `OutputKBMHandlerAdapter` がハンドラを所有。`OutputKBMHandlerLifecycle` の書き換え。`SyntheticDispatcherService`、静的な橋渡し、`ActionsLoadedRuntimeReset` の instance 化。`VirtualKBMLifecycleTests` ほかの書き換え。再ビルド 1 回（`using` の不足）。
- W4: `Shared` の削除。テストをコンテナ経由に。ガード `Phase7Step8W4NoSharedGuardTests`。
- W5: 台帳 §11、モデル図（01〜04）、チェックリスト U-18、本報告書、`Phase7-Status.md`。

## 3. 計画との差と、調査で判明した事項

- 計画どおりに実装した。差は次のとおり。
  - **`SyntheticDispatcher` は、Actions の利用者のために静的な橋渡しを残した**（Q6＝A）。`Shared`／`Global` と違い、状態もフォールバックも持たず、DI が作った唯一の instance を `Bind` で受ける。`ISyntheticDispatcher` の登録が作成時に `Bind` し、`AppHost.CreateHost` がホスト構築直後に解決して有効にする。未束縛のときは、明示的に渡されたハンドラへ送るだけで、合成状態は更新しない（本番では起きない）。Phase8 で削除する（TODO をコードに明記）。
  - **`SyntheticDispatcherService` は `OutputKBMHandlerAdapter` を直接受ける**。旧実装は「ハンドラが未決定なら何もせず戻る」だったが、アダプターは常に存在するため、`Handler == null` を見て同じ挙動を保つ。
  - **`ActionManager`（静的ファサード）の `Mapping` 呼び出しは、取得済みの `sp` から解決**する形にした（`ServiceProviderHolder` の語は増えていない。ラチェット維持）。`PreallocateForProfileApply` は、既存の `ServiceProviderHolder.Provider` の取得を 1 つの変数にまとめ、登録簿も同じ変数から解決する。
  - `GetOrCreateSyntheticKeyAction`／`ClearSyntheticActionCache` は 0 件ではなく（`ProcessControlSettingAction` が使う）、instance メソッドとしてクラスの public に残し、窓口 `IMapping` には含めなかった。
  - `Global.RefreshActionAlias` は残した（`BackingStore` が使う）。`outputKBMMapping` の参照だけ `ProfileSettingsServiceInstance.OutputKBMMapping` に替えた。
- **Step7 の報告書の「`ControlService` のコンストラクター変更は多数のテストの改修になる」は不正確だった**。`new ControlService(` は本体の `ServiceRegistration` の 1 箇所だけで、テストはコンテナから解決する。実際の改修が多かったのは、`Mapping` の static を直接呼ぶテスト、リフレクション、`static` を含む文字列のマーカー（5 つのガード）だった。
- **再テストの原因（2 件）**: (1) `KeyAction` → `ActionManager`（静的ファサード）が、コンテナから `IKeyButtonControllerRegistry` を解決するようになった。テスト用の `ServiceProvider`（`dam` だけを返していた）に登録簿がなく、コントローラーが null になりトグルが成立しなかった。`TestServiceProvider` を複数のサービスを返せるようにして解決。(2) 旧 `Mapping` の静的辞書をリフレクションで読んでいたテストが null 参照になった。登録簿の `_controllers` を読む形に変更。いずれもテスト基盤の問題で、本体の回帰ではない。
- **本体の挙動が変わらないことの確認**: `MappingHotPathAllocationTests`（ゼロ割り当て）、`MapCustom`／`Commit` の特性化テストは、すべて成功。ホットパスのメソッドに、新しくラムダやローカル関数は追加していない（`this` の捕捉による割り当てを避けるため）。
- **`MouseOutputAction` の無効な検索の削除**: 未登録のため `GetService` は常に null で、出力は直後のフォールバックが行っていた。検索と、使われない `TriggerContext` の生成を削除した（出力は同じ。左ボタンの押下は、毎回生成していた不要なオブジェクトも減った）。

## 4. 起動順序・スレッド・ガードレールへの影響

- 5.4（On-Demand パス評価）: `DefaultActionManager`／`DefaultMacroPlayer` は `Func<IMapping>`（遅延）を受けるため、ホスト構築時（`PreallocateActionEntries`）に `Mapping` とその依存は作られない。`Phase7Step8W2MappingInstanceGuardTests` が固定している。`ISyntheticDispatcher` はホスト構築直後に作る（依存は `SyntheticInputState`、アダプター、`IProfileSettingsService` で、`PreallocateActionEntries` が既に解決している範囲）。
- 5.2（適用時 Halt 停止）: `ProcessControlSettingAction` の `HaltReportingRunAction` の中で `profileApplication.ApplyProfile` を呼ぶ構造は、変えていない。
- 5.6（切断時クリーンアップ）: `HandleDeviceDisconnect` の呼び出し順は変えていない。呼び出し元（`ControlService`、`DefaultActionManager`）だけ替わった。
- マクロは別スレッドの `Task` で動くが、`macroControl` の共有の範囲は広がっていない（保持クラスは従来から単一の instance）。K3（`macroDispatchInFlight`）は触れていない。

## 5. 未完了・持ち越し

- **実機確認 U-18**（チェックリスト §11）: 通常入力、キー・マウスの割り当て、ジャイロ、タッチ、フリック、2 段トリガー、マクロ、スペシャルアクション、KBM 方式の切替とフォールバック、マクロ記録画面。U-11〜U-17 も未（U-13 は先送り）。
- Step9 へ: 互換 `AppHost`（`DI/Compat/AppHost.cs`。`Mapping.cs` が使わなくなり、`DS4Windows` 名前空間の呼び出し元は 0 件のはず。削除前に確認）、`Mapping.cs` の `Global` 参照 41（定数・`Clamp`）の再配置、K3 の判断、static のまま残した純粋な関数・定数の最終的な置き場所。
- Step10 へ: `Mapping` のコンストラクターの依存（11 個）の整理、`BackingStore` のコンストラクター注入。
- Phase8 へ: 静的な `SyntheticDispatcher`（橋渡し）、`Actions/` の `ServiceProviderHolder` 17、`ActionManager` の静的な呼び出し、`KeyButtonActionController` ほかの生成経路。

## 6. 次の Step

- **Step9**（`Phase7-Plan.md` §2）: 互換 `AppHost` の削除、`Mapping.cs` の `Global` 定数参照（41）の再配置、K3 の判断。調査と `Phase7-Step9-Plan.md` の作成から着手する。
