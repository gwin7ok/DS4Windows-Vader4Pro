# フェーズ7計画書: `Global`／`BackingStore` の解体、起動の近代化、`Mapping` の instance 化

作成日: 2026-10-17  
対象ブランチ: `For-DI-migration-work`  
前提文書: `Phase7-0-Survey.md`（調査。数値と根拠の正本）、`Phase6-Step12-Plan.md` §4（引き継ぎ台帳）、`DI-App-Wide-Migration-Plan.md` §6.9  
進捗管理: `Phase7-Status.md`。残存台帳: `Phase7-Residual-Ledger.md`  
状態: **計画確定（決定 D1〜D6 は 2026-10-17 に承認済み）。Step1 は完了。**

---

## 0. 目的とスコープ

### 0.1 目的
- 静的クラス `Global`（2,396 行）と、その中に唯一存在する `BackingStore`（6,556 行）の実体を、DI のサービスへ移し、静的アクセスと Service Locator を、コンストラクター注入に置き換える。
- `Mapping`（8,196 行。ほぼ全メンバーが static）を instance 化し、`Global`／`rootHub`／Service Locator への依存をなくす。Phase8（Controls／SpecialActions の統合ディスパッチ）の前提を整える。
- 機能・ログ・安全弁は変えない（`.github/copilot-instructions.md` §2）。

### 0.2 スコープに含むもの
- `BackingStore` の DI 登録（同一インスタンス）と、永続化の切り出し。
- `Global` の生存する実体（約 156 名）のサービス側への移管。
- 起動・終了シーケンスの近代化（Pre-Host、K7-1 の残存経路、`AppHost` の整理、ホストの `Dispose`）。
- `Program.rootHub`（22）、`AppHost.GetService`（40）、`ServiceProviderHolder`（Actions 以外の 6）の削除。
- `outDevTypeTemp`／`activeOutDevType` の所有権の `OutputSlotService` への移動（`Phase6-Step5-Plan.md` §9）。
- DI インターフェースの 0 件メンバー（約 50）の個別判断（§2.4）。
- `Mapping` の instance 化、互換 `AppHost` の削除。
- 既存の課題: K2（完了）、不具合 B（完了）、K3（マクロの二重ガード。`Mapping` の instance 化の中で再設計の要否を判断）。

### 0.3 スコープに含まないもの
- Phase8（Controls／SpecialActions の統合ディスパッチ）。`Actions/` の `ServiceProviderHolder`（17）と、`Actions/` の未配線の抽象（11 メンバー）は、Phase8 の台帳へ移す。
- 実機で確認できない項目（U-1〜U-7）の解消。
- 保存形式の統一の拡大（D4＝F1）: アプリ設定ほか 4 ファイルは DTO のまま。プロファイルは旧形式。
- `BackingStore` のフィールド群のドメインエンティティへの分解（D2＝(i)。Step10 で範囲を再評価）。

## 1. 承認済みの決定（2026-10-17）

- D1: 案 A（外側から内側へ）。
- D2: `BackingStore` は「DI の単一インスタンス化＋永続化の切り出し」まで。分解は Step10 で再評価。
- D3: 起動は S1（コンテナ構築の前にブートストラップを作り、同一インスタンスを登録）。
- D4: F1（保存形式は現状維持。アプリ設定の旧形式の読込は、読み取り専用の互換として切り出す）。
- D5: 既存の課題（不具合 B、K2、`UpdateDS4CLightbarMacro` の削除）は Step1 で修正。
- D6: `ServiceProviderHolder` を追跡（Actions の 17 件は Phase8）。

## 2. Step 一覧

- **Step1: 整理と台帳の確定【完了、2026-10-17】** — 孤立メソッド 10 名の削除、K2、不具合 B、`ServiceProviderHolder` のラチェット。`Phase7-Step1-Completion-Report.md`。
- **Step2: DI インターフェースの 0 件メンバー（約 50）の個別判断** — 経緯の調査→判断の記録→承認→削除または接続の計画。接続すると決めた分の実装は、該当する後続 Step に割り当てる。
- **Step3: `BackingStore` の DI 化と `Global` の転送の削除**
  - 3a: `BackingStore` を DI のシングルトンとして登録（実体は `Global.store` と同一）。サービスのコンストラクターへ注入。
  - 3b: 純粋な転送の約 90 名を、サービスが `BackingStore` を直接使うようにして、`Global` から削除（サービス単位の Wave。`AppSettingsService` の 185 参照が最大）。
  - 3c: イベントとプロファイル適用・プリセットの処理（`ApplyProfile` ほか約 15 名）をサービス側へ移管。**Step3 では実施せず、別計画書（Step3c）へ持ち越した**（2026-10-17。`Phase7-Step3-Completion-Report.md` §5）。
- **Step4: 起動・終了シーケンスの近代化**
  - ブートストラップ（パス・設定の場所・ログ設定を、コンテナ構築の前に作り、同じインスタンスを登録）。Pre-Host 12 箇所と共用ヘルパーの整理。
  - 環境・パスの実体（`exedirpath`、`appdatapath`、ViGEm／HidHide の情報ほか約 20 名）を `PathService`／`EnvironmentService` へ移管。
  - `AppHost` のオーバーロード整理、ホストの `Dispose`、`CreationNote` の扱い、`Global.Load()` の暗黙構築の経路の解消。
- **Step5: `Program.rootHub` と Service Locator の削除** — `rootHub` 22 件、`AppHost.GetService`（`MainWindow` 17 ほか）、`ServiceProviderHolder`（`Mapping` 5、`App` 1）を、注入に置き換える。
- **Step6: 出力スロットの所有権の移動** — `outDevTypeTemp`／`activeOutDevType` → `OutputSlotService`（約 45 参照）。Step5 と入れ替え可能。
- **Step7〜9: `Mapping` の instance 化**
  - Step7: 特性化テスト（入力→出力の固定）の整備。静的な状態の保持クラスへの切り出し。`BackingStore`→`Mapping` の逆参照の解消。
  - Step8: `Mapping` の instance 化（DI のシングルトン＋抽象の窓口）。呼び出し元（`ControlService`、`Mouse`、`SyntheticDispatcher`、`Actions/*`）の切り替え。静的な `profileSettings` 束縛の解消。
  - Step9: 互換 `AppHost`（`DI/Compat/AppHost.cs`）の削除、`Mapping.cs` の `Global` 参照（定数・`Clamp`）の再配置、K3 の判断。
- **Step10: 仕上げ** — `Global` の残り（定数・純粋計算）の再配置、`BackingStore` の永続化の切り出し（`LegacyProfileXmlSerializer`、アプリ設定の読み書き）とゴールデンテスト、`BackingStore` の分解の範囲の再評価、名前空間＝フォルダの扱いの判断、モデル図の更新、Phase7 の総合検証と完了報告。

### 依存関係
- Step2 → 後続（接続すると決めた契約が、Step3 以降の対象を決める）。
- Step3 → Step4（`Global` の環境・パスの実体を移すには、ブートストラップが前提）。
- Step3・Step4 → Step5（`rootHub`／Locator は、注入先のサービスが揃ってから）。
- Step5 → Step7〜9（`Mapping` が受け取る部品を、注入で解決できる状態にしてから）。
- Step6 は Step7〜9 と独立（`Mapping.cs` は出力スロットの配列を使っていない）。
- Step7 → Step8 → Step9。

## 3. ガードレールと共通ルール

- ビルド・テストビルド・テスト実行・コミットはユーザーが行う。エージェントはローカルファイルの編集のみ。
- 各 Step は個別の計画書（`Phase7-StepN-Plan.md`）を作成し、承認を得てから、Wave 単位（1 Wave ＝ 1 コミット）で進める。
- ラチェット（`Step10FourGlobalRatchetGuardTests`、`Phase7Step1ServiceLocatorRatchetGuardTests`）は、減る方向のみ許可。基準値は、新しいファイルへ引き継ぎ、合計を増やさない。
- 1 ファイル 1 型。型の追加・分割時は基準値を引き継ぐ。構造が変わったら、モデル図（`docs-forDIMG/Model-Diagram`）を更新する。
- 既存の不具合を見つけたら、移行前のコードと比較して回帰かを確認し、修正前に承認を得る。
- 実機でないと確認できない項目は、`Phase6-Step11-RealDevice-Verification-Checklist.md` §11 に記録する。
- `Phase5-Plan.md` §5 の 6 大ガードレール（同一 XML 排他ロック、適用時 Halt 停止、スレッド直列化、On-Demand パス評価、ドライバ破棄順序、切断時クリーンアップ）を維持する。特に Step4（`Dispose`）と Step8（ホットパス）。
- ホットパス（`Mapping` の毎レポート実行）では、割り当てゼロの維持（`MappingHotPathAllocationTests`）。

## 4. リスク

- Step3: 変更のファイル数が多い。サービス単位の Wave に分ける。
- Step4: 起動順序の変更。ログ・設定の場所・言語の初期化の順序、`-driverinstall` の分岐、終了時のホスト破棄（ハング、ViGEm の残留）。実機確認が要る。
- Step5: `rootHub` を介した暗黙の共有状態（代入のタイミング）。
- Step6: 実行時状態の静的配列。マルチスレッドの読み取り。スロット 5〜8（U-2）は確認不能のまま。
- Step7〜9（最大）: ホットパス、ゼロアロケーション、マクロの別スレッド、確認不能の U-2・U-5・U-6 が同じ経路にかかる。Step7 の特性化テストを必須とする。

## 5. 完了条件

- ラチェットの基準値が、`Global.*`、`AppHost`、`Program.rootHub`、`ServiceProviderHolder`（Actions を除く）とも、目標値（`Phase7-Residual-Ledger.md` で確定）に達している。
- `Mapping` が instance 化され、互換 `AppHost` が削除されている。
- DI インターフェースの 0 件メンバーが、すべて判断済み（削除、接続、または Phase8 へ移管）。
- ビルド警告 0、全テスト成功。実施可能な実機確認の合格。
- モデル図の更新、Phase7 完了報告書。
