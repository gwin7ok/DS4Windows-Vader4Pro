# フェーズ4 最終総合実機動作確認リスト CP4（Phase4-Step10-RealDevice-Verification-Checklist.md）

正式名称: `docs-forDIMG/MadeByAgent/Phase4-Step10-RealDevice-Verification-Checklist.md`
作成日: 2026-09-01
対象ブランチ: `For-DI-migration-work`
前提ドキュメント:
- `docs-forDIMG/DI-App-Wide-Migration-Plan.md` §3.3, §4.1
- `docs-forDIMG/MadeByAgent/Phase4-Plan.md` §1.1.1, §2, §3 Step10, §4.3
- `docs-forDIMG/MadeByAgent/Phase4-Status.md`
- `docs-forDIMG/MadeByAgent/Phase4-Step9-Completion-Report.md`
- `docs-forDIMG/MadeByAgent/Phase4-Step9-Audit-Report.md`
- 各実機確認リスト（CP1, CP2, CP3）

---

## 本リストの位置づけ

本リストは、Phase4 の **全 13 バックエンドサービスの DI 化、Composition Root 一本化、全 29 箇所の ViewModel 直接 new 全廃、および `[DI]` Trace 監査ログの導入** を経て、フェーズ4 の総仕上げとして実施する **最終総合 E2E 実機動作確認チェックリスト（Checkpoint 4）** である。

実機コントローラー（Vader 4 Pro / DS4 等）、仮想コントローラー出力（ViGEmBus）、WPF UI 全画面、およびログ出力を通じ、**「新方式 DI 経路を通じてアプリケーション全体が 100% 健全・安定に稼働していること」** を総合的に実証する。

各項目は、確認後に結果（`○`: 正常動作、`△`: 一部制限あり、`×`: 不具合あり、`未実施`）およびメモを記入すること。

---

## 1. `[DI]` Trace 監査ログ出力シナリオ（DI 経路の稼働可視化）

| # | 確認内容 | 確認手順 | 結果 | メモ |
|---|---|---|---|---|
| 1-1 | アプリ起動時に `[DI] AppHost.CreateHost` および各サービスの解決ログが出力される | アプリ起動後、Log タブ（または Debug ログ）を確認し、`[DI] AppHost` ログが記録されていることを確認 | [ ] | |
| 1-2 | プロファイル読込・保存・切替時に `[DI] ProfileRepository`, `[DI] ProfileSettingsService` ログが出力される | コントローラースロットのプロファイルを切り替え、`[DI] Profile...` ログが出力されることを確認 | [ ] | |
| 1-3 | SpecialAction 実行・設定変更時に `[DI] SpecialActionRepository` ログが出力される | SpecialAction を追加・実行し、`[DI] SpecialActionRepository` ログが出力されることを確認 | [ ] | |
| 1-4 | ダイアログ開閉時に `[DI] ViewModelFactory` ログが出力される | ProfileEditor や RecordBox 画面を開き、`[DI] ViewModelFactory: Created...` ログが出力されることを確認 | [ ] | |

---

## 2. 第1層（入力監視層）& 第2層（信号変換層）: 実機入力・マッピングシナリオ

| # | 確認内容 | 確認手順 | 結果 | メモ |
|---|---|---|---|---|
| 2-1 | 実機コントローラーの接続・切断・バッテリー状態が `IDeviceStateService` 経由で即時反映される | 実機を接続・切断し、メイン画面の認識状態・バッテリー表示が正確に追従することを確認 | [ ] | |
| 2-2 | ボタン・スティック・ジャイロの入力マッピングが低遅延で正常に追従する | 実機を操作し、プロファイル編集画面の入力テストやゲーム上で遅延・欠落なく入力が追従することを確認 | [ ] | |
| 2-3 | SpecialAction（マクロ再生・プロファイル切替）が実機トリガーで確実に発動する | 設定したトリガー操作を行い、マクロやプロファイル切替が即座に動作することを確認 | [ ] | |

---

## 3. 第3層（信号出力層）: 仮想コントローラー・KBM出力シナリオ

| # | 確認内容 | 確認手順 | 結果 | メモ |
|---|---|---|---|---|
| 3-1 | 仮想コントローラー出力（Xbox 360 / DS4）が Windows（`joy.cpl`）およびゲームで正常動作する | `joy.cpl` を開き、仮想コントローラーのボタン・スティック出力が追従することを確認 | [ ] | |
| 3-2 | 仮想 KBM 出力（`IVirtualKBM`）によるマウス移動・キー送出が正常に動作する | タッチパッドマウスやキー割り当てプロファイルで、Windows 上のカーソル操作やキー入力が動作することを確認 | [ ] | |
| 3-3 | UAC 昇格プログラム起動（`IElevatedProcessLauncher`）が正常に実行される | 昇格が必要な外部プログラム起動アクションをトリガーし、正常に起動することを確認 | [ ] | |

---

## 4. 第4層（UI層）: 全画面統合・長時間安定性シナリオ

| # | 確認内容 | 確認手順 | 結果 | メモ |
|---|---|---|---|---|
| 4-1 | 全画面（Controllers, Profiles, Auto Profiles, Special Actions, Settings, Log, About）が正常に動作する | すべてのタブおよびダイアログを開き、設定変更・保存・UI バインディングが正常に動作することを確認 | [ ] | |
| 4-2 | 長時間稼働・コントローラー抜き差し・スリープ復帰でもクラッシュやメモリリークが発生しない | 連続操作およびコントローラーの抜き差しを繰り返し、安定して動作し続けることを確認 | [ ] | |
| 4-3 | Log タブに DI 関連の例外や赤字エラーが一切出力されていない | Log タブを開き、全テスト操作を通じて未捕捉例外やエラーログが存在しないことを確認 | [ ] | |

---

## 5. 実施記録

| 実施日 | 確認者 | 実施項目 | 結果概要 |
|---|---|---|---|
| （未実施） | - | - | - |

---

## 6. 次のアクション

1. 実機動作確認を行い、本リストに結果を記録する。
2. 全項目合格（○）を確認後、**Phase 4 全体完了報告書（`Phase4-Completion-Report.md`）** を作成し、フェーズ4 を正式完了とする。
