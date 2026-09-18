# Phase6-Step6 計画書: ProfileEditor 出力デバイス切替表示追従バグ是正と負債API安全整理

作成日: 2026-09-11  
改訂日: 2026-09-18（Phase6-Step5 三態SSOT成果の統合・案1［保守的・安定性最優先アプローチ］採用・全面拡充改訂）  
状態: 計画書改訂・承認待ち（実装未着手）  
対象ブランチ: `For-DI-migration-work`  
上位計画書: `docs-forDIMG/MadeByAgent/Phase6-Plan.md`  
準拠指針: `.github/copilot-instructions.md`, `docs-forDIMG/DI-App-Wide-Migration-Plan.md` §4.5, §5.5  
参照エビデンス:  
  - `Phase6-Step5-Plan.md`（出力デバイス三態SSOT台帳）  
  - `Phase5-Step14-Issue7-RootCause-and-CrossSetting-Audit-Report.md`（Issue 7 タスク5・6 監査記録）  
  - `Phase5-Step14-Issue7-fix-implementation-report.md`（Issue 7 是正完了記録）  

---

## 0. 背景と改訂の経緯

### 0.1 実地調査で判明した事実と課題の所在
2026-09-11 に作成された旧計画書および実コード調査により、以下の2つの重要事実が確定している：

1. **実行時ホットスワップ機構は既存レガシー経路で既に正常稼働中**:
   プロファイル保存・ロード時における ViGEm 仮想コントローラーの動的再接続（ホットスワップ）は、`ScpUtil.cs:PostLoadSnippet` 内の以下のロジックにより長年安定稼働している：
   ```csharp
   // ScpUtil.cs: PostLoadSnippet
   if (oldContType != outputDevType[device])
   {
       Program.rootHub.UnplugOutDev(device);
       Program.rootHub.PluginOutDev(device, outputDevType[device]);
   }
   ```
   全体計画書 §4.5 において「ViGEm実体操作は別種の技術的負債としてPhase6/7の対象外」と明記されており、この正常動作している仮想デバイス抜き差し処理に手を加えてドライバ競合や入力遅延のリスク（案2）を冒すことは厳禁である。

2. **ProfileEditor のマッピング表示追従漏れ（実在するUIバグ / Issue 7 タスク5）**:
   `ProfileEditor.xaml.cs` において、コンボボックス（`cboOutputPadType`）を手動操作した際は `mappingListVM.UpdateMappingDevType(profileSettingsVM.ContType)` が呼ばれ、マッピング一覧のボタン名表記（Xboxの「A/B/X/Y」⇄ DS4の「Cross/Circle/Square/Triangle」）が即座に切り替わる。  
   しかし、同一エディタウィンドウを開いたまま外部から別プロファイルを読み込む **`Reload()` メソッド内において同メソッドの呼び出しが欠落**しているため、プロファイル読込後にコンボボックスの表示は変わっても、左側のマッピング一覧が古い機種のボタン名のまま残存する不整合が発生する。

3. **`IOutputSlotService` の未使用負債API（Issue 7 タスク6）**:
   `IOutputSlotService` に定義されている `PluginSlot` / `UnplugSlot` は、呼出元が0件のダミーメソッド（常に `false` を返却）であり、混乱の温床となっている。

### 0.2 方針の確定: 【案1（保守的・安定性最優先アプローチ）の採用】
Phase6-Step5 で確立された「出力デバイス設定の三態（永続設定: `OutContType` / 実行時接続状態: `ActiveOutDevType` / UI一時状態: `OutDevTypeTemp`）」に基づき、以下の保守的かつ確実な方針を採用する：

- **UI表示追従バグの完全解消**: `ProfileEditor.xaml.cs` の `Reload()` 内に `mappingListVM.UpdateMappingDevType(profileSettingsVM.ContType);` を追加し、UI表示の整合性を100%保証する。
- **未使用APIの安全封印**: `IOutputSlotService` の `PluginSlot` / `UnplugSlot` に `[Obsolete]` 属性と技術的負債コメントを付与し、API契約を安全に整理する。
- **ViGEm ホットスワップ実体の温存**: 正常稼働している `ScpUtil.cs:PostLoadSnippet` ⇄ `ControlService` のホットスワップ処理には手を触れず、動作の絶対的安定性を維持する。

---

## 1. 目的

1. `ProfileEditor.xaml.cs` の `Reload()` におけるマッピング一覧機種表示（`mappingListVM.UpdateMappingDevType`）の呼び出し漏れを解消し、プロファイル再読み込み時のボタン名表記不整合を根絶する。
2. Step5 で確立した「三態SSOT（永続 `OutContType` / 実行時 `ActiveOutDevType` / UI一時 `OutDevTypeTemp`）」の境界に沿って、UIとバックエンドの同期フローを完成させる。
3. `IOutputSlotService` の未使用API（`PluginSlot`, `UnplugSlot`）を安全に非推奨化（`[Obsolete]`）し、将来の ViGEm 刷新イニシアチブ（`IVirtualControllerBackend`）への引き継ぎ事項として整理する。

---

## 2. 全件修正対象台帳

| ID | ファイル名 | 行番号 | 現行コード / 状態 | 是正後コード / 移行方針 | 目的・役割 |
|---|---|---|---|---|---|
| C6-01 | `DS4Forms/ProfileEditor.xaml.cs` | 635付近 | `Reload()` 内で `mappingListVM` の更新なし | `mappingListVM.UpdateMappingDevType(profileSettingsVM.ContType);` を追加 | プロファイル再読込時のマッピング一覧ボタン名表示追従 |
| C6-02 | `DI/IOutputSlotService.cs` | - | `bool PluginSlot(...)` | `[Obsolete]` 属性付与・技術的負債コメント明記 | 呼出元0件APIの安全な非推奨化 |
| C6-03 | `DI/IOutputSlotService.cs` | - | `bool UnplugSlot(...)` | `[Obsolete]` 属性付与・技術的負債コメント明記 | 呼出元0件APIの安全な非推奨化 |
| C6-04 | `DS4Control/Services/OutputSlotService.cs` | - | `PluginSlot`, `UnplugSlot` 実装部 | `[Obsolete]` 属性付与・将来の移行先コメント追記 | 実装クラス側の整合性維持 |

---

## 3. 詳細設計とUI同期シーケンス

### 3.1 `ProfileEditor.xaml.cs` の UI 同期設計
`Reload()` メソッド内において、`profileSettingsVM` の読み込みが完了した直後に、マッピング一覧ビューモデル（`mappingListVM`）へ最新のコントローラー種別を伝搬させる。

```csharp
// DS4Windows/DS4Forms/ProfileEditor.xaml.cs: Reload メソッド
public void Reload(int device, bool reloadProfile = true)
{
    ...
    if (reloadProfile)
    {
        profileSettingsVM.LoadProfile(device);
    }
    
    // C6-01: プロファイル読み込み後の出力デバイス種別（Xbox 360 / DS4）をマッピング一覧へ反映
    mappingListVM.UpdateMappingDevType(profileSettingsVM.ContType);
    ...
}
```

#### 同期シーケンス:
1. ユーザーが別プロファイルをロード（またはエディタで再読み込みが発生）。
2. `profileSettingsVM.LoadProfile(device)` により、正本である `IProfileSettingsService.OutContType[device]` が読み込まれる。
3. `mappingListVM.UpdateMappingDevType(profileSettingsVM.ContType)` が即時実行される。
4. マッピングテーブルの各行が、読み込まれたプロファイルの出力機種（例: DS4 なら Cross/Circle/Square/Triangle、Xbox なら A/B/X/Y）に即時書き換わる。

### 3.2 `IOutputSlotService` の負債API非推奨化設計
```csharp
// DS4Windows/DI/IOutputSlotService.cs
public interface IOutputSlotService
{
    ...
    [Obsolete("Virtual controller dynamic plugin is handled directly by ControlService.PluginOutDev and ScpUtil.PostLoadSnippet. This API is unused and reserved for future IVirtualControllerBackend modernization.")]
    bool PluginSlot(int slotId, OutContType deviceType);

    [Obsolete("Virtual controller dynamic unplug is handled directly by ControlService.UnplugOutDev and ScpUtil.PostLoadSnippet. This API is unused and reserved for future IVirtualControllerBackend modernization.")]
    bool UnplugSlot(int slotId);
}
```

---

## 4. PR分割計画とマイクロステップ

安全確実に進行するため、以下の**3つのサブPR（マイクロステップ）**に分割して実装する。

```text
【Phase6-Step6 マイクロステップ構成】
├─ Step6-0: 着手前提検証・ベースライン記録（実装なし）
├─ Step6-1 (PR-1): ProfileEditor.xaml.cs の Reload() に UpdateMappingDevType 追加（C6-01）
├─ Step6-2 (PR-2): IOutputSlotService / OutputSlotService の負債API非推奨化（C6-02〜C6-04）
└─ Step6-3 (PR-3): UI手動検証・単体テスト実行・完了報告書作成
```

---

### Step6-0: 着手前提検証・ベースライン記録（実装なし）
1. Step5 完了後の作業ツリー状態、HEAD コミットハッシュ、未コミット差分ゼロを確認。
2. ソリューション全体のビルド（`dotnet build -c Release`）および全単体テスト（`dotnet test`）が 100% グリーンであることを記録。

---

### Step6-1 (PR-1): `ProfileEditor.xaml.cs` の表示追従修正
- **対象**: `ProfileEditor.xaml.cs`（C6-01）
- **作業内容**:
  1. `Reload()` 内に `mappingListVM.UpdateMappingDevType(profileSettingsVM.ContType);` を追加。
- **検証**:
  - `dotnet build -c Release` でビルドが通ることを確認。

---

### Step6-2 (PR-2): `IOutputSlotService` 負債APIの非推奨化
- **対象**: `IOutputSlotService.cs`, `OutputSlotService.cs`（C6-02 〜 C6-04）
- **作業内容**:
  1. `PluginSlot`, `UnplugSlot` に `[Obsolete]` 属性および技術的負債コメントを付与。
- **検証**:
  - `dotnet test` 全件合格を確認。

---

### Step6-3 (PR-3): UI検証・テスト・完了報告
- **作業内容**:
  1. ProfileEditor のプロファイル切替UIテストを実施。
  2. 完了報告書（`Phase6-Step6-Completion-Report.md`）を作成。

---

## 5. テスト・実機動作検証計画

### 5.1 自動テスト検証
- `DS4WindowsTests` および `StandaloneTests` の全自動テストを実行し、100% 成功を維持すること。
- `dotnet build -c Release` において、`[Obsolete]` の意図された警告以外のエラー・警告が0件であること。

### 5.2 実機・UI動作検証手順（Phase6-Step11連携）
1. **ProfileEditor 内でのプロファイル再読み込み表示検証**:
   - 出力デバイスが「Xbox 360」に設定されたプロファイル A を ProfileEditor で開く。
   - マッピング一覧のボタン表記が「A Button」「B Button」等になっていることを確認。
   - ウィンドウを閉じることなく、出力デバイスが「DualShock 4」に設定されたプロファイル B を「開く（Reload）」で読み込む。
   - **判定基準**: コンボボックスの表示が「DualShock 4」に切り替わると同時に、左側マッピング一覧のボタン表記が即時に「Cross」「Circle」等に切り替わること（追従漏れがないこと）。
2. **逆方向の切り替え検証**:
   - DS4 プロファイルから Xbox 360 プロファイルへの Reload を行い、同様にボタン表記が Xbox 表記へ正しく戻ることを確認。
3. **実行時 ViGEm ホットスワップ検証**:
   - コントローラー接続中、プロファイルドロップダウンから出力デバイスが異なるプロファイルへ切り替える。
   - Windows の「ゲーム コントローラーの設定（`joy.cpl`）」画面において、仮想コントローラーが即座にアンプラグされ、新たな仮想デバイス（Xbox ⇄ DS4）として再プラグインされること。
   - 入力遅延や無反応・フリーズが発生しないこと。

---

## 6. ロールバック方針

UI 表示またはスロット操作に予期せぬ不具合が発生した場合は、該当PRを直ちに `git revert` して直前の健全コミットへ復旧する。

---

## 7. 完了判定チェックリスト

- [ ] `ProfileEditor.xaml.cs` の `Reload()` 内で `mappingListVM.UpdateMappingDevType(profileSettingsVM.ContType)` が呼び出されていること。
- [ ] エディタを開いたまま異なる出力機種のプロファイルを再読み込みした際、マッピング一覧のボタン名表記が即時に追従すること。
- [ ] `IOutputSlotService` および `OutputSlotService` の `PluginSlot` / `UnplugSlot` に `[Obsolete]` 属性が付与されていること。
- [ ] `ScpUtil.cs:PostLoadSnippet` による実稼働ホットスワップ機構が手を加えられずに安全に温存されていること。
- [ ] `dotnet build -c Release` でエラーが0件であること。
- [ ] `DS4WindowsTests` および `StandaloneTests` の全自動テストが100%成功を維持していること。
- [ ] 実機コントローラーによるプロファイル切替時の ViGEm ホットスワップ動作が正常であること。