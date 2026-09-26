# Phase 5 Step 14 フォローアップ完了報告書
## （サブ設定イベント冪等化・DeltaAccel配線・ProfileActionsインターフェース）

### 1. 概要と目的
本作業は、Phase 5 Step 14（プロファイル設定同期・永続化統合）の完了後に抽出された以下の残余課題および堅牢性向上項目を解決することを目的として実施されました。

1. **サブ設定イベント配線の冪等化（Idempotence）と解除API**:
   - `ProfileSettingsService.WireSubSettingsEvents` の多重呼び出しによるイベントハンドラの二重・三重登録を防止する。
   - `UnwireSubSettingsEvents` メソッドを新設し、プロファイル切替時や再読み込み時に既存の購読を安全・確実に全解除する仕組みを導入。
2. **DeltaAccelSettings / FlickStickSettings のバブリング配線**:
   - スティック詳細設定（`StickOutputSetting.outputSettings`）配下の `DeltaAccelSettings` および `FlickStickSettings` のプロパティ変更（`OnSubPropertyChanged`）を補足し、`ProfileSettingsService.ProfileSettingChanged` に正常バブリングする。
3. **ProfileActions のインターフェース正式配備**:
   - `Global.ProfileActions` に依存していた呼び出し元を DI 化可能にするため、`IProfileSettingsService` に `ProfileActions` プロパティを配備し、双方向同期を実装。
4. **自動単体テストの拡充**:
   - 多重呼び出し時のハンドラ非累積、購読解除後の発火抑止、DeltaAccel 変更通知、ProfileActions の読み書き・通知を網羅するテストケースを追加。

---

### 2. 実施内容と実装詳細

#### (1) `IProfileSettingsService.cs` のインターフェース拡張
- `List<string>[] ProfileActions { get; set; }` を定義に追加。
- `void UnwireSubSettingsEvents(int deviceIndex = -1);` を定義に追加。

#### (2) `ProfileSettingsService.cs` の改修
- **購読解除マップの管理**:
  `private readonly Dictionary<int, List<Action>> _subSettingsUnwireMap` を導入し、スロットごとの解除クロージャを保持。
- **`UnwireSubSettingsEvents` の実装**:
  指定スロット（または全スロット）の全購読解除アクションを例外安全に実行し、リストをクリア。
- **`WireSubSettingsEvents` の冪等化**:
  配線処理の冒頭で `UnwireSubSettingsEvents(deviceIndex)` を呼び出すことで、何回呼び出されても常に最新の1重購読状態を維持。
- **DeltaAccel / FlickStick バブリング配線**:
  ```csharp
  // StickOutputSetting 配下の DeltaAccelSettings / FlickStickSettings のバブリング配線
  if (lsOut != null && lsOut.outputSettings != null)
  {
      if (lsOut.outputSettings.controlSettings?.deltaAccelSettings != null)
      {
          var delta = lsOut.outputSettings.controlSettings.deltaAccelSettings;
          Action<string> hDelta = (prop) => OnSubSettingChanged(currentDev, $"LS_DeltaAccel_{prop}");
          delta.OnSubPropertyChanged += hDelta;
          unwireList.Add(() => delta.OnSubPropertyChanged -= hDelta);
      }
      if (lsOut.outputSettings.flickSettings != null)
      {
          var flick = lsOut.outputSettings.flickSettings;
          Action<string> hFlick = (prop) => OnSubSettingChanged(currentDev, $"LS_FlickStick_{prop}");
          flick.OnSubPropertyChanged += hFlick;
          unwireList.Add(() => flick.OnSubPropertyChanged -= hFlick);
      }
  }
  ```
- **`ProfileActions` の実装**:
  `SafeConfig.profileActions` および `Global.ProfileActions` の双方と整合性を保ちながら、変更時に `ProfileSettingChanged` イベントを発火。

#### (3) `ProfileSettingsServiceSubSettingsTests.cs` のテスト拡充
以下のテストケースを追加し、全件グリーンであることを確認：
- `WireSubSettingsEvents_MultipleCalls_ShouldNotAccumulateHandlers` (多重登録抑止)
- `UnwireSubSettingsEvents_ShouldStopNotifications` (購読解除による発火停止)
- `SubSettingChange_DeltaAccel_ShouldFireProfileSettingChanged` (DeltaAccel の変更バブリング)
- `ProfileActions_GetSet_ShouldPersistAndNotify` (ProfileActions の操作・通知)

---

### 3. テスト・検証結果

| テスト項目 | 検証内容 | 結果 |
|:---|:---|:---:|
| アプリケーションビルド (`DS4WinWPF`) | ソリューション全体のコンパイル | **成功** (エラー 0件) |
| テストプロジェクトビルド (`DS4WindowsTests`) | 全テストプロジェクトのビルド | **成功** (エラー 0件) |
| 単体テスト実行 (`ProfileSettingsServiceSubSettingsTests`) | サブ設定バブリング・冪等性・解除テスト | **全件パス** |
| 統合単体テスト実行 | リポジトリ内の全単体テスト | **全件パス** |

---

### 4. 結論

Phase 5 Step 14 のフォローアップ項目はすべて完了し、プロファイル設定のサブ設定バブリングの冪等性・堅牢性と DI インターフェースの完全性が担保されました。これをもって **Phase 5 の全計画が完了** となります。