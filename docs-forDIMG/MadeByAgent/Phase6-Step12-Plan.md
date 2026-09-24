# Phase6-Step12 計画書: 呼出元0件旧静的シムの安全防壁付き物理削除とPhase6最終完了判定

作成日: 2026-09-09  
改訂日: 2026-09-18（Step2〜Step10確定スコープ・Step11実機総合検証成果統合・選択肢2［安全防壁付き物理削除］採用・全面拡充改訂）  
状態: 計画書改訂・承認待ち（実装未着手）  
対象ブランチ: `For-DI-migration-work`  
上位計画書: `docs-forDIMG/MadeByAgent/Phase6-Plan.md`  
準拠指針: `.github/copilot-instructions.md`, `docs-forDIMG/DI-App-Wide-Migration-Plan.md` §6.12  
参照エビデンス:  
  - `Phase6-Step1-Core-Reference-Evidence.md`（Core層・Mapping層 監査記録）  
  - `Phase6-Step1-UI-Reference-Evidence.md`（UI層・ViewModel層 監査記録）  
  - `Phase6-Step2-Plan.md` 〜 `Phase6-Step10-Plan.md`（各ステップ確定実装計画書）  
  - `Phase6-Step11-Plan.md`（第1層自動テスト ＆ 第2層実機総合検証計画）  

---

## 0. 背景と方針確定（選択肢2: 安全防壁付き物理削除の採用）

### 0.1 Step2〜Step11 の完了実績と残存コードの状況
Phase6 では、Step2〜Step10 においてアプリケーション全域の Global 直接参照（実参照式約404箇所、除外・引き継ぎを含めると500箇所以上）をコンストラクタ経由の Pure DI サービス呼び出しへ完全置換した。  
さらに Step11 において、自動回帰テストスイートの全件合格と、実機デバイスマトリクス（Flydigi Vader 4 Pro / DS4 / DualSense）による 10大構造化シナリオ検証を完了した。

この結果、巨大な神静的クラスである `ScpUtil.cs`（約3,500行、`Global` クラス）の中には、**「呼び出し元が完全に消滅し、どこからも使われなくなった旧静的ラッパーメソッド・旧プロパティ」**が大量に発生している。

### 0.2 方針の確定: 【安全防壁付きの物理削除（洗練された選択肢2）の採用】
Phase7 において `Mapping.cs`（約8,500行）の完全インスタンス化と `ScpUtil.cs` の最終解体が予定されているが、**「Phase6 の成果として、今消せる不要なデッドコードを物理削除し、`ScpUtil.cs` を安全にスリム化する」** 方針を採用する。

ただし、`ScpUtil.cs` の中には、**消すと DI サービスや残存ホットパスが即座に崩壊する重要コンポーネント**が存在するため、以下の【5大保護防壁（削除禁止リスト）】を厳格に定義し、安全性を 100% 担保した上で純粋な呼出元0件ラッパーのみを行削除（物理削除）する。

---

## 1. 5大保護防壁（物理削除の絶対禁止リスト）

以下の5領域に属するコードは、呼出元判定の如何に関わらず、**本ステップでの物理削除を絶対に禁止**する：

1. **防壁1: `BackingStore` の全フィールド・配列・プロパティ（正本データストレージ）**:  
   - Phase4〜Phase6 で構築された各 DI サービス（`ProfileSettingsService`, `AppSettingsService` 等）は、データの二重管理・乖離を防ぐため、内部で `BackingStore` のメモリ領域を直接参照・操作している（SSOT の原則）。これを消すと DI サービスごとアプリが崩壊するため、**Phase7 まで完全温存**する。
2. **防壁2: Phase7 引き継ぎメンバ（`Mapping.cs` が参照している101箇所）**:  
   - Step3 で台帳化された `SetCurveAndDeadzone`（34件）、`outputKBMMapping`（29件）、スティック・ボタン・ジャイロ補正（38件）は、Phase7 のインスタンス化まで `Global` を参照し続けるため、**一切削除しない**。
3. **防壁3: Pre-Host ブートストラップ領域（`App.xaml.cs` 起動前の12箇所。2026-09-25 に Step7 で再集計、`Phase6-Step7-Plan.md` §2.5。あわせて共用ヘルパー `ApplyLanguageSetting` の2件［`UseLang`・`SetCulture`］と `AttemptSave` の `appdatapath = null` を TODO 付きで温存）**:  
   - DI コンテナ構築前に実行される初期ログローテーション（`appdatapath`, `LogMaxArchiveFiles`, `LogMinLevel`）、多重起動判定、設定位置検索（`FindConfigLocation`）、および `-driverinstall` 分岐は、**静的呼び出しのまま完全温存**する。
4. **防壁4: 真の定数（`const`）および純粋計算ユーティリティ**:  
   - `MAX_DS4_CONTROLLER_COUNT`, `OLD_XINPUT_CONTROLLER_COUNT`, `TEST_PROFILE_INDEX`, `ASSEMBLY_RESOURCE_PREFIX`, `RESOURCES_PREFIX` 等の定数、および `Clamp`, `getTransitionedColor` などの状態非保持ユーティリティは、4層共通インフラとして**完全温存**する。
5. **防壁5: ViGEm クライアント直接依存（Phase6/7対象外）**:  
   - `RefreshViGEmBusInfo`, `IsRunningSupportedViGEmBus`, `vigembusVersion`, `vigemInstalled` は、将来の仮想バス刷新イニシアチブまで**完全温存**する。

---

## 2. 物理削除の対象（スリム化のスコープ）

Step2〜Step10 の DI 化によって「呼び出し元が完全に 0 件」となり、かつ上記【5大保護防壁】に含まれない、**純粋な旧静的ラッパーメソッド・旧プロパティ**を物理削除する。

### 削除候補の代表例（全リポジトリ走査で0件確認後に削除）:
- **旧アプリ設定ラッパー**:
  - `Global.getUseExclusiveMode()`, `Global.setUseExclusiveMode()`
  - `Global.getFlashWhenLate()`, `Global.setFlashWhenLate()`
  - `Global.getFlashWhenLateAt()`, `Global.setFlashWhenLateAt()`
  - `Global.getQuickCharge()`, `Global.setQuickCharge()`
  - `Global.DCBTatStop`（旧静的アクセサ）
  - `Global.ProcessPriority`（旧静的アクセサ）
  - `Global.CustomSteamFolder`, `Global.FakeExeName`（旧静的アクセサ）
  - `Global.UseAbsoluteEDID`（旧静的アクセサ）
- **旧サーバー・通信設定ラッパー**:
  - `Global.isUsingOSCServer()`, `Global.isUsingOSCSender()`
  - `Global.getOSCServerPortNum()`, `Global.getOSCSenderPortNum()`, `Global.getOSCSenderAddress()`
  - `Global.isInterpretingOscMonitoring()`
  - `Global.isUsingUDPServer()`, `Global.getUDPServerPortNum()`, `Global.getUDPServerListenAddress()`
  - `Global.IsUsingUDPServerSmoothing`, `Global.UDPServerSmoothingMincutoff`, `Global.UDPServerSmoothingBeta`
- **旧プロファイル・デバイス補助ラッパー**:
  - `Global.IsFirstConnection()`, `Global.MarkConnected()`（旧静的シム）
  - `Global.containsLinkedProfile()`, `Global.getLinkedProfile()`（旧静的シム）
  - `Global.RefreshExtrasButtons()`, `Global.containsCustomAction()`, `Global.containsCustomExtras()`（旧静的シム）
  - `Global.LoadBlankDevProfile()`（旧静的シム）
  - `Global.ResetConnectionFlags()`（旧静的シム）
  - `Global.LoadActions()`, `Global.CreateStdActions()`（旧静的シム）
- **不要となった旧イベントシム・フォールバックプロパティ**:
  - その他、Roslyn / grep 静的走査によって呼出元が真に 0 件と証明されたプライベート/パブリック旧ラッパー群。

### Step7 からの登録（2026-09-25）
- **削除候補（`copilot-instructions.md` §2.4 で「使うべきでないもの」と判断、`Phase6-Step7-Plan.md` 決定3＝P1）**:
  - `IPathService.AppDataPath` のセッター（`DI/IPathService.cs` の `{ get; set; }` を `{ get; }` にする）と、`PathService.AppDataPath` のセッター（`DS4Control/Services/PathService.cs`）。
  - 理由: セッターは private フィールド `_customAppDataPath` に保存するだけで、`Global.appdatapath` を変えない（`Global` と連動しない孤立した実装）。アプリ本体・テストとも呼出元 0 件（2026-09-24 確認）。`App.xaml.cs` の `AttemptSave` の `Global.appdatapath = null` の置き換え先にもならず、そちらは TODO 付きで温存した。
  - 注意: `_customAppDataPath` 自体は、テスト用のコンストラクタ引数 `PathService(string appDataPath)` でも使うため残す。削除の前に、呼出元が 0 件のままであることを再確認する。
- **削除候補から外れるもの（注記）**: 上の一覧の `Global.ResetConnectionFlags()`、`Global.LoadActions()`、`Global.CreateStdActions()` は、Step7 で `App.xaml.cs` からの直接呼び出しがなくなったが、DI サービスの委譲先として使われ続ける（`DeviceStateService.ResetConnectionFlags`、`SpecialActionRepository.CreateStandardActions`、`SpecialActionRepository.LoadActions` の `_config` が null の場合）。呼出元 0 件にはならないため、Step12-1 の走査で削除対象にならないことを確認する。

---

## 3. PR分割計画とマイクロステップ

安全防壁を確実に機能させ、コンパイルとテストの整合性を維持するため、以下の**4つのサブPR（マイクロステップ）**で進める。

```text
【Phase6-Step12 マイクロステップ構成】
├─ Step12-0: 着手前提検証（Step11 実機検証合格レポート・全テストグリーン確認）
├─ Step12-1 (PR-1): 全走査スクリプト実行 ＆ 削除対象確定台帳の作成（実装なし）
├─ Step12-2 (PR-2): 呼出元0件ラッパーの安全な物理削除（ScpUtil.cs スリム化）
├─ Step12-3 (PR-3): 全自動回帰テスト再実行 ＆ ビルド・テスト完全証明
└─ Step12-4 (PR-4): Phase6 最終完了報告書（Phase6-Completion-Report.md）の作成
```

---

### Step12-0: 着手前提検証（実装なし）
1. Step11 の成果物である `Phase6-Step11-RealDevice-Verification-Checklist.md` において、全10セクションが合格（○）であり、総合判定が「GO（認可）」であることを確認。
2. 作業ツリーのクリーン状態、HEAD コミットハッシュを記録。
3. `dotnet test` を実行し、ベースラインとして全テスト合格を再確認。

---

### Step12-1 (PR-1): 全走査スクリプト実行 ＆ 削除対象確定台帳の作成
- **作業内容**:
  1. Roslyn または専用 PowerShell スクリプトを実行し、`Global.` の全シンボルに対するソリューション全体の参照カウントを走査。
  2. 参照カウント 0 件のメンバを抽出。
  3. 第1章の【5大保護防壁】と機械的に突き合わせ、保護対象を厳格に除外。
  4. 物理削除を実行する安全なメンバの一覧を `Phase6-Step12-Deletion-Candidates-Report.md` として出力・確定。
- **検証**:
  - 削除候補リストの中に、`BackingStore`、`Mapping.cs` 参照分（101件）、Pre-Host 分（11件）が一切混入していないことを目視および差分突合で二重チェック。

---

### Step12-2 (PR-2): 呼出元0件ラッパーの安全な物理削除
- **対象**: `DS4Control/ScpUtil.cs`
- **作業内容**:
  1. PR-1 で承認された台帳に基づき、対象メンバ（メソッド・プロパティ）のコードブロックを物理削除（行削除）する。
  2. `dotnet build -c Release` を実行し、コンパイルエラーが 0 件であることを確認。
- **検証**:
  - 削除後の `ScpUtil.cs` において、意図した行数削減が達成され、かつ構文エラー・未解決シンボルエラーが皆無であることを確認。

---

### Step12-3 (PR-3): 全自動回帰テスト再実行 ＆ ビルド・テスト完全証明
- **作業内容**:
  1. ソリューション全体のクリーンビルド（`dotnet clean` → `dotnet build -c Release`）を実行。
  2. `DS4Windows.Actions.Tests`、`StandaloneTests`、および Phase6 新規単体テスト全件（`dotnet test`）を実行。
  3. リフレクションや動的バインディングによるランタイムクラッシュがないことを、テスト実行ログで完全証明。
- **検証**:
  - 全自動テスト 100% 成功（Green、失敗ゼロ）を確認。

---

### Step12-4 (PR-4): Phase6 最終完了報告書の作成
- **作業内容**:
  1. `Phase6-Step12-Completion-Report.md` を作成。
  2. 削減された行数、削除されたメンバ数、残存した保護メンバ数（Phase7引き継ぎ分 101件など）を集計。
  3. `docs-forDIMG/MadeByAgent/Phase6-Completion-Report.md` を作成し、Phase6 全体の目的（Global 直参照根絶）が完全に達成されたことを宣言。
  4. Phase7（Mapping.cs 完全インスタンス化 ＆ ScpUtil 最終解体）への正式な引き継ぎセクションを記録。

---

## 4. Phase7 への最終引き継ぎ台帳（ScpUtil 最終解体ロードマップ）

本ステップ完了後、`ScpUtil.cs` に残存するメンバは以下の通り整理され、Phase7 で最終的に完全解体・消滅する：

| 残存コンポーネント | 残存理由 | Phase7 における最終解体ロードマップ |
|---|---|---|
| **`BackingStore`** | DI サービスの正本データストレージ | Phase7 で各ドメインエンティティ（ProfileEntity 等）へ完全移譲し、BackingStore 自体を消去 |
| **`Mapping.cs` 参照分（101箇所）** | スティック・ボタン・KBM のホットパス | Phase7 で `Mapping.cs` をドメインサービス群へ分割・インスタンス化する際に一括消去 |
| **Pre-Host 領域（11箇所）** | DI コンテナ構築前の初期ブートストラップ | Phase7 で `Program.cs` / `App.xaml.cs` の初期起動シーケンスを近代化する際に整理 |
| **`Global.outDevTypeTemp`／`Global.activeOutDevType`**（実行時状態の静的配列） | `OutputSlotService.OutDevTypeTemp`／`ActiveOutDevType` が裏づけとして返している。`ScpUtil.cs` 8 箇所・UI 3 箇所・`ControlService`（参照キャッシュ）が直接読み書きしている（Step5 の決定2＝案X で温存を確定） | Phase7 で配列の所有を `OutputSlotService` へ移し、`Global` 側を転送プロパティ（または削除）にする（Step5 の案Y）。前提条件は `Phase6-Step5-Plan.md` §9 を参照 |
| **DI ホストの暗黙構築（持ち越し K7-1、2026-09-25 登録）** | `Global` の静的初期化子（`ScpUtil.cs:779` の `fallbackProfileRepository = new ProfileRepository(ProfileSettingsServiceInstance)`）が `AppHost.GetService` を呼ぶため、`App.xaml.cs` の明示的な `CreateHost(config, parser)` より前に、起動引数なしでホストが作られる。起動引数は Step7-4（決定7＝案H）で `IStartupArguments` 経由にして回避済み | Phase7 で `Global` の静的初期化を整理する際に、静的初期化がホストを作らないようにし、明示的な構築を本当の最初の構築にする（`Phase6-Step7-Plan.md` 決定7 の案R、`Phase6-Status.md` §6.5 K7-1） |
| **`Program.rootHub`（静的な入口）と `App.xaml.cs` の Service Locator（Step7 決定6、2026-09-25 登録）** | `App.xaml.cs` が `Program.rootHub` の所有者（`CreateControlService` で代入）で、App 内の参照は代入を含めて 12 行（2026-09-25 時点）。`CleanShutdown` は `ServiceProviderHolder.Provider` から `IControllerRegistry` を都度解決する。`Program.rootHub` は App を含めて 19 ファイル（2026-09-25 時点）が参照する共有の入口のため、Step7 では変更しなかった（`Phase6-Step7-Plan.md` §2.7・決定6） | Pure DI の方針に沿う形へ変更する。`App` は Composition Root としてコンテナから `ControlService`・`IControllerRegistry` を受け取り（`InitializePostHostServices` に追加）、各利用側は Step8〜10 で View／ViewModel を Pure DI 化する際にコンストラクタ注入へ切り替える。すべての参照がなくなった時点で `Program.rootHub` を削除する（Phase7。K7-1 の起動シーケンス整理と合わせて行う） |
| **真の定数（`const`）** | 共通定数値 | ドメインごとの定数クラス（`ControllerConstants` 等）へ再配置 |

---

## 5. ロールバック方針

万が一、物理削除後に予期せぬビルドエラーやランタイムエラーが発生した場合は、以下の手順で直ちにロールバックを行う：
1. **即時 revert**: Step12-2（PR-2）の削除コミットを直ちに `git revert` し、Step11 完了時の健全な状態へ復帰する。
2. **原因の単離**: どのメンバが動的参照されていたかをスタックトレースから特定し、そのメンバを保護リストへ追加した上で、PR-1 の台帳を再生成する。

---

## 6. 完了判定チェックリスト（Phase6 全体の完了ゲート）

- [ ] 第1章に定義された【5大保護防壁】（BackingStore、Phase7引き継ぎ101件、Pre-Host11件、const定数、ViGEm依存）が完全に保護されていること。
- [ ] 物理削除されたすべてのメンバが、全リポジトリ走査において呼出元 0 件であることが確認されていること。
- [ ] `ScpUtil.cs` から不要な旧ラッパーが物理削除され、コードのスリム化が達成されていること。
- [ ] `dotnet clean` および `dotnet build -c Release` でエラー・警告が 0 件であること。
- [ ] `Actions.Tests` および `StandaloneTests` を含む全自動テストが 100% 成功していること。
- [ ] 第4章に定義された Phase7 への最終引き継ぎ台帳が完全に記録されていること。
- [ ] `Phase6-Step12-Completion-Report.md` および `Phase6-Completion-Report.md` が作成され、**Phase6 の完全成功（Global 直参照の根絶）**が正式に宣言されていること。