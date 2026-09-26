# フェーズ4-Step10-2 計画書（改訂版）: シム接続拡張の先行実施 → 主要3ファイル呼び出し元の一括DI直接参照化

作成日: 2026-09-01（改訂）
対象ブランチ: `For-DI-migration-work`
前提ドキュメント:
- `docs-forDIMG/DI-App-Wide-Migration-Plan.md` §4.1, §5.4, §6.7, §6.10（全体計画書）
- `docs-forDIMG/MadeByAgent/Phase4-Plan.md`（Phase4詳細計画書）
- `docs-forDIMG/MadeByAgent/Phase4-Status.md`（Phase4進捗管理）
- `docs-forDIMG/MadeByAgent/Phase4-Step10-Plan.md`（Step10計画書）
- `docs-forDIMG/MadeByAgent/Phase4-Step10-2 plan.md`（本書の旧版。今回の改訂で置き換える）
- `.github/copilot-instructions.md`（エージェント作業ルール）

---

## 0. 改訂の経緯

### 0.1 旧版からの方針変更
旧版（初版Plan）では、「既にDIサービスへ実際に委譲されている6メンバー」のみを対象に`ControlService.cs`の5箇所だけを先行置換する、という最小スコープを提案した。今回、依頼者の判断により方針を変更し、**`Mapping.cs`／ViewModel側のシム接続拡張を先に完了させたうえで、`ControlService.cs`／`Mapping.cs`／`ProfileSettingsViewModel.cs`の3ファイルをまとめて呼び出し元DI直接参照化する**方針に改める。

### 0.2 追加調査で判明した重要な事実（アーキテクチャ上の朗報）
`Mapping.cs`・`ProfileSettingsViewModel.cs`が参照する`Global`メンバーの実装を確認した結果、以下が判明した。

- `ProfileSettingsViewModel.cs`が参照する`Global`メンバーは**126種類**。このうち**約118種類**は、`public static X Y => m_Config.y;`という形で、単一の`BackingStore`インスタンス（`m_Config`、`Global.store`と同一）へ委譲するだけの**薄いラッパープロパティ**である（例: `RSOutputSettings => m_Config.rsOutputSettings`, `LightbarSettingsInfo => m_Config.lightbarSettingInfo`）。
- `Mapping.cs`が参照する`Global`メンバーは24種類。このうち`Global.Clamp`（23回参照）・`Global.MAX_DS4_CONTROLLER_COUNT`（24回参照）・`Global.TEST_PROFILE_ITEM_COUNT`（3回参照）は**純粋関数・定数**であり、全体計画書§4.1の分類上**DI化不要**（`ColorUtil`/`VersionUtil`等への移設のみが対象、Global委譲シム化の対象外）。残りの約13種類のうち大半（`RSOutputSettings`, `LSOutputSettings`, `R2OutputSettings`, `L2OutputSettings`, `ProfileChangedNotification`, `outputKBMMapping`等）も同じく`m_Config`委譲パターンである。

**この事実により、シム接続拡張の実装難度は当初想定より大幅に下がる。** 対象メンバーの多くは、`ProfileSettingsService`が独自のバックアップ配列を新設する必要がなく、**`m_Config`（`BackingStore`）への参照を`ProfileSettingsService`が保持し、そのまま委譲するだけ**で済む。これにより、プロファイル読込・保存（XML永続化）と新DI経路のデータが同一インスタンスを指すため、データの二重管理・不整合リスクが原理的に発生しない。

（既存の6メンバー`tempprofilename`等は`m_Config`委譲ではなく独立フィールドだったため、`ProfileSettingsService`内に専用バックアップ配列を持たせる実装になっていた。今回追加する約118+13メンバーとは実装パターンが異なる点に注意。）

### 0.3 対象外として切り出すメンバー
以下は`m_Config`委譲パターンではなく、他のカテゴリに属するため本Stepでは対象外とし、§6「今後の課題」に記録する。

| メンバー | 分類 | 対象外の理由 |
|---|---|---|
| `Global.exedirpath` | パス・環境 | `IPathService`/`IEnvironmentService`領域。Step5成果物との重複整理が必要 |
| `Global.outDevTypeTemp` | デバイス状態 | `m_Config`委譲ではない独立静的フィールド。`IDeviceStateService`領域だが、同サービスは現状未接続（別課題） |
| `Global.IsUsingMinViGEm117333` | 環境判定 | 副作用のない環境チェックメソッド。`IEnvironmentService`領域 |
| `Global.RefreshActionAlias` | SpecialAction | 内部で副作用処理を行うメソッド。個別調査が必要 |
| `Global.CacheProfileCustomsFlags` | プロファイル管理 | 内部で副作用処理を行うメソッド。個別調査が必要 |
| `Global.defaultButtonMapping` | デフォルト値定数 | プロファイル非依存の固定テーブル。DI化要否は低優先度 |
| `Global.ApplyProfile` | プロファイル管理（3-c層） | `IProfileRepository`領域（既にインターフェース自体は存在するが、`Load`/`Save`と同様に別途委譲実装が必要） |
| `Global.SaveControllerConfigs` / `Global.LoadControllerConfigs` | プロファイル管理 | `IProfileRepository`領域 |
| `Global.ProfileActions` | SpecialAction | `ISpecialActionRepository`領域 |
| `Global.outputKBMHandler` | 3-b層出力 | `IVirtualKBM`領域（本プランのスコープ外、フェーズ2該当） |
| `Global.absUseAllMonitors` / `Global.TranslateCoorToAbsDisplay` | モニタ・座標変換 | `IDisplayInfoProvider`領域、優先度低 |

---

## ルール確認（作業開始前に毎回読む）

- **§2.1 フォールバック実装の原則**: `Global`側のシムは削除せず維持。1つの機能に対して複数の実装経路を同時に持たない。
- **§2.2 現在の機能の完全維持**: `m_Config`のプロファイル読込・保存・既定値・配列境界を100%維持する。委譲先を変えるだけで、データの実体・タイミングは変更しない。
- **§2.3 ログ出力の厳格な維持**: `Global`シム経由は`[Legacy]`、`ProfileSettingsService`直接呼び出しは`[DI]`のTraceログを維持する。
- **§3.1 DI実装**: コンストラクタ・インジェクションを使用する。
- **§3.2 巨大ファイルの編集方針**: `ScpUtil.cs`（11,388行）、`Mapping.cs`（8,827行）、`ProfileSettingsViewModel.cs`（4,245行）はいずれもファイル全体を再生成せず、対象メンバーのみをピンポイント置換する。事前に置換対象文字列の出現回数を確認し、一意性を確保してから置換する。

---

## 1. 全体構成（2段階）

| Stage | 内容 | 対象 |
|---|---|---|
| **Stage 1 (Step10-2-A)** | `IProfileSettingsService`のシム接続拡張（`m_Config`委譲パターンの約131メンバーを追加） | `ScpUtil.cs`（`Global`側）、`ProfileSettingsService.cs`（DIサービス側） |
| **Stage 2 (Step10-2-B)** | `ControlService.cs`／`Mapping.cs`／`ProfileSettingsViewModel.cs`の3ファイルをまとめて、Stage1で接続したメンバーの呼び出し元をDI直接参照に置換 | 上記3ファイル |

Stage1完了・単体テスト・実機検証がすべて合格した後にStage2へ進む（§2.1の「1機能=1実装経路」原則に基づき、Stage1未完了のままStage2に着手しない）。

---

## 2. Stage 1（Step10-2-A）: シム接続拡張の詳細設計

### 2.1 設計方針

`ProfileSettingsService`に`BackingStore`への参照を追加する。

```csharp
public class ProfileSettingsService : IProfileSettingsService
{
    // 既存の独立バックアップ配列（tempprofilename等、6メンバー分）はそのまま維持
    ...

    // 新設: m_Config委譲用（Global.storeと同一インスタンスを参照）
    private readonly BackingStore _config;

    public ProfileSettingsService(BackingStore config = null)
    {
        _config = config ?? Global.store;
    }

    // 機械的に追加する委譲プロパティの例
    public StickOutputSetting[] RSOutputSettings => _config.rsOutputSettings;
    public LightbarSettingInfo[] LightbarSettingsInfo => _config.lightbarSettingInfo;
    // ...(約131メンバー分、同一パターンで追加)
}
```

`Global`側は、対応するメンバーを次の形に変更する（1行委譲＋`[Legacy]`ログ）。

```csharp
// 変更前
public static StickOutputSetting[] RSOutputSettings => m_Config.rsOutputSettings;

// 変更後
public static StickOutputSetting[] RSOutputSettings
{
    get
    {
        if (AppLogger.IsTraceEnabled)
            AppLogger.LogTrace("[Legacy] Global.RSOutputSettings: accessed via static shim");
        return ProfileSettingsServiceInstance.RSOutputSettings;
    }
}
```

`IProfileSettingsService`インターフェースには対応するプロパティ宣言を追加する。

### 2.2 対象メンバーのカテゴリ分割（マイクロタスク単位）

131メンバーを一度に変更せず、`ProfileSettingsViewModel.cs`の画面構成（既存のUIカテゴリ）に沿って以下のサブタスクに分割し、**1サブタスク＝1PR相当（ビルド・テスト確認を挟む）**で進める。

| サブタスク | 主なメンバー例 | 件数目安 |
|---|---|---:|
| Step10-2-A-1: スティック関連 | `LSOutputSettings`, `RSOutputSettings`, `LSModInfo`, `RSModInfo`, `LSSens`, `RSSens`, `LSRotation`, `RSRotation`, `LSAntiSnapbackInfo`, `RSAntiSnapbackInfo`, `SquStickInfo`等 | 約24 |
| Step10-2-A-2: トリガー(L2/R2)関連 | `L2OutputSettings`, `R2OutputSettings`, `L2ModInfo`, `R2ModInfo`, `L2Sens`, `R2Sens`, `OutputVirtualTriggerButton`, `OutputDS4TriggerMode`等 | 約12 |
| Step10-2-A-3: タッチパッド関連 | `TouchMouseStickInf`, `TouchAbsMouse`, `TouchRelMouse`, `TouchSensitivity`, `TapSensitivity`, `TouchpadInvert`, `TouchpadJitterCompensation`, `TouchDisInvertTriggers`, `TouchClickPassthru`, `StartTouchpadOff`, `TouchOutMode`等 | 約15 |
| Step10-2-A-4: ジャイロ関連 | `GyroMouseStickInf`, `GyroMouseInfo`, `GyroSwipeInf`, `GyroControlsInf`, `GyroInvert`, `GyroSensitivity`, `GyroSensVerticalScale`, `GyroOutputMode`, `GyroTriggerTurns`, `GyroMouseStickTriggerTurns`, `GyroMouseHorizontalAxis`, `GyroMouseStickHorizontalAxis`, `SetGyroMouse*`系メソッド等 | 約18 |
| Step10-2-A-5: ライトバー・ランブル関連 | `LightbarSettingsInfo`, `MainColor`, `LowColor`, `FlashType`, `FlashAt`, `RumbleBoost`, `InverseRumbleMotors`, `DualSenseRumbleEmulationMode`, `DualSenseHapticPowerLevel`, `UseGenericRumbleStrRescaleForDualSenses`, `getRumbleAutostopTime`/`setRumbleAutostopTime`等 | 約12 |
| Step10-2-A-6: ボタン/マウス出力関連 | `ButtonMouseInfos`, `ButtonAbsMouseInfos`, `TrackballMode`, `TrackballFriction`, `ScrollSensitivity`, `WheelSmoothInfo`, `DoubleTap`, `EnableTouchToggle`等 | 約10 |
| Step10-2-A-7: SA(ステアリングホイール)・その他デッドゾーン関連 | `SASteeringWheelEmulationRange`, `SASteeringWheelEmulationAxis`, `SATriggers`, `SAMousestickTriggers`, `SAWheelFuzzValues`, `SXDeadzone`/`SZDeadzone`/`SXSens`/`SZSens`/`SXMaxzone`/`SZMaxzone`, カーブ関連(`*OutBezierCurveObj`, `get/set*OutCurveMode`)等 | 約24 |
| Step10-2-A-8: 残余（デバイスオプション・雑多フラグ） | `BTPollRate`, `DS4Mapping`, `DinputOnly`, `IdleDisconnectTimeout`, `RightStickDriftXAxis`/`YAxis`, `LeftStickDriftXAxis`/`YAxis`, `EnableOutputDataToDS4`, `UseDs3PitchRollSim`, `LowerRCOn`, `GetDS4CSetting`, `LaunchProgram`等 | 約16 |
| Step10-2-A-9: `Mapping.cs`専用（Step10-2-A-1〜8で未カバーの分） | `ProfileChangedNotification`, `outputKBMMapping`, `DebouncingMs`, `getMainColor` | 約4 |

（件数は概算。実装時に`Phase4-Step10-2-Global-Member-Mapping.md`として正式な対応表を作成し、上記表を正本に更新する。）

### 2.3 Stage1の作業手順（各サブタスク共通のマイクロタスク型）

1. 対象メンバーの`Global`側宣言・`m_Config`側の型を確認。
2. `IProfileSettingsService`にプロパティ宣言を追加。
3. `ProfileSettingsService`に`_config.x`への委譲実装を追加（`[DI]`ログ）。
4. `Global`側を1行委譲＋`[Legacy]`ログに置換（ピンポイント置換、出現1回であることを事前確認）。
5. ビルド確認・対象サブタスクの単体テスト実行。
6. 次のサブタスクへ進む。

### 2.4 Stage1の完了判定基準

- [ ] `IProfileSettingsService`に131メンバー相当のプロパティ・メソッドが追加されている。
- [ ] `ProfileSettingsService`が`Global.store`と同一の`BackingStore`インスタンスを参照し、委譲している（二重管理なし）。
- [ ] `ScpUtil.cs`の対象メンバーすべてが1行委譲＋`[Legacy]`ログに置換されている。
- [ ] 全自動テストが成功する（回帰ゼロ）。
- [ ] ビルドが警告0・エラー0で成功する。
- [ ] プロファイル読込・保存・画面表示（`ProfileSettingsViewModel`の各設定項目）が既存動作と一致することを実機で確認する（`Phase4-Step10-2-A-RealDevice-Verification-Checklist.md`）。

---

## 3. Stage 2（Step10-2-B）: 3ファイル一括の呼び出し元DI直接参照化

Stage1完了後に着手する。詳細設計はStage1完了後、実際に接続されたメンバー一覧を踏まえて別途具体化するが、現時点での方針は以下の通り。

### 3.1 対象と方式
- `ControlService.cs`・`Mapping.cs`・`ProfileSettingsViewModel.cs`の3ファイルに、それぞれ`IProfileSettingsService`をコンストラクタ注入する（`ProfileSettingsViewModel`は`ViewModelFactory`経由で注入する形に拡張）。
- Stage1で接続された約131メンバーの`Global.X`参照を、注入された`IProfileSettingsService`インスタンス経由の参照に置換する。
- Stage1の対象外メンバー（§0.3）は本Stageでも対象外のまま、`Global`経由（`[Legacy]`ログ）を維持する。

### 3.2 想定される規模
- `ProfileSettingsViewModel.cs`: 581箇所中、Stage1対象メンバーに該当する約550箇所前後が置換対象候補（正確な件数はStage1完了後に再集計）。
- `Mapping.cs`: 91箇所中、`Clamp`等の定数・純粋関数（約50箇所）を除いた残り、Stage1対象メンバーに該当する箇所。
- `ControlService.cs`: 136箇所中、Stage1対象メンバーに該当する箇所（旧版で先行対象とした5箇所を含む）。

### 3.3 リスクの高さについての事前所見
`ProfileSettingsViewModel.cs`は581箇所と規模が大きく、1ファイルへの一括変更はレビュー困難・回帰リスクが高い。Stage2は**サブタスク分割方式（Stage1と同じUIカテゴリ単位）**で進めることを基本方針とし、Stage1完了後に改めて詳細なタスク分割表を作成する。

---

## 4. 成果物一覧（Stage1範囲）

| ファイルパス | 種別 | 内容 |
|---|---|---|
| `DS4Windows/DI/IProfileSettingsService.cs` | 更新 | 約131メンバー分のプロパティ・メソッド宣言追加 |
| `DS4Windows/DS4Control/Services/ProfileSettingsService.cs` | 更新 | `BackingStore`参照追加、委譲実装追加 |
| `DS4Windows/DS4Control/ScpUtil.cs` | 更新 | 対象メンバーの1行委譲化、`[Legacy]`ログ追加 |
| `docs-forDIMG/MadeByAgent/Phase4-Step10-2-Global-Member-Mapping.md` | 新規 | 対象131メンバーの正式対応表（`Global`メンバー名⇔`IProfileSettingsService`メンバー名⇔サブタスク番号） |
| `docs-forDIMG/MadeByAgent/Phase4-Step10-2-Plan.md` | 更新 | 本計画書（旧版を置き換え） |
| `docs-forDIMG/MadeByAgent/Phase4-Step10-2-A-Completion-Report.md` | 新規 | Stage1完了報告書 |
| `docs-forDIMG/MadeByAgent/Phase4-Step10-2-A-RealDevice-Verification-Checklist.md` | 新規 | Stage1実機検証チェックリスト |
| `docs-forDIMG/MadeByAgent/Phase4-Status.md` | 更新 | Step10-2の位置づけ・進捗を反映 |

Stage2の成果物一覧は、Stage1完了後に別途計画書として提示する。

---

## 5. リスクと回避策

| リスク | 該当タスク | 回避策 |
|---|---|---|
| 131メンバーの機械的追加における命名・型の写し間違い | Step10-2-A-1〜9 | `Global`側の元宣言と`m_Config`側フィールド型を1件ずつ突き合わせて確認する。サブタスク単位でビルド確認を挟む。 |
| `m_Config`が将来的に非singleton化された場合の追従漏れ | Step10-2-A全体 | 現状`m_Config`はGlobal内の単一static参照であり、本Stepでは変更しない前提を明記。将来変更時は本ドキュメントの前提を再確認する。 |
| サブタスクの粒度が細かすぎて完了までのセッション数が多くなる | Step10-2-A全体 | 9サブタスクに分割済み。各セッションで1〜3サブタスク程度を目安に進行し、都度`Phase4-Status.md`に進捗を記録する。 |
| Stage1未完了のままStage2に誤って着手するリスク | Stage2 | §2.1の1機能1経路原則に基づき、Stage1の完了判定基準（§2.4）を満たすまでStage2のコード変更に着手しない。 |

---

## 6. 今後の課題（本Stage1・Stage2の対象外）

§0.3で切り出した以下は、Step10-2完了後の別ステップ候補として記録する。

- `IProfileRepository`側の`ApplyProfile`/`SaveControllerConfigs`/`LoadControllerConfigs`のシム接続（3-c層、プロファイル切替実行）
- `ISpecialActionRepository`側の`ProfileActions`/`RefreshActionAlias`/`CacheProfileCustomsFlags`のシム接続
- `IDeviceStateService`・`IOutputSlotService`・`IPathService`・`IEnvironmentService`・`INotificationService`の実データ接続（現状これらは`Global`の実データと接続されていない空の並行状態を持つのみ。Step4/Step5で作成されたが未接続のまま）
- `Global.outputKBMHandler`（`IVirtualKBM`領域、全体計画書フェーズ2該当）

---

## 7. 完了判定基準（本計画書全体）

- [ ] Stage1（Step10-2-A）が§2.4の基準をすべて満たしている。
- [ ] Stage2（Step10-2-B）の詳細計画書がStage1完了後に作成され、承認を得ている。
- [ ] Stage2実施後、`ControlService.cs`／`Mapping.cs`／`ProfileSettingsViewModel.cs`のログ出力において、Stage1対象メンバーに関する処理がすべて`[DI]`表示になっていることを実機ログで確認する。
- [ ] `Phase4-Status.md`にStage1・Stage2それぞれの完了が反映されている。
