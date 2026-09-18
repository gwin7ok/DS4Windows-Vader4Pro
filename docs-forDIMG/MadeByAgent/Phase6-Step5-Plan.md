# Phase6-Step5 計画書: OutputSlotService の全面SSOT統合と孤立配列完全撤廃

作成日: 2026-09-11  
改訂日: 2026-09-18（Phase6-Step1 再監査エビデンスに基づく推奨案［OutputSlotService 全面SSOT統合＋孤立配列完全撤廃］採用・全面拡充改訂）  
状態: 計画書改訂・承認待ち（実装未着手）  
対象ブランチ: `For-DI-migration-work`  
上位計画書: `docs-forDIMG/MadeByAgent/Phase6-Plan.md`  
準拠指針: `.github/copilot-instructions.md`, `docs-forDIMG/DI-App-Wide-Migration-Plan.md`  
参照エビデンス:  
  - `Phase6-Step1-Service-Reference-Evidence.md` §1-1（OutputSlotService 監査記録）  
  - `Phase6-Step1-UI-Reference-Evidence.md` §2（MainWindow.xaml.cs 監査記録）  
  - `Phase5-Step14-Issue7-RootCause-and-CrossSetting-Audit-Report.md`（Issue 7 根本原因分析）  
  - `Phase5-Step14-Issue7-fix-implementation-report.md`（Issue 7 是正完了記録）  

---

## 0. 背景と改訂の経緯

### 0.1 旧計画書からの前提変化と最新の実装状況
2026-09-11 に作成された旧計画書では、「`IProfileSettingsService.OutContType` が未実装のためタスク0で新設する」と計画されていた。  
しかし、`Phase5-Step14-Issue7-fix-implementation-report.md` により、**`IProfileSettingsService.OutContType`（`BackingStore.outContType` への完全委譲）は既に実装完了**しており、`ProfileSettingsViewModel.cs` および `SpecialActionsListViewModel.cs` の参照先も同プロパティへ是正済みである。

### 0.2 残存する真の課題と方針確定（推奨案: 選択肢Bの採用）
最新コードベース（`MainWindow.xaml.cs:1412` および `OutputSlotService.cs`）を精査した結果、以下の2つの根本的課題が残存していることが判明した：

1. **UDP 診断コマンドの孤立 API 参照（`MainWindow.xaml.cs:1412`）**:
   ```csharp
   else if (propName == "outconttype")
       propValue = outputSlotService.GetOutputDeviceType(tdevice).ToString();
   ```
   UDP 経由で `query.<device>.outconttype` を問い合わせた際、正本であるプロファイル設定（`IProfileSettingsService.OutContType`）ではなく、`outputSlotService.GetOutputDeviceType` を呼んでいる。
2. **`OutputSlotService` 内部の孤立配列 `_deviceTypes[]`（バグの温床）**:
   `OutputSlotService.cs` 内部に `private OutContType[] _deviceTypes = new OutContType[4]` という Global / BackingStore と全く連動していない孤立配列が存在し、`GetOutputDeviceType` は常に初期値の `OutContType.None` を返却している。
3. **`OutputSlotService.cs:193, 196` の Global 直参照**:
   `Global.outDevTypeTemp`, `Global.activeOutDevType` への直接参照が残存している。

本ステップでは、`MainWindow.xaml.cs` の1行を修正するだけの表面的な対症療法（選択肢A）にとどまらず、**孤立配列 `_deviceTypes[]` を物理的に完全撤廃し、`OutputSlotService` を `IProfileSettingsService` と統合した SSOT（信頼できる唯一の情報源）構造へと刷新する（選択肢B）**を採用する。  
これにより、出力デバイスに関する「設定」「実行時状態」「UI一時状態」の三態を完全に整理し、後続の **Phase6-Step6（出力デバイス切替時のスロット動的再接続・ホットスワップ連動）** に対する堅牢な土台を確立する。

---

## 1. 目的

1. `MainWindow.xaml.cs:1412` における UDP 診断コマンド `outconttype` の参照先を、正本である `IProfileSettingsService.OutContType` へ是正する。
2. `OutputSlotService.cs` 内部の孤立配列 `_deviceTypes[]` を物理削除し、二重管理・値の乖離の根本原因を抹消する。
3. `IOutputSlotService.GetOutputDeviceType` / `SetOutputDeviceType` を `IProfileSettingsService.OutContType` への直接委譲へ改修し、非推奨（`[Obsolete]`）マークを付与する。
4. `OutputSlotService.cs:193, 196` の Global 直参照を解消し、`OutputSlotService` を完全な Pure DI クラスへ移行する。
5. 出力デバイス状態の「三態（永続設定 / 実行時接続状態 / UI一時状態）」を明文化し、Step6 との整合性を保証する。

---

## 2. 出力デバイス設定・状態の「三態」SSOT 台帳

出力デバイスに関する混乱を恒久的に防ぐため、以下の三態モデルを定義・徹底する。

| 状態区分 | プロパティ名・契約インターフェース | 正本格納場所 (SSOT) | ライフサイクル・役割 | 許容される値 |
|---|---|---|---|---|
| **1. 永続設定 (Profile)** | `IProfileSettingsService.OutContType[slot]` | `BackingStore.outContType` (XML永続化) | プロファイルに保存される出力コントローラー種別。 | `OutContType.X360`, `OutContType.DS4` |
| **2. 実行時接続状態 (Runtime)** | `IOutputSlotService.ActiveOutDevType[slot]` | `OutputSlotService.activeOutDevType` / `Global` | 現在 ViGEm バスに実際にプラグインされている仮想コントローラーの種別。未接続時は None。 | `OutContType.None`, `OutContType.X360`, `OutContType.DS4` |
| **3. UI一時編集状態 (Temp)** | `IOutputSlotService.OutDevTypeTemp[slot]` | `OutputSlotService.outDevTypeTemp` / `Global` | スロット管理画面等でユーザーが一時選択している未確定のデバイス種別。確定時に状態1または2へ反映。 | `OutContType.None`, `OutContType.X360`, `OutContType.DS4` |

- **孤立配列 `_deviceTypes[]` の位置づけ**:
  上記三態のいずれにも属さない「第4の偽状態」であり、即時撤廃すべき技術的負債である。
- **UDP診断コマンドの整合**:
  `query.<device>.outconttype` はプロファイル設定値（状態1）を返すのが仕様であるため、`IProfileSettingsService.OutContType` を参照するのが正しい。

---

## 3. 全件修正対象台帳

| ID | ファイル名 | 行番号 | 現行コード | 是正後コード / 移行方針 |
|---|---|---|---|---|
| C5-01 | `DS4Forms/MainWindow.xaml.cs` | 1412 | `outputSlotService.GetOutputDeviceType(tdevice).ToString()` | `profileSettingsService.OutContType[tdevice].ToString()` |
| C5-02 | `DS4Control/Services/OutputSlotService.cs` | 33 | `private OutContType[] _deviceTypes = new OutContType[4] ...` | **フィールド完全削除** |
| C5-03 | `DS4Control/Services/OutputSlotService.cs` | 126 | `GetOutputDeviceType(int slotId)` | `_profileSettings.OutContType[slotId]` への直接委譲（`[Obsolete]` 付与） |
| C5-04 | `DS4Control/Services/OutputSlotService.cs` | 135 | `SetOutputDeviceType(int slotId, OutContType type)` | `_profileSettings.OutContType[slotId] = type` への直接委譲（`[Obsolete]` 付与） |
| C5-05 | `DS4Control/Services/OutputSlotService.cs` | 20 | コンストラクタ `OutputSlotService(IOutputSlotStore store = null)` | `IProfileSettingsService` を追加注入（フォールバック付き） |
| C5-06 | `DS4Control/Services/OutputSlotService.cs` | 193 | `Global.outDevTypeTemp[slot]` | `_store` または自サービス内部プロパティ経由へ置換 |
| C5-07 | `DS4Control/Services/OutputSlotService.cs` | 196 | `Global.activeOutDevType[slot]` | `_store` または自サービス内部プロパティ経由へ置換 |
| C5-08 | `DI/IOutputSlotService.cs` | - | `GetOutputDeviceType`, `SetOutputDeviceType` | `[Obsolete]` 属性付与・廃止予定コメント明記 |

---

## 4. アーキテクチャ設計・Pure DI 拡張

### 4.1 `OutputSlotService` のコンストラクタ拡張
```csharp
// DS4Windows/DS4Control/Services/OutputSlotService.cs
public class OutputSlotService : IOutputSlotService
{
    private readonly IOutputSlotStore _store;
    private readonly IProfileSettingsService _profileSettings;

    public OutputSlotService(
        IOutputSlotStore store = null,
        IProfileSettingsService profileSettings = null)
    {
        _store = store ?? new OutputSlotStore();
        _profileSettings = profileSettings ?? AppHost.GetService<IProfileSettingsService>() ?? Global.ProfileSettingsServiceInstance;
    }
    
    [Obsolete("Use IProfileSettingsService.OutContType instead. This method delegates to IProfileSettingsService.")]
    public OutContType GetOutputDeviceType(int slotId)
    {
        if (slotId < 0 || slotId >= 4) return OutContType.None;
        return _profileSettings.OutContType[slotId];
    }

    [Obsolete("Use IProfileSettingsService.OutContType instead. This method delegates to IProfileSettingsService.")]
    public void SetOutputDeviceType(int slotId, OutContType deviceType)
    {
        if (slotId < 0 || slotId >= 4) return;
        _profileSettings.OutContType[slotId] = deviceType;
    }
}
```

### 4.2 `MainWindow.xaml.cs` の呼出元是正
```csharp
// DS4Windows/DS4Forms/MainWindow.xaml.cs:1412
else if (propName == "outconttype")
{
    // C5-01: 孤立した outputSlotService ではなく、正本である profileSettingsService を参照
    propValue = profileSettingsService.OutContType[tdevice].ToString();
}
```

---

## 5. PR分割計画とマイクロステップ

安全確実に進行するため、以下の**3つのサブPR（マイクロステップ）**に分割して実装する。

```text
【Phase6-Step5 マイクロステップ構成】
├─ Step5-0: 着手前提検証・ベースライン記録（実装なし）
├─ Step5-1 (PR-1): OutputSlotService のコンストラクタ拡張と孤立配列完全撤廃（C5-02〜C5-08）
├─ Step5-2 (PR-2): MainWindow.xaml.cs の UDP 診断コマンド是正（C5-01）
└─ Step5-3 (PR-3): 単体テスト更新・三態検証・完了報告書作成
```

---

### Step5-0: 着手前提検証・ベースライン記録（実装なし）
1. Step4 完了後の作業ツリー状態、HEAD コミットハッシュ、未コミット差分ゼロを確認。
2. ソリューション全体のビルド（`dotnet build -c Release`）および全単体テスト（`dotnet test`）が 100% グリーンであることを記録。

---

### Step5-1 (PR-1): `OutputSlotService` のコンストラクタ拡張と孤立配列完全撤廃
- **対象**: `OutputSlotService.cs`, `IOutputSlotService.cs`（C5-02 〜 C5-08）
- **作業内容**:
  1. `OutputSlotService` に `IProfileSettingsService` を追加注入。
  2. 孤立配列 `_deviceTypes[]` を削除。
  3. `GetOutputDeviceType` / `SetOutputDeviceType` を `_profileSettings.OutContType` への委譲に改修し、`[Obsolete]` を付与。
  4. 193, 196行の Global 直参照を解消。
- **検証**:
  - `dotnet test` 全件合格を確認。

---

### Step5-2 (PR-2): `MainWindow.xaml.cs` の UDP 診断コマンド是正
- **対象**: `MainWindow.xaml.cs:1412`（C5-01）
- **作業内容**:
  1. 1412行目を `profileSettingsService.OutContType[tdevice].ToString()` へ置換。
- **検証**:
  - 単体テスト: UDP 診断コマンド処理ロジックの出力検証。
  - `dotnet test` 全件合格を確認。

---

### Step5-3 (PR-3): 単体テスト更新・三態検証・完了報告
- **作業内容**:
  1. `OutputSlotServiceTests.cs` の既存テストを更新（`_deviceTypes` 単体テストから、`_profileSettings` 連携検証テストへ更新）。
  2. 出力デバイス三態（永続・実行時・一時）が互いに干渉せず正しく分離・連動していることを検証するテストを追加。
  3. 完了報告書（`Phase6-Step5-Completion-Report.md`）を作成。

---

## 6. テスト・回帰検証計画

### 6.1 自動単体テスト計画
1. **`OutputSlotServiceSsotTests.cs`**:
   - `OutputSlotService.GetOutputDeviceType(slotId)` が、注入された `IProfileSettingsService.OutContType[slotId]` の値と常に同期していることの検証。
   - `SetOutputDeviceType` を呼び出した際、`IProfileSettingsService.OutContType` が更新されることの検証。
   - 範囲外スロット番号（-1, 4）を指定した際に安全に `OutContType.None` が返却されることの検証。
2. **`MainWindowUdpQueryTests.cs`**:
   - UDP コマンド `query.0.outconttype` に対して、プロファイル設定値（`Xbox360` または `DS4`）が正確に応答されることの検証（`None` が返らないこと）。

### 6.2 回帰テスト基準
- `DS4WindowsTests` および `StandaloneTests` の全テストが 100% 成功を維持すること。
- `dotnet build -c Release` でエラー・警告（`[Obsolete]` の意図された警告を除く）が0件であること。

---

## 7. ロールバック方針

万が一、UDP コマンド応答やスロット管理画面で予期せぬ不整合が発生した場合は、該当PRを直ちに `git revert` して直前の健全コミットに復旧する。

---

## 8. 完了判定チェックリスト

- [ ] `OutputSlotService.cs` 内部の孤立配列 `_deviceTypes[]` が物理的に完全削除されていること。
- [ ] `GetOutputDeviceType` / `SetOutputDeviceType` が `_profileSettings.OutContType` への委譲に改修され、`[Obsolete]` 属性が付与されていること。
- [ ] `MainWindow.xaml.cs:1412` の UDP 診断コマンド `outconttype` が `profileSettingsService.OutContType[tdevice]` を参照していること。
- [ ] `OutputSlotService.cs:193, 196` の Global 直参照が解消されていること。
- [ ] 出力デバイス状態の三態（永続設定 / 実行時接続 / UI一時状態）の SSOT 境界が確立されていること。
- [ ] `dotnet build -c Release` でエラー・警告が0件であること。
- [ ] `DS4WindowsTests` および `StandaloneTests` の全自動テストが100%成功を維持していること。