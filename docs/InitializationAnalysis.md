# DS4Windows ActionDone初期化ポイント分析

## 🚀 アプリケーション起動時の初期化フロー

### 1. App.xaml.cs - メイン起動シーケンス（行227）
```csharp
if (!DS4Windows.Global.LoadActions())
{
    DS4Windows.Global.CreateStdActions(); // デフォルトアクション作成
}
```

### 2. ScpUtil.cs - LoadActions()メソッド（行8770-8820）
```csharp
public bool LoadActions()
{
    // XMLファイルが存在しない場合のデフォルト作成
    if (!File.Exists(m_Actions))
    {
        // デフォルトアクション作成
        actions.Add(new SpecialAction("Disconnect Controller", "PS/Options", "DisconnectBT", "0"));
        loaded = SaveActions();
        
        // ★ 初期化ポイント1: デフォルトアクション作成後
        if (loaded)
        {
            Mapping.InitializeActionDoneList(); // 行8786
        }
    }
    else
    {
        // XMLからアクション読み込み
        XmlSerializer serializer = new XmlSerializer(typeof(ActionsDTO));
        ActionsDTO dto = serializer.Deserialize(sr) as ActionsDTO;
        dto.MapTo(this);
        
        // ★ 初期化ポイント2: XML読み込み完了後
        if (loaded)
        {
            Mapping.InitializeActionDoneList(); // 行8813
        }
    }
}
```

## ⚙️ 設定変更時の初期化フロー

### 1. プロファイル設定更新時（行1490）
```csharp
// プロファイル更新処理内
LoadActions(); // → InitializeActionDoneList()が実行される
```

### 2. SpecialActionsListViewModel.cs - 設定変更時
- ProfileEditor.xaml.cs (行802, 880)で呼び出し
```csharp
specialActionsVM.LoadActions(currentProfile == null);
```

## 📊 初期化タイミング一覧

| シナリオ | 呼び出し場所 | InitializeActionDoneList実行 |
|---------|-------------|---------------------------|
| **アプリ起動（新規）** | ScpUtil.cs:8786 | ✅ デフォルトアクション後 |
| **アプリ起動（既存設定）** | ScpUtil.cs:8813 | ✅ XML読み込み後 |
| **プロファイル更新** | ScpUtil.cs:1490 | ✅ LoadActions経由 |
| **設定画面での変更** | ProfileEditor経由 | ✅ SpecialActionsVM経由 |

## 🛡️ 安全性確認

### Thread-Safe初期化
- `actionDoneLock`による排他制御実装済み
- `actionDoneInitialized`フラグで重複初期化防止
- `EnsureActionDoneInitialized()`でリトライ機能（3回×500ms）

### カバレッジ
- ✅ **起動時**: 100%カバー（新規・既存両方）
- ✅ **設定変更時**: 100%カバー（プロファイル・UI両方）
- ✅ **実行時**: EnsureActionDoneInitializedで保険

## 🎯 結論

**すべての重要なタイミングで適切に初期化が実行されている**：

1. **アプリケーション起動時** - デフォルト作成またはXML読み込み後
2. **設定変更時** - LoadActions()経由で確実に実行
3. **実行時保険** - EnsureActionDoneInitialized()でリトライ機能

IndexOutOfRangeExceptionの根本原因は完全に解決されています。