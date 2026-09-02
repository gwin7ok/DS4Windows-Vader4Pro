# フェーズ5-Step6 計画書: 残存サービス境界の整理

作成日: 2026-09-02
対象ブランチ: `For-DI-migration-work`
前提ドキュメント:
- `docs-forDIMG/DI-App-Wide-Migration-Plan.md`（全体計画書・全体4層モデル定義）
- `docs-forDIMG/MadeByAgent/Phase5-Plan.md` §2, §3 Step6（Phase5詳細計画書）
- `docs-forDIMG/MadeByAgent/Phase5-Status.md`（Phase5進捗管理）
- `docs-forDIMG/MadeByAgent/Phase5-Step1-legacy-delegation-audit-report.md`（Step1監査結果。本Stepの対象根拠）
- `.github/copilot-instructions.md`（エージェント作業ルール）

---

## ルール確認（作業開始前に毎回読む）

- **§2.1 フォールバック実装・シム維持の原則**:
  - `Global.appdatapath`／`Program.rootHub`への参照は、代替経路が動作確認できるまで併存させ、一度に置き換えない。
- **§2.2 現在の機能の完全維持 (No Feature Drop)**:
  - `ProfileSettingsService`が公開する既存プロパティ・メソッドの挙動（`_config`＝`Global.store`への委譲）を変更しない。本Stepの対象は`Program.rootHub`直接参照の2箇所と、`PathService`のキャッシュ挙動のみに限定する。
- **§2.3 ログ出力の厳格な維持**:
  - 既存の`[DI]`プレフィックス付きログを維持する。
- **§3.1 DI (Dependency Injection) の実装**:
  - `Program.rootHub`への直接参照は、Phase3で導入済みの`DS4Windows.Services.IDeviceStateAccessor`経由に置換する。

---

## 0. Step6の位置づけと現状分析

### 0.1 Step1監査結果に基づく対象範囲
`Phase5-Step1-legacy-delegation-audit-report.md` §2, §3 に基づき、以下を対象とする。

- `IPathService`→`PathService`（#13）: `Global.appdatapath`読み取り（軽微）
- `IProfileSettingsService`→`ProfileSettingsService`（#6）: `Program.rootHub.DS4Controllers[]`参照
- 「デバイス状態」（`IDeviceStateService`）: Step1監査でクリーンと確認済み。**本Stepの対象外**として扱う。
- 「KBMアダプター」: `ServiceRegistration.cs`に該当するDI登録が見当たらず、DI未登録のまま使用されている可能性がある（Step1監査で要確認事項として記録）。

### 0.2 `PathService`の現状（GitHub実コード確認済み）
`PathService.AppDataPath`は、初回アクセス時に`Global.appdatapath`（存在しなければ`AppContext.BaseDirectory`）を読み取り、`_appDataPath`フィールドにキャッシュする実装になっている。

```csharp
public string AppDataPath
{
    get
    {
        lock (_syncLock)
        {
            if (string.IsNullOrWhiteSpace(_appDataPath))
            {
                _appDataPath = !string.IsNullOrEmpty(Global.appdatapath)
                    ? Global.appdatapath
                    : AppContext.BaseDirectory;
                ...
            }
            return _appDataPath;
        }
    }
    ...
}
```

**【発見した潜在的リスク】** `Global.appdatapath`はアプリ起動時に`Global.FindConfigLocation()`→`Global.SaveWhere(path)`で設定される。もし`PathService.AppDataPath`が`FindConfigLocation()`実行**前**に一度でも参照されると、`Global.appdatapath`がまだ空の状態でキャッシュされ（`AppContext.BaseDirectory`にフォールバック）、その後`FindConfigLocation()`が正しいパスを設定しても`PathService`側のキャッシュは古いままになる可能性がある。本Stepでこの初期化順序の安全性を検証する。

### 0.3 `ProfileSettingsService`の`Program.rootHub`直接参照（GitHub実コード確認済み）
`ProfileSettingsService`はほぼ全てのプロパティを`_config`（コンストラクタで`Global.store`を既定注入）への委譲として実装しており、`_config`は`Global.store`と同一インスタンスを参照するため「二重管理」の問題はない（Step5で発見した`SpecialActionRepository`の問題とは性質が異なる）。

唯一問題となるのは、以下2箇所が`Program.rootHub`（静的シングルトン）を直接参照している点である。

```csharp
public byte GetRumbleBoost(int deviceIndex)
{
    if (Program.rootHub.DS4Controllers[deviceIndex] is DualSenseDevice &&
        !UseGenericRumbleStrRescaleForDualSenses[deviceIndex])
        return 100;
    return _config.rumble[deviceIndex];
}

public void SetRumbleAutostopTime(int index, int value)
{
    _config.rumbleAutostopTime[index] = value;
    DS4Device tempDev = Program.rootHub.DS4Controllers[index];
    if (tempDev != null && tempDev.isSynced())
        tempDev.RumbleAutostopTime = value;
}
```

Phase3で`DS4Windows.Services.IDeviceStateAccessor`（`ControlService`へのDIラッパー）が導入済みであり、これを`ProfileSettingsService`のコンストラクタに注入することで`Program.rootHub`への直接依存を置換できる。

### 0.4 「KBMアダプター」のDI未登録疑いの確認
`ServiceRegistration.cs`には`VirtualKBMBase`／`VirtualKBMMapping`関連のDI登録が存在しない。`Global.outputKBMHandler`／`Global.outputKBMMapping`は現在も静的フィールドとして直接管理されている。本Stepの調査タスクで実装ファイルの所在と現在の初期化経路（`Global.InitOutputKBMHandler`／`InitOutputKBMMapping`）を確認し、DI化の要否・難易度を評価する。

### 0.5 全体4層モデルにおける位置づけ
`PathService`／`ProfileSettingsService`は第4層 4-c、KBM関連は出力処理を担うため第3層（信号出力層）寄りの性質を持つ。本Stepはこれら残存する個別の依存関係を整理する、Phase5前半（Step2〜5）の総仕上げに相当する。

---

## 1. 設計方針とアーキテクチャ

### 1.1 `PathService`の初期化順序安全化
`PathService.AppDataPath`が`Global.FindConfigLocation()`実行前に参照された場合でも正しい値を返せるよう、以下いずれかの対応を検討する。

- (a) キャッシュを行わず、毎回`Global.appdatapath`を参照する（既存の`Global.appdatapath`変更検知は不要になるが、パフォーマンス上のキャッシュの利点を失う）。
- (b) `Global.SaveWhere`実行時に`PathService`のキャッシュを無効化するフックを追加する（`Global`→DIサービスへの逆方向通知が必要になり複雑度が増す）。
- (c) アプリ起動シーケンス（`App.xaml.cs`等）で`Global.FindConfigLocation()`が`PathService`の初回アクセスより確実に先に実行されることを保証し、現状維持とする。

タスクStep6-1の起動シーケンス調査結果を踏まえて(a)〜(c)のいずれかを選択する。

### 1.2 `ProfileSettingsService`の`IDeviceStateAccessor`経由への置換
コンストラクタに`DS4Windows.Services.IDeviceStateAccessor`を追加注入し、`GetRumbleBoost`／`SetRumbleAutostopTime`内の`Program.rootHub.DS4Controllers[index]`を`_deviceState.GetController(index)`（Phase3で確認済みのメソッド）に置換する。

```csharp
public ProfileSettingsService(BackingStore config = null, DS4Windows.Services.IDeviceStateAccessor deviceState = null)
{
    _config = config ?? Global.store;
    _deviceState = deviceState; // 未指定時はGetRumbleBoost等で従来のProgram.rootHubへフォールバック（過渡期シム）
}

public byte GetRumbleBoost(int deviceIndex)
{
    DS4Device device = _deviceState != null
        ? _deviceState.GetController(deviceIndex)
        : Program.rootHub.DS4Controllers[deviceIndex]; // フォールバック
    if (device is DualSenseDevice && !UseGenericRumbleStrRescaleForDualSenses[deviceIndex])
        return 100;
    return _config.rumble[deviceIndex];
}
```

DIコンテナ経由で生成される通常経路では`_deviceState`が必ず注入されるため、フォールバックは`Global.ProfileSettingsServiceInstance`のフォールバックインスタンス生成時（DIコンテナ未初期化時）のみ使用される。

### 1.3 KBMアダプターのDI化方針（調査結果次第）
タスクStep6-2の調査で対象クラスが特定でき、かつ影響範囲が本Stepの範囲内に収まる場合は`IOutputKBMService`（仮称）としてDI登録する設計を行う。影響範囲が大きい場合は、本Stepでは「現状維持＋今後の対応方針の文書化」に留め、必要であれば独立Stepとして切り出す。

---

## 2. 成果物一覧

| ファイルパス | 種別 | ライフサイクル | 内容 |
|---|---|---|---|
| `DS4Windows/DS4Control/Services/PathService.cs` | 更新 | **DI永続資産** | 1.1節の対応（キャッシュ安全化） |
| `DS4Windows/DS4Control/Services/ProfileSettingsService.cs` | 更新 | **DI永続資産** | `IDeviceStateAccessor`注入、`Program.rootHub`直接参照の置換（2箇所） |
| `DS4Windows/DI/ServiceRegistration.cs` | 確認・必要に応じ更新 | **DI永続資産** | KBMアダプターのDI登録（1.3節の結果次第） |
| `docs-forDIMG/MadeByAgent/Phase5-Step6-KBM-Investigation-Report.md` | 新規 | ドキュメント | タスクStep6-2のKBM調査結果 |
| `DS4WindowsTests/PathServiceTests.cs` | 新規 | **テスト資産** | 初期化順序（`FindConfigLocation`前後）でのAppDataPath解決の単体テスト |
| `DS4WindowsTests/ProfileSettingsServiceDeviceTests.cs` | 新規 | **テスト資産** | `GetRumbleBoost`／`SetRumbleAutostopTime`が`IDeviceStateAccessor`経由で正しくデバイスを取得することの単体テスト |
| `docs-forDIMG/MadeByAgent/Phase5-Step6-Plan.md` | 新規 | ドキュメント | 本計画書 |
| `docs-forDIMG/MadeByAgent/Phase5-Step6-Completion-Report.md` | 新規 | ドキュメント | Step6完了報告書 |
| `docs-forDIMG/MadeByAgent/Phase5-Status.md` | 更新 | ドキュメント | Step6進捗ステータス更新 |

---

## 3. 作業手順（マイクロタスク分割）

### タスク Step6-1: アプリ起動シーケンスの調査（`PathService`初期化順序）
- `App.xaml.cs`／`AppHost.cs`等を確認し、`Global.FindConfigLocation()`の実行タイミングと、DIコンテナ構築・`PathService`初回アクセスの前後関係を特定する。
- 1.1節の(a)〜(c)のいずれで対応するかを決定し記録する。

### タスク Step6-2: KBMアダプターの所在・DI登録状況の調査
- `VirtualKBMBase`／`VirtualKBMFactory`／`VirtualKBMMapping`関連の実装ファイルを特定する。
- 現在の初期化経路（`Global.InitOutputKBMHandler`／`InitOutputKBMMapping`の呼び出し元）を確認する。
- DI化の要否・影響範囲を評価し、`Phase5-Step6-KBM-Investigation-Report.md`に記録する。

### タスク Step6-3: `PathService`のキャッシュ安全化実装
- タスクStep6-1の結論に基づき、`PathService.AppDataPath`を修正する。

### タスク Step6-4: `ProfileSettingsService`の`Program.rootHub`直接参照の置換
- コンストラクタに`IDeviceStateAccessor`を追加注入する。
- `GetRumbleBoost`／`SetRumbleAutostopTime`内の`Program.rootHub.DS4Controllers[index]`を置換する。
- `Global.ProfileSettingsServiceInstance`のフォールバックインスタンス生成箇所（`ScpUtil.cs`内）が新しいコンストラクタシグネチャと整合することを確認する。

### タスク Step6-5: KBMアダプターのDI化（調査結果に基づき実施可否を判断）
- タスクStep6-2の結果が「本Stepで対応可能」の場合のみ実施する。対応不可の場合は本タスクをスキップし、完了報告書にその理由と今後の扱いを記録する。

### タスク Step6-6: 単体テスト作成と自動テスト実行
- `PathServiceTests.cs`／`ProfileSettingsServiceDeviceTests.cs`を作成する。
- 既存回帰テスト（`DS4WindowsTests`／`StandaloneTests`）が全件通過することを確認する。

### タスク Step6-7: ビルド検証、進捗更新、完了報告書の作成
- `dotnet build DS4WindowsWPF.sln --nologo` を実行し警告0・エラー0を確認する。
- `Phase5-Status.md`のStep6欄を更新し、`Phase5-Step6-Completion-Report.md`を作成する。

---

## 4. リスクと回避策

| リスク | 該当タスク | 回避策 |
|---|---|---|
| `PathService`のキャッシュ方式変更（1.1(a)採用時）により、頻繁な`Global.appdatapath`参照でパフォーマンスがわずかに低下する | Step6-3 | `AppDataPath`の呼び出し頻度が高くないこと（プロファイル読込・保存時のみ）を確認した上で採用する。 |
| `ProfileSettingsService`のコンストラクタシグネチャ変更により、Legacyフォールバックインスタンス生成箇所（`ScpUtil.cs`の`fallbackProfileSettingsService`）でコンパイルエラーが発生する | Step6-4 | 新しいパラメータをオプション引数（既定値`null`）とし、フォールバック生成箇所は無修正で動作するようにする。 |
| KBMアダプターのDI化が想定より大規模（`VirtualKBMFactory`のstatic状態や`outputKBMHandler`のライフサイクル管理が複雑）である | Step6-2, Step6-5 | 影響範囲が大きいと判明した場合は本Stepでの実装を見送り、調査結果のみを記録して独立Stepとして切り出す（Phase5-Plan.md／Phase5-Status.mdへの追記が必要になるため、切り出す場合は事前に確認を取る）。 |

---

## 5. 完了判定基準

- [ ] `PathService.AppDataPath`が、アプリ起動シーケンス上のどのタイミングでアクセスされても正しい値を返すことが確認・保証されている。
- [ ] `ProfileSettingsService`の`GetRumbleBoost`／`SetRumbleAutostopTime`が`IDeviceStateAccessor`経由でデバイスを取得するよう置換され、`Program.rootHub`への直接参照が排除されている（DIコンテナ未初期化時のフォールバックを除く）。
- [ ] KBMアダプターのDI登録状況・調査結果が`Phase5-Step6-KBM-Investigation-Report.md`に記録され、対応可否が判断されている。
- [ ] 新設した`PathServiceTests`／`ProfileSettingsServiceDeviceTests`および既存の全回帰テスト（`DS4WindowsTests`／`StandaloneTests`）が成功する。
- [ ] ソリューションビルドが警告0・エラー0で成功する。
- [ ] `Phase5-Status.md`が更新され、`Phase5-Step6-Completion-Report.md`が作成されている。