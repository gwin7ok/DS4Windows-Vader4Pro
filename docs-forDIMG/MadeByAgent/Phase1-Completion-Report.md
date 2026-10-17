# Phase 1: SpecialAction 判定・実行の分離 - 総合完了報告書

**完了日**: 2026-08-27  
**ステータス**: Phase 1 全工程完了 (100%)  
**承認状態**: ロールアウト準備完了

---

## 1. エグゼクティブサマリー

Phase 1 では、`Mapping.cs`（約8,800行）に埋め込まれていた `SpecialAction` の副作用直接呼び出し（外部プロセス起動、マクロ実行タスク、プロファイル切り替え）を、DI（依存性注入）パターンに基づく Actions サブシステムへ分離・集約しました。

すべての改修において **No Feature Drop（機能完全維持）原則** を遵守し、単体テストによる完全な品質検証を完了しました。

---

## 2. 実装されたアーキテクチャ概要

```
[ 入力層 (Mapping.cs) ]
       │
       │ (DispatchInputEdge)
       ▼
[ ActionFactory / DefaultActionFactory ]
       │
       ├──► [ LaunchProcessActionAdapter ] ──► [ LaunchProcessAction ] ──► [ IProcessLauncher ]
       ├──► [ MacroActionAdapter ]         ──► [ MacroAction ]         ──► [ IMacroPlayer ]
       ├──► [ ProfileSwitchActionAdapter ]  ──► [ ProfileSwitchAction ]  ──► [ IProfileSwitcher ]
       ├──► [ KeyActionAdapter ]           ──► [ KeyOutputAction ]
       └──► [ MouseActionAdapter ]         ──► [ MouseOutputAction ]
```

---

## 3. マイルストーン完了実績

| ステップ | 成果物 | テスト状況 |
| :--- | :--- | :---: |
| **A (インベントリ & 基盤)** | `Direct-Callsites-Inventory.md`, `MockManagedActionManager.cs` | 完了 |
| **B (Trigger 厳密化)** | `Mapping.DispatchInputEdge`, `DispatchOrSetBeingTriggered` | 完了 |
| **C1 (Key send)** | `KeyOutputAction.cs` 配線確認 | 完了 |
| **C2 (Mouse output)** | `MouseOutputAction.cs` | 完了 |
| **C3 (Macro)** | `IMacroPlayer.cs`, `DefaultMacroPlayer.cs`, `MacroAction.cs`, `MacroActionAdapter.cs` | T1〜T5 PASS |
| **C4 (Profile switch)** | `IProfileSwitcher.cs`, `DefaultProfileSwitcher.cs`, `ProfileSwitchAction.cs`, `ProfileSwitchActionAdapter.cs` | T1〜T5 PASS |
| **C5 (Launch process)** | `IProcessLauncher.cs`, `DefaultProcessLauncher.cs`, `LaunchProcessAction.cs`, `LaunchProcessActionAdapter.cs` | T1〜T6 PASS |
| **D (整流化 & DI登録)** | `AppHost.cs`（全サービス Singleton 登録）、`Mapping.cs` レガシーコード整理 | 全テスト PASS |
| **E (完了レビュー)** | `Phase1-Completion-Report.md` | 完了 |

---

## 4. Phase 2 への移行準備

* **Phase 2 の主題**: **KBM出力の抽象化 (`IVirtualKBM`)**
* **準備完了事項**: Phase 1 で構築した Actions サブシステムおよび `AppHost` DI 基盤の上に、そのまま `IVirtualKBM` を注入・接続可能な状態になっています。