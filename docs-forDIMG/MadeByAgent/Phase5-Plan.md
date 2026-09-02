# フェーズ5計画書: DIサービス内部 Legacy 経路監査と責務分離

作成日: 2026-09-02
最終更新日: 2026-09-03（Step1追加調査結果反映・全15ステップ再編）
対象ブランチ: `For-DI-migration-work`
前フェーズ: `Phase4-Plan.md`
全体計画書: `docs-forDIMG/DI-App-Wide-Migration-Plan.md`
Step1監査レポート: `docs-forDIMG/MadeByAgent/Phase5-Step1-legacy-delegation-audit-report.md`

---

## 1. 位置づけ

Phase4 では DI 基盤、Composition Root、主要呼び出し元、ViewModel Factory を整備した。Phase5 では、DI サービスの内部実装が `Global`／`Program.rootHub`／Legacy 実体へ再委譲している経路を監査し、影響範囲と優先度を確定したうえで責務分離を進める。

DI インターフェース経由で呼ばれているだけでは、内部実体の DI 化完了とは判定しない。

---

## 2. Phase4 からの引継ぎと監査履歴

- `ProfileRepository` 内部の `Global.LoadProfile`／`Global.SaveProfile`
- `ProfileApplicationService` 内部の `Global.ApplyProfile`／`Global.LoadProfile`／`Global.LoadTempProfile`
- `SpecialActionRepository` 内部の `Global.LoadActions`／`Global.SaveActions`
- `PathService` 内部の `Global.appdatapath`
- `ProfileSettingsService` 内部の `Program.rootHub` 参照
- Save／Apply の操作ログ、保存成否、再適用成否、通知経路の統一
- CP4 前の自動テスト化、CP4 実機検証、Legacy shim 削除判断

### Step1監査および追加調査による更新（2026-09-03）
Step1の第1次監査（登録済み23サービス）および第2次追加調査（コード全体・DI未登録領域・UI層）により、以下の重要事項が特定された。

1. `IProfileActionProvider`／`IProfileActionChainService` が静的 `Mapping.DispatchProfileActionEdge` 等へ委譲（Step7対象）。
2. `IManagedActionManager` が静的 `ActionManager`／`ActionFactory`／`ActionRegistry` へ委譲（Step8対象）。
3. `IDs4DeviceRegistry` が静的 `DS4Devices` へ全委譲（Step9対象）。
4. `AutoProfileChecker` がバックグラウンドで `Global.ApplyProfile`／`rootHub` を直接呼んで自律実行している（Step10対象）。
5. `Global.SaveSettings`／`LoadSettings`（`Profiles.xml` 内 `<AppSettings>`）のDI契約が存在しない（Step11対象）。
6. `DefaultMacroPlayer` の KBM 直結および `OutputSlotService` の `rootHub.outputSlotManager` 直結（Step12対象）。
7. 各 ViewModel 内部に残存する大量の静的直参照（Step13対象）。

これらを受けて、当初の計画を大幅に拡充し、**全15ステップの包括的ロードマップ**として再編した。

---

## 3. 実施ステップ一覧

```
【Phase5 全15ステップ構成】
├─ Phase5-Step1: 詳細監査と優先度付け【完了】
├─ Phase5-Step2: プロファイル XML 読込・保存（IProfileXmlStore新設・bool成否伝播）
├─ Phase5-Step3: プロファイル適用・復帰（IProfileApplicationServiceへの一本化）
├─ Phase5-Step4: Save／Apply の操作結果と通知（通知自動解決・[DI]ログ標準化）
├─ Phase5-Step5: SpecialAction 永続化（BackingStore.actions二重管理解消）
├─ Phase5-Step6: 残存サービス境界（PathServiceキャッシュ撤廃・IDeviceStateAccessor活用）
├─ Phase5-Step7: プロファイルアクション解決・連鎖処理（IMappingActionDispatcher新設）
├─ Phase5-Step8: Actions基盤（ActionManager / ActionFactory / ActionRegistry の整理）
├─ Phase5-Step9: デバイス検出・列挙（Ds4DeviceRegistry / DS4Devices の整理）
├─ Phase5-Step10【新設】: AutoProfile（自動プロファイル切替）の自律実行系DI化
├─ Phase5-Step11【新設】: アプリ全体設定（AppSettings）の永続化・状態管理のDI化
├─ Phase5-Step12【新設】: アクション実行・出力スロット層の内部委譲整理
├─ Phase5-Step13【新設】: UI層（ViewModels）のDIサービス接続・残存静的参照の撲滅
├─ Phase5-Step14（旧Step10）: 自動テストと実機検証
└─ Phase5-Step15（旧Step11）: Legacy shim の削除判断
```

---

## 4. 各ステップの詳細内容

### Phase5-Step1: 詳細監査と優先度付け【完了】
DI登録済み23サービスおよびコード全体を精査し、全残存Legacy経路を特定・分類した。成果物: `Phase5-Step1-legacy-delegation-audit-report.md`。

### Phase5-Step2: プロファイル XML 読込・保存
`ProfileRepository` が呼んでいる `Global.LoadProfile`／`Global.SaveProfile` を対象とし、純粋なXML I/Oを担う `IProfileXmlStore` を新設して責務を分離する。保存成否の伝播のため最初から `bool SaveProfileXml` として定義する。

### Phase5-Step3: プロファイル適用・復帰
`ProfileApplicationService` をプロファイル適用の唯一の契約とし、`DefaultProfileSwitcher` で重複していた適用経路を統合する。呼び出し元からの `ControlService` / `Program.rootHub` 引き回しを排除し、スロット先行更新（`Global.ProfilePath`）の順序を保証する。

### Phase5-Step4: Save／Apply の操作結果と通知
保存成否の `bool` 伝播とエラーハンドリングを確定する。`IProfileApplicationService.ApplyProfile` の通知引数を `bool? displayNotification = null` とし、サービス内部でユーザー設定を自動解決することで、呼び出し元への不要な結合を防ぎつつ通知抑制バグを是正する。操作ログ（`[DI]`）を標準化する。

### Phase5-Step5: SpecialAction 永続化
`SpecialActionRepository` が独自リストを保持して `BackingStore.actions` と非同期になっていたサイレントバグを是正し、実データへの操作に一本化する。UIおよびActionManagerの呼び出し元調査を先行して実施する。

### Phase5-Step6: 残存サービス境界
`PathService` の初期化順序依存（起動時キャッシュ競合）をキャッシュ撤廃により根本解消する。`ProfileSettingsService` が参照する `Program.rootHub` を `IDeviceStateAccessor` に置換する。KBMアダプター（`IVirtualKBM`）の登録状況を精査する。

### Phase5-Step7: プロファイルアクション解決・連鎖処理の責務分離
`ProfileActionProvider` の `Global` 迂回路を解消し `BackingStore` 直接参照とする。巨大ファイル `Mapping.cs` の `DispatchProfileActionEdge` を薄いインターフェース `IMappingActionDispatcher` で境界化し、`ProfileActionChainService` の単体テストを完全自動化可能にする。

### Phase5-Step8: Actions基盤（ActionManager）の静的委譲分離
`IManagedActionManager`（`DefaultActionManager`）が依存している静的 `ActionManager`／`ActionFactory`／`ActionRegistry` の3系統を整理・インスタンス化し、アクションのトグル状態管理および生成処理をDI境界へ引き戻す。

### Phase5-Step9: デバイス検出・列挙（Ds4DeviceRegistry）の静的委譲分離
`IDs4DeviceRegistry`（`Ds4DeviceRegistryAdapter`）が静的 `DS4Devices` に全委譲している構造を整理し、デバイス列挙・検出の抽象化を進める。

### Phase5-Step10（新設）: AutoProfile（自動プロファイル切替）の自律実行系DI化
- **対象**: `AutoProfileChecker.cs` / `AutoProfileHolder.cs`
- **内容**: バックグラウンドでアクティブウィンドウを監視しプロファイルを自動切替する処理を `IAutoProfileService`（仮）としてDI管理下に置く。切替実行を `IProfileApplicationService` 経由に統一し、`Global.ApplyProfile` / `Program.rootHub` 直参照を排除する。

### Phase5-Step11（新設）: アプリ全体設定（AppSettings）の永続化・状態管理のDI化
- **対象**: `Global.SaveSettings` / `Global.LoadSettings`、`Profiles.xml` 内 `<AppSettings>` セクション
- **内容**: コントローラー個別設定とは独立したアプリ本体全般設定（スタートアップ起動、トレイ最小化、通知設定、UDPサーバー設定等）を扱う `IAppSettingsService` / `IAppSettingsRepository` を新設し、UIや起動処理からの静的直呼び出しを分離する。

### Phase5-Step12（新設）: アクション実行・出力スロット層の内部委譲整理
- **対象**: `DefaultMacroPlayer.cs`（`IMacroPlayer`）、`OutputSlotService.cs`（`IOutputSlotService`） / `OutputSlotManager.cs` / `OutputSlotPersist.cs`
- **内容**: `DefaultMacroPlayer` のキー入力送出を注入された `IVirtualKBM` 経由に切り替える。`OutputSlotService` が `Program.rootHub.outputSlotManager` に直結している構造およびスロット永続化（`OutputSlotPersist.cs`）の静的ファイルI/Oを整理・境界化する。

### Phase5-Step13（新設）: UI層（ViewModels）のDIサービス接続・残存静的参照の撲滅
- **対象**: `ControllersViewModel`, `SettingsViewModel`, `ProfileSettingsViewModel`, `SpecialActionsListViewModel`, `RecordBoxViewModel` 等
- **内容**: Step2〜Step12 で構築された各DIサービスをViewModelに注入・接続し、ViewModel内部に残存している `Global.ProfilePath[...]` や `Program.rootHub.DS4Controllers` の直接読み書きをDIサービス呼び出しに置換する。

### Phase5-Step14（旧Step10）: 自動テストと実機検証
各ステップ完了時にビルド、Actions／Standalone 単体テスト、結合テストを実行する。自動テストで代替できない HID、ドライバ、長時間安定性は実機検証チェックリストで確認する。

### Phase5-Step15（旧Step11）: Legacy shim の削除判断
すべてのDIサービスおよびViewModelが新契約へ移行したことを確認したうえで、残存する `Global` / `Mapping` の不要となった静的シムメソッドの削除・非推奨化を判断する。

---

## 5. 完了条件

- [ ] Step2〜Step13 の各領域において、DIサービス実装内部の `Global`／`Program.rootHub`／静的実体への再委譲が解消されていること。
- [ ] バックグラウンド自律実行系（AutoProfile）およびアプリ全体設定（AppSettings）がDIコンテナ経由で管理されていること。
- [ ] 各 ViewModel 内部から静的直アクセスが排除され、DIサービス経由で動作していること。
- [ ] 既存の全単体テストが成功し、新設された単体テストのカバレッジが確保されていること。
- [ ] ビルドエラーおよび警告の増加がないこと。
- [ ] 実機検証チェックリストにより、コントローラー入力・プロファイル切替・SpecialAction発火が正常動作すること。

---

## 6. 進行ルール

- **マイクロステップの原則**: 各Stepは必ず個別計画書を作成し、承認を得てからマイクロタスク単位で実装する。
- **No Feature Drop**: リファクタリングによる機能削減、設定値欠落、通知抑制不全を絶対に発生させない。
- **スクリプト提供ルール**: 作業成果物の保存・更新は必ず `.github/PowerShell-script-generation-rules-for-deliverables.md` に従ったPowerShellスクリプトで行う。
