# フェーズ4-Step10-2 計画書: 主要呼び出し元の実稼働DIサービス直接参照化（フェーズ5前倒し・先行着手）

作成日: 2026-09-01
対象ブランチ: `For-DI-migration-work`
前提ドキュメント:
- `docs-forDIMG/DI-App-Wide-Migration-Plan.md` §5.4, §6.7, §6.10（全体計画書・Globalシム設計方針）
- `docs-forDIMG/MadeByAgent/Phase4-Plan.md`（Phase4詳細計画書）
- `docs-forDIMG/MadeByAgent/Phase4-Status.md`（Phase4進捗管理）
- `docs-forDIMG/MadeByAgent/Phase4-Step10-Plan.md`（Step10計画書、タスクStep10-2の原定義）
- `.github/copilot-instructions.md`（エージェント作業ルール）

---

## 0. 本ステップの経緯と位置づけ

### 0.1 発端
Step10タスク10-4（実機検証CP4）着手前に、`[DI]`/`[Legacy]`ログの出力実態を調査した結果、以下が判明した。

1. `Global`静的メンバ442件中、DIサービスへ実際に委譲する「シム」として機能しているのは**14件のみ**（`ProfileSettingsServiceInstance`等8つのインスタンスホルダー ＋ `tempprofilename`/`useTempProfile`/`tempprofileDistance`/`useDInputOnly`/`linkedProfileCheck`/`touchpadActive`の6プロパティ）。
2. `ControlService.cs`（136箇所）、`Mapping.cs`（91箇所）、`ProfileSettingsViewModel.cs`（581箇所）はいずれも`IProfileSettingsService`等のDIインターフェースを一切参照しておらず、すべて`Global.X`経由（Legacy扱い）。
3. `ViewModelFactory`（Step9成果物）は生成時に`[DI]`ログを出すのみで、生成された`ProfileSettingsViewModel`内部の処理自体は従来通り`Global`を直接参照。

### 0.2 全体計画書との照合結果
`docs-forDIMG/DI-App-Wide-Migration-Plan.md` §5.4・§6.10を確認した結果、以下が明らかになった。

- 全体計画書の設計意図は、**Strangler Figパターンにより「呼び出し元は変更せず、`Global`内部だけを新サービスへの薄い委譲に置き換える」**ことであった（§5.4: 「75ファイルある既存の`Global.xxx`呼び出し元を一度に全て書き換える必要がなくなる」）。
- §6.10の進捗指標も「`Global.xxx`参照ファイル数はフェーズ4完了時点で**シム経由のみ・新規増加なし**」であり、ゼロ件化は目標にしていない。
- 呼び出し元を直接DI参照に置き換える作業（`Global`シムの削除）は、**§6.7フェーズ5のスコープ**として明記されている。

→ **結論**: 今回`ControlService.cs`/`Mapping.cs`/主要ViewModelの呼び出し元を直接DI参照化する作業は、本来フェーズ5に予定されていたものを、Phase4-Step10の中で**意図的に前倒しして着手する**ものである。この位置づけをPhase4-Status.md／Phase4-Plan.mdに明記する。

### 0.3 対象3ファイルの実施可否再調査
「既にDIサービスへ実際に委譲されている6メンバー」が対象3ファイルでどれだけ使用されているかを調査した結果：

| ファイル | 該当6メンバーの使用数 | 直接置換の可否 |
|---|---:|---|
| `ControlService.cs` | **5箇所**（`linkedProfileCheck`×2, `useTempProfile`×1, `tempprofilename`×1, `tempprofileDistance`×1） | **可**（今回実施） |
| `Mapping.cs` | 0箇所 | 不可（参照している`Global`メンバーが未シム化のため、置換先が存在しない） |
| `ProfileSettingsViewModel.cs` | 0箇所 | 不可（同上） |

`Mapping.cs`・`ProfileSettingsViewModel.cs`は、対象メンバー自体がまだ「新サービスへの薄い委譲」になっていないため、**呼び出し元だけを先に直接DI参照化することができない**（委譲先の実体が存在しない）。これらは本Stepでは対象外とし、§4「今後の課題」に文書化する。

---

## ルール確認（作業開始前に毎回読む）

- **§2.1 フォールバック実装の原則**: 古い方式（`Global`経由）は削除せず、動作確認が取れるまで残す。1つの機能に対して複数の実装経路を同時に持たない（今回は「`ControlService`内の対象5箇所」は必ずDI経路のみに統一し、Global経由との並存はさせない）。
- **§2.2 現在の機能の完全維持**: `linkedProfileCheck`/`useTempProfile`/`tempprofilename`/`tempprofileDistance`の配列サイズ・初期値・既存の条件分岐を100%維持する。
- **§2.3 ログ出力の厳格な維持**: 置換後の処理には`[DI] ControlService.<メソッド名>: <詳細>`形式のTraceログを追加する。
- **§3.1 DI実装**: コンストラクタ・インジェクションを使用する。`ControlService`は既に`IDs4DeviceRegistry`をコンストラクタ注入されており、同じパターンで`IProfileSettingsService`を追加する。
- **§3.2 巨大ファイルの編集方針**: `ControlService.cs`（3,332行）はファイル全体を再生成せず、対象5箇所と該当コンストラクタのみをピンポイント置換する。

---

## 1. 設計方針

### 1.1 `ControlService`へのコンストラクタ注入追加
既存の`IDs4DeviceRegistry`注入パターンを踏襲し、`IProfileSettingsService`を追加する。

```csharp
public ControlService(
    DS4WinWPF.ArgumentParser cmdParser,
    IDs4DeviceRegistry deviceRegistry,
    DS4Windows.DI.IProfileSettingsService profileSettingsService = null)
{
    this.cmdParser = cmdParser;
    this._deviceRegistry = deviceRegistry;
    this._profileSettingsService = profileSettingsService
        ?? DS4WinWPF.AppHost.GetService<DS4Windows.DI.IProfileSettingsService>()
        ?? Global.ProfileSettingsServiceInstance; // 最終フォールバック（AppHost未初期化時の安全策）
    ...
}
```

`ViewModelFactory`と同じ「引数省略時はAppHostから解決、それも失敗したら`Global`経由のフォールバックインスタンスを使う」という多段フォールバックにより、DIコンテナが未初期化のテスト環境等でも動作を維持する。

### 1.2 対象5箇所の置換方針
各箇所を`Global.X` → `_profileSettingsService.X`（対応するプロパティ名）に置換し、`[DI]`ログを追加する。

```csharp
// 変更前
Global.linkedProfileCheck[index] = true;

// 変更後
_profileSettingsService.LinkedProfileCheckArray[index] = true;
if (AppLogger.IsTraceEnabled)
    AppLogger.LogTrace($"[DI] ControlService.PrepareConnectedInputController: LinkedProfileCheckArray[{index}] = true");
```

`Global.linkedProfileCheck`自体（シム）は削除せず存置する（§2.1）。他の同一メソッド内の`Global.SelectedProfile`・`Global.LinkedProfileUI`・`Global.ApplyProfile`等（未シム化メンバー）は本Stepの対象外のため変更しない。

### 1.3 対象外メンバーの扱い
`Mapping.cs`・`ProfileSettingsViewModel.cs`については、本Stepでは変更を行わない。§4「今後の課題」に、今後シム接続を進める際の優先候補（両ファイルで参照頻度の高い`Global`メンバー）を記録し、次ステップ（Step10-3以降、または新設ステップ）の計画立案時に参照できるようにする。

---

## 2. 成果物一覧

| ファイルパス | 種別 | 内容 |
|---|---|---|
| `DS4Windows/DS4Control/ControlService.cs` | 更新 | コンストラクタへの`IProfileSettingsService`注入、対象5箇所の直接DI参照化、`[DI]`ログ追加 |
| `DS4WindowsWPF.csproj`または呼び出し元（`ControlService`生成箇所） | 要確認 | コンストラクタ引数追加に伴う呼び出し元（`App.xaml.cs`等）の修正要否確認 |
| `docs-forDIMG/MadeByAgent/Phase4-Step10-2-Plan.md` | 新規 | 本計画書 |
| `docs-forDIMG/MadeByAgent/Phase4-Step10-2-Completion-Report.md` | 新規 | 完了報告書 |
| `docs-forDIMG/MadeByAgent/Phase4-Status.md` | 更新 | Step10-2の位置づけ・進捗を反映 |
| `docs-forDIMG/MadeByAgent/Phase4-Step10-2-RealDevice-Verification-Checklist.md` | 新規 | 実機検証チェックリスト（プロファイル切替・接続時の`linkedProfileCheck`/一時プロファイル動作確認） |

---

## 3. 作業手順（マイクロタスク分割）

### タスクStep10-2-1: `ControlService`コンストラクタへの`IProfileSettingsService`注入
- コンストラクタ引数追加、多段フォールバック実装。
- 呼び出し元（`ControlService`の`new`箇所、想定は`App.xaml.cs`または`AppHost`経由の1箇所）を確認し、必要なら引数を明示的に渡す形に修正（省略可能引数のため多くの場合は無修正で動作する想定）。

### タスクStep10-2-2: 対象5箇所のピンポイント置換
- `linkedProfileCheck`×2、`useTempProfile`×1、`tempprofilename`×1、`tempprofileDistance`×1を`_profileSettingsService`経由に置換。
- 各箇所に`[DI]`Traceログを追加。

### タスクStep10-2-3: ビルド確認・単体テスト実行
- `dotnet build DS4WindowsWPF.sln --nologo`で警告0・エラー0を確認。
- `DS4WindowsTests`・`StandaloneTests`全件実行、回帰ゼロを確認。

### タスクStep10-2-4: 実機動作確認チェックリスト作成・検証
- `Phase4-Step10-2-RealDevice-Verification-Checklist.md`を作成し、以下を確認：
  - コントローラー接続時のプロファイル自動適用（linkedProfileCheckが関与する経路）
  - 一時プロファイル切替機能（tempprofilename/useTempProfile/tempprofileDistanceが関与する経路）
  - ログに`[DI] ControlService....`が出力され、該当箇所が`[Legacy]`を経由しなくなっていること

### タスクStep10-2-5: 完了報告書作成・進捗更新
- `Phase4-Step10-2-Completion-Report.md`を作成。
- `Phase4-Status.md`にStep10-2の完了を反映し、「今後の課題」として`Mapping.cs`/`ProfileSettingsViewModel.cs`の未シム化メンバー一覧を記録。

---

## 4. 今後の課題（本Step対象外・次ステップ検討材料）

`Mapping.cs`・`ProfileSettingsViewModel.cs`の呼び出し元DI直接参照化には、対応する`Global`メンバーのシム接続（DIサービスへの委譲実装）が前提として必要である。現時点で両ファイルが多用しているにもかかわらず未シム化の主なカテゴリ：

- プロファイル設定値get/set群（約100件、`IProfileSettingsService`側の対応拡張が必要。現状シム化済みは6件のみ）
- デバイス状態管理系（`IDeviceStateService`は実装済みだが、`Global`側・`ControlService`の実データ（`DS4Controllers`等）と接続されておらず空の並行状態を持つのみ）
- 出力スロット管理系（`IOutputSlotService`も同様に未接続）

これらは影響範囲が大きく（`Mapping.cs`8,827行、`ProfileSettingsViewModel.cs`4,245行）、本Stepの「先行3箇所」の枠を超えるため、次ステップとして別途計画書を立てることを提案する。

---

## 5. リスクと回避策

| リスク | 該当タスク | 回避策 |
|---|---|---|
| コンストラクタ引数追加による既存呼び出し元の破壊 | Step10-2-1 | デフォルト引数（`= null`）とし、既存の`new ControlService(cmdParser, deviceRegistry)`呼び出しをそのまま動作させる。呼び出し元は事前にgrepで全数確認する。 |
| `IProfileSettingsService`未初期化時（DIコンテナ構築前）のnullアクセス | Step10-2-1 | 多段フォールバック（AppHost解決失敗時は`Global.ProfileSettingsServiceInstance`を使用）で必ず非null値を保証する。 |
| 置換対象5箇所の周辺にある未シム化メンバー（`SelectedProfile`等）との整合性崩れ | Step10-2-2 | 周辺の未シム化メンバーは変更せず現状維持。データの実体は最終的に同じ`ProfileSettingsService`インスタンスを参照するため、`Global`経由・DI経由のどちらからアクセスしても同一データであることをコードレビューで確認する。 |

---

## 6. 完了判定基準

- [ ] `ControlService`が`IProfileSettingsService`をコンストラクタ注入され、多段フォールバックが実装されている。
- [ ] 対象5箇所が`Global.X`から`_profileSettingsService.X`への直接参照に置換され、`[DI]`ログが出力される。
- [ ] 全自動テストが成功する（回帰ゼロ）。
- [ ] ソリューションビルドが警告0・エラー0で成功する。
- [ ] `Phase4-Step10-2-RealDevice-Verification-Checklist.md`で対象機能（プロファイル自動適用・一時プロファイル切替）の実機動作確認が合格する。
- [ ] `Phase4-Step10-2-Completion-Report.md`が作成され、`Phase4-Status.md`が更新されている。
- [ ] `Mapping.cs`／`ProfileSettingsViewModel.cs`の未シム化状況が「今後の課題」として文書化されている。