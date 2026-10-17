# Phase6-Step8 完了報告書: `ProfileEditor` のバックエンド操作の ViewModel 移設と Global 参照解消

作成日: 2026-10-13  
状態: **完了確定（2026-10-13）**（Step8-1〜8-4 の各段階でビルド・テストビルド・テスト実行成功、実機確認済み）  
対象ブランチ: `For-DI-migration-work`  
計画書: `docs-forDIMG/MadeByAgent/Phase6-Step8-Plan.md`（§0A が現行の正）  
着手前の HEAD: `e022684b`

---

## 1. 結果の要約
- `ProfileEditor.xaml.cs` の非定数の `Global.` 参照 58 件をすべて解消した。残るのは定数 37 件（`ASSEMBLY_RESOURCE_PREFIX` 29、`RESOURCES_PREFIX` 5、`TEST_PROFILE_INDEX` 3）とコメント中の言及だけ。
- 置換先は、新しい契約をほとんど作らずに済んだ。58 件のうち 55 件は既存の契約（`IAppSettingsService`、`IProfileRepository`、`IProfileSettingsService`、各 ViewModel）への置換で足り、新設したのは B8 と B10 の 2 契約だけ。
- 保存の一連・ブランクプロファイルの読み込み・キャッシュ更新・有効アクション名の反映が ViewModel のメソッドになり、UI を起動せずに検証できるようになった。コントローラーのスロットへの再適用は、従来どおり `ProfileSaved` を受けた `MainWindow` の責務のまま（Step7b 決定8）。
- 実機の動作は変わっていない（実機確認はすべて問題なし）。

## 2. 計画書からの主な変更点
- **台帳の作り直し**: 旧計画（2026-09-18）の 60 件は、Step5〜7b の変更で現行コードと合わなくなっていた。Step8-0 で 58 件の台帳（G1〜G8）を作り直した。旧 C8-44（`ApplyProfileToSlot`）は Step7b 決定8 で消滅した。
- **旧 §3.1 の設計を不採用**: 「`SaveProfile` が保存後に `ApplyProfileToSlot` を呼ぶ」設計は、Step7b 決定8（Save／Apply とも保存後に `ProfileSaved` を通知するだけ）に反するため採用しなかった。
- **B9 は作らなかった**: 呼び出し元が消えたため（§2.4 の考え方。使わない API は増やさない）。B3（`UseLang`）は Step7-1 で追加済みだった。
- **B10 の契約が旧計画と違う**: 旧計画の `LoadBlankDevProfile(int)` では不足だった。`Global.LoadBlankDevProfile` は、(a) 空プロファイルの読み込み、(b) 既定スペシャルアクションの作成、(c) 拡張情報のキャッシュ、(d) 一時プロファイル名・使用フラグ・距離フラグのクリア、まで行い、`ControlService` を引数に取る。`IProfileRepository.LoadBlankProfile(deviceIndex, launchProgram, control, xinputChange, postLoad)` として、全引数をそのまま `Global` へ渡す薄いシムにした。
- **B8 の委譲先**: `GetActionOrPlaceholder` は `Global.GetAction` そのものではなく、その実体の `BackingStore.GetAction` に委譲した（リポジトリの既定ストアは `Global.store`＝`m_Config`。結果は同じで、テストで検証できる）。
- **決定2 の追加**: `ProfileEditor` のコンストラクタに `IAppSettingsService` を必須引数で追加する計画（L2）に、実装時に `IProfileSettingsService` を 1 つ加えた。`ProfileSettingsViewModel` 経由だと、ViewModel 生成前に `OutConTypeCombo_SelectionChanged` が書く値が落ち、プロファイル XML に出力種別の項目がない場合に前回の値が残る可能性があるため。
- **「LS／RS 補正値の初期化」は移さなかった**: `Reload` 内の読み取りは、軸別スティックの画面部品と Controller Readings のデッドゾーン表示の配線で、UI の責務のため。
- **`MainWindow` の「バケツリレー撤廃」は行わず**: Pure DI（決定2＝L2）を優先し、`MainWindow` が 2 サービスを渡す形にした。`MainWindow` の解体は Step9／Step10 の範囲。

## 3. ユーザー決定（詳細は計画書 §0A.3）
- **決定1＝S1**: `Global.` の 58 件のみを対象にする。`App.logHolder`（24）、コンストラクタ内の `AppHost.GetService`（4）と `Program.rootHub` のフォールバック（1）、`App.Current.MainWindow`（7）は本 Step で変えず、Step10 の台帳へ登録。
- **決定2＝L2**: レイアウト・言語（23 件）の設定を、コンストラクタの必須引数 `IAppSettingsService` で受け取る。
- **決定3＝B8-1**: `ISpecialActionRepository.GetActionOrPlaceholder` を追加。
- **決定4＝B10-1**: `IProfileRepository.LoadBlankProfile` を追加（`ControlService` を取る形）。
- **決定5＝O1**: `OutConTypeCombo_SelectionChanged` の同じ配列への二重書き込みを 1 回にする。
- **決定6＝M-A**: `Global.` の置換を先に行い、移設は「保存の一連」と「ブランク読み込み」などバックエンド操作に限る。

## 4. 変更した主なファイル
- **契約**: `DI/ISpecialActionRepository.cs`（`GetActionOrPlaceholder`）、`DI/IProfileRepository.cs`（`LoadBlankProfile`）。実装は `DS4Control/Services/SpecialActionRepository.cs`、`DS4Control/Services/ProfileRepository.cs`。
- **ViewModel**: `ProfileSettingsViewModel.cs`（`SaveProfile`、`LoadBlankProfile`、`RefreshCustomsFlags`、`RefreshExtraProfileInfo`、`ResetOutDevTypeTemp`）、`SpecialActionsListViewModel.cs`（`CommitEnabledActionsToProfile`、`GetActionOrPlaceholder`、`ActionsFilePath`）。
- **View**: `ProfileEditor.xaml.cs`（コンストラクタ引数、置換、重複する 2 行の集約）、`MainWindow.xaml.cs`（生成呼び出しの 1 箇所）。
- **テスト（新規）**: `Phase6Step8ContractExtensionTests`（B8・B10 の契約）、`ProfileEditorGlobalReferenceGuardTests`（解消済みメンバーの再混入とコンストラクタ注入の走査）、`ProfileEditorBackendViewModelTests`（アクション名の反映の動作検証と、委譲形のソース走査）。
- **テスト（更新）**: `ProfileEditorTargetDeviceGuardTests`（生成呼び出し・保存呼び出しの検査文字列）。

## 5. 検証結果
- 各マイクロステップ（8-1〜8-4）で、ユーザーによる `dotnet build`、テストビルド、`dotnet test` が全件成功。変更はリモートへ反映済み。
- 実機確認（すべて問題なし）:
  - エディタのスプリッター・列幅の保存と復元、ウィンドウサイズのリセット（Step8-2）。
  - プロファイルを開く、New Profile のプリセットキャンセル（空のプロファイル）、Save／Apply（Step8-3）。
  - スペシャルアクションのチェック切り替えと保存の保持（Step8-4）。
- 途中で 1 度、テストが失敗した（`ProfileEditorBackendViewModelTests`）。原因は既存テストの後始末漏れで、本番コードの不具合ではない（§6）。

## 6. 持ち越し・先送り
- **後始末のない既存テスト**: `ProfileSettingsServiceSubSettingsTests.ProfileActions_GetSet_ShouldPersistAndNotify` が `Global.store.profileActions` を 8 要素の配列に置き換えたまま戻さず、実行順によって `Global.ProfileActions[8]` が範囲外になる。Step8 のテストは独立した 9 要素の配列へ差し替えて回避した。既存テスト側の修正（`finally` で元の配列へ戻す）は未実施（`Phase6-Status.md` §6.5 の既存項目）。
- **Step11 の先送り台帳（§3.3）へ登録**: Actions.xml のエクスポートを、AppData とプログラムフォルダの両方に設定がある環境で確認すること（環境がない場合のみ）。
- **Step9 への申し送り**: `ProfileSettingsViewModel.cs` の `Global.` 参照は定数・コメントを除いて 11 箇所。Step8 でメソッドを追加して行番号が変わったため、Step9-0 で台帳を作り直す（`Phase6-Step9-Plan.md` 冒頭に記載済み）。
- **Step10 へ**: `ProfileEditor` の `App.logHolder`（24）、コンストラクタ内の `AppHost.GetService`（4）と `Program.rootHub`（1）、`App.Current.MainWindow`（7）。`DS4Control/PresetOption.cs:76` の `Global.LoadBlankDevProfile` 呼び出しは、`IProfileRepository.LoadBlankProfile` へ置換できる（本 Step では変更していない）。
- **Step10b へ**: `ProfileEditor.xaml.cs` に複数の型が同居している（`HoverImageInfo`、`RumbleType`、`ResourcePaths`、`ControlIndexCheck`）。本 Step では新しい型を追加していない（§3.3 原則1）。
- **K7b-1**（異常終了、原因未特定）は先送りのまま（`Phase6-Status.md` §6.5）。

## 7. 後続への影響
- `ProfileEditor` のコンストラクタは `ProfileEditor(int targetDevice, IAppSettingsService, IProfileSettingsService)`。生成箇所は `MainWindow.ShowProfileEditor` の 1 箇所。
- ソース走査ガード `ProfileEditorGlobalReferenceGuardTests.ResolvedMembers` により、解消済みの `Global.` 参照が `ProfileEditor.xaml.cs` に再び入るとテストが失敗する。
- モデル図 01〜04 を 2026-10-13 に確認し、03（`ProfileSettingsViewModel`・`SpecialActionsSubViewModel` のメンバーと依存、注釈）、04（§2 の `IProfileRepository`／`ISpecialActionRepository` の責務、2 つの ViewModel の依存、注釈）を更新した。01・02 は構造変更なしで注釈のみ追記。
