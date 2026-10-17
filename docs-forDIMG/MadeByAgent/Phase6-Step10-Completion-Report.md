# Phase6-Step10 完了報告書: 残存小型 UI・ViewModel・補助クラスのドメイン別 Pure DI 化

完了日: 2026-10-15  
対象ブランチ: `For-DI-migration-work`  
計画書: `Phase6-Step10-Plan.md`（台帳は §0A）、個別計画書は §7 を参照  
前提: Step9・Step9b 完了（2026-10-14）

---

## 1. 結果の要約

- 小型 View・小型 ViewModel・補助クラス・サービス実装のコンストラクタについて、依存を「必須の引数（null は `ArgumentNullException`）」にし、`AppHost.GetService`／`Program.rootHub`／`Global.*Instance` のフォールバックを削除した（Pure DI。`copilot-instructions.md` §3.1）。
- 全域の再走査の結果を、退行ガード（`Step10FourGlobalRatchetGuardTests`）で固定した。以後、`Global`／`AppHost`／`Program.rootHub` への直接参照は、基準値を超えると失敗する。
- 移行の作業中に見つかった上流由来の不具合を、ユーザーの承認を得て 12 件是正した（§4）。
- 機能廃止（ユーザー承認済み）: Flydigi Vader 4 Pro のデバイスサポート（`Phase6-Step10-V4P-Removal-Plan.md`）。
- 実機確認は、先送り 3 項目を除いて完了した（§6）。

## 2. マイクロステップの完了状況

- **Step10-0**: 現行コードの再集計と台帳の作り直し（決定 1〜6。すべて推奨案）。
- **Step10-1**: グループ1【起動・環境系】（`WelcomeDialog`、`SaveWhere`、`About`、`UpdaterWindow`、`LanguageSelectDialog`、`LanguagePack*`、`LogViewModel` ほか）。
- **Step10-2**: グループ2【プロファイル・マッピング系】（`BindingWindow*`、`AutoProfiles*`、`DupBox`、`RecordBox*`、`ControllerListViewModel`、`ControllersViewModel` ほか）。
- **Step10-3**: グループ3【スペシャルアクション系】（`SpecialAction*`、`SpecialActions/*`）。
- **Step10-3b**: アクションの名前変更・削除を、全プロファイルへ反映（機能追加）。
- **Step10-3c**: SA の「キー押下」のボタン割り当ての表示と SwitchMode の保存の是正。
- **Step10-3d**: ボタン種別の SA の出力判定を Stage3 に一本化（Toggle の実行時の実装）。
- **Step10-4**: F08（サービス実装のコンストラクタのフォールバック）と全域の再走査（10-4-0〜10-4-6、5b〜5d）。
- **Step10-5**: `IOutputSlotService.PluginSlot`／`UnplugSlot` の接続（画面・UDP コマンド）。
- **付随**: Vader 4 Pro のサポート削除、Step10-5 後の実機確認で見つかった不具合の是正（§4 の 9〜12）。

## 3. 主な決定

- 静的データ表・純粋関数は温存（`ds4inputNames`、`macroDS4Values` など）。
- View の依存は、親のウィンドウが保持するサービスを必須引数で渡す。`StartupMethods` は静的のまま、パスを引数で受け取る。
- 薄いシム（`ScpUtil.cs` の実体を含む）は、Phase6 の範囲では解体しない（§2.1）。
- アクション連鎖（プロファイル切替直後の連鎖実行）は廃止し、Fresh-press ガードを全種別へ適用した。
- ボタン種別の SA の Press／Toggle は Stage3 に一本化。キー種別の統合は Phase8（`Phase8-Unified-Trigger-Dispatch-Plan.md` に追記済み）。
- 規約 §2.4 の判断（削除した型）: `IProfileActionChainService`／`ProfileActionChainService`、`IMappingActionDispatcher`／`MappingActionDispatcher`、`AutoProfileChecker`（「使うべきでないもの」として削除。根拠は `Phase6-Step10-4-Plan.md` §28）。`PluginSlot`／`UnplugSlot` は「使うべきもの」として接続した（Step10-5）。

## 4. 是正した上流由来の不具合（いずれも移行の回帰ではない）

1. **スロット 5〜8 の拒否と戻り値の破棄（K1）**: `ProfileApplicationService` が、8 スロット構成で 5〜8 番目を拒否していた。
2. **`OutputSlotPersist.WriteConfig` の戻り値**: 保存の成否が無視されていた。
3. **プロファイル切替直後の連鎖実行**: `DisconnectBT` などが、切替直後の押しっぱなしで即時に成立していた（Fresh-press ガードを `Mapping.cs` の全種別へ適用）。
4. **名前変更後に Selected Profile が空欄になる**: リンクプロファイルの旧名が残っていた（共通経路 `SyncProfileListAndControllers` に統合）。
5. **名前変更後の自動プロファイルの旧名**（`Auto Profiles.xml`）。
6. **名前変更後のプロファイル切替 SA の切替先**（`Actions.xml`）が None 扱いになる。
7. **SA の「キー押下」のボタン割り当ての表示と SwitchMode の保存**（Step10-3c）。
8. **ボタン種別の SA の Toggle が実行時に未実装**だった（Step10-3d）。
9. **Release モードで Left/Right Touch の出力が出ない**: `Mouse.cs` の 100ms 判定の比較が反転しており、離した直後のフレームでフラグが下りていた（上流 `7b2a69c5` 由来）。
10. **タップの位置ずれの許容が厳しすぎる**: 上流の 10 を 100 に拡大（ユーザー指定。速いタップでもずれ 26〜63 を観測）。
11. **タップのクリックが瞬間的で、フォーカスが切り替わらない**: 50ms 保持する（`TapClickHoldMs`）。Double Tap ON の保留クリックが、指を離した後に出ない問題も、`touchUnchanged` の呼び出し条件に追加して解消した。
12. **プロファイル編集画面で Left Touch のホバー表示が出ない**: `6f04ecf6`（2025-11-11）で `hoverIndexes[leftTouchConBtn]` が誤って欠落していた。

## 5. 全域の退行ガードの基準値（`Step10FourGlobalRatchetGuardTests`）

- `Program.rootHub`: 6 ファイル・25 件（`App.xaml.cs`、`ScpUtil.cs`、`ServiceRegistration.cs`、`ProfileEditor.xaml.cs`、`Mapping.cs`、`DS4Sixaxis.cs`）。
- `AppHost.GetService|Services`: 9 ファイル・48 件（`MainWindow.xaml.cs` 17、`Global.cs` 11 ほか。Step10b 後の現行値。Step10-4 の完了時は 51 件と記録したが、基準値の集計の誤りで、現行の基準値の合計は 48）。
- `Global.*`: 75 ファイル・780 件（Step10b の分割でファイル数が増えた。Step10-4 の完了時は 63 ファイル・778 件と記録）（薄いシムが過半。`ProfileRepository.cs` は 24 → 26。`ReplaceProfileSwitchTarget` が `Global.store` へ委譲するため。記録は `Phase6-Step10-4-Plan.md` §36）。
- 件数が増える変更は、テストが失敗する。基準値を更新するときは、理由を文書化する。

## 6. 実機確認

- 確認済み: 起動・サービス開始停止・プロファイル適用・名前変更の全反映・出力スロットの Plug／Unplug・Permanent の保存と復元・Fresh-press ガード・Toggle／Press・タッチパッド（タップ、ドラッグ、Release モード、ホバー）・SA のキー押下の割り当て。
- **先送り（`Phase6-Step11-Plan.md` §3.3 に登録）**:
  - 振動・DualSense の補正と自動停止時間の反映（Step10-4-2c。振動するデバイスが無い）。
  - スロット 5〜8 の切替と自動プロファイル（Step10-4-2f）。
  - UDP コマンド（`outputslot.<n>.unplug`／`plugds4`／`plugx360`。Step10-5。環境が無い）。

## 7. 変更したファイル（主なもの）

- 個別計画書: `Phase6-Step10-1-Plan.md`、`Phase6-Step10-2-Plan.md`、`Phase6-Step10-3-Plan.md`、`Phase6-Step10-3b-Plan.md`、`Phase6-Step10-3c-Plan.md`、`Phase6-Step10-3d-Plan.md`、`Phase6-Step10-4-Plan.md`、`Phase6-Step10-5-Plan.md`、`Phase6-Step10-V4P-Removal-Plan.md`。
- 新規の本番コード: `ButtonSpecialActionOutput.cs`、`ProfileRepository.ReplaceLinkedProfileName`／`ReplaceProfileSwitchTarget`、`AutoProfileHolder.ReplaceProfileName`、`IOutputSlotService` の実装（`OutputSlotService`）。
- 本番コード（主なもの）: `Mapping.cs`、`Mouse.cs`、`MainWindow.xaml.cs`、`ControlService.cs`、`ProfileEditor.xaml.cs`、`ServiceRegistration.cs`、`ProfileApplicationService.cs`、`CurrentOutDeviceViewModel.cs`、`OutputSlotManagerControl.xaml.cs`、`OutputSlotPersist.cs`。
- テスト（主なもの）: `Step10FourGlobalRatchetGuardTests`、`Step10FourOneGlobalReferenceGuardTests`、`Step10FourTwoGlobalReferenceGuardTests`、`ButtonSpecialActionOutputTests`、`ProfileRenamePropagationTests`、`MouseReleaseButtonHoldTests`、`MouseTapToleranceTests`、`MouseTapClickHoldTests`、`ProfileEditorHoverIndexTests`、`TestRepositoryFactory`。
- 文書: `Phase6-Status.md`、`Phase6-Step10-Plan.md`、`Phase6-Step11-Plan.md`（先送り台帳）、`Phase6-Step10b-Plan.md`（申し送り）、`Phase8-Unified-Trigger-Dispatch-Plan.md`、`DI-App-Wide-Migration-Plan.md`、モデル図 `01`〜`04`。

## 8. 後続への申し送り

- **Step10b（1 ファイル 1 型）**: 今回の新規ファイルは 1 ファイル 1 型で作成済み。既存の複数型ファイルは、Step10 では分割していない。`Mouse.cs` に型は追加していない（定数とメソッドのみ）。
- **Step11（総合検証）**: 先送り 3 項目（§6）と、`Phase6-Step11-Plan.md` §3.3 の台帳。Mouse.cs の `[TAP]` の調査用 Trace（`touchesBegan`／`touchesEnded`）は、ユーザーの判断で残している（Trace レベルのみ。外す場合は Step11 で）。
- **Step12／Phase7**: `MainWindow` の `AppHost` 17 件、`App.xaml.cs` の `Program.rootHub` 12 件（K7-1）、`Mapping`／`SyntheticDispatcher`（Phase7）、静的 `ActionFactory`／`ActionManager` の `ServiceProviderHolder`（D4-4）、`Global.ApplyProfile` の実装本体（Phase7）。
- **Phase8**: キー種別の SA の Stage3 への統合、`MouseOutputAction` の完成（Step9b F03）。
- **持ち越し（変更なし）**: K7b-1（異常終了）、K9-3（プリセットメニューのサブメニュー）、K7-1。
