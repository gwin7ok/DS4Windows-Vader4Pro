# Phase 5 Step 14: プロファイル適用一本化および保存・Rename時同期計画書
Document: docs-forDIMG/MadeByAgent/Phase5-Step14-ProfileSync-And-ApplyUnified-Plan.md  
Branch: gwin7ok/DS4Windows-Vader4Pro (For-DI-migration-work)  
Date: 2026-09-14  

---

## 1. 背景と課題

### 1.1 背景
Phase 5 Step 14（Emulated Controller 整合性是正および保存処理の一本化）の過程において、UI上の選択変更をディスク（XML）および接続中コントローラーへ即座に反映する共通ルートの整備を進めてきた。

### 1.2 判明した課題
1. プロファイル保存時のクラッシュ（TargetException）:
   - プロファイル保存時にディスクを正として一覧を再同期するため `ProfileListHolder.Refresh()` を呼び出した際、内部の `profileListCol.Clear()` が実行される。
   - コントローラー一覧画面（Controllersタブ）の ComboBox が特定アイテム（`ProfileEntity`）を選択した状態でコレクションが空になるため、WPFのバインディングエンジンが参照先を見失い、`System.Reflection.TargetException: Object does not match target type.` を投げて強制クラッシュする。
2. Rename（名前変更）時のオブジェクト・参照破綻:
   - 接続中コントローラーが使用しているプロファイルを一覧画面の「Rename」ボタンで改名した場合、ファイル名（`old.xml` -> `new.xml`）だけが変更され、コントローラーの内部パス（`Global.ProfilePath`）や ComboBox の選択状態が旧名のまま取り残され、未選択状態へのリセットや FileNotFound が発生する。
3. プロファイル適用処理の散在:
   - 手動選択、保存時ホットリロード、スペシャルアクション、自動プロファイル、接続時など、コントローラーへプロファイルを適用するコードが各所に個別実装されており、保守性・追従性のリスクとなっている。

---

## 2. アーキテクチャ設計

### 2.1 共通適用窓口 ApplyProfileToSlot の新設（6つの契機を集約）
コントローラーへプロファイルを安全に適用する単一の静的メソッドを `Global`（`ScpUtil.cs`）に配置し、アプリ内の全適用経路をこの窓口に集約する。

```csharp
public static bool ApplyProfileToSlot(int slotIndex, string profileName, ProfileChangeSource source = ProfileChangeSource.Manual)
```

#### 【集約対象となる6つの契機】
1. 手動選択: `MainWindow.xaml.cs`（`SelectProfCombo_SelectionChanged`）
2. 編集・保存時: `MainWindow.xaml.cs`（`Editor_ProfileSaved`）
3. 切換アクション: `DefaultProfileSwitcher.cs`（`SwitchProfile`）
4. 自動プロファイル: `AutoProfileService.cs`（ウィンドウ検知時）
5. コントローラー接続時: `ControlService.cs`（`PrepareConnectedInputController`）
6. Rename時: `MainWindow.xaml.cs`（`RenameProfileBtn_Click`）

---

### 2.2 4段階トランザクション同期メソッド（SyncProfileListAndControllers）
`MainWindow.xaml.cs` に配置し、保存時・更新時・リネーム時のUI整合性を完全に保証する。

```text
[SyncProfileListAndControllers(string oldProfile = null, string newProfile = null)]
  │
  ├─ ① 【退避 & ガード】
  │     ・フラグ isProfileSyncing = true を設定（ComboBox の SelectionChanged 暴発を遮断）
  │     ・接続中コントローラー各スロット（0〜3）の現在のプロファイル名を string[] activeProfiles に退避
  │
  ├─ ② 【SSOT 再構築】
  │     ・もし Rename 時（oldProfile != null）なら：
  │         - activeProfiles 内の oldProfile を newProfile に置換
  │         - Global.ProfilePath 内の oldProfile を newProfile に置換
  │         - Global.OlderProfilePath 内の oldProfile を newProfile に置換
  │     ・ProfileListHolder.ProfileListCol をクリアして全XMLから再生成（Refresh）
  │       （※イベントガード中のため、Clear() しても TargetException クラッシュは一切発生しない）
  │
  ├─ ③ 【復元（再選択）】
  │     ・コントローラー画面の各スロット ComboBox の選択状態を activeProfiles に基づいて復元
  │     ・フラグ isProfileSyncing = false に戻す
  │
  └─ ④ 【再適用（ホットリロード）】
        ・保存またはリネームの影響を受けたスロットに対し、
          共通メソッド Global.ApplyProfileToSlot(i, activeProfiles[i]) を実行して即座に最新設定を反映！
```

---

## 3. マイクロステップ実装計画

各ステップごとにビルドと単体テスト（`dotnet test`）を実行し、エラーのない状態を維持しながら段階的に進める。

### Step 1: 共通適用メソッド ApplyProfileToSlot の新設
- 対象ファイル: `DS4Windows/DS4Control/ScpUtil.cs`
- 作業内容:
  - `Global` クラス内に `public static bool ApplyProfileToSlot(int slotIndex, string profileName, ProfileChangeSource source = ProfileChangeSource.Manual)` を追加。
  - スロット範囲検証、接続検証、引数検証を行い、`Global.ApplyProfile`（8引数）を呼び出す。
- 検証: `dotnet build` が正常に通ること。

---

### Step 2: 4段階同期メソッド SyncProfileListAndControllers の新設とガード導入
- 対象ファイル: `DS4Windows/DS4Forms/MainWindow.xaml.cs`
- 作業内容:
  - イベントガードフラグ `private bool isProfileSyncing = false;` を追加。
  - 4段階トランザクション同期メソッド `private void SyncProfileListAndControllers(string oldProfile = null, string newProfile = null)` を実装。
  - `SelectProfCombo_SelectionChanged` の先頭に `if (isProfileSyncing) return;` を追加。
- 検証: `dotnet build` が正常に通ること。

---

### Step 3: プロファイル保存契機（契機2）および手動選択（契機1）の統合
- 対象ファイル: `DS4Windows/DS4Forms/MainWindow.xaml.cs`
- 作業内容:
  - `Editor_ProfileSaved` の中身を `SyncProfileListAndControllers();` の呼び出しに置き換え。
  - `SelectProfCombo_SelectionChanged` 内の適用処理を `Global.ApplyProfileToSlot` に差し替え。
- 検証:
  - 実機にてプロファイルを編集し、Emulated Controllerを変更して保存しても クラッシュせず画面が閉じること（課題1の完全解消）。

---

### Step 4: Rename 契機（契機6）の統合
- 対象ファイル: `DS4Windows/DS4Forms/MainWindow.xaml.cs`
- 作業内容:
  - `RenameProfileBtn_Click` において、ファイルのリネーム成功後に `SyncProfileListAndControllers(oldProfile, newProfile);` を呼び出すように改修。
- 検証:
  - 接続中コントローラーが使用しているプロファイルを Rename した場合、UIの ComboBox 表示が空白にならず新名に追従し、内部設定も正常に切り替わること（課題2の完全解消）。

---

### Step 5: 切換アクション（契機3）および自動プロファイル（契機4）の統合
- 対象ファイル:
  - `DS4Windows/Actions/DefaultProfileSwitcher.cs`
  - `DS4Windows/DS4Control/Services/AutoProfileService.cs`
- 作業内容:
  - 各クラスで個別に行われていた `ApplyProfile` の呼び出しを `Global.ApplyProfileToSlot` に差し替え。
- 検証:
  - 全単体テスト（169件）がすべてパスすること。

---

### Step 6: 総合実機テストおよびドキュメント更新
- 作業内容:
  - クラッシュ再現シナリオ、Rename追従シナリオ、ホットリロードシナリオを実機で網羅検証。
  - 完了報告書の作成。

---

## 4. 完了基準（Definition of Done）

1. クラッシュゼロ:
   プロファイル編集画面での [保存] / [適用] を何度連続で行っても、`TargetException` やアプリクラッシュが一切発生しないこと。
2. Rename完全追従:
   接続中のプロファイルをリネームしても、コントローラーの動作・ComboBox表示・内部パスの整合性が維持されること。
3. 一本化の達成:
   アプリ内の全プロファイル適用処理が `ApplyProfileToSlot` を通過していること。
4. 全テスト合格:
   `dotnet test ./DS4WindowsTests/DS4Windows.Actions.Tests.csproj` が 169 passed / 0 failed であること。