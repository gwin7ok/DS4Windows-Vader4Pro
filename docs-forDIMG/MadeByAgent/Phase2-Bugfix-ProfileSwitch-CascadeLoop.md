# バグ修正記録: プロファイル切替 SpecialAction の連続発火（連鎖ループ）不具合

## 1. 発生事象 (5-3 テスト)
同一トリガーキーに各プロファイルの切換 SpecialAction を設定し、プロファイルをループ切換する構成において、トリガーキーを1回短押ししただけで連続してプロファイルが切り替わり続けてしまう（連鎖ループ暴走）。

## 2. 原因分析
- 物理ボタンを押し込んでいる間（数十ミリ秒〜数百ミリ秒）、毎フレームで入力判定ループが実行される。
- 切換先の新プロファイルでも同一のトリガーキーが割り当てられているため、プロファイル適用直後の次フレームで新プロファイル側の SpecialAction が「押下中」と判定され、次々と連鎖発火（カスケード）していた。

## 3. 修正内容
- `DefaultProfileSwitcher.cs` にデバイスごとのデバウンス保護ガード（同一トリガー押し込み中の連続発火を遮断するインターバル制御）を導入。
- 1回のボタン押下で確実に1回のみプロファイルが切り替わるよう制御。

## 4. 確認事項
- [ ] `dotnet publish` による Release ビルド成功
- [ ] `dotnet test` の全件パス
- [ ] 実機での 5-3（プロファイル切換ループ）テストで、1回押すごとに1段階のみ切り替わること

## 5. 【追記 2026-10-14】その後の経緯（Phase5・Phase6-Step9b）
本書 §3 の「`DefaultProfileSwitcher.cs` のデバウンス保護ガード（250ms）」は、`94066f3c`（2026-09-16、通知レガシー経路の削除）で `SwitchProfile` を `IProfileApplicationService` への委譲に一本化した際に、`DefaultProfileSwitcher` から消えている。現在の連鎖発火防止は、次の機構に置き換わっている（Phase5-Step14 の Issue8-1 とカスケードループの根本解決。実機確認済み。`Phase5-Status.md` §2.1）。

- Fresh-press ガード: `ActionInstanceState.RequiresFreshPressAfterReset`。プロファイル適用で `ClearDeviceState` が状態を新品にし、トリガーが `FreshPressReleaseHoldMs`（80ms）連続して未成立になるまで新規成立とみなさない。
- 実行中の再実行防止: `ActionInstanceState.IsExecuting`。
- `ProfileActionChainService` の Profile 型チェーンのスキップ。

Phase6-Step9b-0（`Phase6-Step9b-Plan.md` F01）で、稼働経路は `Mapping.cs` の上流のガードを必ず通ることを確認し、「250ms デバウンスの欠落」は誤検出（分類C）とした。`copilot-instructions.md` §2.2 の記述も現行の機構へ更新した。
