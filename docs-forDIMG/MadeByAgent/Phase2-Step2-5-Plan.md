# [作業計画書] フェーズ2 Step 2-5: 通常1:1マッピング処理 (48箇所) の置換

## 1. 目的と背景
`Mapping.cs` において、コントローラーのボタン・スティック・トリガー入力から直接仮想キーボード・マウスを出力している通常マッピング処理（計48箇所）に存在する `Global.outputKBMHandler` への直接依存を排除し、Step 2-4 で導入した **`VirtualKBM`（`IVirtualKBM`）経由に完全統一**する。

本ステップを完了することで、アプリ内のすべての仮想 KBM 出力（第3-b層）が DI 化され、フェーズ2の目的である「KBM 出力の完全な抽象化・疎結合化」が達成される。

---

## 2. 作業スコープと対象ファイル
- **修正対象**:
  - `DS4Windows/DS4Control/Mapping.cs`（通常マッピング処理内の `Global.outputKBMHandler` 呼び出し計48箇所を `VirtualKBM` にピンポイント置換）
- **ドキュメント**:
  - `docs-forDIMG/MadeByAgent/Phase2-Step2-5-Plan.md` (本計画書)
  - `docs-forDIMG/MadeByAgent/Phase2-Step2-5-Report.md` (作業報告書)
  - `docs-forDIMG/MadeByAgent/Phase2-Status.md` (進捗ダッシュボード更新)

---

## 3. マイクロステップ

1. **Step 2-5-1: 置換箇所の精査**
   - `Mapping.cs` 内の通常マッピングロジック（`MapCustom`、マウス操作、スティック/タッチパッドマウス変換など）における `Global.outputKBMHandler` の残存箇所を正確に特定。
2. **Step 2-5-2: ピンポイント置換スクリプトの実行**
   - 巨大ファイル（8900行超）への影響を最小限にするため、安全な PowerShell スクリプトにより `Global.outputKBMHandler` を `VirtualKBM` へ一括ピンポイント置換。
3. **Step 2-5-3: ビルド検証とドキュメント更新**
   - `dotnet publish` による Release ビルドの成功を確認し、報告書およびステータスダッシュボードを更新。

---

## 4. 完了条件 (Done Criteria)

- [ ] `Mapping.cs` 内の `Global.outputKBMHandler` 呼び出しがすべて `VirtualKBM` 経由に置換されていること（例外的な初期化・静的アクセサ除く）。
- [ ] プロジェクト全体のビルド（`dotnet publish`）がエラーなく完了すること。
- [ ] `Phase2-Step2-5-Plan.md`、`Phase2-Step2-5-Report.md`、`Phase2-Status.md` が最新化されていること。

---

## 5. 次のステップへの連携
Step 2-5 完了後、直ちに **Step 2-6（単体テスト整備・テストビルド検証）** に進み、`DS4WindowsTests` プロジェクトへ `MockVirtualKBM` およびテストケースを追加・検証する。
