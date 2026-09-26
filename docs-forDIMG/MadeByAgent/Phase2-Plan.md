# フェーズ2 計画書: KBM出力の抽象化（IVirtualKBM）

作成日: 2026-08-27
対象ブランチ: For-DI-migration-work
前提ドキュメント:
- `docs-forDIMG/DI-App-Wide-Migration-Plan.md` §6.4（フェーズ2の元定義）
- `docs-forDIMG/MadeByAgent/Phase1-Completion-Report.md`（フェーズ1完了報告）
- `docs-forDIMG/MadeByAgent/Phase1-Status.md`（フェーズ1進捗詳細）
- `.github/copilot-instructions.md`（本ファイルの§で参照する全ルール、および新設§5の外部エージェント運用ルール）

## ルール確認（作業開始前に毎回読む）
- §2.1 修正版: 古い方式を残して移行OK。新方式の動作確認後に削除。複数候補同時実装はNG。
- §2.2 機能100%維持、§2.3 ログ維持、§3.1 コンストラクタインジェクション、§3.2 巨大ファイルはピンポイント置換のみ、
  §4.1 マイクロステップ、§4.2 自己解決禁止、§4.3 ビルドエラー直ちに修正、§4.4 調査結果を.mdで文書化（本ファイル含む）。
- §5（新設）: 指示は段階的に実施し、1フェーズ完了ごとに確認を挟む／受け渡しzipはフォルダパス付きで生成する。
  → 本計画書もこの方針に従い、Step 2-1〜2-6 の**各ステップ完了ごとにチェックポイントを設ける**構成とする。
  特にStep 2-5（通常マッピング48箇所の置換）は影響範囲が大きいため、**着手前に必ずユーザーの明示的な
  承認を得ること**。

---

## 0. 着手前調査で判明した重要な事実（添付の想定計画との相違点）

Phase2着手にあたり、実コードを調査した結果、当初想定されていた計画（`Mapping.cs` や `DefaultMacroPlayer`、
`InputMethods.cs` が Win32 API `SendInput` を直接呼び出している、という前提）とは**異なる実態**が判明した。
これを踏まえて計画を修正する。

### 0.1 `VirtualKBMBase` という抽象化が既に存在する
`DS4Windows/DS4Control/OutputKBM/VirtualKBMBase.cs` に `public abstract class VirtualKBMBase` が既に定義されており、
`PerformKeyPress` / `PerformKeyRelease` / `PerformMouseButtonEvent` / `PerformMouseWheelEvent` /
`MoveRelativeMouse` / `MoveAbsoluteMouse` 等、KBM出力に必要な抽象メソッド一式を持つ。
具象実装として `SendInputHandler`（Win32 `SendInput` 経由）と `FakerInputHandler`（FakerInputドライバ経由）が
既に存在し、`VirtualKBMFactory.DetermineHandler(identifier)` が環境（`Global.fakerInputInstalled`）に応じて
どちらを使うか実行時に決定する。

→ **「KBM出力を抽象化する」作業は、ゼロから設計するのではなく、既存の `VirtualKBMBase` に
新設 `IVirtualKBM` インターフェースを実装させるだけで大部分が完了する。**
`SendInputVirtualKBM` のような実装をゼロから新設する必要はない（当初計画のStep 2-2は不要になる）。

### 0.2 `Mapping.cs` は `InputMethods`（生のWin32 SendInput）を直接呼んでいない
`InputMethods.cs`（`MoveCursorBy`, `performKeyPress` 等、生のWin32 API直呼び出し）を実際に参照しているのは
リポジトリ全体で **`MouseCursor.cs` の1ファイルのみ**であり、しかもその2箇所の呼び出しは
**コメントアウトされていて実行されていない**（死んだコード）。`Mapping.cs` は KBM出力に際して
一貫して `outputKBMHandler`（`Global.outputKBMHandler`、型は `VirtualKBMBase`）経由で呼んでいる。

→ `InputMethods.cs` はPhase2のスコープに含める必要がない（実質未使用のため、触る理由がない）。

### 0.3 `outputKBMHandler` の実際の参照範囲
`grep` による実測: `Global.outputKBMHandler`（`ScpUtil.cs` 774行目で宣言、`VirtualKBMBase outputKBMHandler = null`）を
参照しているファイルは10件、うち `Mapping.cs` 単体で **62箇所**（内訳: `PerformMouseButtonEvent`系30、
`PerformKeyPress/Release`系19、`PerformMouseWheelEvent`4、`MoveAbsoluteMouse`2、その他`Sync`/`fakeKeyRepeat`/
`MoveRelativeMouse`各1）。このうち **14箇所は `PlayMacro`（マクロ実行本体、6527-6942行）内**であり、
残り**48箇所は通常の1入力→1出力マッピング処理内**（`Mapping.cs` の中核データフロー、
`DI-App-Wide-Migration-Plan.md` の3層モデルでいう2-a/3-b相当）に存在する。

**この48箇所の扱いについて（2026-08-27改訂）**: 当初は「Phase2スコープ外、全体計画書§5.5の
『再評価チェックポイント』に委ねる」としていたが、ユーザーとの協議の結果、**Phase2内の独立した
ステップ（Step 2-5）として対応する**方針に変更した。全体計画書 `DI-App-Wide-Migration-Plan.md` §6.4も
同様に改訂済み。詳細は §1.2・§2・§3のStep 2-5を参照。

### 0.4 `outputKBMHandler` の生成タイミングに関する制約
`Global.outputKBMHandler` への実際の代入は `ScpUtil.cs` 3540行目 `outputKBMHandler = VirtualKBMFactory.DetermineHandler(identifier);`
で行われる。これは環境判定（`Global.fakerInputInstalled` 等）に依存し、**アプリ起動シーケンスの中盤**
（`AppHost` のHost構築より後、`ControlService` 初期化に近いタイミング）で実行される。
→ `AppHost.Initialize()`（Host構築時点）でDI登録を行っても、その時点では実体がまだ存在しない可能性が高い。
   単純に `services.AddSingleton<IVirtualKBM>(sp => Global.outputKBMHandler)` と登録すると、
   最初の解決時に `outputKBMHandler` が `null` のままキャッシュされてしまう危険がある。
   → **§1.4で後述する「遅延委譲アダプタ」方式で回避する。**

### 0.5 `MouseOutputAction.cs`（Phase1 C2成果物）は実質未配線のスタブだった
`DS4Windows/Actions/MouseOutputAction.cs` の `Execute()` は `AppLogger.LogTrace` を呼ぶのみで、
実際のマウス出力を一切行っていない。また `DefaultActionFactory.CreateFrom` の `switch` にも
対応する `case` が存在しない（`SpecialAction.ActionTypeId` 列挙体自体に `Mouse` という値は存在しない
— マウス出力は通常の1:1マッピング処理の一部であり、SpecialActionディスパッチの対象ではない）。
`Phase1-Status.md` では「C2完了」と記載されているが、実体は空スタブのまま放置されている。
→ Phase2で `IVirtualKBM` を導入する際、**この空スタブを実際に機能させるかどうかは判断が必要**
   （§4.3で扱う。Phase2の主目的ではないが、同じファイルを触るついでに解消することを推奨）。

---

## 1. Phase2の目的・方針（修正版）

### 1.1 目的
`VirtualKBMBase`（既存抽象クラス）を土台に `IVirtualKBM` インターフェースを新設し、
`Actions/` サブシステム（`KeyOutputAction`, `MouseOutputAction`, マクロ実行経路）が
DI経由でモック可能なKBM出力を利用できるようにする。

### 1.2 スコープ（2026-08-27改訂: 通常マッピング48箇所を独立ステップとして追加）
元の全体計画書の完了判定基準は「**`Actions/` 配下が `IVirtualKBM` のみを参照し、モックによる単体テストが
可能なこと**」であったが、ユーザーとの協議の結果、**通常の1:1マッピング処理48箇所も、マクロ実行14箇所とは
別の独立したステップ（Step 2-5）としてPhase2内で対応する**方針に変更した。§0.3の調査結果を踏まえ、
本計画では以下のようにスコープを明確化する。

| 対象 | Phase2で対応するか | 対応ステップ | 理由 |
|---|---|---|---|
| `Actions/` 配下（`KeyOutputAction`, `MouseOutputAction` 等）の `IOutputContext.OutputHandler` | **対応する** | Step 2-4 | 元計画の完了判定基準そのもの |
| `PlayMacro`/`EndMacro`（`Mapping.cs` 6527-6960、14箇所） | **対応する** | Step 2-4 | 元計画書§6.4に「マクロの逐次送出もこのフェーズで`IVirtualKBM`経由に統合する」と明記されている。影響範囲が`PlayMacro`内に閉じるため相対的に低リスク |
| 通常の1:1マッピング処理（`Mapping.cs` の残り48箇所） | **対応する（独立ステップ）** | **Step 2-5（新設）** | `Mapping.cs` の中核データフロー（毎フレーム・毎ボタン押下で実行される経路）であり、マクロ実行14箇所より影響範囲・リスクが大きいため、**マクロ実行とは別のステップとして分離**し、機能カテゴリ別にPRを分割・段階的に実施する（詳細は Step 2-5 参照） |
| `InputMethods.cs` | **対応しない** | - | 実質デッドコード（§0.2）。触る理由がない。 |
| `MouseOutputAction.cs` の空実装解消 | **任意（推奨、必須ではない）** | Step 2-4 or 2-5 | Phase2の主目的ではないが、同じファイルを触るため §4.3 で扱う。 |


### 1.3 `IVirtualKBM` インターフェース設計方針
`VirtualKBMBase` の **public abstract/virtual メソッドをそのまま反映**する（振る舞い変更なし、
名称・シグネチャも変更しない）。`VirtualKBMBase` 自体に `IVirtualKBM` を実装させることで、
既存の `SendInputHandler`/`FakerInputHandler` が**無改修でそのまま `IVirtualKBM` を満たす**。

### 1.4 DI登録方式（§0.4の制約への対応）
`Global.outputKBMHandler` の生成タイミング問題を回避するため、**状態を持たない遅延委譲アダプタ**
`VirtualKBMHandlerAdapter : IVirtualKBM` を新設する。このアダプタはコンストラクタでは何も保持せず、
各メソッド呼び出し時に毎回 `Global.outputKBMHandler` を参照する（null許容、null時は何もしない）。
これにより、アダプタ自体は `AppHost` のHost構築時点で問題なくSingleton登録でき、実際の出力ハンドラが
後から生成されても正しく機能する。

```csharp
public class VirtualKBMHandlerAdapter : IVirtualKBM
{
    public void PerformKeyPress(uint key) => Global.outputKBMHandler?.PerformKeyPress(key);
    public void PerformKeyRelease(uint key) => Global.outputKBMHandler?.PerformKeyRelease(key);
    // ...他メソッドも同様の遅延委譲
}
```
この方式は既存の `Global.getLSDeadzone()` 等が新サービスへ委譲する Strangler Fig パターン
（`DI-App-Wide-Migration-Plan.md` §5.4）の**逆方向版**（新DIサービスが既存staticフィールドへ委譲）であり、
本プロジェクトで既に確立されている設計思想と整合する。

---

## 2. ステップ分割（6ステップ、2026-08-27改訂: 通常マッピング処理を独立ステップ2-5として追加）

| ステップ | 内容 | 完了基準 | PR粒度 |
|---|---|---|---|
| **2-1** | `IVirtualKBM` インターフェースの設計・新設 | `VirtualKBMBase` の public メソッド一覧と完全一致するインターフェースを作成、コンパイル成功 | 1インターフェース |
| **2-2** | `VirtualKBMBase` に `IVirtualKBM` を実装させる + `VirtualKBMHandlerAdapter`（遅延委譲アダプタ）の新設 | `SendInputHandler`/`FakerInputHandler` が無改修でコンパイル成功。アダプタの単体テストで委譲が確認できる | 1件（クラス2つだが1PR） |
| **2-3** | `AppHost.cs` への `IVirtualKBM` シングルトン登録 | `services.AddSingleton<IVirtualKBM, VirtualKBMHandlerAdapter>();` 追加、既存4サービスと共存してコンパイル成功 | 配線のみ |
| **2-4** | 呼び出し箇所の `IVirtualKBM` 経由への置換（`Actions/` 配下 + マクロ実行14箇所） | `KeyOutputAction`/`MouseOutputAction` の `IOutputContext.OutputHandler` 型を `IVirtualKBM` に変更。`PlayMacro`/`EndMacro` 内の14箇所を `IVirtualKBM` 解決に置換（フォールバックとして直接 `outputKBMHandler` 参照を保持） | `Mapping.cs` の該当14箇所のみピンポイント置換 |
| **2-5（新設）** | 通常の1:1マッピング処理（`Mapping.cs` 残り48箇所）の `IVirtualKBM` 経由への置換 | 48箇所すべてが `IVirtualKBM` 解決経由に置換され、フォールバックが保持されている。実機での連打・同時押し回帰確認済み | 機能カテゴリ別に3〜4PRへ分割（§3のStep 2-5参照）。**着手前に必ずユーザー承認を得ること** |
| **2-6** | 単体テスト（`MockVirtualKBM` を用いた出力テスト）の実装と検証 | `DS4WindowsTests` に `MockVirtualKBM.cs` + `VirtualKBMHandlerAdapterTests.cs` を追加、`dotnet test` 全件成功 | テスト1式 |

**§5ルール（段階的実施）に従い、各ステップ完了後にユーザー確認を挟んでから次ステップに進むこと。**
一度に全ステップを実装しない。**特にStep 2-5は影響範囲が大きいため、着手前に必ずユーザーの明示的な承認を得ること。**


---

## 3. 各ステップの詳細

### Step 2-1: `IVirtualKBM` インターフェース設計

`VirtualKBMBase.cs`（現状確認済み）の public シグネチャをそのまま反映する。

```csharp
namespace DS4Windows.Actions
{
    public interface IVirtualKBM
    {
        bool Connect();
        bool Disconnect();
        void MoveRelativeMouse(int x, int y);
        void MoveAbsoluteMouse(double x, double y);
        void PerformMouseWheelEvent(int vertical, int horizontal);
        void PerformMouseButtonEvent(uint mouseButton);
        void PerformMouseButtonEventAlt(uint mouseButton, int type);
        void PerformMouseButtonPress(uint mouseButton);
        void PerformMouseButtonRelease(uint mouseButton);
        void PerformKeyPress(uint key);
        void PerformKeyPressAlt(uint key);
        void PerformKeyRelease(uint key);
        void PerformKeyReleaseAlt(uint key);
        void Sync();
        string GetDisplayName();
        string GetIdentifier();
        string GetFullDisplayName();
    }
}
```
配置先: `DS4Windows/Actions/IVirtualKBM.cs`（既存の `IProcessLauncher.cs` 等と同じフォルダ、命名規則に合わせる）。

**確認事項（着手前に実コードで再確認すること）**: `VirtualKBMBase` に `fakeKeyRepeat`
（`PlayMacro` から参照されている、§0.3のその他1件）のようなpublicプロパティが他にもないか、
全メンバを再度 `grep` で洗い出してからインターフェースを確定する。本計画書のメソッド一覧は
2026-08-27時点の調査に基づく暫定版であり、実装時に差分がないか要再確認。

### Step 2-2: `VirtualKBMBase` への実装 + アダプタ新設

```csharp
public abstract class VirtualKBMBase : IVirtualKBM
{
    // 既存の abstract/virtual メンバはそのまま。シグネチャ変更なし。
}
```
`SendInputHandler`/`FakerInputHandler` は無改修でコンパイルが通ることを確認する（既存の
`override` 実装がそのまま `IVirtualKBM` の実装として扱われるため）。

`VirtualKBMHandlerAdapter.cs`（新規、§1.4のコード例参照）を `DS4Windows/Actions/` に作成。
`fakeKeyRepeat` 等インターフェースに含めなかったメンバがあれば、アダプタでの扱いを個別検討する。

### Step 2-3: DI登録

`AppHost.cs` の `builder.ConfigureServices` 内、既存4サービス登録の直後に1行追加：
```csharp
services.AddSingleton<IVirtualKBM, VirtualKBMHandlerAdapter>();
```

### Step 2-4: 呼び出し箇所の置換（`Actions/` 配下 + マクロ実行14箇所）

#### 2-4-a: `Actions/` 配下
- `IOutputContext.cs` の `VirtualKBMBase OutputHandler { get; }` を `IVirtualKBM OutputHandler { get; }` に変更。
- `OutputContextImpl.cs` のコンストラクタ引数型を `IVirtualKBM` に変更。
- `KeyOutputAction.cs`／`MouseOutputAction.cs` の呼び出し元（`ctx.OutputHandler`）は型変更の影響を受けるのみで
  ロジック変更は不要（既存コードは `VirtualKBMBase` の具体的なメソッドを直接呼んでいないため影響最小、
  要実装時再確認）。
- `TriggerContextImpl.cs` の `VirtualKBMBase OutputHandler` も同様に `IVirtualKBM` へ変更が必要か確認する
  （C1〜C5で作られた `KeyActionBinding` 等の経路にも波及する可能性があるため、影響範囲を先に洗い出すこと）。

#### 2-4-b: `Mapping.cs` の `PlayMacro`/`EndMacro`（14箇所、ピンポイント置換）
既存の `outputKBMHandler.Xxx(...)` 呼び出しを、`IVirtualKBM` をDI解決した変数（例: `var kbm = ...`）経由の
呼び出しに置換する。**§2.1修正版に従い、DI解決に失敗した場合は既存の `outputKBMHandler` 直接呼び出しに
フォールバックする**（C3〜C5で確立した「`ServiceProviderHolder.Provider` から解決を試み、失敗時は
フォールバック」というパターンをそのまま踏襲）。

`Mapping.cs` は8,800行超の巨大ファイルのため、§3.2ルールに従い**該当14箇所のみを機械的な文字列一致で
ピンポイント置換**する（Pythonでの厳密一致置換を推奨、C5-2実装時と同じ手法）。

### Step 2-5（新設）: 通常の1:1マッピング処理（48箇所）の置換

**このステップはマクロ実行（Step 2-4）とは別の独立したステップとして扱う。着手前に必ずユーザーの
明示的な承認を得ること。** `Mapping.cs` の中核データフロー（毎フレーム・毎ボタン押下で実行されるリアル
タイム経路）を対象とするため、Step 2-4よりも影響範囲・リスクが大きい。

#### 2-5-a: 対象箇所の最新棚卸し
着手時点で `grep -n "outputKBMHandler\." DS4Windows/DS4Control/Mapping.cs` を再実行し、
Step 2-4完了後に残っている48箇所（Step 2-4で対応した14箇所を除いた全て）の正確な行番号・関数名を
一覧化する（Phase1のStep D等、他の変更で行番号がずれている可能性があるため、実装直前の再棚卸しを必須とする）。

#### 2-5-b: 機能カテゴリ別のPR分割方針
1回のPRで48箇所すべてを置換するとレビュー・回帰確認が困難になるため、以下のカテゴリ単位で
PRを分割する（2026-08-27時点の内訳、実装時は2-5-aの再棚卸し結果を正とする）：

| バッチ | 対象メソッド群 | 件数目安 | 優先度 |
|---|---|---|---|
| バッチ1 | `PerformKeyPress`/`PerformKeyPressAlt`/`PerformKeyRelease`/`PerformKeyReleaseAlt` | 約17件 | 高（キー出力、最も使用頻度が高い） |
| バッチ2 | `PerformMouseButtonEvent`/`PerformMouseButtonEventAlt` | 約26件 | 高（マウスボタン出力、件数最大） |
| バッチ3 | `PerformMouseWheelEvent`/`MoveAbsoluteMouse`/`MoveRelativeMouse`/`Sync`/`fakeKeyRepeat` | 約9件 | 中（ホイール・カーソル移動・同期系） |

各バッチは §2.1修正版のフォールバックパターン（`IVirtualKBM` 解決失敗時は既存 `outputKBMHandler`
直接呼び出しへフォールバック）を踏襲する。置換方式はStep 2-4-bと同じくPythonによる厳密一致置換を用いる。

#### 2-5-c: 実機回帰確認（必須）
本ステップはリアルタイム入力経路そのものを変更するため、各バッチ完了ごとに以下を確認する：
- 通常のボタン単発押下・離しが正しく動作すること
- 連打（高速な押下/離しの繰り返し）でタイミングのズレが生じないこと
- 複数ボタン同時押しで意図しない入力欠落が生じないこと
- マウスボタン/ホイール/タッチパッドをマウスとして使う設定で違和感がないこと

`docs-forDIMG/MadeByAgent/Phase2-Step2-5-Verification.md`（新規）に確認結果を記録すること。

### Step 2-6: 単体テスト

`DS4WindowsTests/MockVirtualKBM.cs`（新規）: `IVirtualKBM` の全メソッド呼び出しを記録するモック
（既存の `MockProcessLauncher.cs`/`MockMacroPlayer.cs` と同じスタイル）。

`DS4WindowsTests/VirtualKBMHandlerAdapterTests.cs`（新規）:
- T1: `Global.outputKBMHandler` が `null` の場合、アダプタの各メソッド呼び出しが例外を投げないこと
- T2: `Global.outputKBMHandler` にモックを設定した場合、アダプタ経由の呼び出しが正しく委譲されること
- T3: `IVirtualKBM` インターフェースがコンパイルを通過し、モックで全メソッドが実装可能なこと

`PlayMacro`/`EndMacro` 経路のテスト（Step 2-4-bの検証）は、既存の `MacroActionTests.cs`（Phase1 C3成果物）に
追加する形で、`MockVirtualKBM` を使った出力検証テストを1〜2件追加することを推奨する
（`DefaultMacroPlayer` → `Mapping.PlayMacroDirect` → `PlayMacro` という既存の呼び出し階層を踏まえ、
テストの実装可否は着手時に要判断）。Step 2-5（通常マッピング48箇所）についても、可能な範囲で
`MockVirtualKBM` を使った呼び出し検証テストを追加することを推奨する。

---


---

## 4. リスクと回避策

| リスク | 該当ステップ | 回避策 |
|---|---|---|
| `outputKBMHandler` は物理リソース（ドライバハンドル）を保持しており、初期化タイミング依存の参照が他にもある可能性 | 2-2/2-3 | §1.4の遅延委譲アダプタ方式により、DIコンテナ側は状態を持たないため、初期化順序の問題が発生しない設計にした |
| `IOutputContext.OutputHandler` の型変更が `KeyActionBinding`/`TriggerContextImpl` 等、Phase1で作った複数クラスに波及する | 2-4 | Step 2-4-a着手前に `grep -rn "VirtualKBMBase" DS4Windows/Actions/` で全参照箇所を洗い出し、影響範囲を確定してから着手する |
| `Mapping.cs` の14箇所置換で `PlayMacro` の連打・リピート挙動に回帰が生じる（既存の全体計画書§8でも指摘済みのリスク） | 2-4 | 置換前後で `MacroActionTests.cs` の既存テストが通ることを確認。可能であれば実機での連打・ホールド動作を回帰テスト項目化 |
| **48箇所の置換によりリアルタイム入力経路（毎フレーム実行）にタイミング遅延や入力欠落が生じる** | **2-5** | **機能カテゴリ別（バッチ1〜3）に分割し、各バッチ完了ごとに実機回帰確認を必須とする（§3 Step 2-5-c）。1回の巨大PRにしない** |
| **48箇所は行数・参照箇所が多く、ピンポイント置換の際に取り違え・重複置換が発生するリスク** | **2-5** | 着手直前に `grep` で最新の行番号を再棚卸し（2-5-a）。Python等による厳密な文字列一致置換を用い、目視レビューを徹底する |
| `MouseOutputAction.cs` が空スタブのままだと `IOutputContext.OutputHandler` の型変更のみ行っても実質的な動作確認ができない | 2-4/2-5 | §4.3（任意対応）で扱う。Phase2の必須完了条件ではないため、時間があれば対応、なければ既知の残課題として記録する |

---

## 5. 完了判定基準（Phase2全体）

- [ ] `IVirtualKBM` インターフェースが `VirtualKBMBase` の全public メンバを過不足なく反映している
- [ ] `SendInputHandler`/`FakerInputHandler` が無改修で `IVirtualKBM` を満たしコンパイル成功
- [ ] `AppHost.cs` に `IVirtualKBM` がSingleton登録されている
- [ ] `Actions/` 配下（`IOutputContext`, `KeyOutputAction`, `MouseOutputAction`）が `VirtualKBMBase` を
      直接参照せず `IVirtualKBM` のみを参照する
- [ ] `Mapping.cs` の `PlayMacro`/`EndMacro`（14箇所）が `IVirtualKBM` 経由に置換され、
      フォールバック（既存の直接参照）が保持されている
- [ ] `Mapping.cs` の通常マッピング処理48箇所**すべて**が `IVirtualKBM` 経由に置換され、
      フォールバックが保持されている（Step 2-5完了、バッチ1〜3すべて完了）
- [ ] Step 2-5の各バッチ完了ごとに実機回帰確認が実施され、`Phase2-Step2-5-Verification.md` に記録されている
- [ ] `MockVirtualKBM` を用いた単体テストが `DS4WindowsTests` に追加され、`dotnet test` で全件成功
- [ ] 本ファイルおよび各ステップの実装記録（`Phase2-Step2-x-Implementation.md` 等）が
      `docs-forDIMG/MadeByAgent/` に記録されている
- [ ] `Phase1-Status.md` 相当の `Phase2-Status.md` を新設し、進捗を追跡する

---

## 6. 次のアクション

1. 本計画書についてユーザー確認を得る。
2. Step 2-1（`IVirtualKBM` インターフェース設計）から着手する。§5ルールに従い、
   Step 2-1完了時点でユーザーに報告し、Step 2-2へ進む前に確認を挟む。
3. `VirtualKBMBase` の全メンバ再洗い出し（§3の「確認事項」）をStep 2-1の一部として実施する。
