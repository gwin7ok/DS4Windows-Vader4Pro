# フェーズ4-Step10-2-C Legacy 経路残存調査報告書

作成日: 2026-09-02
対象ブランチ: `For-DI-migration-work`
基準文書:

- `docs-forDIMG/DI-App-Wide-Migration-Plan.md`
- `docs-forDIMG/MadeByAgent/Phase4-Plan.md`
- `docs-forDIMG/MadeByAgent/Phase4-Step10-2-B-Plan.md`
- `docs-forDIMG/MadeByAgent/Phase4-Step10-2-C-Plan.md`

## 1. 調査目的

Stage2 後の実機検証へ進む前に、Phase4 の計画上は DI 化されているべきでありながら、現在も Legacy 経路に残っている箇所を棚卸しした。

この調査では、`Global` や `rootHub` の文字列件数をそのまま未移行件数とは扱わない。定数、純粋関数、Factory 内の正規生成、互換シム、別フェーズの出力処理などを分類し、Phase4 で実際に整理すべき経路を特定する。

## 2. 機械的棚卸し結果

| 検索対象 | 生の確認件数 | ファイル数 | この数字に含まれるもの |
|---|---:|---:|---|
| `Global.`、`rootHub`、ViewModel直接生成、DI構築等 | 約1,001件 | 76ファイル | コメント、定数、互換シム、対象外処理を含む全体量 |
| `App.rootHub`／`Program.rootHub` | 約140件 | 17ファイル | 起動時代入、UI、AutoProfile、デバイス処理を含む |
| `new XxxViewModel(...)` | 31件 | 16ファイル | Factory 内の正規生成、DI 失敗時フォールバックを含む |
| 旧 `ServiceCollection` 構築 | 1系統 | `App.xaml.cs` | 正式 `AppHost` と併存していた旧 Composition Root |

### 2.1 件数の読み方

約 1,001 件という数字は、Legacy 経路を含む可能性のあるコード上の記述を広く拾った値であり、Phase4 の未移行件数ではない。たとえば `Global.MAX_DS4_CONTROLLER_COUNT` はサイズ定数、`Global.Clamp` は純粋関数であり、DI 化しても責務分離の効果がないため、Phase4 の移行対象として数えない。

また、Factory 内の `new ProfileSettingsViewModel(...)` は、Factory が実行時引数と DI サービスを合成するための正規生成である。View が直接 `new` している互換フォールバックとは分類が異なる。

## 3. Phase4 対象の残存課題

### 3.1 Composition Root と DI 登録

- 旧 `App.xaml.cs` の `ServiceCollection` 構築は Phase4 対象だったが、C-1 で削除済み。
- Action 系登録は `ServiceRegistration.cs` へ統合済み。
- `ServiceProviderHolder` は AppHost の Provider と共有するよう整理済み。
- `ControlService` は Singleton 登録し、AppHost から解決する方式へ移行済み。
- `App.rootHub`／`Program.rootHub` への互換代入は CP4 まで維持する。

したがって、この領域の次の確認対象は実機での起動順序、Singleton 同一性、終了時のスレッド・イベント解放である。

### 3.2 ViewModel と UI

- Factory 内の `new` は正規生成であり、未移行件数には含めない。
- View 側の `vmFactory ?? new XxxViewModel(...)` は、CP4 まで残す互換フォールバックである。使用時は `[Legacy]` Trace ログを必須とする。
- Pattern A/B/C の未接続 ViewModel、`ProfileEditor`／`SpecialActionEditor` 等の直接生成は、個別に DI 経路を確認して移行する。
- CP4 完了後に、フォールバック削除を専用変更として再評価する。

### 3.3 `rootHub` 直接依存

- `Mapping.cs`、`DS4Sixaxis.cs` 等の高頻度・低レイヤ経路は **C-1** とし、`IDeviceStateAccessor` の最小契約を使う。
- `MainWindow`、`ProfileEditor`、`RecordBox` 等の複数機能を使う UI は、短期 **C-2** として `ControlService` 注入から開始する。
- `AutoProfileChecker.cs` は **C-1** とし、状態取得、Repository、Switcher を責務別に注入する。
- `PresetOption.cs` は **C-2 から開始**し、将来 `IProfilePresetService` 等へ移行する。
- MainWindow のプロファイル適用は **C-1** とし、将来 `IManualProfileApplicationService` 等へ適用手順を集約する。
- `App.rootHub`／`Program.rootHub` の併存そのものは C-1/C-2 の分類対象外であり、呼び出し元の移行を支える互換管理として CP4 まで維持する。

## 4. 対象外または別フェーズとして扱うもの

次の項目は `Global` や `rootHub` を含んでいても、Step10-2-C の残存課題件数には機械的に含めない。

- `Global.MAX_DS4_CONTROLLER_COUNT` 等の定数
- `Global.Clamp` 等の純粋関数
- `Mapping` の static 状態そのもの
- KBM 出力、プロセス起動、座標変換など、全体計画上の別サービス境界に属する処理
- Factory 内の実行時引数付き ViewModel 生成
- CP4 まで維持する互換フォールバック。ただし使用時のログと使用実績は監査対象

対象外とする場合も、理由と将来の担当フェーズを報告書へ残す。

## 5. `[DI]`／`[Legacy]` ログの残存課題

ログは getter や配列アクセスごとには出さず、次の粒度で整理する。

- `[DI]`: AppHost 作成、主要サービス解決、Factory 生成、Repository 操作、ユーザー操作単位の設定変更
- `[Legacy]`: Global シム入口、フォールバック使用、Legacy 経路の失敗、互換代入の重要状態変更
- 原則として入力ポーリング、高頻度 getter、配列要素アクセスには追加ログを出さない

残存シムについては、ログの有無だけでなく「高頻度のためログを抑制した」という理由も記録する。

## 6. 現時点の結論

Phase4 の Legacy 経路は完全には解消されていないが、残存量は次のように理解する。

- 生の横断検索量: 約 1,001 件／76 ファイル
- `rootHub` 静的依存: 約 140 件／17 ファイル
- ViewModel 直接生成: 31 件／16 ファイル
- Phase4 の主要未解決領域: UI の DI 接続、`rootHub` 呼び出し元の個別移行、Legacy ログ網羅性、CP4 前の自動テスト化
- C-1/C-2 の分類方針: 決定済み
- 短期方式と将来の推奨移行先: 計画書へ引継ぎ済み

したがって、今後は件数削減だけを目的にせず、分類済みの Phase4 対象を C-3、C-4、C-5、C-6 の段階で処理する。

## 7. 今後の実装順序

1. C-3: `rootHub` 呼び出し元を分類方針に沿って個別移行する。
2. C-4: ViewModel フォールバック使用時の `[Legacy]` ログを整備する。
3. C-5: Global シムのログ網羅性を監査する。
4. C-6: CP4 項目を自動テスト／実機／両方に分類し、自動化できる項目をテストへ移す。
5. C-7: 自動テストで代替できなかった項目を中心に CP4 実機検証を行う。
6. C-8: CP4 後にフォールバック削除を専用変更として判断する。
