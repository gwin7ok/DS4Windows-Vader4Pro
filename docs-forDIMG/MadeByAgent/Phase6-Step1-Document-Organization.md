# Phase6-Step1 文書整理（Step2以降利用向け）

作成日: 2026-09-18
目的: 10文書をStep2（実装）で利用しやすい構造に整理
構造: 入力（監査データ）→ 処理（分類・照合）→ 出力（集計・証明）→ 参照（計画・記憶）

## 文書一覧と利用目的（Step2向け）

### A. 入力データ（監査の基礎）
| 文書 | 内容 | Step2での利用 |
|---|---|---|
| `Phase6-Step1-Strict-Verification-Record.md` | 10ファイル762参照の抽出記録 | 移行対象の参照リストとして使用 |
| `Phase6-Step1-Unique-ID-Manifest.md` | 一意ID台帳（File:Line:Col） | 各参照の追跡IDとして使用 |
| `Phase6-Step1-Complete-Unique-ID-Record.md` | 列番号厳密抽出完了記録 | IDの完全性証明として参照 |

### B. 処理結果（分類・照合）
| 文書 | 内容 | Step2での利用 |
|---|---|---|
| `Phase6-Step1-ABC-Classification.md` | a/b/c分類（契約差検証含む） | 移行対象（a/b）と除外（c）の判別に使用 |
| `Phase6-Step1-Individual-Reference-Determination.md` | 個別参照厳密確定台帳 | 各参照の分類確定証明として使用 |
| `Phase6-Step1-Service-Reference-Expansion.md` | サービス別紙展開 | サービス層の移行設計に使用 |
| `Phase6-Step1-Full-Reconciliation-Proof.md` | 完全照合証明（重複ゼロ・漏れゼロ） | 監査の完全性証明として参照 |

### C. 出力（集計・証明）
| 文書 | 内容 | Step2での利用 |
|---|---|---|
| `Phase6-Step1-Classification-Metrics.md` | 分類別実測集計（ファイル別・a/b/c別） | 移行工数の見積もりに使用（a+b ≈ 458件） |
| `Phase6-Step1-C-Classification-Count.md` | c（除外）件数集計（概算 ≈ 304件） | 除外対象の範囲確認に使用 |
| `Phase6-Step1-Metrics-Reconciliation-Proof.md` | 実測集計照合証明（762件一致） | 集計の信頼性証明として参照 |

### D. 計画・記憶（監査の文脈）
| 文書 | 内容 | Step2での利用 |
|---|---|---|
| `Phase6-Step1-Progress-Aggregation.md` | 段階的進行記録（Phase1〜6） | 監査の進行状況確認に使用 |
| `Phase6-Step1-Completion-Summary.md` | 監査完了報告（中間成果） | Step2承認の前提条件（報告書§7維持：承認根拠には使用しない）として参照 |
| `Phase6-Step1-Phase5-Evidence-Check.md` | Phase5証跡確認 | 前提条件（Phase5完了証跡）の確認に使用 |

## Step2での推奨利用順序

1. `Completion-Summary.md` で監査の前提と制約を確認（§7維持：承認根拠には使用しない）
2. `Strict-Verification-Record.md` + `Unique-ID-Manifest.md` で移行対象の参照リストを取得
3. `ABC-Classification.md` + `Individual-Reference-Determination.md` で各参照の分類（a/b/c）を確認
4. `Classification-Metrics.md` + `C-Classification-Count.md` で移行工数（a+b ≈ 458件、c ≈ 304件）を見積もり
5. `Service-Reference-Expansion.md` でサービス層の契約差（ProfileApp、SpecialAction等）を確認
6. `Full-Reconciliation-Proof.md` + `Metrics-Reconciliation-Proof.md` で監査の完全性を確認（重複ゼロ、漏れゼロ、数値一致）
7. `Phase5-Evidence-Check.md` で前提条件（Phase5証跡）の状態を確認

## 制約（報告書§6.2/§7維持、Step2実装時に遵守）

- 本番コード・テストコード・DI登録の変更なし（維持）
- 旧暫定数（193件等）を実測として転記しない
- 1,337出現回数をDI移行対象件数として扱わない
- c（除外 ≈ 304件）は改修対象外（const、Pre-Host、テスト、フォールバック、readonly配列）
- a（契約候補）とb（要設計 ≈ 458件）が改修対象
- Step2計画・Phase6進捗表は変更しない
